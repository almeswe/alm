using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using alm.Other.Enums;
using alm.Core.Errors;
using alm.Other.Structs;
using alm.Core.Compiler;
using alm.Other.InnerTypes;

using static alm.Other.Enums.TokenType;
using static alm.Other.String.StringMethods;
using static alm.Other.Structs.SourceContext;

namespace alm.Core.SyntaxAnalysis
{
    internal sealed class Parser
    {
        private Lexer Lexer;
        public Parser(Lexer lexer) => this.Lexer = lexer;

        public string GetImportPath(string scriptname)
        {
            string envdir = Path.GetDirectoryName(CompilingFile.Path);

            if (File.Exists(Path.Combine(envdir,scriptname) + ".alm"))
                return Path.Combine(envdir, scriptname) + ".alm";
            return string.Empty;
        }
        public SyntaxTreeNode Parse()
        {
            Lexer.GetNextToken();
            //Root root = RunParserOnlyOnArithExpression(ParsingFile.Path);
            Root root = RunParser(CompilingFile.Path);
            return root;
        }

        #region For Debug Cases 

        private Root RunParserOnlyOnArithExpression(string path)
        {
            Root root = new Root(path);
            root.AddNode(ParseExpression());
            return root;
        }
        private Root RunParser(string path) => (Root)ParseProgram(path);
        #endregion 

        public SyntaxTreeNode ParseProgram(string Path)
        {
            SyntaxTreeNode Root = new Root(Path);
            if (Match(tkImport))
            {
                while (Match(tkImport))
                {
                    Root.AddNode(((ImportExpression)ParseImportExpression()));
                    if (Root.Nodes.Last().Errored) break;
                }
            }

            while (!Match(tkEOF))
            {
                Root.AddNode(ParseFunctionDeclaration());
                if (Root.Nodes.Last().Errored) break;
            }
            return Root;
        }
        public SyntaxTreeNode ParseImportExpression()
        {
            if (!Match(tkImport)) return new ImportExpression(new ReservedWordExpected("import", Lexer.CurrentToken));
            Lexer.GetNextToken();

            if (Match(tkQuote))
            {
                Lexer.GetNextToken();
                if (!Match(tkString)) return new ImportExpression(new OnlyDebug("Ожидалась строка", Lexer.CurrentToken));
                StringConst stringImport = new StringConst(Lexer.CurrentToken);
                Lexer.GetNextToken();
                if (!Match(tkQuote)) return new ImportExpression(new ReservedSymbolExpected("\"", Lexer.CurrentToken));
                Lexer.GetNextToken();
                if (!Match(tkSemicolon)) return new ImportExpression(new MissingSemi(Lexer.PreviousToken));
                Lexer.GetNextToken();
                if (File.Exists(stringImport.Value))
                {
                    if (Path.GetExtension(stringImport.Value) != ".alm") return new ImportExpression(new WrongImportExtension(stringImport.SourceContext));
                    string startpath = CompilingFile.Path;
                    CompilingFile.Path = stringImport.Value;
                    AbstractSyntaxTree ast = new AbstractSyntaxTree();
                    ast.BuildTree(stringImport.Value);
                    CompilingFile.Path = startpath;
                    return new ImportExpression(stringImport, (Root)ast.Root);
                }
                return new ImportExpression(new WrongImport(stringImport.SourceContext));
            }
            else if (Match(tkId))
            {
                IdentifierExpression idImport = new IdentifierExpression(Lexer.CurrentToken);
                Lexer.GetNextToken();
                if (!Match(tkSemicolon)) return new ImportExpression(new MissingSemi(Lexer.PreviousToken));
                Lexer.GetNextToken();
                if (File.Exists(GetImportPath(idImport.Name)))
                {
                    string buildedapth = GetImportPath(idImport.Name);
                    string startpath = CompilingFile.Path;
                    CompilingFile.Path = buildedapth;

                    AbstractSyntaxTree ast = new AbstractSyntaxTree();
                    ast.BuildTree(buildedapth);
                    CompilingFile.Path = startpath;
                    
                    return new ImportExpression(idImport, (Root)ast.Root);
                }
                return new ImportExpression(new WrongShortImport(idImport.SourceContext));
            }

            else return new ImportExpression(new ExpectedCorrectImport(Lexer.CurrentToken.Context));
        }
        public SyntaxTreeNode ParseFunctionDeclaration()
        {
            SourceContext funccontext = new SourceContext();

            if (!Match(tkFunc)) return new FunctionDeclaration(new ReservedWordExpected("function", Lexer.CurrentToken));
            funccontext.StartsAt = new Position(Lexer.CurrentToken);
            Lexer.GetNextToken();
            if (!Match(tkId)) return new FunctionDeclaration(new IdentifierExpected(Lexer.CurrentToken));
            IdentifierExpression funcname = new IdentifierExpression(Lexer.CurrentToken);
            funccontext = Lexer.CurrentToken.Context;
            Lexer.GetNextToken();
            if (!Match(tkLpar)) return new FunctionDeclaration(new MissingLpar(Lexer.CurrentToken));
            Lexer.GetNextToken();
            TypeExpression argtype;
            IdentifierExpression argname;
            ArgumentDeclaration arg;
            Arguments args = new Arguments();
            SourceContext argscontext = new SourceContext();
            argscontext.FilePath = CompilingFile.Path;
            argscontext.StartsAt = new Position(Lexer.CurrentToken);

            while (!Match(tkRpar))
            {
                if (!Match(tkType))  return new FunctionDeclaration(new TypeExpected(Lexer.CurrentToken));
                argtype = new TypeExpression(Lexer.CurrentToken);
                Lexer.GetNextToken();
                argname = new IdentifierDeclaration(Lexer.CurrentToken);
                argname.Type = argtype.Type;

                Lexer.GetNextToken();
                if (!Match(tkComma))
                {
                    if (Match(tkRpar))
                    {
                        arg = new ArgumentDeclaration(argtype, argname);
                        args.AddNode(arg);
                        continue;
                    }
                    else return new FunctionDeclaration(new ReservedSymbolExpected(",", Lexer.CurrentToken));
                }
                Lexer.GetNextToken();
                arg = new ArgumentDeclaration(argtype, argname);
                args.AddNode(arg);
            }
            argscontext.EndsAt = new Position(Lexer.CurrentToken);
            args.SourceContext = argscontext;
            Lexer.GetNextToken();
            if (!Match(tkOf)) return new FunctionDeclaration(new ReservedWordExpected("of", Lexer.CurrentToken));
            Lexer.GetNextToken();
            if (!Match(tkType)) return new FunctionDeclaration(new TypeExpected(Lexer.CurrentToken));
            TypeExpression functype = new TypeExpression(Lexer.CurrentToken);
            Lexer.GetNextToken();
            if (!Match(tkLbra)) return new FunctionDeclaration(new MissingLbra(Lexer.CurrentToken));
            Lexer.GetNextToken();
            Body funcbody = new Body();
            SourceContext bodycontext = new SourceContext();
            bodycontext.FilePath = CompilingFile.Path;
            bodycontext.StartsAt = new Position(Lexer.CurrentToken);
            while (!Match(tkRbra))
            {
                if (Match(tkEOF)) return new FunctionDeclaration(new MissingRbra(Lexer.CurrentToken));
                funcbody.AddNode(ParseStatement());
                if (funcbody.Nodes.Last().Errored) break;
            }

            //bodycontext.EndsAt = funccontext.EndsAt = new Position(Lexer.currToken);
            bodycontext.EndsAt = new Position(Lexer.CurrentToken);
            funcbody.SourceContext = bodycontext;
            Lexer.GetNextToken();
            return new FunctionDeclaration(funcname, args, functype, funcbody, funccontext);
        }
        public SyntaxTreeNode ParseVariableDeclaration()
        {
            if (!Match(tkType)) return new DeclarationExpression(new TypeExpected(Lexer.CurrentToken));
            TypeExpression idtype = new TypeExpression(Lexer.CurrentToken);
            Lexer.GetNextToken();
            if (!Match(tkId)) return new DeclarationExpression(new IdentifierExpected(Lexer.CurrentToken));

            IdentifierExpression id = new IdentifierDeclaration(Lexer.CurrentToken, idtype.Type);

            Lexer.GetNextToken();
            AssignmentExpression assign;

            if (Match(tkAssign))
            {
                Lexer.GetNextToken();
                if (Match(tkTrue) || Match(tkFalse))
                {
                    assign = new AssignmentExpression(id, new BooleanConst(Lexer.CurrentToken));
                    Lexer.GetNextToken();
                }
                else if (Match(tkQuote))
                {
                    Lexer.GetNextToken();
                    if (!Match(tkString)) return new AssignmentExpression(new OnlyDebug("Ожидалась строка",Lexer.CurrentToken));
                    assign = new AssignmentExpression(id, new StringConst(Lexer.CurrentToken));
                    Lexer.GetNextToken();
                    if (!Match(tkQuote)) return new AssignmentExpression(new ReservedSymbolExpected("\"", Lexer.PreviousToken));
                    Lexer.GetNextToken();
                }
                else
                {
                    assign = new AssignmentExpression(id, ParseExpression());
                }
                if (!Match(tkSemicolon)) return new DeclarationExpression(new MissingSemi(Lexer.PreviousToken));
                Lexer.GetNextToken();
                return new DeclarationExpression(idtype, assign);
            }
            else if (Match(tkSemicolon))
            {
                Lexer.GetNextToken();
                return new DeclarationExpression(idtype, id);
            }
            else return new DeclarationExpression(new OnlyDebug("Ожидался символ [=] или [;]", Lexer.CurrentToken));
        }
        public SyntaxTreeNode ParseAssignmentExpression()
        {
            if (!Match(tkId)) return new AssignmentExpression(new IdentifierExpected(Lexer.CurrentToken));
            IdentifierExpression id = new IdentifierCall(Lexer.CurrentToken);
            Lexer.GetNextToken();
            if (!Match(tkAssign)) return new AssignmentExpression(new ReservedSymbolExpected("=", Lexer.CurrentToken));
            Lexer.GetNextToken();
            AssignmentExpression assign;

            if (Match(tkTrue) || Match(tkFalse))
            {
                assign = new AssignmentExpression(id, new BooleanConst(Lexer.CurrentToken));
                Lexer.GetNextToken();
            }
            else if (Match(tkQuote))
            {
                Lexer.GetNextToken();
                if (!Match(tkString)) return new AssignmentExpression(new OnlyDebug("Ожидалась строка", Lexer.CurrentToken));
                assign = new AssignmentExpression(id, new StringConst(Lexer.CurrentToken));
                Lexer.GetNextToken();
                if (!Match(tkQuote)) return new AssignmentExpression(new ReservedSymbolExpected("\"", Lexer.CurrentToken));
                Lexer.GetNextToken();
            }
            else
            {
                assign = new AssignmentExpression(id, ParseExpression());
            }

            if (!Match(tkSemicolon)) return new AssignmentExpression(new MissingSemi(Lexer.PreviousToken));
            Lexer.GetNextToken();
            return assign;
        }
        public SyntaxTreeNode ParseStatement()
        {
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkId: return ParseIdentifierExpression();
                case tkIf: return ParseIfStatement();
                case tkWhile: return ParseWhileStatement();
                case tkDo: return ParseDoWhileStatement();
                case tkType: return ParseVariableDeclaration();
                case tkRet: return ParseReturnExpression();

                default: return new ReturnExpression(new OnlyDebug("Ожидалось выражение", Lexer.CurrentToken));
            }
        }
        public SyntaxTreeNode ParseIdentifierExpression()
        {
            if (!Match(tkId)) return new FunctionCall(new IdentifierExpected(Lexer.CurrentToken));
            if (Match(tkLpar,1)) return ParseFunctionCall();
            else return ParseAssignmentExpression();
        }
        public SyntaxTreeNode ParseFunctionCall(bool ParseAsSingleExpression = true)
        {
            SourceContext funccontext = new SourceContext();
            funccontext.FilePath = CompilingFile.Path;
            funccontext.StartsAt = Lexer.CurrentToken.Context.StartsAt;
            IdentifierCall funcname = new IdentifierCall(Lexer.CurrentToken);
            Arguments args = new Arguments();
            Lexer.GetNextToken();
            if (!Match(tkLpar)) return new FunctionCall(new MissingLpar(Lexer.PreviousToken));
            Lexer.GetNextToken();
            while (!Match(tkRpar))
            {
                if (Match(tkTrue) || Match(tkFalse))
                {
                    args.AddNode(new BooleanConst(Lexer.CurrentToken));
                    Lexer.GetNextToken();
                }

                else if (Match(tkQuote))
                {
                    Lexer.GetNextToken();
                    if (!Match(tkString)) return new FunctionCall(new OnlyDebug("Ожидалась строка",Lexer.CurrentToken));
                    args.Nodes.Add(new StringConst(Lexer.CurrentToken));
                    Lexer.GetNextToken();
                    if (!Match(tkQuote)) return new FunctionCall(new ReservedSymbolExpected("\"", Lexer.CurrentToken));
                    Lexer.GetNextToken();
                }

                else
                    args.AddNode(ParseExpression());

                //Lexer.GetNextToken();

                if (!Match(tkComma))
                {
                    if (Match(tkRpar)) continue;
                    else return new FunctionCall(new ReservedSymbolExpected(",", Lexer.CurrentToken));
                }
                Lexer.GetNextToken();
            }
            if (ParseAsSingleExpression)
            {
                funccontext.EndsAt = Lexer.CurrentToken.Context.EndsAt;
                Lexer.GetNextToken();
                if (!Match(tkSemicolon)) return new FunctionCall(new MissingSemi(Lexer.PreviousToken));
                Lexer.GetNextToken();
            }
            else funccontext.EndsAt = Lexer.CurrentToken.Context.EndsAt;
            return new FunctionCall(funcname.Name,args,funccontext);
        }
        public SyntaxTreeNode ParseReturnExpression()
        {
            SourceContext retcontext = new SourceContext();
            retcontext.FilePath = CompilingFile.Path;
            if (!Match(tkRet)) return new ReturnExpression(new ReservedWordExpected("return", Lexer.CurrentToken));
            retcontext.StartsAt = new Position(Lexer.CurrentToken);
            Lexer.GetNextToken();
            SyntaxTreeNode Expression;
            if (Match(tkSemicolon))
            {
                retcontext.EndsAt = new Position(Lexer.CurrentToken);
                Lexer.GetNextToken();
                return new ReturnExpression(retcontext);
            }
            if (Match(tkTrue) || Match(tkFalse))
            {
                Expression = new BooleanConst(Lexer.CurrentToken);
                Lexer.GetNextToken();
            }
            else if (Match(tkQuote))
            {
                Lexer.GetNextToken();
                if (!Match(tkString)) return new ReturnExpression(new OnlyDebug("Ожидалась строка", Lexer.CurrentToken));
                Expression = new StringConst(Lexer.CurrentToken);
                Lexer.GetNextToken();
                if (!Match(tkQuote)) return new AssignmentExpression(new ReservedSymbolExpected("\"", Lexer.CurrentToken));
                Lexer.GetNextToken();
            }
            else
            {
                Expression = ParseExpression();
            }
            retcontext.EndsAt = new Position(Lexer.CurrentToken);
            if (!Match(tkSemicolon)) return new ReturnExpression(new MissingSemi(Lexer.PreviousToken));
            Lexer.GetNextToken();
            return new ReturnExpression(Expression, retcontext);
        }
        public SyntaxTreeNode ParseIfStatement()
        {
            if (!Match(tkIf)) return new IfStatement(new ReservedWordExpected("if", Lexer.CurrentToken));

            SourceContext ifcontext = new SourceContext();
            ifcontext.FilePath = CompilingFile.Path;

            ifcontext.StartsAt = new Position(Lexer.CurrentToken);

            Lexer.GetNextToken();

            SourceContext conditioncontext = new SourceContext();
            conditioncontext.FilePath = CompilingFile.Path;
            conditioncontext.StartsAt = new Position(Lexer.CurrentToken);
            Condition ifcondition = new Condition(ParseBooleanParentisizedExpression());
            conditioncontext.EndsAt = new Position(Lexer.CurrentToken);
            ifcondition.SourceContext = conditioncontext;

            if (!Match(tkLbra)) return new IfStatement(new MissingLbra(Lexer.CurrentToken));
            Lexer.GetNextToken();
            Body ifbody = new Body();
            SourceContext bodycontext = new SourceContext();
            bodycontext.FilePath = CompilingFile.Path;
            bodycontext.StartsAt = new Position(Lexer.CurrentToken);
            while (!Match(tkRbra))
            {
                if (Match(tkEOF)) return new IfStatement(new MissingRbra(Lexer.CurrentToken));
                ifbody.AddNode(ParseStatement());
                if (ifbody.Nodes.Last().Errored) break;
            }
            bodycontext.EndsAt = ifcontext.EndsAt = new Position(Lexer.CurrentToken);
            ifbody.SourceContext = bodycontext;
            IfStatement ifnode = new IfStatement(ifcondition, ifbody, ifcontext);
            Lexer.GetNextToken();

            if (Match(tkElse))
            {
                Lexer.GetNextToken();
                if (!Match(tkLbra)) return new IfStatement(new MissingLbra(Lexer.CurrentToken));
                Lexer.GetNextToken();
                ElseBody elsebody = new ElseBody();
                SourceContext elsecontext = new SourceContext();
                elsecontext.FilePath = CompilingFile.Path;
                elsecontext.StartsAt = new Position(Lexer.CurrentToken);
                while (!Match(tkRbra))
                {
                    if (Match(tkEOF)) return new IfStatement(new MissingRbra(Lexer.CurrentToken));
                    elsebody.AddNode(ParseStatement());
                    if (elsebody.Nodes.Last().Errored) break;
                }
                elsecontext.EndsAt = ifcontext.EndsAt = new Position(Lexer.CurrentToken);
                elsebody.SourceContext = elsecontext;
                ifnode = new IfStatement(ifnode, elsebody, ifcontext);
                Lexer.GetNextToken();

            }
            return ifnode;
        }
        public SyntaxTreeNode ParseWhileStatement()
        {
            if (!Match(tkWhile)) return new WhileStatement(new ReservedWordExpected("while", Lexer.CurrentToken));
            Lexer.GetNextToken();
            Condition condition = new Condition(ParseBooleanParentisizedExpression());
            if (!Match(tkLbra)) return new WhileStatement(new MissingLbra(Lexer.PreviousToken));
            Lexer.GetNextToken();
            Body body = new Body();
            while (!Match(tkRbra))
            {
                if (Match(tkEOF)) return new WhileStatement(new MissingRbra(Lexer.PreviousToken));
                body.AddNode(ParseStatement());
                if (body.Nodes.Last().Errored) break;
            }
            Lexer.GetNextToken();

            return new WhileStatement(condition, body);
        }
        public SyntaxTreeNode ParseDoWhileStatement()
        {
            if (!Match(tkDo)) return new DoWhileStatement(new ReservedWordExpected("do", Lexer.CurrentToken));

            Lexer.GetNextToken();
            if (!Match(tkLbra)) return new DoWhileStatement(new MissingLbra(Lexer.PreviousToken));
            Lexer.GetNextToken();
            Body body = new Body();
            while (!Match(tkRbra))
            {
                if (Match(tkEOF)) return new DoWhileStatement(new MissingRbra(Lexer.PreviousToken));
                body.AddNode(ParseStatement());
                if (body.Nodes.Last().Errored) break;
            }
            Lexer.GetNextToken();
            if (!Match(tkWhile)) return new DoWhileStatement(new ReservedWordExpected("while", Lexer.CurrentToken));
            Lexer.GetNextToken();
            Condition condition = new Condition(ParseBooleanParentisizedExpression());
            if (!Match(tkSemicolon)) return new DoWhileStatement(new MissingSemi(Lexer.PreviousToken));
            Lexer.GetNextToken();

            return new DoWhileStatement(body, condition);
        }
        public SyntaxTreeNode ParseBooleanExpression()
        {
            SyntaxTreeNode node = ParseBooleanTerm();
            if (Match(tkOr))
            {
                Lexer.GetNextToken();
                node = new BooleanExpression(node, Operator.LogicalOR, ParseBooleanExpression());
            }
            return node;
        }
        public SyntaxTreeNode ParseBooleanTerm()
        {
            SyntaxTreeNode node = ParseBooleanNotFactor();

            if (Match(tkAnd))
            {
                Lexer.GetNextToken();
                node = new BooleanExpression(node, Operator.LogicalAND, ParseBooleanNotFactor());
            }
            return node;
        }
        public SyntaxTreeNode ParseBooleanNotFactor()
        {
            SyntaxTreeNode node;
            if (Match(tkNot))
            {
                Lexer.GetNextToken();
                node = new BooleanExpression(Operator.LogicalNOT, ParseBooleanTerm());
            }
            else
            {
                node = ParseBooleanFactor();
            }

            return node;
        }
        public SyntaxTreeNode ParseBooleanFactor()
        {
            SyntaxTreeNode node;
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkId:
                    node = ParseExpression();

                    switch (Lexer.CurrentToken.TokenType)
                    {
                        case tkLess: Lexer.GetNextToken(); node = new BooleanExpression(node, Operator.Less, ParseExpression()); break;
                        case tkMore: Lexer.GetNextToken(); node = new BooleanExpression(node, Operator.Greater, ParseExpression()); break;
                        case tkEqual: Lexer.GetNextToken(); node = new BooleanExpression(node, Operator.Equal, ParseExpression()); break;
                        case tkNotEqual: Lexer.GetNextToken(); node = new BooleanExpression(node, Operator.NotEqual, ParseExpression()); break;
                    }
                    return node;

                case tkNum:
                    node = ParseExpression();

                    switch (Lexer.CurrentToken.TokenType)
                    {
                        case tkLess: Lexer.GetNextToken();     node = new BooleanExpression(node, Operator.Less, ParseExpression()); break;
                        case tkMore: Lexer.GetNextToken();     node = new BooleanExpression(node, Operator.Greater, ParseExpression()); break;
                        case tkEqual: Lexer.GetNextToken();    node = new BooleanExpression(node, Operator.Equal, ParseExpression()); break;
                        case tkNotEqual: Lexer.GetNextToken(); node = new BooleanExpression(node, Operator.NotEqual, ParseExpression()); break;
                    }
                    return node;

                case tkLpar: return ParseBooleanParentisizedExpression();

                case tkTrue:
                case tkFalse:
                    Lexer.GetNextToken();
                    node = new BooleanConst(Lexer.PreviousToken);
                    Token start = Lexer.CurrentToken;
                    switch (start.TokenType)
                    {
                        case tkEqual:
                            Lexer.GetNextToken();
                            node = new BooleanExpression(node, Operator.Equal, ParseExpression());
                            return node;
                        case tkNotEqual:
                            Lexer.GetNextToken();
                            node = new BooleanExpression(node, Operator.NotEqual, ParseExpression());
                            return node;
                        default: return node;
                    }

                default: return new BooleanConst(new ReservedSymbolExpected("< или >,переменная,выражение в скобках", Lexer.CurrentToken));
            }
        }
        public SyntaxTreeNode ParseBooleanRelation()
        {
            SyntaxTreeNode node = ParseExpression();
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkLess: Lexer.GetNextToken(); return new BooleanExpression(node, Operator.Less, ParseBooleanRelation());
                case tkMore: Lexer.GetNextToken(); return new BooleanExpression(node, Operator.Greater, ParseBooleanRelation());
            }
            return node;
        }
        public SyntaxTreeNode ParseBooleanParentisizedExpression()
        {
            if (!Match(tkLpar)) return new BooleanExpression(new MissingLpar(Lexer.CurrentToken));
            Lexer.GetNextToken();
            SyntaxTreeNode node = ParseBooleanExpression();
            if (!Match(tkRpar)) return new BooleanExpression(new MissingRpar(Lexer.CurrentToken));
            Lexer.GetNextToken();
            return node;
        }
        public SyntaxTreeNode ParseExpression()
        {
            SyntaxTreeNode node = ParseTerm();
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkMinus: Lexer.GetNextToken(); node = new BinaryExpression(node, Operator.Minus, ParseExpression()); break;
                case tkPlus: Lexer.GetNextToken();  node = new BinaryExpression(node, Operator.Plus, ParseExpression()); break;
            }
            return node;
        }
        public SyntaxTreeNode ParseTerm()
        {
            SyntaxTreeNode node = ParseSignedFactor();
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkMult: Lexer.GetNextToken(); node = new BinaryExpression(node, Operator.Multiplication, ParseTerm()); break;
                case tkDiv: Lexer.GetNextToken(); node = new BinaryExpression(node, Operator.Division, ParseTerm()); break;
            }
            return node;
        }
        public SyntaxTreeNode ParseSignedFactor()
        {
            SyntaxTreeNode node;
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkId:
                    if(Match(tkLpar,1))
                    {
                        node = ParseFunctionCall(false);
                        Lexer.GetNextToken();
                    }
                    else
                    {
                        node = new IdentifierCall(Lexer.CurrentToken);
                        Lexer.GetNextToken();
                    }
                    return node;

                case tkNum:
                    node = new IntegerConst(Lexer.CurrentToken);
                    Lexer.GetNextToken();
                    return node;

                case tkLpar: return ParseParentisizedExpression();

                default: return new BinaryExpression(new OnlyDebug("Ожидалось число,переменная,или выражение в скобках", Lexer.CurrentToken));
            }
        }
        public SyntaxTreeNode ParseParentisizedExpression()
        {
            if (!Match(tkLpar)) return new BinaryExpression(new MissingLpar(Lexer.CurrentToken));
            Lexer.GetNextToken();
            SyntaxTreeNode node = ParseExpression();
            if (!Match(tkRpar)) return new BinaryExpression(new MissingRpar(Lexer.CurrentToken));
            Lexer.GetNextToken();
            return node;
        }

        public bool Match(TokenType expectedType, int offset = 0)
        {
            if (Lexer.Peek(offset).TokenType == expectedType) return true;
            return false;
        }
        public bool Match(SyntaxTreeNode node, NodeType expectedType)
        {
            if (node.NodeType == expectedType) return true;
            return false;
        }
        public bool Match(Expression expression, NodeType expectedType)
        {
            if (expression.NodeType == expectedType) return true;
            return false;
        }
        public Operator GetOperatorFromTokenType(TokenType type)
        {
            switch (type)
            {
                case tkPlus: return Operator.Plus;
                case tkMinus:return Operator.Minus;
                case tkMult: return Operator.Multiplication;
                case tkDiv:  return Operator.Division;
                default: return Operator.As;
            }
        }
    }
    public abstract class SyntaxTreeNode
    {
        public SyntaxTreeNode Parent { get; set; }
        public abstract NodeType NodeType { get; }

        public bool Errored { get; protected set; } = false;

        public virtual ConsoleColor Color => ConsoleColor.White;
        public List<SyntaxTreeNode> Nodes { get; private set; } = new List<SyntaxTreeNode>();

        public SourceContext SourceContext = new SourceContext();

        public void SetSourceContext(Token token)                                => this.SourceContext = GetSourceContext(token, CompilingFile.Path);
        public void SetSourceContext(Token sToken, Token fToken)                 => this.SourceContext = GetSourceContext(sToken,fToken, CompilingFile.Path);
        public void SetSourceContext(SyntaxTreeNode node)                        => this.SourceContext = GetSourceContext(node, CompilingFile.Path);
        public void SetSourceContext(SyntaxTreeNode lnode, SyntaxTreeNode rnode) => this.SourceContext = GetSourceContext(lnode,rnode, CompilingFile.Path);

        public virtual string ToConsoleString() => $"{NodeType}";
        //public virtual string ToConsoleString() => $"{NodeType} {this.SourceContext}";

        public void AddNode(SyntaxTreeNode node)
        {
            node.Parent = this;
            this.Nodes.Add(node);
        }
        public void AddNodes(params SyntaxTreeNode[] nodes)
        {
            foreach (SyntaxTreeNode node in nodes) AddNode(node);
        }

        public SyntaxTreeNode GetParentByType(string typeString)
        {
            for (SyntaxTreeNode Parent = this.Parent; Parent != null; Parent = Parent.Parent)
                if (LastAfterDot(Parent.GetType().ToString()) == typeString)
                    return Parent;
            return null;
        }

        public SyntaxTreeNode[] GetChildsByType(string typeString, bool recursive = false, bool once = true)
        {
            List<SyntaxTreeNode> Childs = new List<SyntaxTreeNode>();
            if (once) if (LastAfterDot(this.GetType().ToString()) == typeString) Childs.Add(this);
            for (int i = 0;i < this.Nodes.Count; i++)
            {
                if (LastAfterDot(this.Nodes[i].GetType().ToString()) == typeString) Childs.Add(this.Nodes[i]);
                if (recursive) Childs.AddRange(this.Nodes[i].GetChildsByType(typeString, true, false));
            }
            return Childs.ToArray();
        }
    }

    public class Root : SyntaxTreeNode
    {
        private string Filename;
        public SyntaxTreeNode Body { get; set; }

        public override NodeType NodeType => NodeType.Program;

        public Root(string filePath)
        {
            Filename = Path.GetFileName(filePath);
        }
        public override string ToConsoleString() => $"[{Filename}]";
    }
    public class Body : SyntaxTreeNode
    {
        public override NodeType NodeType  => NodeType.Body;
        public override ConsoleColor Color => this.Parent.Color;

        public Body(SyntaxTreeNode body) => this.AddNode(body);
        public Body() { }
    }
    public class Condition : SyntaxTreeNode
    {
        public override NodeType NodeType  => NodeType.Condition;
        public override ConsoleColor Color => this.Parent.Color;
        public Condition(SyntaxTreeNode condition) => this.AddNode(condition);
    }
    public class Arguments : SyntaxTreeNode
    {
        public override NodeType NodeType  => NodeType.Arguments;
        public override ConsoleColor Color => this.Parent.Color;
        public Arguments(SyntaxTreeNode arguments) => this.AddNode(arguments);
        public Arguments() { }
    }
    public class ElseBody : Body
    {
        public override NodeType NodeType => NodeType.ElseBody;
    }

    public sealed class FunctionDeclaration : SyntaxTreeNode
    {
        public string Name { get; private set; }
        public Body Body { get; private set; }
        public int ArgumentCount { get; private set; }
        public Arguments Arguments { get; private set; }
        public InnerType Type { get; private set; }

        public override NodeType NodeType => NodeType.Function;
        public override ConsoleColor Color => ConsoleColor.DarkYellow;

        public FunctionDeclaration(IdentifierExpression id, Arguments arguments, TypeExpression type, Body body, SourceContext context)
        {
            this.Body = body;
            this.Name = id.Name;
            this.Type = type.Type;
            this.Arguments = arguments;
            this.SourceContext = context;
            this.ArgumentCount = arguments.Nodes.Count;

            this.AddNodes(arguments, body);
        }

        public FunctionDeclaration(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }

        public override string ToConsoleString() => $"{Name}:{Type.Representation}:[func decl]";
    }
    public sealed class FunctionCall : Expression , ITypeable
    {
        public override NodeType NodeType  => NodeType.FunctionCall;
        public override ConsoleColor Color => ConsoleColor.DarkYellow;

        public string Name         { get; private set; }
        public int ArgumentCount   { get; private set; }
        public InnerType Type      { get; set; }
        public Arguments Arguments { get; private set; }

        public FunctionCall(string name, Arguments arguments,SourceContext context)
        {
            this.Name = name;
            this.Arguments = arguments;
            this.SourceContext = context;
            this.ArgumentCount = arguments.Nodes.Count;

            this.AddNode(arguments);
        }

        public FunctionCall(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }

        public override string ToConsoleString() => $"{Name}:{Type}:[func call]";
    }
    public sealed class ImportExpression : Expression
    {
        public string ImportPath { get; private set; }
        public Root ImportedRoot { get; private set; }

        public override NodeType NodeType => NodeType.Import;

        public ImportExpression(StringConst import,Root importedRoot)
        {
            SetSourceContext(import);

            this.Right = import;
            this.ImportPath = import.Value;
            this.ImportedRoot = importedRoot;
            this.AddNode(importedRoot);
        }
        public ImportExpression(IdentifierExpression import, Root importedRoot)
        {
            SetSourceContext(import);

            this.Right = import;
            this.ImportPath = import.Name;
            this.ImportedRoot = importedRoot;
            this.AddNode(importedRoot);
        }

        public ImportExpression(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }
    public sealed class ArgumentDeclaration : Expression
    {
        public string Name;
        public InnerType Type;
        public override NodeType NodeType => NodeType.Argument;

        public ArgumentDeclaration(TypeExpression type, IdentifierExpression id)
        {
            this.SetSourceContext(id, type);

            this.Name = id.Name;
            this.Type = type.Type;
            this.Left = type;
            this.Right = id;

            this.AddNodes(type, id);
        }

        public ArgumentDeclaration(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }

    public abstract class Statement : SyntaxTreeNode
    {
        public Body Body           { get; protected set; }
        public ElseBody ElseBody   { get; protected set; }
        public Condition Condition { get; protected set; }
        public override ConsoleColor Color => ConsoleColor.Blue;
    }
    public sealed class IfStatement : Statement
    {
        private NodeType _type;

        public override NodeType NodeType => _type;

        public IfStatement(Condition condition, Body body, SourceContext context)
        {
            _type = NodeType.If;
            this.SourceContext = context;
            this.Condition = condition;
            this.Body = body;
            this.AddNodes(condition, body);
        }
        public IfStatement(IfStatement ifStatement, ElseBody elseBody, SourceContext context)
        {
            _type = NodeType.IfElse;
            this.SourceContext = context;
            this.Body = ifStatement.Body;
            this.Condition = ifStatement.Condition;
            this.ElseBody = elseBody;
            this.AddNodes(ifStatement.Condition, ifStatement.Body, elseBody);
        }

        public IfStatement(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }
    public sealed class WhileStatement : Statement
    {
        public override NodeType NodeType => NodeType.While;
        //public override ConsoleColor Color => ConsoleColor.Blue;

        public WhileStatement(Condition condition, Body body)
        {
            this.Condition = condition;
            this.Body = body;
            this.AddNodes(condition, body);
        }

        public WhileStatement(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }
    public sealed class DoWhileStatement : Statement
    {
        public override NodeType NodeType => NodeType.Do;

        public DoWhileStatement(Body body, Condition condition)
        {
            this.Body = body;
            this.Condition = condition;
            this.AddNodes(body, condition);
        }

        public DoWhileStatement(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }

    public abstract class Expression : SyntaxTreeNode
    {
        public SyntaxTreeNode Left { get; protected set; }
        public Operator Op { get; protected set; }
        public SyntaxTreeNode Right { get; protected set; }
        public override ConsoleColor Color => ConsoleColor.DarkCyan;
    }
    public class IdentifierExpression : Expression, ITypeable
    {
        public string Name { get; private set; }

        public InnerType Type { get; set; }
        public override NodeType NodeType  => NodeType.Variable;
        public override ConsoleColor Color => ConsoleColor.Magenta;

        public IdentifierExpression(Token token)
        {
            this.Name = token.Value;
            SetSourceContext(token);
            this.Type = new Underfined();
        }
        public IdentifierExpression(Token token, InnerType type)
        {
            this.Name = token.Value;
            SetSourceContext(token);
            this.Type = type;
        }

        public IdentifierExpression(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }

        public override string ToConsoleString() => $"{Name}:{Type}";
    }
    public sealed class IdentifierDeclaration : IdentifierExpression
    {
        public IdentifierDeclaration(Token token)                 : base(token) { }
        public IdentifierDeclaration(Token token, InnerType type) : base(token, type) { }

        public IdentifierDeclaration(SyntaxError error) : base(error) { }

        public override string ToConsoleString() => base.ToConsoleString() + ":[decl]";
    }
    public sealed class IdentifierCall : IdentifierExpression
    {
        public IdentifierCall(Token token)                 : base(token) { }
        public IdentifierCall(Token token, InnerType type) : base(token, type) { }

        public IdentifierCall(SyntaxError error) : base(error) { }

        public override string ToConsoleString() => base.ToConsoleString() + ":[call]";
    }
    public abstract class ConstExpression : Expression, ITypeable
    {
        public string Value { get; protected set; }
        public abstract InnerType Type { get; }

        public override string ToConsoleString() => $"{Value}:{Type}";
    }
    public sealed class IntegerConst : ConstExpression
    {
        public override NodeType NodeType  => NodeType.IntegerConstant;
        public override ConsoleColor Color => ConsoleColor.DarkMagenta;

        public override InnerType Type => new Integer32();

        public IntegerConst(Token token)
        {
            this.SetSourceContext(token);
            this.Value = token.Value;
        }

        public IntegerConst(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }
    public sealed class BooleanConst : ConstExpression
    {
        private NodeType _type;

        public override NodeType NodeType  => _type;
        public override ConsoleColor Color => ConsoleColor.Green;

        public override InnerType Type => new Other.InnerTypes.Boolean();

        public BooleanConst(Token token)
        {
            if      (token.Value == "true")  _type = NodeType.True;
            else if (token.Value == "false") _type = NodeType.False;

            else throw new Exception("Либо true,либо false:" + token.Value);

            this.SetSourceContext(token);
            this.Value = token.Value;
        }

        public BooleanConst(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }
    public sealed class StringConst : ConstExpression
    {
        public override NodeType NodeType  => NodeType.StringConstant;
        public override ConsoleColor Color => ConsoleColor.Yellow;

        public override InnerType Type => new Other.InnerTypes.String();

        public StringConst(Token token)
        {
            this.SetSourceContext(token);
            this.Value = token.Value;
        }

        public StringConst(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }

        public override string ToConsoleString() => $"\"{Value}\":{Type}";
    }
    public sealed class TypeExpression : Expression
    {
        public InnerType Type { get; set; }
        public override NodeType NodeType => NodeType.Type;
        public override ConsoleColor Color => ConsoleColor.Red;

        public TypeExpression(Token token)
        {
            this.SetSourceContext(token);
            Type = InnerType.GetFromString(token.Value);
        }

        public TypeExpression(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }

        public override string ToConsoleString() => $"{Type}";
    }
    public sealed class BinaryExpression : Expression
    {
        private NodeType _type;
        private ConsoleColor _color;

        public override NodeType NodeType  => _type;
        public override ConsoleColor Color => _color;

        public BinaryExpression(SyntaxTreeNode left, Operator op, SyntaxTreeNode right)
        {
            switch (op)
            {
                case Operator.Plus:           _type = NodeType.Addition;       _color = ConsoleColor.DarkYellow; break;
                case Operator.Minus:          _type = NodeType.Substraction;   _color = ConsoleColor.DarkYellow; break;
                case Operator.Multiplication: _type = NodeType.Multiplication; _color = ConsoleColor.Yellow;     break;
                case Operator.Division:       _type = NodeType.Division;       _color = ConsoleColor.Yellow;     break;
                default: throw new Exception("Встречен неизвестный оператор: " + op.ToString());
            }
            this.SetSourceContext(left, right);
            this.Left = left;
            this.Op = op;
            this.Right = right;
            this.AddNodes(left, right);
        }

        public BinaryExpression(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }

    }
    public sealed class BooleanExpression : Expression
    {
        private NodeType _type;
        private ConsoleColor _color;

        public override NodeType NodeType  => _type;
        public override ConsoleColor Color => _color;

        public BooleanExpression(SyntaxTreeNode left, Operator op, SyntaxTreeNode right)
        {
            // Opertator -> NodeType
            switch (op)
            {
                case Operator.LogicalAND: _type = NodeType.And; _color = ConsoleColor.DarkGreen; break;
                case Operator.LogicalOR:  _type = NodeType.Or;  _color = ConsoleColor.DarkGreen; break;
                case Operator.LogicalNOT: _type = NodeType.Not; _color = ConsoleColor.DarkGreen; break;
                case Operator.Less:       _type = NodeType.LessThan;  _color = ConsoleColor.Green; break;
                case Operator.LessEqual:  _type = NodeType.EqualLess; _color = ConsoleColor.Green; break;
                case Operator.Greater:    _type = NodeType.MoreThan;  _color = ConsoleColor.Green; break;
                case Operator.GreaterEqual: _type = NodeType.EqualMore; _color = ConsoleColor.Green; break;
                case Operator.Equal:        _type = NodeType.Equal;     _color = ConsoleColor.Green; break;
                case Operator.NotEqual:     _type = NodeType.NotEqual;  _color = ConsoleColor.Green; break;

                default: throw new Exception("Встречен неизвестный оператор: " + op.ToString());
            }
            this.SetSourceContext(left, right);
            this.Left = left;
            this.Op = op;
            this.Right = right;
            this.AddNodes(left, right);
        }

        public BooleanExpression(Operator notOp, SyntaxTreeNode right)
        {
            //if (NotOp != OperatorLogicalNOT) throw new Exception("Конструктор предназначен только для not expression.");
            _type = NodeType.Not;
            _color = ConsoleColor.DarkGreen;
            this.SetSourceContext(right);
            this.Op = notOp;
            this.Right = right;
            this.AddNodes(right);
        }

        public BooleanExpression(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }
    public sealed class AssignmentExpression : Expression
    {
        public override NodeType NodeType => NodeType.Assignment;

        public AssignmentExpression(SyntaxTreeNode left, SyntaxTreeNode right)
        {
            this.SetSourceContext(left, right);

            this.Left = left;
            this.Op = Operator.Assignment;
            this.Right = right;
            this.AddNodes(left, right);

        }

        public AssignmentExpression(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }
    public sealed class DeclarationExpression : Expression
    {
        public override NodeType NodeType => NodeType.Declaration;

        public DeclarationExpression(TypeExpression type, IdentifierExpression id)
        {
            this.SetSourceContext(type, id);
            this.Left = type;
            this.Right = id;
            this.AddNodes(Left, Right);
        }
        public DeclarationExpression(TypeExpression type, AssignmentExpression assign)
        {
            this.SetSourceContext(type, assign);
            this.Left = type;
            this.Right = assign;
            this.AddNodes(Left, Right);
        }

        public DeclarationExpression(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }

    public sealed class ReturnExpression : Expression
    {
        public override NodeType NodeType => NodeType.Return;
        public override ConsoleColor Color => ConsoleColor.Red;

        public ReturnExpression(SyntaxTreeNode expression, SourceContext context)
        {
            this.SourceContext = context;
            this.Right = expression;
            this.AddNodes(Right);
        }

        public ReturnExpression(SourceContext context) => this.SourceContext = context;

        public ReturnExpression(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }

    public interface ITypeable
    {
        InnerType Type { get; }
    }
}