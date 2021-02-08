using System.Linq;
using System.Collections.Generic;

using alm.Core.Errors;
using alm.Core.InnerTypes;

using alm.Core.SyntaxTree;

using alm.Other.Enums;
using alm.Other.Structs;

using static alm.Other.Enums.TokenType;
using static alm.Core.Compiler.Compiler.CompilationVariables;

namespace alm.Core.FrontEnd.SyntaxAnalysis
{
    public sealed class Parser
    {
        private Lexer Lexer;
        private string CurrentParsingFile;

        public Parser(Lexer lexer)
        {
            this.Lexer = lexer;
        }

        public SyntaxTreeNode Parse(string path)
        {
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

            return argumens.ToArray();
        }
        public Expression ParseMethodInvokationExpression()
        {
            SourceContext methodContext = new SourceContext();
            methodContext.FilePath = CurrentParsingFile;
            methodContext.StartsAt = Lexer.CurrentToken.Context.StartsAt;
            IdentifierExpression methodName = new IdentifierExpression(Lexer.CurrentToken);
            Lexer.GetNextToken();

            Expression[] methodParameters = ParseParametersDeclarations();
            methodContext.EndsAt = Lexer.PreviousToken.Context.EndsAt;

            if (IsErrored(methodParameters))
                return new ErroredExpression(new SyntaxErrorMessage("Method's parameter expected", methodContext));

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
                        return new ErroredExpression(new SyntaxErrorMessage("Type [void] cannot be used as variable type", Lexer.PreviousToken));
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
                        return new ErroredExpression(new MissingComma(Lexer.CurrentToken));
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
                return new ErroredExpression(new SyntaxErrorMessage("Array's dimension expected", Lexer.CurrentToken));

            return new ArrayInstance(constructionType, sizes.ToArray(),arrayContext);
        }
        public Expression ParseArrayElementExpression()
        {
            SourceContext arrayElementContext = new SourceContext();
            arrayElementContext.StartsAt = new Position(Lexer.CurrentToken);
            arrayElementContext.FilePath = CurrentParsingFile;
            List<Expression> indexes = new List<Expression>();
            IdentifierExpression identifier = (IdentifierExpression)ParseIdentifierExpression(IdentifierExpression.State.Decl);

            if (!Match(tkSqLbra))
                return new ErroredExpression(new SyntaxErrorMessage("Для получения элемента по индексу массива нужен символ \'[\'", Lexer.CurrentToken));
            Lexer.GetNextToken();

            while (!Match(tkSqRbra) && !Match(tkEOF))
            {
                indexes.Add(ParseExpression());

                if (!Match(tkComma))
                {
                    if (!Match(tkSqRbra))
                        return new ErroredExpression(new MissingComma(Lexer.CurrentToken));
                }
                else
                    Lexer.GetNextToken();
            }
            Lexer.GetNextToken();
            arrayElementContext.EndsAt = new Position(Lexer.PreviousToken);
            return new ArrayElementExpression(identifier, indexes.ToArray(),arrayElementContext);
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
                    return new ErroredStatement(new CorrectImportExpected(Lexer.CurrentToken.Context));

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

            return new ImportStatement(CurrentParsingFile,modules.ToArray());
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
            SourceContext funcContext = Lexer.PreviousToken.Context;

            Expression[] args = ParseArgumentDeclarations();
            if (IsErrored(args))
                return new ErroredStatement(new SyntaxErrorMessage("Error occurred when declaring argument", Lexer.CurrentToken));

            if (!Match(tkColon))
                return new ErroredStatement(new MissingColon(Lexer.CurrentToken));

            Lexer.GetNextToken();

            Expression funcType = ParseTypeExpression();
            if (IsErrored(funcType))
                return new ErroredStatement(new TypeExpected(Lexer.CurrentToken));

            Statement funcBody = ParseEmbeddedStatement();
            if (IsErrored(funcBody))
                return new ErroredStatement(new SyntaxErrorMessage("Statement expected",Lexer.CurrentToken));
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
                return new ErroredStatement(new SyntaxErrorMessage("The name of .NET static library expected", Lexer.CurrentToken));

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
                return new ErroredStatement(new MissingColon(Lexer.CurrentToken));
            Lexer.GetNextToken();

            Expression funcType = ParseTypeExpression();
            if (IsErrored(funcType))
                return new ErroredStatement(new TypeExpected(Lexer.CurrentToken));
            funcContext.EndsAt = new Position(Lexer.CurrentToken);

            if (!Match(tkSemicolon))
                return new ErroredStatement(new MissingSemi(Lexer.CurrentToken));
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
                return new ErroredStatement(new SyntaxErrorMessage("Assign[=] or semicolon[;] symbol expected", Lexer.CurrentToken));
        }
        public Statement ParseAssignmentStatement()
        {
            Expression identifier = ParseAdressor(IdentifierExpression.State.Call);

            TokenType[] expectSymbols = new TokenType[] { tkAssign,tkAddAssign,tkSubAssign,tkIDivAssign,tkFDivAssign,tkMultAssign,tkPowerAssign,tkRemndrAssign,tkLShiftAssign,tkRShiftAssign,tkBitwiseOrAssign,tkBitwiseAndAssign,tkBitwiseXorAssign };

            if (!Match(expectSymbols))
                return new ErroredStatement(new SyntaxErrorMessage("Symbol of assignment expected", Lexer.CurrentToken));
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
                    return new ErroredStatement(new SyntaxErrorMessage("Statement expected", Lexer.CurrentToken));
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

            Statement loopInit;
            if (Match(tkType))
                loopInit = ParseDeclarationStatement();
            else
                loopInit = ParseAssignmentStatement();

            Expression loopCondition = ParseBooleanExpression();
            if (!Match(tkSemicolon))
                return new ErroredStatement(new MissingSemi(Lexer.CurrentToken));
            Lexer.GetNextToken();

            Statement loopStep = ParseAssignmentStatement();

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
                    return new ErroredExpression(new SyntaxErrorMessage("Adressable expression expected", Lexer.CurrentToken));
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
                return new ErroredExpression(new SyntaxErrorMessage("Only one unary minus can be added in a row", Lexer.CurrentToken));
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
                case tkType:
                    return ParseArrayInstance();
                default:
                    return new ErroredExpression(new SyntaxErrorMessage("Constant expression expected",Lexer.CurrentToken));
            }
        }
        public Expression ParseIntegralConstant()
        {
            Lexer.GetNextToken();
            try
            {
                long value = System.Math.Abs(System.Convert.ToInt64(Lexer.PreviousToken.Value));
                if (System.Math.Abs(value) <= System.Int32.MaxValue)
                    return new Int32Constant(Lexer.PreviousToken.Value);
                if (System.Math.Abs(value) <= System.Int64.MaxValue)
                    return new Int64Constant(Lexer.PreviousToken.Value);
            }
            catch (System.Exception e)
            {
                return new ErroredExpression(new SyntaxErrorMessage($"The size of maximum integral (64-bit) type is exceeded [{e.Message}]", Lexer.PreviousToken));
            }
            throw new System.Exception();
        }
        public Expression ParseRealConstant()
        {
            if (!Match(tkRealConst))
                return new ErroredExpression(new SyntaxErrorMessage("Real constant expected", Lexer.CurrentToken));
            Lexer.GetNextToken();
            return new SingleConstant(Lexer.PreviousToken);
        }
        public Expression ParseStringConstant()
        {
            if (!Match(tkDQuote))
                return new ErroredExpression(new MissingDQuote(Lexer.CurrentToken));
            Lexer.GetNextToken();

            if (!Match(tkStringConst))
                return new ErroredExpression(new SyntaxErrorMessage("String expected", Lexer.CurrentToken));

            StringConstant stringConst = new StringConstant(Lexer.CurrentToken);
            Lexer.GetNextToken();

            if (!Match(tkDQuote))
                return new ErroredExpression(new MissingDQuote(Lexer.CurrentToken));
            Lexer.GetNextToken();

            return stringConst;
        }
        public Expression ParseCharConstant()
        {
            if (!Match(tkSQuote))
                return new ErroredExpression(new MissingSQuote(Lexer.CurrentToken));
            Lexer.GetNextToken();

            if (!Match(tkCharConst))
                return new ErroredExpression(new SyntaxErrorMessage("Char expected", Lexer.CurrentToken));

            CharConstant charConst = new CharConstant(Lexer.CurrentToken);
            Lexer.GetNextToken();

            if (!Match(tkSQuote))
                return new ErroredExpression(new MissingSQuote(Lexer.CurrentToken));
            Lexer.GetNextToken();

            return charConst;
        }
        public Expression ParseBooleanConstant()
        {
            if (!Match(tkBooleanConst))
                return new ErroredExpression(new SyntaxErrorMessage("Boolean constant expected", Lexer.CurrentToken));
            Lexer.GetNextToken();
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