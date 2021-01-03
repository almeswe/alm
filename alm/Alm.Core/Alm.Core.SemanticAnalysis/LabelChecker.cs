using alm.Core.Errors;
using alm.Core.VariableTable;
using alm.Core.SyntaxAnalysis;

using static alm.Core.Compiler.Compiler;

namespace alm.Core.SemanticAnalysis
{
    public sealed class LabelChecker
    {
        private static bool IsMainDeclared = false;

        public static void ResolveProgram(AbstractSyntaxTree ast)
        {
            IsMainDeclared = false;
            //foreach (GlobalDeclarationExpression globalDeclarationExpression in Ast.Root.GetChildsByType("GlobalDeclarationExpression", true))
            //    ResolveGlobalDeclarationExpression(globalDeclarationExpression, GlobalTable.Table);

            foreach (FunctionDeclaration function in ast.Root.GetChildsByType("FunctionDeclaration", true))
            {
                ResolveMainFunction(function);
                ResolveFunctionDeclaration(function, GlobalTable.Table);
            }

            if (!IsMainDeclared) Diagnostics.SemanticErrors.Add(new InExexutableFileMainExprected());
        }

        public static void ResolveMainFunction(FunctionDeclaration functionDeclaration)
        {
            if (functionDeclaration.Name == "main")
                if (functionDeclaration.SourceContext.FilePath == CompilingSourceFile)
                    if (!IsMainDeclared) IsMainDeclared = true;
        }

        public static void ResolveFunctionDeclaration(FunctionDeclaration functionDeclaration, Table Table)
        {
            Table ThisTable = Table.CreateTable(Table);
            if (Table.PushFunction(functionDeclaration))
            {
                ResolveArguments(functionDeclaration.Arguments, ThisTable);
                if (!functionDeclaration.External)
                {
                    ResolveBody(functionDeclaration.Body, ThisTable);
                    if (functionDeclaration.Type.GetEquivalence() != typeof(void))
                        if (!ResolveBlockForReturn(functionDeclaration.Body)) 
                            Diagnostics.SemanticErrors.Add(new NotAllCodePathsReturnValue(functionDeclaration.SourceContext));
                }
            }
            else Diagnostics.SemanticErrors.Add(new ThisFunctionAlreadyDeclared(functionDeclaration.Name, functionDeclaration.SourceContext));
        }

        public static void ResolveBody(Body body,Table table)
        {
            for (int i = 0; i < body.Nodes.Count; i++)
            {
                if (body.Nodes[i] is Statement)
                    ResolveStatement((Statement)body.Nodes[i], table);
                else if (body.Nodes[i] is Expression)
                    ResolveExpression((Expression)body.Nodes[i], table);
            }
        }

        public static void ResolveCondition(Condition condition, Table table)
        {
            Body Block = (Body)condition.GetParentByType("Body");

            foreach (IdentifierExpression testId in condition.GetChildsByType("IdentifierCall", true)) ResolveIdExression(testId, table, false, true, Block);
            foreach (FunctionCall testFunc in condition.GetChildsByType("FunctionCall",true)) ResolveFunctionCall(testFunc, table);
        }

        public static void ResolveBinaryExpression(BinaryExpression binaryExpression, Table table)
        {
            Body Block = (Body)binaryExpression.GetParentByType("Body");

            foreach (IdentifierExpression testId in binaryExpression.GetChildsByType("IdentifierCall", true)) ResolveIdExression(testId, table, false, true, Block);
            foreach (FunctionCall testFunc in binaryExpression.GetChildsByType("FunctionCall",true)) ResolveFunctionCall(testFunc, table);
        }

        public static void ResolveArguments(Arguments arguments, Table table)
        {
            for (int i = 0; i < arguments.Nodes.Count; i++)
                ResolveArgumentDeclaration((ArgumentDeclaration)arguments.Nodes[i], table);
        }

        public static void ResolveArgumentDeclaration(ArgumentDeclaration argumentDeclaration, Table table)
        {
            ResolveIdExression((IdentifierExpression)argumentDeclaration.Right, table, true);
        }

        public static int ResolveIdExression(IdentifierExpression identifierExpression, Table table, bool initOnStart = false, bool checkInit = false, Body inBlock = null)
        {
            if (identifierExpression is IdentifierDeclaration)
            {
                if (table.PushIdentifier(identifierExpression))
                {
                    if (initOnStart) table.SetGlobalInitialization(identifierExpression);
                    return 0;
                }
                else
                {
                    Diagnostics.SemanticErrors.Add(new ThisIdentifierAlreadyDeclared(identifierExpression.Name, identifierExpression.SourceContext));
                    return 1;
                }
            }

            else if (identifierExpression is IdentifierCall)
            {
                if (!table.CheckIdentifier(identifierExpression.Name))
                {
                    Diagnostics.SemanticErrors.Add(new ThisIdentifierNotDeclared(identifierExpression.Name, identifierExpression.SourceContext));
                    return 1;
                }
                else
                {
                    identifierExpression.Type = table.FetchIdentifier(identifierExpression).Type;
                    if (checkInit)
                    {
                        if (inBlock != null)
                        {
                            bool initialized = false;
                            for (SyntaxTreeNode ThisBlock = inBlock;ThisBlock != null;ThisBlock = ThisBlock.GetParentByType("Body"))
                                if (table.IsInitializedInBlock(identifierExpression, (Body)ThisBlock)) initialized = true;
                            if (!initialized)
                            {
                                Diagnostics.SemanticErrors.Add(new ThisIdentifierNotInitialized(identifierExpression.Name, identifierExpression.SourceContext));
                                return 1;
                            }
                        }
                        else
                        {
                            if (!table.IsGloballyInitialized(identifierExpression))
                            {
                                Diagnostics.SemanticErrors.Add(new ThisIdentifierNotInitialized(identifierExpression.Name, identifierExpression.SourceContext));
                                return 1;
                            }
                        }
                    }
                }
                return 0;
            }

            return 1;
        }

        public static int ResolveFunctionCall(FunctionCall functionCall, Table table)
        {
            if (table.CheckFunction(functionCall.Name,functionCall.ArgumentCount))
            {
                TableFunction func = table.FetchFunction(functionCall.Name, functionCall.ArgumentCount);
                if (func.ArgumentCount != functionCall.ArgumentCount)
                {
                    Diagnostics.SemanticErrors.Add(new FunctionNotContainsThisNumberOfArguments(functionCall.Name, func.ArgumentCount, functionCall.ArgumentCount, functionCall.SourceContext));
                    return 1;
                }
                functionCall.Type = func.Type;
                foreach (var arg in functionCall.ArgumentsValues.Nodes)
                        ResolveExpression((Expression)arg,table);
                return 0;
            }
            else
            {
                Diagnostics.SemanticErrors.Add(new ThisFunctionNotDeclared(functionCall.Name,functionCall.SourceContext));
                return 1;
            }
        }

        public static void ResolveStatement(Statement statement, Table table)
        {
            ResolveCondition(statement.Condition,table);
            ResolveBody(statement.Body, Table.CreateTable(table));
            if (statement.ElseBody != null) ResolveBody(statement.ElseBody, Table.CreateTable(table));
        }

        public static void ResolveReturnExpression(ReturnExpression returnExpression, Table table)
        {
            Body Block = (Body)returnExpression.GetParentByType("Body");

            if (returnExpression.Right is IdentifierExpression) ResolveIdExression((IdentifierExpression)returnExpression.Right, table, false, true, Block);
            else if (returnExpression.Right is Expression)
            {
                foreach (IdentifierExpression testId in returnExpression.Right.GetChildsByType("IdentifierCall", true)) ResolveIdExression(testId, table, false, true, Block);
                foreach (FunctionCall testFunc in returnExpression.Right.GetChildsByType("FunctionCall",true)) ResolveFunctionCall(testFunc, table);
            }

        }

        public static void ResolveExpression(Expression expression, Table table)
        {
            if      (expression is AssignmentExpression)  ResolveAssignmentExpression((AssignmentExpression)expression, table);
            else if (expression is DeclarationExpression) ResolveDeclarationExpression((DeclarationExpression)expression, table);
            else if (expression is IdentifierExpression)  ResolveIdExression((IdentifierExpression)expression,table,false,true, (Body)expression.GetParentByType("Body"));
            else if (expression is ReturnExpression)      ResolveReturnExpression((ReturnExpression)expression, table);
            else if (expression is BinaryExpression)      ResolveBinaryExpression((BinaryExpression)expression,table);
            else if (expression is FunctionCall)          ResolveFunctionCall((FunctionCall)expression,table);
        }

        public static void ResolveAssignmentExpression(AssignmentExpression assignmentExpression, Table table)
        {
            bool failed = false;
            Body Block  = (Body)assignmentExpression.GetParentByType("Body");

            if (ResolveIdExression((IdentifierExpression)assignmentExpression.Left, table) == 1) failed = true;

            foreach (IdentifierExpression testId in assignmentExpression.Right.GetChildsByType("IdentifierCall", true))
                if (ResolveIdExression(testId, table, false, true, Block) == 1) failed = true;
            foreach (FunctionCall testFunc in assignmentExpression.Right.GetChildsByType("FunctionCall",true))
                if (ResolveFunctionCall(testFunc, table) == 1) failed = true;

            if (!failed) table.SetLocalBlockInitialization((IdentifierExpression)assignmentExpression.Left,Block);
        }

        public static void ResolveGlobalDeclarationExpression(GlobalDeclarationExpression globalDeclarationExpression, Table table)
        {
            DeclarationExpression declarationExpression = (DeclarationExpression)globalDeclarationExpression.Right;
            if (declarationExpression.Right is IdentifierExpression)      ResolveIdExression((IdentifierExpression)declarationExpression.Right, table,true);
            else if (declarationExpression.Right is AssignmentExpression) ResolveAssignmentExpression((AssignmentExpression)declarationExpression.Right, table);
        }

        public static void ResolveDeclarationExpression(DeclarationExpression declarationExpression, Table table)
        {
            if      (declarationExpression.Right is IdentifierExpression) ResolveIdExression((IdentifierExpression)declarationExpression.Right, table);
            else if (declarationExpression.Right is AssignmentExpression) ResolveAssignmentExpression((AssignmentExpression)declarationExpression.Right, table);
        }
        public static bool ResolveBlockForReturn(Body block)
        {
            bool Resolved = false;

            foreach (var node in block.Nodes)
            {
                if (node is Statement)
                {
                    bool IfElseResolved = false;

                    if (!ResolveBlockForReturn(((Statement)node).Body))
                        Resolved = false;
                    else 
                        IfElseResolved = true;

                    if (((Statement)node).ElseBody != null)
                        if (!ResolveBlockForReturn(((Statement)node).ElseBody))
                            Resolved = false;
                        else
                            if (IfElseResolved) Resolved = true;
                }
                if (node is ReturnExpression) Resolved = true;
            }
            return Resolved;
        }
    }
}
