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
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            try
            {
                Application.Run(new RunCatApplicationContext());
            }
            finally
            {
                procMutex?.ReleaseMutex();
            }
        }
    }

    public class RunCatApplicationContext : ApplicationContext
    {
        private const int CPU_TIMER_DEFAULT_INTERVAL = 3000;
        private const int ANIMATE_TIMER_DEFAULT_INTERVAL = 200;
        private PerformanceCounter cpuUsage;
        private ToolStripMenuItem runnerMenu;
        private ToolStripMenuItem themeMenu;
        private ToolStripMenuItem startupMenu;
        private ToolStripMenuItem runnerSpeedLimit;
        private NotifyIcon notifyIcon;
        private string runner = UserSettings.Default.Runner;
        private int current = 0;
        private float minCPU;
        private float interval;
        private string systemTheme = string.Empty;
        private string manualTheme = UserSettings.Default.Theme;
        private string speed = UserSettings.Default.Speed;
        private Icon[] icons;
        private Timer animateTimer = new Timer();
        private Timer cpuTimer = new Timer();


        public RunCatApplicationContext()
        {
            UserSettings.Default.Reload();

            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);

            cpuUsage = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
            _ = cpuUsage.NextValue(); // discards first return value

            var getRunnerItem = (string s1, string s2) =>
            {
                return new ToolStripMenuItem(s1, null, SetRunner) { Checked = runner.Equals(s2) };
            };
            runnerMenu = new("Runner", null, new[]
            {
                getRunnerItem("Car", "cat"),
                getRunnerItem("Parrot", "parrot"),
                getRunnerItem("Horse", "horse"),
            });

            var getThemeItem = (string s1, string s2) =>
            {
                return new ToolStripMenuItem(s1, null, SetThemeIcons) { Checked = manualTheme.Equals(s2) };
            };
            themeMenu = new("Theme", null, new[]
            {
                getThemeItem("Default", string.Empty),
                getThemeItem("Light", "light"),
                getThemeItem("Dark", "dark"),
            });

            startupMenu = new ToolStripMenuItem("Startup", null, SetStartup);
            if (IsStartupEnabled())
            {
                startupMenu.Checked = true;
            }

            var getSpeedItem = (string s1, string s2) =>
            {
                return new ToolStripMenuItem(s1, null, SetSpeedLimit) { Checked = speed.Equals(s2) };
            };
            runnerSpeedLimit = new("Runner Speed Limit", null, new[]
            {
                getSpeedItem("Default", "default"),
                getSpeedItem("CPU 10%", "cpu 10%"),
                getSpeedItem("CPU 10%", "cpu 20%"),
                getSpeedItem("CPU 10%", "cpu 30%"),
                getSpeedItem("CPU 10%", "cpu 40%"),
            });

            ContextMenuStrip contextMenuStrip = new(new Container());
            contextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                runnerMenu,
                themeMenu,
                startupMenu,
                runnerSpeedLimit,
                new ToolStripSeparator(),
                new ToolStripMenuItem($"{Application.ProductName} v{Application.ProductVersion}")
                {
                    Enabled = false
                },
                new ToolStripMenuItem("Exit", null, Exit)
            });

            notifyIcon = new NotifyIcon()
            {
                Icon = Resources.light_cat_0,
                ContextMenuStrip = contextMenuStrip,
                Text = "0.0%",
                Visible = true
            };
            notifyIcon.DoubleClick += new EventHandler(HandleDoubleClick);

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
            using (RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName))
            {
                return rKey.GetValue(Application.ProductName) != null;
            }
        }

        private string GetAppsUseTheme()
        {
            string keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using (RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName))
            {
                object value;
                if (rKey == null || (value = rKey.GetValue("SystemUsesLightTheme")) == null)
                {
                    Console.WriteLine("Oh No! Couldn't get theme light/dark");
                    return "light";
                }
                int theme = (int)value;
                return theme == 0 ? "dark" : "light";
            }
        }

        private void SetIcons()
        {
            string prefix = 0 < manualTheme.Length ? manualTheme : systemTheme;
            ResourceManager rm = Resources.ResourceManager;
            // default runner is cat
            int capacity = 5;
            if (runner.Equals("parrot"))
            {
                capacity = 10;
            } 
            else if (runner.Equals("horse")) 
            {
                capacity = 14;
            }
            List<Icon> list = new List<Icon>(capacity);
            for (int i = 0; i < capacity; i++)
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
            var item = sender as ToolStripMenuItem;
            UpdateCheckedState(item, runnerMenu);
            runner = item.Text.ToLower();
            SetIcons();
        }

        private void SetThemeIcons(object sender, EventArgs e)
        {
            UpdateCheckedState(sender as ToolStripMenuItem, themeMenu);
            manualTheme = string.Empty;
            systemTheme = GetAppsUseTheme();
            SetIcons();
        }

        private void SetSpeed()
        {
            minCPU = speed switch
            {
                "cpu 10%" => 100f,
                "cpu 20%" => 50f,
                "cpu 30%" => 33f,
                "cpu 40%" => 25f,
                _ => minCPU
            };
        }

        private void SetSpeedLimit(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            UpdateCheckedState(item, runnerSpeedLimit);
            speed = item.Text.ToLower();
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
            if (systemTheme.Equals(newTheme)) return;
            systemTheme = newTheme;
            SetIcons();
        }

        private void SetLightIcons(object sender, EventArgs e)
        {
            UpdateCheckedState(sender as ToolStripMenuItem, themeMenu);
            manualTheme = "light";
            SetIcons();
        }

        private void SetDarkIcons(object sender, EventArgs e)
        {
            UpdateCheckedState(sender as ToolStripMenuItem, themeMenu);
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
            string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using (RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                if (startupMenu.Checked)
                {
                    rKey.SetValue(Application.ProductName, Process.GetCurrentProcess().MainModule.FileName);
                }
                else
                {
                    rKey.DeleteValue(Application.ProductName, false);
                }
            }
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
            animateTimer.Tick += new EventHandler(AnimationTick);
        }

        private void CPUTickSpeed()
        {
            if (!speed.Equals("default"))
            {            
                float manualInterval = Math.Max(minCPU, interval);
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
        
        private void HandleDoubleClick(object Sender, EventArgs e)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell",
                UseShellExecute = false,
                Arguments = " -c Start-Process taskmgr.exe",
                CreateNoWindow = true,
            };
            Process.Start(startInfo);
        }

    }
}
