using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace RobloxAccountManager.Core
{
    public static class NativeHelper
    {
        // NTSTATUS Constants
        public const uint STATUS_SUCCESS = 0x00000000;
        public const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;

        // Constants for DuplicateHandle
        public const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
        public const uint DUPLICATE_SAME_ACCESS = 0x00000002;

        [DllImport("ntdll.dll")]
        public static extern uint NtQuerySystemInformation(
            SYSTEM_INFORMATION_CLASS SystemInformationClass,
            IntPtr SystemInformation,
            int SystemInformationLength,
            out int ReturnLength
        );

        [DllImport("ntdll.dll")]
        public static extern uint NtQueryObject(
            IntPtr Handle,
            OBJECT_INFORMATION_CLASS ObjectInformationClass,
            IntPtr ObjectInformation,
            int ObjectInformationLength,
            out int ReturnLength
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            ProcessAccessFlags processAccess,
            bool bInheritHandle,
            int processId
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DuplicateHandle(
            IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle,
            IntPtr hTargetProcessHandle,
            out IntPtr lpTargetHandle,
            uint dwDesiredAccess,
            bool bInheritHandle,
            uint dwOptions
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetCurrentProcessId();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        // Enums and Structs

        public enum SYSTEM_INFORMATION_CLASS
        {
            SystemHandleInformation = 16
        }

        public enum OBJECT_INFORMATION_CLASS
        {
            ObjectBasicInformation = 0,
            ObjectNameInformation = 1,
            ObjectTypeInformation = 2
        }

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x00000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_HANDLE_INFORMATION
        {
            public int ProcessId;
            public byte ObjectTypeNumber;
            public byte Flags;
            public ushort Handle;
            public IntPtr Object;
            public uint GrantedAccess;
        }

        // Helper method to structurally read the handle info from the raw buffer
        // Note: The actual SystemHandleInformation structure starts with a count (IntPtr size or ULONG)
        // followed by an array of SYSTEM_HANDLE_INFORMATION.
        // Handling this in C# requires careful pointer arithmetic.
    }
}
