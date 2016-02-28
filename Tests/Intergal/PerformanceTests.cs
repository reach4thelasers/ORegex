﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Tests.Core;
using Stopwatch = NUnit.Framework.Compatibility.Stopwatch;

namespace Tests.Intergal
{
    [TestFixture]
    public sealed class PerformanceTests
    {
        [Test]
        public void BuildTest()
        {
            const string pattern = "({x}+{x}+){y}+{x}|{x}|{x}|{x}|{y}|{x}[^{x}][^{y}]?|(?<g1>{x}+?|(?<g2>{x}?)?)";
            Trace.WriteLine(string.Format("Input pattern: {0}", pattern));
            const int iterCount = 10;
            const int repeatCount = 10;
            for (int j = 0; j < iterCount; j++)
            {
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < repeatCount; i++)
                {
                    var oregex = new DebugORegex(pattern);
                }
                sw.Stop();
                Trace.WriteLine(string.Format("Done {0} in {1}", repeatCount, sw.Elapsed));
            }
        }

        [Test]
        public void ExhaustingRunTest()
        {
            for (int i = 30; i <= 80; i += 10)
            {
                var str = string.Join("",Enumerable.Repeat("x",i));
                PerformanceTest("{x}+{x}+{y}+", "x+x+y+", str, 1, false);
                Trace.WriteLine("############################################");
            }
        }

        [Test]
        public void SimpleRunTest()
        {
            var str = "asdfffffffffffffffffweage" +
                      "rgerxxxxxxxxxxffffffffff" +
                      "aasdfgargaergerg" +
                      "fffffweagergerxxxxxxxxxxxxxxxxx" +
                      "xxxxxxxyyyyxxxxxxxxfffffffyffffffffweagerge" +
                      "rxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" +
                      "ywfffffffffffffffweagergerxxxxxxxxxxxxxxxxxx" +
                      "xxxxxxefwefwefadxxxxxxxxxxxxffffffff" +
                      "fffffffweagerge" +
                      "rxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

            PerformanceTest("{x}+{x}+{y}+", "x+x+y+", str, 20, true);
            Trace.WriteLine("############################################");
        }

        private static void PerformanceTest(string oregexPattern, string regexPattern, string inputText, int iterCount, bool outputTotal = false)
        {
            var input = inputText.ToCharArray();
            var oregex = new DebugORegex(oregexPattern);
            var regex = new Regex(regexPattern, RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.Compiled);
            Trace.WriteLine(string.Format("Input string: {0}", inputText));

            var regexCount = new TimeSpan();
            var oregexCount = new TimeSpan();
            for (int j = 0; j < iterCount; j++)
            {
                var sw = Stopwatch.StartNew();
                var array1 = oregex.Matches(input).ToArray();
                sw.Stop();
                oregexCount += sw.Elapsed;
                long last = sw.ElapsedTicks;
                Trace.WriteLine(string.Format("ORegex done in\t{0}", sw.Elapsed));

                sw = Stopwatch.StartNew();
                var array2 = regex.Matches(inputText).Cast<Match>().ToArray();
                sw.Stop();
                regexCount += sw.Elapsed;
                Trace.WriteLine(string.Format("Regex done in\t{0}", sw.Elapsed));
            }

            if (outputTotal)
            {
                Trace.WriteLine("############################################");
                Trace.WriteLine(string.Format("ORegex total\t{0}", oregexCount));
                Trace.WriteLine(string.Format("Regex total\t{0}", regexCount));
            }
        }
    }
}