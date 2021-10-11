using System;
using System.Collections.Generic;
using System.Text;

namespace Jass
{
    public class JassLexer
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
        const string opers = "=+-*/!><,";
        /// <summary> алфавит белых символов </summary>
        const string whiteChar = " \t\r";
        /// <summary> алфавит строк </summary>
        const string strChar = "\"";//"\"'";
        /// <summary> алфавит скобок </summary>
        const string brac = "()[]";

        /// <summary> справочник ключевых слов </summary>
        readonly Dictionary<string, string> keywords = new Dictionary<string, string> {
            // Базовые типы
            { "integer", TokenKind.btyp },
            { "real",    TokenKind.btyp },
            { "boolean", TokenKind.btyp },
            { "string",  TokenKind.btyp },
            { "handle",  TokenKind.btyp },
            { "code",    TokenKind.btyp },
            // 
            { "nothing", TokenKind.btyp },
            // 
            { "type",        TokenKind.kwd },
            { "extends",     TokenKind.kwd },
            { "globals",     TokenKind.kwd },
            { "endglobals",  TokenKind.kwd },
            { "constant",    TokenKind.kwd },
            { "native",      TokenKind.kwd },
            { "takes",       TokenKind.kwd },
            { "returns",     TokenKind.kwd },
            { "function",    TokenKind.kwd },
            { "endfunction", TokenKind.kwd },
            //
            { "local", TokenKind.kwd },
            { "array", TokenKind.kwd },
            //
            { "set",      TokenKind.kwd },
            { "call",     TokenKind.kwd },
            { "if",       TokenKind.kwd },
            { "then",     TokenKind.kwd },
            { "endif",    TokenKind.kwd },
            { "else",     TokenKind.kwd },
            { "elseif",   TokenKind.kwd },
            { "loop",     TokenKind.kwd },
            { "endloop",  TokenKind.kwd },
            { "exitwhen", TokenKind.kwd },
            { "return",   TokenKind.kwd },
            { "debug",    TokenKind.kwd },
            //
            { "and", TokenKind.oper },
            { "or",  TokenKind.oper },
            //
            { "not", TokenKind.oper },
            //
            { "null",  TokenKind.@null },
            { "true",  TokenKind.@bool },
            { "false", TokenKind.@bool },
        };

        /// <summary> режим совместимости с YDWE </summary>
        bool isYdweCompatible;

        List<string> operators = new List<string> { "=", ",", "+", "-", "*", "/", ">", "<", "==", "!=", ">=", "<=" };

        /// <summary> создать Лексер </summary>
        /// <param name="isYdweCompatible"> режим совместимости с YDWE, по умолчанию false </param>
        public JassLexer(bool isYdweCompatible = false)  => this.isYdweCompatible = isYdweCompatible;

        /// <summary> 
        /// Проверить на перевод строки.
        /// Инкрементирует переменную line и 
        /// обнуляет переменную pos,
        /// если обнаружен перевод строки.
        /// </summary>
        /// <returns>true если обнаружен перевод строки</returns>
        bool LineBreak(bool set)
        {
            if ('\n' != source[i]) return false;
            if (set) return true;
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
                if ('\r' == source[i]) continue;
                if (LineBreak(true)) break;
                s += source[i];
            }

            if (i < source.Length) i--;
            return new Token { Col = p, Line = l, Pos = j, Text = s, Kind = TokenKind.lcom };
        }

        /// <summary> Попытаться распарсить YDWE макрос </summary>
        /// <returns> null если не удалось распарсить макрос </returns>
        Token TryParseYDWEMacro()
        {
            if (i == source.Length - 1) return null;
            int j = i,
                l = line,
                p = pos;
            #warning Убрать // после добавления интерпретатора макросов
            var s = "//#";
            i++;
            for (; i < source.Length; i++, pos++)
            {
                if ('\r' == source[i]) continue;
                if (LineBreak(true)) break;
                s += source[i];
            }

            if (i < source.Length) i--;
            return new Token { Col = p, Line = l, Pos = j, Text = s, Kind = TokenKind.ymacr };
        }

        /// <summary> Попытаться распарсить число </summary>
        /// <returns> null если не удалось распарсить число </returns>
        Token TryParseNumber()
        {
            var s = "";
            int j = i,
                l = line,
                p = pos;
            bool isOct = '0' == source[i];  // octal    0[0-7]*
            bool isHex = '$' == source[i];  // hex      $[0-9a-fA-F]+
            bool isXFound = false;          //          0[xX][0-9a-fA-F]+
            bool isDotFound = false;        // real     [0-9]+\.[0-9]*|\.[0-9]+
            bool isNumFound = false;
            for (; i < source.Length; s += source[i], i++, pos++)
            {
                // условия при которых продолжаем
                if ('0' <= source[i] && source[i] <= '9')
                {
                    if (isOct && '8' <= source[i]) JassException.Error(l, p, "wrong number: wrong octal number");
                    isNumFound = true;
                    continue;
                }
                // hex в формате 0[xX][0-9a-fA-F]+
                if (isOct && 1 == s.Length && ('x' == source[i] || 'X' == source[i]))
                {
                    isOct = false;
                    isHex = true;
                    isXFound = true;
                    continue;
                }
                if (isHex && (('A' <= source[i] && source[i] <= 'F') ||
                              ('a' <= source[i] && source[i] <= 'f') ||
                              (!isNumFound && '$' == source[i])))
                {
                    isNumFound = true;
                    continue;
                }
                if ('.' == source[i])
                {
                    if (isDotFound) JassException.Error(l, p, "wrong number: multiple dot");
                    if (isHex) JassException.Error(l, p, "wrong number: dot inside hex");
                    if (isOct && s.Length > 1) JassException.Error(l, p, "wrong number: dot inside octadecimal");
                    isOct = false;
                    isDotFound = true;
                    continue;
                }
                // условия при которых завершаем
                if (opers.Contains(source[i])) break;
                if (brac.Contains(source[i])) break;
                if (LineBreak(true)) break;
                if (whiteChar.Contains(source[i])) break;
                // наткнулись на символ не число, не оператор, не перевод строки, не белый символ
                if (isDotFound || !isNumFound)
                {
                    i = j;
                    return null;
                }
                JassException.Error(l, p, "wrong number: not a number");
            }
            var typ = TokenKind.ndec;
            if (isOct) typ = TokenKind.oct;
            if (isHex) typ = isXFound ? TokenKind.xhex : TokenKind.dhex;
            if (isDotFound) typ = TokenKind.real;

            if (i < source.Length) i--;
            return new Token { Col = p, Line = l, Pos = j, Text = s, Kind = typ };
        }

        /// <summary> Попытаться распарсить оператор </summary>
        Token TryParseOperator()
        {
            // костыль
            if (',' == source[i])
                return new Token { Col = pos, Line = line, Pos = i, Text = ",", Kind = TokenKind.oper };

            var s = "";
            int j = i,
                l = line,
                p = pos;

            for (; i < source.Length; s += source[i], i++, pos++)
            {
                if (!opers.Contains(source[i])) break;
                if (2 == s.Length) break;
            }

            if (!operators.Contains(s))
            {
                s = s.Substring(0, 1);
                i--;
            }
            
            if (i < source.Length) i--;
            return new Token { Col = p, Line = l, Pos = j, Text = s, Kind = TokenKind.oper };
        }

        /// <summary> Попытаться распарсить число из 4х ASCII символов </summary>
        Token TryParse4AsciiInt()
        {
            var tok = TryParseString();
            if (tok.Text.Length > 6) JassException.Error(tok.Line, tok.Col, "wrong number: more than 4 ascii symbols");
            foreach (var c in tok.Text)
                if (c > '\u00ff')
                    JassException.Error(tok.Line, tok.Col, "wrong number: non ascii symbol");
            // Надо проверить как оригинальный компилятор относится к 'a\'bc' последовательности
            tok.Kind = TokenKind.adec;
            return tok;
        }

        /// <summary> Попытаться распарсить строку </summary>
        Token TryParseString()
        {
            char eoc = source[i];

            var s = $"{source[i]}";
            int j = i,
                l = line,
                p = pos;

            if (i == source.Length - 1) JassException.Error(l, p, "unclosed string");

            i++;
            for (; i < source.Length; i++, pos++)
            {
                s += source[i];
                if (source[i] == eoc && source[i - 1] != '\\') break;
                if (LineBreak(true))
                    #warning todo: надо проверить как реагирует обычный jass
                    ;
            }

            //if (i < source.Length) i--;
            //return new Token { Col = p, Line = l, Pos = j, Text = s, Type = eoc == '"' ? TokenType.dstr : TokenType.sstr };
            return new Token { Col = p, Line = l, Pos = j, Text = s, Kind = TokenKind.dstr };
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
                if (s.Length > 0 && '0' <= source[i] && source[i] <= '9') continue;
                if (s.Length > 0 && '_' == source[i]) continue;
                // не допустимые символы
                if (whiteChar.Contains(source[i])) break;
                if (LineBreak(true)) break;
                if (brac.Contains(source[i])) break;
                if (opers.Contains(source[i])) break;
                if (strChar.Contains(source[i])) break; // в некоторых случаях может быть норм
                // левые символы
                JassException.Error(l, p, "wrong identifier: unknown symbol");
            }
            var typ = TokenKind.name;
            if (keywords.ContainsKey(s)) typ = keywords[s];
            if ('_' == s[s.Length - 1]) JassException.Error(l, p, "wrong identifier: ends with \"_\"");

            if (i < source.Length) i--;
            return new Token { Col = p, Line = l, Pos = j, Text = s, Kind = typ };
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
                if (isYdweCompatible && '#' == source[i]) {
                    tok = TryParseYDWEMacro();
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
                // int из 4 ascii символов
                if ('\'' == source[i])
                {
                    tok = TryParse4AsciiInt();
                    if (null != tok)
                    {
                        tokens.Add(tok);
                        continue;
                    }
                }
                if ('0' <= source[i] && source[i] <= '9' || '.' == source[i] || '$' == source[i])
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
                        case '(': typ = TokenKind.lbra; break;
                        case ')': typ = TokenKind.rbra; break;
                        case '[': typ = TokenKind.lind; break;
                        case ']': typ = TokenKind.rind; break;
                    }
                    tok = new Token { Col = pos, Line = line, Pos = i, Text = s, Kind = typ };
                    tokens.Add(tok);
                    continue;
                }
                if (LineBreak(false))
                {
                    // записываем конец строки
                    tokens.Add(new Token { Col = pos, Line = line, Pos = i, Text = "\n", Kind = TokenKind.ln });
                    continue;
                }
                tok = TryParseName();
                tokens.Add(tok);
            }
            return tokens;
        }
    }
}
