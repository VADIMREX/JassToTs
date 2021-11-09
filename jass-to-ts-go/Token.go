package JassToTs

import "fmt"

/** Токен */
type Token struct {
	Kind string
	Line int
	Col  int
	Pos  int
	Text string
}

func (this *Token) GetType() string {
	return GetTypeOfKind(this.Kind)
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

func (this *Token) String() string {
    return fmt.Sprintf("%d,%d [%s|%s]: %s", this.Line, this.Col, this.GetType(), this.Kind, this.Text)
}

func (this *Token) Clone() *Token {
	return &Token{
		this.Kind,
		this.Line,
		this.Col,
		this.Pos,
		this.Text,
	}
}