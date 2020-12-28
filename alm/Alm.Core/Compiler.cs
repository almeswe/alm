using System;
using System.IO;

using alm.Core.Shell;
using alm.Core.VariableTable;
using alm.Core.SyntaxAnalysis;
using alm.Core.SemanticAnalysis;
using alm.Core.CodeGeneration.Emitter;

using static alm.Other.ConsoleStuff.ConsoleCustomizer;

namespace alm.Core.Compiler
{
    public sealed class Compiler
    {
        public static readonly string version = "v.1.1.1";

        public bool ErrorsOccured = false;

        public static string CurrentParsingFile;
        public static string CompilingSourceFile;
        public static string CompilingDestinationPath;

        public void CompileThis(string sourcePath, string binaryPath, bool run = true)
        {
            CompilingSourceFile = CurrentParsingFile = sourcePath;
            CompilingDestinationPath = binaryPath;

            if (IsFileExists(sourcePath) && IsCorrectExtension(sourcePath))
            {
                Errors.Diagnostics.Reset();

                GlobalTable.Table = Table.CreateTable(null, 1);

                AbstractSyntaxTree ast = new AbstractSyntaxTree();
                ast.BuildTree(sourcePath);

                CheckForErrors();

                if (!ErrorsOccured)
                {
                    LabelChecker.ResolveProgram(ast);
                    CheckForErrors();
                    if (!ErrorsOccured)
                    { 
                        TypeChecker.ResolveTypes(ast);
                        #if DEBUG
                        if (!Errors.Diagnostics.SemanticAnalysisFailed)
                            if (ShellInfo.ShowTree) ast.ShowTree();
                        #endif
                    }
                }
                CheckForErrors();
                if (!ErrorsOccured)
                {
                    Emitter.LoadBootstrapper(Path.GetFileNameWithoutExtension(sourcePath), Path.GetFileNameWithoutExtension(sourcePath));
                    Emitter.EmitAST(ast);
                    if (run)
                        System.Diagnostics.Process.Start(binaryPath);
                    Emitter.Reset();
                }

                Errors.Diagnostics.ShowErrors();
            }
        }
        private bool IsCorrectExtension(string fileName)
        {
            if (Path.GetExtension(fileName) == ".alm") return true;
            ColorizedPrintln("Расширение файла должно быть \".alm\".", ConsoleColor.DarkRed);
            return false;
        }
        private bool IsFileExists(string fileName)
        {
            if (System.IO.File.Exists(fileName)) return true;
            ColorizedPrintln("Указанный файл не существует.", ConsoleColor.DarkRed);
            return false;
        }
        private void CheckForErrors()
        {
            if (Errors.Diagnostics.SyntaxErrors.Count != 0 || Errors.Diagnostics.SemanticErrors.Count != 0)
                ErrorsOccured = true;
        }
    }
}
