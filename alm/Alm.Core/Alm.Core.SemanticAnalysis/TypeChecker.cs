using alm.Other.InnerTypes;
using alm.Core.Errors;
using alm.Core.SyntaxAnalysis;

using alm.Core.VariableTable;
using static alm.Other.Enums.Operator;

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
        private static InnerType ResolveConditionType(Condition condition)
        {
            InnerType ConditionType;
            InnerType ExpectedType = new Boolean();

            SyntaxTreeNode Node = condition.Nodes[0];

            if (Node is BooleanConst) return ((BooleanConst)Node).Type;

            if      (Node is BooleanExpression)    ConditionType = ResolveBooleanExpressionType((BooleanExpression)Node,false);
            else if (Node is IdentifierExpression) ConditionType = ((IdentifierExpression)Node).Type;
            else if (Node is FunctionCall)         ConditionType = ResolveFunctionCallType((FunctionCall)Node);
            else                                   ConditionType = ResolveNodeType(Node);

            if (ConditionType == ExpectedType) return ExpectedType;
            if (!ErrorShownForBoolean) Diagnostics.SemanticErrors.Add(new IncompatibleConditionType(ConditionType,Node.SourceContext));
            return ConditionType;
        }
        private static InnerType ResolveBooleanExpressionType(BooleanExpression booleanExpression, bool first = false)
        {
            InnerType LeftType;
            InnerType RightType;
            InnerType ExpectedType;

            if (first) ErrorShownForBoolean = false;

            switch (booleanExpression.Op)
            {
                case Less:
                case LessEqual:
                case Greater:
                case GreaterEqual:
                    ExpectedType = new NumericType();break;
                default:
                    ExpectedType = null;break;
            }    

            if      (booleanExpression.Left is BinaryExpression)   LeftType = ResolveBinaryExpressionType((BinaryExpression)booleanExpression.Left,false);
            else if (booleanExpression.Left is BooleanExpression)  LeftType = ResolveBooleanExpressionType((BooleanExpression)booleanExpression.Left,false);
            else if (booleanExpression.Left is FunctionCall)       LeftType = ResolveFunctionCallType((FunctionCall)booleanExpression.Left);
            else if (booleanExpression.Left is null)               LeftType = new Boolean();//for not operator 
            else                                                   LeftType = ResolveNodeType(booleanExpression.Left);

            if      (booleanExpression.Right is BinaryExpression)  RightType = ResolveBinaryExpressionType((BinaryExpression)booleanExpression.Right,false);
            else if (booleanExpression.Right is BooleanExpression) RightType = ResolveBooleanExpressionType((BooleanExpression)booleanExpression.Right,false);
            else if (booleanExpression.Right is FunctionCall)      RightType = ResolveFunctionCallType((FunctionCall)booleanExpression.Right);
            else                                                   RightType = ResolveNodeType(booleanExpression.Right);

            if (ExpectedType is null)
            {
                if (booleanExpression.Op == Equal || booleanExpression.Op == NotEqual)
                {
                    if (LeftType == RightType ) return new Boolean();
                    if (!ErrorShownForBoolean) Diagnostics.SemanticErrors.Add(new IncompatibleBooleanExpressionType(LeftType, RightType, booleanExpression.SourceContext));
                    ErrorShownForBoolean = true;
                    return RightType;
                }
                ExpectedType = new Boolean();
                if (LeftType == ExpectedType && RightType == ExpectedType) return new Boolean();

            }

            if (LeftType is NumericType && RightType is NumericType) return new Boolean();

            if (!(RightType is NumericType))
            {
                if (!ErrorShownForBoolean) Diagnostics.SemanticErrors.Add(new IncompatibleBooleanExpressionType(ExpectedType, RightType, booleanExpression.Right.SourceContext));
                ErrorShownForBoolean = true;
                return RightType;
            }
            else if (!(LeftType is NumericType))
            {
                if (!ErrorShownForBoolean) Diagnostics.SemanticErrors.Add(new IncompatibleBooleanExpressionType(ExpectedType, LeftType, booleanExpression.Left.SourceContext));
                ErrorShownForBoolean = true;
                return LeftType;
            }

            return new Underfined();
        }

        private static InnerType ResolveAssignmentExpressionType(AssignmentExpression assignmentExpression)
        {
            InnerType RightType;
            InnerType LeftType = ((IdentifierExpression)assignmentExpression.Left).Type;

            TryToCastAssignmentExpression(assignmentExpression);

            if      (assignmentExpression.Right is BinaryExpression)     RightType = ResolveBinaryExpressionType(((BinaryExpression)assignmentExpression.Right));
            else if (assignmentExpression.Right is FunctionCall)         RightType = ResolveFunctionCallType((FunctionCall)assignmentExpression.Right);
            else if (assignmentExpression.Right is IdentifierExpression) RightType = ((IdentifierExpression)assignmentExpression.Right).Type;
            else                                                         RightType = ResolveNodeType(assignmentExpression.Right);

            if (RightType == LeftType) return LeftType;
            Diagnostics.SemanticErrors.Add(new IncompatibleAssignmentType(RightType,LeftType,assignmentExpression.SourceContext));
            return new Underfined();
        }
        private static InnerType ResolveBinaryExpressionType(BinaryExpression binaryExpression,bool first = true)
        {
            InnerType LeftType;
            InnerType RightType;
            InnerType ExpectedType = new NumericType();

            //TryToCastBinaryExpression(binaryExpression);

            if (first) ErrorShownForBinary = false;

            if      (binaryExpression.Left is ConstExpression)      LeftType = ((ConstExpression)binaryExpression.Left).Type;
            else if (binaryExpression.Left is IdentifierExpression) LeftType = ((IdentifierExpression)binaryExpression.Left).Type;
            else if (binaryExpression.Left is FunctionCall)         LeftType = ResolveFunctionCallType((FunctionCall)binaryExpression.Left);
            else if (binaryExpression.Left is BinaryExpression)     LeftType = ResolveBinaryExpressionType((BinaryExpression)binaryExpression.Left,false);
            else                                                    LeftType = ResolveNodeType(binaryExpression.Left);

            if      (binaryExpression.Right is ConstExpression)      RightType = ((ConstExpression)binaryExpression.Right).Type;
            else if (binaryExpression.Right is IdentifierExpression) RightType = ((IdentifierExpression)binaryExpression.Right).Type;
            else if (binaryExpression.Right is FunctionCall)         RightType = ResolveFunctionCallType((FunctionCall)binaryExpression.Right);
            else if (binaryExpression.Right is BinaryExpression)     RightType = ResolveBinaryExpressionType((BinaryExpression)binaryExpression.Right,false);
            else                                                     RightType = ResolveNodeType(binaryExpression.Right);

            if (RightType is NumericType && LeftType is NumericType) return RightType;

            if (!(RightType is NumericType))
            {
                if (!ErrorShownForBinary) Diagnostics.SemanticErrors.Add(new IncompatibleBinaryExpressionType(ExpectedType, RightType,binaryExpression.SourceContext));
                ErrorShownForBinary = true;
                return RightType;
            }
            else if (!(LeftType is NumericType))
            {
                if (!ErrorShownForBinary) Diagnostics.SemanticErrors.Add(new IncompatibleBinaryExpressionType(ExpectedType, LeftType, binaryExpression.SourceContext));
                ErrorShownForBinary = true;
                return LeftType;
            }

            return new Underfined();
        }
        private static InnerType ResolveReturnExpressionType(ReturnExpression returnExpression)
        {
            InnerType ExpectedType = ((FunctionDeclaration)returnExpression.GetParentByType("FunctionDeclaration")).Type;
            InnerType RightType;

            TryToCastReturnExpression(returnExpression);

            if      (returnExpression.Right is ConstExpression)      RightType = ((ConstExpression)returnExpression.Right).Type;
            else if (returnExpression.Right is BinaryExpression)     RightType = ResolveBinaryExpressionType(((BinaryExpression)returnExpression.Right));
            else if (returnExpression.Right is FunctionCall)         RightType = ResolveFunctionCallType((FunctionCall)returnExpression.Right);
            else if (returnExpression.Right is IdentifierExpression) RightType = ((IdentifierExpression)returnExpression.Right).Type;
            else                                                     RightType = ResolveNodeType(returnExpression.Right);

            if (ExpectedType != RightType)
            {
                Diagnostics.SemanticErrors.Add(new IncompatibleReturnType(ExpectedType, RightType, returnExpression.SourceContext));
                return RightType;
            }
            return ExpectedType;
        }

        private static InnerType ResolveFunctionCallType(FunctionCall functionCall)
        {
            InnerType ExpectedType, Type = null;

            for (int i = 0; i < functionCall.Arguments.Nodes.Count; i++)
            {
                ExpectedType = GlobalTable.Table.FetchFunction(functionCall).Arguments[i].Type;

                if (functionCall.Arguments.Nodes[i] is BinaryExpression)
                    Type = ResolveBinaryExpressionType((BinaryExpression)functionCall.Arguments.Nodes[i]);

                else if (functionCall.Arguments.Nodes[i] is FunctionCall)
                    Type = ResolveFunctionCallType((FunctionCall)functionCall.Arguments.Nodes[i]);

                else if (functionCall.Arguments.Nodes[i] is IdentifierExpression)
                    Type = ((IdentifierExpression)functionCall.Arguments.Nodes[i]).Type;

                else if (functionCall.Arguments.Nodes[i] is ConstExpression)
                    Type = ((ConstExpression)functionCall.Arguments.Nodes[i]).Type;

                if (ExpectedType != Type)
                    Diagnostics.SemanticErrors.Add(new IncompatibleArgumentType(Type, ExpectedType, functionCall.Arguments.Nodes[i].SourceContext));
            }

            return functionCall.Type;
        }


        public static void ResolveTypes(AbstractSyntaxTree ast)
        {
            ErrorShownForBoolean = false;
            ErrorShownForBinary  = false;

            foreach (var Assign    in ast.Root.GetChildsByType("AssignmentExpression", true)) ResolveAssignmentExpressionType((AssignmentExpression)Assign);
            foreach (var Condition in ast.Root.GetChildsByType("Condition", true))            ResolveConditionType((Condition)Condition);
            foreach (var Return    in ast.Root.GetChildsByType("ReturnExpression", true))     ResolveReturnExpressionType((ReturnExpression)Return);
            foreach (var Body      in ast.Root.GetChildsByType("Body", true)) 
                foreach (var Func in Body.GetChildsByType("FunctionCall"))
                {
                    TryToCastFunctionArguments((FunctionCall)Func);
                    ResolveFunctionCallType((FunctionCall)Func);
                }
        }

        private static bool NeedCastToFloat(Expression expression)
        {
            bool needCast = false;

            foreach (var var in expression.GetChildsByType("IdentifierCall", true))
                if (((IdentifierCall)var).Type is Float)
                    needCast = true;

            foreach (var func in expression.GetChildsByType("FunctionCall", true))
                if (((FunctionCall)func).Type is Float)
                    needCast = true;

            if (expression.GetChildsByType("FloatConst", true).Length > 0)
                needCast = true;

            return needCast ? true : false; 
        }

        private static void TryToCastBinaryExpression(BinaryExpression binaryExpression, bool castItAnyway = false)
        {
            bool needCast;
            if (castItAnyway)
                needCast = true;
            else needCast = NeedCastToFloat(binaryExpression);

            if (!needCast) return;

            if (binaryExpression.Left is IntegerConst)
                TryToCastIntegerConst(binaryExpression, (IntegerConst)binaryExpression.Left, true);

            else if (binaryExpression.Left is FunctionCall)
                TryToCastFunctionCall(binaryExpression, (FunctionCall)binaryExpression.Left, true);

            else if (binaryExpression.Left is IdentifierCall)
                TryToCastIdentifierCall(binaryExpression, (IdentifierCall)binaryExpression.Left, true);

            else if (binaryExpression.Left is BinaryExpression)
                TryToCastBinaryExpression((BinaryExpression)binaryExpression.Left, castItAnyway);

            if (binaryExpression.Right is IntegerConst)
                TryToCastIntegerConst(binaryExpression, (IntegerConst)binaryExpression.Right);

            else if (binaryExpression.Right is FunctionCall)
                TryToCastFunctionCall(binaryExpression, (FunctionCall)binaryExpression.Right);

            else if (binaryExpression.Right is IdentifierCall)
                TryToCastIdentifierCall(binaryExpression, (IdentifierCall)binaryExpression.Right);

            else if (binaryExpression.Right is BinaryExpression)
                TryToCastBinaryExpression((BinaryExpression)binaryExpression.Right,castItAnyway);
        }

        private static void TryToCastAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            IdentifierExpression identifierDeclaration = (IdentifierExpression)assignmentExpression.Left;
            if (identifierDeclaration.Type is Float)
            {
                if (assignmentExpression.Right is IntegerConst)
                    TryToCastIntegerConst(assignmentExpression, (IntegerConst)assignmentExpression.Right);
                else if (assignmentExpression.Right is BinaryExpression)
                    TryToCastBinaryExpression((BinaryExpression)assignmentExpression.Right,true);
                else if (assignmentExpression.Right is IdentifierCall)
                    TryToCastIdentifierCall(assignmentExpression, (IdentifierCall)assignmentExpression.Right);
                else if (assignmentExpression.Right is FunctionCall)
                    TryToCastFunctionCall(assignmentExpression, (FunctionCall)assignmentExpression.Right);
            }
        }

        private static void TryToCastReturnExpression(ReturnExpression returnExpression)
        {
            FunctionDeclaration func = (FunctionDeclaration)returnExpression.GetParentByType("FunctionDeclaration");
            if (func.Type is Float)
            {
                if (returnExpression.Right is IntegerConst)
                    TryToCastIntegerConst(returnExpression, (IntegerConst)returnExpression.Right);
                else if (returnExpression.Right is BinaryExpression)
                    TryToCastBinaryExpression((BinaryExpression)returnExpression.Right,true);
                else if (returnExpression.Right is IdentifierCall)
                    TryToCastIdentifierCall(returnExpression, (IdentifierCall)returnExpression.Right);
                else if (returnExpression.Right is FunctionCall)
                    TryToCastFunctionCall(returnExpression, (FunctionCall)returnExpression.Right);
            }
        }

        private static void TryToCastFunctionArguments(FunctionCall functionCall)
        {
            if (!NeedCastToFloat(functionCall)) return;
            for (int i = 0; i < functionCall.Arguments.Nodes.Count; i++)
            {
                SyntaxTreeNode arg = functionCall.Arguments.Nodes[i];
                if (arg is IntegerConst)
                    TryToCastIntegerConst(functionCall.Arguments, (IntegerConst)arg, i);
                else if (arg is BinaryExpression)
                    TryToCastBinaryExpression((BinaryExpression)arg, true);
                else if (arg is FunctionCall)
                    TryToCastFunctionCall(functionCall.Arguments, (FunctionCall)arg, i);
                else if (arg is IdentifierCall)
                    TryToCastIdentifierCall(functionCall.Arguments, (IdentifierCall)arg, i);
            }
        }

        private static void TryToCastFunctionCall(BinaryExpression binaryExpression, FunctionCall functionCall, bool left = false)
        {
            TryToCastFunctionArguments(functionCall);
            if (!(functionCall.Type is Integer32)) return;
            int index = left ? 0 : 1;
            FunctionCall pointFucntionCall = new FunctionCall("point", new Arguments(functionCall), functionCall.SourceContext);
            pointFucntionCall.Type = new Float();
            if (left)
                binaryExpression.Left = pointFucntionCall;
            else
                binaryExpression.Right = pointFucntionCall;
            binaryExpression.Nodes[index] = pointFucntionCall;
        }

        private static void TryToCastFunctionCall(Expression expression, FunctionCall functionCall)
        {
            TryToCastFunctionArguments(functionCall);
            if (!(functionCall.Type is Integer32)) return;
            FunctionCall pointFucntionCall = new FunctionCall("point", new Arguments(functionCall), functionCall.SourceContext);
            pointFucntionCall.Type = new Float();
            expression.Right = pointFucntionCall;
            expression.Nodes[expression.Nodes.Count - 1] = pointFucntionCall;
        }

        private static void TryToCastFunctionCall(Arguments arguments, FunctionCall functionCall, int index)
        {
            TryToCastFunctionArguments(functionCall);
            if (!(functionCall.Type is Integer32)) return;
            FunctionCall pointFucntionCall = new FunctionCall("point", new Arguments(functionCall), functionCall.SourceContext);
            pointFucntionCall.Type = new Float();
            arguments.Nodes[index] = pointFucntionCall;
        }

        private static void TryToCastIdentifierCall(BinaryExpression binaryExpression, IdentifierCall identifierCall, bool left = false)
        {
            if (!(identifierCall.Type is Integer32)) return;
            int index = left ? 0 : 1;
            FunctionCall pointFucntionCall = new FunctionCall("point", new Arguments(identifierCall), identifierCall.SourceContext);
            pointFucntionCall.Type = new Float();
            if (left)
                binaryExpression.Left = pointFucntionCall;
            else
                binaryExpression.Right = pointFucntionCall;
            binaryExpression.Nodes[index] = pointFucntionCall;
        }

        private static void TryToCastIdentifierCall(Expression expression, IdentifierCall identifierCall)
        {
            if (!(identifierCall.Type is Integer32)) return;
            FunctionCall pointFunctionCall = new FunctionCall("point", new Arguments(identifierCall), identifierCall.SourceContext);
            pointFunctionCall.Type = new Float();
            expression.Right = pointFunctionCall;
            expression.Nodes[expression.Nodes.Count - 1] = pointFunctionCall;
        }

        private static void TryToCastIdentifierCall(Arguments arguments, IdentifierCall identifierCall, int index)
        {
            if (!(identifierCall.Type is Integer32)) return;
            FunctionCall pointFunctionCall = new FunctionCall("point", new Arguments(identifierCall), identifierCall.SourceContext);
            pointFunctionCall.Type = new Float();
            arguments.Nodes[index] = pointFunctionCall;
        }

        private static void TryToCastIntegerConst(BinaryExpression binaryExpression, IntegerConst integerConst, bool left = false)
        {
            int index = left ? 0 : 1;
            FloatConst floatConst = new FloatConst(integerConst.Value + ",0");
            floatConst.Parent = integerConst.Parent;
            floatConst.SourceContext = integerConst.SourceContext;
            if (left)
                binaryExpression.Left = floatConst;
            else
                binaryExpression.Right = floatConst;
            binaryExpression.Nodes[index] = floatConst;
        }

        private static void TryToCastIntegerConst(Expression expression, IntegerConst integerConst)
        {
            FloatConst floatConst = new FloatConst(integerConst.Value + ",0");
            floatConst.Parent = integerConst.Parent;
            floatConst.SourceContext = integerConst.SourceContext;
            expression.Right = floatConst;
            expression.Nodes[expression.Nodes.Count-1] = floatConst;
        }

        private static void TryToCastIntegerConst(Arguments arguments, IntegerConst integerConst, int index)
        {
            FloatConst floatConst = new FloatConst(integerConst.Value + ",0");
            floatConst.Parent = integerConst.Parent;
            floatConst.SourceContext = integerConst.SourceContext;
            arguments.Nodes[index] = floatConst;
        }
    }
}
