﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jass;

namespace JassToTs
{
    class JassToTs
    {
        bool IsDTS;

        public JassToTs(bool IsDTS = false)
        {
            this.IsDTS = IsDTS;
        }

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
                        .Append("// Program\n")
                        .AppendJoin("", stat.Childs.Select(x => ConvertDeclarations(x)));
                case StatementType.TypeDecl:
                    return ConvertTypeDecl(stat);
                case StatementType.Glob:
                    return new StringBuilder()
                        .Append("// global var declaration\n")
                        .AppendJoin("", stat.Childs.Select(x => ConvertVarDecl(x)))
                        .Append("// end global var declaration\n");
                case StatementType.Native:
                case StatementType.CNative:
                case StatementType.Func:
                case StatementType.CFunc:
                    return ConvertFunc(stat);
                case StatementType.GConst:
                case StatementType.GVar:
                case StatementType.GArr:
                case StatementType.LVar:
                case StatementType.LArr:
                    //return ConvertVarDecl(stat);
                case StatementType.Val:
                case StatementType.RVar:
                case StatementType.RFunc:
                case StatementType.RArr:
                case StatementType.FCall:
                case StatementType.Expr:
                case StatementType.Par:
                    //return ConvertExprElem(stat).Append("\n");
                case StatementType.FuncDecl:
                case StatementType.Params: 
                case StatementType.FuncLocals: 
                case StatementType.FuncBody:
                    //
                case StatementType.Debug:
                case StatementType.Set:
                case StatementType.ASet:
                case StatementType.If:
                case StatementType.Cond:
                case StatementType.Then:
                case StatementType.ElseCond:
                case StatementType.Else:
                case StatementType.Loop:
                case StatementType.Exit:
                case StatementType.Return:
                    throw new NotImplementedException();
                default: throw new Exception($"unknown statement {stat.Start}");
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
                case "integer":
                case "real": return "number";
                case "boolean": return "bool";
                case "string": return "string";
                case "handle": return "handle"; // TODO: проверить
                case "code": return "Function"; // TODO: проверить
                case "nothing": return "void";
                default: return type;
            }
        }

        /// <summary> Преобразование комметариев </summary>
        StringBuilder TryConvertComment(Statement tree) =>
            StatementType.Comm == tree.Type ?
                new StringBuilder()
                    .Append(tree.Start.Text)
                    .Append('\n') :
                null;

        /// <summary> Преобразование объявления типа </summary>
        StringBuilder ConvertTypeDecl(Statement tree)
        {
            var sb = new StringBuilder();
            var typeName = "";
            var baseType = "";
            for (var i = 0; i < tree.Childs.Count; i++)
            {
                switch (tree.Childs[i].Type)
                {
                    case StatementType.Comm:
                        sb.Append(tree.Childs[i].Start.Text).Append('\n');
                        continue;
                    case StatementType.TypeName:
                        typeName = tree.Childs[i].Start.Text;
                        continue;
                    case StatementType.BaseType:
                        baseType = tree.Childs[i].Start.Text;
                        continue;
                    default:
                        throw new Exception($"unknown statement {tree.Childs[i].Start}");
                }
            }
            if (IsDTS) sb.Append("declare ");
            return sb.Append("class ")
                     .Append(typeName)
                     .Append(" extends ")
                     .Append(baseType)
                     .Append(" {}\n");
        }

        /// <summary> Преобразование объявления переменной </summary>
        StringBuilder ConvertVarDecl(Statement tree)
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
            var isArr = StatementType.GArr == tree.Type || StatementType.LArr == tree.Type;
            var type = "";
            var name = "";
            StringBuilder expr = null;
            for (var i = 0; i < tree.Childs.Count; i++)
            {
                switch(tree.Childs[i].Type)
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
                    case StatementType.Expr:
                    case StatementType.RVar:
                    case StatementType.FCall:
                    case StatementType.RArr:
                    case StatementType.RFunc:
                    case StatementType.Val:
                    case StatementType.Par:
                        expr = ConvertExprElem (tree.Childs[i]);
                        continue;
                    default:
                        throw new Exception($"unknown statement {tree.Childs[i].Start}");
                }
            }
            if (isDeclare) sb.Append("declare ");
            sb.Append(typeDecl)
              .Append(name)
              .Append(": ")
              .Append(type);
            if (isArr) sb.Append("[]");
            if (null != expr)
                sb.Append(" = ")
                  .Append(expr);
            return sb.Append(";\n");
        }

        /// <summary> Преобразование выражения </summary>
        StringBuilder ConvertExpr(Statement tree)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < tree.Childs.Count; i++)
                sb.Append(ConvertExprElem(tree.Childs[i]));
            return sb;
        }

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
                case StatementType.Val:
                case StatementType.Oper:
                    return sb.Append(elem.Start.Text);
                case StatementType.RArr:
                    return sb.Append("/* Array reference */");
                case StatementType.FCall:
                    return sb.Append(ConvertFuncCall(elem));
                case StatementType.Ind:
                    return sb.Append("[").Append(ConvertExpr(elem)).Append("]");
                case StatementType.Par:
                    return sb.Append("(").Append(ConvertExpr(elem)).Append(")");
                case StatementType.Expr:
                    return ConvertExpr(elem);
                default:
                    throw new Exception($"unknown statement {elem.Start}");
            }
        }

        /// <summary> Преобразование вызова функции </summary>
        StringBuilder ConvertFuncCall(Statement tree)
        {
            var sb = new StringBuilder();
            var name = "";
            var args = new List<StringBuilder>();
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
            return sb.Append(name)
                     .Append("(")
                     .AppendJoin(", ", args)
                     .Append(")");
        }

        /// <summary> Преобразование объявления функции </summary>
        StringBuilder ConvertFunc(Statement tree)
        {
            var sb = new StringBuilder();

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
                            .AppendJoin("", tree.Childs[i].Childs.Select(x => ConvertVarDecl(x)));
                        continue;
                    case StatementType.FuncBody:
                        funcBody = new StringBuilder()
                            .Append("// function body\n")
                            .AppendJoin("", tree.Childs[i].Childs.Select(x => ConvertStatement(x)));
                        continue;
                    default:
                        throw new Exception($"unknown statement {tree.Childs[i].Start}");
                }
            }
            sb.Append(funcDecl);
            if ((null != funcLocals || null != funcBody) && !IsDTS)
            {
                sb.Append(" {\n");
                if (null != funcLocals) sb.Append(funcLocals);
                if (null != funcBody) sb.Append(funcBody);
                sb.Append("}");
            }
            else if (IsDTS) sb.Append(" { }");
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
                        throw new Exception($"unknown statement {tree.Childs[i].Start}");
                }
            }

            if (IsDTS) sb.Append("declare ");
            sb.Append("function ")
              .Append(name)
              .Append(" (");
            if (null != args)
                sb.Append(args);
            sb.Append("): ");
            if (isConst)
                sb.Append("");
            sb.Append(returnType);
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
                        throw new Exception($"unknown statement {tree.Childs[i].Start}");
                }
            }

            sb.Append(name).Append(": ").Append(type);

            return sb;
        }

        /// <summary> Преобразование инструкции </summary>
        StringBuilder ConvertStatement(Statement stat)
        {
            var sb = new StringBuilder();
            switch (stat.Type)
            {
                case StatementType.Comm:
                    return sb.Append(stat.Start.Text).Append('\n');
                case StatementType.Debug:
                    return sb.Append("// ").Append(ConvertExpr(stat)).Append(";\n");
                case StatementType.Set:
                case StatementType.ASet:
                    return ConvertSetStatement(stat);
                case StatementType.FCall:
                    return ConvertExprElem(stat).Append(";\n");
                case StatementType.If:
                    return ConvertIfStatement(stat);
                case StatementType.Cond:
                case StatementType.Then:
                case StatementType.ElseCond:
                case StatementType.Else:
                    throw new NotImplementedException();
                case StatementType.Loop:
                    return sb.Append("while (true) {\n").AppendJoin("", stat.Childs.Select(x=>ConvertStatement(x))).Append("}\n");
                case StatementType.Exit:
                    return sb.Append("if (").Append(ConvertExpr(stat)).Append(") break;\n");
                case StatementType.Return:
                    return sb.Append("return ").Append(ConvertExpr(stat)).Append(";\n");
                default:
                    throw new Exception($"unknown statement {stat.Start}");
            }
        }

        /// <summary> Преобразование присваивания </summary>
        StringBuilder ConvertSetStatement(Statement tree)
        {
            var sb = new StringBuilder();

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
                        throw new Exception($"unknown statement {tree.Childs[i].Start}");
                }
            }

            sb.Append(name);
            if (isArray) sb.Append("[").Append(index).Append("]");
            sb.Append(" = ").Append(newValue).Append(";\n");
            return sb;
        }

        /// <summary> Преобразование if then elseif else </summary>
        StringBuilder ConvertIfStatement(Statement tree)
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
                    case StatementType.Cond:
                        sb.Append("if (").Append(ConvertExpr(tree.Childs[i])).Append(")\n");
                        continue;
                    case StatementType.ElseCond:
                        sb.Append("else ");
                        goto case StatementType.Cond;
                    case StatementType.Then:
                        sb.Append("{\n").AppendJoin("", tree.Childs[i].Childs.Select(x => ConvertStatement(x))).Append("}\n");
                        continue;
                    case StatementType.Else:
                        sb.Append("else\n");
                        goto case StatementType.Then;
                    default:
                        throw new Exception($"unknown statement {tree.Childs[i].Start}");
                }
            }

            sb.Append(name).Append(": ").Append(type);

            return sb;
        }
    }
}
