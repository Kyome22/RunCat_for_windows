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

using FormsTimer = System.Windows.Forms.Timer;
using Microsoft.Win32;
using RunCat365.Properties;
using System.ComponentModel;

namespace RunCat365
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Terminate RunCat365 if there's any existing instance.
            using var procMutex = new Mutex(true, "_RUNCAT_MUTEX", out var result);
            if (!result) return;

            try
            {
                ApplicationConfiguration.Initialize();
                Application.SetColorMode(SystemColorMode.System);
                Application.Run(new RunCat365ApplicationContext());
            }
            finally
            {
                procMutex?.ReleaseMutex();
            }
        }
    }

    public class RunCat365ApplicationContext : ApplicationContext
    {
        private const int FETCH_TIMER_DEFAULT_INTERVAL = 1000;
        private const int FETCH_COUNTER_SIZE = 5;
        private const int ANIMATE_TIMER_DEFAULT_INTERVAL = 200;
        private readonly CPURepository cpuRepository;
        private readonly MemoryRepository memoryRepository;
        private readonly StorageRepository storageRepository;
        private readonly CustomToolStripMenuItem systemInfoMenu;
        private readonly CustomToolStripMenuItem runnerMenu;
        private readonly CustomToolStripMenuItem themeMenu;
        private readonly CustomToolStripMenuItem fpsMaxLimitMenu;
        private readonly CustomToolStripMenuItem startupMenu;
        private readonly NotifyIcon notifyIcon;
        private readonly FormsTimer fetchTimer;
        private readonly FormsTimer animateTimer;
        private readonly List<Icon> icons = [];
        private Runner runner = Runner.Cat;
        private Theme manualTheme = Theme.System;
        private FPSMaxLimit fpsMaxLimit = FPSMaxLimit.FPS40;
        private int fetchCounter = 5;
        private int current = 0;

        public RunCat365ApplicationContext()
        {
            UserSettings.Default.Reload();
            _ = Enum.TryParse(UserSettings.Default.Runner, out runner);
            _ = Enum.TryParse(UserSettings.Default.Theme, out manualTheme);
            _ = Enum.TryParse(UserSettings.Default.FPSMaxLimit, out fpsMaxLimit);

            Application.ApplicationExit += new EventHandler(OnApplicationExit);

            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);

            cpuRepository = new CPURepository();
            memoryRepository = new MemoryRepository();
            storageRepository = new StorageRepository();

            systemInfoMenu = new CustomToolStripMenuItem("-\n-\n-\n-\n-")
            {
                Enabled = false
            };

            runnerMenu = CreateMenuFromEnum<Runner>(
                "Runner",
                r => r.GetString(),
                SetRunner,
                r => runner == r
            );

            themeMenu = CreateMenuFromEnum<Theme>(
                "Theme",
                t => t.GetString(),
                SetThemeIcons,
                t => manualTheme == t
            );

            fpsMaxLimitMenu = CreateMenuFromEnum<FPSMaxLimit>(
                "FPS Max Limit",
                fps => fps.GetString(),
                SetFPSMaxLimit,
                fps => fpsMaxLimit == fps
            );

            startupMenu = new CustomToolStripMenuItem("Startup", null, SetStartup);
            if (IsStartupEnabled())
            {
                startupMenu.Checked = true;
            }

            var appVersion = $"{Application.ProductName} v{Application.ProductVersion}";
            var appVersionMenu = new CustomToolStripMenuItem(appVersion)
            {
                Enabled = false
            };

            var contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(
                systemInfoMenu,
                new ToolStripSeparator(),
                runnerMenu,
                themeMenu,
                fpsMaxLimitMenu,
                startupMenu,
                new ToolStripSeparator(),
                appVersionMenu,
                new ToolStripSeparator(),
                new CustomToolStripMenuItem("Exit", null, Exit)
            );
            contextMenuStrip.Renderer = new ContextMenuRenderer();

            SetIcons();

            notifyIcon = new NotifyIcon()
            {
                Icon = icons[0],
                ContextMenuStrip = contextMenuStrip,
                Text = "-",
                Visible = true
            };

            animateTimer = new FormsTimer
            {
                Interval = ANIMATE_TIMER_DEFAULT_INTERVAL
            };
            animateTimer.Tick += new EventHandler(AnimationTick);
            animateTimer.Start();

            fetchTimer = new FormsTimer
            {
                Interval = FETCH_TIMER_DEFAULT_INTERVAL
            };
            fetchTimer.Tick += new EventHandler(FetchTick);
            fetchTimer.Start();
        }

        private static Bitmap? GetRunnerThumbnailBitmap(Runner runner)
        {
            var systemTheme = GetSystemTheme();
            var iconName = $"{systemTheme.GetString()}_{runner.GetString()}_0".ToLower();
            var obj = Resources.ResourceManager.GetObject(iconName);
            return obj is Icon icon ? icon.ToBitmap() : null;
        }

        private static CustomToolStripMenuItem CreateMenuFromEnum<T>(
            string title,
            Func<T, string> getTitle,
            EventHandler onClickEvent,
            Func<T, bool> isChecked
        ) where T : Enum
        {
            var items = new List<CustomToolStripMenuItem>();
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                var entityName = getTitle(value);
                var iconImage = value is Runner runner ? GetRunnerThumbnailBitmap(runner) : null;
                var item = new CustomToolStripMenuItem(entityName, iconImage, onClickEvent)
                {
                    Checked = isChecked(value) 
                };
                items.Add(item);
            }
            return new CustomToolStripMenuItem(title, null, [.. items]);
        }

        private void OnApplicationExit(object? sender, EventArgs e)
        {
            UserSettings.Default.Runner = runner.ToString();
            UserSettings.Default.Theme = manualTheme.ToString();
            UserSettings.Default.FPSMaxLimit = fpsMaxLimit.ToString();
            UserSettings.Default.Save();
        }

        private static bool IsStartupEnabled()
        {
            var keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using var rKey = Registry.CurrentUser.OpenSubKey(keyName);
            if (rKey is null) return false;
            var value = (rKey.GetValue(Application.ProductName) is not null);
            rKey.Close();
            return value;
        }

        private static Theme GetSystemTheme()
        {
            var keyName = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using var rKey = Registry.CurrentUser.OpenSubKey(keyName);
            if (rKey is null) return Theme.Light;
            var value = rKey.GetValue("SystemUsesLightTheme");
            rKey.Close();
            if (value is null) return Theme.Light;
            return (int)value == 0 ? Theme.Dark : Theme.Light;
        }

        private void SetIcons()
        {
            var systemTheme = GetSystemTheme();
            var prefix = (manualTheme == Theme.System ? systemTheme : manualTheme).GetString();
            var runnerName = runner.GetString();
            var rm = Resources.ResourceManager;
            var capacity = runner.GetFrameNumber();
            var list = new List<Icon>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var iconName = $"{prefix}_{runnerName}_{i}".ToLower();
                var icon = rm.GetObject(iconName);
                if (icon is null) continue;
                list.Add((Icon)icon);
            }
            icons.Clear();
            icons.AddRange(list);
        }

        private static void UpdateCheckedState(ToolStripMenuItem sender, ToolStripMenuItem menu)
        {
            foreach (ToolStripMenuItem item in menu.DropDownItems)
            {
                item.Checked = false;
            }
            sender.Checked = true;
        }

        private static void HandleMenuItemSelection<T>(
            object? sender,
            ToolStripMenuItem parentMenu,
            CustomTryParseDelegate<T> tryParseMethod,
            Action<T> assignValueAction
        )
        {
            if (sender is null) return;
            var item = (ToolStripMenuItem)sender;
            UpdateCheckedState(item, parentMenu);
            if (tryParseMethod(item.Text, out T parsedValue))
            {
                assignValueAction(parsedValue);
            }
        }

        private void SetRunner(object? sender, EventArgs e)
        {
            HandleMenuItemSelection(
                sender,
                runnerMenu,
                (string? s, out Runner r) => Enum.TryParse(s, out r),
                value => runner = value
            );
            SetIcons();
        }

        private void SetThemeIcons(object? sender, EventArgs e)
        {
            HandleMenuItemSelection(
                sender,
                themeMenu,
                (string? s, out Theme t) => Enum.TryParse(s, out t),
                value => manualTheme = value
            );
            SetIcons();
        }

        private void SetFPSMaxLimit(object? sender, EventArgs e)
        {
            HandleMenuItemSelection(
                sender,
                fpsMaxLimitMenu,
                (string? s, out FPSMaxLimit f) => FPSMaxLimitExtension.TryParse(s, out f),
                value => fpsMaxLimit = value
            );
        }

        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General) SetIcons();
        }

        private void SetStartup(object? sender, EventArgs e)
        {
            var productName = Application.ProductName;
            if (productName is null) return;
            var keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using var rKey = Registry.CurrentUser.OpenSubKey(keyName, true);
            if (rKey is null) return;
            if (!startupMenu.Checked)
            {
                var fileName = Environment.ProcessPath;
                if (fileName != null)
                {
                    rKey.SetValue(productName, fileName);
                }
            }
            else
            {
                rKey.DeleteValue(productName, false);
            }
            rKey.Close();
            startupMenu.Checked = !startupMenu.Checked;
        }

        private void Exit(object? sender, EventArgs e)
        {
            cpuRepository.Close();
            animateTimer.Stop();
            fetchTimer.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void AnimationTick(object? sender, EventArgs e)
        {
            if (icons.Count <= current) current = 0;
            notifyIcon.Icon = icons[current];
            current = (current + 1) % icons.Count;
        }

        private void FetchSystemInfo(
            CPUInfo cpuInfo,
            MemoryInfo memoryInfo,
            List<StorageInfo> storageValue
        )
        {
            notifyIcon.Text = cpuInfo.GetDescription();

            var systemInfoValues = new List<string>();
            systemInfoValues.AddRange(cpuInfo.GenerateIndicator());
            systemInfoValues.AddRange(memoryInfo.GenerateIndicator());
            systemInfoValues.AddRange(storageValue.GenerateIndicator());
            systemInfoMenu.Text = string.Join("\n", [.. systemInfoValues]);
        }

        private int CalculateInterval(float cpuTotalValue)
        {
            // Range of interval: 25-500 (ms) = 2-40 (fps)
            var speed = (float)Math.Max(1.0f, (cpuTotalValue / 5.0f) * fpsMaxLimit.GetRate());
            return (int)(500.0f / speed);
        }

        private void FetchTick(object? state, EventArgs e)
        {
            cpuRepository.Update();
            fetchCounter += 1;
            if (fetchCounter < FETCH_COUNTER_SIZE) return;
            fetchCounter = 0;

            var cpuInfo = cpuRepository.Get();
            var memoryInfo = memoryRepository.Get();
            var storageInfo = storageRepository.Get();
            FetchSystemInfo(cpuInfo, memoryInfo, storageInfo);

            animateTimer.Stop();
            animateTimer.Interval = CalculateInterval(cpuInfo.Total);
            animateTimer.Start();
        }
    }

    public delegate bool CustomTryParseDelegate<T>(string? value, out T result);
}