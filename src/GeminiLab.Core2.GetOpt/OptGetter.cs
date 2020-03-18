using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;


namespace GeminiLab.Core2.GetOpt {
    public enum OptionType {
        Switch,
        Parameterized,
        MultiParameterized,
    }

    public enum GetOptError {
        NoError,
        UnknownOption,
        UnexpectedValue,
        UnexpectedAttachedValue,
        ValueExpected,
        EndOfArguments,
        /// <summary>Something unexpected happened. Please report if 'GetOpt' return this value.</summary>
        InternalError,
    }

    public enum GetOptResultType {
        Invalid,
        ShortOption,
        LongOption,
        LongAlias,
        Values,
    }

    public struct GetOptResult {
        public GetOptResultType Type;
        public OptionType OptionType;
        public char Option;
        public string? LongOption;
        public string? Argument;
        public string[]? Arguments;
    }

    public sealed class OptGetter {
        private readonly Dictionary<char, OptionType> _options = new Dictionary<char, OptionType>();
        private readonly Dictionary<string, OptionType> _longOptions = new Dictionary<string, OptionType>();
        private readonly Dictionary<string, char> _aliases = new Dictionary<string, char>();

        public void AddOption(char option, OptionType type) {
            if (!Enum.IsDefined(typeof(OptionType), type)) throw new ArgumentOutOfRangeException(nameof(type));

            _options[option] = type;
        }

        public void AddOption(char option, OptionType type, params string[] longAliases) {
            AddOption(option, type);
            AddAlias(option, longAliases);
        }

        public void AddOption(string longOption, OptionType type) {
            if (!Enum.IsDefined(typeof(OptionType), type)) throw new ArgumentOutOfRangeException(nameof(type));

            _longOptions.Add(longOption ?? throw new ArgumentNullException(nameof(longOption)), type);
        }

        public void AddAlias(char option, params string[] longAliases) {
            if (!_options.ContainsKey(option)) throw new ArgumentOutOfRangeException(nameof(option));

            foreach (var alias in longAliases ?? throw new ArgumentNullException(nameof(longAliases))) {
                if (alias == null) throw new ArgumentOutOfRangeException(nameof(longAliases));

                _aliases.Add(alias, option);
            }
        }

        public bool EnableDashDash { get; set; } = true;

        public bool AcceptOptionAsValue { get; set; } = false;

        // TODO: implement this
        // Enabled:
        //   -ABC = -A BC
        // Disabled:
        //   -ABC = -A -B -C
        // public bool EnableAttachedValue { get; set; } = true;

        // return true => c = option
        // c == '\0' => long option
        private bool isOption(string v, out char c) {
            c = '\0';
            if (v.Length <= 1 || v[0] != '-') return false;

            if (v[1] != '-') {
                c = v[1];
                return true;
            }

            if (v.Length > 2) return true;
            if (EnableDashDash) return false;

            c = '-';
            return true;
        }

        private bool isEnabledDashDash(string v) { return EnableDashDash && v == "--"; }

        private bool tryReadValue([MaybeNullWhen(false)] out string v, bool acceptOptionAsValue) {
            if (_args == null || _argp >= _argc) {
                v = null!;
                return false;
            }

            v = _args[_argp];

            if (!acceptOptionAsValue && (isOption(v, out _) || isEnabledDashDash(v))) {
                v = null!;
                return false;
            }

            ++_argp;
            return true;
        }

        private string[]? _args;
        private int _argc;
        private int _argp;

        public void BeginParse(params string[]? arguments) {
            _args = arguments;

            if (_args != null) {
                _argc = _args.Length;
                _argp = 0;
            }
        }

        public GetOptError GetOpt(out GetOptResult result) {
            /*
            result = new GetOptResult {
                Type = GetOptResultType.Invalid,
                OptionType = OptionType.Switch,
                Option = '\0',
                LongOption = null,
                Argument = null,
                Arguments = null
            };
            */
            result = default;

            if (_args == null || _argp >= _argc) return GetOptError.EndOfArguments;

            var v = _args[_argp++];

            if (isOption(v, out var c)) {
                // use result.Argument to stash attached value
                if (c != '\0' && v.Length > 2) { result.Argument = v.Substring(2); }

                if (c != '\0') {
                    result.Type = GetOptResultType.ShortOption;
                    result.Option = c;

                    if (!_options.TryGetValue(c, out result.OptionType)) { return GetOptError.UnknownOption; }
                } else {
                    var option = v.Substring(2);
                    result.Type = GetOptResultType.LongOption;
                    result.LongOption = option;

                    if (!_longOptions.TryGetValue(option, out result.OptionType)) {
                        if (_aliases.TryGetValue(option, out result.Option)) {
                            result.Type = GetOptResultType.LongAlias;
                            if (!_options.TryGetValue(result.Option, out result.OptionType)) return GetOptError.InternalError;
                        } else {
                            return GetOptError.UnknownOption;
                        }
                    }
                }

                if (result.OptionType == OptionType.Switch) {
                    return result.Argument != null ? GetOptError.UnexpectedAttachedValue : GetOptError.NoError;
                } else if (result.OptionType == OptionType.Parameterized) {
                    if (result.Argument != null || tryReadValue(out result.Argument, AcceptOptionAsValue)) { return GetOptError.NoError; }

                    return GetOptError.ValueExpected;
                } else if (result.OptionType == OptionType.MultiParameterized) {
                    var p = new List<string>();

                    if (result.Argument != null) {
                        p.Add(result.Argument);
                        result.Argument = null;
                    }

                    while (tryReadValue(out var s, false)) p.Add(s);

                    result.Arguments = p.ToArray();
                    return GetOptError.NoError;
                } else { return GetOptError.InternalError; }
            } else if (isEnabledDashDash(v)) {
                var len = _argc - _argp;
                result.Arguments = new string[len];
                result.Type = GetOptResultType.Values;

                Array.Copy(_args, _argp, result.Arguments, 0, len);
                _argp = _argc;
                return GetOptError.NoError;
            } else {
                result.Argument = v;
                return GetOptError.UnexpectedValue;
            }
        }

        public void EndParse() { _args = null; }
    }
}
