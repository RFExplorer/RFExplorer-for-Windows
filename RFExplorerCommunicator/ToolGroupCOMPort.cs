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
using System.Drawing;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace RFExplorerCommunicator
{
    public partial class ToolGroupCOMPort : UserControl
    {
        #region Properties

        private RFECommunicator m_objRFE;        //Reference to the running communicator, it contains data and status
        public RFECommunicator RFExplorer
        {
            get { return m_objRFE; }
            set 
                { 
                    m_objRFE = value;
                    UpdateButtonStatus();
                }
        }

        public string GroupBoxTitle
        {
            set
            {
                m_groupControl_Connection.Text = value;
            }
        }

        private string m_sDefaultCOMSpeed;       //RFExplorerClient.Properties.Settings.Default.COMSpeed or equivalent
        public string DefaultCOMSpeed
        {
            set 
                { 
                    m_sDefaultCOMSpeed = value;
                    m_ComboBaudRate.SelectedItem = m_sDefaultCOMSpeed;
                }
        }
        public bool IsCOMSpeedSelected
        {
            get { return m_ComboBaudRate.SelectedItem != null; }
        }
        public string COMSpeedSelected
        {
            get
                {
                    if (IsCOMSpeedSelected)
                        return m_ComboBaudRate.SelectedItem.ToString();
                    else
                        return "";
                }
        }

        private string m_sDefaultCOMPort;        //RFExplorerClient.Properties.Settings.Default.COMPort or equivalent
        public string DefaultCOMPort
        {
            set 
                { 
                    m_sDefaultCOMPort = value;
                    m_comboCOMPort.SelectedItem = m_sDefaultCOMPort;
                }
        }
        public bool IsCOMPortSelected
        {
            get { return m_comboCOMPort.Items.Count > 0 && m_comboCOMPort.SelectedValue!=null; }
        }
        public string COMPortSelected
        {
            get 
            { 
                if (IsCOMPortSelected)
                    return m_comboCOMPort.SelectedValue.ToString(); 
                else
                    return "";
            }
        }
        #endregion

        #region Constructor

        public ToolGroupCOMPort()
        {
            InitializeComponent();
            UpdateButtonStatus();
        }
        #endregion

        #region Public Events

        public event EventHandler PortConnected;
        private void OnPortConnected(EventArgs eventArgs)
        {
            if (PortConnected != null)
            {
                PortConnected(this, eventArgs);
            }
        }

        public event EventHandler PortClosed;
        private void OnPortClosed(EventArgs eventArgs)
        {
            if (PortClosed != null)
            {
                PortClosed(this, eventArgs);
            }
        }
        #endregion

        #region Public Methods

        public void ConnectPort()
        {
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                string csCOMPort = "";
                if (m_objRFE.ValidCP2101Ports != null && m_objRFE.ValidCP2101Ports.Length > 0)
                {
                    //there are valid ports available
                    if (m_objRFE.ValidCP2101Ports.Length == 1)
                    {
                        if (m_objRFE.PortNameExternal != m_objRFE.ValidCP2101Ports[0])
                        {
                            //if only one, ignore the selection from any combo and use what is available
                            csCOMPort = m_objRFE.ValidCP2101Ports[0];
                            m_sDefaultCOMPort = csCOMPort;
                            m_comboCOMPort.SelectedItem = m_sDefaultCOMPort;
                        }
                    }
                    else
                    {
                        //if more than one, try to use the one from the combo and otherwise fail
                        if ((m_comboCOMPort != null) && (m_comboCOMPort.Items.Count > 0) && (m_comboCOMPort.SelectedValue != null))
                        {
                            foreach (string sTestCOMPort in m_objRFE.ValidCP2101Ports)
                            {
                                if (sTestCOMPort == m_comboCOMPort.SelectedValue.ToString())
                                {
                                    csCOMPort = m_comboCOMPort.SelectedValue.ToString();
                                    break;
                                }
                            }
                        }
                    }
                }
                if (!String.IsNullOrEmpty(csCOMPort))
                {
                    m_objRFE.ConnectPort(csCOMPort, Convert.ToInt32(m_ComboBaudRate.SelectedItem.ToString()));

                    m_objRFE.HoldMode = false;
                    //m_groupControl_Connection.m_CollGroupBox.Collapsed = true;
                    UpdateButtonStatus();
                    OnPortConnected(new EventArgs());
                }
            }
            catch (Exception obEx)
            {
                Console.WriteLine(obEx.ToString());
            }

            Cursor.Current = Cursors.Default;
        }

        public void ClosePort()
        {
            Cursor.Current = Cursors.WaitCursor;
            m_objRFE.ClosePort();
            UpdateComboBox();
            UpdateButtonStatus();
            OnPortClosed(new EventArgs());
            Cursor.Current = Cursors.Default;
        }
                                  
        public void GetConnectedPorts()
        {
            Cursor.Current = Cursors.WaitCursor;
            m_comboCOMPort.DataSource = null;
            if (m_objRFE.GetConnectedPorts())
            {
                m_comboCOMPort.DataSource = m_objRFE.ValidCP2101Ports;
                m_comboCOMPort.SelectedItem = m_sDefaultCOMPort;
            }
            UpdateButtonStatus();
            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// Method to refresh correct Ports availble for device in ComboBox COM Ports
        /// </summary>
        public void UpdateComboBox()
        {
            //TODO: redesign for Linux / Mac
            string sPortExternal;

            if (!m_objRFE.PortConnected)
            {
                sPortExternal = m_objRFE.PortNameExternal;

                if ((sPortExternal != null) && (m_objRFE.ValidCP2101Ports != null))
                {
                    object objSelectedValue = m_comboCOMPort.SelectedItem;
                    string[] sPortsAvailble = m_objRFE.ValidCP2101Ports.Where(str => str != sPortExternal).ToArray();
                    m_comboCOMPort.DataSource = sPortsAvailble;
                    m_comboCOMPort.SelectedItem = objSelectedValue;
                }
                else
                    m_comboCOMPort.DataSource = m_objRFE.ValidCP2101Ports;
            }
        }

        public void UpdateButtonStatus()
        {
            if (m_objRFE != null)
            {
                this.Enabled = true;
                m_btnRescan.Enabled = !m_objRFE.PortConnected;
                m_btnConnect.Enabled = !m_objRFE.PortConnected && (m_comboCOMPort.Items.Count > 0);
                m_btnDisconnect.Enabled = m_objRFE.PortConnected;
                m_comboCOMPort.Enabled = !m_objRFE.PortConnected;
                m_ComboBaudRate.Enabled = !m_objRFE.PortConnected;
            }
            else
                this.Enabled = false;
        }

        /// <summary>
        /// Update layout of the internal controls and set the container
        /// </summary>
        public void UpdateUniversalLayout()
        {
            m_groupControl_Connection.m_ContainerForm = this;
            m_groupControl_Connection.SetUniversalLayout();
        }


        #endregion

        #region Private Events and methods

        private void OnRescan_Click(object sender, EventArgs e)
        {
            GetConnectedPorts();
            UpdateComboBox();
            UpdateButtonStatus();//Disabled button connect
        }        

        private void OnConnect_Click(object sender, EventArgs e)
        {
            ConnectPort();
        }

        private void OnDisconnect_Click(object sender, EventArgs e)
        {
            ClosePort();
        }
              
        private void ToolGroupCOMPort_Load(object sender, EventArgs e)
        {
            //UpdateUniversalLayout();
        }
        #endregion
    }

    internal class GroupControl_COMPort : System.Windows.Forms.GroupBox
    {
        internal ToolGroupCOMPort m_ContainerForm = null;
        internal CollapsibleGroupbox m_CollGroupBox = null;
        string m_sBasicText = "";

        /// <summary>
        /// Defines layout of the components regardless their prior position
        /// </summary>
        internal void SetUniversalLayout()
        {
            if (m_ContainerForm == null)
                return;

            if (Parent.Parent == null)
                return;

            this.AutoSize = true;
            if (Parent.Height > Parent.Parent.Height)
            {
                Parent.MinimumSize = new Size(this.Width + 1, Parent.Parent.Height-1);
                Parent.MaximumSize = new Size(this.Width + 2, Parent.Parent.Height);
                Parent.Height = Parent.Parent.Height;
            }

            int nTopMargin = (m_ContainerForm.Height - (m_ContainerForm.m_btnConnect.Height + m_ContainerForm.m_comboCOMPort.Height))/4;
            if (nTopMargin<10)
            {
                //text size scaled or something, make connect buttons smaller
                m_ContainerForm.m_btnConnect.Height = (int)(1.5 * m_ContainerForm.m_comboCOMPort.Height);
                m_ContainerForm.m_btnDisconnect.Height = m_ContainerForm.m_btnConnect.Height;
                nTopMargin = (m_ContainerForm.Height - (m_ContainerForm.m_btnConnect.Height + m_ContainerForm.m_comboCOMPort.Height)) / 4;
            }

            this.MaximumSize = new Size(this.Width, this.Parent.Height);
            this.MinimumSize = MaximumSize;

            m_ContainerForm.m_comboCOMPort.Top = 2*nTopMargin;
            m_ContainerForm.m_ComboBaudRate.Top = 2*nTopMargin;
            m_ContainerForm.m_btnRescan.Top = 2*nTopMargin-1;

            m_ContainerForm.m_btnConnect.Top = m_ContainerForm.m_comboCOMPort.Bottom + nTopMargin;
            m_ContainerForm.m_btnDisconnect.Top = m_ContainerForm.m_btnConnect.Top;
            m_ContainerForm.m_btnRescan.Height = m_ContainerForm.m_comboCOMPort.Height;
            m_ContainerForm.m_btnRescan.Top = m_ContainerForm.m_comboCOMPort.Top;

            if (m_CollGroupBox == null)
            {
                m_CollGroupBox = new CollapsibleGroupbox(this);
                if (!String.IsNullOrEmpty(Text))
                {
                    if (Text.Contains("Analyzer"))
                        m_sBasicText = "Analyzer ";
                    else
                        m_sBasicText = "Generator ";
                }
                CollapseBtn_Click(null, null); //to update status first time
                this.Paint += new System.Windows.Forms.PaintEventHandler(this.Collapse_Paint);
                m_CollGroupBox.CollapseButtonClick += new EventHandler(CollapseBtn_Click);
            }
        }

        private void CollapseBtn_Click(object sender, EventArgs e)
        {
            m_ContainerForm.MinimumSize = MinimumSize;
            m_ContainerForm.MaximumSize = MaximumSize;

            if (m_ContainerForm.RFExplorer.PortConnected)
                m_CollGroupBox.CollapsedCaption = m_sBasicText + "ON";
            else
                m_CollGroupBox.CollapsedCaption = m_sBasicText + "OFF";
        }

        private void Collapse_Paint(object sender, PaintEventArgs e)
        {
            m_CollGroupBox.Paint(e.Graphics);
        }
    }
}
