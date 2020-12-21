using alm.Core.SyntaxAnalysis;

using static alm.Other.Enums.Operators;
using static alm.Other.String.StringMethods;

namespace alm.Core.CodeGeneration
{
    /*                                MASM assembler
     *              Realization of ALM basic functions by almeswe 2020
     *              
     *                    Args:
     *                      string less than 4 bytes : in msg
     *                      string length less than 4: in ebx
     *                      
     *                    Example of calling:
     *                      mov dword ptr msg , "!ih" -> the console output will be reversed = "hi!"
     *                      mov ebx , 3
     *                      call print_str4
     *                      
     *              print_str4 proc
     *                    invoke GetStdHandle , -11
     *                    invoke WriteConsoleA, eax, offset msg, ebx, 0, 0
     *                    ret 
     *              print_str4 endp
     * 
     * 
     *
    */
    public sealed class CodeGen
    {
        public static string GeneratePrintStr(string msg)
        {
            //print_str(s:string) of integer

            string code       = string.Empty;
            string[] strings4 = SplitStringByFours(msg);

            for (int j = 0; j < strings4.Length-1; j++)
                code += "mov dword ptr msg , " + $"\"{Reverse(strings4[j])}\"\n" +
                        "mov ebx , " + $"{strings4[j].Length}\n"         +
                        "call print_str4\n";

            return code;
        }
        public static string GenerateLenOfStr(string str)
        {
            string code = string.Empty;
            //len(s:string) of integer
            return code;
        }
        public static string GeneratePrintNum(string msg)
        {
            string code = string.Empty;

            return code;
        }
        public static string GenerateBinaryExpression(BinaryExpression BinExpr)
        {
            //TODO Поддержка переменных

            string code = string.Empty;

            if (BinExpr.Nodes[0] is BinaryExpression)
            {
                code += GenerateBinaryExpression((BinaryExpression)BinExpr.Nodes[0]);
                if (BinExpr.Nodes[1] is ConstExpression)
                {
                    switch (BinExpr.Op)
                    {
                        case Plus:
                            code += $"add edx , {((ConstExpression)BinExpr.Nodes[1]).Value}\n";
                            break;
                        case Minus:
                            code += $"sub edx , {((ConstExpression)BinExpr.Nodes[1]).Value}\n";
                            break;
                        case Multiplication:
                            code += $"mov ebx , {((ConstExpression)BinExpr.Nodes[1]).Value}\n" +
                                    $"mul ebx\n"+
                                    $"mov edx , eax\n";
                            break;
                        case Division:
                            code += $"xor edx , edx\n" + 
                                    $"mov ebx , {((ConstExpression)BinExpr.Nodes[1]).Value}\n" +
                                    $"div ebx\n" +
                                    $"mov edx , eax\n";
                            break;
                    }
                }
                if (BinExpr.Nodes[1] is BinaryExpression)
                {
                    code += "mov ecx , edx\n" +
                            GenerateBinaryExpression((BinaryExpression)BinExpr.Nodes[1]);

                    
                    switch (BinExpr.Op)
                    {
                        case Plus:
                            code += $"add edx , ecx\n";
                            break;
                        case Minus:
                            code += $"sub edx , ecx\n";
                            break;
                        case Multiplication:
                            code += $"mov eax , ecx\n" +
                                    $"mov ebx , edx\n" +
                                    $"mul ebx\n"       +
                                    $"mov edx , eax\n";
                            break;
                        case Division:
                            code += $"mov eax , ecx\n" +
                                    $"mov ebx , edx\n" +
                                    $"xor edx , edx\n" +
                                    $"div ebx\n" +
                                    $"mov edx , eax\n";
                            break;
                    }
                }
            }
            else
            {

                if (BinExpr.Nodes[1] is BinaryExpression)
                {
                    code += GenerateBinaryExpression((BinaryExpression)BinExpr.Nodes[1]);
                    switch (BinExpr.Op)
                    {
                        case Plus:
                            code += $"add edx , {((ConstExpression)BinExpr.Nodes[0]).Value}\n";
                            break;
                        case Minus:
                            code += $"sub edx , {((ConstExpression)BinExpr.Nodes[0]).Value}\n";
                            break;
                        case Multiplication:
                            code += $"mov eax , edx\n"+
                                    $"mov ebx , {((ConstExpression)BinExpr.Nodes[0]).Value}\n" +
                                    $"mul ebx \n"     +
                                    $"mov edx , eax\n";
                            break;
                        case Division:
                            code += $"mov eax , edx\n" +
                                    $"xor edx , edx\n" +
                                    $"mov ebx , {((ConstExpression)BinExpr.Nodes[0]).Value}\n" +
                                    $"div ebx \n" +
                                    $"mov edx , eax\n";
                            break;
                    }
                }
                else
                {
                    code += $"mov eax , {((ConstExpression)BinExpr.Nodes[0]).Value}\n";
                    switch (BinExpr.Op)
                    {
                        case Plus:
                            code += $"add eax , {((ConstExpression)BinExpr.Nodes[1]).Value}\n";
                            break;
                        case Minus:
                            code += $"sub eax , {((ConstExpression)BinExpr.Nodes[1]).Value}\n";
                            break;
                        case Multiplication:
                            code += $"mov ebx , {((ConstExpression)BinExpr.Nodes[1]).Value}\n" +
                                    $"mul ebx \n";
                            break;
                        case Division:
                            code += $"xor edx , edx\n" +
                                    $"mov ebx , {((ConstExpression)BinExpr.Nodes[1]).Value}\n" +
                                    $"div ebx \n";
                            break;
                    }
                    code += "mov edx , eax\n";
                }
            }
            return code;
        }
    }
}
