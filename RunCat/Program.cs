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
            Application.Run(new RunCatApplicationContext());

            procMutex.ReleaseMutex();
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
        private NotifyIcon notifyIcon;
        private string runner = UserSettings.Default.Runner;
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
                Text = "0.0%",
                Visible = true
            };

            UpdateThemeIcons();
            SetAnimation();
            CPUTick();
            StartObserveCPU();
            current = 1;
        }

        /// <summary>
        /// Event that is triggered when the application is closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnApplicationExit(object sender, EventArgs e)
        {
            UserSettings.Default.Runner = runner;
            UserSettings.Default.Theme = manualTheme;
            UserSettings.Default.Save();
        }

        /// <summary>
        /// Checks if the startup-option is already enabled.
        /// </summary>
        /// <returns></returns>
        private bool IsStartupEnabled()
        {
            string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using (RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName))
            {
                return (rKey.GetValue(Application.ProductName) != null) ? true : false;
            }
        }

        /// <summary>
        /// Checks the globally selected theme.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Sets the icon.
        /// </summary>
        private void SetIcons()
        {
            string prefix = 0 < manualTheme.Length ? manualTheme : systemTheme;
            ResourceManager rm = Resources.ResourceManager;
            int capacity = runner.Equals("cat") ? 5 : 10;
            List<Icon> list = new List<Icon>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                list.Add((Icon)rm.GetObject($"{prefix}_{runner}_{i}"));
            }
            icons = list.ToArray();
        }

        /// <summary>
        /// Checks whether the individual elements of the menu are selected or not.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="menu"></param>
        private void UpdateCheckedState(ToolStripMenuItem sender, ToolStripMenuItem menu)
        {
            foreach (ToolStripMenuItem item in menu.DropDownItems)
            {
                item.Checked = false;
            }
            sender.Checked = true;
        }

        /// <summary>
        /// Sets the runner.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetRunner(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            UpdateCheckedState(item, runnerMenu);
            runner = item.Text.ToLower();
            SetIcons();
        }

        /// <summary>
        /// Sets theme icons.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetThemeIcons(object sender, EventArgs e)
        {
            UpdateCheckedState((ToolStripMenuItem)sender, themeMenu);
            manualTheme = "";
            systemTheme = GetAppsUseTheme();
            SetIcons();
        }

        /// <summary>
        /// Updates the theme icons, if necessary.
        /// </summary>
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

        /// <summary>
        /// Sets the icons to light theme.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetLightIcons(object sender, EventArgs e)
        {
            UpdateCheckedState((ToolStripMenuItem)sender, themeMenu);
            manualTheme = "light";
            SetIcons();
        }

        /// <summary>
        /// Sets the icons to dark theme.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetDarkIcons(object sender, EventArgs e)
        {
            UpdateCheckedState((ToolStripMenuItem)sender, themeMenu);
            manualTheme = "dark";
            SetIcons();
        }

        /// <summary>
        /// Gets called, whenever a user preference has changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General) UpdateThemeIcons();
        }

        /// <summary>
        /// Sets the startup option.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Exits the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Exit(object sender, EventArgs e)
        {
            cpuUsage.Close();
            animateTimer.Stop();
            cpuTimer.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// Gets called, whenever the animation timer has elapsed and updates the icon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimationTick(object sender, EventArgs e)
        {
            if (icons.Length <= current) current = 0;
            notifyIcon.Icon = icons[current];
            current = (current + 1) % icons.Length;
        }

        /// <summary>
        /// Sets the animation properties.
        /// </summary>
        private void SetAnimation()
        {
            animateTimer.Interval = ANIMATE_TIMER_DEFAULT_INTERVAL;
            animateTimer.Tick += new EventHandler(AnimationTick);
        }

        /// <summary>
        /// Gets the cpu usage, writes it to the icon and updates the animation timer.
        /// </summary>
        private void CPUTick()
        {
            float s = cpuUsage.NextValue();
            notifyIcon.Text = $"CPU: {s:f1}%";
            s = ANIMATE_TIMER_DEFAULT_INTERVAL / (float)Math.Max(1.0f, Math.Min(20.0f, s / 5.0f));
            animateTimer.Stop();
            animateTimer.Interval = (int)s;
            animateTimer.Start();
        }

        /// <summary>
        /// Gets called, whenever the cpu timer has elapsed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ObserveCPUTick(object sender, EventArgs e)
        {
            CPUTick();
        }

        /// <summary>
        /// Sets the cpu timer properties.
        /// </summary>
        private void StartObserveCPU()
        {
            cpuTimer.Interval = CPU_TIMER_DEFAULT_INTERVAL;
            cpuTimer.Tick += new EventHandler(ObserveCPUTick);
            cpuTimer.Start();
        }

    }
}
