using System.Collections.Generic;

using alm.Core.InnerTypes;
using alm.Core.SyntaxTree;

namespace alm.Core.Table
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
        public List<TableMethod> Methods { get; private set; } = new List<TableMethod>();
        public List<TableIdentifier> Identifiers { get; private set; } = new List<TableIdentifier>();

        private static List<TableMethod> BaseMethods = new List<TableMethod>()
        {
            new TableMethod("len",new Int32(),new TableMethodArgument[]{ new TableMethodArgument(new AnyArray(),1) }, 1),
        };

        public Table(Table puttedIn, int level)
        {
            this.PuttedIn = puttedIn;
            this.Level = level;
        }

        public static Table CreateTable(Table puttedIn)
        {
            Table table = new Table(puttedIn, puttedIn.Level + 1);
            puttedIn.AddTable(table);
            return table;
        }
        public static Table CreateTable(Table puttedIn, int level)
        {
            // level = 1 принимается как глобалтная таблица
            Table table = new Table(puttedIn, level);
            if (level == 1)
                table.Methods.AddRange(BaseMethods);
            return table;
        }

        public void AddTable(Table table) => this.ThisContains.Add(table);

        public bool PushMethod(MethodDeclaration methodDeclaration)
        {
            if (!CheckMethod(methodDeclaration.Name, methodDeclaration.GetArgumentsTypes()))
            {
                List<TableMethodArgument> tableArgs = new List<TableMethodArgument>();
                for (int i = 0; i < methodDeclaration.ArgCount; i++)
                    tableArgs.Add(new TableMethodArgument(methodDeclaration.Arguments[i].Type, i));

                Methods.Add(new TableMethod(methodDeclaration.Name, methodDeclaration.ReturnType, tableArgs.ToArray(), this.Level));
                return true;
            }
            return false;
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

        public bool CheckIdentifier(string name)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (TableIdentifier var in table.Identifiers)
                    if (var.Name == name)
                        return true;
            return false;
        }
        public bool CheckMethod(string name, InnerType[] arguments, bool withConvertableTypes = false)
        {
            bool foundWithConvertableTypes = false;
            foreach (TableMethod method in BaseMethods)
                if (method.Name == name)
                    if (TypesAreSame(arguments, method.Arguments))
                        return true;
                    else
                        if (withConvertableTypes)
                            if (TypesAreConvertable(arguments, method.Arguments))
                                foundWithConvertableTypes = true;

            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (TableMethod method in table.Methods)
                    if (method.Name == name)
                        if (TypesAreSame(arguments, method.Arguments))
                            return true;
                        else
                            if (withConvertableTypes)
                                if (TypesAreConvertable(arguments, method.Arguments))
                                    foundWithConvertableTypes = true;

            return foundWithConvertableTypes;
        }

        public TableIdentifier FetchIdentifier(string name)
        {
            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (TableIdentifier var in table.Identifiers)
                    if (var.Name == name)
                        return var;
            return null;
        }
        public TableMethod FetchMethod(string name, InnerType[] arguments)
        {
            TableMethod methodWithConvertableTypes = null;

            foreach (TableMethod method in BaseMethods)
                if (method.Name == name && TypesAreSame(arguments, method.Arguments))
                    return method;
                else
                    if (TypesAreConvertable(arguments, method.Arguments))
                        methodWithConvertableTypes = method;

            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (TableMethod method in table.Methods)
                    if (method.Name == name)
                        if (TypesAreSame(arguments, method.Arguments))
                            return method;
                        else
                            if (TypesAreConvertable(arguments, method.Arguments))
                                methodWithConvertableTypes = method;

            return methodWithConvertableTypes;
        }

        public bool IsMethodWithThisNameDeclared(string name)
        {
            List<TableMethod> tableMethods = this.Methods;
            tableMethods.AddRange(Table.BaseMethods);

            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (TableMethod method in table.Methods)
                    if (method.Name == name)
                            return true;

            return false; 
        }

        private bool TypesAreSame(InnerType[] types, TableMethodArgument[] arguments)
        {
            if (types.Length != arguments.Length)
                return false;
            for (int i = 0; i < types.Length; i++)
                if (types[i] != arguments[i].Type)
                        return false;
            return true;
        }
        private bool TypesAreConvertable(InnerType[] types, TableMethodArgument[] arguments)
        {
            if (types.Length != arguments.Length)
                return false;
            for (int i = 0; i < types.Length; i++)
                if (types[i] != arguments[i].Type)
                {
                    if (types[i] is NumericType && arguments[i].Type is NumericType)
                    { 
                        if (!((NumericType)types[i]).CanCast((NumericType)arguments[i].Type))
                            return false;
                    }
                    else
                        return false;
                }
            return true;
        }

        public void InitializeInBlock(IdentifierExpression identifierExpression, EmbeddedStatement block)
        {
            TableIdentifier identifier = FetchIdentifier(identifierExpression.Name);
            if (identifier != null)
                identifier.InitializedBlocks.Add(block);
        }
        public bool IsInitializedInBlock(IdentifierExpression identifierExpression, EmbeddedStatement block)
        {
            for (SyntaxTreeNode thisBlock = block; thisBlock != null; thisBlock = thisBlock.GetParentByType(typeof(EmbeddedStatement)))
                if (this.FetchIdentifier(identifierExpression.Name).InitializedBlocks.Contains((EmbeddedStatement)thisBlock))
                    return true;
            return false;
        }

    }

    public sealed class TableMethod
    {
        public string Name { get; private set; }

        public int Level { get; private set; }
        public int ArgCount { get; private set; }

        public InnerType ReturnType { get; private set; }
        public TableMethodArgument[] Arguments { get; private set; }

        public TableMethod(string name, InnerType type, TableMethodArgument[] arguments, int level)
        {
            this.Name = name;
            this.ReturnType = type;
            this.Level = level;
            this.Arguments = arguments;
            this.ArgCount = arguments.Length;
        }
    }
    public sealed class TableIdentifier
    {
        public int Level { get; private set; }
        public string Name { get; private set; }

        public InnerType Type { get; private set; }
        public List<EmbeddedStatement> InitializedBlocks { get; set; } = new List<EmbeddedStatement>();

        public TableIdentifier(string name, InnerType type, int level)
        {
            this.Name = name;
            this.Type = type;
            this.Level = level;
        }
    }
    public sealed class TableMethodArgument
    {
        public InnerType Type { get; set; }
        public int Position { get; private set; }

        public TableMethodArgument(InnerType type, int position)
        {
            this.Type = type;
            this.Position = position;
        }
    }
}
