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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;

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
        private Icon[] icons;
        private Timer animateTimer = new Timer();
        private Timer cpuTimer = new Timer();
        
      
        public RunCatApplicationContext()
        {
            cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            notifyIcon = new NotifyIcon()
            {
                Icon = Resources.cat0,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("Exit", Exit)
                }),
                Text = "0.0%",
                Visible = true
            };

            icons = new List<Icon>
            {
                Resources.cat0,
                Resources.cat1,
                Resources.cat2,
                Resources.cat3,
                Resources.cat4
            }
            .ToArray();

            SetAnimation();
            ObserveCPUTick(null, EventArgs.Empty);
            StartObserveCPU();
            current = 1;
        }

        /*private void NotifyIconMouseClick(object sender, MouseEventArgs e)
        {
            Console.WriteLine("click");
            if (e.Button == MouseButtons.Left)
            {
                Console.WriteLine("gomi");
                MethodInfo method = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                method.Invoke(notifyIcon, null);
            }
        }*/

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
            notifyIcon.Text = String.Format("{0:#.#}%", s);
            s = 200.0f / (float)Math.Max(1.0f, Math.Min(20.0f, s / 5.0f)); ;
            Console.WriteLine(s);
            animateTimer.Stop();
            animateTimer.Interval = (int)s;
            animateTimer.Start();
        }

    }
}
