using System;
using System.Linq;

using alm.Other.ConsoleStuff;

namespace alm.Core.FrontEnd.SyntaxAnalysis
{
    public sealed class AbstractSyntaxTree
    {
        public bool Builded { get; private set; } = false;
        public SyntaxTreeNode Root { get; private set; }

        public void BuildTree(string path)
        {
            Lexer lexer = new Lexer(path);
            Parser parser = new Parser(lexer);
            this.Root = parser.Parse(path);
            this.Builded = true;
        }
        public void ShowTree()
        {
            if (Builded)
                ShowTreeInConsole(Root,"",true);
        }

        private void ShowTreeInConsole(SyntaxTreeNode node, string indent = "", bool root = false)
        {
            //├── └── │

            if (root)
            {
                ConsoleCustomizer.ColorizedPrint(indent + "└──", ConsoleColor.DarkGray);
                ConsoleCustomizer.ColorizedPrintln(node.ToConsoleString(), node.Color);
            }

            indent += "   ";
            foreach (SyntaxTreeNode n in node.Nodes)
            {
                if (n != null)
                {
                    if (n == node.Nodes.Last()) ConsoleCustomizer.ColorizedPrint(indent + "└──", ConsoleColor.DarkGray);
                    else ConsoleCustomizer.ColorizedPrint(indent + "├──", ConsoleColor.DarkGray);
                    ConsoleCustomizer.ColorizedPrintln(n.ToConsoleString(), n.Color);
                    if (n.Nodes.Count >= 1 && n == node.Nodes.Last()) ShowTreeInConsole(n, indent);
                    else ShowTreeInConsole(n, indent + "│");
                }
            }
        }
    }
}
