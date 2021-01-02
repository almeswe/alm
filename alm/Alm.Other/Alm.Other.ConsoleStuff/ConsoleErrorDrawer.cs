using System;
using System.IO;

using alm.Core.Errors;

using static alm.Other.String.StringMethods;
using static alm.Other.ConsoleStuff.ConsoleCustomizer;

namespace alm.Other.ConsoleStuff
{
    public sealed class ConsoleErrorDrawer
    {
        private string FilePath;
        private string[] Lines;

        public void DrawError(CompilerError error, string path)
        {
            if (!error.HasLocation) return;
            if (this.FilePath != path)
            {
                this.FilePath = path;
                Lines = File.ReadAllLines(path);
            }
            if (Lines is null) Lines = File.ReadAllLines(path);

            int len;
            int tabs;
            string line;

            len = error.EndsAt.End - error.StartsAt.Start;

            if (len <= 0) len = 1;

            try
            {
                line = Lines[error.StartsAt.Line - 1];
                tabs = Tabulations(line)+1;
            }
            catch (IndexOutOfRangeException)
            {
                line = string.Empty;
                tabs = 1;
            }

            line = "\t\t" + DeleteFirstSpaces(SubstractSymbol(line, '\t'));

            if (line != string.Empty)
            {
                ColorizedPrintln(line, ConsoleColor.Gray);
                ColorizedPrintln("\t\t" + SymbolNTimes(error.StartsAt.Start - tabs, ' ') + SymbolNTimes(len, '~'), ConsoleColor.Red);
            }
        }
    }
}
