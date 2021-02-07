using alm.Core.Errors;
using alm.Core.Table;
using alm.Core.SyntaxTree;
using alm.Core.InnerTypes;

using alm.Other.Enums;

using static alm.Core.Compiler.Compiler;
using static alm.Other.String.StringMethods;

namespace alm.Core.FrontEnd.SemanticAnalysis
{
    public sealed class TypeChecker
    {
        public static bool ReportErrors;

        private static bool ErrorInArithReported, ErrorInBooleanReported;

        public static void ResolveModuleTypes(SyntaxTreeNode moduleRoot)
        {
            ReportErrors = true;
            ErrorInArithReported = false;
            ErrorInBooleanReported = false;
            ResolveReturnStatemetsTypes(moduleRoot);
            ResolveAssignmentStatementsTypes(moduleRoot);
            ResolveConditionExpressionsTypes(moduleRoot);
            ResolveMethodInvokationStatementsTypes(moduleRoot);
        }

        public static InnerType ResolveAdressorExpressionType(Expression adressor)
        {
            if (adressor is IdentifierExpression)
                return ResolveIdentifierExpressionType((IdentifierExpression)adressor);
            else
                return ResolveArrayElementExpressionType((ArrayElementExpression)adressor);
        }
        public static InnerType ResolveExpressionType(Expression expression)
        {
            switch (expression.NodeKind)
            {
                case NodeType.BinaryArithExpression:
                    return ResolveBinaryArithExpressionType((BinaryArithExpression)expression);
                case NodeType.UnaryArithExpression:
                    return ResolveUnaryArithExpressionType((UnaryArithExpression)expression);
                case NodeType.BinaryBooleanExpression:
                    return ResolveBinaryBooleanExpressionType((BinaryBooleanExpression)expression);
                case NodeType.UnaryBooleanExpression:
                    return ResolveUnaryBooleanExpressionType((UnaryBooleanExpression)expression);

                case NodeType.Identifier:
                    return ResolveIdentifierExpressionType((IdentifierExpression)expression);

                case NodeType.IntegerConstant:
                case NodeType.RealConstant:
                case NodeType.CharConstant:
                case NodeType.BooleanConstant:
                case NodeType.StringConstant:
                    return ResolveConstantExpressionType((ConstantExpression)expression);

                case NodeType.MethodInvokation:
                    return ResolveMethodInvokationExpressionType((MethodInvokationExpression)expression);

                case NodeType.ArrayElement:
                    return ResolveArrayElementExpressionType((ArrayElementExpression)expression);
                case NodeType.ArrayInstance:
                    return ResolveArrayInstanceType((ArrayInstance)expression);

                default:
                    throw new System.Exception();
            }
        }
        public static InnerType ResolveArrayInstanceType(ArrayInstance arrayInstance)
        {
            return arrayInstance.Type;
        }
        public static InnerType ResolveMethodInvokationExpressionType(MethodInvokationExpression methodInvokation)
        {
            ResolveMethodsParametersTypes(methodInvokation);
            return methodInvokation.ReturnType;
        }

        public static InnerType ResolveMethodParametersTypes(ParameterDeclaration parameter)
        {
            return ResolveExpressionType(parameter.ParameterInstance);
        }

        public static InnerType ResolveIdentifierExpressionType(IdentifierExpression identifier)
        {
            return identifier.Type;
        }
        public static InnerType ResolveArrayElementExpressionType(ArrayElementExpression arrayElement)
        {
            return arrayElement.Type;
        }
        public static InnerType ResolveConstantExpressionType(ConstantExpression constant)
        {
            return constant.Type;
        }

        public static InnerType ResolveBinaryArithExpressionType(BinaryArithExpression binaryArithExpression)
        {
            InnerType LOperandType = ResolveExpressionType(binaryArithExpression.LeftOperand);
            InnerType ROperandType = ResolveExpressionType(binaryArithExpression.RightOperand);

            switch (binaryArithExpression.OperatorKind)
            {
                case BinaryExpression.BinaryOperator.FDiv:
                case BinaryExpression.BinaryOperator.IDiv:
                    if (LOperandType is NumericType && ROperandType is NumericType)
                        return binaryArithExpression.OperatorKind == BinaryExpression.BinaryOperator.IDiv ? (NumericType)new Int32() : (NumericType)new Single();
                    ReportErrorInArithExpression(new OperatorWithWrongOperandTypes($"Оператор [{binaryArithExpression.GetOperatorRepresentation()}] должен использоваться с операндами числового типа",binaryArithExpression.SourceContext));
                    if (!(LOperandType is NumericType))
                        return LOperandType;
                    else
                        return ROperandType;

                case BinaryExpression.BinaryOperator.Power:
                    if (LOperandType is NumericType && ROperandType is NumericType)
                        return new Single();
                    ReportErrorInArithExpression(new OperatorWithWrongOperandTypes($"Оператор [**] должен использоваться с операндами числового типа", binaryArithExpression.SourceContext));
                    if (!(LOperandType is NumericType))
                        return LOperandType;
                    else
                        return ROperandType;

                case BinaryExpression.BinaryOperator.Mult:
                case BinaryExpression.BinaryOperator.Substraction:
                    if (LOperandType is Char && ROperandType is Char)
                        return new Int32();
                    if (LOperandType is NumericType && ROperandType is NumericType)
                        return TypeCaster.HigherPriorityType(LOperandType, ROperandType);
                    ReportErrorInArithExpression(new OperatorWithWrongOperandTypes($"Оператор [{binaryArithExpression.GetOperatorRepresentation()}] должен использоваться с операндами числового типа", binaryArithExpression.SourceContext));
                    if (!(LOperandType is NumericType))
                        return LOperandType;
                    else
                        return ROperandType;

                case BinaryExpression.BinaryOperator.Addition:
                    if (LOperandType is Char && ROperandType is Char)
                        return new Int32();
                    if ((LOperandType is NumericType && ROperandType is NumericType) | (LOperandType is String && ROperandType is String))
                        if (LOperandType is String)
                            return new String();
                        else
                            return TypeCaster.HigherPriorityType(LOperandType, ROperandType);
                    ReportErrorInArithExpression(new OperatorWithWrongOperandTypes($"Оператор [+] должен использоваться с операндами числового типа, или для конкатенации строк", binaryArithExpression.SourceContext));
                    if (!(LOperandType is NumericType) && !(LOperandType is String))
                        return LOperandType;
                    else
                        return ROperandType;


                case BinaryExpression.BinaryOperator.LShift:
                case BinaryExpression.BinaryOperator.RShift:
                case BinaryExpression.BinaryOperator.BitwiseOr:
                case BinaryExpression.BinaryOperator.BitwiseAnd:
                case BinaryExpression.BinaryOperator.BitwiseXor:
                    if (LOperandType is IntegralType && ROperandType is IntegralType)
                        return TypeCaster.HigherPriorityType(LOperandType, ROperandType);
                    ReportErrorInArithExpression(new OperatorWithWrongOperandTypes($"Оператор [{binaryArithExpression.GetOperatorRepresentation()}] должен использоваться с операндами целочисленного типа", binaryArithExpression.SourceContext));
                    if (!(LOperandType is IntegralType))
                        return LOperandType;
                    else
                        return ROperandType;

                default:
                    return new Undefined();
            }
        }
        public static InnerType ResolveUnaryArithExpressionType(UnaryArithExpression unaryArithExpression)
        {
            InnerType OperandType = ResolveExpressionType(unaryArithExpression.Operand);

            //try to cast
            switch (unaryArithExpression.OperatorKind)
            {
                case UnaryArithExpression.UnaryOperator.UnaryMinus:
                    if (OperandType is NumericType)
                        return OperandType;
                    ReportErrorInArithExpression(new SemanticErrorMessage($"Оператор унарного минуса должен использоваться с операндом числового типа", unaryArithExpression.SourceContext));
                    return OperandType;

                default:
                    return new Undefined();
            }
        }

        public static InnerType ResolveBinaryBooleanExpressionType(BinaryBooleanExpression binaryBooleanExpression)
        {
            InnerType LOperandType = ResolveExpressionType(binaryBooleanExpression.LeftOperand);
            InnerType ROperandType = ResolveExpressionType(binaryBooleanExpression.RightOperand);

            switch (binaryBooleanExpression.OperatorKind)
            {
                case BinaryExpression.BinaryOperator.Equal:
                case BinaryExpression.BinaryOperator.NotEqual:
                    if ((LOperandType is PrimitiveType && ROperandType is PrimitiveType) || (LOperandType is String && ROperandType is String))
                        return new Boolean();
                    if (LOperandType != ROperandType)
                    {
                        ReportErrorInBooleanExpression(new OperatorWithWrongOperandTypes($"Оператор [{binaryBooleanExpression.GetOperatorRepresentation()}] должен использоваться с операндами одного типа",binaryBooleanExpression.SourceContext));
                        return LOperandType;
                    }
                    ReportErrorInBooleanExpression(new OperatorWithWrongOperandTypes($"Оператор [{binaryBooleanExpression.GetOperatorRepresentation()}] должен использоваться с операндами примитивного типа", binaryBooleanExpression.SourceContext));
                    if (!(LOperandType is PrimitiveType) | !(LOperandType is String))
                        return LOperandType;
                    else 
                        return ROperandType;

                case BinaryExpression.BinaryOperator.Conjuction:
                case BinaryExpression.BinaryOperator.Disjunction:
                case BinaryExpression.BinaryOperator.StrictDisjunction:
                    if (LOperandType is Boolean && ROperandType is Boolean)
                        return new Boolean();
                    ReportErrorInBooleanExpression(new OperatorWithWrongOperandTypes($"Оператор [{binaryBooleanExpression.GetOperatorRepresentation()}] должен использоваться с операндами логического типа", binaryBooleanExpression.SourceContext));
                    if (!(LOperandType is Boolean))
                        return LOperandType;
                    else
                        return ROperandType;

                case BinaryExpression.BinaryOperator.LessThan:
                case BinaryExpression.BinaryOperator.GreaterThan:
                case BinaryExpression.BinaryOperator.LessEqualThan:
                case BinaryExpression.BinaryOperator.GreaterEqualThan:
                    if (LOperandType is NumericType && ROperandType is NumericType)
                        return new Boolean();
                    ReportErrorInBooleanExpression(new OperatorWithWrongOperandTypes($"Оператор [{binaryBooleanExpression.GetOperatorRepresentation()}] должен использоваться с операндами числового типа", binaryBooleanExpression.SourceContext));
                    if (!(LOperandType is NumericType))
                        return LOperandType;
                    else
                        return ROperandType;

                default:
                    return new Undefined();
            }
        }
        public static InnerType ResolveUnaryBooleanExpressionType(UnaryBooleanExpression unaryBooleanExpression)
        {
            InnerType OperandType = ResolveExpressionType(unaryBooleanExpression.Operand);

            //try to cast
            switch (unaryBooleanExpression.OperatorKind)
            {
                case UnaryArithExpression.UnaryOperator.UnaryInversion:
                    if (OperandType is Boolean)
                        return OperandType;
                    ReportErrorInBooleanExpression(new SemanticErrorMessage($"Оператор инвертирования должен использоваться с операндом логического типа",unaryBooleanExpression.SourceContext));
                    return OperandType;

                default:
                    return new Undefined();
            }
        }

        public static void ResolveConditionExpressionsTypes(SyntaxTreeNode inNode)
        {
            foreach (IfStatement ifStatement in inNode.GetChildsByType(typeof(IfStatement), true))
                ResolveConditionExpressionType(ifStatement.Condition);
            foreach (WhileLoopStatement whileStatement in inNode.GetChildsByType(typeof(WhileLoopStatement), true))
                ResolveConditionExpressionType(whileStatement.Condition);
            foreach (DoLoopStatement doStatement in inNode.GetChildsByType(typeof(DoLoopStatement), true))
                ResolveConditionExpressionType(doStatement.Condition);
            foreach (ForLoopStatement forStatement in inNode.GetChildsByType(typeof(ForLoopStatement), true))
                ResolveConditionExpressionType(forStatement.Condition);
        }
        public static void ResolveConditionExpressionType(Expression condition)
        {
            TypeCaster.TryToCastConditionExpression(condition);
            ReportErrors = true;
            InnerType ConditionType = ResolveExpressionType(condition);
            if (!(ConditionType is Boolean))
                ReportErrorInBooleanExpression(new IncompatibleConditionType(condition.SourceContext));
        }
        public static void ResolveAssignmentStatementsTypes(SyntaxTreeNode inNode)
        {
            foreach (AssignmentStatement assignment in inNode.GetChildsByType(typeof(AssignmentStatement), true))
                ResolveAssignmentStatementType(assignment);
        }
        public static void ResolveAssignmentStatementType(AssignmentStatement assignment)
        {
            TypeCaster.TryToCastAssignmentStatement(assignment);
            ReportErrors = true;
            InnerType AdressorType = ResolveAdressorExpressionType(assignment.AdressorExpressions[0]);
            InnerType AdressableType = ResolveExpressionType(assignment.AdressableExpression);

            if (AdressorType != AdressableType)
                ReportError(new IncompatibleAssignmentType(AdressorType,AdressableType,assignment.SourceContext));
        }
        public static void ResolveMethodsParametersTypes(MethodInvokationExpression method)
        {
            TableMethod tableMethod = GlobalTable.Table.FetchMethod(method.Name, method.GetArgumentsTypes());
            for (int i = 0; i < tableMethod.ArgCount; i++)
            {
                TableMethodArgument tableArgument = tableMethod.Arguments[i];
                ParameterDeclaration methodParameter = method.Parameters[i];
                InnerType methodParameterType = ResolveExpressionType(methodParameter.ParameterInstance);
                if (tableArgument.Type != methodParameterType)
                    ReportError(new IncompatibleMethodParameterType(method.Name,tableArgument.Type,methodParameterType,methodParameter.SourceContext));
            }
        }
        public static void ResolveReturnStatemetsTypes(SyntaxTreeNode inNode)
        {
            foreach (ReturnStatement returnStatement in inNode.GetChildsByType(typeof(ReturnStatement), true))
                ResolveReturnStatementType(returnStatement);
        }
        public static void ResolveReturnStatementType(ReturnStatement returnStatement)
        {
            TypeCaster.TryToCastReturnStatement(returnStatement);
            ReportErrors = true;
            MethodDeclaration method = (MethodDeclaration)returnStatement.GetParentByType(typeof(MethodDeclaration));
            if (returnStatement.IsVoidReturn && method.ReturnType is Void)
                return;

            InnerType returnStatementType = ResolveExpressionType(returnStatement.ReturnBody);

            if (method.ReturnType != returnStatementType)
                ReportError(new IncompatibleReturnType(method.Name,method.ReturnType,returnStatementType,returnStatement.SourceContext));
        }
        public static void ResolveMethodInvokationStatementsTypes(SyntaxTreeNode inNode)
        {
            foreach (MethodInvokationStatement method in inNode.GetChildsByType(typeof(MethodInvokationStatement), true))
                ResolveMethodInvokationStatementTypes(method);
        }
        public static void ResolveMethodInvokationStatementTypes(MethodInvokationStatement method)
        {
            TypeCaster.TryToCastMethodInvokationStatement(method);
            ReportErrors = true;
            MethodInvokationExpression methodInvokationExpression = (MethodInvokationExpression)method.Instance;
            foreach (ParameterDeclaration parameter in methodInvokationExpression.Parameters)
                ResolveMethodParametersTypes(parameter);
        }

        public static void ReportErrorInArithExpression(SemanticError error)
        {
            if (!ErrorInArithReported)
            {
                ReportError(error);
                if (ReportErrors)
                    ErrorInArithReported = true;
            }
        }
        public static void ReportErrorInBooleanExpression(SemanticError error)
        {
            if (!ErrorInBooleanReported)
            {
                ReportError(error);
                if (ReportErrors)
                    ErrorInBooleanReported = true;
            }
        }
        public static void ReportError(SemanticError error)
        {
            if (ReportErrors)
                Diagnostics.SemanticErrors.Add(error);
        }
    }


    public sealed class TypeCaster
    {
        public enum CastCase
        {
            ByteToFloat,
            ByteToInteger,
            ByteToShort,

            ShortToInteger,
            ShortToFloat,

            IntegerToFloat,

            CharToFloat,
            CharToInteger,

            NoNeedToCast,
            Undefined,
        }

        public static bool CanCast(InnerType fType, InnerType sType, bool bothCases = true)
        {
            if (fType == sType) 
                return true;
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
            if (ftype == stype)
                return CastCase.NoNeedToCast;
            if (CanCast(ftype, stype))
            {
                string castCaseStr;
                if (HigherPriorityType(ftype, stype) == ftype)
                    castCaseStr = UpperCaseFirstChar(stype.ALMRepresentation) + "To" + UpperCaseFirstChar(ftype.ALMRepresentation);
                else 
                    castCaseStr = UpperCaseFirstChar(ftype.ALMRepresentation) + "To" + UpperCaseFirstChar(stype.ALMRepresentation);

                foreach (CastCase castCase in System.Enum.GetValues(typeof(CastCase)))
                    if (castCase.ToString() == castCaseStr)
                        return castCase;
            }
            return CastCase.Undefined;
        }

        public static void TryToCastAssignmentStatement(AssignmentStatement assignment)
        {
            TypeChecker.ReportErrors = false;
            InnerType adressorType = TypeChecker.ResolveExpressionType(assignment.AdressorExpressions[0]);
            TryToCastExpression(assignment.AdressableExpression, adressorType);
        }
        public static void TryToCastMethodInvokationStatement(MethodInvokationStatement method)
        {
            TypeChecker.ReportErrors = false;
            TryToCastMethodParameters((MethodInvokationExpression)method.Instance);
        }
        public static void TryToCastConditionExpression(Expression condition)
        {
            TypeChecker.ReportErrors = false;
            switch (condition.NodeKind)
            {
                case NodeType.UnaryBooleanExpression:
                    TryToCastUnaryBooleanExpression((UnaryBooleanExpression)condition);
                    break;
                case NodeType.BinaryBooleanExpression:
                    TryToCastBinaryBooleanExpression((BinaryBooleanExpression)condition);
                    break;
            }
        }
        public static void TryToCastReturnStatement(ReturnStatement returnStatement)
        {
            TypeChecker.ReportErrors = false;
            MethodDeclaration method = (MethodDeclaration)returnStatement.GetParentByType(typeof(MethodDeclaration));
            if (!returnStatement.IsVoidReturn)
                TryToCastExpression(returnStatement.ReturnBody,method.ReturnType);
        }

        public static void TryToCastExpression(Expression expression, InnerType toType)
        {
            switch (expression.NodeKind)
            {
                case NodeType.BinaryArithExpression:
                    TryToCastBinaryArithExpression((BinaryArithExpression)expression, toType);
                    break;
                case NodeType.UnaryArithExpression:
                    TryToCastUnaryArithExpression((UnaryArithExpression)expression, toType);
                    break;
                case NodeType.UnaryBooleanExpression:
                    TryToCastUnaryBooleanExpression((UnaryBooleanExpression)expression);
                    break;
                case NodeType.BinaryBooleanExpression:
                    TryToCastBinaryBooleanExpression((BinaryBooleanExpression)expression);
                    break;

                case NodeType.RealConstant:
                case NodeType.CharConstant:
                case NodeType.IntegerConstant:
                    TryToCastConstantExpression((ConstantExpression)expression, toType);
                    break;

                case NodeType.Identifier:
                    TryToCastIdentifierExpression((IdentifierExpression)expression, toType);
                    break;

                case NodeType.MethodInvokation:
                    TryToCastMethodInvokation((MethodInvokationExpression)expression, toType);
                    break;

                case NodeType.ArrayElement:
                    TryToCastArrayElementExpression((ArrayElementExpression)expression, toType);
                    break;
                case NodeType.ArrayInstance:
                    break;
            }
        }

        public static void TryToCastUnaryBooleanExpression(UnaryBooleanExpression unaryBoolean)
        {
            TryToCastExpression(unaryBoolean.Operand,new Undefined());
        }
        public static void TryToCastBinaryBooleanExpression(BinaryBooleanExpression binaryBoolean)
        {
            switch (binaryBoolean.OperatorKind)
            {
                case BinaryExpression.BinaryOperator.Equal:
                case BinaryExpression.BinaryOperator.NotEqual:
                case BinaryExpression.BinaryOperator.LessThan:
                case BinaryExpression.BinaryOperator.GreaterThan:
                case BinaryExpression.BinaryOperator.LessEqualThan:
                case BinaryExpression.BinaryOperator.GreaterEqualThan:
                    InnerType LOperandType = TypeChecker.ResolveExpressionType(binaryBoolean.LeftOperand);
                    InnerType ROperandType = TypeChecker.ResolveExpressionType(binaryBoolean.RightOperand);
                    InnerType toType = HigherPriorityType(LOperandType, ROperandType);
                    TryToCastExpression(binaryBoolean.LeftOperand, toType);
                    TryToCastExpression(binaryBoolean.RightOperand, toType);
                    break;

                default:
                    TryToCastExpression(binaryBoolean.LeftOperand,new Undefined());
                    TryToCastExpression(binaryBoolean.RightOperand, new Undefined());
                    break;

            }
        }
        public static void TryToCastUnaryArithExpression(UnaryArithExpression unaryArith, InnerType toType)
        {
            SyntaxTreeNode parent = unaryArith;
            InnerType operandType = TypeChecker.ResolveExpressionType(unaryArith.Operand);
            if (CanCast(operandType, toType, false))
            {
                CastCase castCase = DefineCastCase(operandType, toType);
                Expression castMethod = CreateCastMethod(parent, toType, new ParameterDeclaration[] { new ParameterDeclaration(unaryArith.Operand) }, castCase);
                Replace(parent, unaryArith.Operand, castMethod);
            }
        }
        public static void TryToCastBinaryArithExpression(BinaryArithExpression binaryArith, InnerType toType, bool inCastedBinary = false)
        {
            SyntaxTreeNode parent = binaryArith.Parent;
            InnerType LOperandType = TypeChecker.ResolveExpressionType(binaryArith.LeftOperand);
            InnerType ROperandType = TypeChecker.ResolveExpressionType(binaryArith.RightOperand);
            switch (binaryArith.OperatorKind)
            {
                case BinaryExpression.BinaryOperator.LShift:
                case BinaryExpression.BinaryOperator.RShift:
                case BinaryExpression.BinaryOperator.BitwiseOr:
                case BinaryExpression.BinaryOperator.BitwiseAnd:
                case BinaryExpression.BinaryOperator.BitwiseXor:
                    if (DefineCastCase(LOperandType, toType) != CastCase.Undefined)
                    {
                        if (binaryArith.LeftOperand.NodeKind == NodeType.BinaryArithExpression)
                            TryToCastBinaryArithExpression((BinaryArithExpression)binaryArith.LeftOperand, new Int32(), true);
                        else
                            TryToCastExpression(binaryArith.LeftOperand,new Int32());
                    }
                    if (DefineCastCase(ROperandType, toType) != CastCase.Undefined)
                    {
                        if (binaryArith.RightOperand.NodeKind == NodeType.BinaryArithExpression)
                            TryToCastBinaryArithExpression((BinaryArithExpression)binaryArith.RightOperand, new Int32(), true);
                        else
                            TryToCastExpression(binaryArith.RightOperand, new Int32());
                    }
                    if (!inCastedBinary)
                    {
                        CastCase castCase = DefineCastCase(TypeChecker.ResolveBinaryArithExpressionType(binaryArith), toType);
                        Expression castMethod = CreateCastMethod(parent, toType, new ParameterDeclaration[] { new ParameterDeclaration(binaryArith) }, castCase);
                        Replace(parent, binaryArith, castMethod);
                    }
                    break;

                default:
                    TryToCastExpression(binaryArith.LeftOperand, toType);
                    TryToCastExpression(binaryArith.RightOperand, toType);
                    break;
            }
        }
        public static void TryToCastMethodParameters(MethodInvokationExpression method)
        {
            TableMethod tableMethod = GlobalTable.Table.FetchMethod(method.Name,method.GetArgumentsTypes());
            for (int i = 0; i < tableMethod.ArgCount; i++)
                TryToCastExpression(method.Parameters[i].ParameterInstance,tableMethod.Arguments[i].Type);    
        }
        public static void TryToCastMethodInvokation(MethodInvokationExpression method, InnerType toType)
        {
            SyntaxTreeNode parent = method.Parent;
            TryToCastMethodParameters(method);
            if (CanCast(method.ReturnType, toType, false))
            {
                CastCase castCase = DefineCastCase(method.ReturnType, toType);
                Expression castMethod = CreateCastMethod(parent, toType, new ParameterDeclaration[] { new ParameterDeclaration(method) }, castCase);
                Replace(parent, method, castMethod);
            }
        }
        public static void TryToCastIdentifierExpression(IdentifierExpression identifier, InnerType toType)
        {
            SyntaxTreeNode parent = identifier.Parent;
            if (CanCast(identifier.Type, toType, false))
            {
                CastCase castCase = DefineCastCase(identifier.Type, toType);
                Expression castMethod = CreateCastMethod(parent, toType, new ParameterDeclaration[] { new ParameterDeclaration(identifier) }, castCase);
                Replace(parent, identifier, castMethod);
            }
        }
        public static void TryToCastArrayElementExpression(ArrayElementExpression arrayElement, InnerType toType)
        {
            SyntaxTreeNode parent = arrayElement.Parent;
            if (CanCast(arrayElement.Type, toType, false))
            {
                CastCase castCase = DefineCastCase(arrayElement.Type, toType);
                Expression castMethod = CreateCastMethod(parent, toType, new ParameterDeclaration[] { new ParameterDeclaration(arrayElement) }, castCase);
                Replace(parent, arrayElement, castMethod);
            }
        }
        public static void TryToCastConstantExpression(ConstantExpression constant, InnerType toType)
        {
            SyntaxTreeNode parent = constant.Parent;
            if (CanCast(constant.Type,toType,false))
            {
                CastCase castCase = DefineCastCase(constant.Type,toType);
                Expression castMethod = CreateCastMethod(parent,toType,new ParameterDeclaration[] { new ParameterDeclaration(constant)},castCase);
                Replace(parent,constant,castMethod);
            }
        }
        private static Expression CreateCastMethod(SyntaxTreeNode parent,InnerType returnType,ParameterDeclaration[] parameters, CastCase castCase)
        {
            Expression castMethod;
            if (castCase != CastCase.NoNeedToCast)
            {
                castMethod = new MethodInvokationExpression(GetCastMethodName(castCase), parameters, parameters[0].SourceContext);
                ((MethodInvokationExpression)castMethod).ReturnType = returnType;
            }
            else
                castMethod = parameters[0].ParameterInstance;
            castMethod.Parent = parent;
            return castMethod;
        }

        public static void Replace(SyntaxTreeNode parent,SyntaxTreeNode replaceThis, SyntaxTreeNode addThis)
        {
            for (int i = 0; i < parent.Childs.Count; i++)
                if (parent.Childs[i] == replaceThis)
                    parent.Childs[i] = addThis;

            switch (parent.NodeKind)
            {
                case NodeType.MethodInvokation:
                    for (int i = 0; i < parent.Childs.Count; i++)
                    {
                        if (((MethodInvokationExpression)parent).Parameters[i] == replaceThis)
                        {
                            ((MethodInvokationExpression)parent).Parameters[i] = new ParameterDeclaration((Expression)addThis);
                            ((MethodInvokationExpression)parent).Parameters[i].Parent = parent;
                        }
                    }
                    break;

                case NodeType.MethodInvokationAsStatement:
                    for (int i = 0; i < parent.Childs.Count; i++)
                    {
                        if (((MethodInvokationExpression)((MethodInvokationStatement)parent).Instance).Parameters[i] == replaceThis)
                        {
                            ((MethodInvokationExpression)((MethodInvokationStatement)parent).Instance).Parameters[i] = new ParameterDeclaration((Expression)addThis);
                            ((MethodInvokationExpression)((MethodInvokationStatement)parent).Instance).Parameters[i].Parent = parent;
                        }
                    }
                    break;
            }
        }

        public static string GetCastMethodName(CastCase castCase)
        {
            switch (castCase)
            {
                case CastCase.CharToFloat:
                case CastCase.IntegerToFloat:
                    return "tofloat";

                case CastCase.ByteToInteger:
                case CastCase.ShortToInteger:
                case CastCase.CharToInteger:
                    return "toint32";

                default:
                    throw new System.Exception();
            }
        }
    }
}