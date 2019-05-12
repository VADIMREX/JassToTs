using System;
using System.Collections.Generic;
using System.Text;

namespace Jass
{
    class Token
    {
        public string Type = "";
        public int Line = 0;
        public int Col = 0;
        public int Pos = 0;
        public string Text = "";
        public override string ToString() => $"{Line},{Col} [{Type}]: {Text}";
    }

    class JassLexer
    {
        /// <summary> номер строки в исходном коде </summary>
        int line;
        /// <summary> номер символа в строке в исходном коде </summary>
        int pos;
        /// <summary> текст исходного кода </summary>
        string source;
        /// <summary> глобальная позиция символа в исходном коде </summary>
        int i;

        /// <summary> алфавит операторов </summary>
        const string opers = "=+-*/.,";
        /// <summary> алфавит белых символов </summary>
        const string whiteChar = " \t\r";
        /// <summary> алфавит строк </summary>
        const string strChar = "\"'";
        /// <summary> алфавит скобок </summary>
        const string brac = "()[]";

        readonly string[] keywords = new[] {
            ""
        };

        /// <summary> 
        /// Проверить на перевод строки.
        /// Инкрементирует переменную line и 
        /// обнуляет переменную pos,
        /// если обнаружен перевод строки.
        /// </summary>
        /// <returns>true если обнаружен перевод строки</returns>
        bool LineBreak()
        {
            if ('\n' != source[i]) return false;
            line++;
            pos = 0;
            return true;
        }

        /// <summary> Попытаться распарсить комментарий </summary>
        /// <returns> null если не удалось распарсить комментарий </returns>
        Token TryParseComment()
        {
            if (i == source.Length - 1) return null;
            if ('/' != source[i + 1]) return null;
            int j = i,
                l = line,
                p = pos;
            var s = source.Substring(i, 2);
            i += 2;
            for (; i < source.Length; i++, pos++)
            {
                s += source[i];
                if (LineBreak()) break;
            }

            if (i < source.Length) i--;
            return new Token { Col = p, Line = l, Pos = j, Text = s, Type = "comm" };
        }

        /// <summary> Попытаться распарсить число </summary>
        /// <returns> null если не удалось распарсить число </returns>
        Token TryParseNumber()
        {
            var s = "";
            int j = i,
                l = line,
                p = pos;
            bool isXFound = false;
            bool isDotFound = false;
            bool isNumFound = false;
            for (; i < source.Length; s += source[i], i++, pos++)
            {
                // условия при которых продолжаем
                if ('0' <= source[i] && source[i] <= '9')
                {
                    isNumFound = true;
                    continue;
                }
                if ('.' == source[i])
                {
                    // ситуация вида ..
                    if (!isNumFound && isDotFound) throw new Exception($"Line {l}, Col {p}: multiple dot");
                    if (isDotFound) throw new Exception($"Line {l}, Col {p}: wrong number, multiple dot");
                    if (isXFound) throw new Exception($"Line {l}, Col {p}: wrong number, dot inside hexadecimal");
                    isDotFound = true;
                    continue;
                }
                if ('x' == source[i])
                {
                    // ситуация вида .x
                    if (!isNumFound)
                    {
                        i = j;
                        return null;
                    }
                    if (isXFound) throw new Exception($"Line {l}, Col {p}: wrong number, multiple x in hexadecimal");
                    if (isDotFound) throw new Exception($"Line {l}, Col {p}: wrong number, x inside float");
                    isXFound = true;
                    continue;
                }
                // условия при которых завершаем
                if (opers.Contains(source[i])) break;
                if (brac.Contains(source[i])) break;
                if (LineBreak()) break;
                if (whiteChar.Contains(source[i])) break;
                // наткнулись на символ не число, не оператор, не перевод строки, не белый символ
                if (isDotFound)
                {
                    i = j;
                    return null;
                }
                throw new Exception($"Line {l}, Col {p}: not a number");
            }
            if (isXFound && '0' != s[0]) throw new Exception($"Line {l}, Col {p}: wrong number, not valid hexadecimal");
            var typ = "int";
            if (isXFound) typ = "hex";
            if (isDotFound) typ = "real";

            if (i < source.Length) i--;
            return new Token { Col = p, Line = l, Pos = j, Text = s, Type = typ };
        }

        /// <summary> Попытаться распарсить оператор </summary>
        Token TryParseOperator()
        {
            var s = "";
            int j = i,
                l = line,
                p = pos;

            for (; i < source.Length; s += source[i], i++, pos++)
            {
                if (opers.Contains(source[i])) continue;
                break;
            }

            if (i < source.Length) i--;
            return new Token { Col = p, Line = l, Pos = j, Text = s, Type = "oper" };
        }

        /// <summary> Попытаться распарсить строку </summary>
        Token TryParseString()
        {
            char eoc = source[i];

            var s = $"{source[i]}";
            int j = i,
                l = line,
                p = pos;

            if (i == source.Length - 1) throw new Exception($"Line {l}, Col {p}: unclosed string");

            i++;
            for (; i < source.Length; i++, pos++)
            {
                s += source[i];
                if (source[i] == eoc && source[i - 1] != '\\') break;
                if (LineBreak()) ; // todo: надо проверить как реагирует обычный jass
            }

            if (i < source.Length) i--;
            return new Token { Col = p, Line = l, Pos = j, Text = s, Type = eoc == '"' ? "dstr" : "sstr" };
        }

        /// <summary> Попытаться распарсить имя </summary>
        Token TryParseName()
        {
            var s = "";
            int j = i,
                l = line,
                p = pos;

            for (; i < source.Length; s += source[i], i++, pos++)
            {
                // допустимые символы
                if ('a' <= source[i] && source[i] <= 'z') continue;
                if ('A' <= source[i] && source[i] <= 'Z') continue;
                if ('0' <= source[i] && source[i] <= '9') continue;
                if ('_' == source[i]) continue;
                // не допустимые символы
                if (whiteChar.Contains(source[i])) break;
                if (LineBreak()) break;
                if (brac.Contains(source[i])) break;
                if (opers.Contains(source[i])) break;
                if (strChar.Contains(source[i])) break; // в некоторых случаях может быть норм
                // левые символы
                throw new Exception($"Line {l}, Col {p}: wrong identifier");
            }
            // todo сделать проверку на ключевое слово

            if (i < source.Length) i--;
            return new Token { Col = p, Line = l, Pos = j, Text = s, Type = "name" };
        }

        /// <summary> распарсить исходный код на токены </summary>
        /// <param name="source"> исходный код на языке jass</param>
        /// <returns> список из токенов </returns>
        public List<Token> Tokenize(string source)
        {
            this.source = source;
            i = 0;
            line = 0;
            pos = 0;
            var tokens = new List<Token>();

            Token tok = null;
            for (; i < source.Length; i++, pos++)
            {
                if ('/' == source[i])
                {
                    // попытка распознать комментарий
                    tok = TryParseComment();
                    if (null != tok)
                    {
                        tokens.Add(tok);
                        continue;
                    }
                }
                if (strChar.Contains(source[i]))
                {
                    // попытка распознать строку
                    tok = TryParseString();
                    if (null != tok)
                    {
                        tokens.Add(tok);
                        continue;
                    }
                }
                if ('0' <= source[i] && source[i] <= '9' || '.' == source[i])
                {
                    // попытка распарсить число
                    tok = TryParseNumber();
                    if (null != tok)
                    {
                        tokens.Add(tok);
                        continue;
                    }
                }
                if (opers.Contains(source[i]))
                {
                    // попытка распарсить оператор
                    tok = TryParseOperator();
                    if (null != tok)
                    {
                        tokens.Add(tok);
                        continue;
                    }
                }
                if (whiteChar.Contains(source[i]))
                {
                    // игнорим пробелы
                    continue;
                }
                if (brac.Contains(source[i]))
                {
                    var typ = "";
                    var s = $"{source[i]}";
                    switch (source[i])
                    {
                        case '(': typ = "lbra"; break;
                        case ')': typ = "rbra"; break;
                        case '[': typ = "lind"; break;
                        case ']': typ = "rind"; break;
                    }
                    tok = new Token { Col = pos, Line = line, Pos = i, Text = s, Type = typ };
                    tokens.Add(tok);
                    continue;
                }
                if (LineBreak())
                {
                    // записываем конец строки
                    tokens.Add(new Token { Col = pos, Line = line, Pos = i, Text = "\n", Type = "ln" });
                    continue;
                }
                tok = TryParseName();
                tokens.Add(tok);
            }
            return tokens;
        }
    }
}
