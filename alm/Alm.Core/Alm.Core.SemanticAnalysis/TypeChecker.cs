using alm.Other.InnerTypes;
using alm.Core.Errors;
using alm.Core.SyntaxAnalysis;

using static alm.Other.Enums.Operator;
using alm.Core.VariableTable;

namespace alm.Core.SemanticAnalysis
{
    public sealed class TypeChecker
    {
        private static bool ErrorShownForBinary { get; set; } = false;
        private static bool ErrorShownForBoolean { get; set; } = false;

        private static InnerType ResolveNodeType(SyntaxTreeNode root)
        {
            InnerType type = null;
            bool typeIsSetted = false;
            if (root is ITypeable) return ((ITypeable)root).Type;

            foreach (SyntaxTreeNode node in root.Nodes)
            {
                if (node is ITypeable)
                {
                    if (!typeIsSetted)
                    {
                        type = ((ITypeable)node).Type;
                        typeIsSetted = true;
                    }
                    else
                    {
                        if (type != ((ITypeable)node).Type)
                            return new Underfined();
                    }
                }
                else
                {
                    InnerType testtype = ResolveNodeType(node);
                    if (!typeIsSetted)
                    {
                        type = testtype;
                        typeIsSetted = true;
                    }
                    else
                    {
                        if (testtype != type && testtype != null)
                            return new Underfined();
                        else type = testtype;
                    }
                }
            }
            return type;
        }
        private static InnerType ResolveConditionType(Condition Condition)
        {
            InnerType ConditionType;
            InnerType ExpectedType = new Boolean();

            SyntaxTreeNode Node = Condition.Nodes[0];

            if (Node is BooleanConst) return ((BooleanConst)Node).Type;

            if      (Node is BooleanExpression)    ConditionType = ResolveBooleanExpressionType((BooleanExpression)Node,false);
            else if (Node is IdentifierExpression) ConditionType = ((IdentifierExpression)Node).Type;
            else if (Node is FunctionCall)         ConditionType = ResolveFunctionCallType((FunctionCall)Node);
            else                                   ConditionType = ResolveNodeType(Node);

            if (ConditionType == ExpectedType) return ExpectedType;
            if (!ErrorShownForBoolean) Diagnostics.SemanticErrors.Add(new IncompatibleConditionType(ConditionType,Node.SourceContext));
            return ConditionType;
        }
        private static InnerType ResolveBooleanExpressionType(BooleanExpression BoolExpr, bool first = false)
        {
            InnerType LeftType;
            InnerType RightType;
            InnerType ExpectedType;

            if (first) ErrorShownForBoolean = false;

            switch (BoolExpr.Op)
            {
                case Less:
                case LessEqual:
                case Greater:
                case GreaterEqual:
                    ExpectedType = new Integer32();break;
                default:
                    ExpectedType = null;break;
            }    

            if      (BoolExpr.Left is BinaryExpression)   LeftType = ResolveBinaryExpressionType((BinaryExpression)BoolExpr.Left,false);
            else if (BoolExpr.Left is BooleanExpression)  LeftType = ResolveBooleanExpressionType((BooleanExpression)BoolExpr.Left,false);
            else if (BoolExpr.Left is FunctionCall)       LeftType = ResolveFunctionCallType((FunctionCall)BoolExpr.Left);
            else if (BoolExpr.Left is null)               LeftType = new Boolean();//for not operator 
            else                                          LeftType = ResolveNodeType(BoolExpr.Left);

            if      (BoolExpr.Right is BinaryExpression)  RightType = ResolveBinaryExpressionType((BinaryExpression)BoolExpr.Right,false);
            else if (BoolExpr.Right is BooleanExpression) RightType = ResolveBooleanExpressionType((BooleanExpression)BoolExpr.Right,false);
            else if (BoolExpr.Right is FunctionCall)      RightType = ResolveFunctionCallType((FunctionCall)BoolExpr.Right);
            else                                          RightType = ResolveNodeType(BoolExpr.Right);

            if (ExpectedType is null)
            {
                if (BoolExpr.Op == Equal || BoolExpr.Op == NotEqual)
                {
                    if (LeftType == RightType ) return new Boolean();
                    if (!ErrorShownForBoolean) Diagnostics.SemanticErrors.Add(new IncompatibleBooleanExpressionType(LeftType, RightType, BoolExpr.SourceContext));
                    ErrorShownForBoolean = true;
                    return RightType;
                }
                ExpectedType = new Boolean();
                if (LeftType == ExpectedType && RightType == ExpectedType) return new Boolean();

            }

            if (LeftType == ExpectedType && RightType == ExpectedType) return new Boolean();

            if (ExpectedType != RightType)
            {
                if (!ErrorShownForBoolean) Diagnostics.SemanticErrors.Add(new IncompatibleBooleanExpressionType(ExpectedType, RightType, BoolExpr.Right.SourceContext));
                ErrorShownForBoolean = true;
                return ExpectedType;
            }
            else if (ExpectedType != LeftType)
            {
                if (!ErrorShownForBoolean) Diagnostics.SemanticErrors.Add(new IncompatibleBooleanExpressionType(ExpectedType, LeftType, BoolExpr.Left.SourceContext));
                ErrorShownForBoolean = true;
                return ExpectedType;
            }

            return new Underfined();
        }

        private static InnerType ResolveAssignmentExpressionType(AssignmentExpression Assign)
        {
            InnerType RightType;
            InnerType LeftType = ((IdentifierExpression)Assign.Left).Type;

            if      (Assign.Right is BinaryExpression)     RightType = ResolveBinaryExpressionType(((BinaryExpression)Assign.Right));
            else if (Assign.Right is FunctionCall)         RightType = ResolveFunctionCallType((FunctionCall)Assign.Right);
            else if (Assign.Right is IdentifierExpression) RightType = ((IdentifierExpression)Assign.Right).Type;
            else                                           RightType = ResolveNodeType(Assign.Right);

            if (RightType == LeftType) return LeftType;
            Diagnostics.SemanticErrors.Add(new IncompatibleAssignmentType(RightType,LeftType,Assign.SourceContext));
            return new Underfined();
        }
        private static InnerType ResolveBinaryExpressionType(BinaryExpression BinaryExpr,bool first = true)
        {
            InnerType LeftType;
            InnerType RightType;
            InnerType ExpectedType = new Integer32();

            if (first) ErrorShownForBinary = false;

            if      (BinaryExpr.Left is ConstExpression)      LeftType = ((ConstExpression)BinaryExpr.Left).Type;
            else if (BinaryExpr.Left is IdentifierExpression) LeftType = ((IdentifierExpression)BinaryExpr.Left).Type;
            else if (BinaryExpr.Left is FunctionCall)         LeftType = ResolveFunctionCallType((FunctionCall)BinaryExpr.Left);
            else if (BinaryExpr.Left is BinaryExpression)     LeftType = ResolveBinaryExpressionType((BinaryExpression)BinaryExpr.Left,false);
            else                                              LeftType = ResolveNodeType(BinaryExpr.Left);

            if      (BinaryExpr.Right is ConstExpression)      RightType = ((ConstExpression)BinaryExpr.Right).Type;
            else if (BinaryExpr.Right is IdentifierExpression) RightType = ((IdentifierExpression)BinaryExpr.Right).Type;
            else if (BinaryExpr.Right is FunctionCall)         RightType = ResolveFunctionCallType((FunctionCall)BinaryExpr.Right);
            else if (BinaryExpr.Right is BinaryExpression)     RightType = ResolveBinaryExpressionType((BinaryExpression)BinaryExpr.Right,false);
            else                                               RightType = ResolveNodeType(BinaryExpr.Right);

            if (ExpectedType == RightType && ExpectedType == LeftType) return ExpectedType;

            if (ExpectedType != RightType)
            {
                if (!ErrorShownForBinary) Diagnostics.SemanticErrors.Add(new IncompatibleBinaryExpressionType(ExpectedType, RightType,BinaryExpr.SourceContext));
                ErrorShownForBinary = true;
                return ExpectedType;
            }
            else if (ExpectedType != LeftType)
            {
                if (!ErrorShownForBinary) Diagnostics.SemanticErrors.Add(new IncompatibleBinaryExpressionType(ExpectedType, LeftType, BinaryExpr.SourceContext));
                ErrorShownForBinary = true;
                return ExpectedType;
            }

            return new Underfined();
        }
        private static InnerType ResolveReturnExpressionType(ReturnExpression ReturnExpr)
        {
            InnerType ExpectedType = ((FunctionDeclaration)ReturnExpr.GetParentByType("FunctionDeclaration")).Type;
            InnerType RightType;

            if      (ReturnExpr.Right is ConstExpression)      RightType = ((ConstExpression)ReturnExpr.Right).Type;
            else if (ReturnExpr.Right is BinaryExpression)     RightType = ResolveBinaryExpressionType(((BinaryExpression)ReturnExpr.Right));
            else if (ReturnExpr.Right is FunctionCall)         RightType = ResolveFunctionCallType((FunctionCall)ReturnExpr.Right);
            else if (ReturnExpr.Right is IdentifierExpression) RightType = ((IdentifierExpression)ReturnExpr.Right).Type;
            else                                               RightType = ResolveNodeType(ReturnExpr.Right);

            if (ExpectedType != RightType)
            {
                Diagnostics.SemanticErrors.Add(new IncompatibleReturnType(ExpectedType, RightType, ReturnExpr.SourceContext));
                return RightType;
            }
            return ExpectedType;
        }

        private static InnerType ResolveFunctionCallType(FunctionCall FunctionCall)
        {
            InnerType ExpectedType, Type = null;

            for (int i = 0; i < FunctionCall.Arguments.Nodes.Count; i++)
            {
                ExpectedType = GlobalTable.Table.FetchFunction(FunctionCall).Arguments[i].Type;

                if (FunctionCall.Arguments.Nodes[i] is BinaryExpression)
                    Type = ResolveBinaryExpressionType((BinaryExpression)FunctionCall.Arguments.Nodes[i]);

                else if (FunctionCall.Arguments.Nodes[i] is FunctionCall)
                    Type = ResolveFunctionCallType((FunctionCall)FunctionCall.Arguments.Nodes[i]);

                else if (FunctionCall.Arguments.Nodes[i] is IdentifierExpression)
                    Type = ((IdentifierExpression)FunctionCall.Arguments.Nodes[i]).Type;

                else if (FunctionCall.Arguments.Nodes[i] is ConstExpression)
                    Type = ((ConstExpression)FunctionCall.Arguments.Nodes[i]).Type;

                if (ExpectedType != Type)
                    Diagnostics.SemanticErrors.Add(new IncompatibleArgumentType(Type, ExpectedType, FunctionCall.Arguments.Nodes[i].SourceContext));
            }

            return FunctionCall.Type;
        }


        public static void ResolveTypes(AbstractSyntaxTree Ast)
        {
            ErrorShownForBoolean = false;
            ErrorShownForBinary  = false;

            foreach (var Assign    in Ast.Root.GetChildsByType("AssignmentExpression", true)) ResolveAssignmentExpressionType((AssignmentExpression)Assign);
            foreach (var Condition in Ast.Root.GetChildsByType("Condition", true))            ResolveConditionType((Condition)Condition);
            foreach (var Return    in Ast.Root.GetChildsByType("ReturnExpression", true))     ResolveReturnExpressionType((ReturnExpression)Return);
            foreach (var Body      in Ast.Root.GetChildsByType("Body", true)) 
                foreach (var Functest in Body.GetChildsByType("FunctionCall"))
                    ResolveFunctionCallType((FunctionCall)Functest);
        }
    }
}
