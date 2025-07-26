using System.Runtime.InteropServices;

namespace RunCat365
{
    struct MemoryInfo
    {
        internal uint MemoryLoad { get; set; }
        internal long TotalMemory { get; set; }
        internal long AvailableMemory { get; set; }
        internal long UsedMemory { get; set; }
    }

    internal static class MemoryInfoExtension
    {
        internal static List<string> GenerateIndicator(this MemoryInfo memoryInfo)
        {
            var resultLines = new List<string>
            {
                $"Memory: {memoryInfo.MemoryLoad}%",
                $" ├─ Total: {memoryInfo.TotalMemory.ToByteFormatted()}",
                $" ├─ Used: {memoryInfo.UsedMemory.ToByteFormatted()}",
                $" └─ Available: {memoryInfo.AvailableMemory.ToByteFormatted()}"
            };
            return resultLines;
        }
    }

    internal partial class MemoryRepository
    {
        private MemoryInfo memoryInfo;

        internal MemoryRepository()
        {
            memoryInfo = new MemoryInfo
            {
                MemoryLoad = 0,
                TotalMemory = 0,
                AvailableMemory = 0,
                UsedMemory = 0,
            };
        }

        internal void Update()
        {
            var memStatus = new MemoryStatusEx();
            memStatus.dwLength = (uint)Marshal.SizeOf(memStatus);

            if (GlobalMemoryStatusEx(ref memStatus))
            {
                memoryInfo.MemoryLoad = memStatus.dwMemoryLoad;
                memoryInfo.TotalMemory = (long)memStatus.ullTotalPhys;
                memoryInfo.AvailableMemory = (long)memStatus.ullAvailPhys;
                memoryInfo.UsedMemory = (long)(memStatus.ullTotalPhys - memStatus.ullAvailPhys);
            }
        }

        internal MemoryInfo Get()
        {
            Update();
            return memoryInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MemoryStatusEx
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);
    }
}