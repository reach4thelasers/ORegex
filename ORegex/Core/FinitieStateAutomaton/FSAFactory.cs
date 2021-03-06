﻿using System;
using System.Linq;
using Eocron.Core.Ast;
using Eocron.Core.Ast.GroupQuantifiers;
using Eocron.Core.FinitieStateAutomaton.Predicates;
using Eocron.Core.Parse;

namespace Eocron.Core.FinitieStateAutomaton
{
    public sealed class FSAFactory<TValue>
    {
        private readonly FSAOperator<TValue> _fsaOperator = new FSAOperator<TValue>();

        public FSA<TValue> CreateRawFsa(AstRootNode root, ORegexOptions options)
        {
            var result = new FSA<TValue>(root.CaptureGroupNames[0]) {CaptureNames = root.CaptureGroupNames};
            var start = result.NewState();
            var end = result.NewState();
            Evaluate(start, end, result, root, options);
            result.AddFinal(end);
            result.AddStart(start);
            return result;
        }

        public FiniteAutomaton<TValue> Create(AstRootNode root, ORegexOptions options)
        {
            var nfa = CreateRawFsa(root, options);
            if (options.HasFlag(ORegexOptions.ReversePattern))
                //if options set to reverse pattern we should reverse initial nfa, and we good.
            {
                nfa = _fsaOperator.ReverseFsa(nfa);
            }
            var dfa = _fsaOperator.MinimizeFsa(nfa);
            return new FiniteAutomaton<TValue>(new CFSA<TValue>(dfa), new CFSA<TValue>(nfa));
        }

        public void Evaluate(int start, int end, FSA<TValue> fsa, AstNodeBase node, ORegexOptions options)
        {
            if (node is AstAtomNode<TValue>)
            {
                EvaluateAtom(start, end, fsa, (AstAtomNode<TValue>) node, options);
            }
            else if (node is AstConcatNode)
            {
                EvaluateConcat(start, end, fsa, (AstConcatNode) node, options);
            }
            else if (node is AstOrNode)
            {
                EvaluateOr(start, end, fsa, (AstOrNode) node, options);
            }
            else if (node is AstRepeatNode)
            {
                EvaluateRepeat(start, end, fsa, (AstRepeatNode) node, options);
            }
            else if (node is AstRootNode)
            {
                EvaluateRoot(start, end, fsa, (AstRootNode) node, options);
            }
            else
            {
                throw new NotImplementedException(node.GetType().Name);
            }
        }

        private void EvaluateRoot(int start, int end, FSA<TValue> fsa, AstRootNode astRootNode, ORegexOptions options)
        {
            fsa.ExactBegin = astRootNode.MatchBegin;
            fsa.ExactEnd = astRootNode.MatchEnd;
            Evaluate(start, end, fsa, astRootNode.Regex, options);
        }

        private void EvaluateRepeat(int start, int end, FSA<TValue> fsa, AstRepeatNode astRepeatNode,
            ORegexOptions options)
        {
            var toRepeat = astRepeatNode.Argument;
            var prev = start;
            for (int i = 0; i < astRepeatNode.MinCount; i++)
            {
                var next = CreateNewState(fsa);
                Evaluate(prev, next, fsa, toRepeat, options);
                prev = next;
            }

            if (astRepeatNode.MaxCount == int.MaxValue)
            {
                RepeatZeroOrInfinite(prev, end, fsa, toRepeat, astRepeatNode.IsLazy, options);
            }
            else
            {
                int count = astRepeatNode.MaxCount - astRepeatNode.MinCount - 1;
                int next;
                for (int i = 0; i < count; i++)
                {
                    next = CreateNewState(fsa);
                    RepeatZeroOrOne(prev, next, fsa, toRepeat, astRepeatNode.IsLazy, options);
                    prev = next;
                }
                next = end;
                RepeatZeroOrOne(prev, next, fsa, toRepeat, astRepeatNode.IsLazy, options);
            }
        }

        private void RepeatZeroOrOne(int start, int end, FSA<TValue> fsa, AstNodeBase node, bool isLasy,
            ORegexOptions options)
        {
            if (isLasy)
            {
                fsa.AddEpsilonTransition(start, end);
                Evaluate(start, end, fsa, node, options);
            }
            else
            {
                Evaluate(start, end, fsa, node, options);
                fsa.AddEpsilonTransition(start, end);
            }
        }

        private void RepeatZeroOrInfinite(int start, int end, FSA<TValue> fsa, AstNodeBase predicate, bool isLasy,
            ORegexOptions options)
        {
            var tmp = CreateNewState(fsa);
            if (isLasy)
            {
                fsa.AddEpsilonTransition(start, end);
                fsa.AddEpsilonTransition(tmp, end);
                Evaluate(tmp, tmp, fsa, predicate, options);
                fsa.AddEpsilonTransition(start, tmp);
            }
            else
            {
                Evaluate(tmp, tmp, fsa, predicate, options);
                fsa.AddEpsilonTransition(tmp, end);

                fsa.AddEpsilonTransition(start, tmp);
                fsa.AddEpsilonTransition(start, end);
            }
        }

        private void EvaluateOr(int start, int end, FSA<TValue> fsa, AstOrNode node, ORegexOptions options)
        {
            foreach (var child in node.GetChildren())
            {
                Evaluate(start, end, fsa, child, options);
            }
        }

        private void EvaluateConcat(int start, int end, FSA<TValue> fsa, AstConcatNode node, ORegexOptions options)
        {
            if (node is AstGroupNode)
            {
                var group = (AstGroupNode) node;
                if (group.Quantifier != null)
                {
                    if (group.Quantifier is CaptureQuantifier)
                    {
                        var captureQ = (CaptureQuantifier) group.Quantifier;
                        var sys = new SystemPredicateEdge<TValue>("#capture")
                        {
                            IsCapture = true,
                            CaptureName = captureQ.CaptureName,
                            CaptureId = captureQ.CaptureId
                        };

                        var startTmp = CreateNewState(fsa);
                        fsa.AddTransition(start, sys, startTmp);
                        start = startTmp;
                        var endTmp = CreateNewState(fsa);
                        fsa.AddTransition(endTmp, sys, end);
                        end = endTmp;
                    }
                    else if (group.Quantifier is LookAheadQuantifier)
                    {
                        var lookQ = (LookAheadQuantifier) group.Quantifier;
                        EvaluateLook(start, end, fsa, lookQ, group, options);
                        return;
                    }
                }
            }

            var prev = start;
            int next;
            var children = node.GetChildren().ToArray();
            for (int i = 0; i < children.Length - 1; i++)
            {
                next = CreateNewState(fsa);
                Evaluate(prev, next, fsa, children[i], options);
                prev = next;
            }
            next = end;
            Evaluate(prev, next, fsa, children[children.Length - 1], options);
        }

        private void EvaluateAtom(int start, int end, FSA<TValue> fsa, AstAtomNode<TValue> node, ORegexOptions options)
        {
            EvaluateCondition(start, end, fsa, node.Condition, options);
        }

        private void EvaluateLook(int start, int end, FSA<TValue> fsa, LookAheadQuantifier quantifier,
            AstConcatNode concatNode, ORegexOptions options)
        {
            bool isBehind = options.HasFlag(ORegexOptions.ReversePattern) ? !quantifier.IsBehind : quantifier.IsBehind;
            bool isNegative = quantifier.IsNegative;

            var condOptions = isBehind ? ORegexOptions.RightToLeft : ORegexOptions.None;
            var concat = new AstConcatNode(concatNode.Children, concatNode.Range);
            var root = new AstRootNode(concat, true, false, concat.Range,
                new[] {ORegexAstFactory<TValue>.MainCaptureName});
            var fa = Create(root, condOptions);
            var oregex = new ORegex<TValue>(fa, condOptions);

            var func = new ORegexPredicateEdge<TValue>("#look", oregex, isNegative, isBehind);
            EvaluateCondition(start, end, fsa, func, options);
        }

        private void EvaluateCondition(int start, int end, FSA<TValue> fsa, PredicateEdgeBase<TValue> condition,
            ORegexOptions options)
        {
            fsa.AddTransition(start, condition, end);
        }

        private int CreateNewState(FSA<TValue> fsa)
        {
            return fsa.NewState();
        }
    }
}
