using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jass;

namespace JassToTs
{
    class JassToTs
    {
        public string Convert(Statement tree)
        {
            var sb = ConvertStatement(tree);
            return sb.ToString();
        }

        /// <summary> Преобразовать выражение </summary>
        /// <param name="stat"> выражение </param>
        StringBuilder ConvertStatement(Statement stat)
        {
            switch (stat.Type)
            {
                case StatementType.Prog:
                    return new StringBuilder()
                        .Append("// Program\n")
                        .AppendJoin("", stat.Childs.Select(x => ConvertStatement(x)));
                case StatementType.TypeDecl:
                    return ConvertTypeDecl(stat);
                case StatementType.Glob:
                    return new StringBuilder()
                        .Append("// global var declaration\n")
                        .AppendJoin("", stat.Childs.Select(x => ConvertStatement(x)))
                        .Append("// end global var declaration\n");
                case StatementType.GConst:
                case StatementType.GVar:
                case StatementType.GArr:
                case StatementType.LVar:
                case StatementType.LArr:
                    return ConvertVarDecl(stat);
                case StatementType.Val:
                case StatementType.RVar:
                case StatementType.RFunc:
                case StatementType.RArr:
                case StatementType.FCall:
                case StatementType.Expr:
                case StatementType.Par:
                    return ConvertExprElem(stat).Append("\n");
                case StatementType.Native:
                case StatementType.CNative:
                case StatementType.FuncDecl:
                    return ConvertFunc(stat);
                case StatementType.Params: break;
                case StatementType.Func: break;
                case StatementType.CFunc: break;
                case StatementType.FuncLocals: break;
                case StatementType.FuncBody: break;
                case StatementType.Comm:
                    return new StringBuilder().Append(stat.Start.Text).Append("\n");
                case StatementType.Debug: break;
                case StatementType.Set: break;
                case StatementType.ASet: break;
                case StatementType.If: break;
                case StatementType.Cond: break;
                case StatementType.Then: break;
                case StatementType.ElseCond: break;
                case StatementType.Else: break;
                case StatementType.Loop: break;
                case StatementType.Exit: break;
                case StatementType.Return: break;
                default: break;
            }
            return null;
        }

        StringBuilder TryConvertComment(Statement tree) =>
            StatementType.Comm == tree.Type ?
                new StringBuilder()
                    .Append(tree.Start.Text)
                    .Append('\n') :
                null;

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
            return sb.Append("declare class ")
                     .Append(typeName)
                     .Append(" extends ")
                     .Append(baseType)
                     .Append(" {}\n");
        }

        StringBuilder ConvertVarDecl(Statement tree)
        {
            var sb = new StringBuilder();
            var typeDecl = "";
            switch (tree.Type)
            {
                case StatementType.GConst:
                    typeDecl = "const ";
                    break;
                case StatementType.GVar:
                case StatementType.GArr:
                    typeDecl = "var ";
                    break;
                case StatementType.LVar:
                case StatementType.LArr:
                    typeDecl = "let ";
                    break;
            }
            var isArr = StatementType.GArr == tree.Type || StatementType.LVar == tree.Type;
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
                        type = tree.Childs[i].Start.Text;
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
            sb = sb.Append("declare ")
                   .Append(typeDecl)
                   .Append(name)
                   .Append(": ")
                   .Append(type);
            if (isArr) sb.Append("[]");
            if (null != expr)
                sb.Append(" = ")
                  .Append(expr);
            return sb.Append(";\n");
        }

        StringBuilder ConvertExpr(Statement tree)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < tree.Childs.Count; i++)
                sb.Append(ConvertExprElem(tree.Childs[i]));
            return sb;
        }

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

        StringBuilder ConvertFunc(Statement tree)
        {
            var sb = new StringBuilder();

            StringBuilder funcDecl = null;
            StringBuilder funcBody = null;
            for (var i = 0; i < tree.Childs.Count; i++)
            {
                switch (tree.Childs[i].Type)
                {
                    case StatementType.FuncDecl:
                        funcDecl = ConvertFuncDecl(tree.Childs[i]);
                        continue;
                    case StatementType.FuncBody:
                        funcBody = ConvertFuncDecl(tree.Childs[i]);
                        continue;
                    default:
                        throw new Exception($"unknown statement {tree.Childs[i].Start}");
                }
            }
            sb.Append(funcDecl);
            if (null != funcBody)
                sb.Append(" {\n")
                  .Append(funcBody)
                  .Append("}");
            else
                sb.Append(";");
            return sb.Append("\n");
        }

        StringBuilder ConvertFuncDecl(Statement tree)
        {
            var sb = new StringBuilder();

            var name = "";
            var returnType = "";
            var args = new List<StringBuilder>();
            var isConst = false;

            for (var i = 0; i < tree.Childs.Count; i++)
            {
                switch (tree.Childs[i].Type)
                {
                    case StatementType.Name:
                        continue;
                    case StatementType.Params:
                        continue;
                    case StatementType.Result:
                        continue;
                    default:
                        throw new Exception($"unknown statement {tree.Childs[i].Start}");
                }
            }

            sb.Append("declare function ")
              .Append(name)
              .Append(" (");
            if (args.Count > 0)
                sb.AppendJoin(", ", args);
            sb.Append("): ");
            if (isConst)
                sb.Append("");
            sb.Append(returnType);
            return sb;
        }
    }
}
