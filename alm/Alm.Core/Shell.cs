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
            ShellOptions.SourceFile = @"C:\Users\Almes\source\repos\Compiler\compiler v.5\Alm\alm\Alm.Tests\TestScripts\AlmDebug.alm";
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

    public sealed class Shell
    {
        public void Start()
        {
            while (true)
            {
                ColorizedPrint(">",ConsoleColor.Green);
                ParseInput(Console.ReadLine());
            }
        }

        private void ParseInput(string input)
        {
            string[] subs = SplitSubstrings(input);
            if (!IsCommand(subs[0]))
            {
                ColorizedPrintln($"\"{subs[0]}\" не является командой. (Нажмите \"?\" для получения информации)",ConsoleColor.DarkRed);
                return;
            }
            if (subs[0] == "c")
            {
                if (subs.Length == 3)
                    new Compile(subs[1], subs[2]);
                else ColorizedPrintln($"Команда [{subs[0]}] не содержит такое кол-во аргументов {subs.Length}",ConsoleColor.DarkRed);
            }
        }

        private bool IsCommand(string sub)
        {
            //shtree доступна только в DEBUG
            string[] allCommands = new string[] { "c","rec","file","opfl","cls","exit","shtree" };

            for (int i = 0; i < allCommands.Length; i++)
                if (allCommands[i] == sub && !IsBlocked(allCommands[i])) return true;

            return false;
        }

        private bool IsArgument(string sub)
        {
            if (!IsCommand(sub)) return true;
            return false;
        }

        private bool IsBlocked(string command)
        {
            if (command == "shtree")
            { 
                #if DEBUG
                    return true;
                #endif
            }
            return false;
        }
    }

    internal static class ShellInfo
    {
        public static string SourcePath = @"C:\Users\Almes\source\repos\Compiler\compiler v.5\Alm\alm\Alm.Tests\TestScripts\AlmDebug.alm";
        public static string DestinationPath;

        public static bool ShowTree = false;
        public static bool RunExeAfterCompiling = true;
    }

    internal sealed class CommandArgument
    {
        public Type Type { get; set; }
        public string Name  { get; set; }
        public string Value { get; set; }

        public CommandArgument(string name,string value,Type type)
        {
            this.Type = type;
            this.Name = name;
            this.Value = value;
        }

        public object DefineValue()
        {
            if (IsBoolean())
                return Value == "true" ? true : false;
            else if (IsString())
                return SubstractSymbol(Value, '"');
            else return null;
        }
        public Type DefineType()
        {
            if (Value[0] == '"' && Value[Value.Length - 1] == '"')
                return typeof(string);
            else if (bool.Parse(Value)) return typeof(bool);
            else return typeof(void);
        }
        public bool IsBoolean()
        {
            if (DefineType() == typeof(bool)) return true;
            return false;
        }
        public bool IsString()
        {
            if (DefineType() == typeof(string)) return true;
            return false;
        }
        public bool IsUnknown()
        {
            if (DefineType() == typeof(void)) return true;
            return false;
        }
    }
    internal abstract class Command
    {
        public string Name;
        public CommandArgument[] Arguments;

        public abstract void Execute();
        protected virtual CommandArgument GetArgumentByName(string name)
        {
            for (int i = 0; i < this.Arguments.Length; i++)
                if (name == this.Arguments[i].Name) return this.Arguments[i];

            return null;
        }
        protected virtual bool ArgumentTypesCorrect()
        {
            foreach (var argument in this.Arguments)
            {
                if (argument.Type != argument.DefineType())
                {
                    ColorizedPrintln($"Неизвестный тип аргумента, аргумент [{argument.Name}] имееет тип {argument.Type}.", ConsoleColor.DarkRed); return false;
                }
            }
            return true;
        }
    }

    internal sealed class Compile : Command
    {        
        public Compile(string run,string binPath)
        {
            this.Name = "c";
            this.Arguments = new CommandArgument[] { new CommandArgument("run",run,typeof(bool)),
                                                     new CommandArgument("binPath", binPath, typeof(string)) };
        }

        public override void Execute()
        {
            if (!ArgumentTypesCorrect()) return;

            bool run =       (bool)GetArgumentByName("run").DefineValue();
            string binPath = (string)GetArgumentByName("binPath").DefineValue();

            ShellInfo.DestinationPath = binPath;
            ShellInfo.RunExeAfterCompiling = run;

            new Compiler.Compiler().CompileThis(ShellInfo.SourcePath,binPath,run);
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
            new Compiler.Compiler().CompileThis(ShellOptions.SourceFile,Path.ChangeExtension(Path.GetFileName(ShellOptions.SourceFile),"exe"));
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
