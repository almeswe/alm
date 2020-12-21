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

        public void DrawError(CompilerError Error, string FilePath)
        {
            if (!Error.HasLocation) return;
            if (this.FilePath != FilePath)
            {
                this.FilePath = FilePath;
                Lines = File.ReadAllLines(FilePath);
            }
            if (Lines is null) Lines = File.ReadAllLines(FilePath);

            int len;
            int tabs;
            string line;

            len = Error.EndsAt.End - Error.StartsAt.Start;

            if (len <= 0) len = 1;

            try
            {
                line = Lines[Error.StartsAt.Line - 1];
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
                ColorizedPrintln("\t\t" + SymbolNTimes(Error.StartsAt.Start - tabs, ' ') + SymbolNTimes(len, '~'), ConsoleColor.Red);
            }
        }
    }
}
