package org.vsx.jass;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class JassLexer {
    /** номер строки в исходном коде */
    int line;
    /** номер символа в строке в исходном коде */
    int pos;
    /** текст исходного кода */
    String source;
    /** глобальная позиция символа в исходном коде */
    int i;

    /** алфавит операторов */
    final String opers = "=+-*/!><,";
    /** алфавит белых символов */
    final String whiteChar = " \t\r";
    /** алфавит строк */
    final String strChar = "\"";// "\"'";
    /** алфавит скобок */
    final String brac = "()[]";

    /** справочник ключевых слов */
    final HashMap<String,String> keywords = new HashMap<String,String>(Map.ofEntries(
        // Базовые типы
        Map.entry("integer", TokenKind.btyp),
        Map.entry("real",    TokenKind.btyp),
        Map.entry("boolean", TokenKind.btyp),
        Map.entry("String",  TokenKind.btyp),
        Map.entry("handle",  TokenKind.btyp),
        Map.entry("code",    TokenKind.btyp),
        // 
        Map.entry("nothing", TokenKind.btyp),
        // 
        Map.entry("type",        TokenKind.kwd),
        Map.entry("extends",     TokenKind.kwd),
        Map.entry("globals",     TokenKind.kwd),
        Map.entry("endglobals",  TokenKind.kwd),
        Map.entry("constant",    TokenKind.kwd),
        Map.entry("native",      TokenKind.kwd),
        Map.entry("takes",       TokenKind.kwd),
        Map.entry("returns",     TokenKind.kwd),
        Map.entry("function",    TokenKind.kwd),
        Map.entry("endfunction", TokenKind.kwd),
        //
        Map.entry("local", TokenKind.kwd),
        Map.entry("array", TokenKind.kwd),
        //
        Map.entry("set",      TokenKind.kwd),
        Map.entry("call",     TokenKind.kwd),
        Map.entry("if",       TokenKind.kwd),
        Map.entry("then",     TokenKind.kwd),
        Map.entry("endif",    TokenKind.kwd),
        Map.entry("else",     TokenKind.kwd),
        Map.entry("elseif",   TokenKind.kwd),
        Map.entry("loop",     TokenKind.kwd),
        Map.entry("endloop",  TokenKind.kwd),
        Map.entry("exitwhen", TokenKind.kwd),
        Map.entry("return",   TokenKind.kwd),
        Map.entry("debug",    TokenKind.kwd),
        //
        Map.entry("and", TokenKind.oper),
        Map.entry("or",  TokenKind.oper),
        //
        Map.entry("not", TokenKind.oper),
        //
        Map.entry("null",  TokenKind._null),
        Map.entry("true",  TokenKind._bool),
        Map.entry("false", TokenKind._bool)));
    
    /** режим совместимости с YDWE */
    boolean isYdweCompatible;

    ArrayList<String> operators = new ArrayList<String>(List.of("=", ",", "+", "-", "*", "/", ">", "<", "==", "!=", ">=", "<=" ));

    /** 
     * создать Лексер, без совместимости с YDWE
     */
    public JassLexer() {
        this(false);
    }

    /** 
     * создать Лексер
     * @param isYdweCompatible режим совместимости с YDWE
     */
    public JassLexer(boolean isYdweCompatible) {
        this.isYdweCompatible = isYdweCompatible;
    }

    /** 
     * Проверить на перевод строки.
     * Инкрементирует переменную line и 
     * обнуляет переменную pos,
     * если обнаружен перевод строки.
     * 
     * @return true если обнаружен перевод строки 
     */
    boolean LineBreak(boolean set) {
        if ('\n' != source.charAt(i))
            return false;
        if (set)
            return true;
        line++;
        pos = 0;
        return true;
    }

    /** 
     * Попытаться распарсить комментарий
     * @return  null если не удалось распарсить комментарий 
     */
    Token TryParseComment()
    {
        if (i == source.length() - 1) return null;
        if ('/' != source.charAt(i + 1)) return null;
        int j = i,
            l = line,
            p = pos;
        var s = source.substring(i, i + 2);
        i += 2;
        for (; i < source.length(); i++, pos++)
        {
            if ('\r' == source.charAt(i)) continue;
            if (LineBreak(true)) break;
            s += source.charAt(i);
        }

        if (i < source.length()) i--;
        return new Token(p, l, j, s, TokenKind.lcom);
    }

    /**
     * Попытаться распарсить YDWE макрос
     * @return null если не удалось распарсить макрос
     */
    Token TryParseYDWEMacro()
    {
        if (i == source.length() - 1) return null;
        int j = i,
            l = line,
            p = pos;
        var s = "//#";
        i++;
        for (; i < source.length(); i++, pos++)
        {
            if ('\r' == source.charAt(i)) continue;
            if (LineBreak(true)) break;
            s += source.charAt(i);
        }

        if (i < source.length()) i--;
        return new Token(p, l, j, s, TokenKind.ymacr);
    }

    /** 
     * Попытаться распарсить число
     * @return  null если не удалось распарсить число 
     */
    Token TryParseNumber() throws JassException
    {
        var s = "";
        int j = i,
            l = line,
            p = pos;
        boolean isOct = '0' == source.charAt(i);  // octal    0[0-7]*
        boolean isHex = '$' == source.charAt(i);  // hex      $[0-9a-fA-F]+
        boolean isXFound = false;                 //          0[xX][0-9a-fA-F]+
        boolean isDotFound = false;               // real     [0-9]+\.[0-9]*|\.[0-9]+
        boolean isNumFound = false;
        for (; i < source.length(); s += source.charAt(i), i++, pos++)
        {
            // условия при которых продолжаем
            if ('0' <= source.charAt(i) && source.charAt(i) <= '9')
            {
                if (isOct && '8' <= source.charAt(i)) throw new JassException(l, p, "wrong number: wrong octal number");
                isNumFound = true;
                continue;
            }
            // hex в формате 0[xX][0-9a-fA-F]+
            if (isOct && 1 == s.length() && ('x' == source.charAt(i) || 'X' == source.charAt(i)))
            {
                isOct = false;
                isHex = true;
                isXFound = true;
                continue;
            }
            if (isHex && (('A' <= source.charAt(i) && source.charAt(i) <= 'F') ||
                            ('a' <= source.charAt(i) && source.charAt(i) <= 'f') ||
                            (!isNumFound && '$' == source.charAt(i))))
            {
                isNumFound = true;
                continue;
            }
            if ('.' == source.charAt(i))
            {
                if (isDotFound) throw new JassException(l, p, "wrong number: multiple dot");
                if (isHex) throw new JassException(l, p, "wrong number: dot inside hex");
                if (isOct && s.length() > 1) throw new JassException(l, p, "wrong number: dot inside octadecimal");
                isOct = false;
                isDotFound = true;
                continue;
            }
            // условия при которых завершаем
            if (opers.contains(source.subSequence(i, i + 1))) break;
            if (brac.contains(source.subSequence(i, i + 1))) break;
            if (LineBreak(true)) break;
            if (whiteChar.contains(source.subSequence(i, i + 1))) break;
            // наткнулись на символ не число, не оператор, не перевод строки, не белый символ
            if (isDotFound || !isNumFound)
            {
                i = j;
                return null;
            }
            throw new JassException(l, p, "wrong number: not a number");
        }
        var typ = TokenKind.ndec;
        if (isOct) typ = TokenKind.oct;
        if (isHex) typ = isXFound ? TokenKind.xhex : TokenKind.dhex;
        if (isDotFound) typ = TokenKind.real;

        if (i < source.length()) i--;
        return new Token(p, l, j, s, typ);
    }

    /** Попытаться распарсить оператор */
    Token TryParseOperator() throws JassException
    {
        // костыль
        if (',' == source.charAt(i))
            return new Token(pos, line, i, ",", TokenKind.oper);

        var s = "";
        int j = i,
            l = line,
            p = pos;

        for (; i < source.length(); s += source.charAt(i), i++, pos++)
        {
            if (!opers.contains(source.subSequence(i, i + 1))) break;
            if (2 == s.length()) break;
        }

        if (!operators.contains(s))
        {
            s = s.substring(0, 1);
            i--;
        }

        if (i < source.length()) i--;
        return new Token(p, l, j, s, TokenKind.oper);
    }

    /** Попытаться распарсить число из 4х ASCII символов */
    Token TryParse4AsciiInt() throws JassException
    {
        var tok = TryParseString();
        if (tok.Text.length() > 6) throw new JassException(tok.Line, tok.Col, "wrong number: more than 4 ascii symbols");
        for (char c: tok.Text.toCharArray())
            if (c > '\u00ff')
                throw new JassException(tok.Line, tok.Col, "wrong number: non ascii symbol");
        // Надо проверить как оригинальный компилятор относится к 'a\'bc' последовательности
        tok.Kind = TokenKind.adec;
        return tok;
    }

    /** Попытаться распарсить строку */
    Token TryParseString() throws JassException
    {
        char eoc = source.charAt(i);

        var s = source.substring(i, i + 1);
        int j = i,
            l = line,
            p = pos;

        if (i == source.length() - 1) throw new JassException(l, p, "unclosed String");

        i++;
        for (; i < source.length(); i++, pos++)
        {
            s += source.charAt(i);
            if (source.charAt(i) == eoc && source.charAt(i - 1) != '\\') break;
            if (LineBreak(true))
                /** @warning todo: надо проверить как реагирует обычный jass */
                ;
        }

        //if (i < source.length()) i--;
        //return new Token { Col = p, Line = l, Pos = j, Text = s, Type = eoc == '"' ? TokenType.dstr : TokenType.sstr };
        return new Token(p, l, j, s, TokenKind.dstr);
    }

    /** Попытаться распарсить имя */
    Token TryParseName() throws JassException
    {
        var s = "";
        int j = i,
            l = line,
            p = pos;

        for (; i < source.length(); s += source.charAt(i), i++, pos++)
        {
            // допустимые символы
            if ('a' <= source.charAt(i) && source.charAt(i) <= 'z') continue;
            if ('A' <= source.charAt(i) && source.charAt(i) <= 'Z') continue;
            if (s.length() > 0 && '0' <= source.charAt(i) && source.charAt(i) <= '9') continue;
            if (s.length() > 0 && '_' == source.charAt(i)) continue;
            // не допустимые символы
            if (whiteChar.contains(source.subSequence(i, i + 1))) break;
            if (LineBreak(true)) break;
            if (brac.contains(source.subSequence(i, i + 1))) break;
            if (opers.contains(source.subSequence(i, i + 1))) break;
            if (strChar.contains(source.subSequence(i, i + 1))) break; // в некоторых случаях может быть норм
            // левые символы
            throw new JassException(l, p, "wrong identifier: unknown symbol");
        }
        var typ = TokenKind.name;
        if (keywords.containsKey(s)) typ = keywords.get(s);
        if ('_' == s.charAt(s.length() - 1)) throw new JassException(l, p, "wrong identifier: ends with \"_\"");

        if (i < source.length()) i--;
        return new Token(p, l, j, s, typ);
    }

    /** 
     * распарсить исходный код на токены
     * @param source исходный код на языке jass
     * @return  список из токенов
     */
    public List<Token> Tokenize(String source) throws JassException
    {
        this.source = source;
        i = 0;
        line = 0;
        pos = 0;
        var tokens = new ArrayList<Token>();

        Token tok = null;
        for (; i < source.length(); i++, pos++)
        {
            if ('/' == source.charAt(i))
            {
                // попытка распознать комментарий
                tok = TryParseComment();
                if (null != tok)
                {
                    tokens.add(tok);
                    continue;
                }
            }
            if (isYdweCompatible && '#' == source.charAt(i)) {
                tok = TryParseYDWEMacro();
                if (null != tok)
                {
                    tokens.add(tok);
                    continue;
                }
            }
            if (strChar.contains(source.subSequence(i, i + 1)))
            {
                // попытка распознать строку
                tok = TryParseString();
                if (null != tok)
                {
                    tokens.add(tok);
                    continue;
                }
            }
            // int из 4 ascii символов
            if ('\'' == source.charAt(i))
            {
                tok = TryParse4AsciiInt();
                if (null != tok)
                {
                    tokens.add(tok);
                    continue;
                }
            }
            if ('0' <= source.charAt(i) && source.charAt(i) <= '9' || '.' == source.charAt(i) || '$' == source.charAt(i))
            {
                // попытка распарсить число
                tok = TryParseNumber();
                if (null != tok)
                {
                    tokens.add(tok);
                    continue;
                }
            }
            if (opers.contains(source.subSequence(i, i + 1)))
            {
                // попытка распарсить оператор
                tok = TryParseOperator();
                if (null != tok)
                {
                    tokens.add(tok);
                    continue;
                }
            }
            if (whiteChar.contains(source.subSequence(i, i + 1)))
            {
                // игнорим пробелы
                continue;
            }
            if (brac.contains(source.subSequence(i, i + 1)))
            {
                var typ = "";
                var s = source.substring(i, i + 1);
                switch (source.charAt(i))
                {
                    case '(': typ = TokenKind.lbra; break;
                    case ')': typ = TokenKind.rbra; break;
                    case '[': typ = TokenKind.lind; break;
                    case ']': typ = TokenKind.rind; break;
                }
                tok = new Token(pos, line, i, s, typ);
                tokens.add(tok);
                continue;
            }
            if (LineBreak(false))
            {
                // записываем конец строки
                tokens.add(new Token(pos, line, i, "\n", TokenKind.ln));
                continue;
            }
            tok = TryParseName();
            tokens.add(tok);
        }
        return tokens;
    }
}