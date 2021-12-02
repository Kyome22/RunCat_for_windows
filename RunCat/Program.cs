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

using RunCat.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Resources;
using System.ComponentModel;
using System.Security.Principal;

namespace RunCat
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // terminate runcat if there's any existing instance
            var procMutex = new System.Threading.Mutex(true, "_RUNCAT_MUTEX", out var result);
            if (!result)
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RunCatApplicationContext());

            procMutex.ReleaseMutex();
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
        private readonly NotifyIcon notifyIcon;
        private string runner;
        private int current;
        private string systemTheme = "";
        private string manualTheme = "";
        private Icon[] icons;
        private readonly Timer animateTimer = new();
        private readonly Timer cpuTimer = new();


        public RunCatApplicationContext()
        {
            UserSettings.Default.Reload();
            runner = UserSettings.Default.Runner;
            manualTheme = UserSettings.Default.Theme;

            Application.ApplicationExit += OnApplicationExit;

            SystemEvents.UserPreferenceChanged += UserPreferenceChanged;

            cpuUsage = new("Processor", "% Processor Time", "_Total");
            _ = cpuUsage.NextValue(); // discards first return value

            runnerMenu = new("Runner", null, new ToolStripMenuItem[]
            {
                new("Cat", null, SetRunner)
                {
                    Checked = runner.Equals("cat")
                },
                new("Parrot", null, SetRunner)
                {
                    Checked = runner.Equals("parrot")
                }
            });

            themeMenu = new("Theme", null, new ToolStripMenuItem[]
            {
                new("Default", null, SetThemeIcons)
                {
                    Checked = manualTheme.Equals("")
                },
                new("Light", null, SetLightIcons)
                {
                    Checked = manualTheme.Equals("light")
                },
                new("Dark", null, SetDarkIcons)
                {
                    Checked = manualTheme.Equals("dark")
                }
            });

            startupMenu = new("Startup", null, SetStartup);
            if (IsStartupEnabled())
            {
                startupMenu.Checked = true;
            }

            var contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                runnerMenu,
                themeMenu,
                startupMenu,
                new ToolStripMenuItem("Exit", null, Exit)
            });

            notifyIcon = new()
            {
                Icon = Resources.light_cat_0,
                ContextMenuStrip = contextMenuStrip,
                Text = "0.0%",
                Visible = true
            };

            notifyIcon.DoubleClick += HandleDoubleClick;

            UpdateThemeIcons();
            SetAnimation();
            CPUTick();
            StartObserveCPU();
            current = 1;
        }
        private void OnApplicationExit(object sender, EventArgs e)
        {
            UserSettings.Default.Runner = runner;
            UserSettings.Default.Theme = manualTheme;
            UserSettings.Default.Save();
        }

        private bool IsStartupEnabled()
        {
            var keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using var rKey = Registry.CurrentUser.OpenSubKey(keyName);
            return rKey?.GetValue(Application.ProductName) != null;
        }

        private string GetAppsUseTheme()
        {
            var keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using var rKey = Registry.CurrentUser.OpenSubKey(keyName);
            object value;
            if (rKey == null || (value = rKey.GetValue("SystemUsesLightTheme")) == null)
            {
                Console.WriteLine("Oh No! Couldn't get theme light/dark");
                return "light";
            }
            var theme = (int)value;
            return theme == 0 ? "dark" : "light";
        }

        private void SetIcons()
        {
            var prefix = 0 < manualTheme.Length ? manualTheme : systemTheme;
            var rm = Resources.ResourceManager;
            var capacity = runner.Equals("cat") ? 5 : 10;
            var list = new List<Icon>(capacity);
            for (var i = 0; i < capacity; i++)
            {
                list.Add((Icon)rm.GetObject($"{prefix}_{runner}_{i}"));
            }
            icons = list.ToArray();
        }

        private void UpdateCheckedState(ToolStripMenuItem sender, ToolStripMenuItem menu)
        {
            foreach (ToolStripMenuItem item in menu.DropDownItems)
            {
                item.Checked = false;
            }
            sender.Checked = true;
        }

        private void SetRunner(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            UpdateCheckedState(item, runnerMenu);
            runner = item.Text.ToLower();
            SetIcons();
        }

        private void SetThemeIcons(object sender, EventArgs e)
        {
            UpdateCheckedState((ToolStripMenuItem)sender, themeMenu);
            manualTheme = "";
            systemTheme = GetAppsUseTheme();
            SetIcons();
        }

        private void UpdateThemeIcons()
        {
            if (0 < manualTheme.Length)
            {
                SetIcons();
                return;
            }
            var newTheme = GetAppsUseTheme();
            if (systemTheme.Equals(newTheme)) return;
            systemTheme = newTheme;
            SetIcons();
        }

        private void SetLightIcons(object sender, EventArgs e)
        {
            UpdateCheckedState((ToolStripMenuItem)sender, themeMenu);
            manualTheme = "light";
            SetIcons();
        }

        private void SetDarkIcons(object sender, EventArgs e)
        {
            UpdateCheckedState((ToolStripMenuItem)sender, themeMenu);
            manualTheme = "dark";
            SetIcons();
        }
        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General) UpdateThemeIcons();
        }

        private void SetStartup(object sender, EventArgs e)
        {
            startupMenu.Checked = !startupMenu.Checked;
            var keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using var rKey = Registry.CurrentUser.OpenSubKey(keyName, true);
            if (startupMenu.Checked)
            {
                var processModule = Process.GetCurrentProcess().MainModule;
                if (processModule is { FileName: { } }) rKey?.SetValue(Application.ProductName, processModule.FileName);
            }
            else
            {
                rKey?.DeleteValue(Application.ProductName, false);
            }

            rKey?.Close();
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
            if (icons.Length <= current) current = 0;
            notifyIcon.Icon = icons[current];
            current = (current + 1) % icons.Length;
        }

        private void SetAnimation()
        {
            animateTimer.Interval = ANIMATE_TIMER_DEFAULT_INTERVAL;
            animateTimer.Tick += AnimationTick;
        }

        private void CPUTick()
        {
            var s = cpuUsage.NextValue();
            notifyIcon.Text = $"CPU: {s:f1}%";
            s = ANIMATE_TIMER_DEFAULT_INTERVAL / Math.Max(1.0f, Math.Min(20.0f, s / 5.0f));
            animateTimer.Stop();
            animateTimer.Interval = (int)s;
            animateTimer.Start();
        }

        private void ObserveCPUTick(object sender, EventArgs e)
        {
            CPUTick();
        }

        private void StartObserveCPU()
        {
            cpuTimer.Interval = CPU_TIMER_DEFAULT_INTERVAL;
            cpuTimer.Tick += ObserveCPUTick;
            cpuTimer.Start();
        }

        private void HandleDoubleClick(object sender, EventArgs e)
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                Process.Start("taskmgr.exe");
            }
        }
    }
}
