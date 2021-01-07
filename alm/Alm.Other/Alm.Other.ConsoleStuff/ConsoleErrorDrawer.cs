using System;
using System.IO;

using alm.Core.Errors;

using static alm.Other.String.StringMethods;
using static alm.Other.ConsoleStuff.ConsoleCustomizer;

namespace alm.Other.ConsoleStuff
{
    public sealed class ConsoleErrorDrawer
    {
        private string filePath;
        private string[] lines;

        public void DrawError(CompilerError error, string path)
        {
            if (!error.HasContext)
                return;
            if (this.filePath != path)
            {
                this.filePath = path;
                this.lines = File.ReadAllLines(path);
            }
            if (this.lines is null)
                this.lines = File.ReadAllLines(path);

            int len;
            int tabs;
            string line;

            len = error.EndsAt.CharIndex - error.StartsAt.CharIndex;

            if (len <= 0) 
                len = 1;

            try
            {
                line = lines[error.StartsAt.LineIndex - 1];
            }
            catch (IndexOutOfRangeException)
            {
                line = string.Empty;
            }

            tabs = Tabulations(line)+1;

            line = "\t\t" + DeleteFirstSpaces(SubstractSymbol(line, '\t'));

            if (line != string.Empty)
            {
                ColorizedPrintln(line, ConsoleColor.Gray);
                ColorizedPrintln("\t\t" + SymbolNTimes(error.StartsAt.CharIndex-tabs, ' ') + SymbolNTimes(len, '~'), ConsoleColor.Red);
            }
        }
    }
}
