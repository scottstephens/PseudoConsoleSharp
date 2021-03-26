using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PseudoConsoleSharp;

namespace Tests
{
    [TestFixture]
    public class StreamConsoleTests
    {
        [Test]
        public void OutputNotBuffered()
        {
            var time_to_first_output = TimeToFirstOutput();
            Assert.That(time_to_first_output, Is.LessThan(TimeSpan.FromSeconds(1)));
        }

        public static TimeSpan TimeToFirstOutput()
        {
            StreamServicer stdout, stderr;
            Task stdout_done, stderr_done;

            using (var pc = new StreamConsole())
            using (var input_writer = new StreamWriter(pc.InputStream))
            {
                stdout = new StreamServicer(pc.OutputStream);
                stderr = new StreamServicer(pc.ErrorStream);
                stdout_done = Task.Factory.StartNew(stdout.Run, TaskCreationOptions.LongRunning);
                stderr_done = Task.Factory.StartNew(stderr.Run, TaskCreationOptions.LongRunning);

                //pc.Start("cmd.exe");
                //var read_task = Task.Run(() => TimeRead(output_reader));
                //input_writer.WriteLine("echo XXXX");
                //input_writer.WriteLine("sleep 5");
                //input_writer.WriteLine("exit");
                //input_writer.Flush();

                //pc.Start(@"cmd.exe /C C:\ProgramFiles\CMEGroup\Span4\bin\spanit.exe C:\Users\scott\Downloads\spanit_example_2.txt");
                pc.Start(@"C:\ProgramFiles\CMEGroup\Span4\bin\spanit.exe C:\Users\scott\Downloads\spanit_example_2.txt");

                pc.ProcessEndedGate.WaitOne();
            }

            stdout_done.Wait();
            stderr_done.Wait();

            var stdout_content = stdout.Output.ToString();
            var stderr_content = stderr.Output.ToString();
            return stdout.TimeToFirstRead;
        }

        public class StreamServicer
        {
            public Stream Stream;
            public string Label;

            public TimeSpan TimeToFirstRead;

            public StringBuilder Output = new StringBuilder();

            public StreamServicer(Stream stream)
            {
                this.Stream = stream;
            }

            public void Run()
            {
                var reader = new StreamReader(this.Stream);
                bool first = true;
                var sw = new Stopwatch();
                sw.Start();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    if (first)
                    {
                        sw.Stop();
                        this.TimeToFirstRead = sw.Elapsed;
                    }

                    this.Output.AppendLine(line);
                }
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
