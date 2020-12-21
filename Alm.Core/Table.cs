using System.Collections.Generic;

using alm.Other.InnerTypes;
using alm.Core.SyntaxAnalysis;

using static alm.Other.ConsoleStuff.ConsoleCustomizer;

namespace alm.Core.VariableTable
{
    public sealed class Table
    {
        public Table PuttedIn { get; private set; }
        public int Level      { get; private set; }

        public List<Table>      ThisContains { get; private set; } = new List<Table>();

        public List<Function>   Functions   { get; private set; } = new List<Function>();
        public List<Identifier> Identifiers { get; private set; } = new List<Identifier>();

        private static IReadOnlyList<Function> BaseFunctions = new List<Function>()
        {
            new Function("println",new Integer32(),new Argument[]{ new Argument("message",new String(),1) }, 1),
            new Function("print",new Integer32(),new Argument[]{ new Argument("message", new String(),1) }, 1),
            new Function("tostr",new String(),new Argument[]{ new Argument("num", new Integer32(),1) }, 1),
            new Function("toint",new Integer32(),new Argument[]{ new Argument("str", new String(),1) }, 1),
            new Function("input",new String(),new Argument[]{ }, 1),
        };

        public Table(Table PuttedIn, int Level)
        {
            this.PuttedIn = PuttedIn;
            this.Level = Level;
        }

        public bool PushIdentifier(IdentifierExpression id)
        {
            if (!CheckIdentifier(id))
            {
                this.Identifiers.Add(new Identifier(id.Name, id.Type, this.Level));
                return true;
            }
            return false;
        }
        public bool PushFunction(FunctionDeclaration func)
        {
            if (!CheckFunction(func))
            {
                List<Argument> Args = new List<Argument>();
                for (int i = 0;i < func.Arguments.Nodes.Count; i++)
                    Args.Add(new Argument(((ArgumentDeclaration)func.Arguments.Nodes[i]).Name,((ArgumentDeclaration)func.Arguments.Nodes[i]).Type,i+1));

                Functions.Add(new Function(func.Name,func.Type,Args.ToArray(),this.Level));
                return true;
            }
            return false;
        }

        public Function FetchFunction(FunctionCall func)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Function Function in table.Functions)
                    if (Function.Name == func.Name) return Function;
            return null;
        }
        public Function FetchFunction(Function func)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Function Function in table.Functions)
                    if (Function.Name == func.Name) return Function;
            return null;
        }
        public Identifier FetchIdentifier(IdentifierExpression id)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Identifier var in table.Identifiers)
                    if (var.Name == id.Name) return var;
            return null;
        }

        public void SetGlobalInitialization(IdentifierExpression id)
        {
            Identifier identifier = FetchIdentifier(id);
            if (identifier != null) identifier.IsGloballyInitialized = true;
        }

        public void SetLocalBlockInitialization(IdentifierExpression id,Body Block)
        {
            Identifier identifier = FetchIdentifier(id);
            if (identifier != null) identifier.InitializedBlocks.Add(Block);
        }

        public bool IsGloballyInitialized(IdentifierExpression id)
        {
            return this.FetchIdentifier(id).IsGloballyInitialized ? true : false;
        }

        public bool IsInitializedInBlock(IdentifierExpression id,Body Block)
        {
            Identifier identifier = this.FetchIdentifier(id);
            if (identifier.IsGloballyInitialized) return true;
            return identifier.InitializedBlocks.Contains(Block) ? true : false;
        }

        public void GetInfoAbout(Function func)
        {
            Function function = FetchFunction(func);

            if (function is null)
            {
                ColorizedPrintln($"Нет информации о функции {func.Name}", System.ConsoleColor.DarkRed);
                return;
            }

            ColorizedPrint("Имя:                 ", System.ConsoleColor.Yellow); ColorizedPrintln(function.Name, System.ConsoleColor.Green);
            ColorizedPrint("Тип:                 ", System.ConsoleColor.Yellow); ColorizedPrintln(function.ReturnType.Representation, System.ConsoleColor.DarkYellow);
            ColorizedPrintln($"Аргументы ({function.ArgumentCount}):        ", System.ConsoleColor.Yellow);
            foreach (Argument arg in function.Arguments)
            {
                ColorizedPrint($"    Аргумент:        ", System.ConsoleColor.DarkCyan);ColorizedPrintln(arg.Position.ToString(),System.ConsoleColor.Green);
                ColorizedPrint("     Имя:            ",  System.ConsoleColor.Blue);ColorizedPrintln(arg.Name, System.ConsoleColor.Green);
                ColorizedPrint("     Тип:            ",  System.ConsoleColor.Blue);ColorizedPrintln(arg.Type.Representation, System.ConsoleColor.DarkYellow);
                System.Console.WriteLine(string.Empty);
            }
            ColorizedPrint("Уровень вложенности: ", System.ConsoleColor.Yellow); ColorizedPrintln(function.Level.ToString(), System.ConsoleColor.Green);

            System.Console.WriteLine(string.Empty);
        }
        public void GetInfoAbout(IdentifierExpression id)
        {
            Identifier identifier = FetchIdentifier(id);

            if (identifier is null)
            {
                ColorizedPrintln($"Нет информации о переменной {id.Name}", System.ConsoleColor.DarkRed);
                return;
            }

            ColorizedPrint("Имя:                 ", System.ConsoleColor.Yellow); ColorizedPrintln(identifier.Name, System.ConsoleColor.Green);
            ColorizedPrint("Тип:                 ", System.ConsoleColor.Yellow); ColorizedPrintln(identifier.Type.Representation, System.ConsoleColor.DarkYellow);
            ColorizedPrint("Инициализирована:    ", System.ConsoleColor.Yellow); ColorizedPrintln(identifier.IsGloballyInitialized ? "Да" : "Нет", identifier.IsGloballyInitialized ? System.ConsoleColor.Green : System.ConsoleColor.Red);
            ColorizedPrint("Уровень вложенности: ", System.ConsoleColor.Yellow); ColorizedPrintln(identifier.Level.ToString(), System.ConsoleColor.Green);

            System.Console.WriteLine(string.Empty);
        }
        public void GetInfoAbout(Identifier id)
        {
            if (!CheckIdentifier(id))
            {
                ColorizedPrintln($"Нет информации о переменной {id.Name}", System.ConsoleColor.DarkRed);
                return;
            }

            ColorizedPrint("Имя:                 ", System.ConsoleColor.Yellow); ColorizedPrintln(id.Name, System.ConsoleColor.Green);
            ColorizedPrint("Тип:                 ", System.ConsoleColor.Yellow); ColorizedPrintln(id.Type.Representation, System.ConsoleColor.DarkYellow);
            ColorizedPrint("Инициализирована:    ", System.ConsoleColor.Yellow); ColorizedPrintln(id.IsGloballyInitialized ? "Да" : "Нет", id.IsGloballyInitialized ? System.ConsoleColor.Green : System.ConsoleColor.Red);
            ColorizedPrint("Уровень вложенности: ", System.ConsoleColor.Yellow); ColorizedPrintln(id.Level.ToString(), System.ConsoleColor.Green);

            System.Console.WriteLine(string.Empty);
        }

        public void AddTable(Table Table) => this.ThisContains.Add(Table);
        public void ShowTable()
        {
            foreach (Identifier var in this.Identifiers) this.GetInfoAbout(var);
            foreach (Function func in this.Functions)    this.GetInfoAbout(func);
            foreach (Table table in this.ThisContains) table.ShowTable();
        }

        public bool CheckFunction(FunctionCall func)
        {
            foreach (Function Function in BaseFunctions)
                if (Function.Name == func.Name) return true;
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Function Function in table.Functions)
                    if (Function.Name == func.Name) return true;
            return false;
        }
        public bool CheckFunction(FunctionDeclaration func)
        {
            foreach (Function Function in BaseFunctions)
                if (Function.Name == func.Name) return true;
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Function Function in table.Functions)
                    if (Function.Name == func.Name) return true;
            return false;
        }
        public bool CheckFunction(Function func)
        {
            foreach (Function Function in BaseFunctions)
                if (Function.Name == func.Name) return true;
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Function Function in table.Functions)
                    if (Function.Name == func.Name) return true;
            return false;
        }

        public bool CheckIdentifier(IdentifierExpression id)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Identifier var in table.Identifiers)
                    if (var.Name == id.Name) return true;
            return false;
        }
        public bool CheckIdentifier(Identifier id)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Identifier var in table.Identifiers)
                    if (var.Name == id.Name) return true;
            return false;
        }

        public static Table CreateTable(Table PuttedIn, int Level)
        {
            // level = 1 принимается как глобалтная таблица
            Table table = new Table(PuttedIn, Level);
            if (Level == 1) table.Functions.AddRange(BaseFunctions);
            return table;
        }
        public static Table CreateTable(Table PuttedIn)
        {
            Table table = new Table(PuttedIn, PuttedIn.Level + 1);
            PuttedIn.AddTable(table);
            return table;
        }
    }

    public sealed class Identifier
    {
        public int Level   { get; private set; }
        public int References { get; set; } //?
        public string Name { get; private set; }

        public InnerType Type  { get; private set; }
        public bool IsGloballyInitialized   { get; set; } = false;
        public List<Body> InitializedBlocks { get; set; } = new List<Body>();

        public Identifier(string Name, InnerType Type, int Level)
        {
            this.Name  = Name;
            this.Type  = Type;
            this.Level = Level;
        }
    }
    public sealed class Function
    {
        public int Level    { get; private set; }
        public int ArgumentCount { get; private set; }

        public string Name  { get; private set; }

        public InnerType ReturnType      { get; private set; }
        public Argument[] Arguments { get; private set; }

        public Function(string Name, InnerType ReturnType, Argument[] Arguments, int Level)
        {
            this.Name       = Name;
            this.Level      = Level;
            this.Arguments  = Arguments;
            this.ReturnType = ReturnType;
            this.ArgumentCount = Arguments.Length;
        }
    }
    public sealed class Argument
    {
        public InnerType Type { get; set; }
        public int Position { get; private set; } // Позиция по счету, хз нужна ли будет
        public string Name { get; private set; }

        public Argument(string Name, InnerType Type, int Position)
        {
            this.Type = Type;
            this.Name = Name;
            this.Position = Position;
        }
    }

    public static class GlobalTable
    {
        public static Table Table { get; set; }
    }
}
