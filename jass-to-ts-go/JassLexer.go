package JassToTs

import "strings"

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
	Инкрементирует переменную this.line и
	обнуляет переменную this.pos,
	если обнаружен перевод строки.

	@return true если обнаружен перевод строки
*/
func (this *JassLexer) LineBreak(set bool) bool { // @todo make private
	if '\n' != this.source[this.i] {
		return false
	}
	if set {
		return true
	}
	this.line++
	this.pos = 0
	return true
}

// Попытаться распарсить комментарий
/// <returns>  nil если не удалось распарсить комментарий </returns>
func (this *JassLexer) TryParseComment() *Token { // @todo private
	if this.i == len(this.source)-1 {
		return nil
	}
	if '/' != this.source[this.i+1] {
		return nil
	}
	var j, l, p = this.i, this.line, this.pos
	var s = this.source[this.i : this.i+2]
	this.i += 2
	for this.i < len(this.source) {
		if '\r' == this.source[this.i] {
			continue
		}
		if this.LineBreak(true) {
			break
		}
		s += this.source[this.i:this.i]

		this.i++
		this.pos++
	}

	if this.i < len(this.source) {
		this.i--
	}
	return &Token{Col: p, Line: l, Pos: j, Text: s, Kind: lcom}
}

// Попытаться распарсить YDWE макрос
/// <returns>  nil если не удалось распарсить макрос </returns>
func (this *JassLexer) TryParseYDWEMacro() *Token { // @todo private
	if this.i == len(this.source)-1 {
		return nil
	}
	var j, l, p = this.i, this.line, this.pos
	//#warning Убрать // после добавления интерпретатора макросов
	var s = "//#"
	this.i++
	for this.i < len(this.source) {
		if '\r' == this.source[this.i] {
			continue
		}
		if this.LineBreak(true) {
			break
		}
		s += this.source[this.i:this.i]

		this.i++
		this.pos++
	}

	if this.i < len(this.source) {
		this.i--
	}
	return &Token{Col: p, Line: l, Pos: j, Text: s, Kind: ymacr}
}

// Попытаться распарсить число
/// <returns>  nil если не удалось распарсить число </returns>
func (this *JassLexer) TryParseNumber() *Token { // @todo private
	var s = ""
	var j, l, p = this.i, this.line, this.pos
	var isOct = '0' == this.source[this.i] // octal    0[0-7]*
	var isHex = '$' == this.source[this.i] // hex      $[0-9a-fA-F]+
	var isXFound = false                   //          0[xX][0-9a-fA-F]+
	var isDotFound = false                 // real     [0-9]+\.[0-9]*|\.[0-9]+
	var isNumFound = false
	for this.i < len(this.source) {
		// условия при которых продолжаем
		if '0' <= this.source[this.i] && this.source[this.i] <= '9' {
			if isOct && '8' <= this.source[this.i] {
				JassError(l, p, "wrong number: wrong octal number")
			}
			isNumFound = true
			continue
		}
		// hex в формате 0[xX][0-9a-fA-F]+
		if isOct && 1 == len(s) && ('x' == this.source[this.i] || 'X' == this.source[this.i]) {
			isOct = false
			isHex = true
			isXFound = true
			continue
		}
		if isHex && (('A' <= this.source[this.i] && this.source[this.i] <= 'F') ||
			('a' <= this.source[this.i] && this.source[this.i] <= 'f') ||
			(!isNumFound && '$' == this.source[this.i])) {
			isNumFound = true
			continue
		}
		if '.' == this.source[this.i] {
			if isDotFound {
				JassError(l, p, "wrong number: multiple dot")
			}
			if isHex {
				JassError(l, p, "wrong number: dot inside hex")
			}
			if isOct && len(s) > 1 {
				JassError(l, p, "wrong number: dot inside octadecimal")
			}
			isOct = false
			isDotFound = true
			continue
		}
		// условия при которых завершаем
		if strings.Contains(opers, this.source[this.i:this.i]) {
			break
		}
		if strings.Contains(brac, this.source[this.i:this.i]) {
			break
		}
		if this.LineBreak(true) {
			break
		}
		if strings.Contains(whiteChar, this.source[this.i:this.i]) {
			break
		}
		// наткнулись на символ не число, не оператор, не перевод строки, не белый символ
		if isDotFound || !isNumFound {
			this.i = j
			return nil
		}
		JassError(l, p, "wrong number: not a number")
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

	if this.i < len(this.source) {
		this.i--
	}
	return &Token{Col: p, Line: l, Pos: j, Text: s, Kind: typ}
}

// Попытаться распарсить оператор
func (this *JassLexer) TryParseOperator() *Token { // @todo private
	// костыль
	if ',' == this.source[this.i] {
		return &Token{Col: this.pos, Line: this.line, Pos: this.i, Text: ",", Kind: oper}
	}

	var s = ""
	var j, l, p = this.i, this.line, this.pos

	for this.i < len(this.source) {
		if !strings.Contains(opers, this.source[this.i:this.i]) {
			break
		}
		if 2 == len(s) {
			break
		}
		s += this.source[this.i:this.i]
		this.i++
		this.pos++
	}

	var isContains = false
	for _, op := range operators {
		if op != s { continue }
		isContains = true
		break
	}
	if !isContains {
		s = s[0:1]
		this.i--
	}

	if this.i < len(this.source) {
		this.i--
	}
	return &Token{Col: p, Line: l, Pos: j, Text: s, Kind: oper}
}

// Попытаться распарсить число из 4х ASCII символов
func (this *JassLexer) TryParse4AsciiInt() *Token { // @todo private
	var tok = this.TryParseString()
	if len(tok.Text) > 6 {
		JassError(tok.Line, tok.Col, "wrong number: more than 4 ascii symbols")
	}
	for _, c := range tok.Text {
		if c > '\u00ff' {
			JassError(tok.Line, tok.Col, "wrong number: non ascii symbol")
		}
	}
	// Надо проверить как оригинальный компилятор относится к 'a\'bc' последовательности
	tok.Kind = adec
	return tok
}

// Попытаться распарсить строку
func (this *JassLexer) TryParseString() *Token { // @todo private
	var eoc = this.source[this.i]

	var s = this.source[this.i:this.i]
	var j, l, p = this.i, this.line, this.pos

	if this.i == len(this.source)-1 {
		JassError(l, p, "unclosed string")
	}

	this.i++
	for this.i < len(this.source) {
		s += this.source[this.i:this.i]
		if this.source[this.i] == eoc && this.source[this.i-1] != '\\' {
			break
		}
		if this.LineBreak(true) {
		}
		//#warning todo: надо проверить как реагирует обычный jass
		this.i++
		this.pos++
	}

	//if this.i < len(this.source)) this.i--;
	//return &Token { Col: p, Line: l, Pos: j, Text: s, Type = eoc == '"' ? TokenType.dstr : TokenType.sstr };
	return &Token{Col: p, Line: l, Pos: j, Text: s, Kind: dstr}
}

// Попытаться распарсить имя
func (this *JassLexer) TryParseName() *Token { // @todo private
	var s = ""
	var j, l, p = this.i, this.line, this.pos

	for this.i < len(this.source) {
		// допустимые символы
		if 'a' <= this.source[this.i] && this.source[this.i] <= 'z' {
			continue
		}
		if 'A' <= this.source[this.i] && this.source[this.i] <= 'Z' {
			continue
		}
		if len(s) > 0 && '0' <= this.source[this.i] && this.source[this.i] <= '9' {
			continue
		}
		if len(s) > 0 && '_' == this.source[this.i] {
			continue
		}
		// не допустимые символы
		if strings.Contains(whiteChar, this.source[this.i:this.i]) {
			break
		}
		if this.LineBreak(true) {
			break
		}
		if strings.Contains(brac, this.source[this.i:this.i]) {
			break
		}
		if strings.Contains(opers, this.source[this.i:this.i]) {
			break
		}
		if strings.Contains(strChar, this.source[this.i:this.i]) {
			break
		} // в некоторых случаях может быть норм
		// левые символы
		JassError(l, p, "wrong identifier: unknown symbol")

		s += this.source[this.i:this.i]
		this.i++
		this.pos++
	}
	var typ = name
	var _, ok = keywords[s];
	if ok {
		typ = keywords[s]
	}
	if '_' == s[len(s)-1] {
		JassError(l, p, "wrong identifier: ends with \"_\"")
	}

	if this.i < len(this.source) {
		this.i--
	}
	return &Token{Col: p, Line: l, Pos: j, Text: s, Kind: typ}
}

// распарсить исходный код на токены
/// <param name="source"> исходный код на языке jass</param>
/// <returns> список из токенов </returns>
func (this *JassLexer) Tokenize(source string) []*Token {
	this.source = source
	this.i = 0
	this.line = 0
	this.pos = 0
	var tokens []*Token

	var tok * Token = nil
	for this.i < len(this.source) {
		if '/' == this.source[this.i] {
			// попытка распознать комментарий
			tok = this.TryParseComment()
			if nil != tok {
				tokens = append(tokens, tok)
				continue
			}
		}
		if this.isYdweCompatible && '#' == this.source[this.i] {
			tok = this.TryParseYDWEMacro()
			if nil != tok {
				tokens = append(tokens, tok)
				continue
			}
		}
		if strings.Contains(strChar, source[this.i:this.i]) {
			// попытка распознать строку
			tok = this.TryParseString()
			if nil != tok {
				tokens = append(tokens, tok)
				continue
			}
		}
		// int из 4 ascii символов
		if '\'' == this.source[this.i] {
			tok = this.TryParse4AsciiInt()
			if nil != tok {
				tokens = append(tokens, tok)
				continue
			}
		}
		if '0' <= this.source[this.i] && this.source[this.i] <= '9' || '.' == this.source[this.i] || '$' == this.source[this.i] {
			// попытка распарсить число
			tok = this.TryParseNumber()
			if nil != tok {
				tokens = append(tokens, tok)
				continue
			}
		}
		if strings.Contains(opers, this.source[this.i:this.i]) {
			// попытка распарсить оператор
			tok = this.TryParseOperator()
			if nil != tok {
				tokens = append(tokens, tok)
				continue
			}
		}
		if strings.Contains(whiteChar, source[this.i:this.i]) {
			// игнорим пробелы
			continue
		}
		if strings.Contains(brac, source[this.i:this.i]) {
			var typ = ""
			var s = this.source[this.i:this.i]
			switch source[this.i] {
			case '(':
				typ = lbra
				break
			case ')':
				typ = rbra
				break
			case '[':
				typ = lind
				break
			case ']':
				typ = rind
				break
			}
			tok = &Token{Col: this.pos, Line: this.line, Pos: this.i, Text: s, Kind: typ}
			tokens = append(tokens, tok)
			continue
		}
		if this.LineBreak(false) {
			// записываем конец строки
			tokens = append(tokens, &Token{Col: this.pos, Line: this.line, Pos: this.i, Text: "\n", Kind: ln})
			continue
		}
		tok = this.TryParseName()
		tokens = append(tokens, tok)

		this.i++
		this.pos++
	}
	return tokens
}
