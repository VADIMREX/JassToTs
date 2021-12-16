package org.vsx.jass;

import java.util.List;
import java.util.Stack;
import java.util.function.Function;

public class JassParser {
    /** список токенов по которому идёт разбор */
    List<Token> tokens;
    /** глобальная позиция в списке токенов */
    int i;

    boolean isYdweCompatible;

    public JassParser() {
        this(false);
    }

    public JassParser(boolean isYdweCompatible) {
        this.isYdweCompatible = isYdweCompatible;
    }

    /** 
     * Добавить комментарий
     * @param parent Инструкция содержащая комментарий
     * @return true если был добавлен комментарий
     */
    boolean AddComment(Statement parent)
    {
        if (!TokenType.comm.equals(tokens.get(i).getType())) return false;
        parent.AddChild("Comm", tokens.get(i));
        return true;
    }

    /** Попытаться распарсить объявление типа */
    Statement TryParseTypeDecl() throws JassException
    {
        var stat = new Statement("TypeDecl");

        int j = 0;
        for (; i < tokens.size() && j < 5; i++)
        {
            if (AddComment(stat)) continue;
            if (j < 4 && TokenKind.ln.equals(tokens.get(i).Kind))
                JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong type declaration: linebreak");
            switch (j)
            {
                case 0:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"type".equals(tokens.get(i).Text)) return null;
                    stat.Start = tokens.get(i);
                    j++;
                    break;
                case 1:
                    if (!TokenKind.name.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong type declaration: identifier expected");
                    stat.AddChild("TypeName", tokens.get(i));
                    j++;
                    break;
                case 2:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"extends".equals(tokens.get(i).Text))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong type declaration: extends keyword expected");
                    j++;
                    break;
                case 3:
                    if (!TokenKind.name.equals(tokens.get(i).Kind) && TokenKind.btyp.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong type declaration: type identifier expected");
                    j++;
                    stat.AddChild("BaseType", tokens.get(i));
                    break;
                case 4:
                    if (!TokenKind.ln.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong type declaration: new line expected");
                    j++;
                    break;
            }
        }
        if (i < tokens.size()) i--;
        return stat;
    }

    /** Попытаться распарсить раздел с глобальными переменными */
    Statement TryParseGlobals() throws JassException
    {
        var stat = new Statement("Glob");

        int j = 0;
        for (; i < tokens.size() && j < 4; i++)
        {
            if (TokenKind.lcom.equals(tokens.get(i).Kind)) continue;
            switch (j)
            {
                case 0:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"globals".equals(tokens.get(i).Text)) return null;
                    stat.Start = tokens.get(i);
                    j++;
                    break;
                case 1:
                case 3:
                    if (!TokenKind.ln.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong globals declaration: new line expected");
                    j++;
                    break;
                case 2:
                    if (TokenKind.ln.equals(tokens.get(i).Kind)) continue;
                    if (TokenKind.kwd.equals(tokens.get(i).Kind) && "endglobals".equals(tokens.get(i).Text))
                    {
                        i++;
                        j++;
                        continue;
                    }
                    var vardecl = TryParseGlobalVarDecl();
                    stat.AddChild(vardecl);
                    break;
            }
        }

        if (i < tokens.size()) i--;
        return stat;
    }

    /** Попытаться распарсить объявление глобальной переменной или константы */
    Statement TryParseGlobalVarDecl() throws JassException
    {
        Statement stat = TokenKind.kwd.equals(tokens.get(i).Kind) && "constant".equals(tokens.get(i).Text) ?
            TryParseGlobalConst() :
            TryParseVarDecl();
        stat.Type = String.format("G%s", stat.Type);
        return stat;
    }

    /** Попытаться распарсить глобальную константу */
    Statement TryParseGlobalConst() throws JassException
    {
        // 'constant' type id '=' expr newline
        var stat = new Statement("Const");

        int j = 0;
        for (; i < tokens.size() && j < 5; i++)
        {
            if (AddComment(stat)) continue;
            switch (j)
            {
                case 0:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"constant".equals(tokens.get(i).Text)) return null;
                    stat.Start = tokens.get(i);
                    j++;
                    break;
                case 1:
                    if (!TokenKind.name.equals(tokens.get(i).Kind) && !TokenKind.btyp.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong const declaration: type identifier expected");
                    stat.AddChild("Type", tokens.get(i));
                    j++;
                    break;
                case 2:
                    if (!TokenKind.name.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong const declaration: identifier expected");
                    stat.AddChild("Name", tokens.get(i));
                    j++;
                    break;
                case 3:
                    if (!TokenKind.oper.equals(tokens.get(i).Kind) || !"=".equals(tokens.get(i).Text))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong const declaration: initialization expected");
                    i++;
                    var expr = TryParseExpression();
                    stat.AddChild(expr);
                    j++;
                    break;
                case 4:
                    if (!TokenKind.ln.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong const declaration: new line expected");
                    j++;
                    break;
            }
        }

        if (i < tokens.size()) i--;
        return stat;
    }

    /** Попытаться распарсить объявление переменной */
    Statement TryParseVarDecl() throws JassException
    {
        // type id ('=' expr)? | type 'array' id 
        var stat = new Statement();

        var IsLocal = false;
        var IsArray = false;

        int j = 0;
        for (; i < tokens.size() && j < 5; i++)
        {
            if (AddComment(stat)) continue;
            switch (j)
            {
                // local не обязательно
                case 0:
                    if (TokenKind.kwd.equals(tokens.get(i).Kind) && "local".equals(tokens.get(i).Text))
                    {
                        stat.Start = tokens.get(i);
                        IsLocal = true;
                        j++;
                        break;
                    }
                    j++;
                //   тип
                case 1:
                    if (!TokenKind.name.equals(tokens.get(i).Kind) && !TokenKind.btyp.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong var declaration: type identifier expected");
                    if (null == stat.Start) stat.Start = tokens.get(i);
                    stat.AddChild("Type", tokens.get(i));
                    j++;
                    break;
                //     array не обязательно
                case 2:
                    if (TokenKind.kwd.equals(tokens.get(i).Kind) && "array".equals(tokens.get(i).Text))
                    {
                        IsArray = true;
                        j++;
                        break;
                    }
                    j++;
                //       имя
                case 3:
                    if (!TokenKind.name.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong var declaration: identifier expected");
                    stat.AddChild("Name", tokens.get(i));
                    j += IsArray ? 2 : 1;
                    break;
                //         = значение по умолчанию не обязательно
                case 4:
                    if (TokenKind.oper.equals(tokens.get(i).Kind) && "=".equals(tokens.get(i).Text))
                    {
                        i++;
                        var expr = TryParseExpression();
                        stat.AddChild(expr);
                        j++;
                        break;
                    }
                    j++;
                //           конец строки
                case 5:
                    if (!TokenKind.ln.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong var declaration: new line expected");
                    j++;
                    break;
            }
        }

        stat.Type = String.format("%s%s", IsLocal ? "L" : "", IsArray ? "Arr" : "Var");

        if (i < tokens.size()) i--;
        return stat;
    }

    /** Остановить если встретился перевод строки */
    final Function<Token, Boolean> DefStopper = (token)->TokenType.br.equals(token.getType());

    Statement TryParseExpression()  throws JassException
    {
        return this.TryParseExpression(null);
    }

    /** 
     * Попытаться распарсить выражение
     * @param stopper Предикат используемый для остановки парсинга выражения, по умолчанию {@link JassParser#DefStopper}
     */
    Statement TryParseExpression(Function<Token, Boolean> stopper) throws JassException
    {
        if (null == stopper) stopper = DefStopper;
        //('constant' type id '=' expr newline | var_declr newline)*
        Statement stat = new Statement("Expr", tokens.get(i));
        var par = new Stack<Statement>();
        Statement child;
        for (; i < tokens.size(); i++)
        {
            if (AddComment(stat)) continue;
            if (0 == par.size() && stopper.apply(tokens.get(i))) break;
            switch (tokens.get(i).getType())
            {
                case TokenType.par:
                    switch (tokens.get(i).Kind)
                    {
                        case TokenKind.lbra:
                        case TokenKind.lind:
                            par.push(stat);
                            stat = stat.MakeChild("Par", tokens.get(i));
                            continue;
                        case TokenKind.rbra:
                        case TokenKind.rind:
                            if (!"Par".equals(stat.Type))
                                JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong expression: unexpected closing parenthes");
                            if (!TokenKind.lbra.equals(stat.Start.Kind) && TokenKind.rbra.equals(tokens.get(i).Kind))
                                JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong expression: another parenthes expected");
                            if (!TokenKind.lind.equals(stat.Start.Kind) && TokenKind.rind.equals(tokens.get(i).Kind))
                                JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong expression: another parenthes expected");
                            stat = par.pop();
                            continue;
                    }
                    JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong expression: unexpected parenthes kind");
                case TokenType.val:
                    stat.AddChild("Val", tokens.get(i));
                    continue;
                case TokenType.oper:
                    stat.AddChild("Oper", tokens.get(i));
                    continue;
                case TokenType.kwd:
                    stat.AddChild(TryParseFuncRef());
                    continue;
                case TokenType.name:
                    if (isYdweCompatible && TokenKind.btyp.equals(tokens.get(i).Kind))
                    {
                        var ctoken = tokens.get(i).Clone();
                        ctoken.Text = String.format("\"%s\"", ctoken.Text);
                        child = new Statement("RVar", ctoken);
                        stat.AddChild(child);
                        continue;
                    }
                    child = TryParseArrayRef();
                    if (null == child)
                        child = TryParseFuncCall();
                    if (null == child)
                        child = new Statement("RVar", tokens.get(i));
                    stat.AddChild(child);
                    continue;
            }
            break;
        }
        //if (0 == stat.Childs.size()) JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong expression: empty expression");
        if (i < tokens.size()) i--;
        if (1 == stat.Childs.size()) return stat.Childs.get(0);
        return stat;
    }

    /** 
     * Остановить если найден перево строки, запятая или закрывающая скобка 
     * <pre>{@code token => TokenKind.ln == token.Kind || (TokenKind.oper == token.Kind && "," == token.Text)}</pre>
     */
    final Function<Token, Boolean> ArgStopper = (token) -> TokenKind.ln.equals(token.Kind) ||
                                                           TokenKind.rbra.equals(token.Kind) ||
                                                           (TokenKind.oper.equals(token.Kind) && ",".equals(token.Text));

    /** Попытаться распарсить вызов функции */
    Statement TryParseFuncCall() throws JassException
    {
        Statement stat = new Statement("FCall", tokens.get(i));

        var start = i;
        var j = 0;
        for (; i < tokens.size() && j < 4; i++)
        {
            if (AddComment(stat)) continue;
            switch (j)
            {
                case 0:
                    if (!TokenKind.name.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong expression: identifier expected");
                    stat.AddChild("Name", tokens.get(i));
                    j++;
                    continue;
                case 1:
                    if (!TokenKind.lbra.equals(tokens.get(i).Kind))
                    {
                        i = start;
                        return null;
                    }
                    j++;
                    continue;
                case 2:
                    var arg = TryParseExpression(ArgStopper);
                    if (!"Expr".equals(arg.Type) || arg.Childs.size() > 0) stat.AddChild(arg);
                    else if (stat.Childs.size() > 1)
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong expression: argument expected");
                    j++;
                    continue;
                case 3:
                    if (TokenKind.oper.equals(tokens.get(i).Kind) && ",".equals(tokens.get(i).Text))
                        j--;
                    else if (TokenKind.rbra.equals(tokens.get(i).Kind))
                        j++;
                    else
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong expression: comma or parenthes expected");
                    continue;
            }
        }

        if (i < tokens.size()) i--;
        return stat;
    }

    /**
     * Остановить если найден перевод строки или закрывающая квадратная скобка 
     * <pre>{@code token => TokenKind.ln == token.Kind || TokenKind.rind == token.Kind} </pre>
     */
    Function<Token, Boolean> IndStopper = (token) -> TokenKind.ln.equals(token.Kind) ||
                                                     TokenKind.rind.equals(token.Kind);

    /** Попытаться распарсить ссылку на элемент массива */
    Statement TryParseArrayRef() throws JassException
    {
        Statement stat = new Statement("RArr", tokens.get(i));

        var start = i;
        var j = 0;
        for (; i < tokens.size() && j < 4; i++)
        {
            if (AddComment(stat)) continue;
            switch (j)
            {
                case 0:
                    if (!TokenKind.name.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong expression: identifier expected");
                    stat.AddChild("Name", tokens.get(i));
                    j++;
                    continue;
                case 1:
                    if (!TokenKind.lind.equals(tokens.get(i).Kind))
                    {
                        i = start;
                        return null;
                    }
                    j++;
                    continue;
                case 2:
                    var ind = TryParseExpression(IndStopper);
                    if ("Expr".equals(ind.Type) && 0 == ind.Childs.size())
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong expression: index expected");
                    stat.MakeChild("Ind", ind.Start)
                        .AddChild(ind);
                    j++;
                    continue;
                case 3:
                    if (!TokenKind.rind.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong expression: comma or parenthes expected");
                    j++;
                    continue;
            }
        }

        if (i < tokens.size()) i--;
        return stat;
    }

    /** Попытаться распарсить ссылку на функцию */
    Statement TryParseFuncRef() throws JassException
    {
        Statement stat = new Statement("RFunc", tokens.get(i));

        var j = 0;
        for (; i < tokens.size() && j < 2; i++)
        {
            if (AddComment(stat)) continue;
            switch (j)
            {
                case 0:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) && !"function".equals(tokens.get(i).Text))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong expression: function expected");
                    j++;
                    continue;
                case 1:
                    if (!TokenKind.name.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong expression: identifier expected");
                    stat.Start = tokens.get(i);
                    j++;
                    continue;
            }
        }

        if (i < tokens.size()) i--;
        return stat;
    }

    /** Попытаться распарсить объявление нативной функции */
    Statement TryParseNative() throws JassException
    {
        var stat = new Statement("Native");

        var j = 0;
        boolean IsConst = false;
        for (; i < tokens.size() && j < 4; i++)
        {
            if (AddComment(stat)) continue;
            switch (j)
            {
                // constant не обязательно
                case 0:
                    if (TokenKind.kwd.equals(tokens.get(i).Kind) && "constant".equals(tokens.get(i).Text)) {
                        stat.Start = tokens.get(i);
                        IsConst = true;
                        j++;
                        break;
                    }
                    j++;
                case 1:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"native".equals(tokens.get(i).Text)) return null;
                    if (null == stat.Start) stat.Start = tokens.get(i);
                    j++;
                    break;
                case 2:
                    stat.AddChild(TryParseFuncDecl());
                    j++;
                    break;
                case 3:
                    if (!TokenKind.ln.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong native declaration: new line expected");
                    j++;
                    break;
            }
        }

        if (IsConst) stat.Type = "CNative";

        if (i < tokens.size()) i--;
        return stat;
    }

    /** Попытаться распарсить объявление функции */
    Statement TryParseFuncDecl() throws JassException
    {
        var stat = new Statement("FuncDecl");

        var j = 0;
        for (; i < tokens.size() && j < 5; i++)
        {
            if (AddComment(stat)) continue;
            switch (j)
            {
                case 0:
                    if (!TokenKind.name.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong function declaration: identifier expected");
                    stat.AddChild("Name", tokens.get(i));
                    j++;
                    continue;
                case 1:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"takes".equals(tokens.get(i).Text))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong function declaration: takes keyword expected");
                    j++;
                    continue;
                case 2:
                    if (TokenType.name.equals(tokens.get(i).getType()) && "nothing".equals(tokens.get(i).Text))
                        stat.AddChild("Params", tokens.get(i));
                    else
                        stat.AddChild(TryParseParams());
                    j++;
                    continue;
                case 3:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"returns".equals(tokens.get(i).Text))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong function declaration: returns keyword expected");
                    j++;
                    continue;
                case 4:
                    if ((TokenType.name.equals(tokens.get(i).getType()) && "nothing".equals(tokens.get(i).Text)) ||
                        (TokenType.name.equals(tokens.get(i).getType())))
                        stat.AddChild("Result", tokens.get(i));
                    else
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong function declaration: nothing or type name expected");
                    j++;
                    continue;
            }
        }

        if (i < tokens.size()) i--;
        return stat;
    }

    /** Попытаться распарсить параметры функции */
    Statement TryParseParams() throws JassException
    {
        var stat = new Statement("Params");

        var j = 0;

        Token type = null;
        for (; i < tokens.size() && j < 3; i++)
        {
            if (AddComment(stat)) continue;
            switch (j)
            {
                case 0:
                    if (!TokenType.name.equals(tokens.get(i).getType()))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong function declaration: type name expected");
                    type = tokens.get(i);
                    j++;
                    continue;
                case 1:
                    if (!TokenType.name.equals(tokens.get(i).getType()))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong function declaration: param name expected");
                    stat.MakeChild("Param", type)
                        .AddChild("Type", type)
                        .AddChild("Name", tokens.get(i));
                    j++;
                    continue;
                case 2:
                    if (TokenKind.oper.equals(tokens.get(i).Kind) && ",".equals(tokens.get(i).Text))
                        j -= 2;
                    else if (TokenKind.kwd.equals(tokens.get(i).Kind) && "returns".equals(tokens.get(i).Text))
                        j++;
                    else
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong expression: comma or returns expected");
                    continue;
            }
        }

        if (i < tokens.size() + 1) i -= 2;
        return stat;
    }

    /** Попытаться распарсить функцию */
    Statement TryParseFunc() throws JassException
    {
        var stat = new Statement("Func");

        var j = 0;
        boolean IsConst = false;
        for (; i < tokens.size() && j < 7; i++)
        {
            if (AddComment(stat)) continue;
            switch (j)
            {
                // constant не обязательно
                case 0:
                    if (TokenKind.kwd.equals(tokens.get(i).Kind) && "constant".equals(tokens.get(i).Text))
                    {
                        stat.Start = tokens.get(i);
                        IsConst = true;
                        j++;
                        continue;
                    }
                    j++;
                case 1:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"function".equals(tokens.get(i).Text)) return null;
                    if (null == stat.Start) stat.Start = tokens.get(i);
                    j++;
                    continue;
                case 2:
                    stat.AddChild(TryParseFuncDecl());
                    j++;
                    continue;
                case 3:
                case 6:
                    if (!TokenKind.ln.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong function declaration: new line expected");
                    j++;
                    continue;
                case 4:
                    stat.AddChild(TryParseFuncLocals());
                    stat.AddChild(TryParseFuncBody());
                    j++;
                    continue;
                case 5:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"endfunction".equals(tokens.get(i).Text))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong function declaration: endfunction expected");
                    j++;
                    continue;
            }
        }

        if (IsConst) stat.Type = "CFunc";

        if (i < tokens.size()) i--;
        return stat;
    }

    /** Попытаться распарсит тело функции */
    public Statement TryParseFuncLocals() throws JassException
    {
        var stat = new Statement("FuncLocals");

        for (; i < tokens.size(); i++)
        {
            if (AddComment(stat)) continue;
            if (TokenKind.ln.equals(tokens.get(i).Kind)) continue;

            if (TokenKind.kwd.equals(tokens.get(i).Kind) && "endfunction".equals(tokens.get(i).Text))
                break;
            if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"local".equals(tokens.get(i).Text))
                break;
            stat.AddChild(TryParseVarDecl());
        }

        if (i < tokens.size()) i--;
        return stat;
    }

    /** Попытаться распарсит тело функции */
    public Statement TryParseFuncBody() throws JassException
    {
        var stat = new Statement("FuncBody");

        for (; i < tokens.size(); i++)
        {
            if (AddComment(stat)) continue;
            if (TokenKind.ln.equals(tokens.get(i).Kind)) continue;

            if (TokenKind.kwd.equals(tokens.get(i).Kind) && "endfunction".equals(tokens.get(i).Text))
                break;

            stat.AddChild(TryParseStatement());
        }

        if (i < tokens.size()) i--;
        return stat;
    }

    public Statement TryParseMacroCall() {
        var sb = new StringBuilder("//");
        var stoken = tokens.get(i).Clone();
        for (; i < tokens.size(); i++)
        {
            if (TokenKind.ln.equals(tokens.get(i).Kind)) break;
            sb.append(String.format(" %s", tokens.get(i).Text));
        }
        stoken.Text = sb.toString();
        return new Statement(StatementType.YdweMacro, stoken);
    }

    /** Попытаться распарсить инструкцию */
    public Statement TryParseStatement() throws JassException
    {
        for (; i < tokens.size(); i++)
        {
            if (TokenType.comm.equals(tokens.get(i).getType()))
                return new Statement("Comm", tokens.get(i));

            if (TokenKind.ln.equals(tokens.get(i).Kind)) break;

            if (isYdweCompatible && TokenKind.name.equals(tokens.get(i).Kind)) {
                return TryParseMacroCall();
            }
            if (!TokenKind.kwd.equals(tokens.get(i).Kind)) JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "statement error: keyword expected");

            switch (tokens.get(i).Text)
            {
                case "set":
                    return TryParseSet();
                case "call":
                    return TryParseCall();
                case "if":
                    return TryParseIf();
                case "loop":
                    return TryParseLoop();
                case "return":
                    return TryParseReturn();
                case "exitwhen":
                    return TryParseExit();
                case "debug":
                    i++;
                    var dbg = TryParseStatement();
                    if ("Return".equals(dbg.Type) || "Debug".equals(dbg.Type))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "statement error: wrong statement");
                    var stat = new Statement("Debug");
                    stat.AddChild(dbg);
                    return stat;
            }
            JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "statement error: unknown keyword");
        }
        return new Statement("Error", tokens.get(i));
    }

    Statement TryParseSet() throws JassException
    {
        var stat = new Statement("Set");

        var j = 0;
        var IsArray = false;
        for (; i < tokens.size() && j < 5; i++)
        {
            if (AddComment(stat)) continue;
            switch (j)
            {
                // constant не обязательно
                case 0:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"set".equals(tokens.get(i).Text)) return null;
                    if (null == stat.Start) stat.Start = tokens.get(i);
                    j++;
                    continue;
                case 1:
                    if (!TokenType.name.equals(tokens.get(i).getType()))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong set statement: variable name expected");
                    stat.AddChild("Name", tokens.get(i));
                    j++;
                    continue;
                case 2:
                    if (TokenKind.lind.equals(tokens.get(i).Kind)) {
                        IsArray = true;
                        i++;
                        var ind = TryParseExpression(IndStopper);
                        stat.MakeChild("Ind", ind.Start)
                            .AddChild(ind);
                        i++;
                        j++;
                        continue;
                    }
                    j++;
                case 3:
                    if (!TokenKind.oper.equals(tokens.get(i).Kind) || !"=".equals(tokens.get(i).Text))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong set statement: = expected");
                    i++;
                    var expr = TryParseExpression();
                    stat.AddChild(expr);
                    j++;
                    continue;
                case 4:
                    if (!TokenKind.ln.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong native declaration: new line expected");
                    j++;
                    break;
            }
        }
        if (IsArray) stat.Type = "ASet";

        if (i < tokens.size()) i--;
        return stat;
    }

    Statement TryParseCall() throws JassException
    {
        Statement stat = null;

        var j = 0;
        for (; i < tokens.size() && j < 3; i++)
        {
            if (AddComment(stat)) continue;
            switch (j)
            {
                case 0:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"call".equals(tokens.get(i).Text)) return null;
                    j++;
                    continue;
                case 1:
                    stat = TryParseFuncCall();
                    j++;
                    continue;
                case 2:
                    if (!TokenKind.ln.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong native declaration: new line expected");
                    j++;
                    break;
            }
        }

        if (i < tokens.size()) i--;
        return null == stat ? new Statement("Error", tokens.get(i)) : stat;
    }

    Function<Token, Boolean> IfStopper = (token) -> TokenKind.ln.equals(token.Kind) || 
                                                    (TokenKind.kwd.equals(token.Kind) && "then".equals(token.Text));

    Statement TryParseIf() throws JassException
    {
        /*
        ifthenelse      := 'if' expr 'then' newline 
                            statement_list else_clause? 'endif' 
        else_clause     := 'else' newline statement_list | 
                           'elseif' expr 'then' newline statement_list else_clause?
        */
        Statement stat = new Statement("If");

        Statement cond = new Statement("Cond");
        Statement then = null;
        var j = 0;
        for (; i < tokens.size() && j < 8; i++)
        {
            if (AddComment(stat)) continue;

            switch (j)
            {
                case 0:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"if".equals(tokens.get(i).Text)) return null;
                    j++;
                    continue;
                case 1:
                    cond.AddChild(TryParseExpression(IfStopper));
                    stat.AddChild(cond);
                    j++;
                    continue;
                case 2:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"then".equals(tokens.get(i).Text))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong if statement: then expected");
                    then = new Statement("Then");
                    stat.AddChild(then);
                    j++;
                    continue;
                case 3:
                case 5:
                case 7:
                    if (!TokenKind.ln.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong if statement: new line expected");
                    j++;
                    continue;
                case 4:
                    if (TokenKind.ln.equals(tokens.get(i).Kind)) continue;
                    if (TokenKind.kwd.equals(tokens.get(i).Kind) && "elseif".equals(tokens.get(i).Text))
                    {
                        cond = new Statement("ElseCond");
                        j -= 3;
                        continue;
                    }
                    if (TokenKind.kwd.equals(tokens.get(i).Kind) && "else".equals(tokens.get(i).Text))
                    {
                        then = new Statement("Else");
                        stat.AddChild(then);
                        j++;
                        continue;
                    }
                    if (TokenKind.kwd.equals(tokens.get(i).Kind) && "endif".equals(tokens.get(i).Text))
                    {
                        j += 3;
                        continue;
                    }
                    then.AddChild(TryParseStatement());
                    continue;
                case 6:
                    if (TokenKind.ln.equals(tokens.get(i).Kind)) continue;
                    if (TokenKind.kwd.equals(tokens.get(i).Kind) && "endif".equals(tokens.get(i).Text))
                    {
                        j ++;
                        continue;
                    }
                    then.AddChild(TryParseStatement());
                    continue;
            }
        }

        if (i < tokens.size()) i--;
        return stat;
    }

    Statement TryParseLoop() throws JassException
    {
        Statement stat = new Statement("Loop");

        var j = 0;
        for (; i < tokens.size() && j < 3; i++)
        {
            if (AddComment(stat)) continue;

            switch (j)
            {
                case 0:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"loop".equals(tokens.get(i).Text)) return null;
                    j++;
                    continue;
                case 1:
                    if (TokenKind.ln.equals(tokens.get(i).Kind)) continue;
                    if (TokenKind.kwd.equals(tokens.get(i).Kind) && "endloop".equals(tokens.get(i).Text)) {
                        j++;
                        continue;
                    }
                    //if (TokenKind.kwd.equals(tokens.get(i).Kind && "exitwhen".equals(tokens.get(i).Text))
                    //{
                    //    i++;
                    //    stat.MakeChild("Exit", tokens.get(i))
                    //        .AddChild(TryParseExpression());
                    //    i++;
                    //    continue;
                    //}
                    stat.AddChild(TryParseStatement());
                    continue;
                case 2:
                    if (!TokenKind.ln.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong loop statement: new line expected");
                    j++;
                    break;
            }
        }

        if (i < tokens.size()) i--;
        return stat;
    }

    Statement TryParseExit() throws JassException
    {
        // *return          := 'return' expr?
        Statement stat = new Statement("Exit");

        var j = 0;
        for (; i < tokens.size() && j < 3; i++)
        {
            // if (AddComment(stat)) continue;
            switch (j)
            {
                case 0:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"exitwhen".equals(tokens.get(i).Text)) return null;
                    j++;
                    continue;
                case 1:
                    stat.AddChild(TryParseExpression());
                    j++;
                    continue;
                case 2:
                    if (!TokenKind.ln.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong exitwhen statement: new line expected");
                    j++;
                    break;
            }
        }

        if (i < tokens.size()) i--;
        return stat;
    }

    Statement TryParseReturn() throws JassException
    {
        // *return          := 'return' expr?
        Statement stat = new Statement("Return");

        var j = 0;
        for (; i < tokens.size() && j < 3; i++)
        {
            // if (AddComment(stat)) continue;
            switch (j)
            {
                case 0:
                    if (!TokenKind.kwd.equals(tokens.get(i).Kind) || !"return".equals(tokens.get(i).Text)) return null;
                    j++;
                    continue;
                case 1:
                    stat.AddChild(TryParseExpression());
                    j++;
                    continue;
                case 2:
                    if (!TokenKind.ln.equals(tokens.get(i).Kind))
                        JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong return statement: new line expected");
                    j++;
                    break;
            }
        }

        if (i < tokens.size()) i--;
        return stat;
    }

    public Statement Parse(List<Token> tokens) throws JassException
    {
        this.tokens = tokens;

        var prog = new Statement("Prog");
        boolean isDeclPassed = false;
        Statement stat = null;
        for (i = 0; i < tokens.size(); i++)
        {
            if (AddComment(prog)) continue;
            if (TokenKind.ln.equals(tokens.get(i).Kind)) continue;

            if (!TokenKind.kwd.equals(tokens.get(i).Kind)) JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "error: keyword expected");

            if ("type".equals(tokens.get(i).Text))
            {
                if (isDeclPassed) JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong type declaration: not in declaration block");
                stat = TryParseTypeDecl();
                prog.AddChild(stat);
                continue;
            }
            if ("globals".equals(tokens.get(i).Text))
            {
                if (isDeclPassed) JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong global declaration: not in declaration block");
                stat = TryParseGlobals();
                prog.Childs.add(stat);
                continue;
            }
            if ("constant".equals(tokens.get(i).Text) || "native".equals(tokens.get(i).Text))
            {
                stat = TryParseNative();
                if (null != stat)
                {
                    if (isDeclPassed) JassException.Error(tokens.get(i).Line, tokens.get(i).Col, "wrong native declaration: not in declaration block");
                    prog.Childs.add(stat);
                    continue;
                }
            }

            if ("constant".equals(tokens.get(i).Text) || "function".equals(tokens.get(i).Text))
            {
                if (!isDeclPassed) isDeclPassed = true;
                stat = TryParseFunc();
                prog.Childs.add(stat);
                continue;
            }
        }
        return prog;
    }
}
