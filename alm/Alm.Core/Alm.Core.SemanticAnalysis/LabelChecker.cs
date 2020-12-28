using alm.Core.Errors;
using alm.Core.VariableTable;
using alm.Core.SyntaxAnalysis;

using static alm.Core.Compiler.Compiler;

namespace alm.Core.SemanticAnalysis
{
    public sealed class LabelChecker
    {
        private static bool IsMainDeclared = false;

        public static void ResolveProgram(AbstractSyntaxTree Ast)
        {
            IsMainDeclared = false;
            foreach (FunctionDeclaration function in Ast.Root.GetChildsByType("FunctionDeclaration", true))
            {
                ResolveMainFunction(function);
                ResolveFunctionDeclaration(function, GlobalTable.Table);
            }

            if (!IsMainDeclared) Diagnostics.SemanticErrors.Add(new InExexutableFileMainExprected());
        }

        public static void ResolveMainFunction(FunctionDeclaration FuncDecl)
        {
            if (FuncDecl.Name == "main")
                if (FuncDecl.SourceContext.FilePath == CompilingSourceFile)
                    if (!IsMainDeclared) IsMainDeclared = true;
        }

        public static void ResolveFunctionDeclaration(FunctionDeclaration FuncDecl, Table Table)
        {
            Table ThisTable = Table.CreateTable(Table);
            if (Table.PushFunction(FuncDecl))
            {
                ResolveArguments(FuncDecl.Arguments, ThisTable);
                ResolveBody(FuncDecl.Body, ThisTable);
                if (!ResolveBlockForReturn(FuncDecl.Body)) Diagnostics.SemanticErrors.Add(new NotAllCodePathsReturnValue(FuncDecl.SourceContext));
            }
            else Diagnostics.SemanticErrors.Add(new ThisFunctionAlreadyDeclared(FuncDecl.Name, FuncDecl.SourceContext));
        }

        public static void ResolveBody(Body Body,Table Table)
        {
            for (int i = 0; i < Body.Nodes.Count; i++)
            {
                if (Body.Nodes[i] is Statement)
                    ResolveStatement((Statement)Body.Nodes[i], Table);
                else if (Body.Nodes[i] is Expression)
                    ResolveExpression((Expression)Body.Nodes[i], Table);
            }
        }

        public static void ResolveCondition(Condition Condition, Table Table)
        {
            Body Block = (Body)Condition.GetParentByType("Body");

            foreach (IdentifierExpression testId in Condition.GetChildsByType("IdentifierCall", true)) ResolveIdExression(testId, Table, false, true, Block);
            foreach (FunctionCall testFunc in Condition.GetChildsByType("FunctionCall",true)) ResolveFunctionCall(testFunc, Table);
        }

        public static void ResolveBinaryExpression(BinaryExpression BinaryExpression, Table Table)
        {
            Body Block = (Body)BinaryExpression.GetParentByType("Body");

            foreach (IdentifierExpression testId in BinaryExpression.GetChildsByType("IdentifierCall", true)) ResolveIdExression(testId, Table, false, true, Block);
            foreach (FunctionCall testFunc in BinaryExpression.GetChildsByType("FunctionCall",true)) ResolveFunctionCall(testFunc, Table);
        }

        public static void ResolveArguments(Arguments Arguments, Table Table)
        {
            for (int i = 0; i < Arguments.Nodes.Count; i++)
                ResolveArgumentDeclaration((ArgumentDeclaration)Arguments.Nodes[i], Table);
        }

        public static void ResolveArgumentDeclaration(ArgumentDeclaration ArgumentDeclaration, Table Table)
        {
            ResolveIdExression((IdentifierExpression)ArgumentDeclaration.Right, Table, true);
        }

        public static int ResolveIdExression(IdentifierExpression IdExpression, Table Table, bool InitOnStart = false, bool CheckInit = false, Body InBlock = null)
        {
            if (IdExpression is IdentifierDeclaration)
            {
                if (Table.PushIdentifier(IdExpression))
                {
                    if (InitOnStart) Table.SetGlobalInitialization(IdExpression);
                    return 0;
                }
                else
                {
                    Diagnostics.SemanticErrors.Add(new ThisIdentifierAlreadyDeclared(IdExpression.Name, IdExpression.SourceContext));
                    return 1;
                }
            }

            else if (IdExpression is IdentifierCall)
            {
                if (!Table.CheckIdentifier(IdExpression))
                {
                    Diagnostics.SemanticErrors.Add(new ThisIdentifierNotDeclared(IdExpression.Name, IdExpression.SourceContext));
                    return 1;
                }
                else
                {
                    IdExpression.Type = Table.FetchIdentifier(IdExpression).Type;
                    if (CheckInit)
                    {
                        if (InBlock != null)
                        {
                            bool initialized = false;
                            for (SyntaxTreeNode ThisBlock = InBlock;ThisBlock != null;ThisBlock = ThisBlock.GetParentByType("Body"))
                                if (Table.IsInitializedInBlock(IdExpression, (Body)ThisBlock)) initialized = true;
                            if (!initialized)
                            {
                                Diagnostics.SemanticErrors.Add(new ThisIdentifierNotInitialized(IdExpression.Name, IdExpression.SourceContext));
                                return 1;
                            }
                        }
                        else
                        {
                            if (!Table.IsGloballyInitialized(IdExpression))
                            {
                                Diagnostics.SemanticErrors.Add(new ThisIdentifierNotInitialized(IdExpression.Name, IdExpression.SourceContext));
                                return 1;
                            }
                        }
                    }
                }
                return 0;
            }

            return 1;
        }

        public static int ResolveFunctionCall(FunctionCall FunctionCall, Table Table)
        {
            if (Table.CheckFunction(FunctionCall))
            {
                Function func = Table.FetchFunction(FunctionCall);
                if (func.ArgumentCount != FunctionCall.ArgumentCount)
                {
                    Diagnostics.SemanticErrors.Add(new FunctionNotContainsThisNumberOfArguments(FunctionCall.Name, func.ArgumentCount, FunctionCall.ArgumentCount, FunctionCall.SourceContext));
                    return 1;
                }
                FunctionCall.Type = func.ReturnType;
                foreach (var arg in FunctionCall.Arguments.Nodes)
                        ResolveExpression((Expression)arg,Table);
                return 0;
            }
            else
            {
                Diagnostics.SemanticErrors.Add(new ThisFunctionNotDeclared(FunctionCall.Name,FunctionCall.SourceContext));
                return 1;
            }
        }

        public static void ResolveStatement(Statement Statement, Table Table)
        {
            ResolveCondition(Statement.Condition,Table);
            ResolveBody(Statement.Body, Table.CreateTable(Table));
            if (Statement.ElseBody != null) ResolveBody(Statement.ElseBody, Table.CreateTable(Table));
        }

        public static void ResolveReturnExpression(ReturnExpression ReturnExpression, Table Table)
        {
            Body Block = (Body)ReturnExpression.GetParentByType("Body");

            if (ReturnExpression.Right is IdentifierExpression) ResolveIdExression((IdentifierExpression)ReturnExpression.Right, Table, false, true, Block);
            else if (ReturnExpression.Right is Expression)
            {
                foreach (IdentifierExpression testId in ReturnExpression.Right.GetChildsByType("IdentifierCall", true)) ResolveIdExression(testId, Table, false, true, Block);
                foreach (FunctionCall testFunc in ReturnExpression.Right.GetChildsByType("FunctionCall",true)) ResolveFunctionCall(testFunc, Table);
            }

        }

        public static void ResolveExpression(Expression Expression, Table Table)
        {
            if      (Expression is AssignmentExpression)  ResolveAssignmentExpression((AssignmentExpression)Expression, Table);
            else if (Expression is DeclarationExpression) ResolveDeclarationExpression((DeclarationExpression)Expression, Table);
            else if (Expression is IdentifierExpression)  ResolveIdExression((IdentifierExpression)Expression,Table,false,true, (Body)Expression.GetParentByType("Body"));
            else if (Expression is ReturnExpression)      ResolveReturnExpression((ReturnExpression)Expression, Table);
            else if (Expression is BinaryExpression)      ResolveBinaryExpression((BinaryExpression)Expression,Table);
            else if (Expression is FunctionCall)          ResolveFunctionCall((FunctionCall)Expression,Table);
        }

        public static void ResolveAssignmentExpression(AssignmentExpression AssignmentExpression, Table Table)
        {
            bool failed = false;
            Body Block  = (Body)AssignmentExpression.GetParentByType("Body");

            if (ResolveIdExression((IdentifierExpression)AssignmentExpression.Left, Table) == 1) failed = true;

            foreach (IdentifierExpression testId in AssignmentExpression.Right.GetChildsByType("IdentifierCall", true))
                if (ResolveIdExression(testId, Table, false, true, Block) == 1) failed = true;
            foreach (FunctionCall testFunc in AssignmentExpression.Right.GetChildsByType("FunctionCall",true))
                if (ResolveFunctionCall(testFunc, Table) == 1) failed = true;

            if (!failed) Table.SetLocalBlockInitialization((IdentifierExpression)AssignmentExpression.Left,Block);
        }

        public static void ResolveDeclarationExpression(DeclarationExpression DeclarationExpression, Table Table)
        {
            if      (DeclarationExpression.Right is IdentifierExpression) ResolveIdExression((IdentifierExpression)DeclarationExpression.Right, Table);
            else if (DeclarationExpression.Right is AssignmentExpression) ResolveAssignmentExpression((AssignmentExpression)DeclarationExpression.Right, Table);
        }
        public static bool ResolveBlockForReturn(Body Block)
        {
            bool Resolved = false;

            foreach (var node in Block.Nodes)
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
