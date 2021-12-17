import JassException
from Token import Token, TokenKind

# /** алфавит операторов */
opers = "=+-*/!><,"
# /** алфавит белых символов */
whiteChar = " \t\r"
# /** алфавит строк */
strChar = "\""  # "\"'"
# /** алфавит скобок */
brac = "()[]"

# /** справочник ключевых слов */
keywords = {
    # Базовые типы
    "integer": TokenKind.btyp,
    "real": TokenKind.btyp,
    "boolean": TokenKind.btyp,
    "String": TokenKind.btyp,
    "handle": TokenKind.btyp,
    "code": TokenKind.btyp,
    #
    "nothing": TokenKind.btyp,
    #
    "type": TokenKind.kwd,
    "extends": TokenKind.kwd,
    "globals": TokenKind.kwd,
    "endglobals": TokenKind.kwd,
    "constant": TokenKind.kwd,
    "native": TokenKind.kwd,
    "takes": TokenKind.kwd,
    "returns": TokenKind.kwd,
    "function": TokenKind.kwd,
    "endfunction": TokenKind.kwd,
    #
    "local": TokenKind.kwd,
    "array": TokenKind.kwd,
    #
    "set": TokenKind.kwd,
    "call": TokenKind.kwd,
    "if": TokenKind.kwd,
    "then": TokenKind.kwd,
    "endif": TokenKind.kwd,
    "else": TokenKind.kwd,
    "elseif": TokenKind.kwd,
    "loop": TokenKind.kwd,
    "endloop": TokenKind.kwd,
    "exitwhen": TokenKind.kwd,
    "return": TokenKind.kwd,
    "debug": TokenKind.kwd,
    #
    "and": TokenKind.oper,
    "or": TokenKind.oper,
    #
    "not": TokenKind.oper,
    #
    "null": TokenKind.null,
    "true": TokenKind.bool,
    "false": TokenKind.bool
}

operators = ["=", ",", "+", "-", "*", "/", ">", "<", "==", "!=", ">=", "<="]


class JassLexer:
    def __init__(self, isYdweCompatible=False):
        self.isYdweCompatible = isYdweCompatible

    def LineBreak(self, set) -> bool:
        if '\n' != self.source[self.i]:
            return False
        if set:
            return True
        self.line += 1
        self.pos = 0
        return True

    # Попытаться распарсить комментарий
    # null если не удалось распарсить комментарий
    def TryParseComment(self) -> Token:
        if self.i == len(self.source) - 1: return None
        if '/' != self.source[self.i + 1]: return None
        j = self.i
        l = self.line
        p = self.pos
        s = self.source[self.i: self.i + 2]
        self.i += 2
        while self.i < len(self.source):
            if '\r' == self.source[self.i]: return
            if self.LineBreak(True): break
            s += self.source[self.i]

            self.i += 1
            self.pos += 1

        if self.i < len(self.source): self.i -= 1
        return Token(p, l, j, s, TokenKind.lcom)

    #
    # Попытаться распарсить YDWE макрос
    # @return None если не удалось распарсить макрос
    #
    def TryParseYDWEMacro(self) -> Token:
        if self.i == len(self.source) - 1: return None
        j = self.i
        l = self.line
        p = self.pos
        s = "##"
        self.i += 1
        while self.i < len(self.source):
            if '\r' == self.source[self.i]: return
            if self.LineBreak(True): break
            s += self.source[self.i]

            self.i += 1
            self.pos += 1

        if self.i < len(self.source): self.i -= 1
        return Token(p, l, j, s, TokenKind.ymacr)

    #
    # Попытаться распарсить число
    # @return  None если не удалось распарсить число
    #
    def TryParseNumber(self) -> Token:
        s = ""
        j = self.i
        l = self.line
        p = self.pos
        flags = {
            "isOct": '0' == self.source[self.i],  # octal    0[0-7]*
            "isHex": '$' == self.source[self.i],  # hex      $[0-9a-fA-F]+
            "isXFound": False,                    #          0[xX][0-9a-fA-F]+
            "isDotFound": False,                  # real     [0-9]+\.[0-9]*|\.[0-9]+
            "isNumFound": False
        }
        
        while self.i < len(self.source):
            def iteration(flags):
                # условия при которых продолжаем
                if '0' <= self.source[self.i] and self.source[self.i] <= '9':
                    if flags["isOct"] and '8' <= self.source[self.i]: JassException.Error(l, p, "wrong number: wrong octal number")
                    flags["isNumFound"] = True
                    return True
                # hex в формате 0[xX][0-9a-fA-F]+
                if flags["isOct"] and 1 == len(s) and ('x' == self.source[self.i] or 'X' == self.source[self.i]):
                    flags["isOct"] = False
                    flags["isHex"] = True
                    flags["isXFound"] = True
                    return True
                if flags["isHex"] and (('A' <= self.source[self.i] and self.source[self.i] <= 'F') or
                                ('a' <= self.source[self.i] and self.source[self.i] <= 'f') or
                                (not flags["isNumFound"] and '$' == self.source[self.i])):
                    flags["isNumFound"] = True
                    return True
                if '.' == self.source[self.i]:
                    if flags["isDotFound"]: JassException.Error(l, p, "wrong number: multiple dot")
                    if flags["isHex"]: JassException.Error(l, p, "wrong number: dot inside hex")
                    if flags["isOct"] and len(s) > 1: JassException.Error(l, p, "wrong number: dot inside octadecimal")
                    flags["isOct"] = False
                    flags["isDotFound"] = True
                    return True

                # условия при которых завершаем
                if self.source[self.i] in opers: return False
                if self.source[self.i] in brac: return False
                if self.LineBreak(True): return False
                if self.source[self.i] in whiteChar: return False
                # наткнулись на символ не число, не оператор, не перевод строки, не белый символ
                if flags["isDotFound"] or not flags["isNumFound"]:
                    self.i = j
                    return None
                
                JassException.Error(l, p, "wrong number: not a number")
            b = iteration(flags)
            if None == b: 
                return None
            if not b:
                break
            
            s += self.source[self.i]
            self.i += 1
            self.pos += 1

        typ = TokenKind.ndec
        if flags["isOct"]: typ = TokenKind.oct
        if flags["isHex"]: 
            if flags["isXFound"]: typ = TokenKind.xhex 
            else: typ = TokenKind.dhex
        if flags["isDotFound"]: typ = TokenKind.real

        if self.i < len(self.source): self.i -= 1
        return Token(p, l, j, s, typ)

    # Попытаться распарсить оператор#
    def TryParseOperator(self) -> Token:
        # костыль
        if ',' == self.source[self.i]:
            return Token(self.pos, self.line, self.i, ",", TokenKind.oper)

        s = ""
        j = self.i
        l = self.line
        p = self.pos

        while self.i < len(self.source):
            if self.source[self.i] not in opers: break
            if 2 == len(s): break
        
            s += self.source[self.i]
            self.i += 1
            self.pos += 1
        
        if s not in operators:
            s = s[1]
            self.i -= 1

        if self.i < len(self.source): self.i -= 1
        return Token(p, l, j, s, TokenKind.oper)

    # Попытаться распарсить число из 4х ASCII символов#
    def TryParse4AsciiInt(self) -> Token:
        tok = self.TryParseString()
        if len(tok.Text) > 6: JassException.Error(tok.Line, tok.Col, "wrong number: more than 4 ascii symbols")
        for c in tok.Text:
            if c > '\u00ff':
                JassException.Error(tok.Line, tok.Col, "wrong number: non ascii symbol")
        # Надо проверить как оригинальный компилятор относится к 'a\'bc' последовательности
        tok.Kind = TokenKind.adec
        return tok

    # Попытаться распарсить строку#
    def TryParseString(self) -> Token:
        eoc = self.source[self.i]

        s = self.source[self.i]
        j = self.i
        l = self.line
        p = self.pos

        if self.i == len(self.source) - 1: JassException.Error(l, p, "unclosed String")

        self.i += 1
        while self.i < len(self.source):
            s += self.source[self.i]
            if self.source[self.i] == eoc and self.source[self.i - 1] != '\\': break
            if self.LineBreak(True):
                pass
                # @warning todo: надо проверить как реагирует обычный jass#
            
            self.i += 1
            self.pos += 1

        #if i < len(self.source)) i--
        #return Token { Col = p, Line = l, Pos = j, Text = s, Type = eoc == '"' ? TokenType.dstr : TokenType.sstr }
        return Token(p, l, j, s, TokenKind.dstr)

    # Попытаться распарсить имя#
    def TryParseName(self) -> Token:
        s = ""
        j = self.i
        l = self.line
        p = self.pos

        while self.i < len(self.source):
            def iteration():
                # допустимые символы
                if 'a' <= self.source[self.i] and self.source[self.i] <= 'z': return True
                if 'A' <= self.source[self.i] and self.source[self.i] <= 'Z': return True
                if len(s) > 0 and '0' <= self.source[self.i] and self.source[self.i] <= '9': return True
                if len(s) > 0 and '_' == self.source[self.i]: return True
                # не допустимые символы
                if self.source[self.i] in whiteChar: return False
                if self.LineBreak(True): return False
                if self.source[self.i] in brac: return False
                if self.source[self.i] in opers: return False
                if self.source[self.i] in strChar: return False # в некоторых случаях может быть норм
                # левые символы
                JassException.Error(l, p, "wrong identifier: unknown symbol")
            
            if not iteration():
                break
            
            s += self.source[self.i]
            self.i += 1
            self.pos += 1

        typ = TokenKind._name
        if s in keywords: typ = keywords[s]
        if '_' == s[-1:]: JassException.Error(l, p, "wrong identifier: ends with \"_\"")

        if self.i < len(self.source): self.i -= 1
        return Token(p, l, j, s, typ)

    # распарсить исходный код на токены
    # @param source исходный код на языке jass
    # @return  список из токенов
    def Tokenize(self, source) -> list[Token]:
        self.source = source
        self.i = 0
        self.line = 0
        self.pos = 0
        tokens = []

        while self.i < len(source):
            def iteration():
                if '/' == self.source[self.i]:
                    tok = self.TryParseComment()
                    if None != tok:
                        tokens.append(tok)
                        return

                if self.isYdweCompatible and '#' == self.source[self.i]:
                    tok = self.TryParseYDWEMacro()
                    if None != tok:
                        tokens.append(tok)
                        return

                if self.source[self.i] in strChar:
                    # попытка распознать строку
                    tok = self.TryParseString()
                    if None != tok:
                        tokens.append(tok)
                        return

                # int из 4 ascii символов
                if '\'' == self.source[self.i]:
                    tok = self.TryParse4AsciiInt()
                    if None != tok:
                        tokens.append(tok)
                        return

                if '0' <= self.source[self.i] and self.source[self.i] <= '9' or '.' == self.source[self.i] or '$' == self.source[self.i]:
                    # попытка распарсить число
                    tok = self.TryParseNumber()
                    if None != tok:
                        tokens.append(tok)
                        return

                if self.source[self.i] in opers:
                    # попытка распарсить оператор
                    tok = self.TryParseOperator()
                    if None != tok:
                        tokens.append(tok)
                        return

                if self.source[self.i] in whiteChar:
                    # игнорим пробелы
                    return

                if self.source[self.i] in brac:
                    typ = ""
                    s = self.source[self.i:self.i + 1]
                    match self.source[self.i]:
                        case '(':
                            typ = TokenKind.lbra
                        case ')':
                            typ = TokenKind.rbra
                        case '[':
                            typ = TokenKind.lind
                        case ']':
                            typ = TokenKind.rind

                    tok = Token(self.pos, self.line, self.i, s, typ)
                    tokens.append(tok)
                    return

                if self.LineBreak(False):
                    # записываем конец строки
                    tokens.append(Token(self.pos, self.line, self.i, "\n", TokenKind.ln))
                    return

                tok = self.TryParseName()
                tokens.append(tok)

            iteration()
            self.i += 1
            self.pos += 1
        
        return tokens
