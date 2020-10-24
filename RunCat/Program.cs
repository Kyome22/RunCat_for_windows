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

namespace RunCat
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RunCatApplicationContext());
        }
    }

    public class RunCatApplicationContext : ApplicationContext
    {
        private PerformanceCounter cpuUsage;
        private MenuItem themeMenu;
        private MenuItem startupMenu;
        private NotifyIcon notifyIcon;
        private int current = 0;
        private string systemTheme = "";
        private string manualTheme = "";
        private Icon[] icons;
        private Timer animateTimer = new Timer();
        private Timer cpuTimer = new Timer();


        public RunCatApplicationContext()
        {
            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);

            cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ = cpuUsage.NextValue(); // discards first return value

            themeMenu = new MenuItem("Theme", new MenuItem[]
            {
                new MenuItem("Default", SetThemeIcons)
                {
                    RadioCheck = true,
                    Checked = true
                },
                new MenuItem("Light", SetLightIcons)
                {
                    RadioCheck = true
                },
                new MenuItem("Dark", SetDarkIcons)
                {
                    RadioCheck = true
                }
            });

            startupMenu = new MenuItem("Startup", SetStartup)
            {
                RadioCheck = true
            };
            if (IsStartupEnabled())
            {
                startupMenu.Checked = true;
            }

            notifyIcon = new NotifyIcon()
            {
                Icon = Resources.light_cat0,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    themeMenu,
                    startupMenu,
                    new MenuItem("Exit", Exit)
                }),
                Text = "0.0%",
                Visible = true
            };

            UpdateThemeIcons();
            SetAnimation();
            CPUTick();
            StartObserveCPU();
            current = 1;
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
            string prefix = manualTheme.Length > 0 ? manualTheme : systemTheme;
            ResourceManager rm = Resources.ResourceManager;
            icons = new List<Icon>
            {
                (Icon)rm.GetObject(prefix + "_cat0"),
                (Icon)rm.GetObject(prefix + "_cat1"),
                (Icon)rm.GetObject(prefix + "_cat2"),
                (Icon)rm.GetObject(prefix + "_cat3"),
                (Icon)rm.GetObject(prefix + "_cat4")
            }
            .ToArray();
        }

        private void UpdateCheckedState(MenuItem sender)
        {
            foreach (MenuItem item in themeMenu.MenuItems)
            {
                item.Checked = false;
            }
            sender.Checked = true;
        }

        private void SetThemeIcons(object sender, EventArgs e)
        {
            UpdateCheckedState((MenuItem)sender);
            manualTheme = "";
            systemTheme = GetAppsUseTheme();
            SetIcons();
        }

        private void UpdateThemeIcons()
        {
            if (0 < manualTheme.Length) return;
            string newTheme = GetAppsUseTheme();
            if (systemTheme.Equals(newTheme)) return;
            systemTheme = newTheme;
            SetIcons();
        }

        private void SetLightIcons(object sender, EventArgs e)
        {
            UpdateCheckedState((MenuItem)sender);
            manualTheme = "light";
            SetIcons();
        }

        private void SetDarkIcons(object sender, EventArgs e)
        {
            UpdateCheckedState((MenuItem)sender);
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
                    rKey.SetValue(Application.ProductName, Application.ExecutablePath);
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
            animateTimer.Stop();
            cpuTimer.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            notifyIcon.Icon = icons[current];
            current = (current + 1) % icons.Length;
        }

        private void SetAnimation()
        {
            animateTimer.Interval = 200;
            animateTimer.Tick += new EventHandler(AnimationTick);
        }

        private void CPUTick()
        {
            float s = cpuUsage.NextValue();
            notifyIcon.Text = $"{s:f1}%";
            s = 200.0f / (float)Math.Max(1.0f, Math.Min(20.0f, s / 5.0f));
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
            cpuTimer.Interval = 3000;
            cpuTimer.Tick += new EventHandler(ObserveCPUTick);
            cpuTimer.Start();
        }

    }
}
