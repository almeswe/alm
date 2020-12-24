using System;
using System.Linq;
using System.Collections.Generic;

namespace alm.Other.ConsoleStuff
{
    public sealed class ConsoleCustomizer
    {
        private static ConsoleColor[] colors_copy = new ConsoleColor[]{
        ConsoleColor.DarkBlue,
        ConsoleColor.DarkGreen,
        ConsoleColor.DarkCyan,
        ConsoleColor.DarkRed,
        ConsoleColor.DarkMagenta,
        ConsoleColor.DarkYellow ,
        ConsoleColor.Gray,
        ConsoleColor.DarkGray,
        ConsoleColor.Blue,
        ConsoleColor.Green,
        ConsoleColor.Cyan,
        ConsoleColor.Red,
        ConsoleColor.Magenta,
        ConsoleColor.Yellow,
        ConsoleColor.White
        };
        private static List<ConsoleColor> colors = colors_copy.ToList();
        public static ConsoleColor currColor;
        public static ConsoleColor GetColor()
        {
            /*Все узлы данного узла окрашиваются в один цвет*/
            if (colors.Count > 0)
            {
                currColor = colors[0];
                colors.RemoveAt(0);
                return currColor;
            }
            else
            {
                colors = colors_copy.ToList();
                currColor = colors[0];
                colors.RemoveAt(0);
                return currColor;
            }
        }
        public static ConsoleColor GetColor(int level)
        {
            /* Все узлы одной вложенности окрашиваются в один цвет */
            if (level >= colors_copy.Length) level -= (int)(level / colors_copy.Length) * colors_copy.Length;
            return colors_copy[level];
        }
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