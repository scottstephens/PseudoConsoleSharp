using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PseudoConsoleSharp.Native
{
    public class HandleApi
    {
        public const uint HANDLE_FLAG_INHERIT = 0x00000001;
        public const uint HANDLE_FLAG_PROTECT_FROM_CLOSE = 0x00000002;

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetHandleInformation(IntPtr handle, ref uint flags);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetHandleInformation(IntPtr handle, uint mask, uint flags);
    }
}
