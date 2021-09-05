package org.vsx.jass;

/** Токен */
public class Token {
    public String getType() {
        return TokenKind.GetType(Kind);
    }
    public String Kind = "";
    public int Line = 0;
    public int Col = 0;
    public int Pos = 0;
    public String Text = "";

    public Token() {}
    public Token(int col, int line, int pos, String text, String kind) {
        Col = col;
        Line = line;
        Pos = pos;
        Text = text;
        Kind = kind;
    } 
    
    @Override
    public String toString() {
        return String.format("%s,%s [%s|%s]: %s", Line, Col, getType(), Kind, Text);
    }

    public Token Clone() {
        return new Token(Col, Line, Pos, Text, Kind);
    }
}
