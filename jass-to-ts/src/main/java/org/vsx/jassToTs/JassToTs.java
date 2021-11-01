package org.vsx.jassToTs;

import java.util.ArrayList;

import org.vsx.func.FuncThrows;
import org.vsx.exception.NotImplementedException;
import org.vsx.jass.Statement;
import org.vsx.jass.StatementType;
import org.vsx.jass.TokenKind;

public class JassToTs {
    /**
     * режи файла описания (*.d.ts)
     */
    boolean IsDTS;

    /**
     * Количество символов в 1 сдвиге
     */
    int IndentSize;

    /**
     * Режим оптимизации
     */
    boolean isOptimizationNeeded;
    /**
     * Режим совмести с YDWE
     */
    boolean isYdweCompatible;

    /**
     * Флаг что найдена 1 из функци YDUserData*
     */
    boolean isYdwe_YDUserData = false;

    /**
     * Транслятор из Jass в TypeScript. По умолчанию:
     *  - без оптимизации
     *  - без совместимости с YDWE
     *  - преобразование в .ts
     *  - размер сдиига 4 символа
     */
    public JassToTs() {
        this(false, false, false, 4);
    }

    /**
     * Транслятор из Jass в TypeScript.
     * Размер сдвига 4 символа
     * @param isOptimizationNeeded Режим оптимизации
     * @param isYdweCompatible Режим совместимости с YDWE
     * @param IsDTS Режим файла описания (*.d.ts)
     */
    public JassToTs(boolean isOptimizationNeeded, boolean isYdweCompatible, boolean IsDTS)
    {
        this(isOptimizationNeeded, isYdweCompatible, IsDTS, 4);
    }

    /**
     * Транслятор из Jass в TypeScript
     * @param isOptimizationNeeded Режим оптимизации
     * @param isYdweCompatible Режим совместимости с YDWE
     * @param IsDTS Режим файла описания (*.d.ts)
     * @param IndentSize Количество символов в 1 сдвиге
     */
    public JassToTs(boolean isOptimizationNeeded, boolean isYdweCompatible, boolean IsDTS, int IndentSize)
    {
        this.isOptimizationNeeded = isOptimizationNeeded;
        this.isYdweCompatible = isYdweCompatible;
        this.IsDTS = IsDTS;
        this.IndentSize = IndentSize;
    }

    /**
     * Транслировать дерево в TypeScript код
     */
    public String Convert(Statement tree) throws Exception
    {
        var header = new StringBuilder()
            .append("/// Some references\n")
            .append("/// <reference path=\"war3core.d.ts\"/>\n")
            .append("\n");
        
            var sb = ConvertDeclarations(tree);
        if (isYdwe_YDUserData) 
            header = header.append("/// For YDWE Macro\n")
                           .append("\n")
                           .append("var dataBaseContext: { [id: string]: any } = dataBaseContext || {};\n")
                           .append("\n")
                           .append("function UserDataSet(type: string, handle: handle, varName: string, varValue: any): void {\n")
                           .append("    let key = type + GetHandleId(handle);\n")
                           .append("    let data = dataBaseContext[key] || (dataBaseContext[key] = {});\n")
                           .append("    data[varName] = varValue;\n")
                           .append("}\n")
                           .append("\n")
                           .append("function UserDataGet(type: string, handle: handle, varName: string): any {\n")
                           .append("    let key = type + GetHandleId(handle);\n")
                           .append("    let data = dataBaseContext[key];\n")
                           .append("    return data ? data[varName] : null;\n")
                           .append("}\n")
                           .append("\n")
                           .append("///\n")
                           .append("\n");
                           
        return header.append(sb).toString();
    }

    /**
     * Преобразовать объявления
     * @param stat выражение
     */
    StringBuilder ConvertDeclarations(Statement stat) throws Exception
    {
        switch (stat.Type)
        {
            case StatementType.YdweMacro:
            case StatementType.Comm:
                return new StringBuilder().append(stat.Start.Text).append("\n");
            case StatementType.Prog:
                return new StringBuilder()
                    //.AppendJoin("", stat.Childs.Select(x => ConvertDeclarations(x)));
                    .append(stat.Childs
                                .stream()
                                .map(FuncThrows.RunOrThrow((x)->ConvertDeclarations(x)))
                                .reduce(new StringBuilder(), (x, y) -> x.append(y))
                            );
            case StatementType.TypeDecl:
                return ConvertTypeDecl(stat);
            case StatementType.Glob:
                return new StringBuilder()
                    .append("// global var declaration\n")
                    //.AppendJoin("", stat.Childs.Select(x => ConvertGlobals(x)))
                    .append(stat.Childs
                                .stream()
                                .map(FuncThrows.RunOrThrow((x) -> ConvertGlobals(x)))
                                .reduce(new StringBuilder(), (x, y) -> x.append(y)))
                    .append("// end global var declaration\n");
            case StatementType.Native:
            case StatementType.CNative:
            case StatementType.Func:
            case StatementType.CFunc:
                return ConvertFunc(stat);
            default: 
                JassTranslatorException.Error(String.format("unknown statement %s", stat.Start));
                return new StringBuilder(String.format("/*unknown statement %s*/", stat.Start));
        }
    }

    /**
     * Преобразовать базовый тип
     * @param type базовый JASS тип
     * @return Тип в TypeScript </returns>
     */
    String ConvertType(String type)
    {
        switch (type)
        {
            // Базовые типы
            case "integer":
            case "real": return "number";
            case "boolean": return "boolean";
            case "String": return "String";
            case "handle": return "handle"; // TODO: проверить
            case "code": return "Function"; // TODO: проверить
            case "nothing": return "void";
            default: return type;
        }
    }

    /**
     * Преобразовать оператор
     * @param type оператор JASS
     * @return оператор TypeScript
     */
    String ConvertOperator(String type) throws Exception
    {
        switch (type)
        {
            case "=": case ",":
            case "+": case "-":
            case "*": case "/":
            case ">": case "<":
            case "==": case "!=":
            case ">=": case "<=":
                return type;
            case "and":
                return "&&";
            case "or":
                return "||";
            case "not":
                return "!";
            default:
                JassTranslatorException.Error("unknown operator");
                return String.format("/* unknown operator %s */", type);
        }
    }

    /**
     * Преобразование объявления типа
     */
    StringBuilder ConvertTypeDecl(Statement tree) throws Exception
    {
        var sb = new StringBuilder();
        var typeName = "";
        var baseType = "";
        var comm = new StringBuilder();
        for (var i = 0; i < tree.Childs.size(); i++)
        {
            switch (tree.Childs.get(i).Type)
            {
                case StatementType.YdweMacro:
                case StatementType.Comm:
                    comm.append(tree.Childs.get(i).Start.Text).append('\n');
                    continue;
                case StatementType.TypeName:
                    typeName = tree.Childs.get(i).Start.Text;
                    continue;
                case StatementType.BaseType:
                    baseType = tree.Childs.get(i).Start.Text;
                    continue;
                default:
                    // TODO error
                    JassTranslatorException.Error(String.format("unknown statement %s",tree.Childs.get(i).Start));
            }
        }
        if (IsDTS) sb.append("declare ");
        return sb.append("class ").append(typeName).append(" extends ").append(baseType).append(" { }\n")
                 .append(comm);
    }

    /**
     * Преобразование глобальной переменной
     */
    StringBuilder ConvertGlobals(Statement stat) throws Exception
    {
        switch (stat.Type)
        {
            case StatementType.YdweMacro:
            case StatementType.Comm:
                return new StringBuilder().append(stat.Start.Text).append("\n");
            case StatementType.GConst:
            case StatementType.GVar:
            case StatementType.GArr:
                return ConvertVarDecl(stat);
            default: 
                JassTranslatorException.Error(String.format("unknown statement %s", stat.Start));
                return new StringBuilder(String.format("/* unknown statement %s */", stat.Start));
        }
    }

    /**
     * Преобразование локальной переменной
     */
    StringBuilder ConvertLocals(Statement stat) throws Exception
    {
        return ConvertLocals(stat, 0);
    }

    /**
     * Преобразование локальной переменной
     */
    StringBuilder ConvertLocals(Statement stat, int indent) throws Exception
    {
        switch (stat.Type)
        {
            case StatementType.YdweMacro:
            case StatementType.Comm:
                return AddIndent(new StringBuilder(), indent).append(stat.Start.Text).append("\n");
            case StatementType.LVar:
            case StatementType.LArr:
                return ConvertVarDecl(stat, indent);
            default: 
                JassTranslatorException.Error(String.format("unknown statement %s", stat.Start));
                return new StringBuilder(String.format("/* unknown statement %s */", stat.Start));
        }
    }

    StringBuilder ConvertVarDecl(Statement tree) throws Exception {
        return ConvertVarDecl(tree, 0);
    }

    /**
     * Преобразование объявления переменной
     */
    StringBuilder ConvertVarDecl(Statement tree, int indent) throws Exception
    {
        var sb = new StringBuilder();
        var typeDecl = "";
        var isDeclare = false;
        switch (tree.Type)
        {
            case StatementType.GConst:
                typeDecl = "const ";
                break;
            case StatementType.GVar:
            case StatementType.GArr:
                typeDecl = "var ";
                isDeclare = true && IsDTS;
                break;
            case StatementType.LVar:
            case StatementType.LArr:
                typeDecl = "let ";
                break;
        }
        var isArr = StatementType.GArr.equals(tree.Type) || StatementType.LArr.equals(tree.Type);
        var type = "";
        var name = "";
        var comm = new StringBuilder();
        StringBuilder expr = null;
        for (var i = 0; i < tree.Childs.size(); i++)
        {
            switch (tree.Childs.get(i).Type)
            {
                case StatementType.YdweMacro:
                case StatementType.Comm:
                    comm.append(tree.Childs.get(i).Start.Text).append('\n');
                    continue;
                case StatementType.Type:
                    type = ConvertType(tree.Childs.get(i).Start.Text);
                    continue;
                case StatementType.Name:
                    name = tree.Childs.get(i).Start.Text;
                    continue;
                case StatementType.Expr:
                case StatementType.RVar:
                case StatementType.FCall:
                case StatementType.RArr:
                case StatementType.RFunc:
                case StatementType.Val:
                case StatementType.Par:
                    expr = ConvertExprElem(tree.Childs.get(i));
                    continue;
                default:
                    // TODO error
                    JassTranslatorException.Error(String.format("unknown statement %s", tree.Childs.get(i).Start));
            }
        }
        AddIndent(sb, indent);
        if (isDeclare) sb.append("declare ");
        sb.append(typeDecl).append(name).append(": ").append(type);
        if (isArr) sb.append("[]");
        if (null != expr)
            sb.append(" = ").append(expr);
        return sb.append(";\n")
                 .append(comm);
    }

    /**
     * Преобразование выражения
     */
    StringBuilder ConvertExpr(Statement tree) throws Exception {
        return new StringBuilder()
            //.AppendJoin(" ", tree.Childs.Select(x => ConvertExprElem(x)));
            .append(tree.Childs
                        .stream()
                        .map(FuncThrows.RunOrThrow((x)->ConvertExprElem(x)))
                        .reduce(new StringBuilder(""),
                                (x, y)->x.append(" ")
                                         .append(y))
                        .delete(0, 1)
                    );
    }

    /**
     * Преобразование составной части выражения 
     * (идентификатор, константа, оператор и т.п.)
     */
    StringBuilder ConvertExprElem(Statement elem) throws Exception
    {
        var sb = new StringBuilder();
        switch (elem.Type)
        {
            case StatementType.YdweMacro:
            case StatementType.Comm:
                return sb.append(elem.Start.Text).append('\n');
            case StatementType.RVar:
            case StatementType.RFunc:
                return sb.append(elem.Start.Text);
            case StatementType.Val:
                return sb.append(ConvertValue(elem));
            case StatementType.Oper:
                return sb.append(ConvertOperator(elem.Start.Text));
            case StatementType.RArr:
                return sb.append(ConvertArrayRef(elem));
            case StatementType.FCall:
                return sb.append(ConvertFuncCall(elem));
            case StatementType.Ind:
                return sb.append("[").append(ConvertExpr(elem)).append("]");
            case StatementType.Par:
                {
                    var childExpr = ConvertExpr(elem);
                    if (isOptimizationNeeded) { 
                        while('(' == childExpr.charAt(0) && childExpr.charAt(childExpr.length() - 1) == ')')
                            childExpr = childExpr.delete(0, 1).delete(childExpr.length() - 1, childExpr.length());
                    }
                    return sb.append("(").append(childExpr).append(")");
                }
            case StatementType.Expr:
                return ConvertExpr(elem);
            default:
                JassTranslatorException.Error(String.format("unknown statement %s", elem.Start));
                return new StringBuilder(String.format("/* unknown statement %s */", elem.Start));
        }
    }

    /**
     * Преобразовать безымянную константу
     */
    StringBuilder ConvertValue(Statement stat)
    {
        var sb = new StringBuilder();
        var val = stat.Start.Text;
        switch (stat.Start.Kind) {
            case TokenKind.adec:
                var number = 0;
                for (var i = 1; i < val.length() -1; i++)
                {
                    number = number << 8;
                    number += val.charAt(i);
                }
                sb.append(String.format("%d", number)).append(" /*").append(val).append("*/");
                break;
            //case TokenKind.bin:
            case TokenKind.dhex:
                sb.append("0x").append(val.substring(1));
                break;
            case TokenKind.oct:
                if (!"0".equals(val)) {
                    sb.append("0o").append(val.substring(1));
                    break;
                }
            case TokenKind.ndec:
            case TokenKind.xhex:
            case TokenKind.real:
            case TokenKind._bool:
            case TokenKind._null:
            case TokenKind.dstr:
                sb.append(val);
                break;
            default:
                break;
        }
        return sb;
    }

    /**
     * Преобразовать ссылку на элемент массива
     */
    StringBuilder ConvertArrayRef(Statement stat) throws Exception
    {
        var sb = new StringBuilder();

        var name = "";
        StringBuilder index = null;
        var comm = new StringBuilder();
        for (var i = 0; i < stat.Childs.size(); i++)
        {
            switch (stat.Childs.get(i).Type)
            {
                case StatementType.YdweMacro:
                case StatementType.Comm:
                    comm.append(stat.Childs.get(i).Start.Text).append('\n');
                    continue;
                case StatementType.Name:
                    name = stat.Childs.get(i).Start.Text;
                    continue;
                case StatementType.Ind:
                    index = ConvertExprElem(stat.Childs.get(i));
                    continue;
                default:
                    // TODO error
                    JassTranslatorException.Error(String.format("unknown statement %s", stat.Childs.get(i).Start));
            }
        }

        sb.append(name).append(index).append(comm);

        return sb;
    }

    final FuncThrows<Statement, StringBuilder, Exception> ConvertYDLocal_Set = (tree) -> {
        var sb = new StringBuilder();
        var args = new ArrayList<StringBuilder>();
        var comm = new StringBuilder();
        for (var i = 0; i < tree.Childs.size(); i++)
        {
            switch (tree.Childs.get(i).Type)
            {
                case StatementType.YdweMacro:
                case StatementType.Comm:
                    comm.append(tree.Childs.get(i).Start.Text)
                        .append('\n');
                    continue;
                case StatementType.Name:
                    continue;
                default:
                    args.add(ConvertExprElem(tree.Childs.get(i)));
                    continue;
            }
        }
        return sb.append(args.get(1).substring(1, args.get(1).length() - 1))
                 .append(" = ")
                 .append(args.get(2))
                 .append(comm);
    };

    final FuncThrows<Statement, StringBuilder, Exception> ConvertYDLocal_Get = (tree) -> {
        var sb = new StringBuilder();
        var args = new ArrayList<StringBuilder>();
        var comm = new StringBuilder();
        for (var i = 0; i < tree.Childs.size(); i++)
        {
            switch (tree.Childs.get(i).Type)
            {
                case StatementType.YdweMacro:
                case StatementType.Comm:
                    comm.append(tree.Childs.get(i).Start.Text)
                    .append('\n');
                    continue;
                case StatementType.Name:
                    continue;
                default:
                    args.add(ConvertExprElem(tree.Childs.get(i)));
                    continue;
            }
        }
        return sb.append(args.get(1).substring(1, args.get(1).length() - 1))
                 // TODO подумать как воспроизвести
                 .append(comm);
    };

    final FuncThrows<Statement, StringBuilder, Exception> ConvertYDUserDataSet = (tree) -> {
        isYdwe_YDUserData = true;
        
        var sb = new StringBuilder();
        var args = new ArrayList<StringBuilder>();
        var comm = new StringBuilder();
        for (var i = 0; i < tree.Childs.size(); i++)
        {
            switch (tree.Childs.get(i).Type)
            {
                case StatementType.YdweMacro:
                case StatementType.Comm:
                    comm.append(tree.Childs.get(i).Start.Text).append('\n');
                    continue;
                case StatementType.Name:
                    continue;
                default:
                    args.add(ConvertExprElem(tree.Childs.get(i)));
                    continue;
            }
        }
        return sb.append("UserDataSet")
                 .append("(")
                 .append(args.get(0))
                 .append(", ")
                 .append(args.get(1))
                 .append(", ")
                 .append(args.get(2))
                 .append(", ")
                 .append(args.get(4))
                 .append(")")
                 .append(comm);
    };

    final FuncThrows<Statement, StringBuilder, Exception> ConvertYDUserDataGet = (tree) -> {
        isYdwe_YDUserData = true;

        var sb = new StringBuilder();
        var args = new ArrayList<StringBuilder>();
        var comm = new StringBuilder();
        for (var i = 0; i < tree.Childs.size(); i++)
        {
            switch (tree.Childs.get(i).Type)
            {
                case StatementType.YdweMacro:
                case StatementType.Comm:
                    comm.append(tree.Childs.get(i).Start.Text).append('\n');
                    continue;
                case StatementType.Name:
                    continue;
                default:
                    args.add(ConvertExprElem(tree.Childs.get(i)));
                    continue;
            }
        }
        return sb.append("UserDataGet")
                 .append("(")
                 .append(args.get(0))
                 .append(", ")
                 .append(args.get(1))
                 .append(", ")
                 .append(args.get(2))
                 .append(")")
                 .append(comm);
    };

    final FuncThrows<Statement, StringBuilder, Exception> ConvertYDWEOperatorString3 = (tree) -> {
        isYdwe_YDUserData = true;

        var sb = new StringBuilder();
        var args = new ArrayList<StringBuilder>();
        var comm = new StringBuilder();
        for (var i = 0; i < tree.Childs.size(); i++)
        {
            switch (tree.Childs.get(i).Type)
            {
                case StatementType.YdweMacro:
                case StatementType.Comm:
                    comm.append(tree.Childs.get(i).Start.Text).append('\n');
                    continue;
                case StatementType.Name:
                    continue;
                default:
                    args.add(ConvertExprElem(tree.Childs.get(i)));
                    continue;
            }
        }
        return sb.append(args.stream()
                             .reduce(new StringBuilder(""),
                                     (x,y)->x.append(" + ")
                                             .append(y))
                             .delete(0, 2))
                 .append(comm);
    };

    FuncThrows<Statement, StringBuilder, Exception> CheckYdweMacro(String name) {
        if (name.matches("YDLocal[0-9]+Set")) return ConvertYDLocal_Set;
        if (name.matches("YDLocal[0-9]+Get")) return ConvertYDLocal_Get;
        if (name.matches("YDUserDataSet")) return ConvertYDUserDataSet;
        if (name.matches("YDUserDataGet")) return ConvertYDUserDataGet;
        if (name.matches("YDWEOperatorString3")) return ConvertYDWEOperatorString3;
        return null;
    }
    
    /**
     * Преобразование вызова функции
     */
    StringBuilder ConvertFuncCall(Statement tree) throws Exception
    {
        var sb = new StringBuilder();
        var name = "";
        var args = new ArrayList<StringBuilder>();
        var comm = new StringBuilder();
        for (var i = 0; i < tree.Childs.size(); i++)
        {
            switch (tree.Childs.get(i).Type)
            {
                case StatementType.YdweMacro:
                case StatementType.Comm:
                    comm.append(tree.Childs.get(i).Start.Text).append('\n');
                    continue;
                case StatementType.Name:
                    name = tree.Childs.get(i).Start.Text;
                    /** Возможно здесь вызов макроса @todo переписать на препроцессор макросов */
                    if (!isYdweCompatible) continue;
                    var macro = CheckYdweMacro(name);
                    if (null != macro) return macro.run(tree);
                    sb = new StringBuilder();
                    continue;
                default:
                    args.add(ConvertExprElem(tree.Childs.get(i)));
                    continue;
            }
        }
        return sb.append(name).append("(")
                              //.AppendJoin(", ", args).append(")");
                              .append(args.stream()
                                          .reduce(new StringBuilder(""),
                                                  (x,y)->x.append(", ")
                                                          .append(y))
                                          .delete(0, 2))
                              .append(")")
                              .append(comm);
    }

    /**
     * Преобразование объявления функции
     */
    StringBuilder ConvertFunc(Statement tree) throws Exception
    {
        var sb = new StringBuilder();

        boolean isNative = StatementType.CNative.equals(tree.Type) || StatementType.Native.equals(tree.Type);
        StringBuilder funcDecl = null;
        StringBuilder funcLocals = null;
        StringBuilder funcBody = null;
        for (var i = 0; i < tree.Childs.size(); i++)
        {
            switch (tree.Childs.get(i).Type)
            {
                case StatementType.YdweMacro:
                case StatementType.Comm:
                    sb.append(tree.Childs.get(i).Start.Text).append('\n');
                    continue;
                case StatementType.FuncDecl:
                    funcDecl = ConvertFuncDecl(tree.Childs.get(i));
                    continue;
                case StatementType.FuncLocals:
                    funcLocals = new StringBuilder()
                        .append("// local variables\n")
                        //.AppendJoin("", tree.Childs.Select(x => ConvertLocals(x, 1)));
                        .append(tree.Childs
                                    .get(i)
                                    .Childs
                                    .stream()
                                    .map(FuncThrows.RunOrThrow((x)->ConvertLocals(x, 1)))
                                    .reduce(new StringBuilder(), (x, y)->x.append(y))
                        );
                    continue;
                case StatementType.FuncBody:
                    funcBody = new StringBuilder()
                        .append("// function body\n")
                        //.AppendJoin("", tree.Childs.get(i).Childs.Select(x => ConvertStatement(x, 1)));
                        .append(tree.Childs
                                    .get(i)
                                    .Childs
                                    .stream()
                                    .map(FuncThrows.RunOrThrow((x)->ConvertStatement(x, 1)))
                                    .reduce(new StringBuilder(), (x, y)->x.append(y))
                        );
                    continue;
                default:
                    // TODO error
                    JassTranslatorException.Error(String.format("unknown statement %s", tree.Childs.get(i).Start));
            }
        }
        if (IsDTS || isNative) sb.append("declare ");
        sb.append(funcDecl);
        if (!IsDTS && !isNative)
        {
            if (null != funcLocals || null != funcBody)
            {
                sb.append(" {\n");
                if (null != funcLocals) sb.append(funcLocals);
                if (null != funcBody) sb.append(funcBody);
                sb.append("}");
            }
            else
                sb.append(" { }");
        }
        else sb.append(";");
        return sb.append("\n");
    }

    /**
     * Преобразование заголовка функции
     */
    StringBuilder ConvertFuncDecl(Statement tree) throws Exception
    {
        var sb = new StringBuilder();

        var name = "";
        var comm = new StringBuilder();
        var returnType = "";
        StringBuilder args = null;
        var isConst = false;

        for (var i = 0; i < tree.Childs.size(); i++)
        {
            switch (tree.Childs.get(i).Type)
            {
                case StatementType.YdweMacro:
                case StatementType.Comm:
                    comm.append(tree.Childs.get(i).Start.Text).append('\n');
                    continue;
                case StatementType.Name:
                    name = tree.Childs.get(i).Start.Text;
                    continue;
                case StatementType.Params:
                    args = new StringBuilder()
                        //.AppendJoin(", ", tree.Childs.get(i).Childs.Select(x => ConvertFuncParam(x)));
                        .append(tree.Childs
                                    .get(i)
                                    .Childs
                                    .stream()
                                    .map(FuncThrows.RunOrThrow((x)->ConvertFuncParam(x)))
                                    .reduce(new StringBuilder(""), 
                                            (x,y)->x.append(", ")
                                                    .append(y)))
                                    .delete(0, 2);
                    continue;
                case StatementType.Result:
                    returnType = ConvertType(tree.Childs.get(i).Start.Text);
                    continue;
                default:
                    // TODO error
                    JassTranslatorException.Error(String.format("unknown statement %s", tree.Childs.get(i).Start));
            }
        }

        sb.append("function ")
          .append(name)
          .append(" (");
        if (null != args)
            sb.append(args);
        sb.append("): ");
        if (isConst)
            sb.append("");
        sb.append(returnType)
          .append(comm);
        return sb;
    }

    /**
     * Преобразование параметра функции
     */
    StringBuilder ConvertFuncParam(Statement tree) throws Exception
    {
        var sb = new StringBuilder();

        var type = "";
        var name = "";
        var comm = new StringBuilder();
        for (var i = 0; i < tree.Childs.size(); i++)
        {
            switch (tree.Childs.get(i).Type)
            {
                case StatementType.YdweMacro:
                case StatementType.Comm:
                    comm.append(tree.Childs.get(i).Start.Text).append('\n');
                    continue;
                case StatementType.Type:
                    type = ConvertType(tree.Childs.get(i).Start.Text);
                    continue;
                case StatementType.Name:
                    name = tree.Childs.get(i).Start.Text;
                    continue;
                default:
                    // TODO error
                    JassTranslatorException.Error(String.format("unknown statement %s", tree.Childs.get(i).Start));
            }
        }

        sb.append(name)
          .append(": ")
          .append(type)
          .append(comm);

        return sb;
    }

    /**
     * Преобразование инструкции
     */
    StringBuilder ConvertStatement(Statement stat) throws Exception {
        return ConvertStatement(stat, 0);
    }

    /**
     * Преобразование инструкции
     */
    StringBuilder ConvertStatement(Statement stat, int indent) throws Exception
    {
        var sb = new StringBuilder();
        switch (stat.Type)
        {
            case StatementType.YdweMacro:
            case StatementType.Comm:
                return AddIndent(sb, indent).append(stat.Start.Text).append('\n');
            case StatementType.Debug:
                return AddIndent(sb, indent).append("// ").append(ConvertExpr(stat)).append(";\n");
            case StatementType.Set:
            case StatementType.ASet:
                return ConvertSetStatement(stat, indent);
            case StatementType.FCall:
                var s = ConvertExprElem(stat).append(";\n");
                if (indent > 0) s = sb.append(indent == 0 ? "" : " ".repeat(indent * IndentSize)).append(s);
                return s;
            case StatementType.If:
                return ConvertIfStatement(stat, indent);
            case StatementType.Cond:
            case StatementType.Then:
            case StatementType.ElseCond:
            case StatementType.Else:
                throw new NotImplementedException();
            case StatementType.Loop:
                // AddIndent(sb, indent).Append("while (true) {\n").AppendJoin("", stat.Childs.Select(x => ConvertStatement(x, indent + 1)));
                AddIndent(sb, indent).append("while (true) {\n")
                                     .append(stat.Childs
                                                 .stream()
                                                 .map(FuncThrows.RunOrThrow((x) -> ConvertStatement(x, indent + 1)))
                                                 .reduce(new StringBuilder(""),
                                                         (x, y) -> x.append(y))
                                            );
                AddIndent(sb, indent).append("}\n");
                return sb;
            case StatementType.Exit:
                return AddIndent(sb, indent).append("if (").append(ConvertExpr(stat)).append(") break;\n");
            case StatementType.Return:
                return AddIndent(sb, indent).append("return ").append(ConvertExpr(stat)).append(";\n");
            default:
                JassTranslatorException.Error(String.format("unknown statement %s", stat.Start));
                return new StringBuilder(String.format("/* unknown statement %s */", stat.Start));
        }
    }

    /**
     * Преобразование присваивания
     */
    StringBuilder ConvertSetStatement(Statement tree) throws Exception {
        return ConvertSetStatement(tree, 0);
    }

    /**
     * Преобразование присваивания
     */
    StringBuilder ConvertSetStatement(Statement tree, int indent) throws Exception
    {
        var sb = AddIndent(new StringBuilder(), indent);

        var name = "";
        var comm = new StringBuilder();
        var isArray = StatementType.ASet.equals(tree.Type);
        StringBuilder index = null;
        StringBuilder newValue = null;
        for (var i = 0; i < tree.Childs.size(); i++)
        {
            switch (tree.Childs.get(i).Type)
            {
                case StatementType.YdweMacro:
                case StatementType.Comm:
                    comm.append(tree.Childs.get(i).Start.Text).append('\n');
                    continue;
                case StatementType.Name:
                    name = tree.Childs.get(i).Start.Text;
                    continue;
                case StatementType.Ind:
                    index = ConvertExpr(tree.Childs.get(i));
                    continue;
                case StatementType.Expr:
                case StatementType.RVar:
                case StatementType.FCall:
                case StatementType.RArr:
                case StatementType.RFunc:
                case StatementType.Val:
                case StatementType.Par:
                    newValue = ConvertExprElem(tree.Childs.get(i));
                    continue;
                default:
                    // TODO error
                    JassTranslatorException.Error(String.format("unknown statement %s", tree.Childs.get(i).Start));
            }
        }

        sb.append(name);
        if (isArray) sb.append("[").append(index).append("]");
        sb.append(" = ")
          .append(newValue)
          .append(";\n")
          .append(comm);
        return sb;
    }

    /**
     * Преобразование if then elseif else
     */
    StringBuilder ConvertIfStatement(Statement tree) throws Exception {
        return ConvertIfStatement(tree, 0);
    }

    /**
     * Преобразование if then elseif else
     */
    StringBuilder ConvertIfStatement(Statement tree, int indent) throws Exception
    {
        var sb = AddIndent(new StringBuilder(), indent);

        for (var i = 0; i < tree.Childs.size(); i++)
        {
            switch (tree.Childs.get(i).Type)
            {
                case StatementType.YdweMacro:
                case StatementType.Comm:
                    AddIndent(sb, indent).append(tree.Childs.get(i).Start.Text).append('\n');
                    continue;
                case StatementType.ElseCond:
                    AddIndent(sb, indent).append("else ");
                case StatementType.Cond:
                    sb.append("if (").append(ConvertExpr(tree.Childs.get(i))).append(")\n");
                    continue;
                    case StatementType.Else:
                    if (isOptimizationNeeded && 0 == tree.Childs.get(i).Childs.size())
                        continue;
                    AddIndent(sb, indent).append("else\n");
                case StatementType.Then:
                    if (isOptimizationNeeded && 1 == tree.Childs.get(i).Childs.size()) { 
                        //sb.AppendJoin("", tree.Childs.get(i).Childs.Select(x => ConvertStatement(x, indent + 1)));
                        sb.append(tree.Childs
                                      .get(i)
                                      .Childs
                                      .stream()
                                      .map(FuncThrows.RunOrThrow((x)->ConvertStatement(x, indent + 1)))
                                      .reduce(new StringBuilder(""),
                                              (x,y)->x.append(y))
                                 );          
                        continue;
                    }
                    AddIndent(sb, indent).append("{\n");
                    //sb.AppendJoin("", tree.Childs.get(i).Childs.Select(x => ConvertStatement(x, indent + 1)));
                    sb.append(tree.Childs
                                  .get(i)
                                  .Childs
                                  .stream()
                                  .map(FuncThrows.RunOrThrow((x)->ConvertStatement(x, indent + 1)))
                                  .reduce(new StringBuilder(""),
                                          (x,y)->x.append(y))
                             );          
                    AddIndent(sb, indent).append("}\n");
                    continue;
                default:
                    // TODO error
                    JassTranslatorException.Error(String.format("unknown statement %s", tree.Childs.get(i).Start));
            }
        }

        return sb;
    }

    /**
     * выравнивание кода
     * @param sb куда писать
     * @param indent величина сдвига (&lt;кол-во символов&gt; = {@code indent} * {@link JassToTs#IndentSize})
     * @return {@code sb} с добавленным сдвигом
     */
    StringBuilder AddIndent(StringBuilder sb, int indent) { 
        return indent > 0 && IndentSize > 0 ? 
               sb.append(indent == 0 ? "" : " ".repeat(indent * IndentSize)) : 
               sb;
    }
}
