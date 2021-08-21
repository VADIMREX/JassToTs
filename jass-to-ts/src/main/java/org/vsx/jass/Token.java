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
    //public override String ToString() => $"{Line},{Col} [{Type}|{Kind}]: {Text}";
    @Override
    public String toString() {
        return String.format("%s,%s [%s|%s]: %s", Line, Col, getType(), Kind, Text);
    }
}
