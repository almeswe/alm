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
                ColorizedPrintln($"\"{subs[0]}\" не является командой. (Нажмите \"?\" для получения информации)",ConsoleColor.DarkRed);
                return;
            }
            CallCommandByName(subs);
        }

        private void CallCommandByName(string[] subs)
        {
            //return later 
            if (subs[0] == "c")
            {
                if (subs.Length == 3)
                    new Compile(subs[1], subs[2]).Execute();
                else ColorizedPrintln($"Команда [{subs[0]}] не содержит такое кол-во аргументов {subs.Length - 1}", ConsoleColor.DarkRed);
            }
            else if (subs[0] == "file")
            {
                if (subs.Length == 2)
                    new File(subs[1]).Execute();
                else ColorizedPrintln($"Команда [{subs[0]}] не содержит такое кол-во аргументов {subs.Length - 1}", ConsoleColor.DarkRed);
            }
            else if (subs[0] == "crfl")
            {
                if (subs.Length == 2)
                    new CreateFile(subs[1]).Execute();
                else ColorizedPrintln($"Команда [{subs[0]}] не содержит такое кол-во аргументов {subs.Length - 1}", ConsoleColor.DarkRed);
            }
            else if (subs[0] == "opfl")
            {
                if (subs.Length == 2)
                    new OpenFile(subs[1]).Execute();
                else ColorizedPrintln($"Команда [{subs[0]}] не содержит такое кол-во аргументов {subs.Length - 1}", ConsoleColor.DarkRed);
            }
            else if (subs[0] == "cls")
            {
                if (subs.Length == 1)
                    new Cls().Execute();
                else ColorizedPrintln($"Команда [{subs[0]}] не содержит такое кол-во аргументов {subs.Length - 1}", ConsoleColor.DarkRed);
            }
            else if (subs[0] == "exit")
            {
                if (subs.Length == 1)
                    new Exit().Execute();
                else ColorizedPrintln($"Команда [{subs[0]}] не содержит такое кол-во аргументов {subs.Length - 1}", ConsoleColor.DarkRed);
            }
            else if (subs[0] == "rec")
            {
                if (subs.Length == 1)
                    new Recompile().Execute();
                else ColorizedPrintln($"Команда [{subs[0]}] не содержит такое кол-во аргументов {subs.Length - 1}", ConsoleColor.DarkRed);
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
                return Value == "1" ? true : false;
            else if (IsString())
            {
                if (this.Value == "this")
                    this.Value = ShellInfo.SourcePath;
                return SubstractSymbol(Value, '"');
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
                    ColorizedPrintln($"Неизвестный тип аргумента, аргумент [{argument.Name}] имееет тип {argument.Type.Name.ToLower()}.", ConsoleColor.DarkRed); return false;
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
    internal sealed class File : Command
    {
        public File(string filePath)
        {
            this.Name = "file";
            this.Arguments = new CommandArgument[] { new CommandArgument("filePath",filePath,typeof(string))};
        }

        public override void Execute()
        {
            if (!ArgumentTypesCorrect()) return;

            string filePath = (string)GetArgumentByName("filePath").DefineValue();

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
            this.Arguments = new CommandArgument[]{};
        }

        public override void Execute()
        {
            if (!ArgumentTypesCorrect()) return;
            new Compiler.Compiler().CompileThis(ShellInfo.SourcePath, ShellInfo.DestinationPath, ShellInfo.RunExeAfterCompiling);

        }
    }

    internal sealed class CreateFile : Command
    {
        public CreateFile(string filePath)
        {
            this.Name = "crfl";
            this.Arguments = new CommandArgument[] { new CommandArgument("filePath", filePath, typeof(string)) };
        }

        public override void Execute()
        {
            if (!ArgumentTypesCorrect()) return;

            string filePath = (string)GetArgumentByName("filePath").DefineValue();
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
        public OpenFile(string filePath)
        {
            this.Name = "opfl";
            this.Arguments = new CommandArgument[] { new CommandArgument("filePath", filePath, typeof(string)) };
        }

        public override void Execute()
        {
            if (!ArgumentTypesCorrect()) return;

            string filePath = (string)GetArgumentByName("filePath").DefineValue();

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
            this.Arguments = new CommandArgument[] {};
        }

        public override void Execute()
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
            this.Arguments = new CommandArgument[] { };
        }

        public override void Execute()
        {
            if (!ArgumentTypesCorrect()) return;
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}