using alm.Core.Errors;
using alm.Core.FrontEnd.SemanticAnalysis;
using alm.Core.FrontEnd.SyntaxAnalysis;
using alm.Core.InnerTypes;
using alm.Other.ConsoleStuff;
using alm.Other.Enums;
using alm.Other.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using static alm.Core.Compiler.Compiler;
using static alm.Core.Compiler.Compiler.CompilationVariables;
using static alm.Other.Enums.TokenType;
using static alm.Other.Structs.SourceContext;

namespace alm.Core.SyntaxTree
{
    public sealed class AbstractSyntaxTree
    {
        public SyntaxTreeNode Root { get; private set; }

        public AbstractSyntaxTree(string path)
        {
            this.BuildTree(path);
        }

        public void BuildTree(string path)
        {
            Lexer lexer = new Lexer(path);
            this.Root = new Parser(lexer).Parse(path);
        }
        public void ShowTree()
        {
            #if DEBUG
            if (!Diagnostics.SyntaxAnalysisFailed && !Diagnostics.SemanticAnalysisFailed)
                if (Shell.ShellInfo.ShowTree)
                    ShowTreeInConsole(Root, "", true);
            #endif
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
                    ConsoleCustomizer.ColorizedPrint(indent + (node == startNode.Childs.Last() ? "└──" : "├──"), ConsoleColor.DarkGray);
                    ConsoleCustomizer.ColorizedPrintln(node.ToString(), node.ConsoleColor);
                    ShowTreeInConsole(node, indent + (node.Childs.Count >= 1 && node == startNode.Childs.Last() ? string.Empty : "│"));
                }
            }
        }
    }

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

        public ModuleRoot(string modulePath, SyntaxTreeNode root)
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
        private string LibPath = "libs";

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
            string parsingDir = System.IO.Path.GetDirectoryName(CurrentParsingModule);
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
                    importedModules.Add(new ErroredStatement(new WrongImport(this.SourceContext)));
                    return;
                }

                if (System.IO.Path.GetExtension(currentImportedModule) != ".alm")
                {
                    importedModules.Add(new ErroredStatement(new WrongImportExtension(this.SourceContext)));
                    return;
                }
                //case when trying import module where this import was called
                if (currentImportedModule == CurrentParsingModule)
                {
                    importedModules.Add(new ErroredStatement(new ConnotImportThisModule(currentImportedModule, this.SourceContext)));
                    return;
                }
                //case when trying import already imported module
                if (this.GetImportsModules(currentImportedModule).Contains(CurrentParsingModule))
                {
                    importedModules.Add(new ErroredStatement(new ModuleIsAlreadyImported(currentImportedModule, this.SourceContext)));
                    return;
                }
                //if find import at list in 1 module, skip it (because it alrealdy imported) 
                if (this.GetImportsModules(currentImportedModule).Length > 0)
                {
                    return;
                }

                if (!CompilationImports.ContainsKey(CurrentParsingModule))
                    CompilationImports.Add(CurrentParsingModule, new List<string>() { currentImportedModule });
                else
                    CompilationImports[CurrentParsingModule].Add(currentImportedModule);

                //Parsing module by path 
                Lexer lexer = new Lexer(currentImportedModule);
                Parser parser = new Parser(lexer);
                SyntaxTreeNode importedModule = parser.Parse(currentImportedModule);

                CurrentParsingModule = CompilationEntryModule;

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
            foreach (string key in CompilationImports.Keys)
            {
                foreach (string value in CompilationImports[key])
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

        //divide by two constructorss
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


        public InnerType GetArgumentType(string argName)
        {
            foreach (ArgumentDeclaration argument in Arguments)
                if (argName == argument.Identifier.Name)
                    return argument.Type;
            return null;
        }
        public InnerType[] GetArgumentsTypes()
        {
            InnerType[] types = new InnerType[this.ArgCount];
            for (int i = 0; i < this.ArgCount; i++)
                types[i] = this.Arguments[i].Type;
            return types;
        }
        public ArgumentDeclaration[] CreateArgumentDeclarationInstances(Expression[] expressions)
        {
            ArgumentDeclaration[] arguments = new ArgumentDeclaration[expressions.Length];

            for (int i = 0; i < expressions.Length; i++)
                arguments[i] = (ArgumentDeclaration)expressions[i];

            return arguments;
        }

        public override string ToString() => (this.IsExternal ? "ext " : "") + $"func {this.Name}(args:{this.ArgCount})->{this.ReturnType}";
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

        private IdentifierExpression[] CreateIdentifierInstances(Expression[] expressions, InnerType withType)
        {
            IdentifierExpression[] identifiers = new IdentifierExpression[expressions.Length];
            for (int i = 0; i < expressions.Length; i++)
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
                this.GetParentByType(typeof(DoLoopStatement)) == null &&
                this.GetParentByType(typeof(ForLoopStatement)) == null)
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
        public Expression ReturnBody => this.Childs.Count == 0 ? null : (Expression)this.Childs.Last();
        public bool IsVoidReturn { get; private set; }

        public override NodeType NodeKind => NodeType.Return;

        public ReturnStatement(Expression returnBody, SourceContext context)
        {
            this.SourceContext = context;

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
        public Statement Body { get; protected set; }

        public override string ToString() => $"{this.NodeKind}";
    }
    public sealed class DoLoopStatement : IterationStatement
    {
        public override NodeType NodeKind => NodeType.Do;

        public DoLoopStatement(Expression condition, Statement body, SourceContext context)
        {
            this.SourceContext = context;
            this.Condition = condition;
            this.Body = body;
            this.AddNodes(condition, body);
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
        public IdentifierExpression[] IterableIdentifiers { get; private set; }

        public Statement InitStatement { get; private set; }
        public Statement StepStatement { get; private set; }

        public override NodeType NodeKind => NodeType.For;

        public ForLoopStatement(Statement initBlock, Expression conditionalBlock, Statement stepBlock, Statement body, SourceContext context)
        {
            this.SourceContext = context;
            this.InitStatement = initBlock;
            this.Condition = conditionalBlock;
            this.StepStatement = stepBlock;
            this.Body = body;

            if (initBlock != null)
               this.IterableIdentifiers = GetIterableIdentifiers(initBlock);

            this.AddNodes(initBlock, conditionalBlock, stepBlock, body);
        }

        private IdentifierExpression[] GetIterableIdentifiers(Statement initStatement)
        {
            IdentifierExpression[] identifiers;
            if (initStatement is IdentifierDeclaration)
            {
                identifiers = new IdentifierExpression[((IdentifierDeclaration)initStatement).DeclaringIdentifiers.Length];
                for (int i = 0; i < identifiers.Length; i++)
                    identifiers[i] = ((IdentifierDeclaration)initStatement).DeclaringIdentifiers[i];
            }
            else
            {
                identifiers = new IdentifierExpression[((AssignmentStatement)initStatement).AdressorExpressions.Length];
                for (int i = 0; i < identifiers.Length; i++)
                    identifiers[i] = (IdentifierExpression)((AssignmentStatement)initStatement).AdressorExpressions[i];
            }
            return identifiers;
        }
    }

    public abstract class SelectionStatement : Statement
    {
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
            this.AddNodes(condition, body);
        }

        public IfStatement(Expression condition, Statement body, Statement elseBody, SourceContext context)
        {
            this.SourceContext = context;
            this.Condition = condition;
            this.Body = body;
            this.ElseBody = elseBody;
            this.AddNodes(condition, body, elseBody);
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

        public AssignmentStatement(Expression adressor, AssignOperator operatorKind, Expression adressable)
        {
            this.SetSourceContext(adressor, adressable);
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
                case tkAssign: return AssignOperator.Assignment;
                case tkAddAssign: return AssignOperator.AssignmentAddition;
                case tkIDivAssign: return AssignOperator.AssignmentIDiv;
                case tkFDivAssign: return AssignOperator.AssignmentFDiv;
                case tkSubAssign: return AssignOperator.AssignmentSubtraction;
                case tkMultAssign: return AssignOperator.AssignmentMult;
                case tkPowerAssign: return AssignOperator.AssignmentPower;
                case tkRemndrAssign: return AssignOperator.AssignmentRemainder;

                case tkBitwiseOrAssign: return AssignOperator.AssignmentBitwiseOr;
                case tkBitwiseAnd: return AssignOperator.AssignmentBitwiseAnd;
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
                this.SetSourceContext(statements[0], statements[statements.Length - 1]);
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

        public ArrayInstance(Expression type, Expression[] dimensionSizes,SourceContext context)
        {
            this.SourceContext = context;
            this.Type = ((TypeExpression)type).Type;
            //+1 ??
            this.Dimension = (ushort)(dimensionSizes.Length + 1);
            this.DimensionSizes = dimensionSizes;
            foreach (Expression dimensionSize in dimensionSizes)
                this.AddNode(dimensionSize);
        }

        public bool IsPrimitive() => this.Dimension == 2 ? true : false;
        public override string ToString() => $"instance->{this.Type}";
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
        public Expression ParameterInstance => (Expression)this.Childs.Last();

        public override NodeType NodeKind => NodeType.Parameter;
        public override ConsoleColor ConsoleColor => ConsoleColor.DarkCyan;

        public ParameterDeclaration(Expression parameter)
        {
            this.SetSourceContext(parameter);
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

        public ArrayElementExpression(IdentifierExpression identifier, Expression[] indexes,SourceContext context)
        {
            this.SourceContext = context;
            this.ArrayName = identifier.Name;
            this.Indexes = indexes;
            this.Dimension = (ushort)indexes.Length;
            //this.AddNode(identifier);
            foreach (Expression index in indexes)
                this.AddNode(index);
        }

        public bool IsArrayPrimitive() => this.ArrayDimension == 1 ? true : false;
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

        public IdentifierExpression(Token token, State state = State.Decl)
        {
            this.SetSourceContext(token);
            this.Name = token.Value;
            this.IdentifierState = state;
        }

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
        public ParameterDeclaration[] Parameters { get; private set; }

        public override NodeType NodeKind => NodeType.MethodInvokation;
        public override ConsoleColor ConsoleColor => ConsoleColor.DarkYellow;

        public MethodInvokationExpression(string name, Expression[] parameters, SourceContext context)
        {
            this.Name = name;
            this.SourceContext = context;
            this.Parameters = CreateParameterInstances(parameters);
            this.ArgCount = (ushort)parameters.Length;

            foreach (Expression parameter in parameters)
                this.AddNode(parameter);
        }

        public InnerType[] GetArgumentsTypes()
        {
            InnerType[] types = new InnerType[this.ArgCount];
            TypeChecker.ReportErrors = false;
            for (int i = 0; i < this.ArgCount; i++)
                types[i] = TypeChecker.ResolveExpressionType(this.Parameters[i].ParameterInstance);
            return types;
        }
        public ParameterDeclaration[] CreateParameterInstances(Expression[] parameters)
        {
            ParameterDeclaration[] parameterDeclarations = new ParameterDeclaration[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
                parameterDeclarations[i] = (ParameterDeclaration)parameters[i];

            return parameterDeclarations;
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
        public override NodeType NodeKind => NodeType.IntegralConstant;

        public Int32Constant(string value) : base(value) { }
        public Int32Constant(Token token) : base(token) { }
    }
    public sealed class Int64Constant : ConstantExpression
    {
        public override InnerType Type => new InnerTypes.Int64();
        public override NodeType NodeKind => NodeType.IntegralConstant;

        public Int64Constant(string value) : base(value) { }
        public Int64Constant(Token token) : base(token) { }
    }
    public sealed class SingleConstant : ConstantExpression
    {
        public override InnerType Type => new InnerTypes.Single();
        public override NodeType NodeKind => NodeType.RealConstant;

        public SingleConstant(string value) : base(value) { }
        public SingleConstant(Token token) : base(token) { }
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
        public StringConstant(Token token) : base(token) { }

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
        public string GetOperatorRepresentation()
        {
            switch (this.OperatorKind)
            {
                case BinaryOperator.Conjuction:
                    return "and";
                case BinaryOperator.Disjunction:
                    return "or";
                case BinaryOperator.StrictDisjunction:
                    return "xor";
                case BinaryOperator.LessThan:
                    return "<";
                case BinaryOperator.GreaterThan:
                    return ">";
                case BinaryOperator.LessEqualThan:
                    return ">=";
                case BinaryOperator.GreaterEqualThan:
                    return "<=";
                case BinaryOperator.Equal:
                    return "==";
                case BinaryOperator.NotEqual:
                    return "!=";

                case BinaryOperator.Addition:
                    return "+";
                case BinaryOperator.Substraction:
                    return "-";
                case BinaryOperator.Mult:
                    return "*";
                case BinaryOperator.IDiv:
                    return "%";
                case BinaryOperator.FDiv:
                    return "/";
                case BinaryOperator.Power:
                    return "**";

                case BinaryOperator.BitwiseOr:
                    return "|";
                case BinaryOperator.BitwiseAnd:
                    return "&";
                case BinaryOperator.BitwiseXor:
                    return "^";

                case BinaryOperator.LShift:
                    return "<<";
                case BinaryOperator.RShift:
                    return ">>";

                default:
                    throw new Exception();
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
            this.SetSourceContext(leftOperand, rightOperand);
            if ((int)operatorKind >= 9)
                throw new Exception($"{operatorKind} is not logical operator.");
            this.OperatorKind = operatorKind;
            this.AddNodes(leftOperand, rightOperand);
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

        public UnaryOperator OperatorKind { get; set; }
        public Expression Operand => (Expression)this.Childs[0];

        private string CheckDefine(UnaryOperator unaryOperator)
        {
            switch (unaryOperator)
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
        public override NodeType NodeKind => NodeType.UnaryArithExpression;
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
}