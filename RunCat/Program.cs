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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Resources;
using System.Windows.Forms;
using CommunityToolkit.Diagnostics;
using Microsoft.Win32;
using RunCat.Properties;

namespace RunCat
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            using var procMutex = new System.Threading.Mutex(true, "_RUNCAT_MUTEX", out var result);
            Guard.IsTrue(result, nameof(result), "Another instance of RunCat is already running.");

            ApplicationConfiguration.Initialize();
            Application.Run(new RunCatApplicationContext());
        }
    }

    public class RunCatApplicationContext : ApplicationContext
    {
        private const int CPU_TIMER_DEFAULT_INTERVAL = 3000;
        private const int ANIMATE_TIMER_DEFAULT_INTERVAL = 200;
        private readonly PerformanceCounter cpuUsage;
        private readonly ToolStripMenuItem runnerMenu;
        private readonly ToolStripMenuItem themeMenu;
        private readonly ToolStripMenuItem startupMenu;
        private readonly ToolStripMenuItem runnerSpeedLimit;
        private readonly NotifyIcon notifyIcon;
        private string runner = "";
        private int current = 0;
        private float minCPU;
        private float interval;
        private string systemTheme = "";
        private string manualTheme = UserSettings.Default.Theme;
        private string speed = UserSettings.Default.Speed;
        private Icon[] icons;
        private readonly Timer animateTimer = new();
        private readonly Timer cpuTimer = new();

        public RunCatApplicationContext()
        {
            Guard.IsNotNull(UserSettings.Default, nameof(UserSettings.Default));
            UserSettings.Default.Reload();
            runner = UserSettings.Default.Runner;
            manualTheme = UserSettings.Default.Theme;

            Application.ApplicationExit += OnApplicationExit;
            SystemEvents.UserPreferenceChanged += UserPreferenceChanged;

            cpuUsage = new PerformanceCounter(
                "Processor Information",
                "% Processor Utility",
                "_Total"
            );
            _ = cpuUsage.NextValue();

            runnerMenu = new ToolStripMenuItem(
                "Runner",
                null,
                new ToolStripMenuItem[]
                {
                    new("Cat", null, SetRunner) { Checked = runner.Equals("cat") },
                    new("Parrot", null, SetRunner) { Checked = runner.Equals("parrot") },
                    new("Horse", null, SetRunner) { Checked = runner.Equals("horse") },
                }
            );

            themeMenu = new ToolStripMenuItem(
                "Theme",
                null,
                new ToolStripMenuItem[]
                {
                    new("Default", null, SetThemeIcons)
                    {
                        Checked = string.IsNullOrEmpty(manualTheme),
                    },
                    new("Light", null, SetLightIcons) { Checked = manualTheme.Equals("light") },
                    new("Dark", null, SetDarkIcons) { Checked = manualTheme.Equals("dark") },
                }
            );

            startupMenu = new ToolStripMenuItem("Startup", null, SetStartup)
            {
                Checked = IsStartupEnabled(),
            };

            runnerSpeedLimit = new ToolStripMenuItem(
                "Runner Speed Limit",
                null,
                new ToolStripMenuItem[]
                {
                    new("Default", null, SetSpeedLimit) { Checked = speed.Equals("default") },
                    new("CPU 10%", null, SetSpeedLimit) { Checked = speed.Equals("cpu 10%") },
                    new("CPU 20%", null, SetSpeedLimit) { Checked = speed.Equals("cpu 20%") },
                    new("CPU 30%", null, SetSpeedLimit) { Checked = speed.Equals("cpu 30%") },
                    new("CPU 40%", null, SetSpeedLimit) { Checked = speed.Equals("cpu 40%") },
                }
            );

            var contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(
                [
                    runnerMenu,
                    themeMenu,
                    startupMenu,
                    runnerSpeedLimit,
                    new ToolStripSeparator(),
                    new ToolStripMenuItem(
                        $"{Application.ProductName} v{Application.ProductVersion}"
                    )
                    {
                        Enabled = false,
                    },
                    new ToolStripMenuItem("Exit", null, Exit),
                ]
            );

            notifyIcon = new NotifyIcon
            {
                Icon = Resources.light_cat_0,
                ContextMenuStrip = contextMenuStrip,
                Text = "0.0%",
                Visible = true,
            };
            notifyIcon.DoubleClick += HandleDoubleClick;

            UpdateThemeIcons();
            SetAnimation();
            SetSpeed();
            StartObserveCPU();
            current = 1;
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            UserSettings.Default.Runner = runner;
            UserSettings.Default.Theme = manualTheme;
            UserSettings.Default.Speed = speed;
            UserSettings.Default.Save();
        }

        private bool IsStartupEnabled()
        {
            string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName);
            return rKey.GetValue(Application.ProductName) != null;
        }

        private string GetAppsUseTheme()
        {
            string keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName);
            object value;
            if (rKey == null || (value = rKey.GetValue("SystemUsesLightTheme")) == null)
            {
                Console.WriteLine("Oh No! Couldn't get theme light/dark");
                return "light";
            }
            int theme = (int)value;
            return theme == 0 ? "dark" : "light";
        }

        private void SetIcons()
        {
            string prefix = !string.IsNullOrEmpty(manualTheme) ? manualTheme : systemTheme;
            ResourceManager rm = Resources.ResourceManager;
            int capacity = runner switch
            {
                "parrot" => 10,
                "horse" => 14,
                _ => 5,
            };
            var list = new List<Icon>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var iconObj = rm.GetObject($"{prefix}_{runner}_{i}");
                Guard.IsNotNull(iconObj, nameof(iconObj));
                list.Add((Icon)iconObj);
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

        private void SetRunner(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item)
            {
                return;
            }
            UpdateCheckedState(item, runnerMenu);
            runner = item.Text.ToLowerInvariant();
            SetIcons();
        }

        private void SetThemeIcons(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item)
            {
                return;
            }
            UpdateCheckedState(item, themeMenu);
            manualTheme = string.Empty;
            systemTheme = GetAppsUseTheme();
            SetIcons();
        }

        private void SetSpeed()
        {
            if (speed.Equals("default"))
            {
                return;
            }
            else if (speed.Equals("cpu 10%"))
            {
                minCPU = 100f;
            }
            else if (speed.Equals("cpu 20%"))
            {
                minCPU = 50f;
            }
            else if (speed.Equals("cpu 30%"))
            {
                minCPU = 33f;
            }
            else if (speed.Equals("cpu 40%"))
            {
                minCPU = 25f;
            }
        }

        private void SetSpeedLimit(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item)
            {
                return;
            }
            UpdateCheckedState(item, runnerSpeedLimit);
            speed = item.Text.ToLowerInvariant();
            SetSpeed();
        }

        private void UpdateThemeIcons()
        {
            if (0 < manualTheme.Length)
            {
                SetIcons();
                return;
            }
            string newTheme = GetAppsUseTheme();
            if (systemTheme.Equals(newTheme))
            {
                return;
            }
            systemTheme = newTheme;
            SetIcons();
        }

        private void SetLightIcons(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item)
            {
                return;
            }
            UpdateCheckedState(item, themeMenu);
            manualTheme = "light";
            SetIcons();
        }

        private void SetDarkIcons(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item)
            {
                return;
            }
            UpdateCheckedState(item, themeMenu);
            manualTheme = "dark";
            SetIcons();
        }

        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                UpdateThemeIcons();
            }
        }

        private void SetStartup(object sender, EventArgs e)
        {
            startupMenu.Checked = !startupMenu.Checked;
            string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName, true);
            if (startupMenu.Checked)
            {
                rKey.SetValue(
                    Application.ProductName,
                    Process.GetCurrentProcess().MainModule.FileName
                );
            }
            else
            {
                rKey.DeleteValue(Application.ProductName, false);
            }
            rKey.Close();
        }

        private void Exit(object sender, EventArgs e)
        {
            cpuUsage.Close();
            animateTimer.Stop();
            cpuTimer.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            if (icons.Length <= current)
            {
                current = 0;
            }
            notifyIcon.Icon = icons[current];
            current = (current + 1) % icons.Length;
        }

        private void SetAnimation()
        {
            animateTimer.Interval = ANIMATE_TIMER_DEFAULT_INTERVAL;
            animateTimer.Tick += new EventHandler(AnimationTick);
        }

        private void CPUTickSpeed()
        {
            if (!speed.Equals("default"))
            {
                float manualInterval = (float)Math.Max(minCPU, interval);
                animateTimer.Stop();
                animateTimer.Interval = (int)manualInterval;
                animateTimer.Start();
            }
            else
            {
                animateTimer.Stop();
                animateTimer.Interval = (int)interval;
                animateTimer.Start();
            }
        }

        private void CPUTick()
        {
            interval = Math.Min(100, cpuUsage.NextValue()); // Sometimes got over 100% so it should be limited to 100%
            notifyIcon.Text = $"CPU: {interval:f1}%";
            interval = 200.0f / (float)Math.Max(1.0f, Math.Min(20.0f, interval / 5.0f));
            _ = interval;
            CPUTickSpeed();
        }

        private void ObserveCPUTick(object sender, EventArgs e)
        {
            CPUTick();
        }

        private void StartObserveCPU()
        {
            cpuTimer.Interval = CPU_TIMER_DEFAULT_INTERVAL;
            cpuTimer.Tick += new EventHandler(ObserveCPUTick);
            cpuTimer.Start();
        }

        private void HandleDoubleClick(object sender, EventArgs e)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "taskmgr.exe",
                UseShellExecute = true,
            };
            Process.Start(startInfo);
        }
    }
}
