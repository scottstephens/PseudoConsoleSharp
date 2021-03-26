using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using static PseudoConsoleSharp.Native.PseudoConsoleApi;

namespace PseudoConsoleSharp
{
    public sealed class StreamConsole : IDisposable
    {
        public static readonly IntPtr PseudoConsoleThreadAttribute = (IntPtr)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE;

        public AnonymousPipeServerStream InputStream { get; private set; }
        public AnonymousPipeServerStream OutputStream { get; private set; }
        public AnonymousPipeServerStream ErrorStream { get; private set; }

        private ProcessEx Process;

        public delegate void ProcessEndedDel(StreamConsole sender);
        public event ProcessEndedDel ProcessEnded;
        public ManualResetEvent ProcessEndedGate = new ManualResetEvent(false);

        public StreamConsole()
        {
            this.InputStream = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            this.OutputStream = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            this.ErrorStream = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        }

        public StreamConsole(string command)
            : this()
        {
            this.Start(command);
        }

        public void Start(string command)
        {
            var handle_list = new IntPtr[3];
            handle_list[0] = this.InputStream.ClientSafePipeHandle.DangerousGetHandle();
            handle_list[2] = this.OutputStream.ClientSafePipeHandle.DangerousGetHandle();
            handle_list[1] = this.ErrorStream.ClientSafePipeHandle.DangerousGetHandle();

            this.Process = ProcessExFactory.StartStreamConsole(command, handle_list);
            this.InputStream.DisposeLocalCopyOfClientHandle();
            this.OutputStream.DisposeLocalCopyOfClientHandle();
            this.ErrorStream.DisposeLocalCopyOfClientHandle();
            var wh = new SafeWaitHandle(this.Process.ProcessInfo.hProcess, ownsHandle: false);
            var mre = new ManualResetEvent(false)
            {
                SafeWaitHandle = wh,
            };
            ThreadPool.RegisterWaitForSingleObject(mre, RaiseProcessEnded, this, -1, true);
        }

        private static void RaiseProcessEnded(object state, bool timed_out)
        {
            var p = (StreamConsole)state;
            p.RaiseProcessEnded();
        }

        private void RaiseProcessEnded()
        {
            this.ProcessEndedGate.Set();
            this.ProcessEnded?.Invoke(this);
        }

        public void Dispose()
        {
            this.InputStream.Dispose();
            this.OutputStream.Dispose();
            this.ErrorStream.Dispose();
        }
    }
}
