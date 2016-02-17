﻿using System;

namespace ORegex.Core.FinitieStateAutomaton
{
    public sealed class FSATransition<TValue>
    {
        public readonly int StartState;

        public readonly int EndState;

        public readonly FSAPredicateEdge<TValue> Info;

        public FSATransition(int from, Func<TValue, bool> condition, int to, int classGUID)
        {
            StartState = from;
            Info = new FSAPredicateEdge<TValue>(condition, classGUID);
            EndState = to;
        }

        public FSATransition(int from, FSAPredicateEdge<TValue> info, int to)
        {
            StartState = from;
            Info = info;
            EndState = to;
        }

        public override bool Equals(object obj)
        {
            var other = (FSATransition<TValue>) obj;
            return other.Info == Info && other.StartState == StartState && other.EndState == EndState;
        }

        public override int GetHashCode()
        {
            const int prime = 123;
            int hash = prime;
            hash += StartState.GetHashCode();
            hash *= prime;
            hash += EndState.GetHashCode();
            hash *= prime;
            hash += Info.GetHashCode();
            return hash;
        }
    }
}
