using System;
using System.Linq;
using System.Collections.Generic;

namespace alm.Other.ConsoleStuff
{
    public sealed class ConsoleCustomizer
    {
        public static void ColorizedPrint(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ResetColor();
        }
        public static void ColorizedPrintln(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}