using System;
using System.Linq;
using System.Collections.Generic;

using alm.Core.Errors;
using alm.Core.InnerTypes;

using alm.Other.Enums;
using alm.Other.Structs;
using alm.Other.ConsoleStuff;

using static alm.Other.Enums.TokenType;
using static alm.Core.FrontEnd.SyntaxAnalysis.new_parser_concept.syntax_tree.SourceContext;
using alm.Core.FrontEnd.SemanticAnalysis.new_label_checker2;
using alm.Core.FrontEnd.SemanticAnalysis.type_checker_new;

namespace alm.Core.FrontEnd.SyntaxAnalysis.new_parser_concept.syntax_tree
{
    public static class CompilationVariables
    {
        //Executable file path
        public static string CompilationEntryModule;
        //Current parsing file
        public static string CurrentParsingModule;
        //All imports mentioned in this compilation
        public static Dictionary<string, List<string>> CompilationImports = new Dictionary<string, List<string>>();
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            while (true)
            {
                string path = @"C:\Users\Almes\source\repos\Compiler\compiler v.5\src\tests\main.alm";
                CompilationVariables.CompilationEntryModule = path;
                CompilationVariables.CurrentParsingModule = path;
                CompilationVariables.CompilationImports.Clear();

                var ast = new AbstractSyntaxTree();
                ast.BuildTree(path);
                ast.ShowTree();
                
                Console.ReadLine();
                Console.Clear();
            }
        }
    }

    public sealed class Parser2
    {
        private Lexer Lexer;
        private string CurrentParsingFile;

        public Parser2(Lexer lexer)
        {
            this.Lexer = lexer;
        }

        public SyntaxTreeNode Parse(string path)
        {
            CurrentParsingFile = path;
            CompilationVariables.CurrentParsingModule = path;
            this.CurrentParsingFile = path;
            Lexer.GetNextToken();
            return ParseModule(path);
        }

        public SyntaxTreeNode ParseModule(string module)
        {
            ModuleRoot moduleRoot = new ModuleRoot(module);

            while(Match(tkImport))
            {
                Statement import = ParseImportStatement();
                if (import.Childs.Count > 0)
                    moduleRoot.AddNode(import);
                if (moduleRoot.Childs.Count > 0 && IsErrored(moduleRoot.Childs.Last()))
                    return moduleRoot;
            }

            while (!Match(tkEOF))
            {
                moduleRoot.AddNode(ParseMethodDeclaration());
                if (IsErrored(moduleRoot.Childs.Last()))
                    return moduleRoot;
            }
            return moduleRoot;
        }

        public Expression[] ParseParametersDeclarations()
        {
            List<Expression> parameters = new List<Expression>();

            if (!Match(tkLpar))
                return new Expression[] { new ErroredExpression(new MissingLpar(Lexer.CurrentToken)) };

            Lexer.GetNextToken();
            while (!Match(tkRpar) && !Match(tkEOF))
            {
                parameters.Add(new ParameterDeclaration(ParseExpression()));

                if (!Match(tkComma))
                {
                    if (!Match(tkRpar))
                        return new Expression[] { new ErroredExpression(new MissingComma(Lexer.CurrentToken)) };
                }
                else
                    Lexer.GetNextToken();
            }

            if (!Match(tkRpar))
                return new Expression[] { new ErroredExpression(new MissingRpar(Lexer.CurrentToken)) };

            Lexer.GetNextToken();
            return parameters.ToArray();
        }
        public Expression[] ParseArgumentDeclarations()
        {
            List<Expression> argumens = new List<Expression>();

            Expression identifier,type;

            if (!Match(tkLpar))
                return new Expression[] { new ErroredExpression(new MissingLpar(Lexer.CurrentToken)) };
            Lexer.GetNextToken();

            while (!Match(tkRpar) && !Match(tkEOF))
            {
                type = ParseTypeExpression();
                if (IsErrored(type))
                    return new Expression[] { type };

                identifier = ParseIdentifierExpression(IdentifierExpression.State.Decl,((TypeExpression)type).Type);
                if (IsErrored(identifier))
                    return new Expression[] { identifier };

                if (!Match(tkComma))
                {
                    if (!Match(tkRpar))
                        return new Expression[] { new ErroredExpression(new MissingComma(Lexer.CurrentToken)) };
                }
                else
                    Lexer.GetNextToken();

                argumens.Add(new ArgumentDeclaration(type, identifier));
            }

            if (!Match(tkRpar))
                return new Expression[] { new ErroredExpression(new MissingRpar(Lexer.CurrentToken)) };
            Lexer.GetNextToken();

            //args.SourceContext.EndsAt = new Position(Lexer.CurrentToken);

            return argumens.ToArray();
        }
        public Expression ParseMethodInvokationExpression()
        {
            SourceContext methodContext = new SourceContext();
            methodContext.FilePath = CurrentParsingFile;
            methodContext.StartsAt = Lexer.CurrentToken.Context.StartsAt;
            IdentifierCall methodName = new IdentifierCall(Lexer.CurrentToken);
            Lexer.GetNextToken();

            Expression[] methodParameters = ParseParametersDeclarations();
            methodContext.EndsAt = Lexer.CurrentToken.Context.EndsAt;

            return new MethodInvokationExpression(methodName.Name, methodParameters, methodContext);
        }

        public Expression ParseTypeExpression()
        {
            if (!Match(tkType))
                return new ErroredExpression(new TypeExpected(Lexer.CurrentToken));
            TypeExpression typeExpression = new TypeExpression(Lexer.CurrentToken);
            Lexer.GetNextToken();
            return typeExpression;
        }
        public Expression ParseIdentifierExpression(IdentifierExpression.State state, InnerType type = null)
        {
            if (state == IdentifierExpression.State.Call)
            {
                if (!Match(tkIdentifier))
                    return new ErroredExpression(new IdentifierExpected(Lexer.CurrentToken));
                IdentifierExpression identifier = new IdentifierExpression(Lexer.CurrentToken,state);
                Lexer.GetNextToken();
                return identifier;
            }
            else
            {
                if (!Match(tkIdentifier))
                    return new ErroredExpression(new IdentifierExpected(Lexer.CurrentToken));
                IdentifierExpression identifierExpression;

                if (type == null)
                    identifierExpression = new IdentifierExpression(Lexer.CurrentToken,state);
                else
                {
                    if (type is InnerTypes.Void)
                        return new ErroredExpression(new ErrorMessage("Тип void недопустим для переменной", Lexer.PreviousToken));
                    identifierExpression = new IdentifierExpression(Lexer.CurrentToken, type, state);
                }
                Lexer.GetNextToken();
                return identifierExpression;
            }
        }
        public Expression ParseArrayInstance()
        {
            SourceContext arrayContext = new SourceContext();
            arrayContext.FilePath = CurrentParsingFile;
            arrayContext.StartsAt = new Position(Lexer.CurrentToken);

            Expression constructionType = ParseTypeExpression();
            if (IsErrored(constructionType))
                return new ErroredExpression(new TypeExpected(Lexer.CurrentToken));

            int constructionDimension = 1;
            List<Expression> sizes = new List<Expression>();

            Lexer.GetNextToken();

            while (!Match(tkRpar) && !Match(tkEOF))
            {
                sizes.Add(ParseExpression());
                if (!Match(tkComma))
                {
                    if (!Match(tkRpar))
                        return new ErroredExpression(new ReservedSymbolExpected(",", Lexer.CurrentToken));
                }
                else
                {
                    constructionDimension++;
                    Lexer.GetNextToken();
                }
            }
            Lexer.GetNextToken();
            ((TypeExpression)constructionType).Type = ((TypeExpression)constructionType).Type.CreateArrayInstance(sizes.Count);

            arrayContext.EndsAt = new Position(Lexer.CurrentToken);
            constructionType.SourceContext = arrayContext;

            if (sizes.Count < 1)
                return new ErroredExpression(new ErrorMessage("Ожидался размер массива", Lexer.CurrentToken));

            //Вывод ошибки не очень (по позиции)
            return new ArrayInstance(constructionType, sizes.ToArray());
        }
        public Expression ParseArrayElementExpression()
        {
            List<Expression> indexes = new List<Expression>();
            IdentifierExpression identifier = (IdentifierExpression)ParseIdentifierExpression(IdentifierExpression.State.Decl);

            if (!Match(tkSqLbra))
                return new ErroredExpression(new ErrorMessage("Для получения элемента по индексу массива нужен символ \'[\'", Lexer.CurrentToken));
            Lexer.GetNextToken();

            while (!Match(tkSqRbra) && !Match(tkEOF))
            {
                indexes.Add(ParseExpression());

                if (!Match(tkComma))
                {
                    if (!Match(tkSqRbra))
                        return new ErroredExpression(new ReservedSymbolExpected(",", Lexer.CurrentToken));
                }
                else
                    Lexer.GetNextToken();
            }
            Lexer.GetNextToken();
            return new ArrayElementExpression(identifier, indexes.ToArray());
        }
        public Expression ParseAdressor(IdentifierExpression.State state = IdentifierExpression.State.Call)
        {
            if (Match(tkSqLbra, 1))
                return ParseArrayElementExpression();
            else
                return ParseIdentifierExpression(state);
        }

        public Statement ParseImportStatement()
        {
            List<Expression> modules = new List<Expression>();

            if (!Match(tkImport))
                return new ErroredStatement(new ReservedWordExpected("import", Lexer.CurrentToken));
            Lexer.GetNextToken();
            
            //import first,second,third ... ;
            while (!Match(tkEOF) && !Match(tkSemicolon))
            {
                Expression import = ParseFactor();
                if (import is StringConstant | import is IdentifierExpression)
                    modules.Add(import);
                else
                    return new ErroredStatement(new ExpectedCorrectImport(Lexer.CurrentToken.Context));

                if (!Match(tkComma))
                {
                    if (!Match(tkSemicolon))
                        return new ErroredStatement(new MissingComma(Lexer.CurrentToken));
                }
                else
                    Lexer.GetNextToken();
            }

            if (!Match(tkSemicolon))
                return new ErroredStatement(new MissingSemi(Lexer.PreviousToken));
            Lexer.GetNextToken();

            return new ImportStatement(modules.ToArray());
        }

        public Statement ParseMethodDeclaration()
        {
            if (Match(tkAt))
                return ParseExternalMethodDeclaration();
            if (!Match(tkFunc))
                return new ErroredStatement(new ReservedWordExpected("func", Lexer.CurrentToken));
            Lexer.GetNextToken();

            Expression funcName = ParseIdentifierExpression(IdentifierExpression.State.Decl);
            if (IsErrored(funcName))
                return new ErroredStatement(new IdentifierExpected(Lexer.CurrentToken));
            //SourceContext funcContext = Lexer.PreviousToken.Context;
            SourceContext funcContext = new SourceContext();
            funcContext.StartsAt = new Position(Lexer.PreviousToken);
            funcContext.FilePath = CurrentParsingFile;

            Expression[] args = ParseArgumentDeclarations();
            if (IsErrored(args))
                return new ErroredStatement(new ErrorMessage("Ошибка при объявлении аргумента", Lexer.CurrentToken));

            if (!Match(tkColon))
                return new ErroredStatement(new ReservedSymbolExpected(":", Lexer.CurrentToken));

            Lexer.GetNextToken();

            Expression funcType = ParseTypeExpression();
            if (IsErrored(funcType))
                return new ErroredStatement(new TypeExpected(Lexer.CurrentToken));

            Statement funcBody = ParseEmbeddedStatement();
            if (IsErrored(funcBody))
                return new ErroredStatement(new ErrorMessage("Ожидалось выражение",Lexer.CurrentToken));
            return new MethodDeclaration(funcName, args, funcType, funcBody, funcContext);
        }
        public Statement ParseExternalMethodDeclaration()
        {
            Lexer.GetNextToken();
            if (!Match(tkExternalProp))
                return new ErroredStatement(new ReservedWordExpected("external", Lexer.CurrentToken));
            Lexer.GetNextToken();

            Expression packageName = ParseStringConstant();
            if (IsErrored(packageName))
                return new ErroredStatement(new ErrorMessage("Ожидалось имя библиотеки .NET",Lexer.CurrentToken));

            if (!Match(tkFunc))
                return new ErroredStatement(new ReservedWordExpected("func", Lexer.CurrentToken));
            Lexer.GetNextToken();

            Expression funcName = ParseIdentifierExpression(IdentifierExpression.State.Decl);
            if (IsErrored(funcName))
                return new ErroredStatement(new IdentifierExpected(Lexer.CurrentToken));

            SourceContext funcContext = new SourceContext();
            funcContext.StartsAt = new Position(Lexer.CurrentToken);

            Expression[] arguments = ParseArgumentDeclarations();

            if (!Match(tkColon))
                return new ErroredStatement(new ReservedSymbolExpected(":", Lexer.CurrentToken));
            Lexer.GetNextToken();

            Expression funcType = ParseTypeExpression();
            if (IsErrored(funcType))
                return new ErroredStatement(new TypeExpected(Lexer.CurrentToken));
            funcContext.EndsAt = new Position(Lexer.CurrentToken);

            if (!Match(tkSemicolon))
                return new ErroredStatement(new ReservedSymbolExpected(";", Lexer.CurrentToken));
            Lexer.GetNextToken();

            return new MethodDeclaration(funcName, arguments, funcType,null, funcContext, ((StringConstant)packageName).Value);
        }
        public Statement ParseDeclarationStatement()
        {
            Expression type = ParseTypeExpression();
            if (IsErrored(type))
                return new ErroredStatement(new TypeExpected(Lexer.CurrentToken));

            //integer a,b,c ... ;|=
            List<Expression> identifiers = new List<Expression>();

            while (!Match(tkEOF) && !Match(tkAssign) && !Match(tkSemicolon))
            {
                Expression identifier = ParseIdentifierExpression(IdentifierExpression.State.Decl, ((TypeExpression)type).Type);
                if (IsErrored(identifier))
                    return new ErroredStatement(new IdentifierExpected(Lexer.CurrentToken));
                else
                    identifiers.Add(identifier);
                if (!Match(tkComma))
                {
                    if (!Match(tkSemicolon) && !Match(tkAssign))
                        return new ErroredStatement(new MissingComma(Lexer.CurrentToken));
                }
                else
                    Lexer.GetNextToken();
            }

            if (Match(tkAssign))
            {
                Lexer.GetNextToken();
                AssignmentStatement assign = new AssignmentStatement(identifiers.ToArray(), AssignmentStatement.AssignOperator.Assignment, ParseExpression());

                if (!Match(tkSemicolon))
                    return new ErroredStatement(new MissingSemi(Lexer.PreviousToken));
                Lexer.GetNextToken();

                return new IdentifierDeclaration(type, assign);
            }
            else if (Match(tkSemicolon))
            {
                Lexer.GetNextToken();
                return new IdentifierDeclaration(type, identifiers.ToArray());
            }
            else 
                return new ErroredStatement(new ErrorMessage("Ожидался символ [=] или [;]", Lexer.CurrentToken));
        }
        public Statement ParseAssignmentStatement()
        {
            Expression identifier = ParseAdressor(IdentifierExpression.State.Call);

            TokenType[] expectSymbols = new TokenType[] { tkAssign,tkAddAssign,tkSubAssign,tkIDivAssign,tkFDivAssign,tkMultAssign,tkPowerAssign,tkRemndrAssign,tkLShiftAssign,tkRShiftAssign,tkBitwiseOrAssign,tkBitwiseAndAssign,tkBitwiseXorAssign };

            if (!Match(expectSymbols))
                return new ErroredStatement(new ErrorMessage("Ожидался символ присваивания", Lexer.CurrentToken));
            Lexer.GetNextToken();

            AssignmentStatement assign = new AssignmentStatement(identifier, AssignmentStatement.ConvertTokenType(Lexer.PreviousToken.TokenType), ParseExpression());

            if (!Match(tkSemicolon))
                return new ErroredStatement(new MissingSemi(Lexer.CurrentToken));
            Lexer.GetNextToken();

            return assign;
        }
        public Statement ParseEmbeddedStatement()
        {
            List<Statement> statements = new List<Statement>();
            SourceContext bodyContext = new SourceContext();
            bodyContext.FilePath = CurrentParsingFile;
            bodyContext.StartsAt = new Position(Lexer.CurrentToken);

            bool isSimpleFormat = false;

            if (!Match(tkLbra))
            {
                isSimpleFormat = true;
                statements.Add(ParseStatement());
                bodyContext.EndsAt = new Position(Lexer.CurrentToken);
            }
            else
            {
                Lexer.GetNextToken();
                while (!Match(tkRbra) && !Match(tkEOF))
                {
                    statements.Add(ParseStatement());
                    if (statements.Last() is ErroredStatement) 
                        break;
                }

                if (!Match(tkRbra))
                    return new ErroredStatement(new MissingRbra(Lexer.CurrentToken));
                bodyContext.EndsAt = new Position(Lexer.CurrentToken);
                Lexer.GetNextToken();
            }

            return new EmbeddedStatement(statements.ToArray(),isSimpleFormat);
        }
        public Statement ParseStatement()
        {
            switch(Lexer.CurrentToken.TokenType)
            {
                case tkIdentifier:
                    return ParseIdentifierAmbiguityStatement();
                case tkIf:
                    return ParseIfStatement();
                case tkType:
                    return ParseDeclarationStatement();
                case tkRet:
                    return ParseReturnStatement();
                case tkBreak:
                    return ParseBreakStatement();
                case tkContinue:
                    return ParseContinueStatement();
                case tkWhile:
                    return ParseWhileLoopStatement();
                case tkDo:
                    return ParseDoLoopStatement();
                case tkFor:
                    return ParseForLoopStatement();
                default:
                    return new ErroredStatement(new ErrorMessage("Ожидалось выражение", Lexer.CurrentToken));
            }
        }
        public Statement ParseIdentifierAmbiguityStatement()
        {
            if (Match(tkLpar, 1))
                return ParseMethodInvokationStatement();
            else
                return ParseAssignmentStatement();
        }
        public Statement ParseIfStatement()
        {
            if (!Match(tkIf))
                return new ErroredStatement(new ReservedWordExpected("if", Lexer.CurrentToken));

            SourceContext ifContext = new SourceContext();
            ifContext.FilePath = CurrentParsingFile;
            ifContext.StartsAt = new Position(Lexer.CurrentToken);

            Lexer.GetNextToken();

            Expression ifCondition = ParseBooleanParentisizedExpression();
            Statement ifBody = ParseEmbeddedStatement();
            ifContext.EndsAt = new Position(Lexer.CurrentToken);
            IfStatement ifStmt = new IfStatement(ifCondition, ifBody, ifContext);

            if (Match(tkElse))
            {
                Lexer.GetNextToken();
                Statement elseBody = ParseEmbeddedStatement();
                ifContext.EndsAt = new Position(Lexer.CurrentToken);
                ifStmt = new IfStatement(ifCondition, ifBody, elseBody, ifContext);
            }
            return ifStmt;
        }
        public Statement ParseWhileLoopStatement()
        {
            if (!Match(tkWhile))
                return new ErroredStatement(new ReservedWordExpected("while", Lexer.CurrentToken));
            Lexer.GetNextToken();

            SourceContext loopContext = new SourceContext();
            loopContext.FilePath = CurrentParsingFile;
            loopContext.StartsAt = new Position(Lexer.CurrentToken);

            Expression loopCondition = ParseBooleanParentisizedExpression();
            Statement loopBody = ParseEmbeddedStatement();

            loopContext.EndsAt = new Position(Lexer.CurrentToken);

            return new WhileLoopStatement(loopCondition, loopBody, loopContext);
        }
        public Statement ParseDoLoopStatement()
        {
            if (!Match(tkDo))
                return new ErroredStatement(new ReservedWordExpected("do", Lexer.CurrentToken));
            Lexer.GetNextToken();

            SourceContext loopContext = new SourceContext();
            loopContext.FilePath = CurrentParsingFile;
            loopContext.StartsAt = new Position(Lexer.CurrentToken);

            Statement loopBody = ParseEmbeddedStatement();

            if (!Match(tkWhile))
                return new ErroredStatement(new ReservedWordExpected("while", Lexer.CurrentToken));
            Lexer.GetNextToken();

            Expression loopCondition = ParseBooleanParentisizedExpression();

            if (!Match(tkSemicolon))
                return new ErroredStatement(new MissingSemi(Lexer.PreviousToken));
            Lexer.GetNextToken();

            loopContext.EndsAt = new Position(Lexer.CurrentToken);

            return new DoLoopStatement(loopCondition,loopBody,loopContext);
        }
        public Statement ParseForLoopStatement()
        {
            if (!Match(tkFor))
                return new ErroredStatement(new ReservedWordExpected("for", Lexer.CurrentToken));
            Lexer.GetNextToken();

            if (!Match(tkLpar))
                return new ErroredStatement(new MissingLpar(Lexer.CurrentToken));
            Lexer.GetNextToken();

            SourceContext loopContext = new SourceContext();
            loopContext.FilePath = CurrentParsingFile;
            loopContext.StartsAt = new Position(Lexer.CurrentToken);

            Statement loopInit = ParseDeclarationStatement();
            Statement loopStep = ParseAssignmentStatement();
            Expression loopCondition = ParseBooleanExpression();

            if (!Match(tkRpar))
                return new ErroredStatement(new MissingRpar(Lexer.CurrentToken));
            Lexer.GetNextToken();

            Statement loopBody = ParseEmbeddedStatement();

            loopContext.EndsAt = new Position(Lexer.CurrentToken);

            return new ForLoopStatement(loopInit, loopCondition, loopStep, loopBody, loopContext);
        }
        public Statement ParseMethodInvokationStatement()
        {
            Expression method = ParseMethodInvokationExpression();

            if (!Match(tkSemicolon))
                return new ErroredStatement(new MissingSemi(Lexer.CurrentToken));
            Lexer.GetNextToken();

            return new MethodInvokationStatement(method);
        }

        public Statement ParseContinueStatement()
        {
            if (!Match(tkContinue))
                return new ErroredStatement(new ReservedWordExpected("continue", Lexer.CurrentToken));
            Token continueToken = Lexer.CurrentToken;
            Lexer.GetNextToken();
            if (!Match(tkSemicolon))
                return new ErroredStatement(new MissingSemi(Lexer.PreviousToken));
            Lexer.GetNextToken();
            return new ContinueStatement(continueToken);
        }
        public Statement ParseBreakStatement()
        {
            if (!Match(tkBreak))
                return new ErroredStatement(new ReservedWordExpected("break", Lexer.CurrentToken));
            Token breakToken = Lexer.CurrentToken;
            Lexer.GetNextToken();
            if (!Match(tkSemicolon))
                return new ErroredStatement(new MissingSemi(Lexer.PreviousToken));
            Lexer.GetNextToken();
            return new BreakStatement(breakToken);
        }
        public Statement ParseReturnStatement()
        {
            if (!Match(tkRet))
                return new ErroredStatement(new ReservedWordExpected("return", Lexer.CurrentToken));

            SourceContext retContext = new SourceContext();
            retContext.FilePath = CurrentParsingFile;
            retContext.StartsAt = new Position(Lexer.CurrentToken);
            Lexer.GetNextToken();

            Expression expression = null;
            if (!Match(tkSemicolon))
                expression = ParseExpression();

            retContext.EndsAt = new Position(Lexer.CurrentToken);

            if (!Match(tkSemicolon))
                return new ErroredStatement(new MissingSemi(Lexer.PreviousToken));
            Lexer.GetNextToken();

            return new ReturnStatement(expression, retContext);
        }

        public Expression ParseBooleanExpression()
        {
            return ParseBooleanDisjunction();
        }
        public Expression ParseBooleanDisjunction()
        {
            Expression node = ParseBooleanSrtrictDisjunction();
            if (Match(tkOr))
            {
                Lexer.GetNextToken();
                node = new BinaryBooleanExpression(node, BinaryExpression.BinaryOperator.Disjunction, ParseBooleanDisjunction());
            }
            return node;
        }
        public Expression ParseBooleanSrtrictDisjunction()
        {
            Expression node = ParseBooleanConjunction();
            if (Match(tkXor))
            {
                Lexer.GetNextToken();
                node = new BinaryBooleanExpression(node, BinaryExpression.BinaryOperator.StrictDisjunction, ParseBooleanSrtrictDisjunction());
            }
            return node;
        }
        public Expression ParseBooleanConjunction()
        {
            Expression node = ParseBooleanInversion();
            if (Match(tkAnd))
            {
                Lexer.GetNextToken();
                node = new BinaryBooleanExpression(node, BinaryExpression.BinaryOperator.Conjuction, ParseBooleanConjunction());
            }
            return node;
        }
        public Expression ParseBooleanInversion()
        {
            if (Match(tkNot))
            {
                Lexer.GetNextToken();
                return new UnaryBooleanExpression(UnaryExpression.UnaryOperator.UnaryInversion, ParseBooleanConjunction());
            }
            else
                return ParseBooleanFactor();
        }
        public Expression ParseBooleanFactor()
        {
            Expression node;
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkLpar:
                    return ParseBooleanParentisizedExpression();
                default:
                    node = ParseExpression();
                    switch (Lexer.CurrentToken.TokenType)
                    {
                        case tkLess:    Lexer.GetNextToken();   node = new BinaryBooleanExpression(node, BinaryExpression.BinaryOperator.LessThan, ParseExpression()); break;
                        case tkGreater: Lexer.GetNextToken();   node = new BinaryBooleanExpression(node, BinaryExpression.BinaryOperator.GreaterThan, ParseExpression()); break;
                        case tkEqual:   Lexer.GetNextToken();   node = new BinaryBooleanExpression(node, BinaryExpression.BinaryOperator.Equal, ParseExpression()); break;
                        case tkNotEqual:  Lexer.GetNextToken(); node = new BinaryBooleanExpression(node, BinaryExpression.BinaryOperator.NotEqual, ParseExpression()); break;
                        case tkEqualLess: Lexer.GetNextToken(); node = new BinaryBooleanExpression(node, BinaryExpression.BinaryOperator.LessEqualThan, ParseExpression()); break;
                        case tkEqualGreater: Lexer.GetNextToken(); node = new BinaryBooleanExpression(node, BinaryExpression.BinaryOperator.GreaterEqualThan, ParseExpression()); break;
                    }
                    return node;
            }
        }
        public Expression ParseBooleanParentisizedExpression()
        {
            if (!Match(tkLpar))
                return new ErroredExpression(new MissingLpar(Lexer.CurrentToken));
            Lexer.GetNextToken();

            Expression node = ParseBooleanExpression();

            if (!Match(tkRpar))
                return new ErroredExpression(new MissingRpar(Lexer.CurrentToken));
            Lexer.GetNextToken();

            return node;
        }

        public Expression ParseExpression()
        {
            return ParseBitwiseOr();
        }
        public Expression ParseBitwiseOr()
        {
            Expression node = ParseBitwiseXor();
            if (Match(tkBitwiseOr))
            {
                Lexer.GetNextToken();
                node = new BinaryArithExpression(node, BinaryExpression.BinaryOperator.BitwiseOr, ParseBitwiseOr());
            }
            return node;
        }
        public Expression ParseBitwiseXor()
        {
            Expression node = ParseBitwiseAnd();
            if (Match(tkBitwiseXor))
            {
                Lexer.GetNextToken();
                node = new BinaryArithExpression(node, BinaryExpression.BinaryOperator.BitwiseXor, ParseBitwiseXor());
            }
            return node;
        }
        public Expression ParseBitwiseAnd()
        {
            Expression node = ParseShift();
            if (Match(tkBitwiseAnd))
            {
                Lexer.GetNextToken();
                node = new BinaryArithExpression(node, BinaryExpression.BinaryOperator.BitwiseAnd, ParseBitwiseAnd());
            }
            return node;
        }
        public Expression ParseShift()
        {
            Expression node = ParseAdditive();
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkLShift:
                    Lexer.GetNextToken();
                    node = new BinaryArithExpression(node,BinaryExpression.BinaryOperator.LShift,ParseShift());
                    break;
                case tkRShift:
                    Lexer.GetNextToken();
                    node = new BinaryArithExpression(node, BinaryExpression.BinaryOperator.RShift, ParseShift());
                    break;
            }
            return node;
        }
        public Expression ParseAdditive()
        {
            //bad representation
            Expression node;
            if (Match(tkMinus))
            {
                Lexer.GetNextToken();
                node = new UnaryArithExpression(UnaryExpression.UnaryOperator.UnaryMinus, ParseMultiplicative());
            }
            else
                node = ParseMultiplicative();

            switch (Lexer.CurrentToken.TokenType)
            {
                case tkPlus: Lexer.GetNextToken(); node = new BinaryArithExpression(node, BinaryExpression.BinaryOperator.Addition, ParseAdditive()); break;
                case tkMinus:
                    Lexer.GetNextToken();
                    node = new BinaryArithExpression(node, BinaryExpression.BinaryOperator.Substraction, ParseMultiplicative());
                    switch (Lexer.CurrentToken.TokenType)
                    {
                        case tkPlus:
                            Lexer.GetNextToken();
                            node = new BinaryArithExpression(node, BinaryExpression.BinaryOperator.Addition, ParseAdditive());
                            break;
                        case tkMinus:
                            node = new BinaryArithExpression(node, BinaryExpression.BinaryOperator.Addition, ParseAdditive());
                            break;
                    }
                    break;
            }
            return node;
        }
        public Expression ParseMultiplicative()
        {
            Expression node = ParseExponentiation();
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkMult: 
                    Lexer.GetNextToken(); 
                    node = new BinaryArithExpression(node, BinaryExpression.BinaryOperator.Mult, ParseMultiplicative()); 
                    break;
                case tkIDiv: 
                    Lexer.GetNextToken(); 
                    node = new BinaryArithExpression(node,BinaryExpression.BinaryOperator.IDiv, ParseMultiplicative()); 
                    break;
                case tkFDiv: 
                    Lexer.GetNextToken(); 
                    node = new BinaryArithExpression(node, BinaryExpression.BinaryOperator.FDiv, ParseMultiplicative()); 
                    break;
            }
            return node;
        }
        public Expression ParseExponentiation()
        {
            Expression node = ParseFactor();
            if (Match(tkPower))
            {
                Lexer.GetNextToken();
                node = new BinaryArithExpression(node, BinaryExpression.BinaryOperator.Power, ParseExponentiation());
            }
            return node;
        }
        public Expression ParseFactor()
        {
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkIdentifier:
                    return ParseIdentifierAmbiguityExpression();
                case tkMinus:
                    return ParseUnaryMinusExpression();

                case tkType:
                case tkDQuote:
                case tkSQuote:
                case tkIntConst:
                case tkRealConst:
                case tkBooleanConst:
                    return ParseConstantExpression();

                case tkLpar:
                    return ParseArithParentisizedExpression();

                default:
                    return new ErroredExpression(new ErrorMessage("Ожидалось присваиваемое выражение", Lexer.CurrentToken));
            }
        }
        public Expression ParseArithParentisizedExpression()
        {
            if (!Match(tkLpar))
                return new ErroredExpression(new MissingLpar(Lexer.CurrentToken));
            Lexer.GetNextToken();

            Expression node = ParseExpression();

            if (!Match(tkRpar))
                return new ErroredExpression(new MissingRpar(Lexer.CurrentToken));
            Lexer.GetNextToken();

            return node;
        }

        public Expression ParseUnaryMinusExpression()
        {
            if (Match(tkMinus, -1))
                return new ErroredExpression(new ErrorMessage("Возможно добавление только одного унарного минуса", Lexer.CurrentToken));
            Lexer.GetNextToken();

            return new UnaryArithExpression(UnaryExpression.UnaryOperator.UnaryMinus, ParseMultiplicative());
        }
        public Expression ParseIdentifierAmbiguityExpression()
        {
            if (Match(tkLpar, 1))
                return ParseMethodInvokationExpression();
            else if (Match(tkSqLbra, 1))
                return ParseArrayElementExpression();
            else
                return ParseIdentifierExpression(IdentifierExpression.State.Call);
        }
        public Expression ParseConstantExpression()
        {
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkDQuote:
                    return ParseStringConstant();
                case tkSQuote:
                    return ParseCharConstant();
                case tkIntConst:
                    //todo integral type -> byte,short .. long
                    return ParseIntegralConstant();
                case tkRealConst:
                    return ParseRealConstant();
                case tkBooleanConst:
                    return ParseBooleanConstant();
                //array instance
                case tkType:
                    return ParseArrayInstance();
                default:
                    return new ErroredExpression(new ErrorMessage("Ожидалось константное выражение",Lexer.CurrentToken));
            }
        }
        public Expression ParseIntegralConstant()
        {
            if (!Match(tkIntConst))
                return new ErroredExpression(new ErrorMessage("Ожидалась целочисленная константа", Lexer.CurrentToken));
            Lexer.GetNextToken();
            //for the short time
            return new Int32Constant(Lexer.PreviousToken);
        }
        public Expression ParseRealConstant()
        {
            if (!Match(tkRealConst))
                return new ErroredExpression(new ErrorMessage("Ожидалась вещественная константа", Lexer.CurrentToken));
            Lexer.GetNextToken();
            //for the short time
            return new SingleConstant(Lexer.PreviousToken);
        }
        public Expression ParseStringConstant()
        {
            if (!Match(tkDQuote))
                return new ErroredExpression(new ReservedSymbolExpected("\"", Lexer.CurrentToken));
            Lexer.GetNextToken();

            if (!Match(tkStringConst))
                return new ErroredExpression(new ErrorMessage("Ожидалась строка.", Lexer.CurrentToken));

            StringConstant stringConst = new StringConstant(Lexer.CurrentToken);
            Lexer.GetNextToken();

            if (!Match(tkDQuote))
                return new ErroredExpression(new ReservedSymbolExpected("\"", Lexer.CurrentToken));
            Lexer.GetNextToken();

            return stringConst;
        }
        public Expression ParseCharConstant()
        {
            if (!Match(tkSQuote))
                return new ErroredExpression(new ReservedSymbolExpected("\'", Lexer.CurrentToken));
            Lexer.GetNextToken();

            if (!Match(tkCharConst))
                return new ErroredExpression(new ErrorMessage("Ожидался символ", Lexer.CurrentToken));

            CharConstant charConst = new CharConstant(Lexer.CurrentToken);
            Lexer.GetNextToken();

            if (!Match(tkSQuote))
                return new ErroredExpression(new ReservedSymbolExpected("\'", Lexer.CurrentToken));
            Lexer.GetNextToken();

            return charConst;
        }
        public Expression ParseBooleanConstant()
        {
            if (!Match(tkBooleanConst))
                return new ErroredExpression(new ErrorMessage("Ожидалась логическая константа", Lexer.CurrentToken));
            Lexer.GetNextToken();
            //for the short time
            return new BooleanConstant(Lexer.PreviousToken);
        }

        public Expression GetErrored(Expression[] expressions)
        {
            foreach (Expression expression in expressions)
                if (expression is ErroredExpression)
                    return expression;
            return null;
        }
        public bool IsErrored(Expression[] expressions)
        {
            foreach (Expression expression in expressions)
                if (expression is ErroredExpression)
                    return true;
            return false;
        }
        public bool IsErrored(Expression expression)
        {
            return expression is ErroredExpression ? true : false;
        }
        public bool IsErrored(Statement statement)
        {
            return statement is ErroredStatement ? true : false;
        }
        public bool IsErrored(SyntaxTreeNode node)
        {
            return node is ErroredStatement ? true : false;
        }
        public bool Match(TokenType[] expectations, int offset = 0)
        {
            foreach (TokenType type in expectations)
                if (Match(type, offset))
                    return true;
            return false;
        }
        public bool Match(TokenType expectedKind, int offset = 0)
        {
            return Lexer.Peek(offset).TokenType == expectedKind ? true : false;
        }
        public bool Match(SyntaxTreeNode node, NodeType expectedKind)
        {
            return node.NodeKind == expectedKind ? true : false;
        }
        public bool Match(Expression expression, NodeType expectedKind)
        {
            return expression.NodeKind == expectedKind ? true : false;
        }
    }
}

namespace alm.Core.FrontEnd.SyntaxAnalysis.new_parser_concept.syntax_tree
{
    public abstract class SyntaxTreeNode
    {
        public virtual NodeType NodeKind { get; }
        public virtual ConsoleColor ConsoleColor { get; protected set; } = ConsoleColor.Gray;

        public SyntaxTreeNode Parent { get; set; }
        public List<SyntaxTreeNode> Childs { get; set; } = new List<SyntaxTreeNode>();

        public bool ErrorReported { get; private set; }

        public SourceContext SourceContext { get; set; }

        public void SetSourceContext(Token token) => this.SourceContext = GetSourceContext(token);
        public void SetSourceContext(SyntaxTreeNode node) => this.SourceContext = GetSourceContext(node);
        public void SetSourceContext(Token sToken, Token fToken) => this.SourceContext = GetSourceContext(sToken, fToken);
        public void SetSourceContext(SyntaxTreeNode lnode, SyntaxTreeNode rnode) => this.SourceContext = GetSourceContext(lnode, rnode);

        public void AddNode(SyntaxTreeNode node)
        {
            if (this.Childs == null)
                this.Childs = new List<SyntaxTreeNode>();

            if (node == null)
                return;

            node.Parent = this;
            this.Childs.Add(node);
        }
        public void AddNodes(params SyntaxTreeNode[] nodes)
        {
            foreach (SyntaxTreeNode node in nodes)
                AddNode(node);
        }

        public SyntaxTreeNode GetParentByType(Type type)
        {
            for (SyntaxTreeNode Parent = this.Parent; Parent != null; Parent = Parent.Parent)
                if (Parent.GetType().ToString() == type.ToString())
                    return Parent;
            return null;
        }
        public SyntaxTreeNode[] GetChildsByType(Type type, bool recursive = false, bool checkThisOnce = true)
        {
            List<SyntaxTreeNode> Childs = new List<SyntaxTreeNode>();
            if (checkThisOnce)
                if (this.GetType().ToString() == type.ToString())
                    Childs.Add(this);
            for (int i = 0; i < this.Childs.Count; i++)
            {
                if (this.Childs[i].GetType().ToString() == type.ToString())
                    Childs.Add(this.Childs[i]);
                if (recursive)
                    Childs.AddRange(this.Childs[i].GetChildsByType(type, true, false));
            }
            return Childs.ToArray();
        }

        public override string ToString() => $"{NodeKind}";
    }
    public sealed class ErroredStatement : Statement
    {
        public override NodeType NodeKind => NodeType.ErrorInStatement;
        public override ConsoleColor ConsoleColor => ConsoleColor.DarkRed;

        public ErroredStatement(SyntaxError error) => Diagnostics.SyntaxErrors.Add(error);
    }
    public sealed class ErroredExpression : Expression
    {
        public override NodeType NodeKind => NodeType.ErrorInExpression;
        public override ConsoleColor ConsoleColor => ConsoleColor.DarkRed;

        public ErroredExpression(SyntaxError error) => Diagnostics.SyntaxErrors.Add(error);
    }

    public sealed class ModuleRoot : SyntaxTreeNode
    {
        public string ModulePath { get; private set; }
        public SyntaxTreeNode Module { get; private set; }

        public override NodeType NodeKind => NodeType.Program;

        public ModuleRoot(string modulePath)
        {
            this.ModulePath = modulePath;
        }

        public ModuleRoot(string modulePath,SyntaxTreeNode root)
        {
            this.ModulePath = modulePath;
            this.Module = root;
            this.AddNode(root);
        }

        public override string ToString() => $"Module [{this.ModulePath}]";
    }

    public abstract class Statement : SyntaxTreeNode
    {
        public override ConsoleColor ConsoleColor => ConsoleColor.Blue;
    }
    public sealed class ImportStatement : Statement
    {
        private string LibPath = @"C:\Users\Almes\source\repos\Compiler\compiler v.5\src\libs";

        public string[] ImportPaths { get; private set; }
        public SyntaxTreeNode[] ImportRoots { get; private set; }

        public override NodeType NodeKind => NodeType.Import;
        public override ConsoleColor ConsoleColor => ConsoleColor.Red;

        public ImportStatement(Expression[] modules)
        {
            if (modules.Length > 2)
                this.SetSourceContext(modules[0], modules.Last());
            else
                this.SetSourceContext(modules[0]);
            this.ImportPaths = this.CreatePathInstances(modules);
            this.TryToJoinImportedModules();
        }
        
        private string GetLibImportPath(string path)
        {
            return System.IO.Path.Combine(this.LibPath, path + ".alm");
        }
        private string GetDirectImportPath(string path)
        {
            //change
            string parsingDir  = System.IO.Path.GetDirectoryName(CompilationVariables.CurrentParsingModule);
            string newParsingPath = System.IO.Path.Combine(parsingDir, path);
            return System.IO.File.Exists(newParsingPath) ? newParsingPath : path;
        }

        //trying to create the ast from module
        private void TryToJoinImportedModules()
        {
            List<SyntaxTreeNode> importedModules = new List<SyntaxTreeNode>(); 
            foreach (string currentImportedModule in this.ImportPaths)
            {
                if (!System.IO.File.Exists(currentImportedModule))
                {
                    importedModules.Add(new ErroredStatement(new WrongImport()));
                    return;
                }

                if (System.IO.Path.GetExtension(currentImportedModule) != ".alm")
                {
                    importedModules.Add(new ErroredStatement(new WrongImport()));
                    return;
                }
                //case when trying import module where this import was called
                if (currentImportedModule == CompilationVariables.CurrentParsingModule)
                {
                    importedModules.Add(new ErroredStatement(new WrongImport()));
                    return;
                }
                //case when trying import already imported module
                if (this.GetImportsModules(currentImportedModule).Contains(CompilationVariables.CurrentParsingModule))
                {
                    importedModules.Add(new ErroredStatement(new WrongImport()));
                    return;
                }
                //if find import at list in 1 module, skip it (because it alrealdy imported) 
                if (this.GetImportsModules(currentImportedModule).Length > 0)
                {
                    return;
                }

                if (!CompilationVariables.CompilationImports.ContainsKey(CompilationVariables.CurrentParsingModule))
                    CompilationVariables.CompilationImports.Add(CompilationVariables.CurrentParsingModule, new List<string>() { currentImportedModule });
                else
                    CompilationVariables.CompilationImports[CompilationVariables.CurrentParsingModule].Add(currentImportedModule);

                //Parsing module by path 
                Lexer lexer = new Lexer(currentImportedModule);
                Parser2 parser = new Parser2(lexer);
                SyntaxTreeNode importedModule = parser.Parse(currentImportedModule);

                CompilationVariables.CurrentParsingModule = CompilationVariables.CompilationEntryModule;

                //foreach (SyntaxTreeNode child in importedModule.Childs)
                    this.AddNode(importedModule);

                importedModules.Add(importedModule);
            }
            this.ImportRoots = importedModules.ToArray();
        }
        //gets modules where this import was mentioned
        private string[] GetImportsModules(string import)
        {
            List<string> modules = new List<string>();
            foreach (string key in CompilationVariables.CompilationImports.Keys)
            {
                foreach (string value in CompilationVariables.CompilationImports[key])
                {
                    if (value == import)
                    {
                        modules.Add(key);
                        break;
                    }
                }
            }
            return modules.ToArray();
        }

        private string[] CreatePathInstances(Expression[] expressions)
        {
            string[] paths = new string[expressions.Length];
            for (int i = 0; i < expressions.Length; i++)
                if (expressions[i] is StringConstant)
                    paths[i] = this.GetDirectImportPath(((StringConstant)expressions[i]).Value);
                else
                    paths[i] = this.GetLibImportPath(((IdentifierExpression)expressions[i]).Name);
            return paths;
        }

        public override string ToString() => $"Import [modules:{this.ImportPaths.Length}]";
    }

    public sealed class MethodDeclaration : Statement
    {
        public string Name { get; private set; }
        public ushort ArgCount { get; private set; }
        public InnerType ReturnType { get; private set; }

        public EmbeddedStatement Body { get; private set; }
        public ArgumentDeclaration[] Arguments { get; private set; }

        public bool IsExternal { get; private set; }
        public string NETPackage { get; private set; }

        public override NodeType NodeKind => NodeType.MethodDeclaration;
        public override ConsoleColor ConsoleColor => ConsoleColor.DarkGreen;

        public MethodDeclaration(Expression identifier, Expression[] arguments, Expression type, Statement body, SourceContext context, string package = "")
        {
            this.IsExternal = package == "" ? false : true;
            this.Arguments = this.CreateArgumentDeclarationInstances(arguments);
            this.Body = (EmbeddedStatement)body;
            this.Name = ((IdentifierExpression)identifier).Name;
            this.ReturnType = ((TypeExpression)type).Type;
            this.SourceContext = context;
            this.ArgCount = (ushort)arguments.Length;

            foreach (var argument in arguments)
                this.AddNode(argument);
            if (!IsExternal)
                this.AddNode(body);
            else
                this.NETPackage = package;
        }

        public InnerType GetArgumentsType(string argName)
        {
            foreach (ArgumentDeclaration argument in Arguments)
                if (argName == argument.Identifier.Name)
                    return argument.Type;
            return null;
        }
        public ArgumentDeclaration[] CreateArgumentDeclarationInstances(Expression[] expressions)
        {
            ArgumentDeclaration[] arguments = new ArgumentDeclaration[expressions.Length];

            for (int i = 0; i < expressions.Length; i++)
                arguments[i] = (ArgumentDeclaration)expressions[i];

            return arguments;
        }

        public override string ToString() => (this.IsExternal?"ext ":"") + $"func {this.Name}(args:{this.ArgCount})->{this.ReturnType}";
    }
    public sealed class IdentifierDeclaration : Statement
    {
        // <identifier_type> <identifier> | <identifier_type> <identifier> '=' <expression>
        public InnerType DeclaringIdentifierType { get; private set; }
        public IdentifierExpression[] DeclaringIdentifiers { get; private set; }
        public AssignmentStatement AssingningExpression { get; private set; }

        public override NodeType NodeKind => NodeType.Declaration;
        public override ConsoleColor ConsoleColor => ConsoleColor.Blue;

        public IdentifierDeclaration(Expression type, Expression[] identifiers)
        {
            if (identifiers.Length > 0)
                this.SetSourceContext(type, identifiers.Last());
            else
                this.SetSourceContext(type);

            this.DeclaringIdentifierType = ((TypeExpression)type).Type;
            this.DeclaringIdentifiers = this.CreateIdentifierInstances(identifiers, ((TypeExpression)type).Type);
            this.AddNodes(identifiers);
        }

        //declaration with assignment
        public IdentifierDeclaration(Expression type, AssignmentStatement assignment)
        {
            this.SetSourceContext(type, assignment);
            this.AssingningExpression = assignment;
            this.DeclaringIdentifierType = ((TypeExpression)type).Type;
            this.DeclaringIdentifiers = this.CreateIdentifierInstances(assignment.AdressorExpressions, ((TypeExpression)type).Type);
            this.AddNode(assignment);
        }

        //TODO mult decls -> integer a,b,c = 2; | float a,v,c;

        private IdentifierExpression[] CreateIdentifierInstances(Expression[] expressions,InnerType withType)
        {
            IdentifierExpression[] identifiers = new IdentifierExpression[expressions.Length];
            for (int i =0; i < expressions.Length; i++)
            {
                identifiers[i] = (IdentifierExpression)expressions[i];
                identifiers[i].Type = withType;
            }
            return identifiers;
        }

        public override string ToString() => this.AssingningExpression == null ? "Declaration" : "Decl & Init";
    }

    public abstract class JumpStatement : Statement
    {
        //return , continue , break , goto(?)
        public override ConsoleColor ConsoleColor => ConsoleColor.Red;

        //??
        public bool IsReturn() => this is ReturnStatement ? true : false;
        public bool IsContinue() => this is ContinueStatement ? true : false;
        public bool IsBreak() => this is BreakStatement ? true : false;

        public bool IsSituatedInLoop()
        {
            if (this.GetParentByType(typeof(WhileLoopStatement)) == null &&
               this.GetParentByType(typeof(DoLoopStatement)) == null)
                return false;
            return true;
        }
    }
    public sealed class ContinueStatement : JumpStatement
    {
        public bool IsSituatedCorrectly { get; private set; }

        public override NodeType NodeKind => NodeType.Continue;
        //????
        public ContinueStatement(Token token)
        {
            this.SetSourceContext(token);
            this.IsSituatedCorrectly = base.IsSituatedInLoop();
        }
    }
    public sealed class BreakStatement : JumpStatement
    {
        public bool IsSituatedCorrectly { get; private set; }

        public override NodeType NodeKind => NodeType.Break;
        //????
        public BreakStatement(Token token)
        {
            this.SetSourceContext(token);
            this.IsSituatedCorrectly = base.IsSituatedInLoop();
        }
    }
    public sealed class ReturnStatement : JumpStatement
    {
        public Expression ReturnBody { get; private set; }
        public bool IsVoidReturn { get; private set; }

        public override NodeType NodeKind => NodeType.Return;

        public ReturnStatement(Expression returnBody, SourceContext context)
        {
            this.SourceContext = context;
            this.ReturnBody = returnBody;

            if (returnBody == null)
                this.IsVoidReturn = true;
            else
                this.AddNode(returnBody);
        }
    }

    public abstract class IterationStatement : Statement
    {
        public override ConsoleColor ConsoleColor => ConsoleColor.DarkBlue;

        public Expression Condition { get; protected set; }
        public Statement  Body { get; protected set; }

        public override string ToString() => $"{this.NodeKind}";
    }
    public sealed class DoLoopStatement : IterationStatement
    {
        public override NodeType NodeKind => NodeType.Do;

        public DoLoopStatement(Expression condition,Statement body,SourceContext context)
        {
            this.SourceContext = context;
            this.Condition = condition;
            this.Body = body;
            this.AddNodes(condition,body);
        }

        public override string ToString() => "Do-While";
    }
    public sealed class WhileLoopStatement : IterationStatement
    {
        public override NodeType NodeKind => NodeType.While;

        public WhileLoopStatement(Expression condition, Statement body, SourceContext context)
        {
            this.SourceContext = context;
            this.Condition = condition;
            this.Body = body;
            this.AddNodes(condition, body);
        }
    }
    public sealed class ForLoopStatement : IterationStatement
    {
        public IdentifierExpression[] IterationIdentifiers { get; private set; } 

        public Statement InitExpression { get; private set; }
        public Statement StepExpression { get; private set; }

        public override NodeType NodeKind => NodeType.For;

        public ForLoopStatement(Statement initBlock, Expression conditionalBlock, Statement stepBlock,Statement body, SourceContext context)
        {
            this.SourceContext = context;
            this.InitExpression = initBlock;
            this.Condition = conditionalBlock;
            this.StepExpression = stepBlock;
            this.Body = body;

            //if (initBlock != null)
             //   this.IterationIdentifiers = GetIdentifiers(initBlock);

            this.AddNodes(initBlock,conditionalBlock,stepBlock,body);
        }

        //bad
        private IdentifierExpression[] GetIdentifiers(Expression initExpression) => 
            initExpression.GetChildsByType(typeof(IdentifierExpression))==null?new IdentifierExpression[] { } : (IdentifierExpression[])initExpression.GetChildsByType(typeof(IdentifierExpression));
    }

    public abstract class SelectionStatement : Statement
    {
        // if , else , switch , case
        public Expression Condition { get; protected set; }
        public Statement Body { get; protected set; }
        public Statement ElseBody { get; protected set; }

        public override ConsoleColor ConsoleColor => ConsoleColor.Cyan;
    }
    public sealed class IfStatement : SelectionStatement
    {
        public override NodeType NodeKind => NodeType.If;

        public IfStatement(Expression condition, Statement body, SourceContext context)
        {
            this.SourceContext = context;
            this.Condition = condition;
            this.Body = body;
            this.AddNodes(condition,body);
        }

        public IfStatement(Expression condition, Statement body, Statement elseBody, SourceContext context)
        {
            this.SourceContext = context;
            this.Condition = condition;
            this.Body = body;
            this.ElseBody = elseBody;
            this.AddNodes(condition,body,elseBody);
        }
    }

    public abstract class ExpressionStatement : Statement
    {
        public override ConsoleColor ConsoleColor => ConsoleColor.Blue;
    }
    public sealed class MethodInvokationStatement : ExpressionStatement
    {
        //represents method invoke expression like single statement
        public Expression Instance { get; private set; } 

        public override NodeType NodeKind => NodeType.MethodInvokationAsStatement;
        public override ConsoleColor ConsoleColor => ConsoleColor.Blue;

        public MethodInvokationStatement(Expression methodInvokation)
        {
            this.SetSourceContext(methodInvokation);
            this.Instance = methodInvokation;
            foreach (SyntaxTreeNode child in methodInvokation.Childs)
                this.AddNode(child);
        }

        public override string ToString()
        {
            if (Instance is MethodInvokationExpression)
            {
                MethodInvokationExpression copy = (MethodInvokationExpression)Instance;
                return $"{copy.Name}(params:{copy.ArgCount})->{copy.ReturnType}";
            }
            else
                return "MethodInvoke";
        }
    }
    public sealed class AssignmentStatement : ExpressionStatement
    {
        // <adressor> <assignsymbl> <expression> 
        public enum AssignOperator
        {
            Assignment,
            AssignmentFDiv,
            AssignmentIDiv,
            AssignmentMult,
            AssignmentPower,
            AssignmentAddition,
            AssignmentRemainder,
            AssignmentSubtraction,

            AssignmentBitwiseOr,
            AssignmentBitwiseAnd,
            AssignmentBitwiseXor,

            AssignmentLShift,
            AssignmentRShift
        }

        public AssignOperator OperatorKind { get; private set; }

        public Expression[] AdressorExpressions { get; private set; }
        public Expression AdressableExpression => (Expression)this.Childs.Last();

        public bool MultipleAssign { get; private set; }

        public override NodeType NodeKind => NodeType.AssignmentStatement;

        public AssignmentStatement(Expression adressor,AssignOperator operatorKind, Expression adressable)
        {
            this.SetSourceContext(adressor,adressable);
            this.OperatorKind = operatorKind;
            this.AdressorExpressions = new Expression[] { adressor };
            this.AddNodes(adressor, GetExtendedAdressableExpression(adressable));
        }

        //constructor for multiple declaring & initialization
        public AssignmentStatement(Expression[] adressors, AssignOperator operatorKind, Expression adressable)
        {
            this.SetSourceContext(adressors[0], adressable);
            this.OperatorKind = operatorKind;
            this.AdressorExpressions = adressors;
            this.AddNodes(adressors);
            this.AddNode(adressable);
        }

        public static BinaryArithExpression.BinaryOperator ConvertToBinaryArithOperator(AssignOperator assignOperator)
        {
            switch (assignOperator)
            {
                case AssignOperator.AssignmentAddition:
                    return BinaryExpression.BinaryOperator.Addition;

                case AssignOperator.AssignmentSubtraction:
                    return BinaryExpression.BinaryOperator.Substraction;

                case AssignOperator.AssignmentMult:
                    return BinaryExpression.BinaryOperator.Mult;

                case AssignOperator.AssignmentFDiv:
                    return BinaryExpression.BinaryOperator.FDiv;

                case AssignOperator.AssignmentIDiv:
                    return BinaryExpression.BinaryOperator.IDiv;

                case AssignOperator.AssignmentPower:
                    return BinaryExpression.BinaryOperator.Power;

                case AssignOperator.AssignmentRemainder:
                    return BinaryExpression.BinaryOperator.Remainder;

                case AssignOperator.AssignmentBitwiseOr:
                    return BinaryExpression.BinaryOperator.BitwiseOr;

                case AssignOperator.AssignmentBitwiseAnd:
                    return BinaryExpression.BinaryOperator.BitwiseAnd;

                case AssignOperator.AssignmentBitwiseXor:
                    return BinaryExpression.BinaryOperator.BitwiseXor;

                case AssignOperator.AssignmentLShift:
                    return BinaryExpression.BinaryOperator.LShift;

                case AssignOperator.AssignmentRShift:
                    return BinaryExpression.BinaryOperator.RShift;

                default:
                    throw new Exception($"{assignOperator} idk what is this.");
            }
        }
        public static AssignOperator ConvertTokenType(TokenType type)
        {
            switch (type)
            {
                case tkAssign:       return AssignOperator.Assignment;
                case tkAddAssign:    return AssignOperator.AssignmentAddition;
                case tkIDivAssign:   return AssignOperator.AssignmentIDiv;
                case tkFDivAssign:   return AssignOperator.AssignmentFDiv;
                case tkSubAssign:    return AssignOperator.AssignmentSubtraction;
                case tkMultAssign:   return AssignOperator.AssignmentMult;
                case tkPowerAssign:  return AssignOperator.AssignmentPower;
                case tkRemndrAssign: return AssignOperator.AssignmentRemainder;

                case tkBitwiseOrAssign:  return AssignOperator.AssignmentBitwiseOr;
                case tkBitwiseAnd:       return AssignOperator.AssignmentBitwiseAnd;
                case tkBitwiseXorAssign: return AssignOperator.AssignmentBitwiseXor;

                case tkLShiftAssign: return AssignOperator.AssignmentLShift;
                case tkRShiftAssign: return AssignOperator.AssignmentRShift;

                default: throw new Exception();
            }
        }
        private Expression GetExtendedAdressableExpression(Expression expression)
        {
            if (this.OperatorKind == AssignOperator.Assignment)
                return expression;

            return new BinaryArithExpression(this.AdressorExpressions[0], AssignmentStatement.ConvertToBinaryArithOperator(this.OperatorKind), expression);
        }

        public override string ToString() => $"{this.OperatorKind}";
    }
    public sealed class EmbeddedStatement : ExpressionStatement
    {
        public bool IsSimpleFormat { get; private set; }

        public override NodeType NodeKind => NodeType.Body;

        public EmbeddedStatement(Statement[] statements, bool isSimpleFormat = false)
        {
            this.IsSimpleFormat = isSimpleFormat;
            if (statements.Length > 0)
                this.SetSourceContext(statements[0],statements[statements.Length-1]);
            foreach (Statement statement in statements)
                this.AddNodes(statement);
        }

        public override string ToString() => $"{this.NodeKind}";
    }

    public abstract class Expression : SyntaxTreeNode
    {
        //выражение
        public override ConsoleColor ConsoleColor => ConsoleColor.DarkCyan;
    }
    public sealed class ArrayInstance : Expression 
    {
        public InnerType Type { get; private set; }
        public ushort Dimension { get; private set; }

        public Expression[] DimensionSizes { get; private set; }

        public override NodeType NodeKind => NodeType.ArrayInstance;
        public override ConsoleColor ConsoleColor => ConsoleColor.DarkMagenta;
    
        public ArrayInstance(Expression type, Expression[] dimensionSizes)
        {
            this.SetSourceContext(type);
            this.Type = ((TypeExpression)type).Type;
            //+1 ??
            this.Dimension = (ushort)(dimensionSizes.Length+1);
            this.DimensionSizes = dimensionSizes;
            //this.AddNode(type);
            foreach (Expression dimensionSize in dimensionSizes)
                this.AddNode(dimensionSize);
        }

        public override string ToString() => $"{this.Type} instance";        
    }
    public sealed class ArgumentDeclaration : Expression
    {
        public InnerType Type { get; private set; }
        public IdentifierExpression Identifier { get; private set; }

        public override NodeType NodeKind => NodeType.Argument;

        public ArgumentDeclaration(Expression typeExpression, Expression identifierExpression)
        {
            this.SetSourceContext(typeExpression, identifierExpression);

            this.Type = ((TypeExpression)typeExpression).Type;
            this.Identifier = (IdentifierExpression)identifierExpression;
            //this.AddNodes(typeExpression, identifierExpression);
            this.AddNodes(identifierExpression);
        }
    }
    public sealed class ParameterDeclaration : Expression
    {
        public InnerType Type { get; set; }
        public Expression ParameterInstance { get; private set; }

        public override NodeType NodeKind => NodeType.Parameter;
        public override ConsoleColor ConsoleColor => ConsoleColor.DarkCyan;

        public ParameterDeclaration(Expression parameter)
        {
            this.SetSourceContext(parameter);

            this.ParameterInstance = parameter;
            this.AddNodes(parameter);
        }
    }

    public sealed class ArrayElementExpression : Expression
    {
        public InnerType Type { get; set; }
        public ushort Dimension { get; private set; }
        //?
        public ushort ArrayDimension { get; set; }
        public string ArrayName { get; private set; }
        public Expression[] Indexes { get; private set; }

        public override NodeType NodeKind => NodeType.ArrayElement;
        public override ConsoleColor ConsoleColor => ConsoleColor.Magenta;

        public ArrayElementExpression(IdentifierExpression identifier, Expression[] indexes)
        {
            //problems with error showing ?
            if (indexes.Length > 0)
                this.SetSourceContext(identifier, indexes[indexes.Length - 1]);
            else
                this.SetSourceContext(identifier);
            this.ArrayName = identifier.Name;
            this.Indexes = indexes;
            this.Dimension = (ushort)indexes.Length;
            //this.AddNode(identifier);
            foreach (Expression index in indexes)
                this.AddNode(index);
        }

        //string repr
        public override string ToString()
        {
            string str = string.Empty;

            str += '[';
            for (int i = 0; i < this.Dimension; i++)
                if (i == this.Dimension - 1)
                    str += "*";
                else
                    str += "*,";
            str += ']';
            return $"{this.ArrayName + str}->{this.Type}";
        }
    }
    public sealed class IdentifierExpression : Expression
    {
        public enum State
        { 
            Call = 0,
            Decl = 1
        }

        public string Name { get; private set; }
        // type of id call will init in label checker
        public InnerType Type { get; set; }
        public State IdentifierState { get; private set; }

        public override NodeType NodeKind => NodeType.Identifier;
        public override ConsoleColor ConsoleColor => ConsoleColor.Magenta;

        public IdentifierExpression(Token token,State state = State.Decl)
        {
            this.SetSourceContext(token);
            this.Name = token.Value;
            this.IdentifierState = state;
        }

        //????
        public IdentifierExpression(Token token, InnerType type, State state = State.Decl)
        {
            this.SetSourceContext(token);
            this.Type = type;
            this.Name = token.Value;
            this.IdentifierState = state;
        }

        public bool IsIdentifierCall() => this.IdentifierState == State.Call ? true : false;
        public bool IsIdentifierDecl() => this.IdentifierState == State.Decl ? true : false;

        public override string ToString() => $"{this.Name}->{this.Type}";
    }
    public sealed class TypeExpression : Expression
    {
        public InnerType Type { get; set; }

        public override NodeType NodeKind => NodeType.Type;
        public override ConsoleColor ConsoleColor => ConsoleColor.Red;

        public TypeExpression(Token token)
        {
            this.SetSourceContext(token);
            this.Type = InnerType.Parse(token.Value);
        }

        public override string ToString() => $"{Type}";
    }
    public sealed class MethodInvokationExpression : Expression
    {
        public string Name { get; private set; }
        public ushort ArgCount { get; private set; }

        public InnerType ReturnType { get; set; }
        public Expression[] Parameters { get; private set; }

        public override NodeType NodeKind => NodeType.MethodInvokation;
        public override ConsoleColor ConsoleColor => ConsoleColor.DarkYellow;

        public MethodInvokationExpression(string name, Expression[] parameters, SourceContext context)
        {
            this.Name = name;
            this.SourceContext = context;
            this.Parameters = parameters;
            this.ArgCount = (ushort)parameters.Length;

            foreach (Expression parameter in parameters)
                this.AddNode(parameter);
        }

        public override string ToString() => $"{this.Name}(params:{this.ArgCount})->{this.ReturnType}";
    }

    public abstract class ConstantExpression : Expression
    {
        public string Value { get; protected set; }
        public virtual InnerType Type { get; protected set; }
        public override ConsoleColor ConsoleColor => ConsoleColor.DarkMagenta;

        public ConstantExpression(string value)
        {
            this.Value = value;
        }

        public ConstantExpression(Token token)
        {
            this.SetSourceContext(token);
            this.Value = token.Value;
        }
        
        public override string ToString() => $"{Value}->{Type}";
    }
    public sealed class Int32Constant : ConstantExpression
    {
        public override InnerType Type => new InnerTypes.Int32();
        public override NodeType NodeKind => NodeType.IntegerConstant;

        public Int32Constant(string value) : base(value) { }
        public Int32Constant(Token token)  : base(token) { }
    }
    public sealed class SingleConstant : ConstantExpression
    {
        public override InnerType Type => new InnerTypes.Single();
        public override NodeType NodeKind => NodeType.RealConstant;

        public SingleConstant(string value) : base(value) { }
        public SingleConstant(Token token)  : base(token) { }
    }
    public sealed class BooleanConstant : ConstantExpression
    {
        public override InnerType Type => new InnerTypes.Boolean();
        public override NodeType NodeKind => NodeType.BooleanConstant;
        public override ConsoleColor ConsoleColor => ConsoleColor.Green;

        public BooleanConstant(string value) : base(value) { }
        public BooleanConstant(Token token) : base(token) { }
    }
    public sealed class CharConstant : ConstantExpression
    {
        private static char[] escapeChars = new char[]
        {
            '\'',
            '\"',
            '\\',
            '\n',
            '\t',
            '\b',
            '\f',
            '\v',
            '\0'
        };

        public override InnerType Type => new InnerTypes.Char();
        public override NodeType NodeKind => NodeType.CharConstant;
        public override ConsoleColor ConsoleColor => ConsoleColor.Yellow;

        public CharConstant(string value) : base(value) { }
        public CharConstant(Token token) : base(token) { }

        private bool IsEscapeChar()
        {
            foreach (char escapeChar in escapeChars)
                if (this.Value == escapeChar.ToString())
                    return true;
            return false;
        }

        public override string ToString() => $"\'" + (this.IsEscapeChar() ? "[esc char]" : this.Value) + "\'" + $"->{this.Type}";
    }
    public sealed class StringConstant : ConstantExpression
    {
        public override InnerType Type => new InnerTypes.String();
        public override NodeType NodeKind => NodeType.StringConstant;
        public override ConsoleColor ConsoleColor => ConsoleColor.Yellow;

        public StringConstant(string value) : base(value) { }
        public StringConstant(Token token)  : base(token) { }

        public override string ToString() => $"\"{this.Value}\"->{this.Type}";
    }

    public abstract class BinaryExpression : Expression
    {
        public enum BinaryOperator : int
        {
            Conjuction,        //And
            Disjunction,       //Or
            StrictDisjunction, //Xor

            Equal,
            NotEqual,

            LessThan,
            GreaterThan,

            LessEqualThan,
            GreaterEqualThan,

            Mult,
            FDiv, //float point division 
            IDiv, //integer division
            Power,
            Addition,
            Remainder,
            Substraction,

            BitwiseOr,
            BitwiseAnd,
            BitwiseXor,

            LShift,
            RShift
        }

        public BinaryOperator OperatorKind { get; set; }
        public InnerType LeftOperandType  { get; set; }
        public InnerType RightOperandType { get; set; }

        public Expression LeftOperand => (Expression)this.Childs[0];
        public Expression RightOperand => (Expression)this.Childs[1];

        private string CheckDefine(BinaryOperator binaryOperator)
        {
            switch (binaryOperator)
            {
                case BinaryOperator.Conjuction:
                    return "And";
                case BinaryOperator.Disjunction:
                    return "Or";
                case BinaryOperator.StrictDisjunction:
                    return "Xor";
                default:
                    return binaryOperator.ToString();
            }
        }
        public override string ToString() => $"{CheckDefine(this.OperatorKind)}";
    }
    public sealed class BinaryBooleanExpression : BinaryExpression
    {
        public override ConsoleColor ConsoleColor => ConsoleColor.Green;
        public override NodeType NodeKind => NodeType.BinaryBooleanExpression;

        public BinaryBooleanExpression(Expression leftOperand, BinaryOperator operatorKind, Expression rightOperand)
        {
            this.SetSourceContext(leftOperand,rightOperand);
            if ((int)operatorKind >= 8)
                throw new Exception($"{operatorKind} is not logical operator.");
            this.OperatorKind = operatorKind;
            this.AddNodes(leftOperand,rightOperand);
        }
    }
    public sealed class BinaryArithExpression : BinaryExpression
    {
        public override ConsoleColor ConsoleColor => ConsoleColor.Yellow;
        public override NodeType NodeKind => NodeType.BinaryArithExpression;

        public BinaryArithExpression(Expression leftOperand, BinaryOperator operatorKind, Expression rightOperand)
        {
            this.SetSourceContext(leftOperand, rightOperand);
            if ((int)operatorKind < 8)
                throw new Exception($"{operatorKind} is not arithmetical operator.");
            this.OperatorKind = operatorKind;
            this.AddNodes(leftOperand, rightOperand);
        }
    }

    public abstract class UnaryExpression : Expression
    {
        public enum UnaryOperator
        {
            UnaryInversion,

            UnaryMinus,
            PrefixIncrement,
            PrefixDecrement,

            PostfixIncrement,
            PostfixDecrement,
        }

        public InnerType OperandType { get; set; }

        public UnaryOperator OperatorKind { get; set; }
        public Expression Operand => (Expression)this.Childs[0];

        private string CheckDefine(UnaryOperator unaryOperator)
        {
            switch(unaryOperator)
            {
                case UnaryOperator.UnaryInversion:
                    return "Not";
                case UnaryOperator.UnaryMinus:
                    return "Minus";
                default:
                    return unaryOperator.ToString();
            }
        }

        public override string ToString() => $"{CheckDefine(this.OperatorKind)}";
    }
    public sealed class UnaryArithExpression : UnaryExpression
    {
        public override NodeType NodeKind => NodeType.UnaryArithexpression;
        public override ConsoleColor ConsoleColor => ConsoleColor.DarkYellow;

        public UnaryArithExpression(UnaryOperator prefixOperatorKind, Expression operand)
        {
            this.SetSourceContext(operand);
            if ((int)prefixOperatorKind < 1 && (int)prefixOperatorKind > 3)
                throw new Exception($"{prefixOperatorKind} is not prefix or arithmetical.");
            this.OperatorKind = prefixOperatorKind;
            this.AddNode(operand);
        }

        public UnaryArithExpression(Expression operand, UnaryOperator postfixOperatorKind, bool postfix = true)
        {
            this.SetSourceContext(operand);
            if ((int)postfixOperatorKind < 4)
                throw new Exception($"{postfixOperatorKind} is not postfix or arithmetical.");
            this.OperatorKind = postfixOperatorKind;
            this.AddNode(operand);
        }
    }
    public sealed class UnaryBooleanExpression : UnaryExpression
    {
        public override NodeType NodeKind => NodeType.UnaryBooleanExpression;
        public override ConsoleColor ConsoleColor => ConsoleColor.DarkYellow;

        public UnaryBooleanExpression(UnaryOperator operatorKind, Expression operand)
        {
            this.SetSourceContext(operand);
            if ((int)operatorKind != 0)
                throw new Exception($"{operatorKind} is not logical.");
            this.OperatorKind = operatorKind;
            this.AddNode(operand);
        }
    }

    #region structs
    public struct SourceContext
    {
        public Position StartsAt { get; set; }
        public Position EndsAt { get; set; }

        public string FilePath { get; set; }

        public SourceContext(Position StartsAt, Position EndsAt)
        {
            this.StartsAt = StartsAt;
            this.EndsAt = EndsAt;
            this.FilePath = @"C:\Users\Almes\source\repos\Compiler\compiler v.5\src\libs\main.alm";
        }

        public static SourceContext GetSourceContext(Token Token) => new SourceContext(new Position(Token.Context.StartsAt.CharIndex, Token.Context.StartsAt.LineIndex), new Position(Token.Context.EndsAt.CharIndex, Token.Context.EndsAt.LineIndex));
        public static SourceContext GetSourceContext(Token sToken, Token fToken) => new SourceContext(new Position(sToken.Context.StartsAt.CharIndex, sToken.Context.StartsAt.LineIndex), new Position(fToken.Context.EndsAt.CharIndex, fToken.Context.EndsAt.LineIndex));
        public static SourceContext GetSourceContext(SyntaxTreeNode node) => new SourceContext(node.SourceContext.StartsAt, node.SourceContext.EndsAt);
        public static SourceContext GetSourceContext(SyntaxTreeNode lnode, SyntaxTreeNode rnode) => new SourceContext(lnode.SourceContext.StartsAt, rnode.SourceContext.EndsAt);

        public override string ToString() => $"От {StartsAt} До {EndsAt}";

        public override bool Equals(object obj)
        {
            return obj is SourceContext context &&
                   EqualityComparer<Position>.Default.Equals(StartsAt, context.StartsAt) &&
                   EqualityComparer<Position>.Default.Equals(EndsAt, context.EndsAt) &&
                   FilePath == context.FilePath;
        }

        public override int GetHashCode()
        {
            int hashCode = 955995587;
            hashCode = hashCode * -1521134295 + StartsAt.GetHashCode();
            hashCode = hashCode * -1521134295 + EndsAt.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FilePath);
            return hashCode;
        }

        public static bool operator ==(SourceContext op1, SourceContext op2)
        {
            return op1.Equals(op2);
        }

        public static bool operator !=(SourceContext op1, SourceContext op2)
        {
            return !op1.Equals(op2);
        }
    }
    #endregion
}

namespace alm.Core.FrontEnd.SyntaxAnalysis.new_parser_concept.syntax_tree
{
    public sealed class AbstractSyntaxTree
    {
        public SyntaxTreeNode Root { get; private set; }

        public void BuildTree(string path)
        {
            Diagnostics.Reset();
            Lexer lexer = new Lexer(path);
            Parser2 parser = new Parser2(lexer);
            this.Root = parser.Parse(path);

            if (!Diagnostics.SyntaxAnalysisFailed)
            {
                LabelChecker2.ResolveModule(this.Root);
                if (!Diagnostics.SemanticAnalysisFailed)
                    TypeChecker2.ResolveModuleTypes(this.Root);
            }
        }

        public void ShowTree()
        {
            if (!Diagnostics.SyntaxAnalysisFailed && !Diagnostics.SemanticAnalysisFailed)
                ShowTreeInConsole(Root, "", true);
            else
                Diagnostics.ShowErrorsInConsole();
        }
        private void ShowTreeInConsole(SyntaxTreeNode startNode, string indent = "", bool root = false)
        {
            //├── └── │

            if (root)
            {
                ConsoleCustomizer.ColorizedPrint(indent + "└──", ConsoleColor.DarkGray);
                ConsoleCustomizer.ColorizedPrintln(startNode.ToString(), startNode.ConsoleColor);
            }

            indent += "   ";
            foreach (SyntaxTreeNode node in startNode.Childs)
            {
                if (node != null)
                {
                    ConsoleCustomizer.ColorizedPrint(indent + (node == startNode.Childs.Last()? "└──" : "├──"), ConsoleColor.DarkGray);
                    ConsoleCustomizer.ColorizedPrintln(node.ToString(), node.ConsoleColor);
                    ShowTreeInConsole(node, indent + (node.Childs.Count >= 1 && node == startNode.Childs.Last()? string.Empty : "│"));
                }
            }
        }
    }

}
