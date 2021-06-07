//============================================================================
//RF Explorer for Windows - A Handheld Spectrum Analyzer for everyone!
//Copyright (C) 2010-20 RF Explorer Technologies SL, www.rf-explorer.com
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
using System.Diagnostics;
using System.Threading;

namespace RFExplorerCommunicator
{
    public partial class ToolGroupCOMPort : UserControl
    {
        private const string LINUX_PORT_PREFIX = "/dev/tty";                //Port prefix for Linux
        private const string MAC_PORT_PREFIX = "/dev/tty.SLAB_USBtoUART";   //Port prefix for macOS
        private const string USB_PREFIX = "USB";                            //Port prefix USB
        private const int HIGH_SPEED_PORT = 500000;                         //Port speed 500000 bps 
        private const int LOW_SPEED_PORT = 2400;
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
            get { return m_comboCOMPort.Items.Count > 0 && m_comboCOMPort.SelectedValue != null; }
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

        bool m_bUseAllBaudrates = false; //used to enable all baudrate speeds for advanced usage
        /// <summary>
        /// Used to enable all baudrate speeds for advanced usage
        /// </summary>
        public bool UseAllBaudrates
        {
            get
            {
                return m_bUseAllBaudrates;
            }

            set
            {
                m_bUseAllBaudrates = value;
                m_ComboBaudRate.Items.Clear();
                m_ComboBaudRate.Items.Add("2400");
                m_ComboBaudRate.Items.Add("500000");
                if (m_bUseAllBaudrates)
                {
                    m_ComboBaudRate.Items.Add("1200");
                    m_ComboBaudRate.Items.Add("4800");
                    m_ComboBaudRate.Items.Add("9600");
                    m_ComboBaudRate.Items.Add("19200");
                    m_ComboBaudRate.Items.Add("57600");
                    m_ComboBaudRate.Items.Add("115200");
                }
            }
        }

        DateTime m_LastTimePortConnected;
        /// <summary>
        /// Time capture of the last port connected, used to manage the time between port is
        /// connected until a RF Explorer device connected
        /// </summary>
        public DateTime LastTimePortConnected
        {
            set
            {
                m_LastTimePortConnected = value;
            }
            get
            {
                return m_LastTimePortConnected;
            }
        }

        #endregion

        #region Constructor

        public ToolGroupCOMPort()
        {
            InitializeComponent();
            this.Size = new Size(195, 116);     //Original size
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

        /// <summary>
        /// Connect the selected port
        /// </summary>
        public void ConnectPort()
        {
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                string sCOMPort = "";
                if (m_objRFE.ValidCP2101Ports != null && m_objRFE.ValidCP2101Ports.Length > 0)
                {
                    //there are valid ports available
                    if (m_objRFE.ValidCP2101Ports.Length == 1)
                    {
                        if (m_objRFE.PortNameExternal != m_objRFE.ValidCP2101Ports[0])
                        {
                            //if only one, ignore the selection from any combo and use what is available
                            sCOMPort = m_objRFE.ValidCP2101Ports[0];
                            m_sDefaultCOMPort = sCOMPort;
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
                                string sTestCOMPortName = "";
                                if (RFECommunicator.IsMacOSPlatform())
                                {
                                    sTestCOMPortName = sTestCOMPort.Replace(MAC_PORT_PREFIX, USB_PREFIX);
                                    if (sTestCOMPortName.Equals(USB_PREFIX)) //In MacOS USB 0 match with SLAB_USBtoUART
                                        sTestCOMPortName += "0";
                                }
                                else
                                    sTestCOMPortName = sTestCOMPort.Replace(LINUX_PORT_PREFIX, "");

                                if (sTestCOMPortName == m_comboCOMPort.SelectedValue.ToString())
                                {
                                    sCOMPort = m_comboCOMPort.SelectedValue.ToString();
                                    break;
                                }
                            }
                        }
                    }
                }
                if (!String.IsNullOrEmpty(sCOMPort))
                {
                    if (RFECommunicator.IsUnixLike())
                    {
                        if (RFECommunicator.IsMacOSPlatform())
                        {
                            if (!sCOMPort.StartsWith("/"))
                            {
                                if (sCOMPort.EndsWith(USB_PREFIX + "0"))  //USB0 match with /dev/tty.SLAB_USBtoUART
                                    sCOMPort = MAC_PORT_PREFIX;
                                else
                                {
                                    sCOMPort = sCOMPort.Replace(USB_PREFIX, MAC_PORT_PREFIX); //In MacOS USB 0 match with SLAB_USBtoUART
                                }
                            }
                        }
                        else
                        {
                            if (!sCOMPort.StartsWith("/"))
                            {
                                sCOMPort = LINUX_PORT_PREFIX + sCOMPort;
                            }
                        }
                    }
                }
                m_objRFE.ConnectPort(sCOMPort, Convert.ToInt32(m_ComboBaudRate.SelectedItem.ToString()), RFECommunicator.IsUnixLike(), RFECommunicator.IsUnixLike() && !RFECommunicator.IsMacOSPlatform());

                m_objRFE.HoldMode = false;
                UpdateButtonStatus();
                OnPortConnected(new EventArgs());
            }
            catch (Exception obEx)
            {
                Trace.WriteLine(obEx.ToString());
            }

            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// Close the active port
        /// </summary>
        public void ClosePort()
        {
            Cursor.Current = Cursors.WaitCursor;
            m_objRFE.ClosePort();
            Uncollapse();
            UpdateComboBox();
            UpdateButtonStatus();
            OnPortClosed(new EventArgs());
            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// Fill Combo box and internal containers with all available CP210x ports
        /// </summary>
        public void GetConnectedPorts()
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                if (m_objRFE.GetConnectedPorts())
                {
                    UpdateComboBox();
                    if (!String.IsNullOrEmpty(m_sDefaultCOMPort))
                        m_comboCOMPort.SelectedItem = m_sDefaultCOMPort;
                    else
                        m_comboCOMPort.SelectedIndex = 0;
                }
                else
                    m_comboCOMPort.DataSource = null;
                UpdateButtonStatus();
            }
            catch { }
            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// Refresh correct Ports available for device in ComboBox COM Ports
        /// If there is an available COM port already used by m_objRFE.PortNameExternal it will be ignored
        /// </summary>
        public void UpdateComboBox()
        {
            string sPortExternal;

            if (m_objRFE != null && !m_objRFE.PortConnected)
            {
                if (m_objRFE.ValidCP2101Ports != null)
                {
                    string sSelectedPort = "";
                    if (m_comboCOMPort.SelectedItem != null)
                        sSelectedPort = m_comboCOMPort.SelectedItem.ToString();
                    sPortExternal = m_objRFE.PortNameExternal;
                    string[] arrPortsAvailable = null;

                    if (!String.IsNullOrEmpty(sPortExternal))
                    {
                        //If exists external port associated to the connection, remove from available port list
                        arrPortsAvailable = m_objRFE.ValidCP2101Ports.Where(str => str != sPortExternal).ToArray();
                    }
                    else
                    {
                        arrPortsAvailable = m_objRFE.ValidCP2101Ports;
                    }

                    if (RFECommunicator.IsUnixLike())
                    {
                        if (RFECommunicator.IsMacOSPlatform())
                        {
                            //Mac replace string for USB port name
                            sSelectedPort = sSelectedPort.Replace(MAC_PORT_PREFIX, USB_PREFIX);
                            if (sSelectedPort.Equals(USB_PREFIX))
                                sSelectedPort += "0";
                            for (int nInd = 0; nInd < arrPortsAvailable.Length; nInd++)
                            {
                                arrPortsAvailable[nInd] = arrPortsAvailable[nInd].Replace(MAC_PORT_PREFIX, USB_PREFIX);
                                if (arrPortsAvailable[nInd].Equals(USB_PREFIX))
                                    arrPortsAvailable[nInd] += "0";
                            }
                        }
                        else
                        {
                            //Replace COM port names for Linux
                            sSelectedPort = sSelectedPort.Replace(LINUX_PORT_PREFIX, "");
                            for (int nInd = 0; nInd < arrPortsAvailable.Length; nInd++)
                                arrPortsAvailable[nInd] = arrPortsAvailable[nInd].Replace(LINUX_PORT_PREFIX, "");
                        }
                    }
                    m_comboCOMPort.DataSource = arrPortsAvailable;
                    m_comboCOMPort.SelectedItem = sSelectedPort;
                }
                else
                    m_comboCOMPort.DataSource = null;
            }
        }

        public void UpdateButtonStatus()
        {
            if (m_objRFE != null)
            {
                this.Enabled = true;

                m_btnConnect.Enabled = !m_objRFE.PortConnected && (m_comboCOMPort.Items.Count > 0);
                m_btnDisconnect.Enabled = m_objRFE.PortConnected;
                m_comboCOMPort.Enabled = !m_objRFE.PortConnected;
                m_btnRescan.Enabled = !m_objRFE.PortConnected;
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

        /// <summary>
        ///Collapse the groupbox programmatically
        /// </summary>
        public void Collapse()
        {
            m_groupControl_Connection.m_CollGroupBox.Collapsed = true;
        }

        /// <summary>
        ///Uncollapse the groupbox programmatically
        /// </summary>
        public void Uncollapse()
        {
            m_groupControl_Connection.m_CollGroupBox.Collapsed = false;
        }
        #endregion

        #region Private Events and methods

        private void Rescan()
        {
            GetConnectedPorts();
            UpdateComboBox();
            UpdateButtonStatus();//Disabled button connect
        }

        private void OnRescan_Click(object sender, EventArgs e)
        {
            if (m_btnRescan.Enabled)
                Rescan();
        }

        private void OnConnect_Click(object sender, EventArgs e)
        {
            if (m_btnConnect.Enabled)
                ConnectPort();
        }

        private void OnDisconnect_Click(object sender, EventArgs e)
        {
            if (m_btnDisconnect.Enabled)
                ClosePort();
        }

        private void ToolGroupCOMPort_Load(object sender, EventArgs e)
        {
            //Set ToolGroup Layout

            int nToolGroupWidth = m_btnDisconnect.Right + m_btnConnect.Left;
            this.Size = new Size(nToolGroupWidth, this.Height);
            Rescan();
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
                Parent.MinimumSize = new Size(this.Width + 1, Parent.Parent.Height - 1);
                Parent.MaximumSize = new Size(this.Width + 2, Parent.Parent.Height);
                Parent.Height = Parent.Parent.Height;
            }

            int nTopMargin = (m_ContainerForm.Height - (m_ContainerForm.m_btnConnect.Height + m_ContainerForm.m_comboCOMPort.Height)) / 4;
            if (nTopMargin < 10)
            {
                //text size scaled or something, make connect buttons smaller
                m_ContainerForm.m_btnConnect.Height = (int)(1.5 * m_ContainerForm.m_comboCOMPort.Height);
                m_ContainerForm.m_btnDisconnect.Height = m_ContainerForm.m_btnConnect.Height;
                nTopMargin = (m_ContainerForm.Height - (m_ContainerForm.m_btnConnect.Height + m_ContainerForm.m_comboCOMPort.Height)) / 4;
            }

            this.MaximumSize = new Size(this.Width, this.Parent.Height);
            this.MinimumSize = MaximumSize;

            m_ContainerForm.m_comboCOMPort.Top = 2 * nTopMargin;
            m_ContainerForm.m_ComboBaudRate.Top = 2 * nTopMargin;
            m_ContainerForm.m_btnRescan.Top = 2 * nTopMargin - 1;

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
