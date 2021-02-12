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

        private static List<TableIdentifier> GlobalIdentifiers = new List<TableIdentifier>();
        public List<TableIdentifier> Identifiers { get; private set; } = new List<TableIdentifier>();

        public List<TableMethod> Methods { get; private set; } = new List<TableMethod>();
        private static List<TableMethod> BaseMethods = new List<TableMethod>()
        {
            new TableMethod("len",new Int32(),new TableMethodArgument[]{ new TableMethodArgument(new AnyArray()) }),

            //methods for auto cast
            //->int64
            new TableMethod("ToInt64",new Int64(),new TableMethodArgument[]{ new TableMethodArgument(new Int32())}),
            new TableMethod("ToInt64",new Int64(),new TableMethodArgument[]{ new TableMethodArgument(new Char()) }),
            //->int32
            new TableMethod("ToInt32",new Int32(),new TableMethodArgument[]{ new TableMethodArgument(new Char())}),
            //->float
            new TableMethod("ToSingle",new Single(),new TableMethodArgument[]{ new TableMethodArgument(new Int32())}),
            new TableMethod("ToSingle",new Single(),new TableMethodArgument[]{ new TableMethodArgument(new Int64())}),
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
            Table table = new Table(puttedIn, level);
            if (level == 1)
                table.Methods.AddRange(BaseMethods);
            Table.GlobalIdentifiers = new List<TableIdentifier>();
            return table;
        }

        public void AddTable(Table table) => this.ThisContains.Add(table);

        public bool PushMethod(MethodDeclaration methodDeclaration)
        {
            if (!CheckMethod(methodDeclaration.Name, methodDeclaration.GetArgumentsTypes()))
            {
                List<TableMethodArgument> tableArgs = new List<TableMethodArgument>();
                for (int i = 0; i < methodDeclaration.ArgCount; i++)
                    tableArgs.Add(new TableMethodArgument(methodDeclaration.Arguments[i].Type));

                Methods.Add(new TableMethod(methodDeclaration.Name, methodDeclaration.ReturnType, tableArgs.ToArray()));
                return true;
            }
            return false;
        }
        public bool PushIdentifier(IdentifierExpression identifierExpression, bool global = false)
        {
            if (!CheckIdentifier(identifierExpression.Name))
            {
                if (global)
                    Table.GlobalIdentifiers.Add(new TableIdentifier(identifierExpression.Name, identifierExpression.Type,true));
                else
                    this.Identifiers.Add(new TableIdentifier(identifierExpression.Name, identifierExpression.Type));
                return true;
            }
            return false;
        }

        public bool CheckIdentifier(string name)
        {
            foreach (TableIdentifier identifier in Table.GlobalIdentifiers)
                if (identifier.Name == name)
                    return true;

            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (TableIdentifier identifier in table.Identifiers)
                    if (identifier.Name == name)
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
            foreach (TableIdentifier identifier in Table.GlobalIdentifiers)
                if (identifier.Name == name)
                    return identifier;

            for (Table table = this; table != null; table = table.PuttedIn)
                foreach (TableIdentifier identifier in table.Identifiers)
                    if (identifier.Name == name)
                        return identifier;
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
        public int ArgCount { get; private set; }

        public InnerType ReturnType { get; private set; }
        public TableMethodArgument[] Arguments { get; private set; }

        public TableMethod(string name, InnerType returnType, TableMethodArgument[] arguments)
        {
            this.Name = name;
            this.ReturnType = returnType;
            this.Arguments = arguments;
            this.ArgCount = arguments.Length;
        }
    }
    public sealed class TableIdentifier
    {
        public bool IsGlobal { get; private set; }
        public bool InitializedGlobally { get; set; }

        public string Name { get; private set; }
        public InnerType Type { get; private set; }
        public List<EmbeddedStatement> InitializedBlocks { get; set; } = new List<EmbeddedStatement>();

        public TableIdentifier(string name, InnerType type, bool isGlobal = false)
        {
            this.Name = name;
            this.Type = type;
            this.IsGlobal = isGlobal;
        }
    }
    public sealed class TableMethodArgument
    {
        public InnerType Type { get; set; }

        public TableMethodArgument(InnerType type)
        {
            this.Type = type;
        }
    }
}
