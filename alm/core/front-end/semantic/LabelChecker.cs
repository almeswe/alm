using alm.Core.Table;
using alm.Core.Errors;
using alm.Core.InnerTypes;
using alm.Core.SyntaxTree;

using alm.Other.Enums;

using static alm.Core.Compiler.Compiler;

namespace alm.Core.FrontEnd.SemanticAnalysis
{
    public sealed class LabelChecker
    {
        private static bool IsMainDeclared;

        public static void ResolveModule(SyntaxTreeNode module)
        {
            MarkMethods(module);
            IsMainDeclared = false;
            ResolveGlobalIdentifierDeclarations(module);
            ResolveMethodDeclarations(module);
            if (!IsMainDeclared)
                Diagnostics.SemanticErrors.Add(new MainMethodExpected());
        }

        public static void MarkMethods(SyntaxTreeNode module)
        {
            GlobalTable.Table = Table.Table.CreateTable(null, 1);
            foreach (MethodDeclaration method in module.GetChildsByType(typeof(MethodDeclaration), true))
                if (!GlobalTable.Table.PushMethod(method))
                    Diagnostics.SemanticErrors.Add(new MethodIsAlreadyDeclared(method.Name, method.SourceContext));
        }

        public static void ResolveMainMethodDeclaration(MethodDeclaration method)
        {
            if (method.Name == "main")
                if (method.ArgCount == 0 && method.ReturnType is Int32)
                    IsMainDeclared = true;
        }
        public static void ResolveMethodDeclarations(SyntaxTreeNode inNode)
        {
            foreach (MethodDeclaration method in inNode.GetChildsByType(typeof(MethodDeclaration), true))
                ResolveMethodDeclaration(method);
        }
        public static void ResolveMethodDeclaration(MethodDeclaration method)
        {
            Table.Table MethodTable = Table.Table.CreateTable(GlobalTable.Table);

            ResolveMainMethodDeclaration(method);
            ResolveMethodArguments(method.Arguments, MethodTable);
            if (!method.IsExternal)
            {
                if (!(method.ReturnType is Void))
                    ResolveReturnInBody(method.Body);
                ResolveEmbeddedStatement(method.Body, MethodTable);
            }
        }
    
        public static void ResolveGlobalIdentifierDeclarations(SyntaxTreeNode inNode)
        {
            foreach (GlobalIdentifierDeclaration declaration in inNode.GetChildsByType(typeof(GlobalIdentifierDeclaration), true))
                ResolveGlobalIdentifierDeclaration(declaration);
        }
        public static void ResolveGlobalIdentifierDeclaration(GlobalIdentifierDeclaration declaration)
        {
            if (declaration.Declaration.AssingningExpression != null)
                ResolveAssignmentStatement(declaration.Declaration.AssingningExpression, GlobalTable.Table);
            else
                foreach (IdentifierExpression identifier in declaration.Declaration.DeclaringIdentifiers)
                    ResolveIdentifierExpression(identifier, GlobalTable.Table, null);
        }

        public static void ResolveEmbeddedStatement(EmbeddedStatement body, Table.Table table)
        {
            Table.Table bodyTable = Table.Table.CreateTable(table);
            foreach (SyntaxTreeNode statement in body.Childs)
                ResolveStatement((Statement)statement, bodyTable);
        }

        public static void ResolveStatement(Statement statement, Table.Table table)
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

                case NodeType.For:
                    ResolveForLoopStatement((ForLoopStatement)statement, table);
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

                default:
                    throw new System.Exception();
            }
        }
        public static void ResolveJumpStatement(JumpStatement jumpStatement, Table.Table table)
        {
            if (jumpStatement.IsContinue() | jumpStatement.IsBreak())
            {
                if (!jumpStatement.IsSituatedInLoop())
                    Diagnostics.SemanticErrors.Add(new OperatorMustBeSituatedInLoop(jumpStatement.IsContinue() ? "сontinue" : "break",jumpStatement.SourceContext));
            }
            else
                if (((ReturnStatement)jumpStatement).ReturnBody != null)
                    ResolveExpression(((ReturnStatement)jumpStatement).ReturnBody, table);
        }
        public static void ResolveIterationStatement(IterationStatement iterationStatement, Table.Table table)
        {
            ResolveExpression(iterationStatement.Condition, table);
            ResolveEmbeddedStatement((EmbeddedStatement)iterationStatement.Body, table);
        }
        public static void ResolveForLoopStatement(ForLoopStatement forLoop, Table.Table table)
        {
            ResolveStatement(forLoop.InitStatement, table);
            ResolveExpression(forLoop.Condition, table);
            ResolveStatement(forLoop.StepStatement,table);
            ResolveEmbeddedStatement((EmbeddedStatement)forLoop.Body, table);
        }
        public static void ResolveIfStatement(IfStatement ifStatement, Table.Table table)
        {
            ResolveExpression(ifStatement.Condition,table);
            ResolveEmbeddedStatement((EmbeddedStatement)ifStatement.Body,table);
            if (ifStatement.ElseBody != null)
                ResolveEmbeddedStatement((EmbeddedStatement)ifStatement.ElseBody, table);
        }

        public static void ResolveAssignmentStatement(AssignmentStatement assignment, Table.Table table)
        {
            EmbeddedStatement body = (EmbeddedStatement)assignment.GetParentByType(typeof(EmbeddedStatement));

            ResolveExpression(assignment.AdressableExpression, table);

            if (assignment.AdressorExpressions.Length > 1)
                foreach (IdentifierExpression identifier in assignment.AdressorExpressions)
                {
                    ResolveIdentifierExpression(identifier, table, body);
                    if (table == GlobalTable.Table)
                        table.FetchIdentifier(identifier.Name).InitializedGlobally = true;
                }
            else
                ResolveAdressor(assignment.AdressorExpressions[0], body, table);
        }
        public static void ResolveDeclarationStatement(IdentifierDeclaration declaration, Table.Table table)
        {
            if (declaration.AssingningExpression != null)
                ResolveAssignmentStatement(declaration.AssingningExpression, table);
            else
                foreach (IdentifierExpression identifier in declaration.DeclaringIdentifiers)
                    ResolveIdentifierExpression(identifier, table, null);
        }
        public static void ResolveMethodInvokationStatement(MethodInvokationStatement method, Table.Table table)
        {
            ResolveMethodInvokation((MethodInvokationExpression)method.Instance, table);
        }

        public static void ResolveIdentifierExpression(IdentifierExpression identifier, Table.Table table, EmbeddedStatement initializedInBlock, bool checkInit = true)
        {
            if (identifier.IdentifierState == IdentifierExpression.State.Decl)
            {
                if (!table.PushIdentifier(identifier, table == GlobalTable.Table ? true : false))
                    Diagnostics.SemanticErrors.Add(new IdentifierIsAlreadyDeclared(identifier.Name, identifier.SourceContext));

                if (initializedInBlock != null)
                    table.InitializeInBlock(identifier, initializedInBlock);
            }
            else
            {
                if (!table.CheckIdentifier(identifier.Name))
                    Diagnostics.SemanticErrors.Add(new IdentifierIsNotDeclared(identifier.Name, identifier.SourceContext));
                else
                {
                    TableIdentifier tableIdentifier = table.FetchIdentifier(identifier.Name);
                    identifier.Type = tableIdentifier.Type;
                    //check for initialization in this block
                    if (!table.IsInitializedInBlock(identifier, initializedInBlock) && checkInit && !tableIdentifier.InitializedGlobally)
                        Diagnostics.SemanticErrors.Add(new IdentifierIsNotInitialized(identifier.Name, identifier.SourceContext));
                }
            }
        }
        public static void ResolveIdentifierExpressions(SyntaxTreeNode inNode, Table.Table table)
        {
            EmbeddedStatement body = (EmbeddedStatement)inNode.GetParentByType(typeof(EmbeddedStatement));
            foreach (IdentifierExpression identifier in inNode.GetChildsByType(typeof(IdentifierExpression), true))
                ResolveIdentifierExpression(identifier,table,body);
        }

        public static void ResolveAdressor(Expression adressor,EmbeddedStatement body, Table.Table table)
        {
            if (adressor is IdentifierExpression)
            {
                ResolveIdentifierExpression((IdentifierExpression)adressor, table, body, false);
                if (table == GlobalTable.Table)
                    table.FetchIdentifier(((IdentifierExpression)adressor).Name).InitializedGlobally = true;
                else
                    table.InitializeInBlock((IdentifierExpression)adressor,body);
            }
            else
                ResolveArrayElement((ArrayElementExpression)adressor, table,true);
        }

        public static void ResolveMethodInvokation(MethodInvokationExpression method, Table.Table table)
        {
            ResolveMethodParameters(method.Parameters, table);

            if (table.CheckMethod(method.Name, method.GetParametersTypes(),true))
            {
                TableMethod tableMethod = table.FetchMethod(method.Name, method.GetParametersTypes());
                for (int i = 0; i < method.ArgCount; i++)
                    method.Parameters[i].Type = tableMethod.Arguments[i].Type;
                method.ReturnType = tableMethod.ReturnType;
            }
            else
                if (table.IsMethodWithThisNameDeclared(method.Name))
                    Diagnostics.SemanticErrors.Add(new MethodWithThoseArgumentsIsNotDeclared(method.Name,method.SourceContext));
                else
                    Diagnostics.SemanticErrors.Add(new MethodIsNotDeclared(method.Name, method.SourceContext));
        }
        public static void ResolveMethodInvokations(SyntaxTreeNode inNode, Table.Table table)
        {
            foreach (MethodInvokationExpression method in inNode.GetChildsByType(typeof(MethodInvokationExpression),true))
                ResolveMethodInvokation(method, table);
        }

        public static void ResolveMethodParameters(ParameterDeclaration[] parameters, Table.Table table)
        {
            foreach (ParameterDeclaration expression in parameters)
                ResolveExpression(expression.ParameterInstance, table);
        }
        public static void ResolveMethodArguments(ArgumentDeclaration[] arguments, Table.Table table)
        {
            if (arguments.Length == 0)
                return;
            MethodDeclaration method = (MethodDeclaration)arguments[0].GetParentByType(typeof(MethodDeclaration));
            foreach (ArgumentDeclaration argument in arguments)
                ResolveIdentifierExpression(argument.Identifier, table, method.Body);
        }

        public static void ResolveExpression(Expression expression, Table.Table table)
        {
            ResolveIdentifierExpressions(expression, table);
            ResolveMethodInvokations(expression, table);
            ResolveArrayElements(expression, table);
        }
        public static void ResolveArrayElement(ArrayElementExpression arrayElement, Table.Table table,bool asAdressor = false)
        {
            if (table.CheckIdentifier(arrayElement.ArrayName) && asAdressor)
            {
                TableIdentifier tableIdentifier = table.FetchIdentifier(arrayElement.ArrayName);
                if (tableIdentifier.Type is String)
                    Diagnostics.SemanticErrors.Add(new CannotChangeTheString(arrayElement.SourceContext));
            }

            if (table.CheckIdentifier(arrayElement.ArrayName) && table.FetchIdentifier(arrayElement.ArrayName).Type is ArrayType)
            {
                TableIdentifier tableIdentifier = table.FetchIdentifier(arrayElement.ArrayName);
                arrayElement.ArrayDimension = (ushort)((ArrayType)tableIdentifier.Type).Dimension;
                arrayElement.Type = ((ArrayType)tableIdentifier.Type).GetDimensionElementType(arrayElement.Dimension);
                arrayElement.ArrayType = tableIdentifier.Type;

                if (arrayElement.Type is null || arrayElement.ArrayDimension != arrayElement.Dimension)
                    Diagnostics.SemanticErrors.Add(new WrongArrayElementDimension(arrayElement.SourceContext));
            }
            else
                Diagnostics.SemanticErrors.Add(new ArrayIsNotDeclared(arrayElement.ArrayName,arrayElement.SourceContext));
        }
        public static void ResolveArrayElements(SyntaxTreeNode inNode, Table.Table table)
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
                Diagnostics.SemanticErrors.Add(new NotAllCodePathsReturnsValues(method.SourceContext));
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