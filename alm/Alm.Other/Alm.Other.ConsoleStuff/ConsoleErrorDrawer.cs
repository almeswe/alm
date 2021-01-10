using System;
using System.IO;

using alm.Core.Errors;

using static alm.Other.String.StringMethods;
using static alm.Other.ConsoleStuff.ConsoleCustomizer;

namespace alm.Other.ConsoleStuff
{
    public sealed class ConsoleErrorDrawer
    {
        //табы иногда какого-то хуя разной длины(?)
        private const int tabSize = 4;
        private const char emphChar = '~';
        private const ConsoleColor emphColor = ConsoleColor.Red;

        private string filePath;
        private string[] lines;

        public void DrawError(CompilerError error)
        {
            if (!File.Exists(error.FilePath))
            {
                ColorizedPrintln("Невозможно отрисовать ошибку в консоль,так как файла в котором произошла ошибка не существует.", ConsoleColor.Red);
                return;
            }
            if (!error.HasContext)
                return;

            this.lines = File.ReadAllLines(error.FilePath);

            string erroredLine;
            string separateString;
            string reducedErroredLine;

            int line;
            int difference;
            int emphLineLen = error.EndsAt.CharIndex - error.StartsAt.CharIndex;

            if (this.lines.Length > 0 && this.lines.Length <= error.StartsAt.LineIndex - 1)
                erroredLine = this.lines[error.StartsAt.LineIndex - 1];
            else
            {
                ColorizedPrintln("Невозможно отрисовать ошибку в консоль,так как из файла не получена строка с номером ошибки (возможна она просто пустая).", ConsoleColor.Red);
                return;
            }

            line = error.StartsAt.LineIndex;

            reducedErroredLine = DeleteFirstSameChars(erroredLine, ' ', '\t');

            difference = erroredLine.Length - reducedErroredLine.Length + 1;

            for (int i = error.StartsAt.CharIndex - difference; i < error.EndsAt.CharIndex - difference; i++)
                if (erroredLine[i] == '\t')
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
    }
}
