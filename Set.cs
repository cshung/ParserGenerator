namespace ParsingLab2
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public abstract class Set
    {
        public Set()
        {
            this.Atoms = new List<LeafSet>();
        }

        public abstract void FindLeafSets(List<LeafSet> solution);

        public List<LeafSet> Atoms { get; private set; }

        public abstract void Compute(List<LeafSet> allAtoms);
    }

    public abstract class LeafSet : Set
    {
        public LeafSet()
        {
            this.Origins = new List<LeafSet>();
        }

        public static LeafSet Others = new OtherSet();

        public List<LeafSet> Origins { get; private set; }

        public void AddOrigin(LeafSet immediateParent)
        {
            this.Origins.AddRange(immediateParent.Origins);
        }

        public void SetOriginal()
        {
            this.Origins.Add(this);
        }

        public override void Compute(List<LeafSet> allAtoms)
        {
            // no-op - computation of leaf set is done externally
        }

        public abstract bool Contains(char c);

        public class OtherSet : LeafSet
        {
            public override void FindLeafSets(List<LeafSet> solution)
            {
                throw new NotImplementedException();
            }

            public override bool Contains(char c)
            {
                return true;
            }
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class CharSet : LeafSet
    {
        public HashSet<char> Elements { get; private set; }

        public CharSet()
        {
            this.Elements = new HashSet<char>();
        }

        public override void FindLeafSets(List<LeafSet> solution)
        {
            solution.Add(this);
        }

        public override string ToString()
        {
            return string.Format("{{{0}}}", string.Join(", ", Elements));
        }

        public override bool Contains(char c)
        {
            return this.Elements.Contains(c);
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class CharRange : LeafSet
    {
        public char From { get; set; }
        public char To { get; set; }

        public override void FindLeafSets(List<LeafSet> solution)
        {
            solution.Add(this);
        }

        public override string ToString()
        {
            return string.Format("{0} -> {1}", From, To);
        }

        public override bool Contains(char c)
        {
            return c >= this.From && c <= this.To;
        }
    }

    public class Complement : Set
    {
        public Set Operand { get; set; }

        public override void FindLeafSets(List<LeafSet> solution)
        {
            this.Operand.FindLeafSets(solution);
        }

        public override void Compute(List<LeafSet> allAtoms)
        {
            this.Atoms.AddRange(allAtoms.Except(this.Operand.Atoms));
        }
    }

    public class Union : Set
    {
        public Set Left { get; set; }
        public Set Right { get; set; }

        public override void FindLeafSets(List<LeafSet> solution)
        {
            this.Left.FindLeafSets(solution);
            this.Right.FindLeafSets(solution);
        }

        public override void Compute(List<LeafSet> allAtoms)
        {
            this.Atoms.AddRange(this.Left.Atoms.Union(this.Right.Atoms));
        }
    }

    public class Intersect : Set
    {
        public Set Left { get; set; }
        public Set Right { get; set; }

        public override void FindLeafSets(List<LeafSet> solution)
        {
            this.Left.FindLeafSets(solution);
            this.Right.FindLeafSets(solution);
        }

        public override void Compute(List<LeafSet> allAtoms)
        {
            this.Atoms.AddRange(this.Left.Atoms.Intersect(this.Right.Atoms));
        }
    }

    public static class SetManager
    {
        public static List<LeafSet> BreakDown(List<Set> sets)
        {
            // Step 1: Find all leaves
            List<LeafSet> leafSets = FindLeafSets(sets);

            // Step 2: Compute the atoms of the LeafSets
            List<LeafSet> atoms = ComputeAtom(leafSets);

            // Step 3: Compute the leafSets
            foreach (var atom in atoms)
            {
                foreach (var origin in atom.Origins)
                {
                    origin.Atoms.Add(atom);
                }
            }
            atoms.Add(LeafSet.Others);

            // Step 4: Compute all sets
            foreach (var set in sets)
            {
                set.Compute(atoms);
            }

            return atoms;
        }

        private static List<LeafSet> ComputeAtom(List<LeafSet> leafSets)
        {
            var atoms = new List<LeafSet>();
            if (leafSets.Count == 0)
            {
                return atoms;
            }

            foreach (var leafSet in leafSets)
            {
                leafSet.SetOriginal();
            }

            var evaluating = new List<LeafSet>(leafSets);
            while (evaluating.Count >= 1)
            {
                LeafSet checking = evaluating[0];
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
                        List<LeafSet> brokenDown = BreakDown(checking, evaluating[i]);
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

        private static List<LeafSet> BreakDown(LeafSet leafSet1, LeafSet leafSet2)
        {
            if (leafSet1 == null) { throw new ArgumentNullException("leafSet1"); }
            if (leafSet2 == null) { throw new ArgumentNullException("leafSet2"); }
            int leafSet1Type = leafSet1 is CharSet ? 0 : (leafSet1 is CharRange ? 1 : -1);
            int leafSet2Type = leafSet2 is CharSet ? 0 : (leafSet2 is CharRange ? 1 : -1);
            if (leafSet1Type == -1) { throw new ArgumentException("leafSet1", "Unknown leaf set type"); }
            if (leafSet2Type == -1) { throw new ArgumentException("leafSet2", "Unknown leaf set type"); }

            int leafSetTypes = leafSet1Type * 2 + leafSet2Type;
            CharSet cs1 = leafSet1 as CharSet;
            CharSet cs2 = leafSet2 as CharSet;
            CharRange cr1 = leafSet1 as CharRange;
            CharRange cr2 = leafSet2 as CharRange;
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

        private static List<LeafSet> BreakDown(CharSet set1, CharSet set2)
        {
            HashSet<char> intersection = new HashSet<char>(set1.Elements.Intersect(set2.Elements));
            if (intersection.Count > 0)
            {
                List<LeafSet> brokenDown = new List<LeafSet>();
                CharSet left = new CharSet();
                CharSet middle = new CharSet();
                CharSet right = new CharSet();
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

        private static List<LeafSet> BreakDown(CharRange range, CharSet set)
        {
            HashSet<char> intersection = new HashSet<char>(set.Elements.Where(c => c >= range.From && c <= range.To));
            if (intersection.Count > 0)
            {
                List<LeafSet> brokenDown = new List<LeafSet>();
                CharSet middle = new CharSet();
                CharSet right = new CharSet();

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
                        CharRange brokenRange = new CharRange { From = from, To = to };
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

        private static List<LeafSet> BreakDown(CharRange range1, CharRange range2)
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

            List<CharRange> oneOnly = new List<CharRange>();
            List<CharRange> both = new List<CharRange>();
            List<CharRange> twoOnly = new List<CharRange>();
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
                    CharRange range = new CharRange { From = from, To = to };
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
                List<LeafSet> brokenDown = new List<LeafSet>();
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

        private static List<LeafSet> FindLeafSets(List<Set> sets)
        {
            List<LeafSet> solution = new List<LeafSet>();
            foreach (var set in sets)
            {
                set.FindLeafSets(solution);
            }

            return solution;
        }
    }

    public static class HashSetExtensions
    {
        public static void AddRange<T>(this HashSet<T> thisPtr, IEnumerable<T> elements)
        {
            foreach (var elem in elements)
            {
                thisPtr.Add(elem);
            }
        }
    }
}