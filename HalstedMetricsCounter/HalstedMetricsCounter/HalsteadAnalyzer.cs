using System;
using System.Collections.Generic;
using System.Linq;

namespace HalstedMetricsCounter
{
    public static class HalsteadAnalyzer
    {
        static readonly HashSet<string> SkipKw = new()
        {
            "fn","let","mut","const","static","extern","unsafe","async",
            "move","ref","impl","struct","enum","trait","type","use",
            "mod","where","pub","crate","super","Self","dyn","in"
        };

        static readonly HashSet<string> OpKw = new()
        {
            "if","match","loop","while","for","return",
            "break","continue","await","yield","as"
        };

        static readonly HashSet<string> StdTypes = new()
        {
            "i8","i16","i32","i64","i128","isize",
            "u8","u16","u32","u64","u128","usize",
            "f32","f64","bool","char","str","String",
            "Vec","Option","Result","Box","Rc","Arc",
            "RefCell","Cell","Mutex","RwLock",
            "HashMap","HashSet","BTreeMap","BTreeSet",
            "VecDeque","LinkedList","BinaryHeap",
            "Cow","PhantomData","Pin","Duration","Instant","SystemTime"
        };

        static readonly HashSet<string> LitKw = new()
        {
            "true","false","None","Some","Ok","Err"
        };


        public static Result Analyze(string code)
        {
            var T = Tokenize(code);
            var ops = new Dictionary<string, int>();
            var opds = new Dictionary<string, int>();

            int i = 0, n = T.Count;
            bool skipFnName = false;
            bool afterFnName = false;
            bool lastElse = false;

            while (i < n)
            {
                var t = T[i];

                if (t.Kind == K.Id)
                {
                    string w = t.Value;

                    if (skipFnName) { skipFnName = false; afterFnName = true; i++; continue; }

                    if (SkipKw.Contains(w))
                    {
                        if (w == "fn") skipFnName = true;
                        i++; continue;
                    }
                    if (StdTypes.Contains(w)) { i++; continue; }
                    if (LitKw.Contains(w)) { Add(opds, w); lastElse = false; i++; continue; }

                    if (OpKw.Contains(w))
                    {
                        if (w == "if" && lastElse) { lastElse = false; i++; continue; }
                        Add(ops, w);
                        lastElse = false;
                        i++;
                        if (w == "as")
                        {
                            if (i < n && T[i].Kind == K.Id) i++;
                            if (i < n && T[i].Value == "<")
                            {
                                int d = 1; i++;
                                while (i < n && d > 0)
                                { if (T[i].Value == "<") d++; else if (T[i].Value == ">") d--; i++; }
                            }
                        }
                        continue;
                    }

                    if (w == "else") { lastElse = true; i++; continue; }

                    lastElse = false;

                    if (i + 3 < n && T[i + 1].Value == "." && T[i + 2].Kind == K.Id && T[i + 3].Value == "(")
                    {
                        Add(ops, w + "." + T[i + 2].Value + "()");
                        i += 4;
                        continue;
                    }
                    if (i + 2 < n && T[i + 1].Value == "." && T[i + 2].Kind == K.Id)
                    {
                        Add(opds, w); Add(ops, "."); Add(opds, T[i + 2].Value);
                        i += 3; continue;
                    }
                    if (i + 2 < n && T[i + 1].Value == "!" && T[i + 2].Value == "(")
                    {
                        Add(ops, w + "!()");
                        i += 3;
                        continue;
                    }
                    if (i + 1 < n && T[i + 1].Value == "(")
                    {
                        if (afterFnName) { afterFnName = false; i += 2; continue; }
                        Add(ops, w + "()");
                        i += 2;
                        continue;
                    }

                    Add(opds, w);
                    i++;
                    continue;
                }

                if (t.Kind == K.Num || t.Kind == K.Str) { Add(opds, t.Value); i++; continue; }

                string sv = t.Value;
                if (sv == ")" || sv == "]" || sv == "}" || sv == "{") { i++; continue; }
                if (sv == "[") { Add(ops, "[]"); i++; continue; }
                if (sv == "(")
                {
                    if (afterFnName) { afterFnName = false; i++; continue; }
                    Add(ops, "()");
                    i++;
                    continue;
                }

                Add(ops, sv);
                i++;
            }

            return new Result(ops, opds);
        }

        public static List<HalsteadRow> BuildRows(Result r)
        {
            var opList = r.Operators.OrderByDescending(x => x.Value)
                                     .ThenBy(x => x.Key).ToList();
            var opdList = r.Operands.OrderByDescending(x => x.Value)
                                    .ThenBy(x => x.Key).ToList();

            int max = Math.Max(opList.Count, opdList.Count);
            var rows = new List<HalsteadRow>(max + 1);

            for (int j = 0; j < max; j++)
            {
                var row = new HalsteadRow();
                if (j < opList.Count)
                {
                    row.J1 = (j + 1).ToString();
                    row.Operator = opList[j].Key;
                    row.F1j = opList[j].Value.ToString();
                }
                if (j < opdList.Count)
                {
                    row.J2 = (j + 1).ToString();
                    row.Operand = opdList[j].Key;
                    row.F2j = opdList[j].Value.ToString();
                }
                rows.Add(row);
            }

            var total = new HalsteadRow
            {
                IsTotal = true,
                J1 = "η1 = " + r.Eta1,
                F1j = "N1 = " + r.N1,
                J2 = "η2 = " + r.Eta2,
                F2j = "N2 = " + r.N2
            };
            rows.Add(total);

            return rows;
        }

        static void Add(Dictionary<string, int> d, string k)
        {
            if (d.ContainsKey(k)) d[k]++; else d[k] = 1;
        }


        enum K { Id, Num, Str, Sym }
        class Tk
        {
            public K Kind; public string Value;
            public Tk(K k, string v) { Kind = k; Value = v; }
        }

        static List<Tk> Tokenize(string s)
        {
            var L = new List<Tk>();
            int i = 0, n = s.Length;
            while (i < n)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }

                if (c == '/' && i + 1 < n && s[i + 1] == '/')
                { while (i < n && s[i] != '\n') i++; continue; }
                if (c == '/' && i + 1 < n && s[i + 1] == '*')
                { i += 2; while (i + 1 < n && !(s[i] == '*' && s[i + 1] == '/')) i++; i += 2; continue; }

                if (c == '#' && i + 1 < n && s[i + 1] == '[')
                {
                    i += 2; int d = 1;
                    while (i < n && d > 0)
                    { if (s[i] == '[') d++; else if (s[i] == ']') d--; i++; }
                    continue;
                }

                if (c == '\'')
                {
                    if (i + 2 < n && s[i + 2] == '\'' && s[i + 1] != '\\')
                    { L.Add(new Tk(K.Str, s.Substring(i, 3))); i += 3; continue; }
                    if (i + 3 < n && s[i + 1] == '\\' && s[i + 3] == '\'')
                    { L.Add(new Tk(K.Str, s.Substring(i, 4))); i += 4; continue; }
                    i++; while (i < n && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                    continue;
                }

                if (c == '"')
                {
                    int st = i; i++;
                    while (i < n && s[i] != '"')
                    { if (s[i] == '\\' && i + 1 < n) i++; i++; }
                    i++;
                    L.Add(new Tk(K.Str, s.Substring(st, i - st)));
                    continue;
                }

                if (char.IsDigit(c))
                {
                    int st = i;
                    while (i < n)
                    {
                        char x = s[i];
                        if (char.IsDigit(x) || x == '_') { i++; continue; }
                        if (x == '.' && i + 1 < n && char.IsDigit(s[i + 1])) { i++; continue; }
                        break;
                    }
                    L.Add(new Tk(K.Num, s.Substring(st, i - st)));
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    int st = i;
                    while (i < n && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                    L.Add(new Tk(K.Id, s.Substring(st, i - st)));
                    continue;
                }

                string[] multi = {
                    "<<=", ">>=", "..=",
                    "->", "=>", ">=", "<=", "==", "!=", "&&", "||",
                    "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=",
                    "<<", ">>", "..", "::"
                };
                bool m = false;
                foreach (var op in multi)
                    if (i + op.Length <= n && s.Substring(i, op.Length) == op)
                    { L.Add(new Tk(K.Sym, op)); i += op.Length; m = true; break; }
                if (m) continue;

                L.Add(new Tk(K.Sym, c.ToString()));
                i++;
            }
            return L;
        }
        public class Result
        {
            public Dictionary<string, int> Operators { get; }
            public Dictionary<string, int> Operands { get; }

            public int Eta1 => Operators.Count;
            public int Eta2 => Operands.Count;
            public int Eta => Eta1 + Eta2;

            public int N1 => Operators.Values.Sum();
            public int N2 => Operands.Values.Sum();
            public int N => N1 + N2;

            public double V => N * Math.Log2(Math.Max(Eta, 2));


            public Result(Dictionary<string, int> ops, Dictionary<string, int> opds)
            {
                Operators = ops;
                Operands = opds;
            }
        }
    }
}