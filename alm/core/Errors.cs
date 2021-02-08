using alm.Core.InnerTypes;
using alm.Other.Structs;

namespace alm.Core.Errors
{
    public abstract class CompilerError
    {
        protected SourceContext Context;

        public bool HasContext { get; private set; } = true;
        public Position StartsAt => Context.StartsAt;
        public Position EndsAt   => Context.EndsAt;
        public string FilePath   => Context.FilePath;

        public string Message { get; protected set; }
        public CompilerUnitErrorType UnitErrorType { get; protected set; }
        public virtual string GetMessage()
        {
            if (this.Context == default)
            {
                this.HasContext = false;
                return $"{Message}";
            }
            if (this.FilePath == null)
                return $"{Message} {this.StartsAt}";
            return $"{Message} \nIn file <{System.IO.Path.GetFileName(FilePath)}> at {this.StartsAt}";
        }
    }

    public abstract class SyntaxError : CompilerError
    {
        public SyntaxError(string Message, SourceContext Context)
        {
            base.Message = Message;
            base.Context = Context;
            base.UnitErrorType = CompilerUnitErrorType.SyntaxError;
        }
    }
    public abstract class SemanticError : CompilerError
    {
        public SemanticError(string message, SourceContext context)
        {
            base.Message = message;
            base.Context = context;
            base.UnitErrorType = CompilerUnitErrorType.SemanticError;
        }
    }
    public sealed class SyntaxErrorMessage : SyntaxError
    {
        public SyntaxErrorMessage(string message, Token token) : base($"{message}.", token.Context) { }
        public SyntaxErrorMessage(string message, SourceContext context) : base($"{message}.", context) { }
    }
    public sealed class SemanticErrorMessage : SemanticError
    {
        public SemanticErrorMessage(string message, Token token) : base($"{message}.", token.Context) { }
        public SemanticErrorMessage(string message, SourceContext context) : base($"{message}.", context) { }
    }

    public sealed class MissingLbra : SyntaxError
    {
        public MissingLbra(Token token) : base("Symbol [{] expected.", token.Context) { }
    }
    public sealed class MissingRbra : SyntaxError
    {
        public MissingRbra(Token token) : base("Symbol [}] expected.", token.Context) { }
    }
    public sealed class MissingLpar : SyntaxError
    {
        public MissingLpar(Token token) : base("Symbol [(] expected.", token.Context) { }
    }
    public sealed class MissingRpar : SyntaxError
    {
        public MissingRpar(Token token) : base("Symbol [)] expected.", token.Context) { }
    }
    public sealed class MissingSemi : SyntaxError
    {
        public MissingSemi(Token token) : base("Symbol [;] expected.", new SourceContext(new Position(token.Context.StartsAt.CharIndex, token.Context.StartsAt.LineIndex),token.Context.EndsAt)) { }
    }
    public sealed class MissingAssign : SyntaxError
    {
        public MissingAssign(Token token) : base("Symbol [=] expected.", token.Context) { }
    }
    public sealed class MissingColon : SyntaxError
    {
        public MissingColon(Token token) : base("Symbol [:] expected.", token.Context) { }
    }
    public sealed class MissingComma : SyntaxError
    {
        public MissingComma(Token token) : base("Symbol [,] expected.", token.Context) { }
    }
    public sealed class MissingDQuote : SyntaxError
    {
        public MissingDQuote(Token token) : base("Symbol [\"] expected.", token.Context) { }
    }
    public sealed class MissingSQuote : SyntaxError
    {
        public MissingSQuote(Token token) : base("Symbol [\'] expected.", token.Context) { }
    }
    public sealed class MissingNameOrValue : SyntaxError
    {
        public MissingNameOrValue(Token token) : base("The identifier's name or value expected.", token.Context) { }
    }


    public sealed class TypeExpected : SyntaxError
    {
        public TypeExpected(Token token) : base("Type expression expected.", token.Context) { }
    }
    public sealed class IdentifierExpected : SyntaxError
    {
        public IdentifierExpected(Token token) : base("Identifier expression expected.", token.Context) { }
    }
    public sealed class ReservedWordExpected : SyntaxError
    {
        public ReservedWordExpected(string word, Token token) : base($"Reserved-word [{word}] expected.", token.Context) { }
    }
    public sealed class ReservedSymbolExpected : SyntaxError
    {
        public ReservedSymbolExpected(string symbol, Token token) : base($"Reserved-symbol [{symbol}] expected.", token.Context) { }
    }
    public sealed class MainMethodExpected : SemanticError
    {
        public MainMethodExpected() : base($"Main method expected in executable file.", new SourceContext()) { }
    }
    public sealed class CorrectImportExpected : SyntaxError
    {
        public CorrectImportExpected(SourceContext context) : base($"Import (short or direct path) expression expected.", context) { }
    }

    public sealed class WrongImport : SyntaxError
    {
        public WrongImport(SourceContext context) : base($"Error occurred when trying to import file by non-existent path.", context) { }
    }
    public sealed class WrongImportExtension : SyntaxError
    {
        public WrongImportExtension(SourceContext context) : base($"File of \"alm\" extension expected.", context) { }
    }
    public sealed class WrongArrayElementDimension : SemanticError
    {
        public WrongArrayElementDimension(SourceContext context) : base($"The dimension of the array and array element must be the same.", context) { }
    }

    public sealed class ConnotImportThisModule : SyntaxError
    {
        public ConnotImportThisModule(string path, SourceContext context) : base($"Cannot import file by path: \"{path}\".", context) { }
    }

    public sealed class ModuleIsAlreadyImported : SyntaxError
    {
        public ModuleIsAlreadyImported(string path, SourceContext context) : base($"This file \"{path}\" is already imported.", context) { }
    }

    public sealed class IncompatibleReturnType : SemanticError
    {
        public IncompatibleReturnType(string name,InnerType returnType, InnerType expectedReturnType, SourceContext context) : base($"Incompatible return type for method [{name}], expected [{returnType}] type, but met the [{expectedReturnType}] type.", context) { }
    }
    public sealed class IncompatibleConditionType : SemanticError
    {
        public IncompatibleConditionType(SourceContext context) : base($"Incompatible condition type. Every condition has boolean type.", context) { }
    }
    public sealed class IncompatibleAssignmentType : SemanticError
    {
        public IncompatibleAssignmentType(InnerType adressorType, InnerType adressableType, SourceContext context) : base($"Incompatible types in identifier assigning, expected {adressableType.ALMRepresentation} type, but met the {adressorType.ALMRepresentation} type.", context) { }
    }
    public sealed class IncompatibleMethodParameterType : SemanticError
    {
        public IncompatibleMethodParameterType(string name,InnerType tableParamType, InnerType paramType, SourceContext context) : base($"Incompatible type of method's parameter [{name}], expected [{tableParamType}] type, but met the [{paramType}] type.", context) { }
    }
    public sealed class OperatorMustBeSituatedInLoop : SemanticError
    {
        public OperatorMustBeSituatedInLoop(string op, SourceContext context) : base($"Operator [{op}] must be located in the body of loop statement.", context) { }
    }
    public sealed class OperatorWithWrongOperandTypes : SemanticError
    {
        public OperatorWithWrongOperandTypes(string message, SourceContext context) : base($"{message}.", context) { }
    }

    public sealed class NotAllCodePathsReturnsValues : SemanticError
    {
        public NotAllCodePathsReturnsValues(SourceContext context) : base("Not all code paths returns values.", context) { }
    }

    public sealed class IdentifierIsAlreadyDeclared : SemanticError
    {
        public IdentifierIsAlreadyDeclared(string name,SourceContext context) : base($"Identifier with name [{name}] is already declared in this local area.", context) { }
    }
    public sealed class IdentifierIsNotDeclared : SemanticError
    {
        public IdentifierIsNotDeclared(string name, SourceContext context) : base($"Identifier with name [{name}] is not declared yet.", context) { }
    }
    public sealed class IdentifierIsNotInitialized : SemanticError
    {
        public IdentifierIsNotInitialized(string name, SourceContext context) : base($"Identifier with name [{name}] is not initialized in this local area.", context) { }
    }

    public sealed class MethodIsNotDeclared : SemanticError
    {
        public MethodIsNotDeclared(string name, SourceContext context) : base($"Method [{name}] is not declared.", context) { }
    }
    public sealed class MethodWithThoseArgumentsIsNotDeclared : SemanticError
    {
        public MethodWithThoseArgumentsIsNotDeclared(string name, SourceContext context) : base($"Method [{name}] with those types of arguments is not declared.", context) { }
    }
    public sealed class MethodIsAlreadyDeclared : SemanticError
    {
        public MethodIsAlreadyDeclared(string name, SourceContext context) : base($"Method [{name}] with those type of arguments is already declared here.", context) { }
    }

    public sealed class IncorrectDimension : SemanticError
    {
        public IncorrectDimension(int dimension,SourceContext context) : base($"Expected [{dimension}] dimension", context) { }
    }
    public sealed class ArrayIsNotDeclared : SemanticError
    {
        public ArrayIsNotDeclared(string name,SourceContext context) : base($"Array [{name}] is not declared in this local area.", context) { }
    }
    public sealed class CannotChangeTheString : SemanticError
    {
        public CannotChangeTheString(SourceContext context) : base($"Cannot change the content of \"string\" variable, it has read-only access.", context) { }
    }
    public sealed class CannotFindCastMethod : SemanticError
    {
        public CannotFindCastMethod(string name,string inFile) : base($"Cannot find cast method [{name}], to fix this, import \'cast\' lib.\nIn file <{System.IO.Path.GetFileName(inFile)}>.", new SourceContext()) { }
    }

    public sealed class ErrorForDebug : SemanticError
    {
        public ErrorForDebug(string message) : base($"{message}.", new SourceContext()) { }
    }

    public enum CompilerUnitErrorType
    {
        ScanError,
        SyntaxError,
        SemanticError
    }
}