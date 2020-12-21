using System;
using System.IO;

using alm.Core.SyntaxAnalysis;
using alm.Core.SemanticAnalysis;

using alm.Core.VariableTable;
using alm.Core.CodeGeneration.Emitter;
using static alm.Other.String.StringMethods;
using static alm.Other.ConsoleStuff.ConsoleCustomizer;

namespace alm.Core.Shell
{
    public sealed class CompilerShell
    {
        string input = string.Empty;

        public void Run()
        {
            #if DEBUG
            ShellOptions.SourceFile = @"C:\Users\Almes\source\repos\Compiler\compiler v.5\Test Scripts\AlmDebug.alm";
            if (!File.Exists(ShellOptions.SourceFile)) File.Create(ShellOptions.SourceFile);
            #endif
            #if !DEBUG
            ColorizedPrint("alm Compiler Shell 2020.",ConsoleColor.Green);
            Console.WriteLine();
            Console.WriteLine();
            #endif

            ShellOptions.InitStandartOptions(ShellOptions.SourceFile);

            while (true)
            {
                ColorizedPrint(">", ConsoleColor.Green);
                input = Console.ReadLine();
                ParseInput(input);
                //Console.WriteLine(Console.CursorLeft);
            }
        }
        private void ParseInput(string Input)
        {
            string[] splitted = SplitSubstrings(Input);
            bool found = false;

            int i = 0;
            while (i < splitted.Length)
            {
                foreach (var flag in ShellOptions.ShellFlags)
                {
                    if (splitted[i] == flag.Flag)
                    {
                        found = true;
                        if (i + 1 < splitted.Length)
                            flag.ExecuteFlag(splitted[i+1]);
                        break;
                    }
                }
                i++;
            }

            i = 0;
            while (i < splitted.Length)
            {
                foreach (var command in ShellOptions.ShellCommands)
                {
                    if (splitted[i] == command.Command)
                    {
                        found = true;
                        if (splitted[i] == "fl")
                            if (i + 1 < splitted.Length)
                            {
                                command.Argument = splitted[i + 1];
                                i++;
                            }
                        command.Execute();
                        break;
                    }
                }
                if (!found)
                {
                    ColorizedPrintln("[ShellError]: Unknown command or flag",ConsoleColor.DarkRed);
                    break;
                }

                i++;
            }
        }
    }

    public abstract class ShellCommand
    {
        public abstract string Command { get; }
        public abstract ShellCommandFlag[] Flags { get; }
        public string Argument { get; set; }


        public virtual bool IsThatCommand(string Command) => Command == this.Command ? true : false;

        public virtual void Execute() => Console.Clear();
        public virtual void ShowFlags()
        {
            foreach (var Flag in Flags)
                ColorizedPrintln("   " + Flag.Representation(),ConsoleColor.Blue);
        }
    }

    public interface ShellCommandFlag
    {
        string Flag { get; }
        string Value{ get; }

        void ExecuteFlag(string Argument);
        string Representation();
    }

    internal sealed class ClearConsole: ShellCommand
    {
        public override string Command => "cls";

        public override ShellCommandFlag[] Flags => null;

        public override void Execute()   => Console.Clear();
        public override void ShowFlags() => ColorizedPrintln("   # empty", ConsoleColor.Blue);
    }
    internal sealed class Recompile : ShellCommand
    {
        public override string Command => "rec";
        public ShellCommandFlag SourceFlag   = new Source();
        public ShellCommandFlag ShowTreeFlag = new ShowTree();

        public override ShellCommandFlag[] Flags => new ShellCommandFlag[]
        {
            SourceFlag,
            ShowTreeFlag
        };

        public override void Execute()
        {
            if (File.Exists(ShellOptions.SourceFile))
            {
                if (Path.GetExtension(ShellOptions.SourceFile) == ".alm")
                {
                    Errors.Diagnostics.Reset();

                    GlobalTable.Table = Table.CreateTable(null, 1);

                    AbstractSyntaxTree ast = new AbstractSyntaxTree();
                    ast.BuildTree(ShellOptions.SourceFile);

                    if (!Errors.Diagnostics.SyntaxAnalysisFailed)
                    {
                        LabelChecker.ResolveProgram(ast);
                        if (!Errors.Diagnostics.SemanticAnalysisFailed)
                        {
                            TypeChecker.ResolveTypes(ast);
                            if (!Errors.Diagnostics.SemanticAnalysisFailed)
                                if (ShellOptions.ShowTree) ast.ShowTree();
                        }
                    }

                    Errors.Diagnostics.ShowErrors();

                    //Emit
                    if (!Errors.Diagnostics.SyntaxAnalysisFailed && !Errors.Diagnostics.SemanticAnalysisFailed)
                    {
                        Emitter.LoadBootstrapper("123", "123");
                        Emitter.EmitAST(ast);
                        Emitter.Reset();
                    }
                    //
                }
                else ColorizedPrintln("Extension must be \".alm\".", ConsoleColor.DarkRed);
            }
            else ColorizedPrintln("File doesn't exist", ConsoleColor.DarkRed);
        }
    }

    internal sealed class OpenSourceFile : ShellCommand
    {
        public override string Command => "open";
        public ShellCommandFlag SourceFlag = new Source();

        public override ShellCommandFlag[] Flags => new ShellCommandFlag[]
        {
            SourceFlag,
        };

        public override void Execute()
        {
            if (File.Exists(ShellOptions.SourceFile))
            {
                try
                {
                    System.Diagnostics.Process.Start(ShellOptions.SourceFile);
                }
                catch { ColorizedPrintln("Error opening file", ConsoleColor.DarkRed); }
            }
            else ColorizedPrintln("File doesn't exist", ConsoleColor.DarkRed);
        }
    }

    internal sealed class ExitShell : ShellCommand
    {
        public override string Command => "exit";

        public override ShellCommandFlag[] Flags => null;
        public override void Execute() => System.Diagnostics.Process.GetCurrentProcess().Kill();
        public override void ShowFlags() => ColorizedPrintln("   # empty", ConsoleColor.Blue);
    }

    internal sealed class ShowCommandFlags : ShellCommand
    {
        public override string Command           => "fl";
        public override ShellCommandFlag[] Flags => null;

        public override void Execute()
        {
            if (this.Argument is null)
            {
                ColorizedPrintln("[flags] [command] ", ConsoleColor.DarkRed);
                return;
            }
            foreach (var command in ShellOptions.ShellCommands)
            {
                if (command.IsThatCommand(Argument))
                {
                    command.ShowFlags();
                    return;
                }
            }
            ColorizedPrintln("[flags] [command] ", ConsoleColor.DarkRed);
        }
        public override void ShowFlags() => ColorizedPrintln("   # empty", ConsoleColor.Green);
    }

    internal sealed class PreviousFilePath : ShellCommand
    {
        public string Value => ShellOptions.PreviousSourceFile;

        public override string Command => $"prevc";

        public override ShellCommandFlag[] Flags => null;

        public override void Execute() => ColorizedPrintln("   "+Value,ConsoleColor.Blue);
        public override void ShowFlags() => ColorizedPrintln("   # empty", ConsoleColor.Blue);
    }

    internal sealed class Source : ShellCommandFlag
    {
        public string SourcePath => ShellOptions.SourceFile;

        public string Flag => $"src";

        public string Value => SourcePath;

        public void ExecuteFlag(string Argument)
        {
            if (File.Exists(SubstractSymbol(Argument, '"')))
            {
                ShellOptions.PreviousSourceFile = ShellOptions.SourceFile;
                ShellOptions.SourceFile = SubstractSymbol(Argument, '"');
            }
            else ColorizedPrintln($"   Bad argument in [src] flag ({Argument}), this file doesn't exist (Mark: the file path must be quoted)", ConsoleColor.DarkRed);
        }

        public string Representation() => $"# {Flag} -> {Value}";
    }



    internal sealed class ShowTree : ShellCommandFlag
    {
        public string Flag => $"tree";

        public string Value => ShellOptions.ShowTree ? "on" : "off";

        public void ExecuteFlag(string Argument)
        {
            if (Argument == "on")       ShellOptions.ShowTree = true;
            else if (Argument == "off") ShellOptions.ShowTree = false;
            else ColorizedPrintln($"   Bad argument in [tree] flag ({Argument})",ConsoleColor.DarkRed);
        }

        public string Representation() => $"# {Flag} -> {Value}";
    }


    public sealed class ShellOptions
    {
        public static ShellCommand[] ShellCommands { get; private set; } = new ShellCommand[] { new Recompile(), 
                                                                                                new ClearConsole(), 
                                                                                                new ShowCommandFlags(),
                                                                                                new OpenSourceFile(),
                                                                                                new PreviousFilePath(),
                                                                                                new ExitShell() };
        public static ShellCommandFlag[] ShellFlags { get; private set; } = new ShellCommandFlag[] { new Source(), 
                                                                                                     new ShowTree() };


        public static string PreviousSourceFile;
        public static string SourceFile;
        public static bool   ShowTree;

        //...
        public static void InitStandartOptions(string FilePath)
        {
            SourceFile = FilePath;
            PreviousSourceFile = SourceFile;
            ShowTree   = false;
            //...
        }
    }
}
