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
            return $"{Message} \nВ файле ({FilePath[0]}:\\...\\{System.IO.Path.GetFileName(FilePath)}) {this.StartsAt}";
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
        public MissingLbra(Token token) : base("Ожидался символ [{].", token.Context) { }
    }
    public sealed class MissingRbra : SyntaxError
    {
        public MissingRbra(Token token) : base("Ожидался символ [}].", token.Context) { }
    }
    public sealed class MissingLpar : SyntaxError
    {
        public MissingLpar(Token token) : base("Ожидался символ [(].", token.Context) { }
    }
    public sealed class MissingRpar : SyntaxError
    {
        public MissingRpar(Token token) : base("Ожидался символ [)].", token.Context) { }
    }
    public sealed class MissingSemi : SyntaxError
    {
        public MissingSemi(Token token) : base("Ожидался символ [;].", new SourceContext(new Position(token.Context.StartsAt.CharIndex, token.Context.StartsAt.LineIndex),token.Context.EndsAt)) { }
    }
    public sealed class MissingAssign : SyntaxError
    {
        public MissingAssign(Token token) : base("Ожидался символ [=].", token.Context) { }
    }
    public sealed class MissingNameOrValue : SyntaxError
    {
        public MissingNameOrValue(Token token) : base("Ожидалось название переменной или ее значение.", token.Context) { }
    }
    public sealed class MissingColon : SyntaxError
    {
        public MissingColon(Token token) : base("Ожидался символ [:].", token.Context) { }
    }
    public sealed class MissingComma : SyntaxError
    {
        public MissingComma(Token token) : base("Ожидался символ [,].", token.Context) { }
    }
    public sealed class MissingDQuote : SyntaxError
    {
        public MissingDQuote(Token token) : base("Ожидался символ [\"].", token.Context) { }
    }
    public sealed class MissingSQuote : SyntaxError
    {
        public MissingSQuote(Token token) : base("Ожидался символ [\'].", token.Context) { }
    }


    public sealed class TypeExpected : SyntaxError
    {
        public TypeExpected(Token token) : base("Ожидался тип переменной.", token.Context) { }
    }
    public sealed class IdentifierExpected : SyntaxError
    {
        public IdentifierExpected(Token token) : base("Ожидался идентификатор.", token.Context) { }
    }
    public sealed class ReservedWordExpected : SyntaxError
    {
        public ReservedWordExpected(string word, Token token) : base($"Ожидалось ключевое слово [{word}].", token.Context) { }
    }
    public sealed class ReservedSymbolExpected : SyntaxError
    {
        public ReservedSymbolExpected(string symbol, Token token) : base($"Ожидался символ [{symbol}].", token.Context) { }
    }
    public sealed class MainMethodExpected : SemanticError
    {
        public MainMethodExpected() : base($"В запускаемом файле должен быть метод main.", new SourceContext()) { }
    }
    public sealed class CorrectImportExpected : SyntaxError
    {
        public CorrectImportExpected(SourceContext context) : base($"Ожидалось выражение для импортирования модуля.", context) { }
    }

    public sealed class WrongImport : SyntaxError
    {
        public WrongImport(SourceContext context) : base($"Попытка импортирования несуществующего файла.", context) { }
        public WrongImport() : base($"Ошибка импорта [debug parser2].", new SourceContext()) { }
    }
    public sealed class WrongImportExtension : SyntaxError
    {
        public WrongImportExtension(SourceContext context) : base($"Расширение файла должно быть \"alm\".", context) { }
    }
    public sealed class WrongArrayElementDimension : SemanticError
    {
        public WrongArrayElementDimension(SourceContext context) : base($"Разная размерность массива и элемента массива.", context) { }
    }

    public sealed class ConnotImportThisModule : SyntaxError
    {
        public ConnotImportThisModule(string path, SourceContext context) : base($"Файл \"{path}\" нельзя импортировать.", context) { }
    }

    public sealed class ModuleIsAlreadyImported : SyntaxError
    {
        public ModuleIsAlreadyImported(string path, SourceContext context) : base($"Файл \"{path}\" уже импортирован.", context) { }
    }

    public sealed class IncompatibleReturnType : SemanticError
    {
        public IncompatibleReturnType(string name,InnerType returnType, InnerType expectedReturnType, SourceContext context) : base($"Несовместимый тип возвращаемого значения метода [{name}], ожидался тип [{returnType}], а встречен тип [{expectedReturnType}].", context) { }
    }
    public sealed class IncompatibleConditionType : SemanticError
    {
        public IncompatibleConditionType(SourceContext context) : base($"Несовместимые типы. Любой тип условия должен быть типа boolean.", context) { }
    }
    public sealed class IncompatibleAssignmentType : SemanticError
    {
        public IncompatibleAssignmentType(InnerType adressorType, InnerType adressableType, SourceContext context) : base($"Несовместимые типы в присваивании переменной, ожидался тип {adressableType.ALMRepresentation}, а встеречен тип {adressorType.ALMRepresentation}.", context) { }
    }
    public sealed class IncompatibleMethodParameterType : SemanticError
    {
        public IncompatibleMethodParameterType(string name,InnerType tableParamType, InnerType paramType, SourceContext context) : base($"Несовместимый тип параметра метода [{name}], ожидался тип [{tableParamType}], а встречен тип [{paramType}].", context) { }
    }
    public sealed class OperatorMustBeSituatedInLoop : SemanticError
    {
        public OperatorMustBeSituatedInLoop(string op, SourceContext context) : base($"Оператор [{op}] должен находиться в теле цикла.", context) { }
    }
    public sealed class OperatorWithWrongOperandTypes : SemanticError
    {
        public OperatorWithWrongOperandTypes(string message, SourceContext context) : base($"{message}.", context) { }
    }

    public sealed class NotAllCodePathsReturnValue : SemanticError
    {
        public NotAllCodePathsReturnValue(SourceContext context) : base("Не все пути к коду возвращают значение.", context) { }
    }

    public sealed class IdentifierIsAlreadyDeclared : SemanticError
    {
        public IdentifierIsAlreadyDeclared(string name,SourceContext context) : base($"Переменная [{name}] уже объявлена.", context) { }
    }
    public sealed class IdentifierIsNotDeclared : SemanticError
    {
        public IdentifierIsNotDeclared(string name, SourceContext context) : base($"Переменная [{name}] не объявлена.", context) { }
    }
    public sealed class IdentifierIsNotInitialized : SemanticError
    {
        public IdentifierIsNotInitialized(string name, SourceContext context) : base($"Переменной [{name}] не присвоено значение в данной локальной области.", context) { }
    }

    public sealed class MethodIsNotDeclared : SemanticError
    {
        public MethodIsNotDeclared(string name, SourceContext context) : base($"Метод [{name}] с такими типами параметров не объявлен.", context) { }
    }
    public sealed class MethodIsAlreadyDeclared : SemanticError
    {
        public MethodIsAlreadyDeclared(string name, SourceContext context) : base($"Метод [{name}] с такими типами параметров уже объявлен.", context) { }
    }

    public sealed class IncorrectDimension : SemanticError
    {
        public IncorrectDimension(int dimension,SourceContext context) : base($"Неправильная размерность, ожидалась [{dimension}]", context) { }
    }
    public sealed class ArrayIsNotDeclared : SemanticError
    {
        public ArrayIsNotDeclared(string name,SourceContext context) : base($"Массив [{name}] не объявлен.", context) { }
    }
    public sealed class CannotChangeTheString : SemanticError
    {
        public CannotChangeTheString(SourceContext context) : base($"Нельзя изменять содержимое строки.", context) { }
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
