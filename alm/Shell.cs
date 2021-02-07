using System;

using static alm.Other.String.StringMethods;
using static alm.Other.ConsoleStuff.ConsoleCustomizer;

namespace alm.Core.Shell
{
    public sealed class Shell
    {
        public static void Main(string[] args) => new Shell().Start();

        public void Start()
        {
            ColorizedPrintln($"Alm Compiler shell {Compiler.Compiler.version}", ConsoleColor.DarkGreen);
            ColorizedPrintln("Enter command \"?\" for more information.");
            ColorizedPrintln($"Copyright (c) 2020-2021 by Alexey Almeswe.", ConsoleColor.DarkGreen);

            Console.WriteLine("");
            while (true)
            {
                ColorizedPrint(">", ConsoleColor.Green);
                ParseInput(Console.ReadLine());
            }
        }

        private void ParseInput(string input)
        {
            string[] subs = Split(input,' ');
            if (subs.Length == 0)
            {
                ColorizedPrintln($"Empty field. (Enter command \"?\" for more information)", ConsoleColor.DarkRed);
                return;
            }
            if (!IsCommand(subs[0]))
            {
                ColorizedPrintln($"\"{subs[0]}\" not a shell command. (Enter command \"?\" for more information)",ConsoleColor.DarkRed);
                return;
            }
            CallCommandByName(subs);
        }

        private void CallCommandByName(string[] subs)
        {
            Command[] Commands = new Command[]
            {
                new Info(),
                new Compile(),
                new Recompile(),
                new File(),
                new OpenFile(),
                new OpenDir(),
                new CreateFile(),
                new Cls(),
                new Exit(),
                new ShowTree()
            };

            foreach (Command command in Commands)
            {
                if (command.Name == subs[0])
                    if (subs.Length - 1 == command.ArgumentCount)
                        command.Execute(subs);
                    else ColorizedPrintln($"Command [{subs[0]}] doesn't accept this number of arguments [{subs.Length - 1}].", ConsoleColor.DarkRed);
            }
        }

        private bool IsCommand(string sub)
        {
            //shtree доступна только в DEBUG
            string[] allCommands = new string[] { "?","c","rec","file","opfl","opdir","crfl","cls","exit","shtree" };

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
                    return false;
                #endif
                #if !DEBUG
                    return true;
                #endif
            }
            return false;
        }
    }

    internal static class ShellInfo
    {
        #if DEBUG
        //public static string SourcePath = @"C:\Users\Almes\source\repos\Compiler\compiler v.5\Alm\alm\Alm.Tests\TestScripts\AlmDebug.alm";
        public static string SourcePath = @"C:\Users\Almes\source\repos\Compiler\compiler v.5\src\tests\main.alm";
        public static string DestinationPath = "alm.exe";

        #endif

        #if !DEBUG
        public static string SourcePath = " ";
        public static string DestinationPath = " ";
        #endif


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
                return this.Value == "1" ? true : false;
            else if (IsString())
            {
                if (this.Value == "this")
                    this.Value = ShellInfo.SourcePath;
                if (this.Value == "here")
                    this.Value = Environment.CurrentDirectory;
                else this.Value = SubstractChar(this.Value, '"');
                return this.Value;
            }
            else return null;
        }
        public Type DefineType()
        {
            if ((Value[0] == '"' && Value[Value.Length - 1] == '"') || Value == "this" || Value == "here")
                return typeof(string);
            else if (Value == "1" || Value == "0") return typeof(bool);
            else return typeof(void);
        }
        public bool IsBoolean()
        {
            return DefineType() == typeof(bool) ? true : false;
        }
        public bool IsString()
        {
            return DefineType() == typeof(string) ? true : false;
        }
        public bool IsUnknown()
        {
            return DefineType() == typeof(void) ? true : false;
        }
    }

    internal abstract class Command
    {
        public string Name;
        public int ArgumentCount;
        public CommandArgument[] Arguments = new CommandArgument[] { };

        protected virtual bool ArgumentTypesCorrect()
        {
            foreach (var argument in this.Arguments)
            {
                if (argument.Type != argument.DefineType())
                {
                    ColorizedPrintln($"Unknown type of argument, argument [{argument.Name}] has {argument.Type.Name.ToLower()} type.", ConsoleColor.DarkRed);
                    return false;
                }
            }
            return true;
        }
        public abstract void Execute(string[] arguments);
    }

    internal sealed class Info : Command
    {
        public Info()
        {
            this.Name = "?";
            this.ArgumentCount = 0;
        }

        //filePath
        public override void Execute(string[] arguments)
        {
            if (!ArgumentTypesCorrect()) return;
            ColorizedPrintln($"Alm shell commands {Compiler.Compiler.version}.");

            ColorizedPrintln("\tVariants of argument representation: ");
            ColorizedPrint("\t\t{str}  ",ConsoleColor.DarkCyan); ColorizedPrintln(": \"string sample\"");
            ColorizedPrint("\t\t{bool} ",ConsoleColor.Blue); ColorizedPrintln(": 1,0");

            ColorizedPrintln("\tExamples of using alm shell commands: ");
            ColorizedPrint("\t\t> ", ConsoleColor.Green); ColorizedPrintln("c 1 \"test.exe\"");
            ColorizedPrint("\t\t> ", ConsoleColor.Green); ColorizedPrintln("file \"C:\\Windows\\this.alm\"");
            ColorizedPrint("\t\t> ", ConsoleColor.Green); ColorizedPrintln("rec");
            ColorizedPrint("\t\t> ", ConsoleColor.Green); ColorizedPrintln("opfl this");

            ColorizedPrint("\t-"); ColorizedPrint("?",ConsoleColor.Red); ColorizedPrint(" [null]",ConsoleColor.Yellow); ColorizedPrintln(" :"); ColorizedPrintln("\t\tShows information about the name of shell commands, their definitions, and their arguments.");

            ColorizedPrint("\t-"); ColorizedPrint("c", ConsoleColor.Red);    ColorizedPrint(" [run {bool}]",ConsoleColor.Blue); ColorizedPrint(" [binName {str}]", ConsoleColor.DarkCyan); ColorizedPrintln(" :"); ColorizedPrintln("\t\tCompiles the file specified in the [file] command and creates an executable file with the specified name in the shell folder.");
            ColorizedPrint("\t-"); ColorizedPrint("rec", ConsoleColor.Red);  ColorizedPrint(" [null]", ConsoleColor.Yellow);  ColorizedPrintln(" :"); ColorizedPrintln("\t\tCompiles with the arguments specified in the last [c] command invokation.");
            ColorizedPrint("\t-"); ColorizedPrint("file", ConsoleColor.Red); ColorizedPrint(" [path {str}]", ConsoleColor.DarkCyan); ColorizedPrintln(" :"); ColorizedPrintln("\t\tSpecifies the path to the file with which the compilation commands will interact.");
            ColorizedPrint("\t-"); ColorizedPrint("opfl", ConsoleColor.Red); ColorizedPrint(" [path {str}]", ConsoleColor.DarkCyan); ColorizedPrintln(" :"); ColorizedPrintln("\t\tOpens a file in notepad at the specified path, or if you use \"this\" as an argument, the file specified in the [file] command will open.");
            ColorizedPrint("\t-"); ColorizedPrint("opdir", ConsoleColor.Red); ColorizedPrint(" [dirpath {str}]", ConsoleColor.DarkCyan); ColorizedPrintln(" :"); ColorizedPrintln("\t\tOpens a folder in Explorer at the specified path, or if you use \"here\" as an argument, the folder where the shell is located will open.");
            ColorizedPrint("\t-"); ColorizedPrint("crfl", ConsoleColor.Red); ColorizedPrint(" [path {str}]", ConsoleColor.DarkCyan); ColorizedPrintln(" :"); ColorizedPrintln("\t\tCreates a file at the specified path, and sets the value of the [file] command to the specified path.");
            ColorizedPrint("\t-"); ColorizedPrint("cls", ConsoleColor.Red);  ColorizedPrint(" [null]", ConsoleColor.Yellow); ColorizedPrintln(" :"); ColorizedPrintln("\t\tClears the console.");
            ColorizedPrint("\t-"); ColorizedPrint("exit", ConsoleColor.Red); ColorizedPrint(" [null]", ConsoleColor.Yellow); ColorizedPrintln(" :"); ColorizedPrintln("\t\tCloses the shell.");

            #if DEBUG
            ColorizedPrint("\t-"); ColorizedPrint("shtree", ConsoleColor.Red); ColorizedPrint(" [show {bool}]", ConsoleColor.Blue); ColorizedPrintln(" :"); ColorizedPrintln("\t\tPrints the parse tree of the program to the console.");
            #endif
        }
    }

    internal sealed class ShowTree : Command
    {
        public ShowTree()
        {
            this.Name = "shtree";
            this.ArgumentCount = 1;
        }

        //show
        public override void Execute(string[] arguments)
        {
            this.Arguments = new CommandArgument[] { new CommandArgument("show", arguments[1], typeof(bool)) };

            if (!ArgumentTypesCorrect()) return;

            bool show = (bool)this.Arguments[0].DefineValue();

            ShellInfo.ShowTree = show;
        }
    }

    internal sealed class Compile : Command
    {        
        public Compile()
        {
            this.Name = "c";
            this.ArgumentCount = 2;
        }

        //run,binName
        public override void Execute(string[] arguments)
        {
            this.Arguments = new CommandArgument[] { new CommandArgument("run",     arguments[1],typeof(bool)),
                                                     new CommandArgument("binName", arguments[2], typeof(string)) };

            if (!ArgumentTypesCorrect()) return;


            bool run       = (bool)  this.Arguments[0].DefineValue();
            string binPath = (string)this.Arguments[1].DefineValue();

            ShellInfo.DestinationPath = binPath;
            ShellInfo.RunExeAfterCompiling = run;

            new Compiler.Compiler().Compile(ShellInfo.SourcePath,binPath,run);
        }
    }
    internal sealed class File : Command
    {
        public File()
        {
            this.Name = "file";
            this.ArgumentCount = 1;
        }

        //filePath
        public override void Execute(string[] arguments)
        {
            this.Arguments = new CommandArgument[] { new CommandArgument("filePath", arguments[1], typeof(string)) };
            if (!ArgumentTypesCorrect()) return;


            string filePath = (string)this.Arguments[0].DefineValue();

            if (System.IO.File.Exists(filePath))
                ShellInfo.SourcePath = filePath;
            else ColorizedPrintln($"The specified path to file doesn't exist.", ConsoleColor.DarkRed);
        }
    }

    internal sealed class Recompile : Command
    {
        public Recompile()
        {
            this.Name = "rec";
            this.ArgumentCount = 0;
        }

        //null
        public override void Execute(string[] arguments)
        {
            if (!ArgumentTypesCorrect()) return;
            new Compiler.Compiler().Compile(ShellInfo.SourcePath, ShellInfo.DestinationPath, ShellInfo.RunExeAfterCompiling);

        }
    }

    internal sealed class CreateFile : Command
    {
        public CreateFile()
        {
            this.Name = "crfl";
            this.ArgumentCount = 1;
        }

        //filePath
        public override void Execute(string[] arguments)
        {
            this.Arguments = new CommandArgument[] { new CommandArgument("filePath", arguments[1], typeof(string)) };
            if (!ArgumentTypesCorrect()) return;


            string filePath = (string)this.Arguments[0].DefineValue();
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    System.IO.File.Create(filePath).Close();
                    ShellInfo.SourcePath = filePath;
                }
                else 
                    ColorizedPrintln($"The file already exists.", ConsoleColor.DarkRed);
            }
            catch (Exception e){ ColorizedPrintln($"Error occurred when trying to create the file.[{e.Message}]",ConsoleColor.DarkRed); }

        }
    }

    internal sealed class OpenDir : Command
    {
        public OpenDir()
        {
            this.Name = "opdir";
            this.ArgumentCount = 1;
        }

        //filePath
        public override void Execute(string[] arguments)
        {
            this.Arguments = new CommandArgument[] { new CommandArgument("dirPath", arguments[1], typeof(string)) };
            if (!ArgumentTypesCorrect()) return;


            string dirPath = (string)this.Arguments[0].DefineValue();
            if (System.IO.Directory.Exists(dirPath))
            {
                try 
                {
                    System.Diagnostics.Process.Start(dirPath);
                }
                catch (Exception e) { ColorizedPrintln($"Error occurred when trying to open the folder.[{e.Message}]", ConsoleColor.DarkRed); }
            }
            else 
                ColorizedPrintln($"The specified path to folder doesn't exist.", ConsoleColor.DarkRed);
        }
    }

    internal sealed class OpenFile : Command
    {
        public OpenFile()
        {
            this.Name = "opfl";
            this.ArgumentCount = 1;
        }

        //filePath
        public override void Execute(string[] arguments)
        {
            this.Arguments = new CommandArgument[] { new CommandArgument("filePath", arguments[1], typeof(string)) };
            if (!ArgumentTypesCorrect()) return;

            string filePath = (string)this.Arguments[0].DefineValue();

            try
            {
                System.Diagnostics.Process.Start(filePath);
            }
            catch (Exception e) { ColorizedPrintln($"Error occurred when trying to open the file.[{e.Message}]", ConsoleColor.DarkRed); }
        }
    }

    internal sealed class Cls : Command
    {
        public Cls()
        {
            this.Name = "cls";
            this.ArgumentCount = 0;
        }

        //null
        public override void Execute(string[] arguments)
        {
            if (!ArgumentTypesCorrect()) return;
            Console.Clear();
        }
    }

    internal sealed class Exit : Command
    {
        public Exit()
        {
            this.Name = "exit";
            this.ArgumentCount = 0;
        }

        //null
        public override void Execute(string[] arguments)
        {
            if (!ArgumentTypesCorrect()) return;
            try
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
            catch (Exception e) { ColorizedPrintln($"Error occurred when trying to close the shell.[{e.Message}]"); }
        }
    }
}