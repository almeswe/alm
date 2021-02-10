using System;
using System.IO;
using System.Collections.Generic;

using alm.Core.Errors;
using alm.Other.ConsoleStuff;

using alm.Core.BackEnd;
using alm.Core.FrontEnd.SemanticAnalysis;

using static alm.Other.ConsoleStuff.ConsoleCustomizer;

namespace alm.Core.Compiler
{
    public sealed class Compiler
    {
        public class Diagnostics
        {
            public static bool SyntaxAnalysisFailed { get { return SyntaxErrors.Count > 0 ? true : false; } private set { } }
            public static bool SemanticAnalysisFailed { get { return SemanticErrors.Count > 0 ? true : false; } private set { } }

            public static List<SyntaxError> SyntaxErrors     { get; set; } = new List<SyntaxError>();
            public static List<SemanticError> SemanticErrors { get; set; } = new List<SemanticError>();

            public static void ShowErrorsInConsole()
            {
                ConsoleErrorDrawer drawer = new ConsoleErrorDrawer();
                for (int i = 0; i < SyntaxErrors.Count; i++)
                {
                    ColorizedPrintln(SyntaxErrors[i].GetMessage(), System.ConsoleColor.DarkRed);
                    drawer.DrawError(SyntaxErrors[i]);
                }
                for (int i = 0; i < SemanticErrors.Count; i++)
                {
                    ColorizedPrintln(SemanticErrors[i].GetMessage(), System.ConsoleColor.DarkRed);
                    drawer.DrawError(SemanticErrors[i]);
                }
            }

            public static void Reset()
            {
                SyntaxAnalysisFailed = SemanticAnalysisFailed = false;
                SyntaxErrors = new List<SyntaxError>();
                SemanticErrors = new List<SemanticError>();
            }
        }
        public class CompilationVariables
        {
            public static string CurrentParsingModule;
            public static string CompilationEntryModule;
            public static string CompilationBinaryPath;

            public static Dictionary<string, List<string>> CompilationImports = new Dictionary<string, List<string>>();

            public static void Reset()
            {
                CurrentParsingModule = CompilationEntryModule = CompilationBinaryPath = "";
                CompilationImports.Clear();
            }
        }

        public static readonly string version = "v.2.0.0";

        public void Compile(string sourcePath, string binaryPath, bool run = true)
        {
            CompilationVariables.CompilationEntryModule = CompilationVariables.CurrentParsingModule = sourcePath;
            CompilationVariables.CompilationBinaryPath = binaryPath;

            if (IsFileExists(sourcePath) && IsCorrectExtension(sourcePath))
            {
                Diagnostics.Reset();

                SyntaxTree.AbstractSyntaxTree tree = new SyntaxTree.AbstractSyntaxTree(sourcePath);

                if (!Diagnostics.SyntaxAnalysisFailed)
                {
                    LabelChecker.ResolveModule(tree.Root);
                    if (!Diagnostics.SemanticAnalysisFailed)
                        TypeChecker.ResolveModuleTypes(tree.Root);
                }
                if (!Diagnostics.SemanticAnalysisFailed && !Diagnostics.SyntaxAnalysisFailed)
                {
                    Emitter.EmitModule(tree.Root,Path.GetFileNameWithoutExtension(sourcePath) +"Assembly", Path.GetFileNameWithoutExtension(sourcePath) + "Module","MainClass");
                    if (run)
                        System.Diagnostics.Process.Start(CompilationVariables.CompilationBinaryPath);
                }

                tree.ShowTree();

                Diagnostics.ShowErrorsInConsole();
            }

            CompilationVariables.Reset();
        }

        private bool IsFileExists(string fileName)
        {
            if (System.IO.File.Exists(fileName)) return true;
            ColorizedPrintln("Указанный файл не существует.", ConsoleColor.DarkRed);
            return false;
        }
        private bool IsCorrectExtension(string fileName)
        {
            if (Path.GetExtension(fileName) == ".alm") return true;
            ColorizedPrintln("Расширение файла должно быть \".alm\".", ConsoleColor.DarkRed);
            return false;
        }

    }
}
