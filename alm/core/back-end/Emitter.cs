﻿using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

using alm.Core.InnerTypes;

using alm.Other.Enums;
using alm.Other.ConsoleStuff;
using alm.Core.FrontEnd.SyntaxAnalysis;

using static alm.Core.Compiler.Compiler;

namespace alm.Core.BackEnd
{
    public sealed class Emitter
    {
        private static string exeName;
        private static bool IsLoaded = false;
        public static ModuleBuilder module;
        public static AssemblyBuilder assembly;

        private static Dictionary<string, int> methodArgs = new Dictionary<string, int>();
        private static Dictionary<MethodInfo, int> methods = new Dictionary<MethodInfo, int>();
        private static Dictionary<string, LocalVariableInfo> methodLocals = new Dictionary<string, LocalVariableInfo>();
        private static Dictionary<FunctionDeclaration, string> externalMethods = new Dictionary<FunctionDeclaration, string>();

        private static void EmitBinaryExpression(ILGenerator methodIL, BinaryExpression binexpr)
        {
            if (binexpr.Left is BinaryExpression)
            {
                EmitBinaryExpression(methodIL, (BinaryExpression)binexpr.Left);
                if (binexpr.Right is ConstExpression)
                    EmitConstExpression(methodIL, (ConstExpression)binexpr.Right);
                else if (binexpr.Right is IdentifierExpression)
                    EmitIdentifierCall(methodIL, (IdentifierExpression)binexpr.Right);
                else if (binexpr.Right is BinaryExpression)
                    EmitBinaryExpression(methodIL, (BinaryExpression)binexpr.Right);
                else if (binexpr.Right is FunctionCall)
                    EmitFunctionCall(methodIL, (FunctionCall)binexpr.Right);
                else if (binexpr.Right is ArrayElement)
                    EmitArrayElement(methodIL, (ArrayElement)binexpr.Right);
            }
            else if (binexpr.Left is ConstExpression)
            {
                EmitConstExpression(methodIL, (ConstExpression)binexpr.Left);
                if (binexpr.Right is ConstExpression)
                    EmitConstExpression(methodIL, (ConstExpression)binexpr.Right);
                else if (binexpr.Right is IdentifierExpression)
                    EmitIdentifierCall(methodIL, (IdentifierExpression)binexpr.Right);
                else if (binexpr.Right is BinaryExpression)
                    EmitBinaryExpression(methodIL, (BinaryExpression)binexpr.Right);
                else if (binexpr.Right is FunctionCall)
                    EmitFunctionCall(methodIL, (FunctionCall)binexpr.Right);
                else if (binexpr.Left is ArrayElement)
                    EmitArrayElement(methodIL, (ArrayElement)binexpr.Left);
            }
            else if (binexpr.Left is FunctionCall)
            {
                EmitFunctionCall(methodIL,(FunctionCall)binexpr.Left);
                if (binexpr.Right is ConstExpression)
                    EmitConstExpression(methodIL,(ConstExpression)binexpr.Right);
                else if (binexpr.Right is IdentifierExpression)
                    EmitIdentifierCall(methodIL, (IdentifierExpression)binexpr.Right);
                else if (binexpr.Right is BinaryExpression)
                    EmitBinaryExpression(methodIL, (BinaryExpression)binexpr.Right);
                else if (binexpr.Right is FunctionCall)
                    EmitFunctionCall(methodIL, (FunctionCall)binexpr.Right);
                else if (binexpr.Right is ArrayElement)
                    EmitArrayElement(methodIL, (ArrayElement)binexpr.Right);
            }
            else if (binexpr.Left is IdentifierExpression)
            {
                EmitIdentifierCall(methodIL, (IdentifierExpression)binexpr.Left);
                if (binexpr.Right is ConstExpression)
                    EmitConstExpression(methodIL, (ConstExpression)binexpr.Right);
                else if (binexpr.Right is IdentifierExpression)
                    EmitIdentifierCall(methodIL,(IdentifierExpression)binexpr.Right);
                else if (binexpr.Right is BinaryExpression)
                    EmitBinaryExpression(methodIL, (BinaryExpression)binexpr.Right);
                else if (binexpr.Right is FunctionCall)
                    EmitFunctionCall(methodIL, (FunctionCall)binexpr.Right);
                else if (binexpr.Right is ArrayElement)
                    EmitArrayElement(methodIL, (ArrayElement)binexpr.Right);
            }

            else if (binexpr.Left is ArrayElement)
            {
                EmitArrayElement(methodIL, (ArrayElement)binexpr.Left);
                if (binexpr.Right is ConstExpression)
                    EmitConstExpression(methodIL, (ConstExpression)binexpr.Right);
                else if (binexpr.Right is IdentifierExpression)
                    EmitIdentifierCall(methodIL, (IdentifierExpression)binexpr.Right);
                else if (binexpr.Right is BinaryExpression)
                    EmitBinaryExpression(methodIL, (BinaryExpression)binexpr.Right);
                else if (binexpr.Right is FunctionCall)
                    EmitFunctionCall(methodIL, (FunctionCall)binexpr.Right);
                else if (binexpr.Right is ArrayElement)
                    EmitArrayElement(methodIL, (ArrayElement)binexpr.Right);
            }

            methodIL.Emit(DefineArithOpCode(binexpr.Op));
        }
        private static void EmitAssignmentExpression(ILGenerator methodIL, AssignmentExpression assignmentExpression)
        {
            if (assignmentExpression.Left is ArrayElement)
            {
                ArrayElement arrayElement = (ArrayElement)assignmentExpression.Left;

                if (IsArgument(arrayElement.ArrayName))
                    methodIL.Emit(OpCodes.Ldarg,GetArgumentsIndex(arrayElement.ArrayName));
                else
                    methodIL.Emit(OpCodes.Ldloc, (LocalBuilder)GetCreatedLocal(arrayElement.ArrayName));

                for (int i = 0; i < arrayElement.ArrayDimension; i++)
                    EmitExpression(methodIL, arrayElement.IndexInEachDimension[i]);
                EmitExpression(methodIL,(Expression)assignmentExpression.Right);

                if (arrayElement.ArrayDimension == 1)
                {
                    if (arrayElement.Type.GetEquivalence() == typeof(string))
                        methodIL.Emit(OpCodes.Stelem_Ref);
                    else
                        methodIL.Emit(OpCodes.Stelem, arrayElement.Type.GetEquivalence());
                }
                else
                {
                    Type[] args = new Type[arrayElement.Dimension+1];
                    for (int i = 0; i < args.Length-1; i++)
                        args[i] = typeof(int);
                    args[arrayElement.Dimension] = arrayElement.Type.GetEquivalence();

                    methodIL.EmitCall(OpCodes.Call, arrayElement.Type.CreateArrayInstance(arrayElement.ArrayDimension).GetEquivalence().GetMethod("Set", args), null);
                }
            }
            else
            {
                string name = ((IdentifierExpression)assignmentExpression.Left).Name;
                EmitExpression(methodIL, (Expression)assignmentExpression.Right);
                if (IsArgument(name))
                    methodIL.Emit(OpCodes.Starg, GetArgumentsIndex(name));
                else
                    methodIL.Emit(OpCodes.Stloc, (LocalBuilder)GetCreatedLocal(name));
            }
        }

        private static void EmitDeclarationExpression(ILGenerator methodIL, DeclarationExpression declarationExpression)
        {
            if (declarationExpression.Right is AssignmentExpression)
            {
                EmitIdentifierDeclaration(methodIL,(IdentifierExpression)((AssignmentExpression)declarationExpression.Right).Left);
                EmitAssignmentExpression(methodIL, (AssignmentExpression)declarationExpression.Right);
            }
            else if (declarationExpression.Right is IdentifierExpression) 
                EmitIdentifierDeclaration(methodIL, (IdentifierExpression)declarationExpression.Right);
        }

        private static void EmitConstExpression(ILGenerator methodIL, ConstExpression constExpression)
        {
            if (constExpression.Type.GetEquivalence() == typeof(int))
                methodIL.Emit(OpCodes.Ldc_I4, Convert.ToInt32(constExpression.Value));
            else if (constExpression.Type.GetEquivalence() == typeof(string))
                methodIL.Emit(OpCodes.Ldstr, constExpression.Value);
            else if (constExpression.Type.GetEquivalence() == typeof(bool))
                methodIL.Emit(OpCodes.Ldc_I4, constExpression.Value == "true" ? 1 : 0);
            else if (constExpression.Type.GetEquivalence() == typeof(float))
                methodIL.Emit(OpCodes.Ldc_R4, float.Parse(constExpression.Value));
            else if (constExpression.Type.GetEquivalence() == typeof(char))
                methodIL.Emit(OpCodes.Ldc_I4, Convert.ToChar(constExpression.Value));
        }
        private static void EmitIdentifierDeclaration(ILGenerator methodIL, IdentifierExpression identifierExpression)
        {
            LocalBuilder local = methodIL.DeclareLocal(identifierExpression.Type.GetEquivalence());
            methodLocals.Add(identifierExpression.Name, local);
        }
        private static void EmitIdentifierCall(ILGenerator methodIL, IdentifierExpression identifierExpression)
        {
            if (IsArgument(identifierExpression.Name))
            {
                int index = GetArgumentsIndex(identifierExpression.Name);
                methodIL.Emit(OpCodes.Ldarg, index);
            }
            else
            {
                LocalVariableInfo local = GetCreatedLocal(identifierExpression.Name);
                methodIL.Emit(OpCodes.Ldloc, (LocalBuilder)local);
            }
        }
        private static void EmitReturnExpression(ILGenerator methodIL, ReturnExpression returnExpression)
        {
            if (returnExpression.Right != null) 
                EmitExpression(methodIL, (Expression)returnExpression.Right);
            methodIL.Emit(OpCodes.Ret);
        }
        private static void EmitFunctionCall(ILGenerator methodIL ,FunctionCall functionCall)
        {
            if (EmitBaseFunction(methodIL, functionCall)) return;

            MethodInfo method = GetCreatedMethod(functionCall.Name,functionCall.ArgumentCount);

            foreach (var arg in functionCall.ArgumentsValues.Nodes) 
                EmitExpression(methodIL, (Expression)arg);

            if (IsMethodExternal(functionCall.Name,functionCall.ArgumentCount))
                EmitExternalFunctionCall(methodIL, functionCall);
            else 
                methodIL.EmitCall(OpCodes.Call, method, null);

            if (functionCall.Type.GetEquivalence() != typeof(void))
                if (IsReturnValueUseless(functionCall)) 
                    methodIL.Emit(OpCodes.Pop);
        }
        private static void EmitFunctionDeclaration(FunctionDeclaration functionDeclaration)
        {
            string Name = functionDeclaration.Name;
            Type returnType = functionDeclaration.Type.GetEquivalence();
            Type[] argTypes = functionDeclaration.ArgumentCount == 0? new Type[0] : new Type[functionDeclaration.ArgumentCount];

            for (int i = 0; i < functionDeclaration.ArgumentCount; i++) 
                argTypes[i] = ((ArgumentDeclaration)functionDeclaration.Arguments.Nodes[i]).Type.GetEquivalence();

            MethodBuilder method  = module.DefineGlobalMethod(Name,MethodAttributes.Public | MethodAttributes.Static ,returnType,argTypes);
            ILGenerator methodIL = method.GetILGenerator();

            methods.Add(method,functionDeclaration.ArgumentCount);

            for (int i = 0; i < functionDeclaration.Arguments.Nodes.Count; i++)
                methodArgs.Add(((ArgumentDeclaration)functionDeclaration.Arguments.Nodes[i]).Name, i);

            if (!functionDeclaration.External)
                EmitBody(methodIL, functionDeclaration.Body);
            else
                externalMethods.Add(functionDeclaration, functionDeclaration.Package);

            methodIL.Emit(OpCodes.Nop);

            if (returnType == typeof(void) || !functionDeclaration.External)
                methodIL.Emit(OpCodes.Ret);

            methodArgs.Clear();
            methodLocals.Clear();
        }

        private static void EmitArrayElement(ILGenerator methodIL, ArrayElement arrayElement)
        {
            //ArrayType ?? 
            Type arrayType;
            if (IsArgument(arrayElement.ArrayName))
            {
                arrayType = arrayElement.Type.CreateArrayInstance(arrayElement.ArrayDimension).GetEquivalence();
                methodIL.Emit(OpCodes.Ldarg, GetArgumentsIndex(arrayElement.ArrayName));
            }
            else
            {
                LocalBuilder local = (LocalBuilder)GetCreatedLocal(arrayElement.ArrayName);
                arrayType = local.LocalType;
                methodIL.Emit(OpCodes.Ldloc, local);
            }

            for (int i = 0; i < arrayElement.IndexInEachDimension.Length; i++)
            {
                EmitExpression(methodIL, arrayElement.IndexInEachDimension[i]);
                if (arrayElement.ArrayDimension == 1)
                    if (i != arrayElement.IndexInEachDimension.Length-1) 
                        methodIL.Emit(OpCodes.Ldelem_Ref);
            }

            if (arrayElement.ArrayDimension == 1)
                //replace IsStringKindType
                if (IsStringKindType(arrayType) && arrayElement.Type.GetEquivalence() == typeof(char))
                    methodIL.EmitCall(OpCodes.Callvirt, typeof(string).GetMethod("get_Chars", new Type[] { typeof(int) }), null);
                else
                    methodIL.Emit(OpCodes.Ldelem, arrayElement.Type.GetEquivalence());
            else
            {
                Type[] args = new Type[arrayElement.Dimension];
                for (int i = 0; i < args.Length; i++)
                    args[i] = typeof(int);

                methodIL.EmitCall(OpCodes.Call, arrayElement.Type.CreateArrayInstance(arrayElement.ArrayDimension).GetEquivalence().GetMethod("Get", args), null);
            }

        }
        private static void EmitArrayInstance(ILGenerator methodIL,ArrayConst arrayInstance)
        {
            for (int i = 0; i<arrayInstance.Dimension; i++)
                EmitExpression(methodIL,arrayInstance.SizeOfEachDimension[i]);
            if (arrayInstance.Dimension == 1)
                methodIL.Emit(OpCodes.Newarr, ((ArrayType)arrayInstance.Type).GetAtomElementType().GetEquivalence());
            else
            {
                Type[] args = new Type[arrayInstance.Dimension];
                for (int i = 0; i < args.Length; i++)
                    args[i] = typeof(int);

                methodIL.Emit(OpCodes.Newobj, arrayInstance.Type.GetEquivalence().GetConstructor(args));
            }
        }

        private static void EmitExpression(ILGenerator methodIL, Expression expression)
        {
            if      (expression is FunctionCall)           EmitFunctionCall(methodIL, (FunctionCall)expression);
            else if (expression is ReturnExpression)       EmitReturnExpression(methodIL, (ReturnExpression)expression);
            else if (expression is ConstExpression)        EmitConstExpression(methodIL, (ConstExpression)expression);
            else if (expression is BinaryExpression)       EmitBinaryExpression(methodIL, (BinaryExpression)expression);
            else if (expression is IdentifierCall)         EmitIdentifierCall(methodIL, (IdentifierCall)expression);
            else if (expression is DeclarationExpression)  EmitDeclarationExpression(methodIL, (DeclarationExpression)expression);
            else if (expression is AssignmentExpression)   EmitAssignmentExpression(methodIL, (AssignmentExpression)expression);
            else if (expression is ArrayConst)             EmitArrayInstance(methodIL,(ArrayConst)expression);
            else if (expression is ArrayElement)           EmitArrayElement(methodIL,(ArrayElement)expression);

        }
        private static void EmitStatement(ILGenerator methodIL, Statement statement)
        {
            if (statement is IfStatement)         EmitIfStatement(methodIL,(IfStatement)statement);
            else if (statement is WhileStatement) EmitWhileStatement(methodIL,(WhileStatement)statement);
            else if (statement is DoWhileStatement) EmitDoWhileStatement(methodIL, (DoWhileStatement)statement);
        }

        private static void EmitIfStatement(ILGenerator methodIL, IfStatement ifStatement)
        {
            Label toIfBody  = methodIL.DefineLabel();
            Label toEndOfIf = methodIL.DefineLabel();

            EmitCondition(methodIL, ifStatement.Condition);
            methodIL.Emit(OpCodes.Brtrue, toIfBody);

            if (ifStatement.ElseBody != null)
                EmitBody(methodIL, ifStatement.ElseBody);
            methodIL.Emit(OpCodes.Br, toEndOfIf);

            methodIL.MarkLabel(toIfBody);
            EmitBody(methodIL,ifStatement.Body);

            methodIL.MarkLabel(toEndOfIf);
        }
        private static void EmitWhileStatement(ILGenerator methodIL, WhileStatement whileStatement)
        {
            Label toCondition = methodIL.DefineLabel();
            Label toEndOfLoop = methodIL.DefineLabel();
            Label toLoopBody  = methodIL.DefineLabel();

            methodIL.MarkLabel(toCondition);
            EmitCondition(methodIL,whileStatement.Condition);

            methodIL.Emit(OpCodes.Brtrue, toLoopBody);
            methodIL.Emit(OpCodes.Br,  toEndOfLoop);

            methodIL.MarkLabel(toLoopBody);
            EmitBody(methodIL,whileStatement.Body);
            methodIL.Emit(OpCodes.Br,toCondition);

            methodIL.MarkLabel(toEndOfLoop);
        }
        private static void EmitDoWhileStatement(ILGenerator methodIL, DoWhileStatement doWhileStatement)
        {
            Label toCondition = methodIL.DefineLabel();
            Label toEndOfLoop = methodIL.DefineLabel();
            Label toLoopBody = methodIL.DefineLabel();

            methodIL.MarkLabel(toLoopBody);
            EmitBody(methodIL, doWhileStatement.Body);

            methodIL.MarkLabel(toCondition);
            EmitCondition(methodIL, doWhileStatement.Condition);

            methodIL.Emit(OpCodes.Brtrue, toLoopBody);
            methodIL.Emit(OpCodes.Br, toEndOfLoop);

            methodIL.MarkLabel(toEndOfLoop);
        }

        private static void EmitOr(ILGenerator methodIL, BooleanExpression booleanExpression)
        {
            EmitBooleanExpression(methodIL, (Expression)booleanExpression.Right);
            EmitBooleanExpression(methodIL, (Expression)booleanExpression.Left);
            methodIL.Emit(OpCodes.Or);
        }
        private static void EmitAnd(ILGenerator methodIL, BooleanExpression booleanExpression)
        {
            EmitBooleanExpression(methodIL, (Expression)booleanExpression.Right);
            EmitBooleanExpression(methodIL, (Expression)booleanExpression.Left);
            methodIL.Emit(OpCodes.And);
        }
        private static void EmitNot(ILGenerator methodIL, BooleanExpression booleanExpression)
        {
            //OpCodes.Not doesn't working
            Label reverseToTrue  = methodIL.DefineLabel();
            Label reverseToFalse = methodIL.DefineLabel();
            Label toEnd          = methodIL.DefineLabel();
            EmitBooleanExpression(methodIL, (Expression)booleanExpression.Right);
            methodIL.Emit(OpCodes.Brfalse,reverseToTrue);

            methodIL.MarkLabel(reverseToFalse);
            methodIL.Emit(OpCodes.Ldc_I4,0);
            methodIL.Emit(OpCodes.Br, toEnd);

            methodIL.MarkLabel(reverseToTrue);
            methodIL.Emit(OpCodes.Ldc_I4, 1);

            methodIL.MarkLabel(toEnd);
        }
        private static void EmitLessThan(ILGenerator methodIL, BooleanExpression booleanExpression)
        {
            Label toTrueCase = methodIL.DefineLabel();
            Label toFalseCase = methodIL.DefineLabel();
            Label toEnd = methodIL.DefineLabel();

            EmitExpression(methodIL, (Expression)booleanExpression.Left);
            EmitExpression(methodIL, (Expression)booleanExpression.Right);
            methodIL.Emit(OpCodes.Blt, toTrueCase);

            methodIL.MarkLabel(toFalseCase);
            methodIL.Emit(OpCodes.Ldc_I4, 0);
            methodIL.Emit(OpCodes.Br, toEnd);

            methodIL.MarkLabel(toTrueCase);
            methodIL.Emit(OpCodes.Ldc_I4, 1);

            methodIL.MarkLabel(toEnd);
        }
        private static void EmitGreaterThan(ILGenerator methodIL, BooleanExpression booleanExpression)
        {
            Label toTrueCase  = methodIL.DefineLabel();
            Label toFalseCase = methodIL.DefineLabel();
            Label toEnd       = methodIL.DefineLabel();

            EmitExpression(methodIL, (Expression)booleanExpression.Left);
            EmitExpression(methodIL, (Expression)booleanExpression.Right);
            methodIL.Emit(OpCodes.Bgt, toTrueCase);

            methodIL.MarkLabel(toFalseCase);
            methodIL.Emit(OpCodes.Ldc_I4, 0);
            methodIL.Emit(OpCodes.Br, toEnd);

            methodIL.MarkLabel(toTrueCase);
            methodIL.Emit(OpCodes.Ldc_I4, 1);

            methodIL.MarkLabel(toEnd);
        }
        private static void EmitGreaterEqualThan(ILGenerator methodIL, BooleanExpression booleanExpression)
        {
            Label toTrueCase = methodIL.DefineLabel();
            Label toFalseCase = methodIL.DefineLabel();
            Label toEnd = methodIL.DefineLabel();

            EmitExpression(methodIL, (Expression)booleanExpression.Left);
            EmitExpression(methodIL, (Expression)booleanExpression.Right);
            methodIL.Emit(OpCodes.Bge, toTrueCase);

            methodIL.MarkLabel(toFalseCase);
            methodIL.Emit(OpCodes.Ldc_I4, 0);
            methodIL.Emit(OpCodes.Br, toEnd);

            methodIL.MarkLabel(toTrueCase);
            methodIL.Emit(OpCodes.Ldc_I4, 1);

            methodIL.MarkLabel(toEnd);
        }
        private static void EmitLessEqualThan(ILGenerator methodIL, BooleanExpression booleanExpression)
        {
            Label toTrueCase = methodIL.DefineLabel();
            Label toFalseCase = methodIL.DefineLabel();
            Label toEnd = methodIL.DefineLabel();

            EmitExpression(methodIL, (Expression)booleanExpression.Left);
            EmitExpression(methodIL, (Expression)booleanExpression.Right);
            methodIL.Emit(OpCodes.Ble, toTrueCase);

            methodIL.MarkLabel(toFalseCase);
            methodIL.Emit(OpCodes.Ldc_I4, 0);
            methodIL.Emit(OpCodes.Br, toEnd);

            methodIL.MarkLabel(toTrueCase);
            methodIL.Emit(OpCodes.Ldc_I4, 1);

            methodIL.MarkLabel(toEnd);
        }
        private static void EmitEqual(ILGenerator methodIL, BooleanExpression booleanExpression)
        {
            Label toTrueCase = methodIL.DefineLabel();
            Label toFalseCase = methodIL.DefineLabel();
            Label toEnd = methodIL.DefineLabel();

            EmitExpression(methodIL, (Expression)booleanExpression.Right);
            EmitExpression(methodIL, (Expression)booleanExpression.Left);
            if (BothAreSameType(booleanExpression,typeof(string)))
            {
                methodIL.EmitCall(OpCodes.Call, typeof(string).GetMethod("Equals", new Type[] { typeof(string), typeof(string) }), null);
                methodIL.Emit(OpCodes.Brtrue, toTrueCase);
                methodIL.Emit(OpCodes.Br, toFalseCase);
            }
            else
                methodIL.Emit(OpCodes.Beq, toTrueCase);

            methodIL.MarkLabel(toFalseCase);
            methodIL.Emit(OpCodes.Ldc_I4, 0);
            methodIL.Emit(OpCodes.Br, toEnd);

            methodIL.MarkLabel(toTrueCase);
            methodIL.Emit(OpCodes.Ldc_I4, 1);

            methodIL.MarkLabel(toEnd);
        }
        private static void EmitNotEqual(ILGenerator methodIL, BooleanExpression booleanExpression)
        {
            Label toTrueCase  = methodIL.DefineLabel();
            Label toFalseCase = methodIL.DefineLabel();
            Label toEnd       = methodIL.DefineLabel();

            EmitExpression(methodIL, (Expression)booleanExpression.Right);
            EmitExpression(methodIL, (Expression)booleanExpression.Left);
            if (BothAreSameType(booleanExpression, typeof(string)))
            {
                methodIL.EmitCall(OpCodes.Call, typeof(string).GetMethod("Equals", new Type[] { typeof(string), typeof(string) }), null);
                methodIL.Emit(OpCodes.Brfalse, toTrueCase);
                methodIL.Emit(OpCodes.Br, toFalseCase);
            }
            else
                methodIL.Emit(OpCodes.Beq, toFalseCase);

            methodIL.MarkLabel(toTrueCase);
            methodIL.Emit(OpCodes.Ldc_I4, 1);
            methodIL.Emit(OpCodes.Br, toEnd);

            methodIL.MarkLabel(toFalseCase);
            methodIL.Emit(OpCodes.Ldc_I4, 0);

            methodIL.MarkLabel(toEnd);
        }

        private static void EmitCondition(ILGenerator methodIL, Condition condition)
        {
            if      (condition.Nodes[0] is BooleanConst)         EmitConstExpression(methodIL, (ConstExpression)condition.Nodes[0]);
            else if (condition.Nodes[0] is IdentifierExpression) EmitIdentifierCall(methodIL,  (IdentifierCall)condition.Nodes[0]);
            else if (condition.Nodes[0] is FunctionCall)         EmitFunctionCall(methodIL,  (FunctionCall)condition.Nodes[0]);
            else EmitBooleanExpression(methodIL,(BooleanExpression)condition.Nodes[0]);
        }
        private static void EmitBooleanExpression(ILGenerator methodIL, Expression booleanExpression)
        {
            if      (booleanExpression is BooleanConst)         EmitConstExpression(methodIL, (ConstExpression)booleanExpression);
            else if (booleanExpression is IdentifierExpression) EmitIdentifierCall(methodIL, (IdentifierCall)booleanExpression);
            else if (booleanExpression is FunctionCall)         EmitFunctionCall(methodIL, (FunctionCall)booleanExpression);
            else if (booleanExpression.Op == Operator.Equal)       EmitEqual(methodIL, (BooleanExpression)booleanExpression);
            else if (booleanExpression.Op == Operator.NotEqual)    EmitNotEqual(methodIL, (BooleanExpression)booleanExpression);
            else if (booleanExpression.Op == Operator.Less)        EmitLessThan(methodIL, (BooleanExpression)booleanExpression);
            else if (booleanExpression.Op == Operator.Greater)     EmitGreaterThan(methodIL, (BooleanExpression)booleanExpression);
            else if (booleanExpression.Op == Operator.LessEqual)   EmitLessEqualThan(methodIL, (BooleanExpression)booleanExpression);
            else if (booleanExpression.Op == Operator.GreaterEqual)EmitGreaterEqualThan(methodIL, (BooleanExpression)booleanExpression);
            else if (booleanExpression.Op == Operator.LogicalAND)  EmitAnd(methodIL, (BooleanExpression)booleanExpression);
            else if (booleanExpression.Op == Operator.LogicalOR)   EmitOr(methodIL, (BooleanExpression)booleanExpression);
            else if (booleanExpression.Op == Operator.LogicalNOT)  EmitNot(methodIL, (BooleanExpression)booleanExpression);
        }
        private static void EmitBody(ILGenerator methodIL,Body body)
        {
            foreach (SyntaxTreeNode node in body.Nodes)
                if      (node is Expression) EmitExpression(methodIL,(Expression)node);
                else if (node is Statement)  EmitStatement(methodIL, (Statement)node);
        }

        public static void EmitAST(AbstractSyntaxTree ast)
        {
            foreach (FunctionDeclaration func in ast.Root.GetChildsByType("FunctionDeclaration",true))
                EmitFunctionDeclaration(func);

            module.CreateGlobalFunctions();
            try
            {
                assembly.SetEntryPoint(GetCreatedMethod("main", 0));
            }
            catch (Exception e) { ConsoleCustomizer.ColorizedPrintln($"Ошибка при задании точки входа программы.[{e.Message}]", ConsoleColor.DarkRed); }
            try
            {
                assembly.Save(exeName);
            }
            catch (Exception e) { ConsoleCustomizer.ColorizedPrintln($"Ошибка при попытке сохранения исполняемого файла.[{e.Message}]",ConsoleColor.DarkRed); }
        }

        private static void EmitExternalFunctionCall(ILGenerator methodIL, FunctionCall functionCall)
        {
            Type type = Type.GetType(GetPackage(functionCall.Name));
            FunctionDeclaration func = GetExternalMethod(functionCall.Name);
            Type[] argTypes = new Type[func.ArgumentCount];
            for (int i = 0; i < func.ArgumentCount;i++)
                argTypes[i] = ((ArgumentDeclaration)func.Arguments.Nodes[i]).Type.GetEquivalence();
            methodIL.EmitCall(OpCodes.Call,type.GetMethod(functionCall.Name,argTypes),null);
        }
        private static bool EmitBaseFunction(ILGenerator methodIL, FunctionCall functionCall)
        {
            switch(functionCall.Name)
            {
                case "println": EmitPrintlnString(methodIL, functionCall); return true;
                case "print"  : EmitPrintString(methodIL, functionCall);   return true;
                case "input"  : EmitInput(methodIL, functionCall);         return true;

                case "tostr"  : EmitIntToStr(methodIL, functionCall);      return true;
                case "tostrf" : EmitFloatToStr(methodIL, functionCall);    return true;
                case "toint"  : EmitStrToInt(methodIL, functionCall);      return true;

                case "tofloat": EmitStrToFloat(methodIL, functionCall);    return true;
                case "point"  : EmitPoint(methodIL, functionCall);         return true;
                case "round"  : EmitRound(methodIL, functionCall);         return true;

                case "chartoint32": EmitCharToInt32(methodIL, functionCall);return true;
                case "toint32"    : EmitIntToInt32(methodIL, functionCall); return true;
                case "len"        : EmitLen(methodIL, functionCall);        return true;
                default: return false;
            }
        }
        private static void EmitPrintString(ILGenerator methodIL, FunctionCall functionCall)
        {
            EmitExpression(methodIL, (Expression)functionCall.ArgumentsValues.Nodes[0]);
            methodIL.EmitCall(OpCodes.Call, typeof(Console).GetMethod("Write", new Type[] { typeof(string) }), null);
        }
        private static void EmitPrintlnString(ILGenerator methodIL, FunctionCall functionCall)
        {
            EmitExpression(methodIL, (Expression)functionCall.ArgumentsValues.Nodes[0]);
            methodIL.EmitCall(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }), null);
        }
        private static void EmitInput(ILGenerator methodIL,FunctionCall functionCall)
        {
            methodIL.EmitCall(OpCodes.Call, typeof(Console).GetMethod("ReadLine",new Type[0]), null);
            if (IsReturnValueUseless(functionCall)) 
                methodIL.Emit(OpCodes.Pop);
        }
        private static void EmitIntToStr(ILGenerator methodIL, FunctionCall functionCall)
        {
            EmitExpression(methodIL, (Expression)functionCall.ArgumentsValues.Nodes[0]);
            methodIL.EmitCall(OpCodes.Call, typeof(Convert).GetMethod("ToString", new Type[] { typeof(int) }), null);
        }
        private static void EmitFloatToStr(ILGenerator methodIL, FunctionCall functionCall)
        {
            EmitExpression(methodIL, (Expression)functionCall.ArgumentsValues.Nodes[0]);
            methodIL.EmitCall(OpCodes.Call, typeof(Convert).GetMethod("ToString", new Type[] { typeof(float) }), null);
        }
        private static void EmitStrToInt(ILGenerator methodIL, FunctionCall functionCall)
        {
            EmitExpression(methodIL, (Expression)functionCall.ArgumentsValues.Nodes[0]);
            methodIL.EmitCall(OpCodes.Call, typeof(Convert).GetMethod("ToInt32", new Type[] { typeof(string) }), null);
        }
        private static void EmitStrToFloat(ILGenerator methodIL, FunctionCall functionCall)
        {
            EmitExpression(methodIL, (Expression)functionCall.ArgumentsValues.Nodes[0]);
            methodIL.EmitCall(OpCodes.Call, typeof(float).GetMethod("Parse", new Type[] { typeof(string) }), null);
        }
        private static void EmitRound(ILGenerator methodIL, FunctionCall functionCall)
        {
            EmitExpression(methodIL, (Expression)functionCall.ArgumentsValues.Nodes[0]);
            methodIL.EmitCall(OpCodes.Call, typeof(Convert).GetMethod("ToInt32", new Type[] { typeof(float) }), null);
        }
        private static void EmitPoint(ILGenerator methodIL, FunctionCall functionCall)
        {
            EmitExpression(methodIL, (Expression)functionCall.ArgumentsValues.Nodes[0]);
            methodIL.EmitCall(OpCodes.Call, typeof(Convert).GetMethod("ToSingle", new Type[] { typeof(int) }), null);
        }
        private static void EmitCharToInt32(ILGenerator methodIL, FunctionCall functionCall)
        {
            EmitExpression(methodIL, (Expression)functionCall.ArgumentsValues.Nodes[0]);
            methodIL.EmitCall(OpCodes.Call, typeof(Convert).GetMethod("ToInt32", new Type[] { typeof(char) }), null);
        }
        private static void EmitIntToInt32(ILGenerator methodIL, FunctionCall functionCall)
        {
            EmitExpression(methodIL, (Expression)functionCall.ArgumentsValues.Nodes[0]);
            methodIL.EmitCall(OpCodes.Call, typeof(Convert).GetMethod("ToInt32", new Type[] { typeof(sbyte) }), null);
        }
        private static void EmitLen(ILGenerator methodIL, FunctionCall functionCall)
        {
            EmitExpression(methodIL, (Expression)functionCall.ArgumentsValues.Nodes[0]);
            methodIL.Emit(OpCodes.Ldlen);
        }

        private static bool IsStringKindType(Type type)
        {
            string str = "System.String";
            string tstr= type.ToString();

            if (str.Length > tstr.Length)
                return false;
            for (int i = 0; i < str.Length; i++)
                if (str[i] != tstr[i])
                    return false;
            return true;
        }
        private static bool IsArgument(string localName)
        {
            foreach (var arg in methodArgs.Keys)
                if (localName == arg)
                    return true;
            return false;
        }
        private static bool IsMethodExternal(string methodName,int args)
        {
            foreach (var name in externalMethods)
                if (methodName == name.Key.Name && name.Key.ArgumentCount == args)
                    return true;

            return false;
        }
        private static bool IsReturnValueUseless(SyntaxTreeNode node)
        {
            if (node.Parent is Body)
                return true;
            return false;
        }

        private static string GetPackage(string methodName)
        {
            foreach (var name in externalMethods)
                if (methodName == name.Key.Name)
                    return name.Value;

            return string.Empty;
        }
        private static int GetArgumentsIndex(string argName)
        {
            foreach (var arg in methodArgs)
                if (argName == arg.Key)
                    return arg.Value;
            return -1;
        }
        private static LocalVariableInfo GetCreatedLocal(string localName)
        {
            LocalVariableInfo local = null;
            foreach (var l in methodLocals)
                if (localName == l.Key) local = l.Value;
            return local;
        }
        private static MethodInfo GetCreatedMethod(string methodName, int args)
        {
            MethodInfo method = null;
            foreach (var m in methods)
                if (methodName == m.Key.Name)
                    if (m.Value == args)
                        method = m.Key;
            return method;
        }
        private static FunctionDeclaration GetExternalMethod(string methodName)
        {
            foreach (var name in externalMethods)
                if (methodName == name.Key.Name)
                    return name.Key;

            return null;
        }

        private static OpCode DefineArithOpCode(Operator op)
        {
            switch (op)
            {
                case Operator.Plus: return OpCodes.Add;
                case Operator.Minus: return OpCodes.Sub;
                case Operator.Multiplication: return OpCodes.Mul;
                case Operator.Division: return OpCodes.Div;
                default: throw new Exception($"?? [{op}]");
            }
        }

        public static void Reset()
        {
            IsLoaded = false;
            assembly = null;
            module = null;
            exeName = string.Empty;
            methods.Clear();
            methodArgs.Clear();
            methodLocals.Clear();
            externalMethods.Clear();
        }
        public static void LoadBootstrapper(string assemblyName, string moduleName)
        {
            if (!IsLoaded)
            {
                exeName = System.IO.Path.GetFileName(CompilingDestinationPath);

                AppDomain domain = System.Threading.Thread.GetDomain();
                AssemblyName asmName = new AssemblyName(assemblyName);

                assembly = domain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave);
                module = assembly.DefineDynamicModule(moduleName, exeName);
                IsLoaded = true;
            }
        }
        private static bool BothAreSameType(BooleanExpression booleanExpression,Type type)
        {
            foreach (var node in booleanExpression.Nodes)
            {
                if (node is IdentifierExpression)
                {
                    if (((IdentifierExpression)node).Type.GetEquivalence() != type) 
                        return false;
                }
                else if (node is ConstExpression)
                {
                    if (((ConstExpression)node).Type.GetEquivalence() != type)
                        return false;
                }
                else if (node is FunctionCall)
                {
                    if (((FunctionCall)node).Type.GetEquivalence() != type)
                        return false;
                }
                else return false;
            }
            return true;
        }
    }
}