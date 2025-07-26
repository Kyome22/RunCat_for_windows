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

global using CPUInfo = float;
using System.Diagnostics;

namespace RunCat365
{
    internal static class CPUInfoExtension
    {
        internal static string GenerateIndicator(this CPUInfo cpuInfo)
        {
            return $"CPU: {cpuInfo:f1}%";
        }
    }

    internal class CPURepository
    {
        private readonly PerformanceCounter cpuCounter;
        private readonly List<CPUInfo> cpuInfoList = [];
        private const int CPU_INFO_LIST_LIMIT_SIZE = 5;

        internal CPURepository()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ = cpuCounter.NextValue(); // discards first return value
        }

        internal void Update()
        {
            // Range of CPU percentage: 0-100 (%)
            var value = Math.Min(100, cpuCounter.NextValue());
            cpuInfoList.Add(value);
            if (CPU_INFO_LIST_LIMIT_SIZE < cpuInfoList.Count)
            {
                cpuInfoList.RemoveAt(0);
            }
        }

        internal CPUInfo Get()
        {
            return cpuInfoList.Average();
        }

        internal void Close()
        {
            cpuCounter.Close();
        }
    }
}
