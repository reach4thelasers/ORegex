﻿using ORegex.Core.StateMachine;

namespace ORegex.Core.Parse
{
    public sealed class ORegexCompiler<TValue>
    {
        private readonly ORegexParser<TValue> _parser;
        private readonly FSAFactory<TValue> _stb;
        public ORegexCompiler()
        {
            _parser = new ORegexParser<TValue>();
            _stb = new FSAFactory<TValue>();
        }

        public FSA<TValue> Build(string input, PredicateTable<TValue> predicateTable)
        {
            var ast = _parser.Parse(input, predicateTable);
            var dfa = _stb.Create(ast);
            return dfa;
        }
    }
}