using System;
using System.Collections.Generic;
using System.Text;
using static PseudoConsoleSharp.Native.ConsoleApi;

namespace PseudoConsoleSharp
{
    internal class Utilities
    {
        /// <summary>
        /// Newer versions of the windows console support interpreting virtual terminal sequences, we just have to opt-in
        /// </summary>
        public static void EnableVirtualTerminalSequenceProcessing()
        {
            var hStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            if (!GetConsoleMode(hStdOut, out uint outConsoleMode))
            {
                throw new InvalidOperationException("Could not get console mode");
            }

            outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
            if (!SetConsoleMode(hStdOut, outConsoleMode))
            {
                throw new InvalidOperationException("Could not enable virtual terminal processing");
            }
        }
    }
}
