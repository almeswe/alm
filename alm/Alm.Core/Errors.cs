using System.Collections.Generic;

using alm.Other.InnerTypes;
using alm.Other.Structs;
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

        public static void ShowErrors()
        {
            ConsoleErrorDrawer drawer = new ConsoleErrorDrawer();
            for (int i = 0; i < SyntaxErrors.Count; i++)
            {
                ColorizedPrintln(SyntaxErrors[i].GetMessage(), System.ConsoleColor.DarkRed);
                drawer.DrawError(SyntaxErrors[i], SyntaxErrors[i].FilePath);
            }
            for (int i = 0; i < SemanticErrors.Count; i++)
            {
                ColorizedPrintln(SemanticErrors[i].GetMessage(), System.ConsoleColor.DarkRed);
                drawer.DrawError(SemanticErrors[i], SemanticErrors[i].FilePath);
            }
        }

        public static void Reset()
        {
            SyntaxAnalysisFailed = false;
            SemanticAnalysisFailed = false;
            SyntaxErrors = new List<SyntaxError>();
            SemanticErrors = new List<SemanticError>();
        }
    }

    public abstract class CompilerError
    {
        protected SourceContext Context;

        public bool HasLocation { get; private set; } = true;
        public Position StartsAt => Context.StartsAt;
        public Position EndsAt   => Context.EndsAt;
        public string FilePath   => Context.FilePath;

        public string Message { get; protected set; }
        public CompilerUnitErrorType UnitErrorType { get; protected set; }
        public virtual string GetMessage()
        {
            if (this.Context == default(SourceContext))
            {
                this.HasLocation = false;
                return $"{Message}";
            }
            return $"{Message} \nВ файле ({FilePath[0]}:\\...\\{System.IO.Path.GetFileName(FilePath)}) {this.StartsAt}";
        }
    }

    public abstract class SemanticError : CompilerError
    {
        public SemanticError(string Message, SourceContext Context)
        {
            base.Message = Message;
            base.Context = Context;
            base.UnitErrorType = CompilerUnitErrorType.SemanticError;
        }
    }

    public abstract class SyntaxError: CompilerError
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
        public MissingLbra(Token Token) : base("Ожидался символ [{]", Token.Context) { }
    }
    public sealed class MissingRbra : SyntaxError
    {
        public MissingRbra(Token Token) : base("Ожидался символ [}]", Token.Context) { }
    }
    public sealed class MissingLpar : SyntaxError
    {
        public MissingLpar(Token Token) : base("Ожидался символ [(]", Token.Context) { }
    }
    public sealed class MissingRpar : SyntaxError
    {
        public MissingRpar(Token Token) : base("Ожидался символ [)]", Token.Context) { }
    }
    public sealed class MissingSemi : SyntaxError
    {
        public MissingSemi(Token Token) : base("Ожидался символ [;]", new SourceContext(new Position(Token.Context.StartsAt.Start+1, Token.Context.StartsAt.End+1, Token.Context.StartsAt.Line),Token.Context.EndsAt)) { }
    }
    public sealed class MissingAssign : SyntaxError
    {
        public MissingAssign(Token Token) : base("Ожидался символ [=]", Token.Context) { }
    }
    public sealed class MissingNameOrValue : SyntaxError
    {
        public MissingNameOrValue(Token Token) : base("Ожидалось название переменной или ее значение", Token.Context) { }
    }
    public sealed class NotAppliableWithDigit : SyntaxError
    {
        public NotAppliableWithDigit(string Operator, Token Token) : base($"Данный оператор [{Operator}] невозможно применить к числу", Token.Context) { }
    }
    public sealed class IdentifierExpected : SyntaxError
    {
        public IdentifierExpected(Token Token) : base("Ожидался идентификатор", Token.Context) { }
    }
    public sealed class TypeExpected : SyntaxError
    {
        public TypeExpected(Token Token) : base("Ожидался тип переменной", Token.Context) { }
    }
    public sealed class ReservedWordExpected : SyntaxError
    {
        public ReservedWordExpected(string Word, Token Token) : base($"Ожидалось ключевое слово [{Word}]", Token.Context) { }
    }
    public sealed class ReservedSymbolExpected : SyntaxError
    {
        public ReservedSymbolExpected(string Symbol, Token Token) : base($"Ожидался символ [{Symbol}]", Token.Context) { }
    }

    public sealed class WrongImport : SyntaxError
    {
        public WrongImport(SourceContext Context) : base($"Попытка импортирования несуществующего файла.", Context) { }
    }

    public sealed class WrongShortImport : SyntaxError
    {
        public WrongShortImport(SourceContext Context) : base($"Попытка импортирования несуществующего файла либо не лежащего в одной папке с запускаемым файлом.", Context) { }
    }

    public sealed class CannotImportThisFile : SyntaxError
    {
        public CannotImportThisFile(string Path, SourceContext Context) : base($"Файл \"{Path}\" нельзя импортировать.", Context) { }
    }

    public sealed class ThisFileAlreadyImported : SyntaxError
    {
        public ThisFileAlreadyImported(string Path,SourceContext Context) : base($"Файл \"{Path}\" уже импортирован.", Context) { }
    }

    public sealed class WrongImportExtension : SyntaxError
    {
        public WrongImportExtension(SourceContext Context) : base($"Расширение файла должно быть \"alm\".", Context) { }
    }

    public sealed class ExpectedCorrectImport : SyntaxError
    {
        public ExpectedCorrectImport(SourceContext Context) : base($"Ожидалось короткое или полное имя файла для импортирования.", Context) { }
    }

    public sealed class OnlyDebug : SyntaxError
    {
        public OnlyDebug(string Message, Token Token) : base(Message, Token.Context) { }
    }

    public sealed class IncompatibleTypes : SemanticError
    {
        public IncompatibleTypes(InnerType ExpectedType,InnerType Type,SourceContext Context) : base($"Несовместимые типы. Попытка приведения {ExpectedType.Representation} к {Type.Representation}",Context) { }
    }
    public sealed class IncompatibleReturnType : SemanticError
    {
        public IncompatibleReturnType(InnerType ExpectedType, InnerType Type, SourceContext Context) : base($"Возвращаемый тип в [return] должен быть {ExpectedType.Representation}, а встречен {Type.Representation}", Context) { }
    }
    public sealed class IncompatibleConditionType : SemanticError
    {
        public IncompatibleConditionType(InnerType Type,SourceContext Context) : base($"Несовместимые типы. Любой тип условия должен быть типа boolean",Context) { }
    }
    public sealed class IncompatibleBinaryExpressionType: SemanticError
    {
        public IncompatibleBinaryExpressionType(InnerType ExpectedType, InnerType Type, SourceContext Context) : base($"Несовместимые типы в бинарной операции, ожидался тип {ExpectedType.Representation}, а встеречен {Type.Representation}", Context) { }
    }
    public sealed class IncompatibleBooleanExpressionType : SemanticError
    {
        public IncompatibleBooleanExpressionType(InnerType ExpectedType, InnerType Type, SourceContext Context) : base($"Несовместимые типы в булевом выражении, ожидался тип {ExpectedType.Representation}, а встеречен {Type.Representation}", Context) { }
    }
    public sealed class IncompatibleAssignmentType : SemanticError
    {
        public IncompatibleAssignmentType(InnerType Type, InnerType ExpectedType, SourceContext Context) : base($"Несовместимые типы в присваивании переменной, ожидался тип {ExpectedType.Representation}, а встеречен {Type.Representation}", Context) { }
    }
    public sealed class IncompatibleArgumentType : SemanticError
    {
        public IncompatibleArgumentType(InnerType Type, InnerType ExpectedType, SourceContext Context) : base($"Несовместимый тип в аргументе функции, ожидался тип {ExpectedType.Representation}, а встеречен {Type.Representation}", Context) { }
    }
    public sealed class NotAllCodePathsReturnValue : SemanticError
    {
        public NotAllCodePathsReturnValue(SourceContext Context) : base("Не все пути к коду возвращают значение.", Context) { }
    }

    public sealed class ThisIdentifierAlreadyDeclared : SemanticError
    {
        public ThisIdentifierAlreadyDeclared(string Name,SourceContext Context) : base($"Переменная [{Name}] уже объявлена.", Context) { }
    }

    public sealed class ThisIdentifierNotDeclared : SemanticError
    {
        public ThisIdentifierNotDeclared(string Name, SourceContext Context) : base($"Переменная [{Name}] не объявлена.", Context) { }
    }

    public sealed class ThisIdentifierNotInitialized : SemanticError
    {
        public ThisIdentifierNotInitialized(string Name, SourceContext Context) : base($"Переменной [{Name}] не присвоено значение.", Context) { }
    }

    public sealed class ThisFunctionNotDeclared : SemanticError
    {
        public ThisFunctionNotDeclared(string Name, SourceContext Context) : base($"Функция с именем [{Name}] не объявлена.", Context) { }
    }

    public sealed class ThisFunctionAlreadyDeclared : SemanticError
    {
        public ThisFunctionAlreadyDeclared(string Name, SourceContext Context) : base($"Функция с именем [{Name}] уже объявлена.", Context) { }
    }

    public sealed class InExexutableFileMainExprected : SemanticError
    {
        public InExexutableFileMainExprected() : base($"В запускаемом файле должен быть метод main.",new SourceContext()) { }
    }

    public sealed class FunctionNotContainsThisNumberOfArguments : SemanticError
    {
        public FunctionNotContainsThisNumberOfArguments(string Name,int CorrectArgumentCount, int ArgumentCount, SourceContext Context) : base($"Функция [{Name}] не содержит такое количество аргументов [{ArgumentCount}], ожидалось [{CorrectArgumentCount}].", Context) { }
    }
    public enum CompilerUnitErrorType
    {
        SyntaxError,
        SemanticError
    }
}
