using System;
using System.IO;

using alm.Core.Errors;

using static alm.Other.String.StringMethods;
using static alm.Other.ConsoleStuff.ConsoleCustomizer;

namespace alm.Other.ConsoleStuff
{
    public sealed class ConsoleErrorDrawer
    {
        private const int tabSize = 4;
        private const char emphChar = '~';
        private const ConsoleColor emphColor = ConsoleColor.Red;

        private string[] lines;

        public void DrawError(CompilerError error)
        {
            try
            {
                if (!error.HasContext)
                    return;

                if (!File.Exists(error.FilePath))
                {
                    ColorizedPrintln("Cannot draw error in the console, because file by this path does not exist.", ConsoleColor.Red);
                    return;
                }

                this.lines = File.ReadAllLines(error.FilePath);

                string erroredLine;
                string separateString;
                string reducedErroredLine;

                int line;
                int difference;
                int emphLineLen = error.EndsAt.CharIndex - error.StartsAt.CharIndex;

                if (this.lines.Length > 0 && this.lines.Length >= error.StartsAt.LineIndex - 1)
                    erroredLine = this.lines[error.StartsAt.LineIndex - 1];
                else
                {
                    ColorizedPrintln("Cannot draw error in the console, because the errored line was not received (may be this line is empty).", ConsoleColor.Red);
                    return;
                }

                line = error.StartsAt.LineIndex;

                reducedErroredLine = DeleteFirstSameChars(erroredLine, ' ', '\t');

                difference = erroredLine.Length - reducedErroredLine.Length + 1;

                for (int i = error.StartsAt.CharIndex - difference; i < error.EndsAt.CharIndex - difference; i++)
                    if (reducedErroredLine[i] == '\t')
                        emphLineLen += tabSize;

                separateString = string.Empty;
                for (int i = 0; i < error.StartsAt.CharIndex - difference; i++)
                {
                    separateString += ' ';
                    if (reducedErroredLine[i] == '\t')
                        for (int j = 0; j < tabSize - 1; j++)
                            separateString += ' ';
                }


                ColorizedPrintln($"{line}.\t\t" + reducedErroredLine, ConsoleColor.Gray);
                ColorizedPrintln("\t\t" + separateString + CharNTimes(emphLineLen, emphChar), emphColor);
            }
            catch
            {
                ColorizedPrintln("Error occurred when trying to draw error in console.", ConsoleColor.Red);
                return;
            }
        }
    }
}
