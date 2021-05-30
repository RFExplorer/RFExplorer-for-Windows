//============================================================================
//RF Explorer for Windows - A Handheld Spectrum Analyzer for everyone!
//Copyright © 2010-21 RF Explorer Technologies SL, www.rf-explorer.com
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using RFExplorerCommunicator;

namespace RFExplorerSimpleClient
{
    public partial class SimpleMainForm : Form
    {
        #region Members
        System.Windows.Forms.Timer m_RefreshTimer = new System.Windows.Forms.Timer();
        string m_sRFEReceivedString = "";
        RFECommunicator m_objRFE;
        #endregion

        #region Main Form handling
        public SimpleMainForm()
        {
            InitializeComponent();

            m_objRFE = new RFECommunicator(true);
            m_objRFE.PortClosedEvent += new EventHandler(OnRFE_PortClosed);
            m_objRFE.ReportInfoAddedEvent += new EventHandler(OnRFE_ReportLog);
            m_objRFE.ReceivedConfigurationDataEvent += new EventHandler(OnRFE_ReceivedConfigData);
            m_objRFE.UpdateDataEvent += new EventHandler(OnRFE_UpdateData);
        }

        private void SimpleMainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_objRFE.Close();
        }

        private void SimpleMainForm_Load(object sender, EventArgs e)
        {
            GetConnectedPortsRFExplorer(); 
            UpdateButtonStatus();

            m_RefreshTimer.Enabled = true;
            m_RefreshTimer.Interval = 100;
            m_RefreshTimer.Tick += new System.EventHandler(this.RefreshTimer_tick);
        }
        #endregion

        #region RFExplorer Events
        private void OnRFE_ReceivedConfigData(object sender, EventArgs e)
        {
            ReportDebug(m_sRFEReceivedString);
            m_objRFE.SweepData.CleanAll(); //we do not want mixed data sweep values
        }

        private void OnRFE_UpdateData(object sender, EventArgs e)
        {
            labelSweeps.Text = "Sweeps: " + m_objRFE.SweepData.Count.ToString();

            RFESweepData objData = m_objRFE.SweepData.GetData(m_objRFE.SweepData.Count - 1);
            if (objData != null)
            {
                UInt16 nPeak = objData.GetPeakStep();

                labelFrequency.Text = objData.GetFrequencyMHZ(nPeak).ToString("f3") + " MHZ";
                labelAmplitude.Text = objData.GetAmplitudeDBM(nPeak).ToString("f2") + " dBm";
            }
        }

        private void OnRFE_ReportLog(object sender, EventArgs e)
        {
            EventReportInfo objArg = (EventReportInfo)e;
            ReportDebug(objArg.Data);
        }

        private void ReportDebug(string sLine)
        {
            if (!m_edRFEReportLog.IsDisposed && !m_chkDebug.IsDisposed && m_chkDebug.Checked)
            {
                if (sLine.Length > 0)
                    m_edRFEReportLog.AppendText(sLine);
                m_edRFEReportLog.AppendText(Environment.NewLine);
            }
        }

        private void OnRFE_PortClosed(object sender, EventArgs e)
        {
            ReportDebug("RF Explorer PortClosed");
        }

        private void m_chkDebug_CheckedChanged(object sender, EventArgs e)
        {
            if (m_chkDebug.Checked)
            {
                this.Size = new Size(this.Size.Width, 400);
                m_edRFEReportLog.Visible = true;
            }
            else
            {
                this.Size = new Size(this.Size.Width, 187);
                m_edRFEReportLog.Visible = false;
            }
        }
        #endregion

        #region RF Explorer handling
        private void UpdateButtonStatus()
        {
            btnConnectRFExplorer.Enabled = !m_objRFE.PortConnected && (comboBoxPortsRFExplorer.Items.Count > 0);
            btnDisconnectRFExplorer.Enabled = m_objRFE.PortConnected;
            comboBoxPortsRFExplorer.Enabled = !m_objRFE.PortConnected;
            comboBoxBaudrateRFExplorer.Enabled = !m_objRFE.PortConnected;
            btnRescanPortsRFExplorer.Enabled = !m_objRFE.PortConnected;

            if (!m_objRFE.PortConnected)
            {
                if (comboBoxBaudrateRFExplorer.SelectedItem == null)
                {
                    comboBoxBaudrateRFExplorer.SelectedItem = "500000";
                }
            }
        }

        private void btnConnectRFExplorer_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            if (comboBoxPortsRFExplorer.Items.Count > 0)
            {
                m_objRFE.ConnectPort(comboBoxPortsRFExplorer.SelectedValue.ToString(), Convert.ToInt32(comboBoxBaudrateRFExplorer.SelectedItem.ToString()));
                if (m_objRFE.PortConnected)
                {
                    m_objRFE.SendCommand_RequestConfigData();
                }
                Thread.Sleep(2000);
                m_objRFE.ProcessReceivedString(true, out m_sRFEReceivedString);
            }
            UpdateButtonStatus();
            Cursor.Current = Cursors.Default;
        }

        private void btnDisconnectRFExplorer_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            m_objRFE.ClosePort();
            UpdateButtonStatus();
            Cursor.Current = Cursors.Default;
        }

        private void GetConnectedPortsRFExplorer()
        {
            Cursor.Current = Cursors.WaitCursor;
            comboBoxPortsRFExplorer.DataSource = null;
            if (m_objRFE.GetConnectedPorts())
            {
                comboBoxPortsRFExplorer.DataSource = m_objRFE.ValidCP2101Ports;
            }
            UpdateButtonStatus();
            Cursor.Current = Cursors.Default;
        }

        private void btnRescanPortsRFExplorer_Click(object sender, EventArgs e)
        {
            GetConnectedPortsRFExplorer();
        }

        private void RefreshTimer_tick(object sender, EventArgs e)
        {
            if (m_objRFE.PortConnected)
            {
                m_objRFE.ProcessReceivedString(true, out m_sRFEReceivedString);
            }
        }
        #endregion
    }
}
