# ParserGenerator
ParserGenerator is a prototype LR(1) parser generator written to practice the theory I have learnt in the compiler course.

# Regular expression
This project includes a regular expression compiler that takes a regular expression string to a state machine that can do a regular expression match without backtracking. Here are the features that it supports

- The `|` operator to choose one of the options on the left or on the right
- The `()` operator to group a sub regular expression
- The `[]` operator to denote a set of allowed characters
- The `[^]` operator to denote a set of disallowed characters
- The `[-]` operator to denote a range of characters
- The `*` operator to represent repeating 0 or many times

## Parsing expression grammar
The regular expression engine used a parser generated by this project (i.e. bootstrapping) to parse the expression string. The parser will create an expression tree composed of `RegularExpression` objects.

## Character classes
To begin the regular expression compilation, the regular expression compiler discover (through a top down recursive call to `FindLeafCharacterClasses`) the leaf character classes and break them down into mutually exclusive character classes. To avoid a mouthful of character classes, we abbreviate leaf character classes as leaves and mutually exclusive character classes as atoms.

As shown above, a character class can be a complicated object, it could be a complement of a range, or it could be an explicit set of characters. To make computation possible, only explicit character set and ranges are called a leaf, the rest is composed with it through `Union`, `Intersect` and `Complement`, the standard set operations. 

Once we have all the leaves, we break it down into atoms. To do so, we repeatedly try to `BreakDown` pairs. For each pair, either they have an intersection, or not. If they do, we break them down into 3 disjoint sets and put it back to the queue. If a set doesn't intersect with any pairs, then it is an atom.

As a final twist, we need an `OtherSet` to represent the set of all other characters, this will be needed for complement.

Once we get all the atoms, we let the character classes to pick the atoms that is used to build that character class. This is done through the `PickAtoms` method, again, this is called top down recursively.

To summarize this phase, each character class will be associated with a list of mutually disjoint character classes.

## Finite state automatas
After computing the atoms, the regular expression is transformed into a non-deterministic finite state automata in the standard way. The only special thing is that using multiple edges (which is allowed in non-deterministic finite automata), we create one edge per atom for `CharSetRegularExpression`. The standard subset construction algorithm is then used to convert that into a deterministic finite automata. Together with the atoms, the deterministic finite automata is packaged into the `CompiledRegularExpression`. 

## Grammar
TODO