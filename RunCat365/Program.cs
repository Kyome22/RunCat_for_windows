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
using System.Diagnostics;
using System.Resources;

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
        private const int CPU_TIMER_DEFAULT_INTERVAL = 1000;
        private const int CPU_VALUES_LIMIT_SIZE = 5;
        private const int ANIMATE_TIMER_DEFAULT_INTERVAL = 200;
        private readonly PerformanceCounter cpuCounter;
        private readonly ToolStripMenuItem storageInfoMenu;
        private readonly ToolStripMenuItem runnerMenu;
        private readonly ToolStripMenuItem themeMenu;
        private readonly ToolStripMenuItem startupMenu;
        private readonly ToolStripMenuItem fpsMaxLimitMenu;
        private readonly NotifyIcon notifyIcon;
        private readonly FormsTimer animateTimer;
        private readonly FormsTimer cpuTimer;
        private Runner runner = Runner.Cat;
        private Theme manualTheme = Theme.System;
        private FPSMaxLimit fpsMaxLimit = FPSMaxLimit.FPS40;
        private List<float> cpuValues = [];
        private Icon[] icons = [];
        private int current = 0;
        private float interval;

        public RunCat365ApplicationContext()
        {
            UserSettings.Default.Reload();
            _ = Enum.TryParse(UserSettings.Default.Runner, out runner);
            _ = Enum.TryParse(UserSettings.Default.Theme, out manualTheme);
            _ = Enum.TryParse(UserSettings.Default.FPSMaxLimit, out fpsMaxLimit);

            Application.ApplicationExit += new EventHandler(OnApplicationExit);

            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ = cpuCounter.NextValue(); // discards first return value

            storageInfoMenu = new ToolStripMenuItem("Storage: -")
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

            startupMenu = new ToolStripMenuItem("Startup", null, SetStartup);
            if (IsStartupEnabled())
            {
                startupMenu.Checked = true;
            }

            var appVersion = $"{Application.ProductName} v{Application.ProductVersion}";
            var appVersionMenu = new ToolStripMenuItem(appVersion)
            {
                Enabled = false
            };

            var contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(
                storageInfoMenu,
                runnerMenu,
                themeMenu,
                fpsMaxLimitMenu,
                startupMenu,
                new ToolStripSeparator(),
                appVersionMenu,
                new ToolStripMenuItem("Exit", null, Exit)
            );

            SetIcons();

            notifyIcon = new NotifyIcon()
            {
                Icon = icons[0],
                ContextMenuStrip = contextMenuStrip,
                Text = "0.0%",
                Visible = true
            };

            animateTimer = new FormsTimer
            {
                Interval = ANIMATE_TIMER_DEFAULT_INTERVAL
            };
            animateTimer.Tick += new EventHandler(AnimationTick);
            animateTimer.Start();

            cpuTimer = new FormsTimer
            {
                Interval = CPU_TIMER_DEFAULT_INTERVAL
            };
            cpuTimer.Tick += new EventHandler(CPUTick);
            cpuTimer.Start();
        }

        private static ToolStripMenuItem CreateMenuFromEnum<T>(
            string title,
            Func<T, string> getTitle,
            EventHandler onClickEvent,
            Func<T, bool> isChecked
        ) where T : Enum
        {
            var items = new List<ToolStripMenuItem>();
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                var item = new ToolStripMenuItem(getTitle(value), null, onClickEvent)
                {
                    Checked = isChecked(value) 
                };
                items.Add(item);
            }
            return new ToolStripMenuItem(title, null, [.. items]);
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
            Theme systemTheme = GetSystemTheme();
            var prefix = (manualTheme == Theme.System ? systemTheme : manualTheme).GetString();
            var runnerName = runner.GetString();
            ResourceManager rm = Resources.ResourceManager;
            var capacity = runner.GetFrameNumber();
            var list = new List<Icon>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var iconName = $"{prefix}_{runnerName}_{i}".ToLower();
                var icon = rm.GetObject(iconName);
                if (icon is null) continue;
                list.Add((Icon)icon);
            }
            icons = [.. list];
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
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
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
                (string? s, out FPSMaxLimit f) => _FPSMaxLimit.TryParse(s, out f),
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
            cpuCounter.Close();
            animateTimer.Stop();
            cpuTimer.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void AnimationTick(object? sender, EventArgs e)
        {
            if (icons.Length <= current) current = 0;
            notifyIcon.Icon = icons[current];
            current = (current + 1) % icons.Length;
        }

        private void CPUTick(object? state, EventArgs e)
        {
            // Range of CPU percentage: 0-100 (%)
            var value = Math.Min(100, cpuCounter.NextValue());
            cpuValues.Add(value);
            if (cpuValues.Count < CPU_VALUES_LIMIT_SIZE) return;

            var averageValue = cpuValues.Average();
            cpuValues.Clear();
            notifyIcon.Text = $"CPU: {averageValue:f1}%";
            // Range of interval: 25-500 (ms) = 2-40 (fps)
            interval = 500.0f / (float)Math.Max(1.0f, (averageValue / 5.0f) * fpsMaxLimit.GetRate());

            var storageInfoList = StorageRepository.Get();
            var storageInfo = storageInfoList[0];
            storageInfoMenu.Text = $"{storageInfo.DriveName}: {storageInfo.UsedSpaceSize.ToByteFormatted()}";

            animateTimer.Stop();
            animateTimer.Interval = (int)interval;
            animateTimer.Start();
        }
    }

    public delegate bool CustomTryParseDelegate<T>(string? value, out T result);
}