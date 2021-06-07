namespace RFExplorerCommunicator
{
    partial class ToolGroupCOMPort
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.m_ToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.m_btnRescan = new System.Windows.Forms.Button();
            this.m_ComboBaudRate = new System.Windows.Forms.ComboBox();
            this.m_comboCOMPort = new System.Windows.Forms.ComboBox();
            this.m_btnConnect = new System.Windows.Forms.Button();
            this.m_btnDisconnect = new System.Windows.Forms.Button();
            this.m_groupControl_Connection = new RFExplorerCommunicator.GroupControl_COMPort();
            this.m_groupControl_Connection.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_ToolTip
            // 
            this.m_ToolTip.AutomaticDelay = 1500;
            this.m_ToolTip.AutoPopDelay = 15000;
            this.m_ToolTip.InitialDelay = 500;
            this.m_ToolTip.ReshowDelay = 300;
            // 
            // m_btnRescan
            // 
            this.m_btnRescan.AutoSize = true;
            this.m_btnRescan.Location = new System.Drawing.Point(93, 23);
            this.m_btnRescan.MaximumSize = new System.Drawing.Size(23, 21);
            this.m_btnRescan.Name = "m_btnRescan";
            this.m_btnRescan.Size = new System.Drawing.Size(23, 21);
            this.m_btnRescan.TabIndex = 11;
            this.m_btnRescan.Text = "*";
            this.m_ToolTip.SetToolTip(this.m_btnRescan, "Click to rescan all USB ports to find newly connected RF Explorer device");
            this.m_btnRescan.UseVisualStyleBackColor = true;
            this.m_btnRescan.Click += new System.EventHandler(this.OnRescan_Click);
            // 
            // m_ComboBaudRate
            // 
            this.m_ComboBaudRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.m_ComboBaudRate.Items.AddRange(new object[] {
            "2400",
            "500000"});
            this.m_ComboBaudRate.Location = new System.Drawing.Point(123, 23);
            this.m_ComboBaudRate.Name = "m_ComboBaudRate";
            this.m_ComboBaudRate.Size = new System.Drawing.Size(65, 21);
            this.m_ComboBaudRate.TabIndex = 12;
            this.m_ToolTip.SetToolTip(this.m_ComboBaudRate, "Select baudrate communication (suggested 500000)");
            // 
            // m_comboCOMPort
            // 
            this.m_comboCOMPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.m_comboCOMPort.Location = new System.Drawing.Point(6, 23);
            this.m_comboCOMPort.MinimumSize = new System.Drawing.Size(73, 0);
            this.m_comboCOMPort.Name = "m_comboCOMPort";
            this.m_comboCOMPort.Size = new System.Drawing.Size(82, 21);
            this.m_comboCOMPort.TabIndex = 10;
            this.m_ToolTip.SetToolTip(this.m_comboCOMPort, "Select COM port where RF Explorer device is connected");
            // 
            // m_btnConnect
            // 
            this.m_btnConnect.AutoSize = true;
            this.m_btnConnect.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.m_btnConnect.Font = new System.Drawing.Font("Tahoma", 8.75F);
            this.m_btnConnect.Location = new System.Drawing.Point(5, 49);
            this.m_btnConnect.Name = "m_btnConnect";
            this.m_btnConnect.Size = new System.Drawing.Size(94, 43);
            this.m_btnConnect.TabIndex = 13;
            this.m_btnConnect.Text = "Connect";
            this.m_ToolTip.SetToolTip(this.m_btnConnect, "Connect to RF Explorer device");
            this.m_btnConnect.Click += new System.EventHandler(this.OnConnect_Click);
            // 
            // m_btnDisconnect
            // 
            this.m_btnDisconnect.AutoSize = true;
            this.m_btnDisconnect.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.m_btnDisconnect.Font = new System.Drawing.Font("Tahoma", 8.75F);
            this.m_btnDisconnect.Location = new System.Drawing.Point(105, 49);
            this.m_btnDisconnect.Name = "m_btnDisconnect";
            this.m_btnDisconnect.Size = new System.Drawing.Size(83, 43);
            this.m_btnDisconnect.TabIndex = 41;
            this.m_btnDisconnect.Text = "Disconnect";
            this.m_ToolTip.SetToolTip(this.m_btnDisconnect, "Disconnect from RF Explorer device");
            this.m_btnDisconnect.Click += new System.EventHandler(this.OnDisconnect_Click);
            // 
            // m_groupControl_Connection
            // 
            this.m_groupControl_Connection.AutoSize = true;
            this.m_groupControl_Connection.Controls.Add(this.m_btnRescan);
            this.m_groupControl_Connection.Controls.Add(this.m_ComboBaudRate);
            this.m_groupControl_Connection.Controls.Add(this.m_comboCOMPort);
            this.m_groupControl_Connection.Controls.Add(this.m_btnConnect);
            this.m_groupControl_Connection.Controls.Add(this.m_btnDisconnect);
            this.m_groupControl_Connection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_groupControl_Connection.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.m_groupControl_Connection.Location = new System.Drawing.Point(0, 0);
            this.m_groupControl_Connection.Margin = new System.Windows.Forms.Padding(0);
            this.m_groupControl_Connection.Name = "m_groupControl_Connection";
            this.m_groupControl_Connection.Padding = new System.Windows.Forms.Padding(0);
            this.m_groupControl_Connection.Size = new System.Drawing.Size(195, 133);
            this.m_groupControl_Connection.TabIndex = 51;
            this.m_groupControl_Connection.TabStop = false;
            this.m_groupControl_Connection.Text = "COM Port";
            // 
            // ToolGroupCOMPort
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSize = true;
            this.Controls.Add(this.m_groupControl_Connection);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.MinimumSize = new System.Drawing.Size(195, 116);
            this.Name = "ToolGroupCOMPort";
            this.Size = new System.Drawing.Size(195, 133);
            this.Load += new System.EventHandler(this.ToolGroupCOMPort_Load);
            this.m_groupControl_Connection.ResumeLayout(false);
            this.m_groupControl_Connection.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private GroupControl_COMPort m_groupControl_Connection;
        internal System.Windows.Forms.Button m_btnRescan;
        internal System.Windows.Forms.ComboBox m_ComboBaudRate;
        internal System.Windows.Forms.Button m_btnConnect;
        internal System.Windows.Forms.Button m_btnDisconnect;
        internal System.Windows.Forms.ComboBox m_comboCOMPort;
        private System.Windows.Forms.ToolTip m_ToolTip;
    }
}
