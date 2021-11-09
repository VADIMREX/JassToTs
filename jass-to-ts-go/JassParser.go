package JassToTs

type JassParser struct {
}

func NewJassParser(isYdweCompatible bool) *JassParser {
	return &JassParser{}
}

func (this *JassParser) Parse(tokens []*Token) *Statement {
	return nil
}