using System;
using System.Linq;

using alm.Core.Errors;
using alm.Other.ConsoleStuff;

namespace alm.Core.SyntaxAnalysis
{
    public sealed class AbstractSyntaxTree
    {
        public bool Builded { get; private set; } = false;
        public bool Failure { get; private set; } = false;
        public string Path  { get; private set; }
        public SyntaxTreeNode Root { get; private set; }

        public void BuildTree(string path)
        {
            Path = path;
            Lexer lexer = new Lexer(path);
            lexer.GetTokens();
            Parser parser = new Parser(lexer);
            this.Root = parser.Parse();
            Builded = true;
            if (Diagnostics.SyntaxErrors.Count > 0) Failure = true;
        }
        public void ShowTree()
        {
            if (Builded)
                if (!Failure)
                    ParseAllNodes(Root,"",true);
        }

        private void ParseAllNodes(SyntaxTreeNode node, string indent = "", bool root = false)
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
                    if (n.Nodes.Count >= 1 && n == node.Nodes.Last()) ParseAllNodes(n, indent);
                    else ParseAllNodes(n, indent + "│");
                }
            }
        }
    }
}