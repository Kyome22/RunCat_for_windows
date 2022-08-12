namespace RunCat
{
    partial class InfoForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Lbl_Cpu = new System.Windows.Forms.Label();
            this.Lbl_Mem = new System.Windows.Forms.Label();
            this.container = new System.Windows.Forms.FlowLayoutPanel();
            this.container.SuspendLayout();
            this.SuspendLayout();
            // 
            // Lbl_Cpu
            // 
            this.Lbl_Cpu.BackColor = System.Drawing.SystemColors.Highlight;
            this.Lbl_Cpu.Enabled = false;
            this.Lbl_Cpu.Location = new System.Drawing.Point(0, 0);
            this.Lbl_Cpu.Margin = new System.Windows.Forms.Padding(0);
            this.Lbl_Cpu.Name = "Lbl_Cpu";
            this.Lbl_Cpu.Size = new System.Drawing.Size(100, 20);
            this.Lbl_Cpu.TabIndex = 0;
            this.Lbl_Cpu.Text = "CPU: 34.7%";
            this.Lbl_Cpu.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Lbl_Mem
            // 
            this.Lbl_Mem.BackColor = System.Drawing.SystemColors.Info;
            this.Lbl_Mem.Enabled = false;
            this.Lbl_Mem.Location = new System.Drawing.Point(100, 0);
            this.Lbl_Mem.Margin = new System.Windows.Forms.Padding(0);
            this.Lbl_Mem.Name = "Lbl_Mem";
            this.Lbl_Mem.Size = new System.Drawing.Size(140, 20);
            this.Lbl_Mem.TabIndex = 1;
            this.Lbl_Mem.Text = "Mem: 14.8/15.9G";
            this.Lbl_Mem.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // container
            // 
            this.container.BackColor = System.Drawing.Color.Transparent;
            this.container.Controls.Add(this.Lbl_Cpu);
            this.container.Controls.Add(this.Lbl_Mem);
            this.container.Location = new System.Drawing.Point(0, 0);
            this.container.Margin = new System.Windows.Forms.Padding(0);
            this.container.Name = "container";
            this.container.Size = new System.Drawing.Size(240, 20);
            this.container.TabIndex = 2;
            this.container.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Container_MouseDown);
            this.container.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Container_MouseMove);
            this.container.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Container_MouseUp);
            // 
            // InfoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(240, 20);
            this.ControlBox = false;
            this.Controls.Add(this.container);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "InfoForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Form1";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.SystemColors.Control;
            this.Activated +=new System.EventHandler(InfoForm_Activated);
            this.container.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label Lbl_Cpu;
        private System.Windows.Forms.Label Lbl_Mem;
        private System.Windows.Forms.FlowLayoutPanel container;
    }
}