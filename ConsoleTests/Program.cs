using System;

namespace ConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            //var ts = Tests.PseudoConsoleTests.TimeToFirstOutput();
            var ts = Tests.StreamConsoleTests.TimeToFirstOutput();
            Console.WriteLine(ts);
            Console.ReadLine();
        }
    }
}
