using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

using alm.Core.SyntaxTree;
using alm.Core.InnerTypes;

using alm.Other.Enums;
using alm.Other.ConsoleStuff;

using static alm.Core.Compiler.Compiler.CompilationVariables;

using alm.Core.FrontEnd.SemanticAnalysis;

namespace alm.Core.BackEnd
{
    public sealed class Emitter
    {
        private static string exeName;
        private static bool IsLoaded;
        private static ModuleBuilder module;
        private static AssemblyBuilder assembly;

        private static Dictionary<string, int> MethodArguments = new Dictionary<string, int>();
        private static Dictionary<MethodInfo, Type[]> Methods = new Dictionary<MethodInfo, Type[]>();
        private static Dictionary<string, LocalVariableInfo> MethodLocals = new Dictionary<string, LocalVariableInfo>();
        private static Dictionary<MethodDeclaration, Type[]> ExternalMethods = new Dictionary<MethodDeclaration, Type[]>();

        private static Label BreakLabel = default;
        private static Label ContinueLabel = default;

        private static void EmitMethodDeclaration(MethodDeclaration method)
        {
            Type[] argumentTypes = CreateTypes(method.GetArgumentsTypes());
            MethodBuilder methodBuilder = module.DefineGlobalMethod(method.Name, MethodAttributes.Public | MethodAttributes.Static, method.ReturnType.GetEquivalence(), argumentTypes);
            ILGenerator methodIL = methodBuilder.GetILGenerator();

            if (!method.IsExternal)
                Methods.Add(methodBuilder, argumentTypes);
            else
                ExternalMethods.Add(method, argumentTypes);

            EmitMethodArguments(method.Arguments);
            methodIL.Emit(OpCodes.Nop);
            if (!method.IsExternal)
                EmitEmbeddedStatement(method.Body, methodIL);

            MethodLocals.Clear();
            MethodArguments.Clear();
        }
        private static void EmitExternalMethodInvokation(MethodInvokationExpression method, ILGenerator methodIL)
        {
            Type[] arguments = CreateTypes(method.GetArgumentsTypes());
            Type type = Type.GetType(GetCreatedExternalMethod(method.Name, arguments).NETPackage);
            EmitMethodParameters(method.Parameters, methodIL);
            methodIL.EmitCall(OpCodes.Call, type.GetMethod(method.Name, arguments), null);
        }

        private static void EmitMethodArguments(ArgumentDeclaration[] arguments)
        {
            for (int i = 0; i < arguments.Length; i++)
                MethodArguments.Add(arguments[i].Identifier.Name, i);
        }
        private static void EmitMethodParameters(Expression[] parameters, ILGenerator methodIL)
        {
            foreach (ParameterDeclaration parameter in parameters)
                EmitExpression(parameter.ParameterInstance, methodIL);
        }
        private static void EmitEmbeddedStatement(EmbeddedStatement body, ILGenerator methodIL)
        {
            foreach (Statement statement in body.Childs)
                EmitStatement(statement, methodIL);
            if (body.Parent is MethodDeclaration)
                if (((MethodDeclaration)body.Parent).ReturnType.GetEquivalence() == typeof(void))
                    methodIL.Emit(OpCodes.Ret);
        }

        private static void EmitStatement(Statement statement, ILGenerator methodIL)
        {
            switch (statement.NodeKind)
            {
                case NodeType.AssignmentStatement:
                    EmitAssignmentStatement((AssignmentStatement)statement, methodIL);
                    break;

                case NodeType.Declaration:
                    EmitDeclarationStatement((IdentifierDeclaration)statement, methodIL);
                    break;

                case NodeType.MethodInvokationAsStatement:
                    EmitMethodInvokationStatement((MethodInvokationStatement)statement, methodIL);
                    break;

                case NodeType.If:
                    EmitIfStatement((IfStatement)statement, methodIL);
                    break;

                case NodeType.While:
                    EmitWhileLoopStatement((WhileLoopStatement)statement, methodIL);
                    break;

                case NodeType.Do:
                    EmitDoLoopStatement((DoLoopStatement)statement, methodIL);
                    break;

                case NodeType.For:
                    EmitForLoopStatement((ForLoopStatement)statement, methodIL);
                    break;

                case NodeType.Return:
                    EmitReturnStatement((ReturnStatement)statement, methodIL);
                    break;

                case NodeType.Break:
                    methodIL.Emit(OpCodes.Br, BreakLabel);
                    break;
                case NodeType.Continue:
                    methodIL.Emit(OpCodes.Br, ContinueLabel);
                    break;

                default:
                    throw new Exception();
            }
        }

        private static void EmitCondition(Expression condition, ILGenerator methodIL)
        {
            EmitExpression(condition, methodIL);
        }
        private static void EmitUnaryBooleanExpression(UnaryBooleanExpression unaryBoolean, ILGenerator methodIL)
        {
            switch (unaryBoolean.OperatorKind)
            {
                case UnaryExpression.UnaryOperator.UnaryInversion:
                    EmitExpression(unaryBoolean.Operand, methodIL);
                    EmitInversion(methodIL);
                    break;

                default:
                    throw new Exception();
            }
        }
        private static void EmitBinaryBooleanExpression(BinaryBooleanExpression binaryBoolean, ILGenerator methodIL)
        {
            switch (binaryBoolean.OperatorKind)
            {
                case BinaryExpression.BinaryOperator.Conjuction:
                    EmitPrimitiveBinaryOperation(binaryBoolean.LeftOperand, binaryBoolean.RightOperand, OpCodes.And, methodIL);
                    break;
                case BinaryExpression.BinaryOperator.Disjunction:
                    EmitPrimitiveBinaryOperation(binaryBoolean.LeftOperand, binaryBoolean.RightOperand, OpCodes.Or, methodIL);
                    break;
                case BinaryExpression.BinaryOperator.StrictDisjunction:
                    EmitPrimitiveBinaryOperation(binaryBoolean.LeftOperand, binaryBoolean.RightOperand, OpCodes.Xor, methodIL);
                    break;

                case BinaryExpression.BinaryOperator.LessThan:
                    EmitBooleanRelationOperation(binaryBoolean.LeftOperand, binaryBoolean.RightOperand, OpCodes.Blt, methodIL);
                    break;
                case BinaryExpression.BinaryOperator.GreaterThan:
                    EmitBooleanRelationOperation(binaryBoolean.LeftOperand, binaryBoolean.RightOperand, OpCodes.Bgt, methodIL);
                    break;
                case BinaryExpression.BinaryOperator.LessEqualThan:
                    EmitBooleanRelationOperation(binaryBoolean.LeftOperand, binaryBoolean.RightOperand, OpCodes.Ble, methodIL);
                    break;
                case BinaryExpression.BinaryOperator.GreaterEqualThan:
                    EmitBooleanRelationOperation(binaryBoolean.LeftOperand, binaryBoolean.RightOperand, OpCodes.Bge, methodIL);
                    break;

                case BinaryExpression.BinaryOperator.Equal:
                    if (OperandsHaveSameTypes(binaryBoolean, new InnerTypes.String()))
                        EmitStringEquality(binaryBoolean.LeftOperand, binaryBoolean.RightOperand, methodIL);
                    else
                        EmitBooleanRelationOperation(binaryBoolean.LeftOperand, binaryBoolean.RightOperand, OpCodes.Beq, methodIL);
                    break;
                case BinaryExpression.BinaryOperator.NotEqual:
                    if (OperandsHaveSameTypes(binaryBoolean, new InnerTypes.String()))
                        EmitStringEquality(binaryBoolean.LeftOperand, binaryBoolean.RightOperand, methodIL);
                    else
                        EmitBooleanRelationOperation(binaryBoolean.LeftOperand, binaryBoolean.RightOperand, OpCodes.Beq, methodIL);
                    EmitInversion(methodIL);
                    break;


                default:
                    throw new Exception();

            }
        }
        private static void EmitInversion(ILGenerator methodIL)
        {
            Label invToFalse = methodIL.DefineLabel();
            Label invToTrue = methodIL.DefineLabel();
            Label toEndCase = methodIL.DefineLabel();

            methodIL.Emit(OpCodes.Brfalse, invToTrue);
            methodIL.MarkLabel(invToFalse);
            methodIL.Emit(OpCodes.Ldc_I4_0);
            methodIL.Emit(OpCodes.Br, toEndCase);
            methodIL.MarkLabel(invToTrue);
            methodIL.Emit(OpCodes.Ldc_I4_1);
            methodIL.MarkLabel(toEndCase);
        }
        private static void EmitBooleanRelationOperation(Expression LOperand, Expression ROperand, OpCode opCode, ILGenerator methodIL)
        {
            Label toTrueCase = methodIL.DefineLabel();
            Label toFalseCase = methodIL.DefineLabel();
            Label toEndCase = methodIL.DefineLabel();

            EmitExpression(LOperand, methodIL);
            EmitExpression(ROperand, methodIL);
            methodIL.Emit(opCode, toTrueCase);
            methodIL.MarkLabel(toFalseCase);
            methodIL.Emit(OpCodes.Ldc_I4_0);
            methodIL.Emit(OpCodes.Br, toEndCase);
            methodIL.MarkLabel(toTrueCase);
            methodIL.Emit(OpCodes.Ldc_I4_1);
            methodIL.MarkLabel(toEndCase);
        }

        private static void EmitIfStatement(IfStatement ifStatement, ILGenerator methodIL)
        {
            Label toEnd = methodIL.DefineLabel();
            Label toTrueCase = methodIL.DefineLabel();

            EmitCondition(ifStatement.Condition, methodIL);

            methodIL.Emit(OpCodes.Brtrue, toTrueCase);

            if (ifStatement.ElseBody != null)
                EmitEmbeddedStatement((EmbeddedStatement)ifStatement.ElseBody, methodIL);
            methodIL.Emit(OpCodes.Br, toEnd);
            methodIL.MarkLabel(toTrueCase);
            EmitEmbeddedStatement((EmbeddedStatement)ifStatement.Body, methodIL);

            methodIL.MarkLabel(toEnd);
        }
        private static void EmitWhileLoopStatement(WhileLoopStatement whileLoop, ILGenerator methodIL)
        {
            Label toLoopCond = methodIL.DefineLabel();
            Label toLoopBody = methodIL.DefineLabel();
            Label toBodyEnd = methodIL.DefineLabel();
            Label toLoopEnd = methodIL.DefineLabel();

            BreakLabel    = toLoopEnd;
            ContinueLabel = toBodyEnd;

            methodIL.MarkLabel(toLoopCond);
            EmitCondition(whileLoop.Condition, methodIL);

            methodIL.Emit(OpCodes.Brtrue, toLoopBody);
            methodIL.Emit(OpCodes.Br, toLoopEnd);

            methodIL.MarkLabel(toLoopBody);
            EmitEmbeddedStatement((EmbeddedStatement)whileLoop.Body, methodIL);
            methodIL.MarkLabel(toBodyEnd);
            methodIL.Emit(OpCodes.Br, toLoopCond);

            methodIL.MarkLabel(toLoopEnd);
        }
        private static void EmitDoLoopStatement(DoLoopStatement doLoop, ILGenerator methodIL)
        {
            Label toLoopCond = methodIL.DefineLabel();
            Label toLoopBody = methodIL.DefineLabel();
            Label toBodyEnd = methodIL.DefineLabel();
            Label toLoopEnd = methodIL.DefineLabel();

            BreakLabel    = toLoopEnd;
            ContinueLabel = toBodyEnd;

            methodIL.MarkLabel(toLoopBody);
            EmitEmbeddedStatement((EmbeddedStatement)doLoop.Body, methodIL);
            methodIL.MarkLabel(toBodyEnd);

            methodIL.MarkLabel(toLoopCond);
            EmitCondition(doLoop.Condition, methodIL);
            methodIL.Emit(OpCodes.Brtrue, toLoopBody);

            methodIL.MarkLabel(toLoopEnd);
        }
        private static void EmitForLoopStatement(ForLoopStatement forLoop, ILGenerator methodIL)
        {
            Label toLoopCond = methodIL.DefineLabel();
            Label toLoopBody = methodIL.DefineLabel();
            Label toBodyEnd = methodIL.DefineLabel();
            Label toLoopEnd = methodIL.DefineLabel();

            BreakLabel = toLoopEnd;
            ContinueLabel = toBodyEnd;

            EmitStatement(forLoop.InitStatement,methodIL);

            methodIL.MarkLabel(toLoopCond);
            EmitCondition(forLoop.Condition, methodIL);

            methodIL.Emit(OpCodes.Brtrue, toLoopBody);
            methodIL.Emit(OpCodes.Br, toLoopEnd);

            methodIL.MarkLabel(toLoopBody);
            EmitEmbeddedStatement((EmbeddedStatement)forLoop.Body, methodIL);
            methodIL.MarkLabel(toBodyEnd);
            EmitStatement(forLoop.StepStatement,methodIL);
            methodIL.Emit(OpCodes.Br, toLoopCond);

            methodIL.MarkLabel(toLoopEnd);
        }

        private static void EmitDeclarationStatement(IdentifierDeclaration declaration, ILGenerator methodIL)
        {
            foreach (IdentifierExpression identifier in declaration.DeclaringIdentifiers)
                EmitIdentifierExpression(identifier, methodIL);

            if (declaration.AssingningExpression != null)
                EmitAssignmentStatement(declaration.AssingningExpression, methodIL);
        }
        private static void EmitAssignmentStatement(AssignmentStatement assignment, ILGenerator methodIL)
        {
            foreach (Expression expression in assignment.AdressorExpressions)
            {
                //array element | identifier
                if (expression is ArrayElementExpression)
                    EmitArrayElementAsAdressor((ArrayElementExpression)expression, assignment.AdressableExpression, methodIL);
                if (expression is IdentifierExpression)
                    EmitIdentifierExpressionAsAdressor((IdentifierExpression)expression, assignment.AdressableExpression, methodIL);
            }
        }
        private static void EmitMethodInvokationStatement(MethodInvokationStatement method, ILGenerator methodIL)
        {
            EmitMethodInvokation((MethodInvokationExpression)method.Instance, methodIL);
            if (!(((MethodInvokationExpression)method.Instance).ReturnType is InnerTypes.Void))
                methodIL.Emit(OpCodes.Pop);
        }
        private static void EmitReturnStatement(ReturnStatement returnStatement, ILGenerator methodIL)
        {
            if (!returnStatement.IsVoidReturn)
                EmitExpression(returnStatement.ReturnBody, methodIL);
            methodIL.Emit(OpCodes.Ret);
        }

        private static void EmitIdentifierExpressionAsAdressor(IdentifierExpression identifier, Expression adressableExpression, ILGenerator methodIL)
        {
            EmitExpression(adressableExpression, methodIL);
            if (IsArgument(identifier.Name))
                methodIL.Emit(OpCodes.Starg, GetArgumentsIndex(identifier.Name));
            else
                methodIL.Emit(OpCodes.Stloc, (LocalBuilder)GetCreatedLocal(identifier.Name));
        }
        private static void EmitArrayElementAsAdressor(ArrayElementExpression arrayElement, Expression adressableExpression, ILGenerator methodIL)
        {
            if (IsArgument(arrayElement.ArrayName))
                methodIL.Emit(OpCodes.Ldarg, GetArgumentsIndex(arrayElement.ArrayName));
            else
                methodIL.Emit(OpCodes.Ldloc, (LocalBuilder)GetCreatedLocal(arrayElement.ArrayName));

            for (int i = 0; i < arrayElement.Indexes.Length; i++)
                EmitExpression(arrayElement.Indexes[i], methodIL);
            EmitExpression(adressableExpression, methodIL);

            if (arrayElement.IsArrayPrimitive())
                if (arrayElement.Type is InnerTypes.String)
                    methodIL.Emit(OpCodes.Stelem_Ref);
                else
                    methodIL.Emit(OpCodes.Stelem, arrayElement.Type.GetEquivalence());
            else
            {
                Type[] args = Int32FilledArray(arrayElement.ArrayDimension + 1);
                args[arrayElement.ArrayDimension] = arrayElement.Type.GetEquivalence();
                methodIL.EmitCall(OpCodes.Call, arrayElement.Type.CreateArrayInstance(arrayElement.ArrayDimension).GetEquivalence().GetMethod("Set", args), null);
            }
        }
        private static void EmitArrayElement(ArrayElementExpression arrayElement, ILGenerator methodIL)
        {
            Type arrayType = arrayElement.Type.GetEquivalence();

            if (IsArgument(arrayElement.ArrayName))
                methodIL.Emit(OpCodes.Ldarg, GetArgumentsIndex(arrayElement.ArrayName));
            else
                methodIL.Emit(OpCodes.Ldloc, (LocalBuilder)GetCreatedLocal(arrayElement.ArrayName));

            EmitArrayElementIndexes(arrayElement, methodIL);

            if (arrayElement.IsArrayPrimitive())
                if ((arrayType.ToString().Contains("System.String") || arrayType.ToString().Contains("System.Char")) && arrayElement.Type.GetEquivalence() == typeof(char))
                    EmitGetCharsMethod(methodIL);
                else
                    methodIL.Emit(OpCodes.Ldelem, arrayElement.Type.GetEquivalence());
            else
                methodIL.EmitCall(OpCodes.Call, arrayElement.Type.CreateArrayInstance(arrayElement.ArrayDimension).GetEquivalence().GetMethod("Get", Int32FilledArray(arrayElement.ArrayDimension)), null);
        }
        private static void EmitArrayElementIndexes(ArrayElementExpression arrayElement, ILGenerator methodIL)
        {
            for (int i = 0; i < arrayElement.Indexes.Length; i++)
            {
                EmitExpression(arrayElement.Indexes[i], methodIL);
                if (arrayElement.IsArrayPrimitive())
                    if (i != arrayElement.Indexes.Length - 1)
                        methodIL.Emit(OpCodes.Ldelem_Ref);
            }
        }
        private static void EmitArrayInstance(ArrayInstance arrayInstance, ILGenerator methodIL)
        {
            for (int i = 0; i < arrayInstance.DimensionSizes.Length; i++)
                EmitExpression(arrayInstance.DimensionSizes[i], methodIL);

            if (arrayInstance.IsPrimitive())
                methodIL.Emit(OpCodes.Newarr, ((ArrayType)arrayInstance.Type).GetAtomElementType().GetEquivalence());
            else
                methodIL.Emit(OpCodes.Newobj, arrayInstance.Type.GetEquivalence().GetConstructor(Int32FilledArray(arrayInstance.Dimension - 1)));
        }

        private static void EmitMethodInvokation(MethodInvokationExpression method, ILGenerator methodIL)
        {
            if (EmitBaseMethod(method, methodIL))
                return;
            if (GetCreatedExternalMethod(method.Name, CreateTypes(method.GetArgumentsTypes())) != null)
                EmitExternalMethodInvokation(method, methodIL);
            else
            {
                MethodInfo createdMethod = GetCreatedMethod(method.Name, CreateTypes(method.GetArgumentsTypes()));
                EmitMethodParameters(method.Parameters, methodIL);
                methodIL.EmitCall(OpCodes.Call, createdMethod, null);
            }
        }
        private static bool EmitBaseMethod(MethodInvokationExpression method, ILGenerator methodIL)
        {
            switch (method.Name)
            {
                case "len":
                    EmitExpression(method.Parameters[0].ParameterInstance, methodIL);
                    methodIL.Emit(OpCodes.Ldlen);
                    return true;
            }
            return false;
        }

        private static void EmitExpression(Expression expression, ILGenerator methodIL)
        {
            switch (expression.NodeKind)
            {
                case NodeType.BinaryBooleanExpression:
                    EmitBinaryBooleanExpression((BinaryBooleanExpression)expression, methodIL);
                    break;
                case NodeType.UnaryBooleanExpression:
                    EmitUnaryBooleanExpression((UnaryBooleanExpression)expression, methodIL);
                    break;

                case NodeType.BinaryArithExpression:
                    EmitBinaryArithExpression((BinaryArithExpression)expression, methodIL);
                    break;
                case NodeType.UnaryArithExpression:
                    EmitUnaryArithExpression((UnaryArithExpression)expression, methodIL);
                    break;

                case NodeType.Identifier:
                    EmitIdentifierExpression((IdentifierExpression)expression, methodIL);
                    break;

                case NodeType.RealConstant:
                case NodeType.CharConstant:
                case NodeType.StringConstant:
                case NodeType.BooleanConstant:
                case NodeType.IntegerConstant:
                    EmitConstantExpression((ConstantExpression)expression, methodIL);
                    break;

                case NodeType.ArrayInstance:
                    EmitArrayInstance((ArrayInstance)expression, methodIL);
                    break;
                case NodeType.ArrayElement:
                    EmitArrayElement((ArrayElementExpression)expression, methodIL);
                    break;

                case NodeType.MethodInvokation:
                    EmitMethodInvokation((MethodInvokationExpression)expression, methodIL);
                    break;

                default:
                    throw new Exception();
            }
        }

        private static void EmitIdentifierExpression(IdentifierExpression identifier, ILGenerator methodIL)
        {
            if (identifier.IdentifierState == IdentifierExpression.State.Call)
            {
                if (IsArgument(identifier.Name))
                    methodIL.Emit(OpCodes.Ldarg, GetArgumentsIndex(identifier.Name));
                else
                    methodIL.Emit(OpCodes.Ldloc, (LocalBuilder)GetCreatedLocal(identifier.Name));
            }
            else
                MethodLocals.Add(identifier.Name, methodIL.DeclareLocal(identifier.Type.GetEquivalence()));
        }
        private static void EmitBinaryArithExpression(BinaryArithExpression binaryArith, ILGenerator methodIL)
        {
            switch (binaryArith.OperatorKind)
            {
                case BinaryExpression.BinaryOperator.FDiv:
                    EmitPrimitiveBinaryOperation(binaryArith.LeftOperand, binaryArith.RightOperand, OpCodes.Div, methodIL);
                    break;
                case BinaryExpression.BinaryOperator.IDiv:
                    EmitPrimitiveBinaryOperation(binaryArith.LeftOperand, binaryArith.RightOperand, OpCodes.Rem, methodIL);
                    break;
                case BinaryExpression.BinaryOperator.Mult:
                    EmitPrimitiveBinaryOperation(binaryArith.LeftOperand, binaryArith.RightOperand, OpCodes.Mul, methodIL);
                    break;
                case BinaryExpression.BinaryOperator.Substraction:
                    EmitPrimitiveBinaryOperation(binaryArith.LeftOperand, binaryArith.RightOperand, OpCodes.Sub, methodIL);
                    break;
                case BinaryExpression.BinaryOperator.Addition:
                    if (OperandsHaveSameTypes(binaryArith, new InnerTypes.String()))
                        EmitStringConcatenation(binaryArith.LeftOperand, binaryArith.RightOperand, methodIL);
                    else
                        EmitPrimitiveBinaryOperation(binaryArith.LeftOperand, binaryArith.RightOperand, OpCodes.Add, methodIL);
                    break;
                case BinaryExpression.BinaryOperator.Power:
                    EmitPowerOperation(binaryArith.LeftOperand, binaryArith.RightOperand, methodIL);
                    break;

                case BinaryExpression.BinaryOperator.BitwiseAnd:
                    EmitPrimitiveBinaryOperation(binaryArith.LeftOperand, binaryArith.RightOperand, OpCodes.And, methodIL);
                    break;
                case BinaryExpression.BinaryOperator.BitwiseOr:
                    EmitPrimitiveBinaryOperation(binaryArith.LeftOperand, binaryArith.RightOperand, OpCodes.Or, methodIL);
                    break;
                case BinaryExpression.BinaryOperator.BitwiseXor:
                    EmitPrimitiveBinaryOperation(binaryArith.LeftOperand, binaryArith.RightOperand, OpCodes.Xor, methodIL);
                    break;

                case BinaryExpression.BinaryOperator.LShift:
                    EmitPrimitiveBinaryOperation(binaryArith.LeftOperand, binaryArith.RightOperand, OpCodes.Shl, methodIL);
                    break;
                case BinaryExpression.BinaryOperator.RShift:
                    EmitPrimitiveBinaryOperation(binaryArith.LeftOperand, binaryArith.RightOperand, OpCodes.Shr, methodIL);
                    break;

                default:
                    throw new Exception();
            }
        }
        private static void EmitUnaryArithExpression(UnaryArithExpression unaryArith, ILGenerator methodIL)
        {
            switch (unaryArith.OperatorKind)
            {
                case UnaryExpression.UnaryOperator.UnaryMinus:
                    //simply mult by -1
                    EmitExpression(unaryArith.Operand, methodIL);
                    methodIL.Emit(OpCodes.Ldc_I4, -1);
                    methodIL.Emit(OpCodes.Mul);
                    break;

                default:
                    throw new Exception();
            }
        }
        private static void EmitPowerOperation(Expression LOperand, Expression ROperand, ILGenerator methodIL)
        {
            EmitExpression(LOperand, methodIL);
            EmitExpression(ROperand, methodIL);
            methodIL.EmitCall(OpCodes.Call, typeof(Math).GetMethod("Pow", new Type[] { typeof(double), typeof(double) }), null);
        }
        private static void EmitPrimitiveBinaryOperation(Expression LOperand, Expression ROperand, OpCode opCode, ILGenerator methodIL)
        {
            EmitExpression(LOperand, methodIL);
            EmitExpression(ROperand, methodIL);
            methodIL.Emit(opCode);
        }

        private static void EmitStringEquality(Expression LOperand, Expression ROperand, ILGenerator methodIL)
        {
            EmitExpression(LOperand, methodIL);
            EmitExpression(ROperand, methodIL);
            methodIL.EmitCall(OpCodes.Call, typeof(string).GetMethod("Equals", new Type[] { typeof(string), typeof(string) }), null);
        }
        private static void EmitStringConcatenation(Expression LOperand, Expression ROperand, ILGenerator methodIL)
        {
            EmitExpression(LOperand, methodIL);
            EmitExpression(ROperand, methodIL);
            methodIL.EmitCall(OpCodes.Call, typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }), null);
        }
        private static void EmitGetCharsMethod(ILGenerator methodIL)
        {
            methodIL.EmitCall(OpCodes.Callvirt, typeof(string).GetMethod("get_Chars", new Type[] { typeof(int) }), null);
        }

        private static void EmitConstantExpression(ConstantExpression constant, ILGenerator methodIL)
        {
            if (constant.Type.GetEquivalence() == typeof(int))
                methodIL.Emit(OpCodes.Ldc_I4, Convert.ToInt32(constant.Value));
            else if (constant.Type.GetEquivalence() == typeof(string))
                methodIL.Emit(OpCodes.Ldstr, constant.Value);
            else if (constant.Type.GetEquivalence() == typeof(bool))
                methodIL.Emit(OpCodes.Ldc_I4, constant.Value == "true" ? 1 : 0);
            else if (constant.Type.GetEquivalence() == typeof(float))
                methodIL.Emit(OpCodes.Ldc_R4, float.Parse(constant.Value));
            else if (constant.Type.GetEquivalence() == typeof(char))
                methodIL.Emit(OpCodes.Ldc_I4, Convert.ToChar(constant.Value));
        }

        private static void EmitMethodDeclarations(SyntaxTreeNode inNode)
        {
            foreach (MethodDeclaration method in inNode.GetChildsByType(typeof(MethodDeclaration), true))
                EmitMethodDeclaration(method);
            module.CreateGlobalFunctions();
        }
        public static void EmitModule(SyntaxTreeNode moduleRoot, string moduleName = "alm", string assemblyName = "alm")
        {
            LoadBootstrapper(moduleName, assemblyName);
            EmitMethodDeclarations(moduleRoot);
            try
            {
                assembly.SetEntryPoint(GetCreatedMethod("main", new Type[0]));
            }
            catch (Exception e) { ConsoleCustomizer.ColorizedPrintln($"Ошибка при задании точки входа программы.[{e.Message}]", ConsoleColor.DarkRed); }
            try
            {
                assembly.Save(exeName);
                System.Diagnostics.Process.Start(exeName);
            }
            catch (Exception e) { ConsoleCustomizer.ColorizedPrintln($"Ошибка при попытке сохранения исполняемого файла.[{e.Message}]", ConsoleColor.DarkRed); }

            Reset();
        }

        private static LocalVariableInfo GetCreatedLocal(string localName)
        {
            foreach (KeyValuePair<string, LocalVariableInfo> local in MethodLocals)
                if (localName == local.Key)
                    return local.Value;
            return null;
        }

        private static bool IsArgument(string localName)
        {
            foreach (string local in MethodArguments.Keys)
                if (localName == local)
                    return true;
            return false;
        }
        private static int GetArgumentsIndex(string argumentName)
        {
            foreach (KeyValuePair<string, int> arg in MethodArguments)
                if (argumentName == arg.Key)
                    return arg.Value;
            return -1;
        }

        private static bool OperandsHaveSameTypes(BinaryExpression binary, InnerType type)
        {
            if (TypeChecker.ResolveExpressionType(binary.LeftOperand) == TypeChecker.ResolveExpressionType(binary.RightOperand))
                if (TypeChecker.ResolveExpressionType(binary.LeftOperand) == type)
                    return true;
            return false;
        }
        private static MethodInfo GetCreatedMethod(string methodName, Type[] arguments)
        {
            foreach (KeyValuePair<MethodInfo, Type[]> method in Methods)
                if (method.Key.Name == methodName && TypesAreEqual(arguments, method.Value))
                    return method.Key;
            return null;
        }
        private static MethodDeclaration GetCreatedExternalMethod(string methodName, Type[] arguments)
        {
            foreach (KeyValuePair<MethodDeclaration, Type[]> method in ExternalMethods)
                if (method.Key.Name == methodName && TypesAreEqual(arguments, method.Value))
                    return method.Key;
            return null;
        }
        private static Type[] CreateTypes(InnerType[] types)
        {
            Type[] newTypes = new Type[types.Length];
            for (int i = 0; i < types.Length; i++)
                newTypes[i] = types[i].GetEquivalence();
            return newTypes;
        }
        private static Type[] Int32FilledArray(int size)
        {
            Type[] args = new Type[size];
            for (int i = 0; i < args.Length; i++)
                args[i] = typeof(int);
            return args;
        }

        private static bool TypesAreEqual(Type[] arguments1, Type[] arguments2)
        {
            if (arguments1.Length != arguments2.Length)
                return false;
            for (int i = 0; i < arguments1.Length; i++)
                if (arguments1[i] != arguments2[i])
                    return false;
            return true;
        }

        public static void Reset()
        {
            IsLoaded = false;
            assembly = null;
            module = null;
            exeName = string.Empty;
            Methods.Clear();
            MethodLocals.Clear();
            MethodArguments.Clear();
            ExternalMethods.Clear();
        }
        public static void LoadBootstrapper(string assemblyName, string moduleName)
        {
            if (!IsLoaded)
            {
                exeName = System.IO.Path.GetFileName(CompilationBinaryPath);

                AppDomain domain = System.Threading.Thread.GetDomain();
                AssemblyName asmName = new AssemblyName(assemblyName);

                assembly = domain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave);
                module = assembly.DefineDynamicModule(moduleName, exeName);
                IsLoaded = true;
            }
        }
    }
}