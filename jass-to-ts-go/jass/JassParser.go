package jass

type JassParser struct {
}

func NewJassParser(isYdweCompatible bool) *JassParser {
	return &JassParser{}
}

func (p *JassParser) Parse(tokens []*Token) *Statement {
	return nil
}
