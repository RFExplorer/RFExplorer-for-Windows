//============================================================================
//RF Explorer for Windows - A Handheld Spectrum Analyzer for everyone!
//Copyright © 2010-13 Ariel Rocholl, www.rf-explorer.com
//
//This application is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 3.0 of the License, or (at your option) any later version.
//
//This software is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//General Public License for more details.
//
//You should have received a copy of the GNU General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//=============================================================================

namespace RFExplorerSimpleClient
{
    partial class SimpleMainForm
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
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.labelAmplitude = new System.Windows.Forms.Label();
            this.labelFrequency = new System.Windows.Forms.Label();
            this.labelSweeps = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnRescanPortsRFExplorer = new System.Windows.Forms.Button();
            this.comboBoxBaudrateRFExplorer = new System.Windows.Forms.ComboBox();
            this.btnConnectRFExplorer = new System.Windows.Forms.Button();
            this.btnDisconnectRFExplorer = new System.Windows.Forms.Button();
            this.comboBoxPortsRFExplorer = new System.Windows.Forms.ComboBox();
            this.m_chkDebug = new System.Windows.Forms.CheckBox();
            this.m_edRFEReportLog = new System.Windows.Forms.TextBox();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.labelAmplitude);
            this.groupBox2.Controls.Add(this.labelFrequency);
            this.groupBox2.Controls.Add(this.labelSweeps);
            this.groupBox2.Location = new System.Drawing.Point(278, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(200, 104);
            this.groupBox2.TabIndex = 60;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "RF Explorer live Data";
            // 
            // labelAmplitude
            // 
            this.labelAmplitude.AutoSize = true;
            this.labelAmplitude.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold);
            this.labelAmplitude.Location = new System.Drawing.Point(6, 50);
            this.labelAmplitude.Name = "labelAmplitude";
            this.labelAmplitude.Size = new System.Drawing.Size(59, 25);
            this.labelAmplitude.TabIndex = 58;
            this.labelAmplitude.Text = "dBM";
            // 
            // labelFrequency
            // 
            this.labelFrequency.AutoSize = true;
            this.labelFrequency.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelFrequency.Location = new System.Drawing.Point(6, 19);
            this.labelFrequency.Name = "labelFrequency";
            this.labelFrequency.Size = new System.Drawing.Size(61, 25);
            this.labelFrequency.TabIndex = 58;
            this.labelFrequency.Text = "MHZ";
            // 
            // labelSweeps
            // 
            this.labelSweeps.AutoSize = true;
            this.labelSweeps.Location = new System.Drawing.Point(6, 78);
            this.labelSweeps.Name = "labelSweeps";
            this.labelSweeps.Size = new System.Drawing.Size(57, 13);
            this.labelSweeps.TabIndex = 57;
            this.labelSweeps.Text = "Sweeps: 0";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnRescanPortsRFExplorer);
            this.groupBox1.Controls.Add(this.comboBoxBaudrateRFExplorer);
            this.groupBox1.Controls.Add(this.btnConnectRFExplorer);
            this.groupBox1.Controls.Add(this.btnDisconnectRFExplorer);
            this.groupBox1.Controls.Add(this.comboBoxPortsRFExplorer);
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(260, 104);
            this.groupBox1.TabIndex = 59;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "RF Explorer COM Port";
            // 
            // btnRescanPortsRFExplorer
            // 
            this.btnRescanPortsRFExplorer.AutoSize = true;
            this.btnRescanPortsRFExplorer.Location = new System.Drawing.Point(110, 18);
            this.btnRescanPortsRFExplorer.Name = "btnRescanPortsRFExplorer";
            this.btnRescanPortsRFExplorer.Size = new System.Drawing.Size(34, 23);
            this.btnRescanPortsRFExplorer.TabIndex = 11;
            this.btnRescanPortsRFExplorer.Text = "*";
            this.btnRescanPortsRFExplorer.UseVisualStyleBackColor = true;
            this.btnRescanPortsRFExplorer.Click += new System.EventHandler(this.btnRescanPortsRFExplorer_Click);
            // 
            // comboBoxBaudrateRFExplorer
            // 
            this.comboBoxBaudrateRFExplorer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxBaudrateRFExplorer.Items.AddRange(new object[] {
            "500000",
            "2400"});
            this.comboBoxBaudrateRFExplorer.Location = new System.Drawing.Point(150, 19);
            this.comboBoxBaudrateRFExplorer.Name = "comboBoxBaudrateRFExplorer";
            this.comboBoxBaudrateRFExplorer.Size = new System.Drawing.Size(92, 21);
            this.comboBoxBaudrateRFExplorer.TabIndex = 12;
            // 
            // btnConnectRFExplorer
            // 
            this.btnConnectRFExplorer.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnConnectRFExplorer.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnConnectRFExplorer.Location = new System.Drawing.Point(22, 48);
            this.btnConnectRFExplorer.Name = "btnConnectRFExplorer";
            this.btnConnectRFExplorer.Size = new System.Drawing.Size(107, 36);
            this.btnConnectRFExplorer.TabIndex = 13;
            this.btnConnectRFExplorer.Text = "Connect";
            this.btnConnectRFExplorer.Click += new System.EventHandler(this.btnConnectRFExplorer_Click);
            // 
            // btnDisconnectRFExplorer
            // 
            this.btnDisconnectRFExplorer.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnDisconnectRFExplorer.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDisconnectRFExplorer.Location = new System.Drawing.Point(135, 48);
            this.btnDisconnectRFExplorer.Name = "btnDisconnectRFExplorer";
            this.btnDisconnectRFExplorer.Size = new System.Drawing.Size(107, 36);
            this.btnDisconnectRFExplorer.TabIndex = 41;
            this.btnDisconnectRFExplorer.Text = "Disconnect";
            this.btnDisconnectRFExplorer.Click += new System.EventHandler(this.btnDisconnectRFExplorer_Click);
            // 
            // comboBoxPortsRFExplorer
            // 
            this.comboBoxPortsRFExplorer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortsRFExplorer.Location = new System.Drawing.Point(22, 19);
            this.comboBoxPortsRFExplorer.Name = "comboBoxPortsRFExplorer";
            this.comboBoxPortsRFExplorer.Size = new System.Drawing.Size(82, 21);
            this.comboBoxPortsRFExplorer.TabIndex = 10;
            // 
            // m_chkDebug
            // 
            this.m_chkDebug.AutoSize = true;
            this.m_chkDebug.Location = new System.Drawing.Point(12, 121);
            this.m_chkDebug.Name = "m_chkDebug";
            this.m_chkDebug.Size = new System.Drawing.Size(108, 17);
            this.m_chkDebug.TabIndex = 61;
            this.m_chkDebug.Text = "Show Debug info";
            this.m_chkDebug.UseVisualStyleBackColor = true;
            this.m_chkDebug.CheckedChanged += new System.EventHandler(this.m_chkDebug_CheckedChanged);
            // 
            // m_edRFEReportLog
            // 
            this.m_edRFEReportLog.BackColor = System.Drawing.Color.White;
            this.m_edRFEReportLog.Location = new System.Drawing.Point(12, 144);
            this.m_edRFEReportLog.Multiline = true;
            this.m_edRFEReportLog.Name = "m_edRFEReportLog";
            this.m_edRFEReportLog.ReadOnly = true;
            this.m_edRFEReportLog.Size = new System.Drawing.Size(466, 194);
            this.m_edRFEReportLog.TabIndex = 62;
            this.m_edRFEReportLog.Visible = false;
            // 
            // SimpleMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(492, 144);
            this.Controls.Add(this.m_edRFEReportLog);
            this.Controls.Add(this.m_chkDebug);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "SimpleMainForm";
            this.Text = "RF Explorer - simple automation sample";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SimpleMainForm_FormClosing);
            this.Load += new System.EventHandler(this.SimpleMainForm_Load);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label labelAmplitude;
        private System.Windows.Forms.Label labelFrequency;
        private System.Windows.Forms.Label labelSweeps;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnRescanPortsRFExplorer;
        private System.Windows.Forms.ComboBox comboBoxBaudrateRFExplorer;
        private System.Windows.Forms.Button btnConnectRFExplorer;
        private System.Windows.Forms.Button btnDisconnectRFExplorer;
        private System.Windows.Forms.ComboBox comboBoxPortsRFExplorer;
        private System.Windows.Forms.CheckBox m_chkDebug;
        private System.Windows.Forms.TextBox m_edRFEReportLog;
    }
}

