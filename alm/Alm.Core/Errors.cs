using System.Collections.Generic;

using alm.Other.Structs;
using alm.Other.InnerTypes;
using alm.Other.ConsoleStuff;

using static alm.Other.ConsoleStuff.ConsoleCustomizer;

namespace alm.Core.Errors
{
    public static class Diagnostics
    {
        public static bool SyntaxAnalysisFailed   { get { return SyntaxErrors.Count > 0 ?   true : false; } private set { } }
        public static bool SemanticAnalysisFailed { get { return SemanticErrors.Count > 0 ? true : false; } private set { } }

        public static List<SyntaxError>   SyntaxErrors   { get; set; } = new List<SyntaxError>();
        public static List<SemanticError> SemanticErrors { get; set; } = new List<SemanticError>();

        public static void ShowErrorsInConsole()
        {
            ConsoleErrorDrawer drawer = new ConsoleErrorDrawer();
            for (int i = 0; i < SyntaxErrors.Count; i++)
            {
                ColorizedPrintln(SyntaxErrors[i].GetMessage(), System.ConsoleColor.DarkRed);
                drawer.DrawError(SyntaxErrors[i]);
            }
            for (int i = 0; i < SemanticErrors.Count; i++)
            {
                ColorizedPrintln(SemanticErrors[i].GetMessage(), System.ConsoleColor.DarkRed);
                drawer.DrawError(SemanticErrors[i]);
            }
        }

        public static void Reset()
        {
            SyntaxAnalysisFailed   = false;
            SemanticAnalysisFailed = false;
            SyntaxErrors = new List<SyntaxError>();
            SemanticErrors = new List<SemanticError>();
        }
    }

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
            return $"{Message} \nВ файле ({FilePath[0]}:\\...\\{System.IO.Path.GetFileName(FilePath)}) {this.StartsAt}";
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

    public abstract class SyntaxError : CompilerError
    {
        public SyntaxError(string Message, SourceContext Context)
        {
            base.Message = Message;
            base.Context = Context;
            base.UnitErrorType = CompilerUnitErrorType.SyntaxError;
        }
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

    public sealed class IdentifierExpected : SyntaxError
    {
        public IdentifierExpected(Token token) : base("Ожидался идентификатор.", token.Context) { }
    }

    public sealed class TypeExpected : SyntaxError
    {
        public TypeExpected(Token token) : base("Ожидался тип переменной.", token.Context) { }
    }

    public sealed class ReservedWordExpected : SyntaxError
    {
        public ReservedWordExpected(string word, Token token) : base($"Ожидалось ключевое слово [{word}].", token.Context) { }
    }

    public sealed class ReservedSymbolExpected : SyntaxError
    {
        public ReservedSymbolExpected(string symbol, Token token) : base($"Ожидался символ [{symbol}].", token.Context) { }
    }

    public sealed class WrongImport : SyntaxError
    {
        public WrongImport(SourceContext context) : base($"Попытка импортирования несуществующего файла.", context) { }
    }

    public sealed class WrongShortImport : SyntaxError
    {
        public WrongShortImport(SourceContext context) : base($"Попытка импортирования несуществующего файла либо не лежащего в одной папке с запускаемым файлом.", context) { }
    }

    public sealed class CannotImportThisFile : SyntaxError
    {
        public CannotImportThisFile(string path, SourceContext context) : base($"Файл \"{path}\" нельзя импортировать.", context) { }
    }

    public sealed class ThisFileAlreadyImported : SyntaxError
    {
        public ThisFileAlreadyImported(string path, SourceContext context) : base($"Файл \"{path}\" уже импортирован.", context) { }
    }

    public sealed class WrongImportExtension : SyntaxError
    {
        public WrongImportExtension(SourceContext context) : base($"Расширение файла должно быть \"alm\".", context) { }
    }

    public sealed class ExpectedCorrectImport : SyntaxError
    {
        public ExpectedCorrectImport(SourceContext context) : base($"Ожидалось короткое или полное имя файла для импортирования.", context) { }
    }

    public sealed class ErrorMessage : SyntaxError
    {
        public ErrorMessage(string message, Token token) : base($"{message}.", token.Context) { }
    }

    public sealed class IncompatibleReturnType : SemanticError
    {
        public IncompatibleReturnType(InnerType expectedType, InnerType type, SourceContext context) : base($"Возвращаемый тип в [return] должен быть {expectedType.Representation}, а встречен {type.Representation}.", context) { }
    }

    public sealed class IncompatibleConditionType : SemanticError
    {
        public IncompatibleConditionType(SourceContext context) : base($"Несовместимые типы. Любой тип условия должен быть типа boolean.", context) { }
    }

    public sealed class IncompatibleBinaryExpressionType: SemanticError
    {
        public IncompatibleBinaryExpressionType(InnerType type, SourceContext context) : base($"Несовместимые типы в бинарной операции, ожидался числовой тип, а встеречен {type.Representation}.", context) { }
        public IncompatibleBinaryExpressionType(InnerType expectedType, InnerType type, SourceContext context) : base($"Несовместимые типы в бинарной операции, ожидался тип {expectedType.Representation}, а встеречен {type.Representation}.", context) { }
    }

    public sealed class IncompatibleBooleanExpressionType : SemanticError
    {
        public IncompatibleBooleanExpressionType(InnerType type, SourceContext context) : base($"Несовместимые типы в операции сравнения, ожидался числовой тип, а встеречен {type.Representation}.", context) { }
        public IncompatibleBooleanExpressionType(InnerType expectedType, InnerType type, SourceContext context) : base($"Несовместимые типы в булевом выражении, ожидался тип {expectedType.Representation}, а встеречен {type.Representation}.", context) { }
    }

    public sealed class IncompatibleAssignmentType : SemanticError
    {
        public IncompatibleAssignmentType(InnerType type, InnerType expectedType, SourceContext context) : base($"Несовместимые типы в присваивании переменной, ожидался тип {expectedType.Representation}, а встеречен {type.Representation}.", context) { }
    }

    public sealed class IncompatibleArgumentType : SemanticError
    {
        public IncompatibleArgumentType(InnerType type, InnerType expectedType, SourceContext context) : base($"Несовместимый тип в аргументе функции, ожидался тип {expectedType.Representation}, а встеречен {type.Representation}.", context) { }
    }

    public sealed class NotAllCodePathsReturnValue : SemanticError
    {
        public NotAllCodePathsReturnValue(SourceContext context) : base("Не все пути к коду возвращают значение.", context) { }
    }

    public sealed class ThisIdentifierAlreadyDeclared : SemanticError
    {
        public ThisIdentifierAlreadyDeclared(string name,SourceContext context) : base($"Переменная [{name}] уже объявлена.", context) { }
    }

    public sealed class ThisIdentifierNotDeclared : SemanticError
    {
        public ThisIdentifierNotDeclared(string name, SourceContext context) : base($"Переменная [{name}] не объявлена.", context) { }
    }

    public sealed class ThisIdentifierNotInitialized : SemanticError
    {
        public ThisIdentifierNotInitialized(string name, SourceContext context) : base($"Переменной [{name}] не присвоено значение.", context) { }
    }

    public sealed class ThisFunctionNotDeclared : SemanticError
    {
        public ThisFunctionNotDeclared(string name, SourceContext context) : base($"Функция [{name}] с такими типами параметров не объявлена.", context) { }
    }

    public sealed class ThisFunctionAlreadyDeclared : SemanticError
    {
        public ThisFunctionAlreadyDeclared(string name, SourceContext context) : base($"Функция [{name}] с таким количеством параметров уже объявлена.", context) { }
    }

    public sealed class ElementNotFromThisDimension : SemanticError
    {
        public ElementNotFromThisDimension(SourceContext context) : base($"Попытка получения элемента массива который не соответствует его размерности.", context) { }
    }

    public sealed class ArrayDoesNotExist : SemanticError
    {
        public ArrayDoesNotExist(string name,SourceContext context) : base($"Массив [{name}] с таким именем не объявлен.", context) { }
    }

    public sealed class InExexutableFileMainExprected : SemanticError
    {
        public InExexutableFileMainExprected() : base($"В запускаемом файле должен быть метод main.",new SourceContext()) { }
    }

    public sealed class FunctionNotContainsThisNumberOfArguments : SemanticError
    {
        public FunctionNotContainsThisNumberOfArguments(string name, int actualArgumentCount, int argumentCount, SourceContext context) : base($"Функция [{name}] не содержит такое количество аргументов [{argumentCount}], ожидалось [{actualArgumentCount}].", context) { }
    }

    public enum CompilerUnitErrorType
    {
        ScanError,
        SyntaxError,
        SemanticError
    }
}
