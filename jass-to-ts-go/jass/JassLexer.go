package jass

import (
	"strings"
)

const (
	// алфавит операторов
	opers = "=+-*/!><,"

	// алфавит белых символов
	whiteChar = " \t\r"

	// алфавит строк
	strChar = "\"" //"\"'";
	// алфавит скобок
	brac = "()[]"
)

var keywords = map[string]string{
	// Базовые типы
	"integer": btyp,
	"real":    btyp,

	"boolean": btyp,
	"string":  btyp,
	"handle":  btyp,
	"code":    btyp,
	//
	"nothing": btyp,
	//
	"type":        kwd,
	"extends":     kwd,
	"globals":     kwd,
	"endglobals":  kwd,
	"constant":    kwd,
	"native":      kwd,
	"takes":       kwd,
	"returns":     kwd,
	"function":    kwd,
	"endfunction": kwd,
	//
	"local": kwd,
	"array": kwd,
	//
	"set":      kwd,
	"call":     kwd,
	"if":       kwd,
	"then":     kwd,
	"endif":    kwd,
	"else":     kwd,
	"elseif":   kwd,
	"loop":     kwd,
	"endloop":  kwd,
	"exitwhen": kwd,
	"return":   kwd,
	"debug":    kwd,
	//
	"and": oper,
	"or":  oper,
	//
	"not": oper,
	//
	"null":  null,
	"true":  _bool,
	"false": _bool,
}

var operators = []string{"=", ",", "+", "-", "*", "/", ">", "<", "==", "!=", ">=", "<="}

type JassLexer struct {
	// номер строки в исходном коде
	line int

	// номер символа в строке в исходном коде
	pos int

	// текст исходного кода
	source string

	// глобальная позиция символа в исходном коде
	i int

	/** режим совместимости с YDWE */
	isYdweCompatible bool
}

/*
создать Лексер
@param isYdweCompatible режим совместимости с YDWE
*/
func NewJassLexer(isYdweCompatible bool) *JassLexer {
	return &JassLexer{isYdweCompatible: isYdweCompatible}
}

/*
Проверить на перевод строки.
Инкрементирует переменную jl.line и
обнуляет переменную jl.pos,
если обнаружен перевод строки.

@return true если обнаружен перевод строки
*/
func (jl *JassLexer) LineBreak(set bool) bool { // @todo make private
	if jl.source[jl.i] != '\n' {
		return false
	}
	if set {
		return true
	}
	jl.line++
	jl.pos = 0
	return true
}

// Попытаться распарсить комментарий
// / <returns>  nil если не удалось распарсить комментарий </returns>
func (jl *JassLexer) TryParseComment() *Token { // @todo private
	if jl.i == len(jl.source)-1 {
		return nil
	}
	if '/' != jl.source[jl.i+1] {
		return nil
	}
	var j, l, p = jl.i, jl.line, jl.pos
	var s = jl.source[jl.i : jl.i+2]
	jl.i += 2
	for jl.i < len(jl.source) {
		if '\r' == jl.source[jl.i] {
			continue
		}
		if jl.LineBreak(true) {
			break
		}
		s += jl.source[jl.i:jl.i]

		jl.i++
		jl.pos++
	}

	if jl.i < len(jl.source) {
		jl.i--
	}
	return &Token{Col: p, Line: l, Pos: j, Text: s, Kind: lcom}
}

// Попытаться распарсить YDWE макрос
// / <returns>  nil если не удалось распарсить макрос </returns>
func (jl *JassLexer) TryParseYDWEMacro() *Token { // @todo private
	if jl.i == len(jl.source)-1 {
		return nil
	}
	var j, l, p = jl.i, jl.line, jl.pos
	//#warning Убрать // после добавления интерпретатора макросов
	var s = "//#"
	jl.i++
	for jl.i < len(jl.source) {
		if '\r' == jl.source[jl.i] {
			continue
		}
		if jl.LineBreak(true) {
			break
		}
		s += jl.source[jl.i:jl.i]

		jl.i++
		jl.pos++
	}

	if jl.i < len(jl.source) {
		jl.i--
	}
	return &Token{Col: p, Line: l, Pos: j, Text: s, Kind: ymacr}
}

// Попытаться распарсить число
// / <returns>  nil если не удалось распарсить число </returns>
func (jl *JassLexer) TryParseNumber() (*Token, error) { // @todo private
	var s = ""
	var j, l, p = jl.i, jl.line, jl.pos
	var isOct = '0' == jl.source[jl.i] // octal    0[0-7]*
	var isHex = '$' == jl.source[jl.i] // hex      $[0-9a-fA-F]+
	var isXFound = false               //          0[xX][0-9a-fA-F]+
	var isDotFound = false             // real     [0-9]+\.[0-9]*|\.[0-9]+
	var isNumFound = false
	for jl.i < len(jl.source) {
		// условия при которых продолжаем
		if '0' <= jl.source[jl.i] && jl.source[jl.i] <= '9' {
			if isOct && '8' <= jl.source[jl.i] {
				err := NewJassError(l, p, "wrong number: wrong octal number")
				if err != nil {
					return nil, err
				}
			}
			isNumFound = true
			continue
		}
		// hex в формате 0[xX][0-9a-fA-F]+
		if isOct && 1 == len(s) && ('x' == jl.source[jl.i] || 'X' == jl.source[jl.i]) {
			isOct = false
			isHex = true
			isXFound = true
			continue
		}
		if isHex && (('A' <= jl.source[jl.i] && jl.source[jl.i] <= 'F') ||
			('a' <= jl.source[jl.i] && jl.source[jl.i] <= 'f') ||
			(!isNumFound && '$' == jl.source[jl.i])) {
			isNumFound = true
			continue
		}
		if '.' == jl.source[jl.i] {
			if isDotFound {
				err := NewJassError(l, p, "wrong number: multiple dot")
				if err != nil {
					return nil, err
				}
			}
			if isHex {
				err := NewJassError(l, p, "wrong number: dot inside hex")
				if err != nil {
					return nil, err
				}
			}
			if isOct && len(s) > 1 {
				err := NewJassError(l, p, "wrong number: dot inside octadecimal")
				if err != nil {
					return nil, err
				}
			}
			isOct = false
			isDotFound = true
			continue
		}
		// условия при которых завершаем
		if strings.Contains(opers, jl.source[jl.i:jl.i]) {
			break
		}
		if strings.Contains(brac, jl.source[jl.i:jl.i]) {
			break
		}
		if jl.LineBreak(true) {
			break
		}
		if strings.Contains(whiteChar, jl.source[jl.i:jl.i]) {
			break
		}
		// наткнулись на символ не число, не оператор, не перевод строки, не белый символ
		if isDotFound || !isNumFound {
			jl.i = j
			return nil, nil
		}
		err := NewJassError(l, p, "wrong number: not a number")
		if err != nil {
			return nil, err
		}
	}
	var typ = ndec
	if isOct {
		typ = oct
	}
	if isHex {
		if isXFound {
			typ = xhex
		} else {
			typ = dhex
		}
	}
	if isDotFound {
		typ = real
	}

	if jl.i < len(jl.source) {
		jl.i--
	}
	return &Token{Col: p, Line: l, Pos: j, Text: s, Kind: typ}, nil
}

// Попытаться распарсить оператор
func (jl *JassLexer) TryParseOperator() *Token { // @todo private
	// костыль
	if ',' == jl.source[jl.i] {
		return &Token{Col: jl.pos, Line: jl.line, Pos: jl.i, Text: ",", Kind: oper}
	}

	var s = ""
	var j, l, p = jl.i, jl.line, jl.pos

	for jl.i < len(jl.source) {
		if !strings.Contains(opers, jl.source[jl.i:jl.i]) {
			break
		}
		if 2 == len(s) {
			break
		}
		s += jl.source[jl.i:jl.i]
		jl.i++
		jl.pos++
	}

	var isContains = false
	for _, op := range operators {
		if op != s {
			continue
		}
		isContains = true
		break
	}
	if !isContains {
		s = s[0:1]
		jl.i--
	}

	if jl.i < len(jl.source) {
		jl.i--
	}
	return &Token{Col: p, Line: l, Pos: j, Text: s, Kind: oper}
}

// Попытаться распарсить число из 4х ASCII символов
func (jl *JassLexer) TryParse4AsciiInt() (*Token, error) { // @todo private
	tok, err := jl.TryParseString()
	if err != nil {
		return tok, err
	}
	if len(tok.Text) > 6 {
		err := NewJassError(tok.Line, tok.Col, "wrong number: more than 4 ascii symbols")
		if err != nil {
			return nil, err
		}
	}
	for _, c := range tok.Text {
		if c > '\u00ff' {
			err := NewJassError(tok.Line, tok.Col, "wrong number: non ascii symbol")
			if err != nil {
				return nil, err
			}
		}
	}
	// Надо проверить как оригинальный компилятор относится к 'a\'bc' последовательности
	tok.Kind = adec
	return tok, nil
}

// Попытаться распарсить строку
func (jl *JassLexer) TryParseString() (*Token, error) { // @todo private
	var eoc = jl.source[jl.i]

	var s = jl.source[jl.i:jl.i]
	var j, l, p = jl.i, jl.line, jl.pos

	if jl.i == len(jl.source)-1 {
		err := NewJassError(l, p, "unclosed string")
		if err != nil {
			return nil, err
		}
	}

	jl.i++
	for jl.i < len(jl.source) {
		s += jl.source[jl.i:jl.i]
		if jl.source[jl.i] == eoc && jl.source[jl.i-1] != '\\' {
			break
		}
		if jl.LineBreak(true) {
		}
		//#warning todo: надо проверить как реагирует обычный jass
		jl.i++
		jl.pos++
	}

	//if jl.i < len(jl.source)) jl.i--;
	//return &Token { Col: p, Line: l, Pos: j, Text: s, Type = eoc == '"' ? TokenType.dstr : TokenType.sstr };
	return &Token{Col: p, Line: l, Pos: j, Text: s, Kind: dstr}, nil
}

// Попытаться распарсить имя
func (jl *JassLexer) TryParseName() (*Token, error) { // @todo private
	var s = ""
	var j, l, p = jl.i, jl.line, jl.pos

	for jl.i < len(jl.source) {
		// допустимые символы
		if 'a' <= jl.source[jl.i] && jl.source[jl.i] <= 'z' {
			continue
		}
		if 'A' <= jl.source[jl.i] && jl.source[jl.i] <= 'Z' {
			continue
		}
		if len(s) > 0 && '0' <= jl.source[jl.i] && jl.source[jl.i] <= '9' {
			continue
		}
		if len(s) > 0 && '_' == jl.source[jl.i] {
			continue
		}
		// не допустимые символы
		if strings.Contains(whiteChar, jl.source[jl.i:jl.i]) {
			break
		}
		if jl.LineBreak(true) {
			break
		}
		if strings.Contains(brac, jl.source[jl.i:jl.i]) {
			break
		}
		if strings.Contains(opers, jl.source[jl.i:jl.i]) {
			break
		}
		if strings.Contains(strChar, jl.source[jl.i:jl.i]) {
			break
		} // в некоторых случаях может быть норм
		// левые символы
		err := NewJassError(l, p, "wrong identifier: unknown symbol")
		if err != nil {
			return nil, err
		}

		s += jl.source[jl.i:jl.i]
		jl.i++
		jl.pos++
	}
	var typ = name
	var _, ok = keywords[s]
	if ok {
		typ = keywords[s]
	}
	if '_' == s[len(s)-1] {
		err := NewJassError(l, p, "wrong identifier: ends with \"_\"")
		if err != nil {
			return nil, err
		}
	}

	if jl.i < len(jl.source) {
		jl.i--
	}
	return &Token{Col: p, Line: l, Pos: j, Text: s, Kind: typ}, nil
}

// распарсить исходный код на токены
// / <param name="source"> исходный код на языке jass</param>
// / <returns> список из токенов </returns>
func (jl *JassLexer) Tokenize(source string) ([]*Token, error) {
	jl.source = source
	jl.i = 0
	jl.line = 0
	jl.pos = 0
	tokens := []*Token{}

	for jl.i < len(jl.source) {
		if '/' == jl.source[jl.i] {
			// попытка распознать комментарий
			tok := jl.TryParseComment()
			if nil != tok {
				tokens = append(tokens, tok)
				continue
			}
		}
		if jl.isYdweCompatible && '#' == jl.source[jl.i] {
			tok := jl.TryParseYDWEMacro()
			if nil != tok {
				tokens = append(tokens, tok)
				continue
			}
		}
		if strings.Contains(strChar, source[jl.i:jl.i]) {
			// попытка распознать строку
			tok, err := jl.TryParseString()
			if err != nil {
				return tokens, err
			}
			if nil != tok {
				tokens = append(tokens, tok)
				continue
			}
		}
		// int из 4 ascii символов
		if '\'' == jl.source[jl.i] {
			tok, err := jl.TryParse4AsciiInt()
			if err != nil {
				return tokens, err
			}
			if nil != tok {
				tokens = append(tokens, tok)
				continue
			}
		}
		if '0' <= jl.source[jl.i] && jl.source[jl.i] <= '9' || '.' == jl.source[jl.i] || '$' == jl.source[jl.i] {
			// попытка распарсить число
			tok, err := jl.TryParseNumber()
			if err != nil {
				return tokens, err
			}
			if nil != tok {
				tokens = append(tokens, tok)
				continue
			}
		}
		if strings.Contains(opers, jl.source[jl.i:jl.i]) {
			// попытка распарсить оператор
			tok := jl.TryParseOperator()
			if nil != tok {
				tokens = append(tokens, tok)
				continue
			}
		}
		if strings.Contains(whiteChar, source[jl.i:jl.i]) {
			// игнорим пробелы
			continue
		}
		if strings.Contains(brac, source[jl.i:jl.i]) {
			var typ = ""
			var s = jl.source[jl.i:jl.i]
			switch source[jl.i] {
			case '(':
				typ = lbra
			case ')':
				typ = rbra
			case '[':
				typ = lind
			case ']':
				typ = rind
			}
			tok := &Token{Col: jl.pos, Line: jl.line, Pos: jl.i, Text: s, Kind: typ}
			tokens = append(tokens, tok)
			continue
		}
		if jl.LineBreak(false) {
			// записываем конец строки
			tokens = append(tokens, &Token{Col: jl.pos, Line: jl.line, Pos: jl.i, Text: "\n", Kind: ln})
			continue
		}
		tok, err := jl.TryParseName()
		if err != nil {
			return tokens, err
		}
		tokens = append(tokens, tok)

		jl.i++
		jl.pos++
	}
	return tokens, nil
}
