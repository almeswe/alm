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

            foreach (FunctionDeclaration function in ast.Root.GetChildsByType("FunctionDeclaration", true))
            {
                ResolveMainFunction(function);
                ResolveFunctionDeclaration(function, GlobalTable.Table);
            }

            if (!IsMainDeclared) 
                Diagnostics.SemanticErrors.Add(new InExexutableFileMainExprected());
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
                if (body.Nodes[i] is Statement)
                    ResolveStatement((Statement)body.Nodes[i], table);
                else if (body.Nodes[i] is Expression)
                    ResolveExpression((Expression)body.Nodes[i], table);
        }

        public static void ResolveCondition(Condition condition, Table table)
        {
            Body Block = (Body)condition.GetParentByType("Body");
            //condition.Nodes[0] -> BooleanExpression
            ResolveMainElements(condition.Nodes[0], Block, table);
        }

        public static void ResolveBinaryExpression(BinaryExpression binaryExpression, Table table)
        {
            Body Block = (Body)binaryExpression.GetParentByType("Body");
            ResolveMainElements(binaryExpression,Block,table);
        }

        public static void ResolveArguments(Arguments arguments, Table table)
        {
            foreach (IdentifierExpression identifier in arguments.GetChildsByType("IdentifierDeclaration", true))
                //arg's representation is: <arg_decl> ::= <type_expr> <id_expr>
                ResolveIdentifierExpression(identifier, table, true);
        }

        public static bool ResolveIdentifierExpression(IdentifierExpression identifierExpression, Table table, bool initOnStart = false, bool checkInit = false, Body inBlock = null)
        {
            if (identifierExpression is IdentifierDeclaration)
            {
                if (table.PushIdentifier(identifierExpression))
                {
                    if (initOnStart)
                        table.SetGlobalInitialization(identifierExpression);
                    return true;
                }
                Diagnostics.SemanticErrors.Add(new ThisIdentifierAlreadyDeclared(identifierExpression.Name, identifierExpression.SourceContext));
                return false;
            }

            else if (identifierExpression is IdentifierCall)
            {
                if (!table.CheckIdentifier(identifierExpression.Name))
                {
                    Diagnostics.SemanticErrors.Add(new ThisIdentifierNotDeclared(identifierExpression.Name, identifierExpression.SourceContext));
                    return false;
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
                                if (table.IsInitializedInBlock(identifierExpression, (Body)ThisBlock)) 
                                    initialized = true;

                            if (!initialized)
                            {
                                Diagnostics.SemanticErrors.Add(new ThisIdentifierNotInitialized(identifierExpression.Name, identifierExpression.SourceContext));
                                return false;
                            }
                        }
                        else
                        {
                            if (!table.IsGloballyInitialized(identifierExpression))
                            {
                                Diagnostics.SemanticErrors.Add(new ThisIdentifierNotInitialized(identifierExpression.Name, identifierExpression.SourceContext));
                                return false;
                            }
                        }
                    }
                }
                return true;
            }

            return false;
        }

        public static bool ResolveFunctionCall(FunctionCall functionCall, Table table)
        {
            if (table.CheckFunction(functionCall.Name,functionCall.ArgumentCount))
            {
                TableFunction func = table.FetchFunction(functionCall.Name, functionCall.ArgumentCount);
                if (func.ArgumentCount != functionCall.ArgumentCount)
                {
                    Diagnostics.SemanticErrors.Add(new FunctionNotContainsThisNumberOfArguments(functionCall.Name, func.ArgumentCount, functionCall.ArgumentCount, functionCall.SourceContext));
                    return false;
                }
                functionCall.Type = func.Type;
                foreach (var arg in functionCall.ArgumentsValues.Nodes)
                        ResolveExpression((Expression)arg,table);
                return true;
            }
            else
            {
                Diagnostics.SemanticErrors.Add(new ThisFunctionNotDeclared(functionCall.Name,functionCall.SourceContext));
                return false;
            }
        }

        public static void ResolveStatement(Statement statement, Table table)
        {
            ResolveCondition(statement.Condition,table);
            ResolveBody(statement.Body, Table.CreateTable(table));
            if (statement.ElseBody != null) 
                ResolveBody(statement.ElseBody, Table.CreateTable(table));
        }

        public static void ResolveReturnExpression(ReturnExpression returnExpression, Table table)
        {
            Body Block = (Body)returnExpression.GetParentByType("Body");
            ResolveMainElements(returnExpression.Right, Block, table);
        }

        public static void ResolveExpression(Expression expression, Table table)
        {
            if      (expression is AssignmentExpression)  ResolveAssignmentExpression((AssignmentExpression)expression, table);
            else if (expression is DeclarationExpression) ResolveDeclarationExpression((DeclarationExpression)expression, table);
            else if (expression is IdentifierExpression)  ResolveIdentifierExpression((IdentifierExpression)expression,table,false,true, (Body)expression.GetParentByType("Body"));
            else if (expression is ReturnExpression)      ResolveReturnExpression((ReturnExpression)expression, table);
            else if (expression is BinaryExpression)      ResolveBinaryExpression((BinaryExpression)expression,table);
            else if (expression is FunctionCall)          ResolveFunctionCall((FunctionCall)expression,table);
            else if (expression is ArrayElement)          ResolveArrayElement((ArrayElement)expression, table);
        }

        public static bool ResolveArrayElement(ArrayElement arrayElement, Table table)
        {
            if (table.CheckIdentifier(arrayElement.ArrayName))
            {
                TableIdentifier id = table.FetchIdentifier(arrayElement.ArrayName);
                if (id.Type is Other.InnerTypes.ArrayType)
                {
                    //При многомерном массиве не сработает
                    arrayElement.Type = ((Other.InnerTypes.ArrayType)id.Type).GetElementType(arrayElement.Dimension);
                    return true;
                }
                else
                {
                    Diagnostics.SemanticErrors.Add(new ArrayDoesNotExist(arrayElement.ArrayName, arrayElement.SourceContext));
                    return false;
                }
                
            }
            Diagnostics.SemanticErrors.Add(new ArrayDoesNotExist(arrayElement.ArrayName, arrayElement.SourceContext));
            return false;
        }

        public static void ResolveAssignmentExpression(AssignmentExpression assignmentExpression, Table table)
        {
            bool failed = false;
            Body Block  = (Body)assignmentExpression.GetParentByType("Body");

            if (assignmentExpression.Left is IdentifierExpression)
            {
                if (!ResolveIdentifierExpression((IdentifierExpression)assignmentExpression.Left, table))
                    failed = true;
            }
            else if (assignmentExpression.Left is ArrayElement)
            {
                ResolveArrayElement((ArrayElement)assignmentExpression.Left, table);
                failed = true;
            }

            if (ResolveMainElements(assignmentExpression.Right,Block,table))
                failed = true;

            if (!failed) 
                table.SetLocalBlockInitialization((IdentifierExpression)assignmentExpression.Left,Block);
        }

        public static void ResolveDeclarationExpression(DeclarationExpression declarationExpression, Table table)
        {
            if      (declarationExpression.Right is IdentifierExpression) ResolveIdentifierExpression((IdentifierExpression)declarationExpression.Right, table);
            else if (declarationExpression.Right is AssignmentExpression) ResolveAssignmentExpression((AssignmentExpression)declarationExpression.Right, table);
        }

        public static bool ResolveMainElements(SyntaxTreeNode node, Body blockForIdentifier, Table table)
        {
            bool failed = false;

            foreach (IdentifierExpression identifierExpression in node.GetChildsByType("IdentifierCall", true))
                if (!ResolveIdentifierExpression(identifierExpression, table, false, true, blockForIdentifier))
                    failed = true;
            foreach (ArrayElement arrayElement in node.GetChildsByType("ArrayElement", true))
                if (!ResolveArrayElement(arrayElement, table))
                    failed = true;
            foreach (FunctionCall functionCall in node.GetChildsByType("FunctionCall", true))
                if (!ResolveFunctionCall(functionCall, table))
                    failed = true;

            return failed;
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
