package org.vsx.jass;

import java.util.LinkedList;

public class Statement
{
    public String Type;
    public Token Start;
    public Statement Parent;
    public LinkedList<Statement> Childs = new LinkedList<Statement>();

    public Statement() {

    }

    public Statement(Statement parent, String type, Token start) {
        this.Parent = parent;
        this.Type = type;
        this.Start = start;
    }

    /**
     * Создать и добавить дочернюю инструкцию
     * @param type  тип инструкции
     * @param token токен с которого начинается потомок
     * @return дочернюю инструкцию
     * @see Statement#AddChild
     */
    public Statement MakeChild(String type, Token token)
    {
        var child = new Statement(this, type, token);
        Childs.add(child);
        return child;
    }


    /**
     * Создать и добавить дочернюю инструкцию
     * @param type тип инструкции
     * @param token токен с которого начинается потомок
     * @return себя
     * @see Statement#MakeChild
     */
    public Statement AddChild(String type, Token token)
    {
        MakeChild(type, token);
        return this;
    }

    /**
     * Добавить дочернюю инструкцию
     * @param child дочерняя инструкция
     * @return себя
     */
    public Statement AddChild(Statement child)
    {
        child.Parent = this;
        Childs.add(child);
        return this;
    }

    @Override
    public String toString() { return toString(0); }
    public String toString(int ident) {
        String s = "";
        for (Statement statement : Childs) {
            s += statement.toString(ident + 2);
        }
        return String.format("%s%s %s\n%s", "", Type, Start, s);
        // $"// {"".PadLeft(ident, ' ')}{Type} {Start}\n" +
        // (Childs.Count > 0 ?
        //     Childs.Select(x => x.ToString(ident + 2)).Aggregate((x, y) => $"{x}{y}") :
        //     ""
        // );2
    }
}
