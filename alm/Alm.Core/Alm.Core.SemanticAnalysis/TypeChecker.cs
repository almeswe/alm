using alm.Other.InnerTypes;
using alm.Core.Errors;
using alm.Core.SyntaxAnalysis;

using alm.Core.VariableTable;
using static alm.Other.Enums.Operator;
using static alm.Other.String.StringMethods;

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

            ConditionType = ResolveExpressionType((Expression)condition.Nodes[0]);

            if (ConditionType == ExpectedType) return ExpectedType;
            if (!ErrorShownForBoolean) Diagnostics.SemanticErrors.Add(new IncompatibleConditionType(Node.SourceContext));
            return ConditionType;
        }

        private static InnerType ResolveEqualityType(BooleanExpression booleanExpression, bool first = false, bool alreadyCasted = false)
        {
            InnerType LeftType = ResolveExpressionType((Expression)booleanExpression.Left);
            InnerType RightType = ResolveExpressionType((Expression)booleanExpression.Right);

            if (LeftType == RightType)
                return new Boolean();
            else
            {
                if (!alreadyCasted)
                {
                    TypeCaster.CastBooleanExpression(booleanExpression, TypeCaster.HigherPriorityType(RightType, LeftType), TypeCaster.DefineCastCase(RightType, LeftType));
                    return ResolveEqualityType(booleanExpression, first, true);
                }
                if (!ErrorShownForBoolean) Diagnostics.SemanticErrors.Add(new IncompatibleBooleanExpressionType(LeftType, RightType, booleanExpression.SourceContext));
                ErrorShownForBoolean = true;
                return LeftType;
            }
        }

        private static InnerType ResolveRelationType(BooleanExpression booleanExpression, bool first = false, bool alreadyCasted = false)
        {
            InnerType LeftType  = ResolveExpressionType((Expression)booleanExpression.Left);
            InnerType RightType = ResolveExpressionType((Expression)booleanExpression.Right);

            if (LeftType is NumericType && RightType is NumericType)
                if (RightType == LeftType)
                    return new Boolean();
                else
                {
                    if (!alreadyCasted)
                    {
                        TypeCaster.CastBooleanExpression(booleanExpression, TypeCaster.HigherPriorityType(RightType, LeftType), TypeCaster.DefineCastCase(RightType, LeftType));
                        return ResolveRelationType(booleanExpression, first, true);
                    }
                    if (!ErrorShownForBinary) Diagnostics.SemanticErrors.Add(new IncompatibleBooleanExpressionType(LeftType, RightType, booleanExpression.SourceContext));
                    ErrorShownForBinary = true;
                    return LeftType;
                }

            if (!(RightType is NumericType))
            {
                if (!ErrorShownForBoolean) Diagnostics.SemanticErrors.Add(new IncompatibleBooleanExpressionType(RightType, booleanExpression.Right.SourceContext));
                ErrorShownForBoolean = true;
                return RightType;
            }
            if (!(LeftType is NumericType))
            {
                if (!ErrorShownForBoolean) Diagnostics.SemanticErrors.Add(new IncompatibleBooleanExpressionType(LeftType, booleanExpression.Left.SourceContext));
                ErrorShownForBoolean = true;
                return LeftType;
            }
            return new Underfined();
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
                    ExpectedType = new Int32();break;
                default:
                    ExpectedType = null;break;
            }

            if (booleanExpression.Left is null) LeftType = new Boolean();//for not operator
            else                                LeftType = ResolveExpressionType((Expression)booleanExpression.Left);

            RightType = ResolveExpressionType((Expression)booleanExpression.Right);

            if (ExpectedType is null)
            {
                if (booleanExpression.Op == Equal || booleanExpression.Op == NotEqual)
                    return ResolveEqualityType(booleanExpression,first);
                ExpectedType = new Boolean();
                if (LeftType == ExpectedType && RightType == ExpectedType) return new Boolean();
            }
            return ResolveRelationType(booleanExpression, first);
        }

        private static InnerType ResolveBinaryExpressionType(BinaryExpression binaryExpression,bool first = true,bool alreadyCasted = false)
        {
            InnerType LeftType;
            InnerType RightType;

            if (first) ErrorShownForBinary = false;

            LeftType  = ResolveExpressionType((Expression)binaryExpression.Left);
            RightType = ResolveExpressionType((Expression)binaryExpression.Right);


            if (RightType is NumericType && LeftType is NumericType) 
                if (RightType == LeftType)
                    return RightType;
                else
                {
                    if (!alreadyCasted)
                    {
                        TypeCaster.CastBinaryExpression(binaryExpression,TypeCaster.HigherPriorityType(RightType,LeftType),TypeCaster.DefineCastCase(RightType, LeftType));
                        return ResolveBinaryExpressionType(binaryExpression,first,true);
                    }
                    if (!ErrorShownForBinary) Diagnostics.SemanticErrors.Add(new IncompatibleBinaryExpressionType(LeftType, RightType, binaryExpression.SourceContext));
                    ErrorShownForBinary = true;
                    return LeftType;
                }

            if (!(RightType is NumericType))
            {
                if (!ErrorShownForBinary) Diagnostics.SemanticErrors.Add(new IncompatibleBinaryExpressionType(RightType,binaryExpression.Right.SourceContext));
                ErrorShownForBinary = true;
                return RightType;
            }
            if (!(LeftType is NumericType))
            {
                if (!ErrorShownForBoolean) Diagnostics.SemanticErrors.Add(new IncompatibleBinaryExpressionType(LeftType, binaryExpression.Left.SourceContext));
                ErrorShownForBoolean = true;
                return LeftType;
            }
            return new Underfined();
        }

        public static InnerType ResolveExpressionType(Expression expression)
        {
            if      (expression is ConstExpression)      return ((ConstExpression)expression).Type;
            else if (expression is IdentifierExpression) return ((IdentifierExpression)expression).Type;
            else if (expression is FunctionCall)         return ResolveFunctionCallType((FunctionCall)expression);
            else if (expression is BinaryExpression)     return ResolveBinaryExpressionType((BinaryExpression)expression, false);
            else if (expression is BooleanExpression)    return ResolveBooleanExpressionType((BooleanExpression)expression,false);
            else                                         return ResolveNodeType(expression);
        }

        private static InnerType ResolveAssignmentExpressionType(AssignmentExpression assignmentExpression, bool alreadyCasted = false)
        {
            InnerType RightType;
            InnerType LeftType = ((IdentifierExpression)assignmentExpression.Left).Type;

            RightType = ResolveExpressionType((Expression)assignmentExpression.Right);

            if (RightType == LeftType)
                return LeftType;
            else
            {
                if (!alreadyCasted)
                {
                    TypeCaster.CastAssignmentExpression(assignmentExpression, RightType, LeftType);
                    return ResolveAssignmentExpressionType(assignmentExpression, true);
                }
            }
            Diagnostics.SemanticErrors.Add(new IncompatibleAssignmentType(RightType, LeftType, assignmentExpression.SourceContext));
            return new Underfined();
        }

        private static InnerType ResolveReturnExpressionType(ReturnExpression returnExpression, bool alreadyCasted = false)
        {
            InnerType ExpectedType = ((FunctionDeclaration)returnExpression.GetParentByType("FunctionDeclaration")).Type;
            InnerType RightType;

            if (returnExpression.Right == null)
                RightType = new Void();
            else RightType = ResolveExpressionType((Expression)returnExpression.Right);

            if (ExpectedType != RightType)
            {
                if (!alreadyCasted)
                {
                    TypeCaster.CastReturnExpression(returnExpression, RightType, ExpectedType);
                    return ResolveReturnExpressionType(returnExpression, true);
                }

                Diagnostics.SemanticErrors.Add(new IncompatibleReturnType(ExpectedType, RightType, returnExpression.SourceContext));
                return RightType;
            }
            return ExpectedType;
        }

        private static InnerType ResolveFunctionCallType(FunctionCall functionCall)
        {
            InnerType ExpectedType, Type = null;

            int i = 0;
            bool alreadyCasted = false;

            while (i < functionCall.ArgumentsValues.Nodes.Count)
            {
                ExpectedType = GlobalTable.Table.FetchFunction(functionCall.Name,functionCall.ArgumentCount).Arguments[i].Type;

                Type = ResolveExpressionType((Expression)functionCall.ArgumentsValues.Nodes[i]);

                if (ExpectedType != Type)
                {
                    if (!alreadyCasted)
                    {
                        TypeCaster.CastFunctionArgument((Expression)functionCall.ArgumentsValues.Nodes[i], Type, ExpectedType);
                        alreadyCasted = true;
                        i--;
                    }
                    else
                        Diagnostics.SemanticErrors.Add(new IncompatibleArgumentType(Type, ExpectedType, functionCall.ArgumentsValues.Nodes[i].SourceContext));
                }
                else
                    alreadyCasted = false;
                i++;
            }

            return functionCall.Type;
        }


        public static void ResolveTypes(AbstractSyntaxTree ast)
        {
            ErrorShownForBoolean = false;
            ErrorShownForBinary  = false;

            foreach (var Assign in ast.Root.GetChildsByType("AssignmentExpression", true)) 
                ResolveAssignmentExpressionType((AssignmentExpression)Assign);

            foreach (var Condition in ast.Root.GetChildsByType("Condition", true))            
                ResolveConditionType((Condition)Condition);

            foreach (var Return in ast.Root.GetChildsByType("ReturnExpression", true))     
                ResolveReturnExpressionType((ReturnExpression)Return);

            foreach (var Body in ast.Root.GetChildsByType("Body", true)) 
                foreach (var Func in Body.GetChildsByType("FunctionCall"))
                    ResolveFunctionCallType((FunctionCall)Func);
        }
    }

    public sealed class TypeCaster
    {
        public enum CastCase
        {
            IntegerToFloat,
            CharToInteger,
            Int32ToFloat64,
            Int64ToFloat32,
            Int64ToFloat64,
            Float32ToFloat64,
            Int32ToInt64,
            Undefined,
        }

        public static bool CanCast(InnerType fType, InnerType sType, bool bothCases = true)
        {
            if (fType == sType) return false;
            if (fType is NumericType && sType is NumericType)
            {
                NumericType toC = (NumericType)sType;
                NumericType fromC = (NumericType)fType;

                if (bothCases)
                {
                    if (fromC.CanCast(toC) || toC.CanCast(fromC))
                        return true;
                }
                else
                    if (fromC.CanCast(toC))
                    return true;
                return false;
            }
            else return false;
        }

        public static InnerType HigherPriorityType(InnerType ftype, InnerType stype)
        {
            if (ftype is NumericType && stype is NumericType)
            {
                NumericType ftypeC = (NumericType)ftype;
                NumericType stypeC = (NumericType)stype;
                if (ftypeC.CastPriority > stypeC.CastPriority)
                    return ftype;
                else return stype;
            }
            else return null;
        }

        public static CastCase DefineCastCase(InnerType ftype, InnerType stype)
        {
            if (CanCast(ftype, stype))
            {
                string castCaseStr;
                if (HigherPriorityType(ftype, stype) == ftype)
                    castCaseStr = UpperCaseFirstChar(stype.Representation) + "To" + UpperCaseFirstChar(ftype.Representation);
                else castCaseStr = UpperCaseFirstChar(ftype.Representation) + "To" + UpperCaseFirstChar(stype.Representation);
                foreach (CastCase castCase in System.Enum.GetValues(typeof(CastCase)))
                    if (castCase.ToString() == castCaseStr)
                        return castCase;
                return CastCase.Undefined;
            }
            return CastCase.Undefined;
        }

        public static void CastAssignmentExpression(AssignmentExpression assignmentExpression, InnerType expressionType, InnerType expectedType)
        {
            if (CanCast(expressionType, expectedType, false))
            {
                CastCase castCase = DefineCastCase(expressionType, expectedType);
                CastExpression((Expression)assignmentExpression.Right, expectedType, castCase);
            }
        }

        public static void CastReturnExpression(ReturnExpression returnExpression, InnerType expressionType, InnerType expectedType)
        {
            if (CanCast(expressionType, expectedType, false))
            {
                CastCase castCase = DefineCastCase(expressionType, expectedType);
                CastExpression((Expression)returnExpression.Right, expectedType, castCase);
            }
        }

        public static void CastExpression(SyntaxTreeNode expression, InnerType toType, CastCase castCase)
        {
            if (expression is ConstExpression)
                CastConstExpression(expression.Parent, (ConstExpression)expression, toType, castCase);
            else if (expression is IdentifierCall)
                CastIdentifierCall(expression.Parent, (IdentifierCall)expression, toType, castCase);
            else if (expression is FunctionCall)
                CastFunctionCall(expression.Parent, (FunctionCall)expression, toType, castCase);
            else if (expression is BinaryExpression)
                CastBinaryExpression((BinaryExpression)expression, toType, castCase);
        }

        public static void CastBinaryExpression(BinaryExpression binaryExpression, InnerType toType, CastCase castCase)
        {
            CastExpression(binaryExpression.Left, toType, castCase);
            CastExpression(binaryExpression.Right, toType, castCase);
        }

        public static void CastBooleanExpression(BooleanExpression booleanExpression, InnerType toType, CastCase castCase)
        {
            CastExpression(booleanExpression.Left, toType, castCase);
            CastExpression(booleanExpression.Right, toType, castCase);
        }

        public static void CastConstExpression(SyntaxTreeNode parent, ConstExpression constExpression, InnerType toType, CastCase castCase)
        {
            if (!CanCast(constExpression.Type, toType, false))
                return;

            FunctionCall pointFunctionCall = new FunctionCall(GetCastFunctionName(castCase) , new Arguments(constExpression), constExpression.SourceContext);
            pointFunctionCall.Type = toType;
            Replace(parent, constExpression, pointFunctionCall);
        }

        public static void CastIdentifierCall(SyntaxTreeNode parent, IdentifierCall identifierCall, InnerType toType, CastCase castCase)
        {
            if (!CanCast(identifierCall.Type, toType, false))
                return;

            FunctionCall pointFunctionCall = new FunctionCall(GetCastFunctionName(castCase), new Arguments(identifierCall), identifierCall.SourceContext);
            pointFunctionCall.Type = toType;
            Replace(parent,identifierCall,pointFunctionCall);
        }

        public static void CastFunctionCall(SyntaxTreeNode parent, FunctionCall functionCall, InnerType toType, CastCase castCase)
        {
            if (!CanCast(functionCall.Type, toType, false))
                return;

            FunctionCall pointFunctionCall = new FunctionCall(GetCastFunctionName(castCase), new Arguments(functionCall), functionCall.SourceContext);
            pointFunctionCall.Type = toType;
            Replace(parent, functionCall, pointFunctionCall);
        }

        public static void CastFunctionArgument(Expression argument,InnerType expressionType,InnerType argumentType)
        {
            if (!CanCast(expressionType, argumentType, false))
                return;
            CastCase castCase = DefineCastCase(expressionType, argumentType);
            CastExpression(argument,argumentType,castCase);
        }

        public static void Replace(SyntaxTreeNode parent, SyntaxTreeNode replaceThis, SyntaxTreeNode addThis)
        {
            if (parent is Expression)
            {
                Expression expression = (Expression)parent;
                if (expression.Left == replaceThis)
                    ((Expression)parent).Left = addThis;
                if (expression.Right == replaceThis)
                    ((Expression)parent).Right = addThis;
            }

            for (int i = 0; i < parent.Nodes.Count; i++)
                if (parent.Nodes[i] == replaceThis)
                    parent.Nodes[i] = addThis;
        }

        public static string GetCastFunctionName(CastCase castCase)
        {
            switch(castCase)
            {
                case CastCase.IntegerToFloat:
                    return "point";
                case CastCase.CharToInteger:
                    return "chartoint32";
                default: throw new System.Exception("??");
            }
        }
    }
}