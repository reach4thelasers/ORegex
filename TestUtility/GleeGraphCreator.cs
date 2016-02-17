﻿using Microsoft.Glee.Drawing;
using ORegex;
using ORegex.Core.FinitieStateAutomaton;

namespace TestUtility
{
    public sealed class GleeGraphCreator
    {
        public Graph Create<TValue>(FSA<TValue> fsa, PredicateTable<TValue> table)
        {
            var graph = new Graph(fsa.Name);
            FillGraph(graph, fsa, table);
            return graph;
        }

        public Graph Create<TValue>(CFSA<TValue> fsa, PredicateTable<TValue> table)
        {
            var graph = new Graph(fsa.Name);
            FillGraph(graph, fsa, table);
            return graph;
        }

        private static void FillGraph<TValue>(Graph graph, FSA<TValue> fsm, PredicateTable<TValue> table)
        {
            foreach (var t in fsm.Transitions)
            {
                Edge edge = graph.AddEdge("q" + t.StartState, table.GetName(t.Info.Predicate) + "_c" + t.Info.ClassGUID, "q" + t.EndState);
                if (fsm.F.Contains(t.EndState))
                {
                    edge.TargetNode.Attr.Fillcolor = Color.Gray;
                    edge.TargetNode.Attr.Shape = Shape.DoubleCircle;
                }
            }
        }

        private static void FillGraph<TValue>(Graph graph, CFSA<TValue> fsa, PredicateTable<TValue> table)
        {
            foreach (var t in fsa.Transitions)
            {
                Edge edge = graph.AddEdge("q" + t.StartState, table.GetName(t.Condition) + "_c" + t.ClassGUID, "q" + t.EndState);

                if (fsa.IsFinal(t.EndState))
                {
                    edge.TargetNode.Attr.Fillcolor = Microsoft.Glee.Drawing.Color.Gray;
                    edge.TargetNode.Attr.Shape = Shape.DoubleCircle;
                }
            }
        }
    }
}
