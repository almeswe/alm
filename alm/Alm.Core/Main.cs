using alm.Core.Shell;
using alm.Core.vm_lc3;

namespace alm.Core.Main
{
    public class Program
    {
        static void Main(string[] args) => new CompilerShell().Run();
        /*static void Main(string[] args)
        {
            System.Console.WriteLine(CodeGeneration.CodeGen.GeneratePrintStr("try except"));
            System.Console.ReadLine();
        }*/
        /*static void Main(string[] args)
        {
            Stack stack = new Stack(ushort.MaxValue);
            stack.Random();
            var start = System.DateTime.Now;
            System.Console.WriteLine(stack.PercentOfUsingMemory);
            System.Console.WriteLine(start-System.DateTime.Now);
            System.Console.ReadLine();
        }*/
    }
}
