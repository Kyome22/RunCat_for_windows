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

    public class RunCatApplicationContext: ApplicationContext
    {
        private PerformanceCounter cpuUsage;
        private NotifyIcon notifyIcon;
        private int current = 0;
        private string theme = "";
        private Icon[] icons;
        private Timer animateTimer = new Timer();
        private Timer cpuTimer = new Timer();
        
      
        public RunCatApplicationContext()
        {
            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);

            cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            notifyIcon = new NotifyIcon()
            {
                Icon = Resources.light_cat0,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("Exit", Exit)
                }),
                Text = "0.0%",
                Visible = true
            };

            SetIcons();
            SetAnimation();
            ObserveCPUTick(null, EventArgs.Empty);
            StartObserveCPU();
            current = 1;
        }

        private string GetAppsUseTheme()
        {
            string keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            try
            {
                RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName);
                int theme = (int)rKey.GetValue("AppsUseLightTheme");
                rKey.Close();
                return theme == 0 ? "dark" : "light";
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("Oh No! Couldn't get theme light/dark");
                return "light";
            }
        }

        private void SetIcons()
        {
            string newTheme = GetAppsUseTheme();
            if (theme.Equals(newTheme)) return;
            theme = newTheme;
            ResourceManager rm = Resources.ResourceManager;
            icons = new List<Icon>
            {
                (Icon)rm.GetObject(theme + "_cat0"),
                (Icon)rm.GetObject(theme + "_cat1"),
                (Icon)rm.GetObject(theme + "_cat2"),
                (Icon)rm.GetObject(theme + "_cat3"),
                (Icon)rm.GetObject(theme + "_cat4")
            }
            .ToArray();
        }

        private void Exit(object sender, EventArgs e)
        {
            animateTimer.Stop();
            cpuTimer.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void SetAnimation()
        {
            animateTimer.Interval = 200;
            animateTimer.Tick += new EventHandler(AnimationTick);
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            notifyIcon.Icon = icons[current];
            current = (current + 1) % icons.Length;
        }

        private void StartObserveCPU()
        {
            cpuTimer.Interval = 3000;
            cpuTimer.Tick += new EventHandler(ObserveCPUTick);
            cpuTimer.Start();
        }

        private void ObserveCPUTick(object sender, EventArgs e)
        {
            float s = cpuUsage.NextValue();
            Console.WriteLine(s);
            notifyIcon.Text = String.Format("{0:#.#}%", s);
            s = 200.0f / (float)Math.Max(1.0f, Math.Min(20.0f, s / 5.0f));
            animateTimer.Stop();
            animateTimer.Interval = (int)s;
            animateTimer.Start();
        }

        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                SetIcons();
            }
        }

    }
}
