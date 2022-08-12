using RunCat.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace RunCat
{
    public partial class InfoForm : Form
    {
        private Timer timer=new Timer();
        private PerformanceCounter cpuUsage;
        private PerformanceCounter ramUsage;
        private double physicalMemory;
        private float interval;
        private double ramInterval;
        private Point position;
        Point mPoint;
        public InfoForm()
        {
            InitializeComponent();
            position = UserSettings.Default.MonitorPosition;
            cpuUsage = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
            _ = cpuUsage.NextValue(); // discards first return value
            ramUsage = new PerformanceCounter("Memory", "Available MBytes");
            _ = ramUsage.NextValue(); // discards first return value
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            var installedMemory = gcMemoryInfo.TotalAvailableMemoryBytes;
            physicalMemory = (double)installedMemory / 1024.0 / 1024.0 / 1024.0;
            timer.Tick += new EventHandler(MonitorSystem);
            timer.Interval = 1000;
            timer.Start();
        }
        private void SetCpuText(string infos)
        {
            //在这个线程中唤起控件
            if (this.Lbl_Cpu.InvokeRequired)
            {
                Action<string> action = new Action<string>(SetCpuText);
                Invoke(action, infos );
            }
            else
            {
                this.Lbl_Cpu.Text = infos;
            }
        }
        private void SetMemText(string infos)
        {
            //在这个线程中唤起控件
            if (this.Lbl_Mem.InvokeRequired)
            {
                Action<string> action = new Action<string>(SetMemText);
                Invoke(action, infos);
            }
            else
            {
                this.Lbl_Mem.Text = infos;
            }
        }
        private  void MonitorSystem(object obj,EventArgs args)
        {
            interval = Math.Min(100, cpuUsage.NextValue()); // Sometimes got over 100% so it should be limited to 100%
            ramInterval = ramUsage.NextValue() / 1024.0;
            SetCpuText($"CPU: {interval:f1}%");
            SetMemText($"Mem: {(physicalMemory-ramInterval):f1}/{physicalMemory:f1}G");
        }

        private void Container_MouseDown(object sender, MouseEventArgs e)
        {
            mPoint = new Point(e.X, e.Y);
        }
        private void Container_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                position = new Point(this.Location.X + e.X - mPoint.X, this.Location.Y + e.Y - mPoint.Y);
                this.Location = position;
                UserSettings.Default.MonitorPosition = position;
            }
        }
        private void Container_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                UserSettings.Default.MonitorPosition = position;
                UserSettings.Default.Save();
            }
        }
        private void InfoForm_Activated(object sender, EventArgs e)
        {
            this.Location = position;

        }
    }
}
