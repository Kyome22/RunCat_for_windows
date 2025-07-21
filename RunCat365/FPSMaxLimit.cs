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
    enum FPSMaxLimit
    {
        FPS40,
        FPS30,
        FPS20,
        FPS10,
    }

    static class FPSMaxLimitExtensions
    {
        internal static string GetString(this FPSMaxLimit fpsMaxLimit)
        {
            return fpsMaxLimit switch
            {
                FPSMaxLimit.FPS40 => "40fps",
                FPSMaxLimit.FPS30 => "30fps",
                FPSMaxLimit.FPS20 => "20fps",
                FPSMaxLimit.FPS10 => "10fps",
                _ => "",
            };
        }

        internal static float GetRate(this FPSMaxLimit fPSMaxLimit)
        {
            return fPSMaxLimit switch
            {
                FPSMaxLimit.FPS40 => 1f,
                FPSMaxLimit.FPS30 => 0.75f,
                FPSMaxLimit.FPS20 => 0.5f,
                FPSMaxLimit.FPS10 => 0.25f,
                _ => 1f,
            };
        }
    }

    static class _FPSMaxLimit
    {
        internal static FPSMaxLimit Parse(string value)
        {
            return value switch
            {
                "40fps" => FPSMaxLimit.FPS40,
                "30fps" => FPSMaxLimit.FPS30,
                "20fps" => FPSMaxLimit.FPS20,
                "10fps" => FPSMaxLimit.FPS10,
                _ => FPSMaxLimit.FPS40,
            };
        }
    }
}
