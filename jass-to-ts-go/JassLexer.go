package JassToTs

import (
	TokenKind "TokenKind"
)

// номер строки в исходном коде
var line int;
// номер символа в строке в исходном коде
var pos int;
// текст исходного кода
var source int;
// глобальная позиция символа в исходном коде
var i int;

// алфавит операторов
const opers = "=+-*/!><,";
// алфавит белых символов
const whiteChar = " \t\r";
// алфавит строк
const strChar = "\"";//"\"'";
// алфавит скобок
const brac = "()[]";

var a = BTYP

// справочник ключевых слов
var keywords = map[string]string{
	// Базовые типы
	"integer", TokenKind.BTYP,
	"real",    TokenKind.BTYP,

	"boolean", TokenKind.BTYP,
	"string",  TokenKind.BTYP,
	"handle",  TokenKind.BTYP,
	"code",    TokenKind.BTYP,
	// 
	"nothing", TokenKind.BTYP,
	// 
	 "type",        TokenKind.KWD,
	 "extends",     TokenKind.KWD,
	 "globals",     TokenKind.KWD,
	 "endglobals",  TokenKind.KWD ,
	 "constant",    TokenKind.KWD ,
	 "native",      TokenKind.KWD ,
	 "takes",       TokenKind.KWD ,
	 "returns",     TokenKind.KWD ,
	 "function",    TokenKind.KWD,
	 "endfunction", TokenKind.KWD,
	//
	 "local", TokenKind.KWD ,
	 "array", TokenKind.KWD ,
	//
	 "set",      TokenKind.KWD,
	 "call",     TokenKind.KWD,
	 "if",       TokenKind.KWD,
	 "then",     TokenKind.KWD,
	 "endif",    TokenKind.KWD,
	 "else",     TokenKind.KWD,
	 "elseif",   TokenKind.KWD,
	 "loop",     TokenKind.KWD,
	 "endloop",  TokenKind.KWD,
	 "exitwhen", TokenKind.KWD,
	 "return",   TokenKind.KWD,
	 "debug",    TokenKind.KWD,
	//
	 "and", TokenKind.OPER ,
	 "or",  TokenKind.OPER ,
	//
	 "not", TokenKind.OPER ,
	//
	 "null",  TokenKind._NULL ,
	 "true",  TokenKind._BOOL ,
	 "false", TokenKind._BOOL ,
}