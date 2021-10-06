using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jass;

namespace JassToTs
{
    class JassToGalaxyRaw
    {
        /// <summary> Количество символов в 1 сдвиге </summary>
        int IndentSize;
        bool isOptimizationNeeded;

        public JassToGalaxyRaw(bool isOptimizationNeeded, int IndentSize = 4)
        {
            this.isOptimizationNeeded = isOptimizationNeeded;
            this.IndentSize = IndentSize;
        }

        /// <summary> Транслировать дерево в TypeScript код </summary>
        public string Convert(Statement tree)
        {
            var sb = ConvertDeclarations(tree);
            return sb.ToString();
        }

        /// <summary> Преобразовать объявления </summary>
        /// <param name="stat"> выражение </param>
        StringBuilder ConvertDeclarations(Statement stat)
        {
            switch (stat.Type)
            {
                case StatementType.Comm:
                    return new StringBuilder().Append(stat.Start.Text).Append("\n");
                case StatementType.Prog:
                    return new StringBuilder()
                        //.Append("/// Some references\n")
                        //.Append("/// <reference path=\"war3core.d.ts\"/>\n")
                        .AppendJoin("", stat.Childs.Select(x => ConvertDeclarations(x)));
                case StatementType.TypeDecl:
                    return ConvertTypeDecl(stat);
                case StatementType.Glob:
                    return new StringBuilder()
                        .Append("//--------------------------------------------------------------------------------------------------")
                        .Append("// Global Variables")
                        .Append("//--------------------------------------------------------------------------------------------------")
                        .AppendJoin("", stat.Childs.Select(x => ConvertGlobals(x)))
                        .Append("// end global var declaration\n");
                case StatementType.Native:
                case StatementType.CNative:
                case StatementType.Func:
                case StatementType.CFunc:
                    return ConvertFunc(stat);
                default: throw new JassTranslatorException($"unknown statement {stat.Start}");
            }
        }

        /// <summary> Преобразовать базовый тип </summary>
        /// <param name="type"> базовый JASS тип </param>
        /// <returns> Тип в TypeScript </returns>
        string ConvertType(string type)
        {
            switch (type)
            {
                // Базовые типы
                case "integer": return "int";
                case "real": return "fixed";
                case "boolean": return "bool";
                case "string": return "string";
                case "handle": return "int"; // TODO: проверить
                case "code": return "string"; // TODO: проверить
                case "nothing": return "void";
                default: return type;
            }
        }

        /// <summary> Преобразовать оператор </summary>
        /// <param name="type"> оператор JASS </param>
        /// <returns> оператор TypeScript </returns>
        string ConvertOperator(string type)
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
                    throw new JassTranslatorException("unknown operator");
            }
        }

        /// <summary> Преобразование объявления типа </summary>
        StringBuilder ConvertTypeDecl(Statement tree)
        {
            var sb = new StringBuilder();
            var typeName = "";
            var baseType = "";
            var comm = new StringBuilder();
            for (var i = 0; i < tree.Childs.Count; i++)
            {
                switch (tree.Childs[i].Type)
                {
                    case StatementType.Comm:
                        comm.Append(tree.Childs[i].Start.Text).Append('\n');
                        continue;
                    case StatementType.TypeName:
                        typeName = tree.Childs[i].Start.Text;
                        continue;
                    case StatementType.BaseType:
                        baseType = tree.Childs[i].Start.Text;
                        continue;
                    default:
                        throw new JassTranslatorException($"unknown statement {tree.Childs[i].Start}");
                }
            }
            return sb.Append("struct ").Append(typeName).Append(" /* extends ").Append(baseType).Append(" */ { }\n")
                     .Append(comm);
        }

        /// <summary> Преобразование глобальной переменной </summary>
        StringBuilder ConvertGlobals(Statement stat)
        {
            switch (stat.Type)
            {
                case StatementType.Comm:
                    return new StringBuilder().Append(stat.Start.Text).Append("\n");
                case StatementType.GConst:
                case StatementType.GVar:
                case StatementType.GArr:
                    return ConvertVarDecl(stat);
                default: throw new JassTranslatorException($"unknown statement {stat.Start}");
            }
        }

        /// <summary> Преобразование локальной переменной </summary>
        StringBuilder ConvertLocals(Statement stat, int indent = 0)
        {
            switch (stat.Type)
            {
                case StatementType.Comm:
                    return AddIndent(new StringBuilder(), indent).Append(stat.Start.Text).Append("\n");
                case StatementType.LVar:
                case StatementType.LArr:
                    return ConvertVarDecl(stat, indent);
                default: throw new JassTranslatorException($"unknown statement {stat.Start}");
            }
        }

        /// <summary> Преобразование объявления переменной </summary>
        StringBuilder ConvertVarDecl(Statement tree, int indent = 0)
        {
            var sb = new StringBuilder();
            var typeDecl = "";
            //var isDeclare = false;
            switch (tree.Type)
            {
                case StatementType.GConst:
                    typeDecl = "const ";
                    break;
                case StatementType.GVar:
                case StatementType.GArr:
                    //typeDecl = "var ";
                    break;
                case StatementType.LVar:
                case StatementType.LArr:
                    //typeDecl = "let ";
                    break;
            }
            var isArr = StatementType.GArr == tree.Type || StatementType.LArr == tree.Type;
            var type = "";
            var name = "";
            var comm = new StringBuilder();
            StringBuilder expr = null;
            for (var i = 0; i < tree.Childs.Count; i++)
            {
                switch (tree.Childs[i].Type)
                {
                    case StatementType.Comm:
                        comm.Append(tree.Childs[i].Start.Text).Append('\n');
                        continue;
                    case StatementType.Type:
                        type = ConvertType(tree.Childs[i].Start.Text);
                        continue;
                    case StatementType.Name:
                        name = tree.Childs[i].Start.Text;
                        continue;
                    case StatementType.Expr:
                    case StatementType.RVar:
                    case StatementType.FCall:
                    case StatementType.RArr:
                    case StatementType.RFunc:
                    case StatementType.Val:
                    case StatementType.Par:
                        expr = ConvertExprElem(tree.Childs[i]);
                        continue;
                    default:
                        throw new JassTranslatorException($"unknown statement {tree.Childs[i].Start}");
                }
            }
            AddIndent(sb, indent);
            //if (isDeclare) sb.Append("declare ");
            sb.Append(typeDecl).Append(type);
            if (isArr) sb.Append("[]");
            sb.Append(" ").Append(name);
            if (null != expr)
                sb.Append(" = ").Append(expr);
            return sb.Append(";\n")
                     .Append(comm);
        }

        /// <summary> Преобразование выражения </summary>
        StringBuilder ConvertExpr(Statement tree) =>
            new StringBuilder().AppendJoin(" ", tree.Childs.Select(x => ConvertExprElem(x)));

        /// <summary> Преобразование составной части выражения 
        /// (идентификатор, константа, оператор и т.п.) </summary>
        StringBuilder ConvertExprElem(Statement elem)
        {
            var sb = new StringBuilder();
            switch (elem.Type)
            {
                case StatementType.Comm:
                    return sb.Append(elem.Start.Text).Append('\n');
                case StatementType.RVar:
                case StatementType.RFunc:
                    return sb.Append(elem.Start.Text);
                case StatementType.Val:
                    return sb.Append(ConvertValue(elem));
                case StatementType.Oper:
                    return sb.Append(ConvertOperator(elem.Start.Text));
                case StatementType.RArr:
                    return sb.Append(ConvertArrayRef(elem));
                case StatementType.FCall:
                    return sb.Append(ConvertFuncCall(elem));
                case StatementType.Ind:
                    return sb.Append("[").Append(ConvertExpr(elem)).Append("]");
                case StatementType.Par:
                    {
                        var childExpr = ConvertExpr(elem);
                        if (isOptimizationNeeded) { 
                            while('(' == childExpr[0] && childExpr[childExpr.Length - 1] == ')')
                                childExpr = childExpr.Remove(0, 1).Remove(childExpr.Length - 1, 1);
                        }
                        return sb.Append("(").Append(childExpr).Append(")");
                    }
                case StatementType.Expr:
                    return ConvertExpr(elem);
                default:
                    throw new JassTranslatorException($"unknown statement {elem.Start}");
            }
        }

        /// <summary> Преобразовать безымянную константу </summary>
        StringBuilder ConvertValue(Statement stat)
        {
            var sb = new StringBuilder();
            var val = stat.Start.Text;
            switch (stat.Start.Kind) {
                case TokenKind.adec:
                    var number = 0;
                    for (var i = 1; i < val.Length -1; i++)
                    {
                        number = number << 8;
                        number += val[i];
                    }
                    sb.Append(number.ToString()).Append(" /*").Append(val).Append("*/");
                    break;
                //case TokenKind.bin:
                case TokenKind.oct:
                    if ("0" == val) goto case TokenKind.ndec;
                    sb.Append("0o").Append(val.Substring(1));
                    break;
                case TokenKind.dhex:
                    sb.Append("0x").Append(val.Substring(1));
                    break;
                case TokenKind.ndec:
                case TokenKind.xhex:
                case TokenKind.real:
                case TokenKind.@bool:
                case TokenKind.@null:
                case TokenKind.dstr:
                    sb.Append(val);
                    break;
                default:
                    break;
            }
            return sb;
        }

        /// <summary> Преобразовать ссылку на элемент массива </summary>
        StringBuilder ConvertArrayRef(Statement stat)
        {
            var sb = new StringBuilder();

            var name = "";
            StringBuilder index = null;
            var comm = new StringBuilder();
            for (var i = 0; i < stat.Childs.Count; i++)
            {
                switch (stat.Childs[i].Type)
                {
                    case StatementType.Comm:
                        comm.Append(stat.Childs[i].Start.Text).Append('\n');
                        continue;
                    case StatementType.Name:
                        name = stat.Childs[i].Start.Text;
                        continue;
                    case StatementType.Ind:
                        index = ConvertExprElem(stat.Childs[i]);
                        continue;
                    default:
                        throw new JassTranslatorException($"unknown statement {stat.Childs[i].Start}");
                }
            }

            sb.Append(name).Append(index).Append(comm);

            return sb;
        }

        /// <summary> Преобразование вызова функции </summary>
        StringBuilder ConvertFuncCall(Statement tree)
        {
            var sb = new StringBuilder();
            var name = "";
            var args = new List<StringBuilder>();
            var comm = new StringBuilder();
            for (var i = 0; i < tree.Childs.Count; i++)
            {
                switch (tree.Childs[i].Type)
                {
                    case StatementType.Comm:
                        return sb.Append(tree.Childs[i]).Append('\n');
                    case StatementType.Name:
                        name = tree.Childs[i].Start.Text;
                        continue;
                    default:
                        args.Add(ConvertExprElem(tree.Childs[i]));
                        continue;
                }
            }
            return sb.Append(name).Append("(").AppendJoin(", ", args).Append(")");
        }

        /// <summary> Преобразование объявления функции </summary>
        StringBuilder ConvertFunc(Statement tree)
        {
            var sb = new StringBuilder();

            bool isNative = StatementType.CNative == tree.Type || StatementType.Native == tree.Type;
            StringBuilder funcDecl = null;
            StringBuilder funcLocals = null;
            StringBuilder funcBody = null;
            for (var i = 0; i < tree.Childs.Count; i++)
            {
                switch (tree.Childs[i].Type)
                {
                    case StatementType.Comm:
                        sb.Append(tree.Childs[i].Start.Text).Append('\n');
                        continue;
                    case StatementType.FuncDecl:
                        funcDecl = ConvertFuncDecl(tree.Childs[i]);
                        continue;
                    case StatementType.FuncLocals:
                        funcLocals = new StringBuilder()
                            .Append("// local variables\n")
                            .AppendJoin("", tree.Childs[i].Childs.Select(x => ConvertLocals(x, 1)));
                        continue;
                    case StatementType.FuncBody:
                        funcBody = new StringBuilder()
                            .Append("// function body\n")
                            .AppendJoin("", tree.Childs[i].Childs.Select(x => ConvertStatement(x, 1)));
                        continue;
                    default:
                        throw new JassTranslatorException($"unknown statement {tree.Childs[i].Start}");
                }
            }
            if (isNative) sb.Append("declare ");
            sb.Append(funcDecl);
            if (!isNative)
            {
                if (null != funcLocals || null != funcBody)
                {
                    sb.Append(" {\n");
                    if (null != funcLocals) sb.Append(funcLocals);
                    if (null != funcBody) sb.Append(funcBody);
                    sb.Append("}");
                }
                else
                    sb.Append(" { }");
            }
            else sb.Append(";");
            return sb.Append("\n");
        }

        /// <summary> Преобразование заголовка функции </summary>
        StringBuilder ConvertFuncDecl(Statement tree)
        {
            var sb = new StringBuilder();

            var name = "";
            var returnType = "";
            StringBuilder args = null;
            var isConst = false;

            for (var i = 0; i < tree.Childs.Count; i++)
            {
                switch (tree.Childs[i].Type)
                {
                    case StatementType.Comm:
                        sb.Append(tree.Childs[i].Start.Text).Append('\n');
                        continue;
                    case StatementType.Name:
                        name = tree.Childs[i].Start.Text;
                        continue;
                    case StatementType.Params:
                        args = new StringBuilder().AppendJoin(", ", tree.Childs[i].Childs.Select(x => ConvertFuncParam(x)));
                        continue;
                    case StatementType.Result:
                        returnType = ConvertType(tree.Childs[i].Start.Text);
                        continue;
                    default:
                        throw new JassTranslatorException($"unknown statement {tree.Childs[i].Start}");
                }
            }

            sb.Append(returnType)
              .Append(" ")
              .Append(name)
              .Append(" (");
            if (null != args)
                sb.Append(args);
            sb.Append(")");
            if (isConst)
                sb.Append("");
            return sb;
        }

        /// <summary> Преобразование параметра функции </summary>
        StringBuilder ConvertFuncParam(Statement tree)
        {
            var sb = new StringBuilder();

            var type = "";
            var name = "";
            for (var i = 0; i < tree.Childs.Count; i++)
            {
                switch (tree.Childs[i].Type)
                {
                    case StatementType.Comm:
                        sb.Append(tree.Childs[i].Start.Text).Append('\n');
                        continue;
                    case StatementType.Type:
                        type = ConvertType(tree.Childs[i].Start.Text);
                        continue;
                    case StatementType.Name:
                        name = tree.Childs[i].Start.Text;
                        continue;
                    default:
                        throw new JassTranslatorException($"unknown statement {tree.Childs[i].Start}");
                }
            }

            sb.Append(name).Append(": ").Append(type);

            return sb;
        }

        /// <summary> Преобразование инструкции </summary>
        StringBuilder ConvertStatement(Statement stat, int indent = 0)
        {
            var sb = new StringBuilder();
            switch (stat.Type)
            {
                case StatementType.Comm:
                    return AddIndent(sb, indent).Append(stat.Start.Text).Append('\n');
                case StatementType.Debug:
                    return AddIndent(sb, indent).Append("// ").Append(ConvertExpr(stat)).Append(";\n");
                case StatementType.Set:
                case StatementType.ASet:
                    return ConvertSetStatement(stat, indent);
                case StatementType.FCall:
                    var s = ConvertExprElem(stat).Append(";\n");
                    if (indent > 0) s = sb.Append(new string(' ', indent * IndentSize)).Append(s);
                    return s;
                case StatementType.If:
                    return ConvertIfStatement(stat, indent);
                case StatementType.Cond:
                case StatementType.Then:
                case StatementType.ElseCond:
                case StatementType.Else:
                    throw new NotImplementedException();
                case StatementType.Loop:
                    AddIndent(sb, indent).Append("while (true) {\n").AppendJoin("", stat.Childs.Select(x => ConvertStatement(x, indent + 1)));
                    AddIndent(sb, indent).Append("}\n");
                    return sb;
                case StatementType.Exit:
                    return AddIndent(sb, indent).Append("if (").Append(ConvertExpr(stat)).Append(") break;\n");
                case StatementType.Return:
                    return AddIndent(sb, indent).Append("return ").Append(ConvertExpr(stat)).Append(";\n");
                default:
                    throw new JassTranslatorException($"unknown statement {stat.Start}");
            }
        }

        /// <summary> Преобразование присваивания </summary>
        StringBuilder ConvertSetStatement(Statement tree, int indent = 0)
        {
            var sb = AddIndent(new StringBuilder(), indent);

            var name = "";
            var isArray = StatementType.ASet == tree.Type;
            StringBuilder index = null;
            StringBuilder newValue = null;
            for (var i = 0; i < tree.Childs.Count; i++)
            {
                switch (tree.Childs[i].Type)
                {
                    case StatementType.Comm:
                        sb.Append(tree.Childs[i].Start.Text).Append('\n');
                        continue;
                    case StatementType.Name:
                        name = tree.Childs[i].Start.Text;
                        continue;
                    case StatementType.Ind:
                        index = ConvertExpr(tree.Childs[i]);
                        continue;
                    case StatementType.Expr:
                    case StatementType.RVar:
                    case StatementType.FCall:
                    case StatementType.RArr:
                    case StatementType.RFunc:
                    case StatementType.Val:
                    case StatementType.Par:
                        newValue = ConvertExprElem(tree.Childs[i]);
                        continue;
                    default:
                        throw new JassTranslatorException($"unknown statement {tree.Childs[i].Start}");
                }
            }

            sb.Append(name);
            if (isArray) sb.Append("[").Append(index).Append("]");
            sb.Append(" = ").Append(newValue).Append(";\n");
            return sb;
        }

        /// <summary> Преобразование if then elseif else </summary>
        StringBuilder ConvertIfStatement(Statement tree, int indent = 0)
        {
            var sb = AddIndent(new StringBuilder(), indent);

            for (var i = 0; i < tree.Childs.Count; i++)
            {
                switch (tree.Childs[i].Type)
                {
                    case StatementType.Comm:
                        AddIndent(sb, indent).Append(tree.Childs[i].Start.Text).Append('\n');
                        continue;
                    case StatementType.Cond:
                        sb.Append("if (").Append(ConvertExpr(tree.Childs[i])).Append(")\n");
                        continue;
                    case StatementType.ElseCond:
                        AddIndent(sb, indent).Append("else ");
                        goto case StatementType.Cond;
                    case StatementType.Then:
                        AddIndent(sb, indent).Append("{\n");
                        sb.AppendJoin("", tree.Childs[i].Childs.Select(x => ConvertStatement(x, indent + 1)));
                        AddIndent(sb, indent).Append("}\n");
                        continue;
                    case StatementType.Else:
                        AddIndent(sb, indent).Append("else\n");
                        goto case StatementType.Then;
                    default:
                        throw new JassTranslatorException($"unknown statement {tree.Childs[i].Start}");
                }
            }

            return sb;
        }

        /// <summary> выравнивание кода </summary>
        /// <param name="sb"> куда писать </param>
        /// <param name="indent"> величина сдвига (&lt;кол-во символов&gt; = <paramref name="indent"/> * <see cref="IndentSize"/>)</param>
        /// <returns> <paramref name="sb"/> с добавленным сдвигом </returns>
        StringBuilder AddIndent(StringBuilder sb, int indent) => 
            indent > 0 && IndentSize > 0 ? 
                sb.Append(new string(' ', indent * IndentSize)) : 
                sb;
    }
}
