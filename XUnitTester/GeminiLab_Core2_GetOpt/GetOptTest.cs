using System;
using System.Collections.Generic;
using GeminiLab.Core2.GetOpt;
using Xunit;

namespace XUnitTester.GeminiLab_Core2_GetOpt {
    public class GetOptTest {
        private static OptGetter OptGetter() {
            var rv = new OptGetter();

            rv.AddOption('a', OptionType.Switch, "alpha");
            rv.AddOption('b', OptionType.Parameterized, "bravo");
            rv.AddOption('c', OptionType.MultiParameterized, "charlie");
            rv.AddOption("long-long", OptionType.Switch);
            rv.AddOption('x', OptionType.Parameterized);
            rv.AddAlias('x', "extreme", "extremely");

            rv.AcceptOptionAsValue = false;
            rv.EnableDashDash = true;

            return rv;
        }

        private static void AssertGetOptResultEqual(in GetOptResult actual, GetOptResultType type, 
            OptionType optionType, char option, string longOption, string argument, string[] arguments) {
            Assert.Equal(type, actual.Type);
            Assert.Equal(optionType, actual.OptionType);
            Assert.Equal(option, actual.Option);
            Assert.Equal(longOption, actual.LongOption);
            Assert.Equal(argument, actual.Argument);
            Assert.Equal((IEnumerable<string>)arguments, actual.Arguments);
        }

        [Fact]
        public static void NormalOptionTest() {
            var opt = OptGetter();

            opt.BeginParse("--bravo", "beta", "-c", "p", "p1", "p2", "p3", "--alpha", "--long-long");

            Assert.Equal(GetOptError.NoError, opt.GetOpt(out var result));
            AssertGetOptResultEqual(result, GetOptResultType.LongAlias, OptionType.Parameterized, 'b', "bravo", "beta", null);
            Assert.Equal(GetOptError.NoError, opt.GetOpt(out result));
            AssertGetOptResultEqual(result, GetOptResultType.ShortOption, OptionType.MultiParameterized, 'c', null, null, new []{ "p", "p1", "p2", "p3" });
            Assert.Equal(GetOptError.NoError, opt.GetOpt(out result));
            AssertGetOptResultEqual(result, GetOptResultType.LongAlias, OptionType.Switch, 'a', "alpha", null, null);
            Assert.Equal(GetOptError.NoError, opt.GetOpt(out result));
            AssertGetOptResultEqual(result, GetOptResultType.LongOption, OptionType.Switch, '\0', "long-long", null, null);
            Assert.Equal(GetOptError.EndOfArguments, opt.GetOpt(out _));

            opt.EndParse();
        }

        [Fact]
        public static void UninitializedTest() {
            var opt = OptGetter();

            Assert.Equal(GetOptError.EndOfArguments, opt.GetOpt(out _));
        }

        [Fact]
        public static void DashDashTest() {
            var opt = OptGetter();

            opt.BeginParse("-a", "--", "1", "2", "--bravo");

            Assert.Equal(GetOptError.NoError, opt.GetOpt(out var result));
            AssertGetOptResultEqual(result, GetOptResultType.ShortOption, OptionType.Switch, 'a', null, null, null);
            Assert.Equal(GetOptError.NoError, opt.GetOpt(out result));
            AssertGetOptResultEqual(result, GetOptResultType.Values, OptionType.Switch, '\0', null, null, new[] { "1", "2", "--bravo" });
            Assert.Equal(GetOptError.EndOfArguments, opt.GetOpt(out _));

            opt.EndParse();
        }

        [Fact]
        public static void ErrorTest() {
            var opt = OptGetter();
            opt.EnableDashDash = false;

            opt.BeginParse("-a", "--", "1", "2", "--bravo", "-ax", "--exception");

            Assert.Equal(GetOptError.NoError, opt.GetOpt(out var result));
            AssertGetOptResultEqual(result, GetOptResultType.ShortOption, OptionType.Switch, 'a', null, null, null);
            Assert.Equal(GetOptError.UnknownOption, opt.GetOpt(out result));
            AssertGetOptResultEqual(result, GetOptResultType.ShortOption, OptionType.Switch, '-', null, null, null);
            Assert.Equal(GetOptError.UnexpectedValue, opt.GetOpt(out result));
            AssertGetOptResultEqual(result, GetOptResultType.Invalid, OptionType.Switch, '\0', null, "1", null);
            Assert.Equal(GetOptError.UnexpectedValue, opt.GetOpt(out result));
            AssertGetOptResultEqual(result, GetOptResultType.Invalid, OptionType.Switch, '\0', null, "2", null);
            Assert.Equal(GetOptError.ValueExpected, opt.GetOpt(out result));
            AssertGetOptResultEqual(result, GetOptResultType.LongAlias, OptionType.Parameterized, 'b', "bravo", null, null);
            Assert.Equal(GetOptError.UnexpectedAttachedValue, opt.GetOpt(out result));
            AssertGetOptResultEqual(result, GetOptResultType.ShortOption, OptionType.Switch, 'a', null, "x", null);
            Assert.Equal(GetOptError.UnknownOption, opt.GetOpt(out result));
            AssertGetOptResultEqual(result, GetOptResultType.LongOption, OptionType.Switch, '\0', "exception", null, null);
            Assert.Equal(GetOptError.EndOfArguments, opt.GetOpt(out _));

            opt.EndParse();
        }

        [Fact]
        public static void AttachedValueTest() {
            var opt = OptGetter();

            opt.BeginParse("-a", "-beta", "-chr", "hs", "ht");

            Assert.Equal(GetOptError.NoError, opt.GetOpt(out var result));
            AssertGetOptResultEqual(result, GetOptResultType.ShortOption, OptionType.Switch, 'a', null, null, null);
            Assert.Equal(GetOptError.NoError, opt.GetOpt(out result));
            AssertGetOptResultEqual(result, GetOptResultType.ShortOption, OptionType.Parameterized, 'b', null, "eta", null);
            Assert.Equal(GetOptError.NoError, opt.GetOpt(out result));
            AssertGetOptResultEqual(result, GetOptResultType.ShortOption, OptionType.MultiParameterized, 'c', null, null, new []{ "hr", "hs", "ht" });
            Assert.Equal(GetOptError.EndOfArguments, opt.GetOpt(out _));

            opt.EndParse();
        }

        [Fact]
        public static void IllArgumentTest() {
            var opt = OptGetter();

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                opt.AddOption('i', (OptionType)7777, "invalid");
            });

            Assert.Throws<ArgumentNullException>(() => {
                opt.AddOption(null, OptionType.Switch);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                opt.AddOption("invalid", (OptionType)7777);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                opt.AddAlias('i', "1");
            });

            Assert.Throws<ArgumentNullException>(() => {
                opt.AddAlias('a', longAliases: null);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                opt.AddAlias('a', longAliases: new[] { "apollo", null });
            });
        }
    }
}
