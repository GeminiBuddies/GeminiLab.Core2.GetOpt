using GeminiLab.Core2;
using GeminiLab.Core2.GetOpt;

namespace Exam {
    public static class OptGetterTester {
        private static string Mix(char c, string s) {
            if (c == '\0') return s ?? "<null>";
            return s == null ? new string(c, 1) : $"{c}|{s}";
        }

        public static void TestOptGetter(OptGetter opt, params string[] p) {
            // GetOptResult r = default;
            Exconsole.WriteLine(">" + p.JoinBy(" "));
            opt.BeginParse(p);

            bool eoa = false;
            while (!eoa) {
                GetOptError err;
                if ((err = opt.GetOpt(out var result)) == GetOptError.EndOfArguments) {
                    eoa = true;
                }

                Exconsole.WriteLine($"  {err}: {result.Type}: \"{Mix(result.Option, result.LongOption)}\", p: {result.Argument ?? "<null>"}, pp: {result.Arguments?.JoinBy(", ") ?? "<null>"}");
            }

            opt.EndParse();
        }
    }
}
