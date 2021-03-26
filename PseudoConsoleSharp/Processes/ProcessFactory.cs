using System;
using System.Runtime.InteropServices;
using static PseudoConsoleSharp.Native.ProcessApi;
using static PseudoConsoleSharp.Native.HandleApi;

namespace PseudoConsoleSharp
{
    /// <summary>
    /// Support for starting and configuring processes.
    /// </summary>
    /// <remarks>
    /// Possible to replace with managed code? The key is being able to provide the PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE attribute
    /// </remarks>
    internal static class ProcessExFactory
    {
        /// <summary>
        /// Start and configure a process. The return value represents the process and should be disposed.
        /// </summary>
        internal static ProcessEx Start(string command, IntPtr attributes, IntPtr hPC)
        {
            var startupInfo = ConfigureProcessThread(hPC, attributes);
            var processInfo = RunProcess(ref startupInfo, command);
            return new ProcessEx(startupInfo, processInfo);
        }

        private static STARTUPINFOEX ConfigureProcessThread(IntPtr hPC, IntPtr attributes)
        {
            // this method implements the behavior described in https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session#preparing-for-creation-of-the-child-process

            var lpSize = IntPtr.Zero;
            var success = InitializeProcThreadAttributeList(
                lpAttributeList: IntPtr.Zero,
                dwAttributeCount: 1,
                dwFlags: 0,
                lpSize: ref lpSize
            );
            if (success || lpSize == IntPtr.Zero) // we're not expecting `success` here, we just want to get the calculated lpSize
            {
                throw new InvalidOperationException("Could not calculate the number of bytes for the attribute list. " + Marshal.GetLastWin32Error());
            }

            var startupInfo = new STARTUPINFOEX();
            startupInfo.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();
            startupInfo.lpAttributeList = Marshal.AllocHGlobal(lpSize);

            success = InitializeProcThreadAttributeList(
                lpAttributeList: startupInfo.lpAttributeList,
                dwAttributeCount: 1,
                dwFlags: 0,
                lpSize: ref lpSize
            );
            if (!success)
            {
                throw new InvalidOperationException("Could not set up attribute list. " + Marshal.GetLastWin32Error());
            }

            success = UpdateProcThreadAttribute(
                lpAttributeList: startupInfo.lpAttributeList,
                dwFlags: 0,
                attribute: attributes,
                lpValue: hPC,
                cbSize: (IntPtr)IntPtr.Size,
                lpPreviousValue: IntPtr.Zero,
                lpReturnSize: IntPtr.Zero
            );
            if (!success)
            {
                throw new InvalidOperationException("Could not set pseudoconsole thread attribute. " + Marshal.GetLastWin32Error());
            }

            return startupInfo;
        }

        private static PROCESS_INFORMATION RunProcess(ref STARTUPINFOEX sInfoEx, string commandLine, bool inherit_handles=false)
        {
            int securityAttributeSize = Marshal.SizeOf<SECURITY_ATTRIBUTES>();
            var pSec = new SECURITY_ATTRIBUTES { nLength = securityAttributeSize };
            var tSec = new SECURITY_ATTRIBUTES { nLength = securityAttributeSize };
            var success = CreateProcess(
                lpApplicationName: null,
                lpCommandLine: commandLine,
                lpProcessAttributes: ref pSec,
                lpThreadAttributes: ref tSec,
                bInheritHandles: inherit_handles,
                dwCreationFlags: EXTENDED_STARTUPINFO_PRESENT,
                lpEnvironment: IntPtr.Zero,
                lpCurrentDirectory: null,
                lpStartupInfo: ref sInfoEx,
                lpProcessInformation: out PROCESS_INFORMATION pInfo
            );
            if (!success)
            {
                throw new InvalidOperationException("Could not create process. " + Marshal.GetLastWin32Error());
            }

            return pInfo;
        }

        internal static ProcessEx StartStreamConsole(string command, IntPtr[] handles)
        {
            CreateStreamConsoleStartInfo(out STARTUPINFOEX si, handles);            
            var processInfo = RunProcess(ref si, command, inherit_handles: true);
            return new ProcessEx(si, processInfo);
        }

        private static void CreateStreamConsoleStartInfo(out STARTUPINFOEX si, IntPtr[] handles)
        {
            si = new STARTUPINFOEX();
            si.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();

            StartupInfoEx_AllocateAttributeList(ref si, 1);

            uint[] inherit_flags = new uint[handles.Length];
            for (int ii = 0; ii < handles.Length; ++ii)
                inherit_flags[ii] = FOPEN | FDEV;

            StartupInfoEx_SetInheritedHandles(ref si, handles, inherit_flags);

            si.StartupInfo.hStdOutput = handles[2];
            si.StartupInfo.hStdError = handles[2];
            si.StartupInfo.dwFlags = STARTF_USESTDHANDLES;
        }

        private static void StartupInfoEx_AllocateAttributeList(ref STARTUPINFOEX startupInfo, int attribute_count)
        {
            var lpSize = IntPtr.Zero;
            var success = InitializeProcThreadAttributeList(
                lpAttributeList: IntPtr.Zero,
                dwAttributeCount: attribute_count,
                dwFlags: 0,
                lpSize: ref lpSize
            );
            if (success || lpSize == IntPtr.Zero) // we're not expecting `success` here, we just want to get the calculated lpSize
            {
                throw new InvalidOperationException("Could not calculate the number of bytes for the attribute list. " + Marshal.GetLastWin32Error());
            }

            startupInfo.lpAttributeList = Marshal.AllocHGlobal(lpSize);

            success = InitializeProcThreadAttributeList(
                lpAttributeList: startupInfo.lpAttributeList,
                dwAttributeCount: attribute_count,
                dwFlags: 0,
                lpSize: ref lpSize
            );
            if (!success)
            {
                throw new InvalidOperationException("Could not set up attribute list. " + Marshal.GetLastWin32Error());
            }
        }

        private static void StartupInfoEx_SetInheritedHandles(ref STARTUPINFOEX startupInfo, IntPtr[] handles_to_inherit, uint[] flags_to_inherit=null)
        {
            // this method implements the behavior described in https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session#preparing-for-creation-of-the-child-process

            if (flags_to_inherit != null && flags_to_inherit.Length != handles_to_inherit.Length)
                throw new ArgumentException("If flags_to_inherit is non-null, it must have the same length as handles_to_inherit");

            var handle_list_size = handles_to_inherit.Length * Marshal.SizeOf<IntPtr>();
            var handle_list = Marshal.AllocHGlobal(handle_list_size);
            for (int ii = 0; ii < handles_to_inherit.Length; ++ii)
            {
                var dest = handle_list + Marshal.SizeOf<IntPtr>() * ii;
                Marshal.StructureToPtr(handles_to_inherit[ii], dest, false);
            }

            bool success = UpdateProcThreadAttribute(
                lpAttributeList: startupInfo.lpAttributeList,
                dwFlags: 0,
                attribute: (IntPtr)PROC_THREAD_ATTRIBUTE_HANDLE_LIST,
                lpValue: handle_list,
                cbSize: (IntPtr)handle_list_size,
                lpPreviousValue: IntPtr.Zero,
                lpReturnSize: IntPtr.Zero
            );
            if (!success)
            {
                throw new InvalidOperationException("Could not set pseudoconsole thread attribute. " + Marshal.GetLastWin32Error());
            }

            if (flags_to_inherit != null)
            {
                for (int ii = 0; ii < handles_to_inherit.Length; ++ii)
                {
                    SetHandleInformation(handles_to_inherit[ii], HANDLE_FLAG_INHERIT, HANDLE_FLAG_INHERIT);
                }
                var buffer_len = (short)(4 + 1 * flags_to_inherit.Length + Marshal.SizeOf<IntPtr>() * flags_to_inherit.Length);

                var buffer_ptr = Marshal.AllocHGlobal(buffer_len);
                Marshal.StructureToPtr((uint)flags_to_inherit.Length, buffer_ptr, false);
                for (int ii = 0; ii < flags_to_inherit.Length; ++ii)
                {
                    Marshal.StructureToPtr((byte)flags_to_inherit[ii], buffer_ptr + 4 + ii, false);
                }

                for (int ii = 0; ii < flags_to_inherit.Length; ++ii)
                {
                    Marshal.StructureToPtr(handles_to_inherit[ii], buffer_ptr + 4 + flags_to_inherit.Length + Marshal.SizeOf<IntPtr>() * ii, false);
                }

                startupInfo.StartupInfo.cbReserved2 = buffer_len;
                startupInfo.StartupInfo.lpReserved2 = buffer_ptr;
            }
        }

    }
}
