using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static PseudoConsoleSharp.Native.ConsoleApi;

namespace PseudoConsoleSharp
{
    /// <summary>
    /// The UI of the terminal. It's just a normal console window, but we're managing the input/output.
    /// In a "real" project this could be some other UI.
    /// </summary>
    internal sealed class Terminal
    {
        private const string ExitCommand = "exit\r";
        private const string CtrlC_Command = "\x3";

        public Terminal()
        {
            Utilities.EnableVirtualTerminalSequenceProcessing();
        }

        /// <summary>
        /// Start the psuedoconsole and run the process as shown in 
        /// https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session#creating-the-pseudoconsole
        /// </summary>
        /// <param name="command">the command to run, e.g. cmd.exe</param>
        public void Run(string command)
        {
            using (var pseudoConsole = new PseudoConsole())
            //using (var process = ProcessExFactory.Start(command, PseudoConsole.PseudoConsoleThreadAttribute, pseudoConsole.Handle))
            {
                pseudoConsole.Start(command);
                // copy all pseudoconsole output to stdout
                Task.Run(() => CopyPipeToOutput(pseudoConsole.OutputStream));
                // prompt for stdin input and send the result to the pseudoconsole
                Task.Run(() => CopyInputToPipe(pseudoConsole.InputStream));
                // free resources in case the console is ungracefully closed (e.g. by the 'x' in the window titlebar)
                OnClose(() => DisposeResources(pseudoConsole));

                pseudoConsole.ProcessEndedGate.WaitOne();
            }
            Thread.Sleep(60 * 1000);
        }

        /// <summary>
        /// Reads terminal input and copies it to the PseudoConsole
        /// </summary>
        /// <param name="inputWriteSide">the "write" side of the pseudo console input pipe</param>
        private static void CopyInputToPipe(Stream inputWriteSide)
        {
            using (var writer = new StreamWriter(inputWriteSide))
            {
                //ForwardCtrlC(writer);
                writer.AutoFlush = true;
                writer.WriteLine(@"cd \");

                while (true)
                {
                    // send input character-by-character to the pipe
                    char key = Console.ReadKey(intercept: true).KeyChar;
                    writer.Write(key);
                }
            }
        }

        /// <summary>
        /// Don't let ctrl-c kill the terminal, it should be sent to the process in the terminal.
        /// </summary>
        private static void ForwardCtrlC(StreamWriter writer)
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                writer.Write(CtrlC_Command);
            };
        }

        /// <summary>
        /// Reads PseudoConsole output and copies it to the terminal's standard out.
        /// </summary>
        /// <param name="outputReadSide">the "read" side of the pseudo console output pipe</param>
        private static void CopyPipeToOutput(Stream outputReadSide)
        {
            using (var reader = new StreamReader(outputReadSide, Encoding.UTF8))
            {
                //pseudoConsoleOutput.CopyTo(terminalOutput);
                while (true)
                {
                    var line = reader.ReadLine();
                    Console.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// Set a callback for when the terminal is closed (e.g. via the "X" window decoration button).
        /// Intended for resource cleanup logic.
        /// </summary>
        private static void OnClose(Action handler)
        {
            SetConsoleCtrlHandler(eventType =>
            {
                if(eventType == CtrlTypes.CTRL_CLOSE_EVENT)
                {
                    handler();
                }
                return false;
            }, true);
        }

        private void DisposeResources(params IDisposable[] disposables)
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
