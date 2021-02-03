using alm.Core.Errors;
using alm.Core.InnerTypes;
using alm.Core.FrontEnd.SyntaxAnalysis.new_parser_concept.syntax_tree;
using alm.Core.Table2;

using alm.Other.Enums;

namespace alm.Core.FrontEnd.SemanticAnalysis.new_label_checker2
{
    public static class GlobalTable
    {
        public static Table Table { get; set; }
    }

    public sealed class LabelChecker2
    {
        private static bool IsMainDeclared;

        public static void ResolveModule(SyntaxTreeNode module)
        {
            IsMainDeclared = false;
            GlobalTable.Table = Table.CreateTable(null,1);
            foreach (MethodDeclaration method in module.GetChildsByType(typeof(MethodDeclaration),true))
                ResolveMethodDeclaration(method);

            if (!IsMainDeclared)
                Diagnostics.SemanticErrors.Add(new ErrorForDebug("В запускаемом файле должен быть метод main"));
        }

        public static void ResolveMainDeclaration(MethodDeclaration method)
        {
            if (method.Name == "main")
                if (method.ArgCount == 0 && method.ReturnType is Int32)
                    IsMainDeclared = true;
        }

        public static void ResolveMethodDeclaration(MethodDeclaration method)
        {
            Table MethodTable = Table.CreateTable(GlobalTable.Table);
            if (GlobalTable.Table.PushMethod(method))
            {
                ResolveMainDeclaration(method);
                ResolveMethodArguments(method.Arguments, MethodTable);
                if (!method.IsExternal)
                {
                    if (!(method.ReturnType is Void))
                        ResolveReturnInBody(method.Body);
                   ResolveEmbeddedStatement(method.Body, MethodTable);
                }
            }
        }
        public static void ResolveEmbeddedStatement(EmbeddedStatement body, Table table)
        {
            Table bodyTable = Table.CreateTable(table);
            foreach (SyntaxTreeNode statement in body.Childs)
                ResolveStatement((Statement)statement, bodyTable);
        }

        public static void ResolveStatement(Statement statement,Table table)
        {
            switch(statement.NodeKind)
            {
                case NodeType.AssignmentStatement:
                    ResolveAssignmentStatement((AssignmentStatement)statement,table);
                    break;
                case NodeType.Declaration:
                    ResolveDeclarationStatement((IdentifierDeclaration)statement,table);
                    break;
                case NodeType.MethodInvokationAsStatement:
                    ResolveMethodInvokationStatement((MethodInvokationStatement)statement,table);
                    break;
                case NodeType.If:
                    ResolveIfStatement((IfStatement)statement,table);
                    break;
                case NodeType.Do:
                case NodeType.While:
                    ResolveIterationStatement((IterationStatement)statement,table);
                    break;
                case NodeType.Break:
                case NodeType.Return:
                case NodeType.Continue:
                    ResolveJumpStatement((JumpStatement)statement,table);
                    break;
            }
        }
        public static void ResolveJumpStatement(JumpStatement jumpStatement, Table table)
        {
            if (jumpStatement.IsContinue() | jumpStatement.IsBreak())
            {
                if (!jumpStatement.IsSituatedInLoop())
                    Diagnostics.SemanticErrors.Add(new ErrorForDebug("Оператор " + (jumpStatement.IsContinue() ? "сontinue" : "break") + " должен находится в теле цикла"));
            }
            else
                ResolveExpression(((ReturnStatement)jumpStatement).ReturnBody, table);
        }
        public static void ResolveIterationStatement(IterationStatement iterationStatement, Table table)
        {
            ResolveExpression(iterationStatement.Condition, table);
            ResolveEmbeddedStatement((EmbeddedStatement)iterationStatement.Body, table);
        }
        public static void ResolveIfStatement(IfStatement ifStatement, Table table)
        {
            ResolveExpression(ifStatement.Condition,table);
            ResolveEmbeddedStatement((EmbeddedStatement)ifStatement.Body,table);
            if (ifStatement.ElseBody != null)
                ResolveEmbeddedStatement((EmbeddedStatement)ifStatement.ElseBody, table);
        }

        public static void ResolveAssignmentStatement(AssignmentStatement assignment, Table table)
        {
            EmbeddedStatement body = (EmbeddedStatement)assignment.GetParentByType(typeof(EmbeddedStatement));

            ResolveExpression(assignment.AdressableExpression, table);

            if (assignment.AdressorExpressions.Length > 1)
                foreach (IdentifierExpression identifier in assignment.AdressorExpressions)
                    ResolveIdentifierExpression(identifier, table, body);
            else
                ResolveAdressor(assignment.AdressorExpressions[0],body, table);
        }
        public static void ResolveDeclarationStatement(IdentifierDeclaration declaration, Table table)
        {
            if (declaration.AssingningExpression != null)
                ResolveAssignmentStatement(declaration.AssingningExpression, table);
            else
                foreach (IdentifierExpression identifier in declaration.DeclaringIdentifiers)
                    ResolveIdentifierExpression(identifier, table, null);
        }
        public static void ResolveMethodInvokationStatement(MethodInvokationStatement method, Table table)
        {
            ResolveMethodInvokation((MethodInvokationExpression)method.Instance, table);
            ResolveParameters(((MethodInvokationExpression)method.Instance).Parameters, table);
        }

        public static void ResolveIdentifierExpression(IdentifierExpression identifier, Table table, EmbeddedStatement initializedInBlock, bool checkInit = true)
        {
            if (identifier.IdentifierState == IdentifierExpression.State.Decl)
            {
                if (!table.PushIdentifier(identifier))
                {
                    Diagnostics.SemanticErrors.Add(new ErrorForDebug($"Переменная [{identifier.Name}] уже объявлена"));
                }
                if (initializedInBlock != null)
                {
                    table.InitializeInBlock(identifier, initializedInBlock);
                }
            }
            else
            {
                if (!table.CheckIdentifier(identifier.Name))
                {
                    Diagnostics.SemanticErrors.Add(new ErrorForDebug($"Переменная [{identifier.Name}] не объявлена"));
                }
                else
                {
                    TableIdentifier tableIdentifier = table.FetchIdentifier(identifier.Name);
                    identifier.Type = tableIdentifier.Type;

                    //check for initialization in this block
                    if (!table.IsInitializedInBlock(identifier, initializedInBlock) && checkInit)
                        Diagnostics.SemanticErrors.Add(new ErrorForDebug($"Переменной [{identifier.Name}] не присвоено значение в этой локальной области"));
                }
            }
        }
        public static void ResolveIdentifierExpressions(SyntaxTreeNode inNode, Table table)
        {
            EmbeddedStatement body = (EmbeddedStatement)inNode.GetParentByType(typeof(EmbeddedStatement));
            foreach (IdentifierExpression identifier in inNode.GetChildsByType(typeof(IdentifierExpression), true))
                ResolveIdentifierExpression(identifier,table,body);
        }

        public static void ResolveAdressor(Expression adressor,EmbeddedStatement body, Table table)
        {
            if (adressor is IdentifierExpression)
            {
                ResolveIdentifierExpression((IdentifierExpression)adressor, table, body, false);
                table.InitializeInBlock((IdentifierExpression)adressor,body);
            }
            else
                ResolveArrayElement((ArrayElementExpression)adressor, table);
        }

        public static void ResolveMethodInvokation(MethodInvokationExpression method,Table table)
        {   
            if (table.CheckMethod(method.Name,method.ArgCount))
            {
                TableMethod tableMethod = table.FetchMethod(method.Name,method.ArgCount);
                if (method.ArgCount != tableMethod.ArgCount)
                    Diagnostics.SemanticErrors.Add(new ErrorForDebug($"Функция [{method.Name}] не содержит такое количество аргументов [{method.ArgCount}], ожидалось [{tableMethod.ArgCount}]"));
                else
                {
                    for (int i = 0; i < method.ArgCount; i++)
                        ((ParameterDeclaration)method.Parameters[i]).Type = tableMethod.Arguments[i].Type;
                    method.ReturnType = tableMethod.ReturnType;
                }
            }
            else
                Diagnostics.SemanticErrors.Add(new ErrorForDebug($"Функция [{method.Name}] не объявлена, либо не принимает такое количество аргументов"));
        }
        public static void ResolveMethodInvokations(SyntaxTreeNode inNode, Table table)
        {
            foreach (MethodInvokationExpression method in inNode.GetChildsByType(typeof(MethodInvokationExpression),true))
                ResolveMethodInvokation(method, table);
        }

        public static void ResolveParameters(Expression[] parameters,Table table)
        {
            foreach (Expression expression in parameters)
                ResolveExpression(expression, table);
        }
        public static void ResolveMethodArguments(ArgumentDeclaration[] arguments, Table table)
        {
            if (arguments.Length == 0)
                return;
            MethodDeclaration method = (MethodDeclaration)arguments[0].GetParentByType(typeof(MethodDeclaration));
            foreach (ArgumentDeclaration argument in arguments)
                ResolveIdentifierExpression(argument.Identifier, table, method.Body);
        }

        public static void ResolveExpression(Expression expression, Table table)
        {
            ResolveIdentifierExpressions(expression, table);
            ResolveMethodInvokations(expression, table);
            ResolveArrayElements(expression, table);
        }
        public static void ResolveArrayElement(ArrayElementExpression arrayElement, Table table)
        {
            if (table.CheckIdentifier(arrayElement.ArrayName) && table.FetchIdentifier(arrayElement.ArrayName).Type is ArrayType)
            {
                TableIdentifier tableIdentifier = table.FetchIdentifier(arrayElement.ArrayName);
                arrayElement.ArrayDimension = (ushort)((ArrayType)tableIdentifier.Type).Dimension;
                arrayElement.Type = ((ArrayType)tableIdentifier.Type).GetDimensionElementType(arrayElement.Dimension);

                if (arrayElement.Type is null)
                    Diagnostics.SemanticErrors.Add(new ErrorForDebug($"Элемент размерности не соответствующей размерности массива"));
                if (arrayElement.ArrayDimension != arrayElement.Dimension)
                    Diagnostics.SemanticErrors.Add(new ErrorForDebug($"Разная размерность массива и элемента массива"));
            }
            else
                Diagnostics.SemanticErrors.Add(new ErrorForDebug($"Массив с именем {arrayElement.ArrayName} не объявлен"));
        }
        public static void ResolveArrayElements(SyntaxTreeNode inNode, Table table)
        {
            foreach (ArrayElementExpression arrayElement in inNode.GetChildsByType(typeof(ArrayElementExpression), true))
                ResolveArrayElement(arrayElement, table);
        }

        public static bool ResolveReturnInBody(EmbeddedStatement body)
        {
            if (body.GetChildsByType(typeof(ReturnStatement)).Length != 0)
                return true;

            MethodDeclaration method = (MethodDeclaration)body.GetParentByType(typeof(MethodDeclaration));
            SyntaxTreeNode[] ifConstructions = body.GetChildsByType(typeof(IfStatement),false,false);

            if (body.Childs.Count == 0 || ifConstructions.Length == 0)
            {
                Diagnostics.SemanticErrors.Add(new ErrorForDebug($"Не все пути к коду возвращают значение [{method.Name}]"));
                return false;
            }

            foreach (IfStatement ifConstruction in ifConstructions)
            {
                if (ifConstruction.ElseBody == null &&
                    !(ResolveReturnInBody((EmbeddedStatement)ifConstruction.Body)
                      && ResolveReturnInBody((EmbeddedStatement)ifConstruction.ElseBody)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}