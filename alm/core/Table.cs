using System.Collections.Generic;

using alm.Core.InnerTypes;
using alm.Core.FrontEnd.SyntaxAnalysis;

namespace alm.Core.VariableTable
{
    public static class GlobalTable
    {
        public static Table Table { get; set; }
    }

    public sealed class Table
    {
        public int Level { get; private set; }
        public Table PuttedIn { get; private set; }

        public List<Table> ThisContains { get; private set; } = new List<Table>();
        public List<TableFunction> Functions { get; private set; } = new List<TableFunction>();
        public List<TableIdentifier> Identifiers { get; private set; } = new List<TableIdentifier>();

        private static IReadOnlyList<TableFunction> BaseFunctions = new List<TableFunction>()
        {
            new TableFunction("tostr",  new String(), new TableFunctionArgument[] { new TableFunctionArgument(new Int32(),1) }, 1),
            new TableFunction("tostrf", new String(), new TableFunctionArgument[] { new TableFunctionArgument(new Single(), 1) }, 1),
            new TableFunction("tofloat",new Single(), new TableFunctionArgument[]{ new TableFunctionArgument(new String(), 1) }, 1),
            new TableFunction("point",  new Single(), new TableFunctionArgument[]{ new TableFunctionArgument(new Int32(), 1) }, 1),
            new TableFunction("round",  new Int32(), new TableFunctionArgument[]{ new TableFunctionArgument(new Single(), 1) }, 1),
            new TableFunction("toint",  new Int32(), new TableFunctionArgument[]{ new TableFunctionArgument(new String(),1) }, 1),
            new TableFunction("toint32",  new Int32(), new TableFunctionArgument[]{ new TableFunctionArgument(new Int32(),1) }, 1),
            new TableFunction("chartoint32",new Int32(),new TableFunctionArgument[]{ new TableFunctionArgument(new Char(),1) }, 1),

            new TableFunction("len",new Int32(),new TableFunctionArgument[]{ new TableFunctionArgument(new AnyArray(),1) }, 1),
        };

        public Table(Table puttedIn, int level)
        {
            this.PuttedIn = puttedIn;
            this.Level = level;
        }

        public bool PushIdentifier(IdentifierExpression identifierExpression)
        {
            if (!CheckIdentifier(identifierExpression.Name))
            {
                this.Identifiers.Add(new TableIdentifier(identifierExpression.Name, identifierExpression.Type, this.Level));
                return true;
            }
            return false;
        }

        public bool PushFunction(FunctionDeclaration functionDeclaration)
        {
            if (!CheckFunction(functionDeclaration.Name, functionDeclaration.ArgumentCount))
            {
                List<TableFunctionArgument> Args = new List<TableFunctionArgument>();
                for (int i = 0; i < functionDeclaration.Arguments.Nodes.Count; i++)
                    Args.Add(new TableFunctionArgument(((ArgumentDeclaration)functionDeclaration.Arguments.Nodes[i]).Type, i + 1));

                Functions.Add(new TableFunction(functionDeclaration.Name, functionDeclaration.Type, Args.ToArray(), this.Level));
                return true;
            }
            return false;
        }

        public TableFunction FetchFunction(string name,int args)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (TableFunction function in table.Functions)
                    if (function.Name == name)
                        if (function.ArgumentCount == args)
                            return function;
            return null;
        }

        public TableFunction[] FetchAllFunctions(string name)
        {
            List<TableFunction> functions = new List<TableFunction>();
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (TableFunction function in table.Functions)
                    if (function.Name == name)
                        functions.Add(function);
            return functions.Count == 0 ? null: functions.ToArray();
        }

        public TableIdentifier FetchIdentifier(IdentifierExpression identifierExpression)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (TableIdentifier var in table.Identifiers)
                    if (var.Name == identifierExpression.Name) 
                        return var;
            return null;
        }

        public TableIdentifier FetchIdentifier(string name)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (TableIdentifier var in table.Identifiers)
                    if (var.Name == name)
                        return var;
            return null;
        }

        public void SetGlobalInitialization(IdentifierExpression identifierExpression)
        {
            TableIdentifier identifier = FetchIdentifier(identifierExpression);
            if (identifier != null) identifier.IsGloballyInitialized = true;
        }

        public void SetLocalBlockInitialization(IdentifierExpression identifierExpression, Body block)
        {
            TableIdentifier identifier = FetchIdentifier(identifierExpression);
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

        public bool IsInitializedInBlock(IdentifierExpression identifierExpression, Body block)
        {
            TableIdentifier identifier = this.FetchIdentifier(identifierExpression);
            if (identifier.IsGloballyInitialized) return true;
            return identifier.InitializedBlocks.Contains(block) ? true : false;
        }

        public void AddTable(Table table) => this.ThisContains.Add(table);

        public bool CheckFunction(string name, int args)
        {
            foreach (TableFunction function in BaseFunctions)
                if (function.Name == name) 
                    if (function.ArgumentCount == args)
                        return true;
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (TableFunction function in table.Functions)
                    if (function.Name == name)
                        if (function.ArgumentCount == args)
                            return true;
            return false;
        }

        public bool CheckIdentifier(string name)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (TableIdentifier var in table.Identifiers)
                    if (var.Name == name) 
                        return true;
            return false;
        }

        public static Table CreateTable(Table puttedIn, int level)
        {
            // level = 1 принимается как глобалтная таблица
            Table table = new Table(puttedIn, level);
            if (level == 1) 
                table.Functions.AddRange(BaseFunctions);
            return table;
        }

        public static Table CreateTable(Table puttedIn)
        {
            Table table = new Table(puttedIn, puttedIn.Level + 1);
            puttedIn.AddTable(table);
            return table;
        }
    }

    public sealed class TableIdentifier
    {
        public int Level { get; private set; }
        public string Name { get; private set; }

        public InnerType Type { get; private set; }
        public bool IsGloballyInitialized { get; set; } = false;
        public List<Body> InitializedBlocks { get; set; } = new List<Body>();

        public TableIdentifier(string name, InnerType type, int level)
        {
            this.Name = name;
            this.Type = type;
            this.Level = level;
        }
    }

    public sealed class TableFunction
    {
        public string Name { get; private set; }

        public int Level { get; private set; }
        public int ArgumentCount { get; private set; }

        public InnerType Type { get; private set; }
        public TableFunctionArgument[] Arguments { get; private set; }

        public TableFunction(string name, InnerType type, TableFunctionArgument[] arguments, int level)
        {
            this.Name = name;
            this.Type = type;
            this.Level = level;
            this.Arguments = arguments;
            this.ArgumentCount = arguments.Length;
        }
    }

    public sealed class TableFunctionArgument
    {
        public InnerType Type { get; set; }
        public int Position { get; private set; }

        public TableFunctionArgument(InnerType type, int position)
        {
            this.Type = type;
            this.Position = position;
        }
    }
}
