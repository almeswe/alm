using System.Collections.Generic;

using alm.Other.InnerTypes;
using alm.Core.SyntaxAnalysis;

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
            /*new Function("println",new Integer32(),new Argument[]{ new Argument("message",new String(),1) }, 1),
            new Function("print",new Integer32(),new Argument[]{ new Argument("message", new String(),1) }, 1),
            new Function("input",new String(),new Argument[]{ }, 1),*/

            new Function("tostr",new String(),new Argument[]{ new Argument("num", new Integer32(),1) }, 1),
            new Function("tostrf",new String(),new Argument[]{ new Argument("num", new Float(),1) }, 1),
            new Function("tofloat",new Float(),new Argument[]{ new Argument("str", new String(),1) }, 1),
            new Function("point",new Float(),new Argument[]{ new Argument("num", new Integer32(),1) }, 1),
            new Function("round",new Integer32(),new Argument[]{ new Argument("fnum", new Float(),1) }, 1),
            new Function("toint",new Integer32(),new Argument[]{ new Argument("str", new String(),1) }, 1),
        };

        public Table(Table puttedIn, int level)
        {
            this.PuttedIn = puttedIn;
            this.Level = level;
        }

        public bool PushIdentifier(IdentifierExpression identifierExpression)
        {
            if (!CheckIdentifier(identifierExpression))
            {
                this.Identifiers.Add(new Identifier(identifierExpression.Name, identifierExpression.Type, this.Level));
                return true;
            }
            return false;
        }
        public bool PushFunction(FunctionDeclaration functionDeclaration)
        {
            if (!CheckFunction(functionDeclaration))
            {
                List<Argument> Args = new List<Argument>();
                for (int i = 0;i < functionDeclaration.Arguments.Nodes.Count; i++)
                    Args.Add(new Argument(((ArgumentDeclaration)functionDeclaration.Arguments.Nodes[i]).Name,((ArgumentDeclaration)functionDeclaration.Arguments.Nodes[i]).Type,i+1));

                Functions.Add(new Function(functionDeclaration.Name,functionDeclaration.Type,Args.ToArray(),this.Level));
                return true;
            }
            return false;
        }

        public Function FetchFunction(FunctionCall functionCall)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Function Function in table.Functions)
                    if (Function.Name == functionCall.Name) return Function;
            return null;
        }
        public Function FetchFunction(Function functionCall)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Function Function in table.Functions)
                    if (Function.Name == functionCall.Name) return Function;
            return null;
        }
        public Identifier FetchIdentifier(IdentifierExpression identifierExpression)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Identifier var in table.Identifiers)
                    if (var.Name == identifierExpression.Name) return var;
            return null;
        }

        public void SetGlobalInitialization(IdentifierExpression identifierExpression)
        {
            Identifier identifier = FetchIdentifier(identifierExpression);
            if (identifier != null) identifier.IsGloballyInitialized = true;
        }

        public void SetLocalBlockInitialization(IdentifierExpression identifierExpression,Body block)
        {
            Identifier identifier = FetchIdentifier(identifierExpression);
            if (identifier != null)
            {
                if (block == null)
                    identifier.IsGloballyInitialized = true;
                else identifier.InitializedBlocks.Add(block);
            }
        }

        public bool IsGloballyInitialized(IdentifierExpression identifierExpression)
        {
            return this.FetchIdentifier(identifierExpression).IsGloballyInitialized ? true : false;
        }

        public bool IsInitializedInBlock(IdentifierExpression identifierExpression,Body block)
        {
            Identifier identifier = this.FetchIdentifier(identifierExpression);
            if (identifier.IsGloballyInitialized) return true;
            return identifier.InitializedBlocks.Contains(block) ? true : false;
        }

        public void AddTable(Table table) => this.ThisContains.Add(table);

        public bool CheckFunction(FunctionCall functionCall)
        {
            foreach (Function Function in BaseFunctions)
                if (Function.Name == functionCall.Name) return true;
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Function Function in table.Functions)
                    if (Function.Name == functionCall.Name) return true;
            return false;
        }
        public bool CheckFunction(FunctionDeclaration functionDeclaration)
        {
            foreach (Function Function in BaseFunctions)
                if (Function.Name == functionDeclaration.Name) return true;
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Function Function in table.Functions)
                    if (Function.Name == functionDeclaration.Name) return true;
            return false;
        }
        public bool CheckFunction(Function functionCall)
        {
            foreach (Function Function in BaseFunctions)
                if (Function.Name == functionCall.Name) return true;
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Function Function in table.Functions)
                    if (Function.Name == functionCall.Name) return true;
            return false;
        }

        public bool CheckIdentifier(IdentifierExpression identifierExpression)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Identifier var in table.Identifiers)
                    if (var.Name == identifierExpression.Name) return true;
            return false;
        }
        public bool CheckIdentifier(Identifier identifier)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (Identifier var in table.Identifiers)
                    if (var.Name == identifier.Name) return true;
            return false;
        }

        public static Table CreateTable(Table puttedIn, int level)
        {
            // level = 1 принимается как глобалтная таблица
            Table table = new Table(puttedIn, level);
            if (level == 1) table.Functions.AddRange(BaseFunctions);
            return table;
        }
        public static Table CreateTable(Table puttedIn)
        {
            Table table = new Table(puttedIn, puttedIn.Level + 1);
            puttedIn.AddTable(table);
            return table;
        }
    }

    public sealed class Identifier
    {
        public int Level   { get; private set; }
        public string Name { get; private set; }

        public InnerType Type  { get; private set; }
        public bool IsGloballyInitialized   { get; set; } = false;
        public List<Body> InitializedBlocks { get; set; } = new List<Body>();

        public Identifier(string name, InnerType type, int level)
        {
            this.Name  = name;
            this.Type  = type;
            this.Level = level;
        }
    }
    public sealed class Function
    {
        public int Level    { get; private set; }
        public int ArgumentCount { get; private set; }
        public string Name  { get; private set; }

        public InnerType ReturnType      { get; private set; }
        public Argument[] Arguments { get; private set; }

        public Function(string name, InnerType type, Argument[] arguments, int level)
        {
            this.Name       = name;
            this.Level      = level;
            this.Arguments  = arguments;
            this.ReturnType = type;
            this.ArgumentCount = arguments.Length;
        }
    }
    public sealed class Argument
    {
        public InnerType Type { get; set; }
        public int Position { get; private set; } 
        public string Name { get; private set; }

        public Argument(string name, InnerType type, int position)
        {
            this.Type = type;
            this.Name = name;
            this.Position = position;
        }
    }

    public static class GlobalTable
    {
        public static Table Table { get; set; }
    }
}
