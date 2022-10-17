pub struct Token {
    //Type => TokenKind.GetType(Kind),
    Kind: String,
    Line: i8,
    Col: i8,
    Pos: i8,
    Text: String,
}

impl Token {
    pub fn new(kind: String, line: i8, col: i8, pos: i8, text: String) -> Token {
        Token {
            Kind: kind,
            Line: line,
            Col: col,
            Pos: pos,
            Text: text,
        }
    }
    //public override string ToString() => $"{Line},{Col} [{Type}|{Kind}]: {Text}";
    //public Token Clone() => new Token { Kind = Kind, Line = Line, Col = Col, Pos = Pos, Text = Text };
}