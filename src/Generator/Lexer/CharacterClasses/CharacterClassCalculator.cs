namespace Andrew.ParserGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class CharacterClassCalculator
    {
        public static List<LeafCharacterClass> BreakDown(List<CharacterClass> characterClasses)
        {
            // Step 1: Find all leaves
            List<LeafCharacterClass> leafCharacterClasses = FindLeafCharacterClasses(characterClasses);

            // Step 2: Compute the atoms of the LeafSets
            List<LeafCharacterClass> atoms = ComputeAtom(leafCharacterClasses);

            // Step 3: Compute the leafSets
            foreach (var atom in atoms)
            {
                foreach (var origin in atom.Origins)
                {
                    origin.Atoms.Add(atom);
                }
            }

            // It is important for matching that the other class is in the last of the atom list for matching purpose
            atoms.Add(LeafCharacterClass.Others);

            // Step 4: Compute all sets recursively
            foreach (var characterClass in characterClasses)
            {
                characterClass.PickAtoms(atoms);
            }

            return atoms;
        }

        private static List<LeafCharacterClass> ComputeAtom(List<LeafCharacterClass> leafCharacterClasses)
        {
            var atoms = new List<LeafCharacterClass>();
            if (leafCharacterClasses.Count == 0)
            {
                return atoms;
            }

            foreach (var leafCharacterClass in leafCharacterClasses)
            {
                leafCharacterClass.SetOriginal();
            }

            var evaluating = new List<LeafCharacterClass>(leafCharacterClasses);
            while (evaluating.Count >= 1)
            {
                LeafCharacterClass checking = evaluating[0];
                if (evaluating.Count == 1)
                {
                    evaluating.Remove(checking);
                    atoms.Add(checking);
                }
                else
                {
                    bool hasIntersection = false;
                    for (int i = 1; i < evaluating.Count; i++)
                    {
                        List<LeafCharacterClass> brokenDown = BreakDown(checking, evaluating[i]);
                        if (brokenDown != null)
                        {
                            hasIntersection = true;
                            evaluating.Remove(evaluating[i]); // Remove by index need to happen first
                            evaluating.Remove(checking);
                            evaluating.AddRange(brokenDown);
                            break;
                        }
                    }
                    if (!hasIntersection)
                    {
                        evaluating.Remove(checking);
                        atoms.Add(checking);
                    }
                }
            }
            return atoms;
        }

        private static List<LeafCharacterClass> BreakDown(LeafCharacterClass class1, LeafCharacterClass class2)
        {
            if (class1 == null) { throw new ArgumentNullException("class1"); }
            if (class2 == null) { throw new ArgumentNullException("class2"); }
            int class1Type = class1 is ExplicitCharacterClass ? 0 : (class1 is RangeCharacterClass ? 1 : -1);
            int class2Type = class2 is ExplicitCharacterClass ? 0 : (class2 is RangeCharacterClass ? 1 : -1);
            if (class1Type == -1) { throw new ArgumentException("class1", "Unknown leaf character class type"); }
            if (class2Type == -1) { throw new ArgumentException("class2", "Unknown leaf character class type"); }

            int leafSetTypes = class1Type * 2 + class2Type;
            ExplicitCharacterClass cs1 = class1 as ExplicitCharacterClass;
            ExplicitCharacterClass cs2 = class2 as ExplicitCharacterClass;
            RangeCharacterClass cr1 = class1 as RangeCharacterClass;
            RangeCharacterClass cr2 = class2 as RangeCharacterClass;
            switch (leafSetTypes)
            {
                case 0:
                    return BreakDown(cs1, cs2);
                case 1:
                    return BreakDown(cr2, cs1);
                case 2:
                    return BreakDown(cr1, cs2);
                case 3:
                    return BreakDown(cr1, cr2);
                default:
                    throw new InvalidOperationException("Should never reach here");
            }
        }

        private static List<LeafCharacterClass> BreakDown(ExplicitCharacterClass set1, ExplicitCharacterClass set2)
        {
            HashSet<char> intersection = new HashSet<char>(set1.Elements.Intersect(set2.Elements));
            if (intersection.Count > 0)
            {
                List<LeafCharacterClass> brokenDown = new List<LeafCharacterClass>();
                ExplicitCharacterClass left = new ExplicitCharacterClass();
                ExplicitCharacterClass middle = new ExplicitCharacterClass();
                ExplicitCharacterClass right = new ExplicitCharacterClass();
                left.Elements.AddRange(set1.Elements.Except(intersection));
                middle.Elements.AddRange(intersection);
                right.Elements.AddRange(set2.Elements.Except(intersection));
                if (left.Elements.Count > 0)
                {
                    brokenDown.Add(left);
                    left.AddOrigin(set1);
                };

                brokenDown.Add(middle);
                middle.AddOrigin(set1);
                middle.AddOrigin(set2);

                if (right.Elements.Count > 0)
                {
                    brokenDown.Add(right);
                    right.AddOrigin(set2);
                };

                return brokenDown;
            }
            else
            {
                return null;
            }
        }

        private static List<LeafCharacterClass> BreakDown(RangeCharacterClass range, ExplicitCharacterClass set)
        {
            HashSet<char> intersection = new HashSet<char>(set.Elements.Where(c => c >= range.From && c <= range.To));
            if (intersection.Count > 0)
            {
                List<LeafCharacterClass> brokenDown = new List<LeafCharacterClass>();
                ExplicitCharacterClass middle = new ExplicitCharacterClass();
                ExplicitCharacterClass right = new ExplicitCharacterClass();

                List<int> nonExclusiveIndexes = new List<int>(intersection.Select(t => (int)t));
                nonExclusiveIndexes.Add(range.From - 1);
                nonExclusiveIndexes.Add(range.To + 1);

                nonExclusiveIndexes.Sort();

                for (int i = 0; i < nonExclusiveIndexes.Count - 1; i++)
                {
                    char from = (char)(nonExclusiveIndexes[i] + 1);
                    char to = (char)(nonExclusiveIndexes[i + 1] - 1);
                    if (from <= to)
                    {
                        RangeCharacterClass brokenRange = new RangeCharacterClass { From = from, To = to };
                        brokenDown.Add(brokenRange);
                        brokenRange.AddOrigin(range);
                    }
                }

                middle.Elements.AddRange(intersection);
                right.Elements.AddRange(set.Elements.Except(intersection));

                brokenDown.Add(middle);
                middle.AddOrigin(range);
                middle.AddOrigin(set);

                if (right.Elements.Count > 0)
                {
                    brokenDown.Add(right);
                    right.AddOrigin(set);
                }
                return brokenDown;
            }
            else
            {
                return null;
            }
        }

        private static List<LeafCharacterClass> BreakDown(RangeCharacterClass range1, RangeCharacterClass range2)
        {
            // <Position, Set Number, In = 0/Out = 1
            var events = new List<Tuple<char, int, int>>();

            events.Add(Tuple.Create(range1.From, 1, 0));
            events.Add(Tuple.Create(range1.To, 1, 1));
            events.Add(Tuple.Create(range2.From, 2, 0));
            events.Add(Tuple.Create(range2.To, 2, 1));

            events.Sort((a, b) => a.Item1 - b.Item1 != 0 ? (a.Item1 - b.Item1) : (a.Item3 - b.Item3));

            bool in1 = false;
            bool in2 = false;

            List<RangeCharacterClass> oneOnly = new List<RangeCharacterClass>();
            List<RangeCharacterClass> both = new List<RangeCharacterClass>();
            List<RangeCharacterClass> twoOnly = new List<RangeCharacterClass>();
            for (int i = 0; i < 3; i++)
            {
                if (events[i].Item3 == 0)
                {
                    if (events[i].Item2 == 1)
                    {
                        in1 = true;
                    }
                    else
                    {
                        in2 = true;
                    }
                }
                else
                {
                    if (events[i].Item2 == 1)
                    {
                        in1 = false;
                    }
                    else
                    {
                        in2 = false;
                    }
                }

                char from = events[i].Item3 == 0 ? events[i].Item1 : (char)(events[i].Item1 + 1);
                char to = events[i + 1].Item3 == 0 ? (char)(events[i + 1].Item1 - 1) : events[i + 1].Item1;

                if (from <= to)
                {
                    RangeCharacterClass range = new RangeCharacterClass { From = from, To = to };
                    int state = (in1 ? 0 : 1) * 2 + (in2 ? 0 : 1);
                    switch (state)
                    {
                        case 0:
                            // We were in both
                            both.Add(range);
                            break;
                        case 1:
                            // We were in 1 but not in 2.
                            oneOnly.Add(range);
                            break;
                        case 2:
                            // We were in 2 but not in 1.
                            twoOnly.Add(range);
                            break;
                        case 3:
                            // Do nothing, this belongs to none
                            break;
                    }
                }
            }

            if (both.Count == 0)
            {
                return null;
            }
            else
            {
                List<LeafCharacterClass> brokenDown = new List<LeafCharacterClass>();
                foreach (var oneOnlySet in oneOnly)
                {
                    brokenDown.Add(oneOnlySet);
                    oneOnlySet.AddOrigin(range1);
                }
                foreach (var bothSet in both)
                {
                    brokenDown.Add(bothSet);
                    bothSet.AddOrigin(range1);
                    bothSet.AddOrigin(range2);
                }
                foreach (var twoOnlySet in twoOnly)
                {
                    brokenDown.Add(twoOnlySet);
                    twoOnlySet.AddOrigin(range2);
                }

                return brokenDown;
            }
        }

        private static List<LeafCharacterClass> FindLeafCharacterClasses(List<CharacterClass> characterClasses)
        {
            List<LeafCharacterClass> solution = new List<LeafCharacterClass>();
            foreach (var characterClass in characterClasses)
            {
                characterClass.FindLeafCharacterClasses(solution);
            }

            return solution;
        }
    }
}