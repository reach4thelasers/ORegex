using System;
using System.Collections.Generic;
using state = System.Int32;

namespace ORegex.Core.StateMachine
{

    /// <summary>
    /// Implements a non-deterministic finite automata
    /// </summary>
    public sealed class NFA<TValue>
    {
        public state initial;
        public state final;
        private int size;
        // Inputs this NFA responds to
        public HashSet<Func<TValue, bool>> inputs;
        public Func<TValue, bool>[][] transTable;

        public NFA(NFA<TValue> nfa)
        {
            initial = nfa.initial;
            final = nfa.final;
            size = nfa.size;
            inputs = nfa.inputs;
            transTable = nfa.transTable;
        }

        /// <summary>
        /// Constructed with the NFA size (amount of states), the initial state and the
        /// final state
        /// </summary>
        /// <param name="size_">Amount of states.</param>
        /// <param name="initial_">Initial state.</param>
        /// <param name="final_">Final state.</param>
        public NFA(int size_, state initial_, state final_)
        {
            initial = initial_;
            final = final_;
            size = size_;

            IsLegalState(initial);
            IsLegalState(final);

            inputs = new HashSet<Func<TValue, bool>>();

            // Initializes transTable with an "empty graph", no transitions between its
            // states
            transTable = new Func<TValue, bool>[size][];

            for (int i = 0; i < size; ++i)
                transTable[i] = new Func<TValue, bool>[size];
        }

        public bool IsLegalState(state s)
        {
            // We have 'size' states, numbered 0 to size-1
            if (s < 0 || s >= size)
                return false;

            return true;
        }

        /// <summary>
        /// Adds a transition between two states.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="trans"></param>
        public void AddTrans(state from, state to, Func<TValue, bool> trans)
        {
            IsLegalState(from);
            IsLegalState(to);

            transTable[from][to] = trans;

            if (trans != State<TValue>.Epsilon)
                inputs.Add(trans);
        }

        public void AddTrans(Edge<TValue> edge)
        {
            AddTrans(edge.StartState, edge.EndState, edge.Condition);
        }

        /// <summary>
        /// Renames all the NFA's states. For each nfa state: number += shift.
        /// Functionally, this doesn't affect the NFA, it only makes it larger and renames
        /// its states.
        /// </summary>
        /// <param name="shift"></param>
        public void ShiftStates(int shift)
        {
            int newSize = size + shift;

            if (shift < 1)
                return;

            // Creates a new, empty transition table (of the new size).
            var newTransTable = new Func<TValue, bool>[newSize][];

            for (int i = 0; i < newSize; ++i)
                newTransTable[i] = new Func<TValue, bool>[newSize];

            // Copies all the transitions to the new table, at their new locations.
            for (state i = 0; i < size; ++i)
                for (state j = 0; j < size; ++j)
                    newTransTable[i + shift][j + shift] = transTable[i][j];

            // Updates the NFA members.
            size = newSize;
            initial += shift;
            final += shift;
            transTable = newTransTable;
        }

        /// <summary>
        /// Appends a new, empty state to the NFA.
        /// </summary>
        public void AppendEmptyState()
        {
            transTable = Resize(transTable, size + 1);

            size += 1;
        }

        private static Func<TValue, bool>[][] Resize(Func<TValue, bool>[][] transTable, int newSize)
        {
            Func<TValue, bool>[][] newTransTable = new Func<TValue, bool>[newSize][];

            for (int i = 0; i < newSize; ++i)
                newTransTable[i] = new Func<TValue, bool>[newSize];

            for (int i = 0; i <= transTable.Length - 1; i++)
                for (int j = 0; j <= transTable[i].Length - 1; j++)
                {
                    newTransTable[i][j] = transTable[i][j];
                }

            return newTransTable;
        }

        /// <summary>
        /// Returns a set of NFA states from which there is a transition on input symbol
        /// inp from some state s in states.
        /// </summary>
        /// <param name="states"></param>
        /// <param name="inp"></param>
        /// <returns></returns>
        public Set<state> Move(Set<state> states, Func<TValue, bool> inp)
        {
            var result = new Set<state>();

            // For each state in the set of states
            foreach (state state in states)
            {
                int i = 0;

                // For each transition from this state
                foreach (var input in transTable[state])
                {
                    // If the transition is on input inp, add it to the resulting set
                    if (ReferenceEquals(input, inp))
                    {
                        state u = Array.IndexOf(transTable[state], input, i);
                        result.Add(u);
                    }
                    i = i + 1;
                }
            }
            return result;
        }
    }
}