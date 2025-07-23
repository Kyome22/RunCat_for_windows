// Copyright 2020 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

namespace RunCat365
{
    struct StorageInfo
    {
        internal string DriveName { get; set; }
        internal System.IO.DriveType DriveType { get; set; }
        internal long TotalSize { get; set; }
        internal long AvailableSpaceSize { get; set; }
        internal long UsedSpaceSize { get; set; }
    }

    static class ByteFormatter
    {
        internal static string ToByteFormatted(this long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double doubleBytes = bytes;
            while (1024 <= doubleBytes && i < units.Length - 1)
            {
                doubleBytes /= 1024;
                i++;
            }
            return string.Format("{0:0.##} {1}", doubleBytes, units[i]);
        }
    }

    static class StorageRepository
    {
        internal static StorageInfo[] Get()
        {
            var storageInfoList = new List<StorageInfo>();
            var allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo driveInfo in allDrives)
            {
                var storageInfo = new StorageInfo
                {
                    DriveName = driveInfo.Name,
                    DriveType = driveInfo.DriveType,
                    TotalSize = 0,
                    AvailableSpaceSize = 0,
                    UsedSpaceSize = 0
                };
                if (driveInfo.IsReady)
                {
                    try
                    {
                        storageInfo.TotalSize = driveInfo.TotalSize;
                        storageInfo.AvailableSpaceSize = driveInfo.AvailableFreeSpace;
                        storageInfo.UsedSpaceSize = driveInfo.TotalSize - driveInfo.AvailableFreeSpace;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
                storageInfoList.Add(storageInfo);
            }
            return [.. storageInfoList];
        }
    }
}
