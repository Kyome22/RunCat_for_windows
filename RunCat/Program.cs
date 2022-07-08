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
using System.Threading.Tasks;

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
        private const int ANIMATE_TIMER_DEFAULT_INTERVAL = 200;
        private PerformanceCounter cpuUsage;
        private ToolStripMenuItem runnerMenu;
        private ToolStripMenuItem themeMenu;
        private ToolStripMenuItem startupMenu;
        private ToolStripMenuItem runnerSpeedLimit;
        private ToolStripMenuItem cpuTimerIntervalMenu;
        private NotifyIcon notifyIcon;
        private string runner = "";
        private int cpuTimerInterval = UserSettings.Default.CpuTimerInterval;
        private int current = 0;
        private float minCPU;
        private float interval;
        private string systemTheme = "";
        private string manualTheme = UserSettings.Default.Theme;
        private string speed = UserSettings.Default.Speed;
        private Icon[] icons;
        private Timer cpuTimer = new Timer();


        public RunCatApplicationContext()
        {
            UserSettings.Default.Reload();
            runner = UserSettings.Default.Runner;
            manualTheme = UserSettings.Default.Theme;

            Application.ApplicationExit += new EventHandler(OnApplicationExit);

            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);

            cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ = cpuUsage.NextValue(); // discards first return value

            runnerMenu = new ToolStripMenuItem("Runner", null, new ToolStripMenuItem[]
            {
                new ToolStripMenuItem("Cat", null, SetRunner)
                {
                    Checked = runner.Equals("cat")
                },
                new ToolStripMenuItem("Parrot", null, SetRunner)
                {
                    Checked = runner.Equals("parrot")
                },
                new ToolStripMenuItem("Horse", null, SetRunner)
                {
                    Checked = runner.Equals("horse")
                }
            });

            themeMenu = new ToolStripMenuItem("Theme", null, new ToolStripMenuItem[]
            {
                new ToolStripMenuItem("Default", null, SetThemeIcons)
                {
                    Checked = manualTheme.Equals("")
                },
                new ToolStripMenuItem("Light", null, SetLightIcons)
                {
                    Checked = manualTheme.Equals("light")
                },
                new ToolStripMenuItem("Dark", null, SetDarkIcons)
                {
                    Checked = manualTheme.Equals("dark")
                }
            });

            startupMenu = new ToolStripMenuItem("Startup", null, SetStartup);
            if (IsStartupEnabled())
            {
                startupMenu.Checked = true;
            }

            runnerSpeedLimit = new ToolStripMenuItem("Runner Speed Limit", null, new ToolStripMenuItem[]
            {
                new ToolStripMenuItem("Default", null, SetSpeedLimit)
                {
                    Checked = speed.Equals("default")
                },
                new ToolStripMenuItem("CPU 10%", null, SetSpeedLimit)
                {
                    Checked = speed.Equals("cpu 10%")
                },
                new ToolStripMenuItem("CPU 20%", null, SetSpeedLimit)
                {
                    Checked = speed.Equals("cpu 20%")
                },
                new ToolStripMenuItem("CPU 30%", null, SetSpeedLimit)
                {
                    Checked = speed.Equals("cpu 30%")
                },
                new ToolStripMenuItem("CPU 40%", null, SetSpeedLimit)
                {
                    Checked = speed.Equals("cpu 40%")
                }
            });

            cpuTimerIntervalMenu = new ToolStripMenuItem("CPU Update Interval", null, new ToolStripMenuItem[]
            {
                new ToolStripMenuItem("3 Seconds", null, SetCpuTimerInterval)
                {
                    Checked = cpuTimerInterval.Equals(3000),
                    Tag = 3000
                },
                new ToolStripMenuItem("1 Second", null, SetCpuTimerInterval)
                {
                    Checked = cpuTimerInterval.Equals(1000),
                    Tag = 1000
                },
                new ToolStripMenuItem("0.5 Second", null, SetCpuTimerInterval)
                {
                    Checked = cpuTimerInterval.Equals(500),
                    Tag = 500
                },
                new ToolStripMenuItem("0.25 Second", null, SetCpuTimerInterval)
                {
                    Checked = cpuTimerInterval.Equals(250),
                    Tag = 250
                },
                new ToolStripMenuItem("0.05 Second", null, SetCpuTimerInterval)
                {
                    Checked = cpuTimerInterval.Equals(50),
                    Tag = 50
                },
            });

            ContextMenuStrip contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                runnerMenu,
                themeMenu,
                startupMenu,
                runnerSpeedLimit,
                cpuTimerIntervalMenu,
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
            SetSpeed();
            RefreshCpuTimerInterval();
            StartObserveCPU();
            AnimationTick();

            current = 1;
        }
        private void OnApplicationExit(object sender, EventArgs e)
        {
            UserSettings.Default.Runner = runner;
            UserSettings.Default.Theme = manualTheme;
            UserSettings.Default.Speed = speed;
            UserSettings.Default.CpuTimerInterval = cpuTimerInterval;
            UserSettings.Default.Save();
        }

        private bool IsStartupEnabled()
        {
            string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using (RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName))
            {
                return (rKey.GetValue(Application.ProductName) != null) ? true : false;
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

        private void SetCpuTimerInterval(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            UpdateCheckedState(item, cpuTimerIntervalMenu);
            cpuTimerInterval = (int)item.Tag;
            RefreshCpuTimerInterval();
        }

        private void SetRunner(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
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

        private void SetSpeed()
        {
            if (speed.Equals("default"))
                return;
            else if (speed.Equals("cpu 10%"))
                minCPU = 100f;
            else if (speed.Equals("cpu 20%"))
                minCPU = 50f;
            else if (speed.Equals("cpu 30%"))
                minCPU = 33f;    
            else if (speed.Equals("cpu 40%"))
                minCPU = 25f;   
        }

        private void SetSpeedLimit(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
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
                rKey.Close();
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            cpuUsage.Close();
            cpuTimer.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private async void AnimationTick()
        {
            if (icons.Length <= current) current = 0;
            notifyIcon.Icon = icons[current];
            current = (current + 1) % icons.Length;
            await Task.Delay((int) interval);
            _ = Task.Run(() => AnimationTick()); // Prevents stack overflow by recursive function call
        }

        private void CPUTick()
        {
            var rawInterval = cpuUsage.NextValue();
            notifyIcon.Text = $"CPU: {rawInterval:f1}%";
            rawInterval = 200.0f / (float)Math.Max(1.0f, Math.Min(20.0f, rawInterval / 5.0f));

            // Apply interval later to prevent using the raw CPU usage value before processing.
            if (!speed.Equals("default"))
                interval = Math.Max(minCPU, rawInterval);
            else
                interval = rawInterval;
        }
        private void ObserveCPUTick(object sender, EventArgs e)
        {
            CPUTick();
        }

        private void RefreshCpuTimerInterval() => cpuTimer.Interval = cpuTimerInterval;

        private void StartObserveCPU()
        {
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
