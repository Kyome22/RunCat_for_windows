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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RunCatApplicationContext());
        }
    }

    public class RunCatApplicationContext : ApplicationContext
    {
        private PerformanceCounter cpuUsage;
        private ToolStripMenuItem runnerMenu;
        private ToolStripMenuItem themeMenu;
        private ToolStripMenuItem startupMenu;
        private NotifyIcon notifyIcon;
        private string runner = UserSettings.Default.Runner;
        private float scale = 200.0f;
        private int current = 0;
        private string systemTheme = "";
        private string manualTheme = UserSettings.Default.Theme;
        private Icon[] icons;
        private Timer animateTimer = new Timer();
        private Timer cpuTimer = new Timer();


        public RunCatApplicationContext()
        {
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
                new ToolStripMenuItem("bbbele", null, SetRunner)
                {
                    Checked = runner.Equals("bbbele")
                },
                new ToolStripMenuItem("bbbfus", null, SetRunner)
                {
                    Checked = runner.Equals("bbbfus")
                }
            });

            themeMenu = new ToolStripMenuItem("Theme", null, new ToolStripMenuItem[]
            {
                // them menu chon theme vao day
                //
                // new ToolStripMenuItem(ten_theme, null, SetThemeIcons)
                // {
                //     Checked = manualTheme.Equals(ten_theme)
                // },
                //

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

            ContextMenuStrip contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                runnerMenu,
                themeMenu,
                startupMenu,
                new ToolStripMenuItem("Exit", null, Exit)
            });

            notifyIcon = new NotifyIcon()
            {
                Icon = Resources.light_cat_0,
                ContextMenuStrip = contextMenuStrip,
                Text = "CPU: 0.0%",
                Visible = true
            };

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
            int capacity = 0;
            switch (runner){
                // them cac theme vao day
                // case ten_theme:{
                //      capacity: so_hinh_anh_cua_theme;
                //      scale: toc_do_chay_dang_float;
                // }
                // scale cang to, chay cang cham

                case "cat":
                    {
                        capacity = 5;
                        scale = 200.0f;
                        break;
                    }
                case "parrot":
                    {
                        capacity = 10;
                        scale = 200.0f;
                        break;
                    }
                case "bbbele":
                    {
                        capacity = 7;
                        scale = 888.8f;
                        break;
                    }
                case "bbbfus":
                    {
                        capacity = 4;
                        scale = 888.8f;
                        break;
                    }
            }
            Icon[] list = new Icon[capacity];
            for (int i = 0; i < capacity; i++)
            {
                list[i] = (Icon)rm.GetObject($"{prefix}_{runner}_{i}");
            }
            icons = list;
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
            animateTimer.Interval = 200;
            animateTimer.Tick += new EventHandler(AnimationTick);
        }

        private void CPUTick()
        {
            float s = cpuUsage.NextValue();
            notifyIcon.Text = $"CPU: {s:f1}%";
            s = scale / (float)Math.Max(1.0f, Math.Min(20.0f, s / 10.0f));
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
