﻿using System;

namespace ORegex.Core.StateMachine
{
    public sealed class FSAPredicateEdge<TValue> : FSAEdgeInfoBase<TValue>
    {
        public readonly Func<TValue, bool> Predicate; 
        public FSAPredicateEdge(Func<TValue, bool> predicate)
        {
            Predicate = predicate.ThrowIfNull();
        }

        public override bool MeetCondition(ObjectStream<TValue> input)
        {
            return Predicate(input.CurrentElement);
        }

        public override bool Equals(object obj)
        {
            var pred = obj as FSAPredicateEdge<TValue>;
            if (pred != null)
            {
                return pred.Predicate.Equals(Predicate);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Predicate.GetHashCode();
        }

        public static bool IsEpsilonPredicate(FSAEdgeInfoBase<TValue> info)
        {
            var pInfo = info as FSAPredicateEdge<TValue>;
            if (pInfo != null)
            {
                return pInfo.Predicate == PredicateConst<TValue>.Epsilon;
            }
            return false;
        }
    }
}
