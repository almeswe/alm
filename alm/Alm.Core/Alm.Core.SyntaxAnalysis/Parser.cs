using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using alm.Other.Enums;
using alm.Core.Errors;
using alm.Other.Structs;
using alm.Other.InnerTypes;

using static alm.Other.Enums.TokenType;
using static alm.Core.Compiler.Compiler;
using static alm.Other.String.StringMethods;
using static alm.Other.Structs.SourceContext;

namespace alm.Core.SyntaxAnalysis
{
    internal sealed class Parser
    {
        private Lexer Lexer;
        private static List<string> Imports = new List<string>();

        public Parser(Lexer lexer) => this.Lexer = lexer;

        public string GetLongImportPath(string shortPath)
        {
            string envdir = Path.GetDirectoryName(CurrentParsingFile);

            if (File.Exists(Path.Combine(envdir,shortPath) + ".alm"))
                return Path.Combine(envdir, shortPath) + ".alm";
            return string.Empty;
        }
        public ImportExpression Import(IdentifierExpression identifierExpression)
        {
            string buildedPath = GetLongImportPath(identifierExpression.Name);
            if (File.Exists(buildedPath))
            {
                if (buildedPath == CurrentParsingFile)  
                    return new ImportExpression(new CannotImportThisFile(buildedPath,identifierExpression.SourceContext));

                if (Imports.Contains(buildedPath + CurrentParsingFile) || Imports.Contains(CurrentParsingFile + buildedPath))
                    return new ImportExpression(new ThisFileAlreadyImported(buildedPath, identifierExpression.SourceContext));

                else if (Imports.Contains(buildedPath)) 
                    return new ImportExpression();

                else
                {
                    Imports.Add(CurrentParsingFile + buildedPath);
                    Imports.Add(buildedPath + CurrentParsingFile);
                    Imports.Add(buildedPath);
                }
                CurrentParsingFile = buildedPath;

                AbstractSyntaxTree ast = new AbstractSyntaxTree();
                ast.BuildTree(buildedPath);

                CurrentParsingFile = CompilingSourceFile;

                return new ImportExpression(identifierExpression, (Root)ast.Root);
            }
            return new ImportExpression(new WrongShortImport(identifierExpression.SourceContext));
        }
        public ImportExpression Import(StringConst stringConst)
        {
            if (File.Exists(stringConst.Value))
            {
                if (Path.GetExtension(stringConst.Value) != ".alm") 
                    return new ImportExpression(new WrongImportExtension(stringConst.SourceContext));

                if (stringConst.Value == CurrentParsingFile)
                    return new ImportExpression(new CannotImportThisFile(stringConst.Value, stringConst.SourceContext));

                if (Imports.Contains(stringConst.Value + CurrentParsingFile) || Imports.Contains(CurrentParsingFile + stringConst.Value)) 
                    return new ImportExpression(new ThisFileAlreadyImported(stringConst.Value, stringConst.SourceContext));

                else if (Imports.Contains(stringConst.Value)) 
                    return new ImportExpression();

                else
                {
                    Imports.Add(CurrentParsingFile + stringConst.Value);
                    Imports.Add(stringConst.Value + CurrentParsingFile);
                    Imports.Add(stringConst.Value);
                }
                CurrentParsingFile = stringConst.Value;

                AbstractSyntaxTree ast = new AbstractSyntaxTree();
                ast.BuildTree(stringConst.Value);

                CurrentParsingFile = CompilingSourceFile;
                return new ImportExpression(stringConst, (Root)ast.Root);
            }
            return new ImportExpression(new WrongImport(stringConst.SourceContext));
        }

        public SyntaxTreeNode Parse(string parsingFile)
        {
            Lexer.GetNextToken();
            //Root root = RunParserOnlyOnArithExpression(ParsingFile.Path);
            if (parsingFile == CompilingSourceFile) Imports.Clear();
            Root root = RunParser(parsingFile);
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

            while (Match(tkImport))
            {
                Root.AddNode(ParseImportExpression());
                if (Root.Nodes.Last().Errored) break;
            }

            while (!Match(tkEOF))
                Root.AddNode(ParseFunctionDeclaration());
            return Root;
        }

        public SyntaxTreeNode ParseImportExpression()
        {
            if (!Match(tkImport)) 
                return new ImportExpression(new ReservedWordExpected("import", Lexer.CurrentToken));
            Lexer.GetNextToken();

            if (Match(tkDQuote))
            {
                StringConst stringImport = ParseStringConst();
                return Import(stringImport);
            }
            else if (Match(tkId))
            {
                IdentifierExpression idImport = ParseIdentifierDeclaration();

                if (!Match(tkSemicolon)) 
                    return new ImportExpression(new MissingSemi(Lexer.PreviousToken));
                Lexer.GetNextToken();

                return Import(idImport);
            }

            else return new ImportExpression(new ExpectedCorrectImport(Lexer.CurrentToken.Context));
        }
        public SyntaxTreeNode ParseExternalFunctionDeclaration()
        {
            Lexer.GetNextToken();
            if (!Match(tkExternalProp)) 
                return new FunctionDeclaration(new ReservedWordExpected("external", Lexer.CurrentToken));
            Lexer.GetNextToken();

            StringConst packageName = ParseStringConst();

            if (!Match(tkFunc)) 
                return new FunctionDeclaration(new ReservedWordExpected("func", Lexer.CurrentToken));
            Lexer.GetNextToken();

            IdentifierExpression funcname = ParseIdentifierDeclaration();
            SourceContext funccontext = Lexer.PreviousToken.Context;
            Arguments args = ParseArgumentDeclarations();

            if (!Match(tkColon)) 
                return new FunctionDeclaration(new ReservedSymbolExpected(":", Lexer.CurrentToken));
            Lexer.GetNextToken();

            TypeExpression functype = ParseTypeExpression();

            if(!Match(tkSemicolon)) 
                return new FunctionDeclaration(new ReservedSymbolExpected(";", Lexer.CurrentToken));
            Lexer.GetNextToken();

            return new FunctionDeclaration(funcname, args, functype, packageName.Value, funccontext);
        }

        public Arguments ParseArgumentDeclarations()
        {
            //used for function declaration args
            Arguments args = new Arguments();
            args.SourceContext.FilePath = CurrentParsingFile;
            args.SourceContext.StartsAt = new Position(Lexer.CurrentToken);

            TypeExpression argType;
            IdentifierDeclaration argName;

            // (.... )

            if (!Match(tkLpar)) 
                return new Arguments(new MissingLpar(Lexer.CurrentToken));
            Lexer.GetNextToken();

            while (!Match(tkRpar) && !Match(tkEOF))
            {
                argType = ParseTypeExpression();
                argName = ParseIdentifierDeclaration(argType.Type);

                if (!Match(tkComma))
                {
                    if (!Match(tkRpar))
                        return new Arguments(new ReservedSymbolExpected(",", Lexer.CurrentToken));
                }
                else 
                    Lexer.GetNextToken();
                args.AddNode(new ArgumentDeclaration(argType, argName));
            }

            if (!Match(tkRpar)) 
                return new Arguments(new MissingRpar(Lexer.CurrentToken));

            args.SourceContext.EndsAt = new Position(Lexer.CurrentToken);

            Lexer.GetNextToken();
            return args;
        }

        public Arguments ParseArgumentExpressions()
        {
            //used for function call args
            Arguments argValues = new Arguments();
            argValues.SourceContext.FilePath = CurrentParsingFile;
            argValues.SourceContext.StartsAt = new Position(Lexer.CurrentToken);

            if (!Match(tkLpar))
                return new Arguments(new MissingLpar(Lexer.CurrentToken));

            Lexer.GetNextToken();
            while (!Match(tkRpar) && !Match(tkEOF))
            {
                argValues.AddNode(ParseExpression());

                if (!Match(tkComma))
                {
                    if (!Match(tkRpar))
                        return new Arguments(new ReservedSymbolExpected(",", Lexer.CurrentToken));
                }
                else
                    Lexer.GetNextToken();
            }

            if (!Match(tkRpar))
                return new Arguments(new MissingRpar(Lexer.CurrentToken));

            argValues.SourceContext.EndsAt = new Position(Lexer.CurrentToken);

            Lexer.GetNextToken();
            return argValues;
        }

        public Body ParseBodyBlock()
        {
            Body body = new Body();
            body.SourceContext.FilePath = CurrentParsingFile;
            body.SourceContext.StartsAt = new Position(Lexer.CurrentToken);

            if (!Match(tkLbra)) 
                return new Body(new MissingLbra(Lexer.CurrentToken));
            Lexer.GetNextToken();
            
            while (!Match(tkRbra) && !Match(tkEOF))
                body.AddNode(ParseStatement());
            
            if(!Match(tkRbra)) 
                //Сделать отображение строки в ConsoleErrorDrawer, тк без нее не понятно где ошибка
                return new Body(new MissingRbra(Lexer.CurrentToken));

            body.SourceContext.EndsAt = new Position(Lexer.CurrentToken);

            Lexer.GetNextToken();
            return body;
        }

        public TypeExpression ParseTypeExpression()
        {
            if (!Match(tkType))
                return new TypeExpression(new TypeExpected(Lexer.CurrentToken));
            TypeExpression typeExpression = new TypeExpression(Lexer.CurrentToken);
            Lexer.GetNextToken();
            return typeExpression;
        }

        public IdentifierCall ParseIdentifierCall()
        {
            if (!Match(tkId))
                return new IdentifierCall(new IdentifierExpected(Lexer.CurrentToken));
            IdentifierCall identifierCall = new IdentifierCall(Lexer.CurrentToken);
            Lexer.GetNextToken();
            return identifierCall;
        }

        public IdentifierDeclaration ParseIdentifierDeclaration(InnerType type = null)
        {
            if (!Match(tkId))
                return new IdentifierDeclaration(new IdentifierExpected(Lexer.CurrentToken));
            IdentifierDeclaration identifierExpression;

            if (type == null)
                identifierExpression = new IdentifierDeclaration(Lexer.CurrentToken);
            else
                identifierExpression = new IdentifierDeclaration(Lexer.CurrentToken,type);
            Lexer.GetNextToken();
            return identifierExpression;
        }

        public SyntaxTreeNode ParseFunctionDeclaration()
        {
            if (Match(tkAt)) 
                return ParseExternalFunctionDeclaration();
            if (!Match(tkFunc)) 
                return new FunctionDeclaration(new ReservedWordExpected("func", Lexer.CurrentToken));

            Lexer.GetNextToken();
            IdentifierExpression funcName = ParseIdentifierDeclaration();
            SourceContext funcContext = Lexer.PreviousToken.Context;
            Arguments args = ParseArgumentDeclarations();

            if (!Match(tkColon)) 
                return new FunctionDeclaration(new ReservedSymbolExpected(":", Lexer.CurrentToken));

            Lexer.GetNextToken();
            TypeExpression funcType = ParseTypeExpression();
            Body           funcBody = ParseBodyBlock();
            return new FunctionDeclaration(funcName, args, funcType, funcBody, funcContext);
        }
        
        public SyntaxTreeNode ParseVariableDeclaration()
        {
            TypeExpression   idType = ParseTypeExpression();
            IdentifierExpression id = ParseIdentifierDeclaration(idType.Type);
            AssignmentExpression assign;

            if (Match(tkAssign))
            {
                Lexer.GetNextToken();
                assign = new AssignmentExpression(id,Operator.Assignment, ParseExpression());
           
                if (!Match(tkSemicolon)) 
                    return new DeclarationExpression(new MissingSemi(Lexer.PreviousToken));
                Lexer.GetNextToken();

                return new DeclarationExpression(idType, assign);
            }
            else if (Match(tkSemicolon))
            {
                Lexer.GetNextToken();
                return new DeclarationExpression(idType, id);
            }
            else return new DeclarationExpression(new ErrorMessage("Ожидался символ [=] или [;]", Lexer.CurrentToken));
        }
        public SyntaxTreeNode ParseAssignmentExpression()
        {
            IdentifierExpression id = ParseIdentifierCall();

            if (!Match(tkAssign)     &&
                !Match(tkAddAssign)  &&
                !Match(tkMultAssign) &&
                !Match(tkDivAssign)  &&
                !Match(tkSubAssign)) 
                return new AssignmentExpression(new ErrorMessage("Ожидался символ присваивания", Lexer.CurrentToken));

            Lexer.GetNextToken();
            AssignmentExpression assign = new AssignmentExpression(id,GetOperatorFromTokenType(Lexer.PreviousToken.TokenType),ParseExpression());

            if (!Match(tkSemicolon)) 
                return new AssignmentExpression(new MissingSemi(Lexer.CurrentToken));
            Lexer.GetNextToken();
            return assign;
        }
        public SyntaxTreeNode ParseStatement()
        {
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkId:   return ParseIdentifierAmbiguity();
                case tkIf:   return ParseIfStatement();
                case tkWhile:return ParseWhileStatement();
                case tkDo:   return ParseDoWhileStatement();
                case tkType: return ParseVariableDeclaration();
                case tkRet:  return ParseReturnExpression();

                // wrong error return
                default: return new ReturnExpression(new ErrorMessage("Ожидалось выражение", Lexer.CurrentToken));
            }
        }
        public SyntaxTreeNode ParseIdentifierAmbiguity()
        {
            if (Match(tkLpar,1)) 
                return ParseFunctionCall();
            else 
                return ParseAssignmentExpression();
        }

        public Condition ParseCondition()
        {
            Condition condition = new Condition();
            condition.SourceContext.FilePath = CurrentParsingFile;
            condition.SourceContext.StartsAt = new Position(Lexer.CurrentToken);
            condition.AddNode(ParseBooleanParentisizedExpression());
            condition.SourceContext.EndsAt = new Position(Lexer.CurrentToken);

            return condition;
        }

        public SyntaxTreeNode ParseFunctionCall(bool parseAsSingleExpression = true)
        {
            SourceContext funcContext = new SourceContext();
            funcContext.FilePath = CurrentParsingFile;
            funcContext.StartsAt = Lexer.CurrentToken.Context.StartsAt;
            IdentifierCall funcName = new IdentifierCall(Lexer.CurrentToken);
            Lexer.GetNextToken();

            Arguments argValues = ParseArgumentExpressions();
            funcContext.EndsAt = Lexer.CurrentToken.Context.EndsAt;

            if (parseAsSingleExpression)
            {
                 // ....
                 // someFunc();
                 //           ^
                 // ....

                if (!Match(tkSemicolon)) 
                    return new FunctionCall(new MissingSemi(Lexer.PreviousToken));
                Lexer.GetNextToken();
            }
            return new FunctionCall(funcName.Name, argValues, funcContext);
        }

        public SyntaxTreeNode ParseReturnExpression()
        {
            if (!Match(tkRet))
                return new ReturnExpression(new ReservedWordExpected("return", Lexer.CurrentToken));

            SourceContext retcontext = new SourceContext();
            retcontext.FilePath = CurrentParsingFile;
            retcontext.StartsAt = new Position(Lexer.CurrentToken);

            Lexer.GetNextToken();
            Expression expression;
            if (Match(tkSemicolon)) 
                expression = null;
            else
                expression = (Expression)ParseExpression();
            retcontext.EndsAt = new Position(Lexer.CurrentToken);

            if (!Match(tkSemicolon)) 
                return new ReturnExpression(new MissingSemi(Lexer.PreviousToken));
            Lexer.GetNextToken();

            return new ReturnExpression(expression, retcontext);
        }

        public SyntaxTreeNode ParseIfStatement()
        {
            if (!Match(tkIf)) 
                return new IfStatement(new ReservedWordExpected("if", Lexer.CurrentToken));

            SourceContext ifContext = new SourceContext();
            ifContext.FilePath = CurrentParsingFile;
            ifContext.StartsAt = new Position(Lexer.CurrentToken);

            Lexer.GetNextToken();

            Condition ifCondition = ParseCondition();
            Body      ifBody = ParseBodyBlock();
            IfStatement ifStmt = new IfStatement(ifCondition, ifBody, ifContext);

            if (Match(tkElse))
            {
                Lexer.GetNextToken();
                Body elseBody = ParseBodyBlock();
                ifStmt = new IfStatement(ifStmt, elseBody, ifContext);
            }
            return ifStmt;
        }
        public SyntaxTreeNode ParseWhileStatement()
        {
            if (!Match(tkWhile))
                return new WhileStatement(new ReservedWordExpected("while", Lexer.CurrentToken));
            Lexer.GetNextToken();

            Condition loopCondition = ParseCondition();
            Body      loopBody = ParseBodyBlock();

            return new WhileStatement(loopCondition, loopBody);
        }
        public SyntaxTreeNode ParseDoWhileStatement()
        {
            if (!Match(tkDo)) 
                return new DoWhileStatement(new ReservedWordExpected("do", Lexer.CurrentToken));
            Lexer.GetNextToken();

            Body loopBody = ParseBodyBlock();

            if (!Match(tkWhile)) 
                return new DoWhileStatement(new ReservedWordExpected("while", Lexer.CurrentToken));
            Lexer.GetNextToken();

            Condition loopCondition = ParseCondition();

            if (!Match(tkSemicolon)) 
                return new DoWhileStatement(new MissingSemi(Lexer.PreviousToken));
            Lexer.GetNextToken();

            return new DoWhileStatement(loopBody, loopCondition);
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
                node = ParseBooleanFactor();

            return node;
        }
        public SyntaxTreeNode ParseBooleanFactor()
        {
            SyntaxTreeNode node;
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkLpar:
                    return ParseBooleanParentisizedExpression();
                default:
                    node = ParseExpression();
                    switch (Lexer.CurrentToken.TokenType)
                    {
                        case tkLess:    Lexer.GetNextToken(); node = new BooleanExpression(node, Operator.Less, ParseExpression()); break;
                        case tkGreater: Lexer.GetNextToken(); node = new BooleanExpression(node, Operator.Greater, ParseExpression()); break;
                        case tkEqual:   Lexer.GetNextToken(); node = new BooleanExpression(node, Operator.Equal, ParseExpression()); break;
                        case tkNotEqual:  Lexer.GetNextToken();   node = new BooleanExpression(node, Operator.NotEqual, ParseExpression()); break;
                        case tkEqualLess: Lexer.GetNextToken();   node = new BooleanExpression(node, Operator.LessEqual, ParseExpression());break;
                        case tkEqualGreater: Lexer.GetNextToken();node = new BooleanExpression(node, Operator.GreaterEqual, ParseExpression()); break;
                    }
                    return node;
            }
        }

        public SyntaxTreeNode ParseBooleanParentisizedExpression()
        {
            if (!Match(tkLpar)) 
                return new BooleanExpression(new MissingLpar(Lexer.CurrentToken));
            Lexer.GetNextToken();

            SyntaxTreeNode node = ParseBooleanExpression();

            if (!Match(tkRpar)) 
                return new BooleanExpression(new MissingRpar(Lexer.CurrentToken));
            Lexer.GetNextToken();

            return node;
        }
        public SyntaxTreeNode ParseExpression()
        {
            SyntaxTreeNode node = ParseTerm();
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkMinus: Lexer.GetNextToken();  node = new BinaryExpression(node, Operator.Minus, ParseExpression()); break;
                case tkPlus:  Lexer.GetNextToken();  node = new BinaryExpression(node, Operator.Plus, ParseExpression()); break;
            }
            return node;
        }
        public SyntaxTreeNode ParseTerm()
        {
            SyntaxTreeNode node = ParseSignedFactor();
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkMult: Lexer.GetNextToken(); node = new BinaryExpression(node,  Operator.Multiplication, ParseTerm()); break;
                case tkDiv:  Lexer.GetNextToken(); node = new BinaryExpression(node,  Operator.Division, ParseTerm());      break;
            }
            return node;
        }

        public SyntaxTreeNode ParseSignedFactor()
        {
            SyntaxTreeNode node = ParseFactor();
            if (Match(tkMinus))
                node = new BinaryExpression(node,Operator.Plus,ParseFactor());
            return node;
        }

        public SyntaxTreeNode ParseFactor()
        {
            SyntaxTreeNode node;
            switch (Lexer.CurrentToken.TokenType)
            {
                case tkId:
                    if (Match(tkLpar, 1))
                        node = ParseFunctionCall(false);
                    else
                        node = ParseIdentifierCall();
                    return node;

                case tkMinus:
                    //Переписать
                    if (Match(tkMinus, -1)) return new BinaryExpression(new ErrorMessage("Возможно добавление только одного унарного минуса.",Lexer.CurrentToken));
                    Lexer.GetNextToken();
                    return new BinaryExpression(new IntegerConst("-1"), Operator.Multiplication, ParseFactor());

                case tkIntConst:
                    node = new IntegerConst(Lexer.CurrentToken);
                    Lexer.GetNextToken();
                    return node;

                case tkFloatConst:
                    node = new FloatConst(Lexer.CurrentToken);
                    Lexer.GetNextToken();
                    return node;

                case tkBooleanConst:
                    node = new BooleanConst(Lexer.CurrentToken);
                    Lexer.GetNextToken();
                    return node;

                case tkDQuote:
                    return ParseStringConst();

                case tkSQuote:
                    return ParseCharConst();

                case tkLpar: return ParseParentisizedExpression();

                default: return new BinaryExpression(new ErrorMessage("Ожидалось число,переменная,или выражение в скобках", Lexer.CurrentToken));
            }
        }
        public SyntaxTreeNode ParseParentisizedExpression()
        {
            if (!Match(tkLpar)) 
                return new BinaryExpression(new MissingLpar(Lexer.CurrentToken));
            Lexer.GetNextToken();

            SyntaxTreeNode node = ParseExpression();

            if (!Match(tkRpar)) 
                return new BinaryExpression(new MissingRpar(Lexer.CurrentToken));
            Lexer.GetNextToken();

            return node;
        }

        public StringConst ParseStringConst()
        {
            if (!Match(tkDQuote)) 
                return new StringConst(new ReservedSymbolExpected("\"", Lexer.CurrentToken));
            Lexer.GetNextToken();

            if (!Match(tkStringConst)) 
                return new StringConst(new ErrorMessage("Ожидалась строка.", Lexer.CurrentToken));

            StringConst stringConst = new StringConst(Lexer.CurrentToken);
            Lexer.GetNextToken();

            if (!Match(tkDQuote)) 
                return new StringConst(new ReservedSymbolExpected("\"", Lexer.CurrentToken));
            Lexer.GetNextToken();

            return stringConst;
        }

        public CharConst ParseCharConst()
        {
            if (!Match(tkSQuote)) 
                return new CharConst(new ReservedSymbolExpected("\'", Lexer.CurrentToken));
            Lexer.GetNextToken();

            if (!Match(tkCharConst)) 
                return new CharConst(new ErrorMessage("Ожидался символ", Lexer.CurrentToken));

            CharConst charConst = new CharConst(Lexer.CurrentToken);
            Lexer.GetNextToken();

            if (!Match(tkSQuote)) 
                return new CharConst(new ReservedSymbolExpected("\'", Lexer.CurrentToken));
            Lexer.GetNextToken();

            return charConst;
        }

        public bool Match(TokenType expectedType, int offset = 0)
        {
            return Lexer.Peek(offset).TokenType == expectedType ? true : false;
        }

        public bool Match(SyntaxTreeNode node, NodeType expectedType)
        {
            return node.NodeType == expectedType ? true : false;
        }

        public bool Match(Expression expression, NodeType expectedType)
        {
            return expression.NodeType == expectedType? true : false;
        }

        public Operator GetOperatorFromTokenType(TokenType type)
        {
            switch (type)
            {
                case tkPlus: return Operator.Plus;
                case tkMinus:return Operator.Minus;
                case tkMult: return Operator.Multiplication;
                case tkDiv:  return Operator.Division;

                case tkAssign:     return Operator.Assignment;
                case tkAddAssign:  return Operator.AssignmentAddition;
                case tkDivAssign:  return Operator.AssignmentDivision;
                case tkSubAssign:  return Operator.AssignmentSubtraction;
                case tkMultAssign: return Operator.AssignmentMultiplication;
                default: throw new Exception($"??[{type}]");
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

        public void SetSourceContext(Token token) => this.SourceContext = GetSourceContext(token);
        public void SetSourceContext(Token sToken, Token fToken) => this.SourceContext = GetSourceContext(sToken,fToken);
        public void SetSourceContext(SyntaxTreeNode node)        => this.SourceContext = GetSourceContext(node);
        public void SetSourceContext(SyntaxTreeNode lnode, SyntaxTreeNode rnode) => this.SourceContext = GetSourceContext(lnode,rnode);

        public virtual string ToConsoleString() => $"{NodeType}";

        public void AddNode(SyntaxTreeNode node)
        {
            if (node == null)
                return;
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
        public Body(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }

    public class Condition : SyntaxTreeNode
    {
        public override NodeType NodeType  => NodeType.Condition;
        public override ConsoleColor Color => this.Parent.Color;
        public Condition(SyntaxTreeNode condition) => this.AddNode(condition);
        public Condition() { }
    }

    public class Arguments : SyntaxTreeNode
    {
        public override NodeType NodeType  => NodeType.Arguments;
        public override ConsoleColor Color => this.Parent.Color;
        public Arguments(SyntaxTreeNode arguments) => this.AddNode(arguments);
        public Arguments() { }
        public Arguments(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }

    public sealed class FunctionDeclaration : SyntaxTreeNode
    {
        public string Name { get; private set; }
        public Body Body { get; private set; }
        public int ArgumentCount { get; private set; }
        public Arguments Arguments { get; private set; }
        public InnerType Type { get; private set; }

        public bool External  { get; protected set; }
        public string Package { get; protected set; }

        public override NodeType NodeType => NodeType.Function;
        public override ConsoleColor Color => ConsoleColor.DarkYellow;

        public FunctionDeclaration(IdentifierExpression identifierExpression, Arguments arguments, TypeExpression type, Body body, SourceContext context)
        {
            this.External = false;
            this.Body = body;
            this.Name = identifierExpression.Name;
            this.Type = type.Type;
            this.Arguments = arguments;
            this.SourceContext = context;
            this.ArgumentCount = arguments.Nodes.Count;

            this.AddNodes(arguments, body);
        }

        //External case
        public FunctionDeclaration(IdentifierExpression identifierExpression, Arguments arguments, TypeExpression type,string package, SourceContext context)
        {
            this.External = true;
            this.Package = package;
            this.Name = identifierExpression.Name;
            this.Type = type.Type;
            this.Arguments = arguments;
            this.SourceContext = context;
            this.ArgumentCount = arguments.Nodes.Count;

            this.AddNodes(arguments);
        }

        public FunctionDeclaration(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }

        public override string ToConsoleString() => $"{Name}:{Type.Representation}:[decl]" + (External ? ":[external]":"");
    }

    public sealed class FunctionCall : Expression , ITypeable
    {
        public override NodeType NodeType  => NodeType.FunctionCall;
        public override ConsoleColor Color => ConsoleColor.DarkYellow;

        public InnerType Type      { get; set; }
        public string Name         { get; private set; }
        public int ArgumentCount   { get; private set; }
        public InnerType[] Arguments { get; set; }
        public Arguments ArgumentsValues { get; private set; }

        public FunctionCall(string name, Arguments argumentValues,SourceContext context)
        {
            this.Name = name;
            this.SourceContext = context;
            this.ArgumentsValues = argumentValues;
            this.ArgumentCount = argumentValues.Nodes.Count;

            this.AddNode(argumentValues);
        }

        public FunctionCall(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }

        public override string ToConsoleString() => $"{Name}:{Type}:[call]";
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

        public ImportExpression() { }

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
        public Body Body     { get; protected set; }
        public Body ElseBody { get; protected set; }
        public Condition Condition { get; protected set; }
        public override ConsoleColor Color => ConsoleColor.Blue;
    }

    public sealed class IfStatement : Statement
    {
        private NodeType type;

        public override NodeType NodeType => type;

        public IfStatement(Condition condition, Body body, SourceContext context)
        {
            this.type = NodeType.If;
            this.SourceContext = context;
            this.Condition = condition;
            this.Body = body;
            this.AddNodes(condition, body);
        }
        public IfStatement(IfStatement ifStatement, Body elseBody, SourceContext context)
        {
            this.type = NodeType.IfElse;
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
        public SyntaxTreeNode Left  { get; set; }
        public Operator Op          { get; protected set; }
        public SyntaxTreeNode Right { get; set; }
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

    public sealed class FloatConst : ConstExpression
    {
        public override NodeType NodeType => NodeType.FloatConstant;
        public override ConsoleColor Color => ConsoleColor.DarkMagenta;

        public override InnerType Type => new Real32();

        public FloatConst(string value)
        {
            this.Value = value;
        }

        public FloatConst(Token token)
        {
            this.SetSourceContext(token);
            this.Value = token.Value;
        }

        public FloatConst(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }
    public sealed class IntegerConst : ConstExpression
    {
        public override NodeType NodeType  => NodeType.IntegerConstant;
        public override ConsoleColor Color => ConsoleColor.DarkMagenta;

        public override InnerType Type => new Other.InnerTypes.Int32();

        public IntegerConst(string value)
        {
            this.Value = value;
        }

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
        private NodeType type;

        public override NodeType NodeType  => type;
        public override ConsoleColor Color => ConsoleColor.Green;

        public override InnerType Type => new Other.InnerTypes.Boolean();

        public BooleanConst(Token token)
        {
            if      (token.Value == "true")  type = NodeType.True;
            else if (token.Value == "false") type = NodeType.False;

            this.SetSourceContext(token);
            this.Value = token.Value;
        }

        public BooleanConst(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }

    public sealed class CharConst : ConstExpression
    {
        public override NodeType NodeType => NodeType.CharConstant;
        public override ConsoleColor Color => ConsoleColor.Yellow;

        public override InnerType Type => new Other.InnerTypes.Char();

        public CharConst(Token token)
        {
            this.SetSourceContext(token);
            this.Value = token.Value;
        }

        public CharConst(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }

        public override string ToConsoleString() => $"\'{Value}\':{Type}";
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

    public sealed class UnaryExpression : Expression
    {
        public override NodeType NodeType => NodeType.UnaryExpression;

        public UnaryExpression(Operator op, SyntaxTreeNode right)
        {
            this.Op = op;
            switch(op)
            {
                case Operator.Minus:
                    this.Right = new BinaryExpression(new IntegerConst("-1"),Operator.Multiplication,right);
                    break;
                /*case Operator.PrefixIncrement:
                    this.Right = new BinaryExpression(right,Operator )
                    break;*/
            }
            this.SetSourceContext(right);
            this.AddNode(right);
        }
    }

    public sealed class BinaryExpression : Expression
    {
        private NodeType type;
        private ConsoleColor color;

        public override NodeType NodeType  => type;
        public override ConsoleColor Color => color;

        public BinaryExpression(SyntaxTreeNode left, Operator op, SyntaxTreeNode right)
        {
            switch (op)
            {
                case Operator.Plus:           type = NodeType.Addition;       color = ConsoleColor.DarkYellow; break;
                case Operator.Minus:          type = NodeType.Substraction;   color = ConsoleColor.DarkYellow; break;
                case Operator.Multiplication: type = NodeType.Multiplication; color = ConsoleColor.Yellow;     break;
                case Operator.Division:       type = NodeType.Division;       color = ConsoleColor.Yellow;     break;
                default: throw new Exception("Встречен неизвестный оператор: " + op.ToString());
            }
            this.SetSourceContext(left, right);
            this.Left = left;
            this.Op = op;
            this.Right = right;
            this.AddNodes(left, right);
        }

        public BinaryExpression(Operator op, SyntaxTreeNode right)
        {
            this.SetSourceContext(right);
            this.Op = op;
            this.Right = right;
            this.AddNodes(right);
        }

        public BinaryExpression(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }

    }
    public sealed class BooleanExpression : Expression
    {
        private NodeType type;
        private ConsoleColor color;

        public override NodeType NodeType  => type;
        public override ConsoleColor Color => color;

        public BooleanExpression(SyntaxTreeNode left, Operator op, SyntaxTreeNode right)
        {
            // Opertator -> NodeType
            switch (op)
            {
                case Operator.LogicalAND: type = NodeType.And; color = ConsoleColor.DarkGreen; break;
                case Operator.LogicalOR:  type = NodeType.Or;  color = ConsoleColor.DarkGreen; break;
                case Operator.LogicalNOT: type = NodeType.Not; color = ConsoleColor.DarkGreen; break;
                case Operator.Less:       type = NodeType.LessThan;  color = ConsoleColor.Green; break;
                case Operator.LessEqual:  type = NodeType.EqualLess; color = ConsoleColor.Green; break;
                case Operator.Greater:    type = NodeType.MoreThan;  color = ConsoleColor.Green; break;
                case Operator.GreaterEqual: type = NodeType.EqualMore; color = ConsoleColor.Green; break;
                case Operator.Equal:        type = NodeType.Equal;     color = ConsoleColor.Green; break;
                case Operator.NotEqual:     type = NodeType.NotEqual;  color = ConsoleColor.Green; break;

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
            type  = NodeType.Not;
            color = ConsoleColor.DarkGreen;
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

        public AssignmentExpression(SyntaxTreeNode left,Operator assignType, SyntaxTreeNode right)
        {
            this.Op = assignType;
            this.Left = left;
            switch (assignType)
            {
                case Operator.Assignment:
                    this.Right = right;
                    break;
                case Operator.AssignmentAddition:
                    this.Right = new BinaryExpression(left,Operator.Plus,right);
                    break;
                case Operator.AssignmentSubtraction:
                    this.Right = new BinaryExpression(left, Operator.Minus, right);
                    break;
                case Operator.AssignmentMultiplication:
                    this.Right = new BinaryExpression(left, Operator.Multiplication, right);
                    break;
                case Operator.AssignmentDivision:
                    this.Right = new BinaryExpression(left, Operator.Division, right);
                    break;
            }
            this.SetSourceContext(this.Left, this.Right);
            this.AddNodes(left,right);
        }

        public AssignmentExpression(SyntaxError error)
        {
            this.Errored = true;
            Diagnostics.SyntaxErrors.Add(error);
        }
    }

    public sealed class GlobalDeclarationExpression : Expression
    {
        public override NodeType NodeType => NodeType.Declaration;

        public GlobalDeclarationExpression(SyntaxTreeNode declarationExpression)
        {
            this.SetSourceContext(declarationExpression);
            this.Right = declarationExpression;
            this.AddNodes(Right);
        }

        public GlobalDeclarationExpression(SyntaxError error)
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
            this.AddNode(Right);
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

    public interface IArrayTypeable
    {

    }
}