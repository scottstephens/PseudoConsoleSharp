using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using PseudoConsoleSharp;

namespace Tests
{
    [TestFixture]
    public class PseudoConsoleTests
    {
        [Test]
        public void OutputNotBuffered()
        {
            var time_to_first_output = TimeToFirstOutput();
            Assert.That(time_to_first_output, Is.LessThan(TimeSpan.FromSeconds(1)));
        }

        public static TimeSpan TimeToFirstOutput()
        {
            using (var pc = new PseudoConsole())
            using (var input_writer = new StreamWriter(pc.InputStream))
            using (var output_reader = new StreamReader(pc.OutputStream))
            {

                //pc.Start("cmd.exe");
                //var read_task = Task.Run(() => TimeRead(output_reader));
                //input_writer.WriteLine("echo XXXX");
                //input_writer.WriteLine("sleep 5");
                //input_writer.WriteLine("exit");
                //input_writer.Flush();

                //pc.Start(@"cmd.exe /C C:\ProgramFiles\CMEGroup\Span4\bin\spanit.exe C:\Users\scott\Downloads\spanit_example_2.txt");
                pc.Start(@"C:\ProgramFiles\CMEGroup\Span4\bin\spanit.exe C:\Users\scott\Downloads\spanit_example_2.txt");
                var read_task = Task.Run(() => TimeRead(output_reader));

                read_task.Wait();
                return read_task.Result;
            }
        }

        public static TimeSpan TimeRead(StreamReader reader)
        {
            var sw = new Stopwatch();
            sw.Start();
            var line = reader.ReadLine();
            sw.Stop();
            return sw.Elapsed;
        }
    }
}
