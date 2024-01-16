package jass

import (
	"fmt"
)

/** Токен */
type Token struct {
	Kind string
	Line int
	Col  int
	Pos  int
	Text string
}

func (token *Token) GetType() string {
	return GetTokenTypeOfKind(token.Kind)
}

func NewToken() *Token {
	return &Token{
		"",
		0,
		0,
		0,
		"",
	}
}

func (token *Token) String() string {
	return fmt.Sprintf("%d,%d [%s|%s]: %s", token.Line, token.Col, token.GetType(), token.Kind, token.Text)
}

func (token *Token) Clone() *Token {
	return &Token{
		token.Kind,
		token.Line,
		token.Col,
		token.Pos,
		token.Text,
	}
}
