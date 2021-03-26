using Microsoft.Win32.SafeHandles;
using System;
using static PseudoConsoleSharp.Native.PseudoConsoleApi;
using System.IO.Pipes;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace PseudoConsoleSharp
{
    /// <summary>
    /// Utility functions around the new Pseudo Console APIs
    /// </summary>
    public sealed class PseudoConsole : IDisposable
    {
        public static readonly IntPtr PseudoConsoleThreadAttribute = (IntPtr)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE;

        public IntPtr Handle { get; }
        public AnonymousPipeServerStream InputStream { get; private set; }
        public AnonymousPipeServerStream OutputStream { get; private set; }

        private ProcessEx Process;

        public delegate void ProcessEndedDel(PseudoConsole sender);
        public event ProcessEndedDel ProcessEnded;
        public ManualResetEvent ProcessEndedGate = new ManualResetEvent(false);

        public PseudoConsole()
        {
            this.InputStream = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            this.OutputStream = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

            this.Handle = Create(this.InputStream.ClientSafePipeHandle, this.OutputStream.ClientSafePipeHandle, 120, 80);
        }

        public PseudoConsole(string command)
            : this()
        {
            this.Start(command);
        }

        public void Start(string command)
        {
            this.Process = ProcessExFactory.Start(command, PseudoConsole.PseudoConsoleThreadAttribute, this.Handle);
            this.InputStream.DisposeLocalCopyOfClientHandle();
            this.OutputStream.DisposeLocalCopyOfClientHandle();
            var wh = new SafeWaitHandle(this.Process.ProcessInfo.hProcess, ownsHandle: false);
            var mre = new ManualResetEvent(false)
            {
                SafeWaitHandle = wh,
            };
            ThreadPool.RegisterWaitForSingleObject(mre, RaiseProcessEnded, this, -1, true);
        }

        private static void RaiseProcessEnded(object state, bool timed_out)
        {
            var p = (PseudoConsole)state;
            p.RaiseProcessEnded();
        }

        private void RaiseProcessEnded()
        {
            this.ProcessEndedGate.Set();
            this.ProcessEnded?.Invoke(this);
        }

        internal static IntPtr Create(SafeHandle inputReadSide, SafeHandle outputWriteSide, int width, int height)
        {
            var createResult = CreatePseudoConsole(
                new COORD { X = (short)width, Y = (short)height },
                inputReadSide, outputWriteSide,
                0, out IntPtr hPC);
            if(createResult != 0)
            {
                throw new InvalidOperationException("Could not create psuedo console. Error Code " + createResult);
            }
            return hPC;
        }

        public void Dispose()
        {
            ClosePseudoConsole(Handle);
            this.InputStream.Dispose();
            this.OutputStream.Dispose();
        }
    }
}
