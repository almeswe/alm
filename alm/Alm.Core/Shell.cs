using System;

using static alm.Other.String.StringMethods;
using static alm.Other.ConsoleStuff.ConsoleCustomizer;

namespace alm.Core.Shell
{
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
                ColorizedPrintln($"\"{subs[0]}\" не является командой.",ConsoleColor.DarkRed);
                return;
            }
            CallCommandByName(subs);
        }

        private void CallCommandByName(string[] subs)
        {
            Command[] Commands = new Command[]
            {
                new Compile(),
                new Recompile(),
                new File(),
                new OpenFile(),
                new CreateFile(),
                new Cls(),
                new Exit()
            };

            foreach (Command command in Commands)
            {
                if (command.Name == subs[0])
                    if (subs.Length - 1 == command.ArgumentCount)
                        command.Execute(subs);
                    else ColorizedPrintln($"Команда [{subs[0]}] не содержит такое кол-во аргументов [{subs.Length - 1}]", ConsoleColor.DarkRed);
            }
        }

        private bool IsCommand(string sub)
        {
            //shtree доступна только в DEBUG
            string[] allCommands = new string[] { "c","rec","file","opfl","crfl","cls","exit","shtree" };

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
        #if DEBUG
        public static string SourcePath = @"C:\Users\Almes\source\repos\Compiler\compiler v.5\Alm\alm\Alm.Tests\TestScripts\AlmDebug.alm";
        #endif

        #if !DEBUG
        public static string SourcePath = "alm";
        #endif

        public static string DestinationPath = "alm.exe";

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
                else this.Value = SubstractSymbol(this.Value,'"');
                return this.Value;
            }
            else return null;
        }
        public Type DefineType()
        {
            if ((Value[0] == '"' && Value[Value.Length - 1] == '"') || Value == "this")
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

        public abstract void Execute(string[] arguments);
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
                    ColorizedPrintln($"Неизвестный тип аргумента, аргумент [{argument.Name}] имееет тип {argument.Type.Name.ToLower()}.", ConsoleColor.DarkRed); return false;
                }
            }
            return true;
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
            if (!ArgumentTypesCorrect()) return;

            this.Arguments = new CommandArgument[] { new CommandArgument("run",     arguments[1],typeof(bool)),
                                                     new CommandArgument("binName", arguments[2], typeof(string)) };

            bool run       = (bool)  this.Arguments[0].DefineValue();
            string binPath = (string)this.Arguments[1].DefineValue();

            ShellInfo.DestinationPath = binPath;
            ShellInfo.RunExeAfterCompiling = run;

            new Compiler.Compiler().CompileThis(ShellInfo.SourcePath,binPath,run);
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
            if (!ArgumentTypesCorrect()) return;

            this.Arguments = new CommandArgument[] { new CommandArgument("filePath", arguments[1], typeof(string)) };

            string filePath = (string)this.Arguments[0].DefineValue();

            if (System.IO.File.Exists(filePath))
                ShellInfo.SourcePath = filePath;
            else ColorizedPrintln($"Указанный файл не существует.", ConsoleColor.DarkRed);
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
            new Compiler.Compiler().CompileThis(ShellInfo.SourcePath, ShellInfo.DestinationPath, ShellInfo.RunExeAfterCompiling);

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
            if (!ArgumentTypesCorrect()) return;

            this.Arguments = new CommandArgument[] { new CommandArgument("filePath", arguments[1], typeof(string)) };

            string filePath = (string)this.Arguments[0].DefineValue();
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    System.IO.File.Create(filePath).Close();
                    ShellInfo.SourcePath = filePath;
                }
                else ColorizedPrintln($"Файл существует.", ConsoleColor.DarkRed);
            }
            catch (Exception e){ ColorizedPrintln($"Возникла ошибка при создании файла.[{e.Message}]",ConsoleColor.DarkRed); }

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
            if (!ArgumentTypesCorrect()) return;

            this.Arguments = new CommandArgument[] { new CommandArgument("filePath", arguments[1], typeof(string)) };
            string filePath = (string)this.Arguments[0].DefineValue();

            try
            {
                System.Diagnostics.Process.Start(filePath);
            }
            catch (Exception e) { ColorizedPrintln($"Возникла ошибка при открытии файла.[{e.Message}]", ConsoleColor.DarkRed); }
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
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}