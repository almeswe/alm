using alm.Core.Errors;
using alm.Core.InnerTypes;
using alm.Core.Table2;
using alm.Core.FrontEnd.SyntaxAnalysis.new_parser_concept.syntax_tree;

using static alm.Other.String.StringMethods;

using alm.Other.Enums;
using alm.Core.FrontEnd.SemanticAnalysis.new_label_checker2;

namespace alm.Core.FrontEnd.SemanticAnalysis.type_checker_new
{
    public sealed class TypeChecker2
    {
        private static bool ErrorInArithReported, ErrorInBooleanReported;

        public static void ResolveModuleTypes(SyntaxTreeNode moduleRoot)
        {
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
                case NodeType.UnaryArithexpression:
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

            //try to cast

            switch (binaryArithExpression.OperatorKind)
            {
                case BinaryExpression.BinaryOperator.FDiv:
                case BinaryExpression.BinaryOperator.IDiv:
                    if (LOperandType is NumericType && ROperandType is NumericType)
                        return binaryArithExpression.OperatorKind == BinaryExpression.BinaryOperator.IDiv ? (NumericType)new Int32() : (NumericType)new Single();
                    ReportErrorInArithExpression(new ErrorForDebug($"Оператор [/,%] должен использоваться с операндами числового типа"));
                    if (!(LOperandType is NumericType))
                        return LOperandType;
                    else
                        return ROperandType;

                case BinaryExpression.BinaryOperator.Power:
                case BinaryExpression.BinaryOperator.Mult:
                case BinaryExpression.BinaryOperator.Addition:
                case BinaryExpression.BinaryOperator.Substraction:
                    if (LOperandType is NumericType && ROperandType is NumericType)
                        return TypeCaster.HigherPriorityType(LOperandType, ROperandType);
                    ReportErrorInArithExpression(new ErrorForDebug($"Оператор [+,-,*,**] должен использоваться с операндами числового типа"));
                    if (!(LOperandType is NumericType))
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
                    ReportErrorInArithExpression(new ErrorForDebug($"Оператор [>>,<<,|,&,^] должен использоваться с операндами целочисленного типа"));
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
                    ReportErrorInArithExpression(new ErrorForDebug($"Оператор унарного минуса должен использоваться с операндом числового типа"));
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
                    if (LOperandType != ROperandType)
                    {
                        ReportErrorInBooleanExpression(new ErrorForDebug($"Оператор [!=,==] должен использоваться с операндами одного типа"));
                        return LOperandType;
                    }
                    if ((LOperandType is PrimitiveType && ROperandType is PrimitiveType) || (LOperandType is String && ROperandType is String))
                        return new Boolean();
                    ReportErrorInBooleanExpression(new ErrorForDebug($"Оператор [!=,==] должен использоваться с операндами примитивного типа"));
                    if (!(LOperandType is PrimitiveType) | !(LOperandType is String))
                        return LOperandType;
                    else 
                        return ROperandType;

                case BinaryExpression.BinaryOperator.Conjuction:
                case BinaryExpression.BinaryOperator.Disjunction:
                case BinaryExpression.BinaryOperator.StrictDisjunction:
                    if (LOperandType is Boolean && ROperandType is Boolean)
                        return new Boolean();
                    ReportErrorInBooleanExpression(new ErrorForDebug($"Оператор [or,and,xor] должен использоваться с операндами логического типа"));
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
                    ReportErrorInBooleanExpression(new ErrorForDebug($"Оператор [<,<=,>,>=] должен использоваться с операндами числового типа"));
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
                    ReportErrorInBooleanExpression(new ErrorForDebug($"Оператор инвертирования должен использоваться с операндом логического типа"));
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
        }
        public static void ResolveConditionExpressionType(Expression condition)
        {
            InnerType ConditionType = ResolveExpressionType(condition);
            if (!(ConditionType is Boolean))
                Diagnostics.SemanticErrors.Add(new ErrorForDebug($"Выражение условия должно быть типа [boolean]"));
        }
        public static void ResolveAssignmentStatementsTypes(SyntaxTreeNode inNode)
        {
            foreach (AssignmentStatement assignment in inNode.GetChildsByType(typeof(AssignmentStatement), true))
                ResolveAssignmentStatementType(assignment);
        }
        public static void ResolveAssignmentStatementType(AssignmentStatement assignment)
        {
            TypeCaster.TryToCastAssignmentStatement(assignment);
            InnerType AdressorType = ResolveAdressorExpressionType(assignment.AdressorExpressions[0]);
            InnerType AdressableType = ResolveExpressionType(assignment.AdressableExpression);

            //try to cast 
            if (AdressorType != AdressableType)
                ReportError(new ErrorForDebug($"Несовместимые типы при присваивании значения переменной, ожидался тип [{AdressorType}], а встречен тип [{AdressableType}]"));
        }
        public static void ResolveMethodsParametersTypes(MethodInvokationExpression method)
        {
            TableMethod tableMethod = GlobalTable.Table.FetchMethod(method.Name, method.ArgCount);
            for (int i = 0; i < tableMethod.ArgCount; i++)
            {
                TableMethodArgument tableArgument = tableMethod.Arguments[i];
                ParameterDeclaration methodParameter = (ParameterDeclaration)method.Parameters[i];
                InnerType methodParameterType = ResolveExpressionType(methodParameter.ParameterInstance);
                if (tableArgument.Type != methodParameterType)
                    Diagnostics.SemanticErrors.Add(new ErrorForDebug($"Несовместимый тип параметра функции, ожидался тип [{tableArgument.Type}], а встречен тип [{methodParameterType}]"));
            }
        }
        public static void ResolveReturnStatemetsTypes(SyntaxTreeNode inNode)
        {
            foreach (ReturnStatement returnStatement in inNode.GetChildsByType(typeof(ReturnStatement), true))
                ResolveReturnStatementType(returnStatement);
        }
        public static void ResolveReturnStatementType(ReturnStatement returnStatement)
        {
            MethodDeclaration method = (MethodDeclaration)returnStatement.GetParentByType(typeof(MethodDeclaration));
            InnerType returnStatementType = ResolveExpressionType(returnStatement.ReturnBody);

            if (returnStatement.IsVoidReturn && method.ReturnType is Void)
                return;

            if (method.ReturnType != returnStatementType)
                ReportError(new ErrorForDebug($"Несовместимый тип возвращаемого значения функции, ожидался тип [{method.ReturnType}], а встречен тип [{returnStatementType}]"));
        }
        public static void ResolveMethodInvokationStatementsTypes(SyntaxTreeNode inNode)
        {
            foreach (MethodInvokationStatement method in inNode.GetChildsByType(typeof(MethodInvokationStatement), true))
                ResolveMethodInvokationStatementTypes(method);
        }
        public static void ResolveMethodInvokationStatementTypes(MethodInvokationStatement method)
        {
            MethodInvokationExpression methodInvokationExpression = (MethodInvokationExpression)method.Instance;
            foreach (ParameterDeclaration parameter in methodInvokationExpression.Parameters)
                ResolveMethodParametersTypes(parameter);
        }

        public static void ReportErrorInArithExpression(SemanticError error)
        {
            if (!ErrorInArithReported)
            {
                ReportError(error);
                ErrorInArithReported = true;
            }
        }
        public static void ReportErrorInBooleanExpression(SemanticError error)
        {
            if (!ErrorInBooleanReported)
            {
                ReportError(error);
                ErrorInBooleanReported = true;
            }
        }
        public static void ReportError(SemanticError error)
        {
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
                    castCaseStr = UpperCaseFirstChar(stype.ALMRepresentation) + "To" + UpperCaseFirstChar(ftype.ALMRepresentation);
                else 
                    castCaseStr = UpperCaseFirstChar(ftype.ALMRepresentation) + "To" + UpperCaseFirstChar(stype.ALMRepresentation);

                foreach (CastCase castCase in System.Enum.GetValues(typeof(CastCase)))
                    if (castCase.ToString() == castCaseStr)
                        return castCase;
            }
            return CastCase.Undefined;
        }

        public static void TryToCastExpression(Expression expression, InnerType toType)
        {
            switch (expression.NodeKind)
            {
                /*case NodeType.BinaryArithExpression:
                    //CastBinaryArithExpression(expression.Parent, (BinaryArithExpression)expression, toType, castCase);
                    break;*/

                case NodeType.RealConstant:
                case NodeType.CharConstant:
                case NodeType.IntegerConstant:
                case NodeType.StringConstant:
                    TryToCastConstantExpression((ConstantExpression)expression,toType);
                    break;

                case NodeType.Identifier:
                    TryToCastIdentifierExpression((IdentifierExpression)expression,toType);
                    break;

                case NodeType.MethodInvokation:
                    TryToCastMethodInvokation((MethodInvokationExpression)expression, toType);
                    break;

                case NodeType.Parameter:

                    break;

                case NodeType.ArrayElement:
                    TryToCastArrayElementExpression((ArrayElementExpression)expression,toType);
                    break;
                case NodeType.ArrayInstance:
                    break;
                    
                default:
                    throw new System.Exception();
            }
        }
        public static void TryToCastAssignmentStatement(AssignmentStatement assignment)
        {
            InnerType adressorType = TypeChecker2.ResolveExpressionType(assignment.AdressorExpressions[0]);
            TryToCastExpression(assignment.AdressableExpression, adressorType);
        }

        public static void TryToCastMethodParameter(ParameterDeclaration parameter, InnerType toType)
        {
            // method -> param -> inst
            MethodInvokationExpression parent = (MethodInvokationExpression)parameter.Parent;
            InnerType instanceType = TypeChecker2.ResolveExpressionType(parameter.ParameterInstance);

            if (CanCast(instanceType, toType, false))
            {
                CastCase castCase = DefineCastCase(instanceType, toType);
                MethodInvokationExpression castMethod = CreateCastMethod(parent, toType, new ParameterDeclaration[] { parameter }, castCase);
                for (int i = 0; i < parent.Childs.Count; i++)
                {
                    if (parent.Childs[i] == parameter)
                        parent.Childs[i] = new ParameterDeclaration(castMethod);
                    if (parent.Parameters[i] == parameter)
                    {
                        parent.Parameters[i] = new ParameterDeclaration(castMethod);
                        parent.Parameters[i].Parent = parent;
                    }
                }
            }
        }

        public static void TryToCastMethodParameters(MethodInvokationExpression method)
        {
            TableMethod tableMethod = GlobalTable.Table.FetchMethod(method.Name,method.ArgCount);
            for (int i = 0; i < tableMethod.ArgCount; i++)
                TryToCastMethodParameter((ParameterDeclaration)method.Parameters[i],tableMethod.Arguments[i].Type);    
        }
        public static void TryToCastMethodInvokation(MethodInvokationExpression method, InnerType toType)
        {
            SyntaxTreeNode parent = method.Parent;
            TryToCastMethodParameters(method);
            if (CanCast(method.ReturnType, toType, false))
            {
                CastCase castCase = DefineCastCase(method.ReturnType, toType);
                MethodInvokationExpression castMethod = CreateCastMethod(parent, toType, new ParameterDeclaration[] { new ParameterDeclaration(method) }, castCase);
                Replace(parent, method, castMethod);
            }
        }
        public static void TryToCastIdentifierExpression(IdentifierExpression identifier, InnerType toType)
        {
            SyntaxTreeNode parent = identifier.Parent;
            if (CanCast(identifier.Type, toType, false))
            {
                CastCase castCase = DefineCastCase(identifier.Type, toType);
                MethodInvokationExpression castMethod = CreateCastMethod(parent, toType, new ParameterDeclaration[] { new ParameterDeclaration(identifier) }, castCase);
                Replace(parent, identifier, castMethod);
            }
        }
        public static void TryToCastArrayElementExpression(ArrayElementExpression arrayElement, InnerType toType)
        {
            SyntaxTreeNode parent = arrayElement.Parent;
            if (CanCast(arrayElement.Type, toType, false))
            {
                CastCase castCase = DefineCastCase(arrayElement.Type, toType);
                MethodInvokationExpression castMethod = CreateCastMethod(parent, toType, new ParameterDeclaration[] { new ParameterDeclaration(arrayElement) }, castCase);
                Replace(parent, arrayElement, castMethod);
            }
        }
        public static void TryToCastConstantExpression(ConstantExpression constant, InnerType toType)
        {
            SyntaxTreeNode parent = constant.Parent;
            if (CanCast(constant.Type,toType,false))
            {
                CastCase castCase = DefineCastCase(constant.Type,toType);
                MethodInvokationExpression castMethod = CreateCastMethod(parent,toType,new ParameterDeclaration[] { new ParameterDeclaration(constant)},castCase);
                Replace(parent,constant,castMethod);
            }
        }
        private static MethodInvokationExpression CreateCastMethod(SyntaxTreeNode parent,InnerType returnType,ParameterDeclaration[] parameters, CastCase castCase)
        {
            MethodInvokationExpression castMethod = new MethodInvokationExpression(GetCastFunctionName(castCase),parameters,parameters[0].SourceContext);
            castMethod.ReturnType = returnType;
            castMethod.Parent = parent;
            return castMethod;
        }

        public static void CastAssignmentExpression(AssignmentStatement assignmentStatement, InnerType expressionType, InnerType expectedType)
        {
            if (CanCast(expressionType, expectedType, false))
            {
                CastCase castCase = DefineCastCase(expressionType, expectedType);
                CastExpression(assignmentStatement.AdressableExpression, expectedType, castCase);
            }
        }
        public static void CastExpression(Expression expression, InnerType toType, CastCase castCase)
        {
            if (castCase == CastCase.Undefined)
                return;
            switch(expression.NodeKind)
            {
                case NodeType.BinaryArithExpression:
                    CastBinaryArithExpression(expression.Parent,(BinaryArithExpression)expression, toType, castCase);
                    break;

                case NodeType.RealConstant:
                case NodeType.CharConstant:
                case NodeType.IntegerConstant:
                    CastConstantExpression(expression.Parent,(ConstantExpression)expression, toType, castCase);
                    break;

                case NodeType.Identifier:
                    CastIdentifierExpression(expression.Parent, (IdentifierExpression)expression, toType, castCase);
                    break;

                default:
                    throw new System.Exception();
            }
        }
        public static void CastIdentifierExpression(SyntaxTreeNode parent, IdentifierExpression identifier, InnerType toType,CastCase castCase)
        {
            MethodInvokationExpression castMethod = new MethodInvokationExpression(GetCastFunctionName(castCase), new ParameterDeclaration[] { new ParameterDeclaration(identifier) }, identifier.SourceContext);
            castMethod.ReturnType = toType;
            castMethod.Parent = parent;
            Replace(parent, identifier, castMethod);
        }
        public static void CastBinaryArithExpression(SyntaxTreeNode parent, BinaryArithExpression binaryArith, InnerType toType, CastCase castCase)
        {
            switch (binaryArith.OperatorKind)
            {
                case BinaryExpression.BinaryOperator.LShift:
                case BinaryExpression.BinaryOperator.RShift:
                case BinaryExpression.BinaryOperator.BitwiseOr:
                case BinaryExpression.BinaryOperator.BitwiseAnd:
                case BinaryExpression.BinaryOperator.BitwiseXor:
                    CastExpression(binaryArith.LeftOperand, new Int32(), DefineCastCase(TypeChecker2.ResolveExpressionType(binaryArith.LeftOperand),new Int32()));
                    CastExpression(binaryArith.RightOperand, new Int32(), DefineCastCase(TypeChecker2.ResolveExpressionType(binaryArith.RightOperand), new Int32()));
                    break;

                default:
                    CastExpression(binaryArith.LeftOperand, toType, castCase);
                    CastExpression(binaryArith.RightOperand, toType, castCase);
                    break;
            }
        }
        public static void CastConstantExpression(SyntaxTreeNode parent, ConstantExpression constant, InnerType toType, CastCase castCase)
        {
            MethodInvokationExpression castMethod = new MethodInvokationExpression(GetCastFunctionName(castCase), new ParameterDeclaration[] { new ParameterDeclaration(constant) }, constant.SourceContext);
            castMethod.ReturnType = toType;
            castMethod.Parent = parent;
            Replace(parent, constant, castMethod);
        }

        public static void Replace(SyntaxTreeNode parent,SyntaxTreeNode replaceThis, SyntaxTreeNode addThis)
        {
            for (int i = 0; i < parent.Childs.Count; i++)
                if (parent.Childs[i] == replaceThis)
                    parent.Childs[i] = addThis;

            switch (parent.NodeKind)
            {
                case NodeType.MethodInvokation:
                    for (int i = 0; i < ((MethodInvokationExpression)parent).Parameters.Length; i++)
                        if (((MethodInvokationExpression)parent).Parameters[i] == replaceThis)
                        {
                            ((MethodInvokationExpression)parent).Parameters[i] = new ParameterDeclaration((Expression)addThis);
                            ((MethodInvokationExpression)parent).Parameters[i].Parent = parent;
                        }
                    break;
            }
        }

        public static string GetCastFunctionName(CastCase castCase)
        {
            switch (castCase)
            {
                case CastCase.CharToFloat:
                case CastCase.IntegerToFloat:
                    return "point";
                case CastCase.ByteToInteger:
                case CastCase.ShortToInteger:
                    return "toint32";

                case CastCase.CharToInteger:
                    return "chartoint32";
                default:
                    throw new System.Exception("??");
            }
        }

    }
}
