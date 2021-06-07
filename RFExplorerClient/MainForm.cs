//============================================================================
//RF Explorer for Windows - A Handheld Spectrum Analyzer for everyone!
//Copyright © 2010-16 Ariel Rocholl, www.rf-explorer.com
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
//Contributed by:
//       Manuel Ballesteros
//       Julian Calderon
//       David Cortes
//       Josef Jahn
//=============================================================================

//#define CALLSTACK_REALTIME
//#define CALLSTACK
//#define SUPPORT_EXPERIMENTAL
//#define DEBUG

using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZedGraph;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using RFExplorerCommunicator;
using RFEClientControls;
using System.Collections;

namespace RFExplorerClient
{
    enum RFExplorerFileType             //Different file types supported by the application
    {
        SweepDataFile,
        RemoteScreenFile,
        RemoteScreenRFSFile,
        WaterfallScreenshotFile,
        AnalyzerScreenshotFile,
        CumulativeCSVDataFile,
        SimpleCSVDataFile,
        LimitLineDataFile,
        PowerChannelScreenshotFile,
        SNANormalizedDataFile,
        SNATrackingS1PFile,
        SNATrackingDataFile,
        SNATrackingCSVFile,
        SNATrackingScreenshotFile,
        None
    };

    public partial class MainForm : Form
    {
        #region Constants
        private const uint MAX_MRU_FILES = 15;

        private const float m_fSizeX = 130;         //Size of the dump screen in pixels (128x64 + 2 border) + 20 height for text header
        private const float m_fSizeY = 66;

        private const string _MarkerEnabledSA = "MarkerEnabled";
        private const string _MarkerEnabledSNA = "MarkerEnabledSNA";
        private const string _Common_Settings = "Common_Settings";
        private const string _MarkerFrequencySA = "MarkerFrequency";
        private const string _MarkerTrackSignalSA = "MarkerTrackSignal";
        private const string _MarkerFrequencySNA = "MarkerFrequencySNA";
        private const string _MarkerTrackSignalSNA = "MarkerTrackSignalSNA";
        private const string _ViewMaxHold = "ViewMaxHold";
        private const string _ViewMax = "ViewMax";
        private const string _ViewMin = "ViewMin";
        private const string _ViewRT = "ViewRT";
        private const string _ViewAvg = "ViewAvg";
        private const string _Calculator = "Calculator";
        private const string _BottomAmp = "BottomAmp";
        private const string _TopAmp = "TopAmp";
        private const string _StepFreq = "StepFreq";
        private const string _StartFreq = "StartFreq";
        private const string _RFGenSteps = "RFGenSteps";
        private const string _RFGenStartFreq = "RFGenStartFreq";
        private const string _RFGenStopFreq = "RFGenStopFreq";
        private const string _RFGenPower = "RFGenPower";
        private const string _RFGenStepTime = "RFGenStepTime";
        private const string _Default = "Default";
        private const string _Name = "Name";
        private const string _OnlyIfConnected = "OnlyIfConnected"; //used for resources that should be displayed or enabled only if connected

        private const string _S1P_File_Selector = "RF Explorer S1P files (*.s1p)|*.s1p|All files (*.*)|*.*";
        private const string _CSV_File_Selector = "RF Explorer CSV files (*.csv)|*.csv|All files (*.*)|*.*";
        private const string _Filename_RFExplorer_Settings = "RFExplorer_Settings.xml";
        private const string _RFE_File_Selector = "RF Explorer Data files (*.rfe)|*.rfe|All files (*.*)|*.*";
        private const string _SNANORM_File_Selector = "RF Explorer Normalized Data files (*.snanorm)|*.snanorm|All files (*.*)|*.*";
        private const string _SNA_File_Selector = "RF Explorer SNA Tracking Data files (*.sna)|*.sna|All files (*.*)|*.*";
        private const string _RFS_File_Selector = "RF Explorer Screen files (*.rfs)|*.rfs|All files (*.*)|*.*";
        private const string _PNG_File_Selector = "Image PNG files (*.png)|*.png|All files (*.*)|*.*";
        private const string _RFL_File_Selector = "RF Explorer Limit Line Data files (*.rfl)|*.rfl|All files (*.*)|*.*";
        private const string _RFA_File_Selector = "RF Explorer Amplitude Data files (*.rfa)|*.rfa|All files (*.*)|*.*";

        private const string _Firmware = "Firmware";
        private const string _Windows = "Windows";
        private const string _Freq = "Freq";
        private const string _OffsetDB = "OffsetDB";
        private const string _Cal = "Cal";
        private const string _Span = "Span";
        private const string _Step = "Step";
        private const string _RBW = "RBW";
        private const string _MainBoard = "MainBoard";
        private const string _ExpansionBoard = "ExpansionBoard";
        private const string _RFEGEN_TRACKING_TITLE = "RF Explorer SNA Tracking";
        #endregion

        #region Data Members
        RFECommunicator m_objRFEAnalyzer;     //The one and only RFE analyzer connected object
        RFECommunicator m_objRFEGenerator;    //The one and only RFE generator connected object

        ToolGroupCOMPort m_ToolGroup_COMPortAnalyzer;
        ToolGroupCOMPort m_ToolGroup_COMPortGenerator;
        ToolGroupRFGenCW m_ToolGroup_RFGenCW = new ToolGroupRFGenCW();
        ToolGroupTraces m_ToolGroup_AnalyzerTraces = new ToolGroupTraces();
        ToolGroupAnalyzerMode m_ToolGroup_AnalyzerDataFeed = new ToolGroupAnalyzerMode();
        ToolGroupMarkers m_ToolGroup_Markers_SNA;
        ToolGroupMarkers m_ToolGroup_Markers_SA;
        ToolGroupRemoteScreen m_ToolGroup_RemoteScreen = new ToolGroupRemoteScreen();
        internal ToolGroupAnalyzerFreqSettings m_ToolGroup_AnalyzerFreqSettings = new ToolGroupAnalyzerFreqSettings();
        ToolGroupRFEGenTracking m_ToolGroup_RFEGenTracking = new ToolGroupRFEGenTracking();
        ToolGroupCommands m_ToolGroup_Commands = new ToolGroupCommands();
        ToolGroupRFEGenFreqSweep m_ToolGroupRFEGenFreqSweep = new ToolGroupRFEGenFreqSweep();
        ToolGroupRFEGenAmpSweep m_ToolGroupRFEGenAmplSweep = new ToolGroupRFEGenAmpSweep();

        string m_sLastSettingsLoaded = _Default;
        int m_nDrawingIteration = 0;        //Iteration counter to do regular updates on GUI

        DataSet m_DataSettings;             //Settings data collection
        //A global flag to indicate the properties were correctly read from file, otherwise we should not persist them later
        //as it may indicate some type of crash and damage default settings
        bool m_bPropertiesReadOk = false;
        //Used to alert about firmware version popup only once per session
        bool m_bVersionAlerted = false;

        public About_RFExplorer m_winAboutModeless; //About box modeless dialog

        string m_sStartFile = "";           //File sent as argument while loading application
        string m_sFilenameRFE = "";         //RFE data file name
        string m_sFilenameRFS = "";         //RFS screen file name
        string m_sReportFilePath = "";      //Path and name of the report log file

        bool m_bFirstTick = true;           //Used to put some text and guarantee action done once after mainform load
        bool m_bFirstText = true;           //First report text printed

        //Graphics objects cached to reduce drawing overhead
        Pen m_PenDarkBlue;
        Pen m_PenRed;
        Brush m_BrushDarkBlue;

        //Colors used in configuration and marker panels
        Color m_ColorPanelText;
        Color m_ColorPanelTextDisabled;
        Color m_ColorPanelTextHighlight;
        Color m_ColorPanelBackground;
        Panel m_panelSAMarkers;             //panel to display marker values in Spectrum analyzer
        Panel m_panelSAConfiguration;       //panel to display RF Explorer configuration settings
        Panel m_panelSNAMarkers;            //panel to display marker values in SNA
        Panel m_panelSNAConfiguration;      //panel to display RF Explorer configuration settings

        TextObj[] m_arrWiFiBarText;         //Text for the 13 Wifi channels
        TextObj m_StatusGraphText_Analyzer; //Configuration data text received from RF Explorer to show on graph
        TextObj m_StatusGraphText_Tracking; //Configuration data text used in Tracking mode to show on graph
        TextObj m_OverloadText;             //Alert message with ADC overload
        TextObj m_TrackingStatus;           //Indicate current status to the user
        TextObj m_TrackingProgressText;     //Indicate current tracking progress and sweep time
        string m_sUserDefinedText = "";     //Display user defined text

        bool m_bIsWinXP = false;            //True if it is a Windows XP platform, which has some GUI differences with Win7/Vista

        //Line curve item for the analyzer zed graph
        LineItem m_GraphLine_Avg, m_GraphLine_Min, m_GraphLine_Realtime, m_GraphLine_Max, m_GraphLine_MaxHold;
        LineItem m_GraphLine_Tracking_Normal, m_GraphLine_Tracking_Avg;
        //pair list used by the line curve items
        PointPairList m_PointList_Realtime, m_PointList_Max, m_PointList_Min, m_PointList_Avg, m_PointList_MaxHold;
        PointPairList m_PointList_Tracking_Normal, m_PointList_Tracking_Avg;
        //Bar curve used by the Wifi analyzer
        BarItem m_MaxBar;

        //Support for limit lines
        LineItem m_GraphLimitLineAnalyzer_Max, m_GraphLimitLineAnalyzer_Min, m_GraphLimitLineAnalyzer_Overload;
        LimitLine m_LimitLineAnalyzer_Max, m_LimitLineAnalyzer_Min, m_LimitLineAnalyzer_Overload;
        LineItem m_GraphLimitLineGenerator_Max, m_GraphLimitLineGenerator_Min;
        LimitLine m_LimitLineGenerator_Max, m_LimitLineGenerator_Min;

        //Button group list for easy handling
        Button[] m_arrAnalyzerButtonList = new Button[15];

        string m_sAppDataFolder = "";       //Default folder based on %APPDATA% to store log and report files
        string m_sDefaultDataFolder = "";   //Default folder to store CSV and RFE or other data files
        string m_sDefaultUserFolder = "";   //User defined folder to store CSV and RFE or other data files
        string m_sSettingsFile = "";        //Filename and path of the named settings file

        bool m_bPrintModeEnabled = false;   //Will be true when the painting is being done for printing, mainly used to remove black background
        RFESweepDataCollection m_WaterfallSweepMaxHold = null;
        bool m_bLayoutInitialized = false;  //True after Load Frame is completed

        MarkerCollection m_MarkersSA;         //Marker collection for Spectrum analyzer
        MarkerCollection m_MarkersSNA;        //marker collection for tracking SNA
        ToolStripMenuItem[] m_arrMarkersEnabledMenu;  //Menu items with "Enabled" marker entry
        ToolStripTextBox[] m_arrMarkersFrequencyMenu; //Menu items with frequency MHZ editable, note marker 0 entry is empty (not used)

        ZedGraphControl m_graphPowerChannel = null;
        GasGaugeRegion m_PowerChannelRegion_Low = null;
        GasGaugeRegion m_PowerChannelRegion_Medium = null;
        GasGaugeRegion m_PowerChannelRegion_High = null;
        GasGaugeNeedle m_PowerChannelNeedle = null;
        TextObj m_PowerChannelText;

        Bitmap m_ClipboardBitmap = null; //bitmap object used to copy into clipboard

        //The one and only reusable analyzer graph
        private ZedGraph.ZedGraphControl m_GraphSpectrumAnalyzer;
        //The graph for tracking generator
        private ZedGraph.ZedGraphControl m_GraphTrackingGenerator;

        //A multimedia sound player to notify important events
        System.Media.SoundPlayer m_SoundPlayer = new System.Media.SoundPlayer();

        //variable used as flag to make sure we do not restart a playing sound
        bool m_bSoundPlaying = false;

        //Dialog used to define a custom text
        UserDefinedText m_dlgUserDefinedText = new UserDefinedText();

        //Counter to store the method calls of AnalyzerReceivedDeviceModel
        int m_CounterAnalyzerReceivedDeviceModel = 0;

        /// <summary>
        /// Spectrum analyzer shared waterfall view
        /// </summary>
        RFEWaterfallGL.SharpGLForm m_objSAWaterfall=null;
        Panel m_objPanelSAWaterfall;
        /// <summary>
        /// Dedicated specific waterfall tab view control
        /// </summary>
        RFEWaterfallGL.SharpGLForm m_objMainWaterfall=null;
        Panel m_objPanelMainWaterfall;
        #endregion

        #region Main Window
        public MainForm(string sFile)
        {
#if CALLSTACK
            Console.WriteLine("CALLSTACK:MainForm");
#endif
            //Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Trace.AutoFlush = true;

            try
            {
                if (!String.IsNullOrEmpty(sFile))
                    m_sStartFile = sFile;

                SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
                SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                SetStyle(ControlStyles.UserPaint, true);
                InitializeComponent();
                CreateWaterfallControl();

                m_GraphSpectrumAnalyzer = new ZedGraph.ZedGraphControl();
                CreateRFGenTab();

                m_WaterfallSweepMaxHold = new RFESweepDataCollection(100, false);
                m_MarkersSA = new MarkerCollection();
                m_MarkersSA.Initialize();
                m_MarkersSNA = new MarkerCollection();
                m_MarkersSNA.Initialize();
                this.m_ToolGroup_Markers_SA = new ToolGroupMarkers(m_MarkersSA);
                this.m_ToolGroup_Markers_SNA = new ToolGroupMarkers(m_MarkersSNA);
                CreateMarkerMenu();

                m_objRFEAnalyzer = new RFECommunicator(true);
                m_objRFEAnalyzer.PortClosedEvent += new EventHandler(OnAnalyzerPortClosed);
                m_objRFEAnalyzer.PortConnectedEvent += new EventHandler(OnAnalyzerPortConnected);
                m_objRFEAnalyzer.ReportInfoAddedEvent += new EventHandler(OnAnalyzerReportLog);
                m_objRFEAnalyzer.ReceivedConfigurationDataEvent += new EventHandler(OnAnalyzerReceivedConfigData);
                m_objRFEAnalyzer.UpdateDataEvent += new EventHandler(OnAnalyzerUpdateData);
                m_objRFEAnalyzer.UpdateRemoteScreenEvent += new EventHandler(OnAnalyzerUpdateRemoteScreen);
                m_objRFEAnalyzer.ReceivedDeviceModelEvent += new EventHandler(OnAnalyzerReceivedDeviceModel);
                m_objRFEAnalyzer.UpdateDataTrakingNormalizationEvent += new EventHandler(OnAnalyzerUpdateDataTrakingNormalization);
                m_objRFEAnalyzer.UpdateDataTrakingEvent += new EventHandler(OnAnalyzerUpdateTrackingData);
                m_objRFEAnalyzer.ShowDetailedCOMPortInfo = false;

                m_ToolGroup_COMPortAnalyzer = new ToolGroupCOMPort();
                m_ToolGroup_COMPortAnalyzer.RFExplorer = m_objRFEAnalyzer;
                m_ToolGroup_COMPortAnalyzer.PortClosed += new EventHandler(OnAnalyzerButtons_PortClosed);
                m_ToolGroup_COMPortAnalyzer.PortConnected += new EventHandler(OnAnalyzerButtons_PortConnected);
                m_ToolGroup_COMPortAnalyzer.GroupBoxTitle = "COM Port Spectrum Analyzer";

                m_objRFEGenerator = new RFECommunicator(false);
                m_objRFEGenerator.PortClosedEvent += new EventHandler(OnGeneratorPortClosed);
                m_objRFEGenerator.PortConnectedEvent += new EventHandler(OnGeneratorPortConnected);
                m_objRFEGenerator.ReportInfoAddedEvent += new EventHandler(OnGeneratorReportLog);
                m_objRFEGenerator.ReceivedConfigurationDataEvent += new EventHandler(OnGeneratorReceivedConfigData);
                //m_objRFEGen.UpdateData += new EventHandler(OnRFE_UpdateData);
                m_objRFEGenerator.UpdateRemoteScreenEvent += new EventHandler(OnAnalyzerUpdateRemoteScreen);
                m_objRFEGenerator.ReceivedDeviceModelEvent += new EventHandler(OnGeneratorReceivedDeviceModel);
                m_objRFEGenerator.ShowDetailedCOMPortInfo = false;
                m_ToolGroup_COMPortGenerator = new ToolGroupCOMPort();
                m_ToolGroup_COMPortGenerator.RFExplorer = m_objRFEGenerator;
                m_ToolGroup_COMPortGenerator.PortClosed += new EventHandler(OnGeneratorButtons_PortClosed);
                m_ToolGroup_COMPortGenerator.PortConnected += new EventHandler(OnGeneratorButtons_PortConnected);
                m_ToolGroup_COMPortGenerator.GroupBoxTitle = "COM Port Signal Generator";

                printMainDocument.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(this.printDocument_PrintPage);
                printMainDocument.DocumentName = "RF Explorer";

                CreateToolgroups();
            }
            catch (Exception obEx)
            {
                string sReportFile = Environment.GetEnvironmentVariable("APPDATA") + "\\rfexplorer_crash.log";
                using (StreamWriter sr = new StreamWriter(sReportFile, true))
                {
                    sr.WriteLine(Environment.NewLine + Environment.NewLine +
                        "------------------------------------------------------------" + Environment.NewLine +
                        "Report log date " + DateTime.Now.ToString() + Environment.NewLine);
                    sr.WriteLine("Exception: " + obEx.ToString());
                    sr.Flush();
                }
            }
        }

        void CreateWaterfallControl()
        {
            // 
            // m_objMainWaterfall
            // 
            m_objMainWaterfall = new RFEWaterfallGL.SharpGLForm();
            m_objMainWaterfall.Analyzer = null;
            m_objMainWaterfall.AutoSize = true;
            m_objMainWaterfall.ContextMenuStrip = menuContextWaterfall;
            m_objMainWaterfall.DrawFloor = true;
            m_objMainWaterfall.DrawFPS = true;
            m_objMainWaterfall.DrawTitle = false;
            m_objMainWaterfall.Location = new System.Drawing.Point(0, 0);
            m_objMainWaterfall.Margin = new System.Windows.Forms.Padding(4);
            m_objMainWaterfall.Name = "m_objMainWaterfall";
            m_objMainWaterfall.PerspectiveModeView = RFEWaterfallGL.SharpGLForm.WaterfallPerspectives.Perspective1;
            m_objMainWaterfall.Size = new System.Drawing.Size(50, 50);
            m_objMainWaterfall.TabIndex = 0;
            m_objMainWaterfall.Transparent = true;
            // 
            // m_objPanelMainWaterfall
            // 
            m_objPanelMainWaterfall = new Panel();
            m_objPanelMainWaterfall.Controls.Add(this.m_objMainWaterfall);
            m_objPanelMainWaterfall.Location = new System.Drawing.Point(12, 140);
            m_objPanelMainWaterfall.Name = "m_objPanelMainWaterfall";
            m_objPanelMainWaterfall.Size = new System.Drawing.Size(912, 363);
            m_objPanelMainWaterfall.TabIndex = 1;
            m_objPanelMainWaterfall.BorderStyle = BorderStyle.None;
            m_tabWaterfall.Controls.Add(this.m_objPanelMainWaterfall);
            m_objMainWaterfall.Dock = DockStyle.Fill;

            // 
            // m_objSAWaterfall
            // 
            m_objSAWaterfall = new RFEWaterfallGL.SharpGLForm();
            m_objSAWaterfall.Analyzer = null;
            m_objSAWaterfall.AutoSize = true;
            m_objSAWaterfall.ContextMenuStrip = menuContextWaterfall;
            m_objSAWaterfall.DrawFloor = false;
            m_objSAWaterfall.DrawFPS = false;
            m_objSAWaterfall.DrawTitle = false;
            m_objSAWaterfall.Location = new Point(0, 0);
            m_objSAWaterfall.Margin = new Padding(0);
            m_objSAWaterfall.Padding = new Padding(0, 0, 10, 0);
            m_objSAWaterfall.Name = "m_objSAWaterfall";
            m_objSAWaterfall.PerspectiveModeView = RFEWaterfallGL.SharpGLForm.WaterfallPerspectives.Perspective2D;
            m_objSAWaterfall.Size = new Size(50, 50);
            m_objSAWaterfall.TabIndex = 0;
            m_objSAWaterfall.Transparent = false;
            // 
            // m_objPanelSAWaterfall
            //
            m_objPanelSAWaterfall = new Panel();
            m_objPanelSAWaterfall.Controls.Add(this.m_objSAWaterfall);
            m_objPanelSAWaterfall.Location = new System.Drawing.Point(12, 140);
            m_objPanelSAWaterfall.Name = "m_objPanelSAWaterfall";
            m_objPanelSAWaterfall.Size = new System.Drawing.Size(912, 363);
            m_objPanelSAWaterfall.TabIndex = 1;
            m_objPanelSAWaterfall.BorderStyle = BorderStyle.FixedSingle;
            m_objPanelSAWaterfall.Visible = false;
            m_tabSpectrumAnalyzer.Controls.Add(m_objPanelSAWaterfall);
            m_objSAWaterfall.Dock = DockStyle.Fill;
        }

        /// <summary>
        /// Create here all toolgroups except those for COM serial control
        /// </summary>
        void CreateToolgroups()
        {
            // 
            // m_ToolGroup_Markers_SA
            // 
            this.m_ToolGroup_Markers_SA.AutoSize = true;
            this.m_ToolGroup_Markers_SA.Location = new System.Drawing.Point(0, 0);
            this.m_ToolGroup_Markers_SA.Name = "m_ToolGroup_Markers_SA";
            this.m_ToolGroup_Markers_SA.MarkerChangedEvent += new System.EventHandler(this.OnMarkerChanged_SA);
            this.m_ToolGroup_Markers_SA.MarkerValueChangedEvent += new System.EventHandler(this.OnMarkerValueChanged_SA);

            // 
            // m_ToolGroup_Markers_SNA
            // 
            this.m_ToolGroup_Markers_SNA.AutoSize = true;
            this.m_ToolGroup_Markers_SNA.Location = new System.Drawing.Point(0, 0);
            this.m_ToolGroup_Markers_SNA.Name = "m_ToolGroup_Markers_SNA";
            this.m_ToolGroup_Markers_SNA.MarkerChangedEvent += new System.EventHandler(this.OnMarkerChanged_SNA);
            this.m_ToolGroup_Markers_SNA.MarkerValueChangedEvent += new System.EventHandler(this.OnMarkerValueChanged_SNA);

            // 
            // m_ToolGroup_RFGenCW
            // 
            this.m_ToolGroup_RFGenCW.AutoSize = true;
            this.m_ToolGroup_RFGenCW.Location = new System.Drawing.Point(0, 0);
            this.m_ToolGroup_RFGenCW.Name = "m_ToolGroup_RFGenCW";
            this.m_ToolGroup_RFGenCW.FrequencyChangeEvent += new System.EventHandler(this.OnGeneratorFrequencyChanged);
            this.m_ToolGroup_RFGenCW.PowerChangeEvent += new System.EventHandler(this.OnGeneratorPowerChange);
            this.m_ToolGroup_RFGenCW.StartStopEvent += new System.EventHandler(this.OnGeneratorStartStopClick);
            this.m_ToolGroup_RFGenCW.ReportInfoEvent += new System.EventHandler(this.OnToolGroupRFGenReportInfoChanged);
            // 
            // m_ToolGroup_AnalyzerTraces
            // 
            this.m_ToolGroup_AnalyzerTraces.AutoSize = true;
            this.m_ToolGroup_AnalyzerTraces.Average = false;
            this.m_ToolGroup_AnalyzerTraces.AxisLabels = false;
            this.m_ToolGroup_AnalyzerTraces.FillTrace = false;
            this.m_ToolGroup_AnalyzerTraces.Location = new Point(0, 0);
            this.m_ToolGroup_AnalyzerTraces.MaxHold = false;
            this.m_ToolGroup_AnalyzerTraces.MaxPeak = false;
            this.m_ToolGroup_AnalyzerTraces.Minimum = false;
            this.m_ToolGroup_AnalyzerTraces.Name = "m_ToolGroup_AnalyzerTraces";
            this.m_ToolGroup_AnalyzerTraces.Realtime = false;
            this.m_ToolGroup_AnalyzerTraces.ShowGrid = false;
            this.m_ToolGroup_AnalyzerTraces.Smooth = false;
            this.m_ToolGroup_AnalyzerTraces.ThickTrace = false;
            this.m_ToolGroup_AnalyzerTraces.ConfigurationChangeEvent += new System.EventHandler(this.OnTraceConfigurationChangeEvent);
            //
            //m_ToolGroup_AnalyzerDataFeed
            //
            this.m_ToolGroup_AnalyzerDataFeed.AutoSize = true;
            this.m_ToolGroup_AnalyzerDataFeed.Location = new Point(0, 0);
            this.m_ToolGroup_AnalyzerDataFeed.Name = "m_ToolGroup_AnalyzerDataFeed";
            this.m_ToolGroup_AnalyzerDataFeed.RunModeChangeEvent += new EventHandler(this.OnRunModeChanged);
            this.m_ToolGroup_AnalyzerDataFeed.HoldModeChangeEvent += new EventHandler(this.OnHoldModeChanged);
            this.m_ToolGroup_AnalyzerDataFeed.NumericSampleSAChangeEvent += new EventHandler(this.OnSweepIndexChanged);
            // 
            // m_ToolGroup_AnalyzerFreqSettings
            // 
            this.m_ToolGroup_AnalyzerFreqSettings.AutoSize = true;
            this.m_ToolGroup_AnalyzerFreqSettings.FreqCenter = 2435D;
            this.m_ToolGroup_AnalyzerFreqSettings.FreqSpan = 0D;
            this.m_ToolGroup_AnalyzerFreqSettings.FreqStart = 2433D;
            this.m_ToolGroup_AnalyzerFreqSettings.Margin = new Padding(4, 0, 0, 0);
            this.m_ToolGroup_AnalyzerFreqSettings.Location = new Point(0, 0);
            //this.m_ToolGroup_AnalyzerFreqSettings.MinimumSize = new Size(400, 116); //this toolgroup is the only one we size, all others will refer to this one
            this.m_ToolGroup_AnalyzerFreqSettings.Name = "m_ToolGroup_AnalyzerFreqSettings";
            this.m_ToolGroup_AnalyzerFreqSettings.SendAnalyzerConfigurationEvent += new System.EventHandler(this.OnSendAnalyzerConfiguration);
            this.m_ToolGroup_AnalyzerFreqSettings.ReportInfoEvent += new System.EventHandler(this.OnToolGroupReport);
            // 
            // m_ToolGroup_RFEGenTracking
            // 
            this.m_ToolGroup_RFEGenTracking.AutoSize = true;
            this.m_ToolGroup_RFEGenTracking.Location = new Point(0, 0);
            this.m_ToolGroup_RFEGenTracking.Margin = new Padding(4, 0, 0, 0);
            this.m_ToolGroup_RFEGenTracking.Name = "m_ToolGroup_RFEGenTracking";
            this.m_ToolGroup_RFEGenTracking.TabIndex = 63;
            this.m_ToolGroup_RFEGenTracking.NormalizeTrackingStartEvent += new EventHandler(this.OnNormalizeTrackingStartChanged);
            this.m_ToolGroup_RFEGenTracking.TrackingStopEvent += new EventHandler(this.OnTrackingStopChanged);
            this.m_ToolGroup_RFEGenTracking.TrackingStartEvent += new EventHandler(this.OnTrackingStartChanged);
            this.m_ToolGroup_RFEGenTracking.SNAOptionsChangeEvent += new EventHandler(this.m_ListSNAOptions_SelectedIndexChanged);
            // 
            // m_ToolGroup_Commands
            // 
            this.m_ToolGroup_Commands.AutoSize = true;
            this.m_ToolGroup_Commands.Location = new System.Drawing.Point(0, 0);
            this.m_ToolGroup_Commands.Name = "m_ToolGroup_Commands";
            this.m_ToolGroup_Commands.TabIndex = 51;
            this.m_ToolGroup_Commands.DebugChangeEvent += new EventHandler(this.OnDebug_CheckedChanged);
            this.m_ToolGroup_Commands.ReportInfoEvent += new System.EventHandler(this.OnToolGroupReport);
            this.m_ToolGroup_Commands.CustomCommandEvent += new System.EventHandler(this.OnCustomCommandProperties);
            // 
            // m_ToolGroupRFEGenFreqSweep
            // 
            this.m_ToolGroupRFEGenFreqSweep.AutoSize = true;
            this.m_ToolGroupRFEGenFreqSweep.Location = new System.Drawing.Point(0, 0);
            this.m_ToolGroupRFEGenFreqSweep.Name = "m_ToolGroupRFEGenFreqSweep";
            this.m_ToolGroupRFEGenFreqSweep.TabIndex = 63;
            this.m_ToolGroupRFEGenFreqSweep.StartFreqSweepEvent += new System.EventHandler(this.OnStartFreqSweep_Click);
            this.m_ToolGroupRFEGenFreqSweep.StopFreqSweepEvent += new System.EventHandler(this.OnStopSweep_Click);
            this.m_ToolGroupRFEGenFreqSweep.RFGenFreqSweepStepsLeaveEvent += new System.EventHandler(this.OnRFGenFreqSweepSteps_Leave);
            this.m_ToolGroupRFEGenFreqSweep.RFGenFreqSweepStepsLeaveEvent += new System.EventHandler(this.OnRFGenFreqSweepStart_Leave);
            this.m_ToolGroupRFEGenFreqSweep.RFGenFreqSweepStepsLeaveEvent += new System.EventHandler(this.OnRFGenFreqSweepStop_Leave);
            this.m_ToolGroupRFEGenFreqSweep.ReportInfoEvent += new System.EventHandler(this.OnToolGroupRFGenReportInfoChanged);
            // 
            // m_ToolGroupRFEGenAmplSweep
            // 
            this.m_ToolGroupRFEGenAmplSweep.AutoSize = true;
            this.m_ToolGroupRFEGenAmplSweep.Location = new System.Drawing.Point(0, 0);
            this.m_ToolGroupRFEGenAmplSweep.Name = "m_ToolGroupRFEGenFreqSweep";
            this.m_ToolGroupRFEGenAmplSweep.TabIndex = 64;
            this.m_ToolGroupRFEGenAmplSweep.StartAmplSweepEvent += new System.EventHandler(this.OnStartAmpSweep_Click);
            this.m_ToolGroupRFEGenAmplSweep.StopAmplSweepEvent += new System.EventHandler(this.OnStopSweep_Click);
            this.m_ToolGroupRFEGenAmplSweep.ReportInfoEvent += new System.EventHandler(this.OnToolGroupRFGenReportInfoChanged);
            //
            // m_ToolGroup_RemoteScreen
            //
            this.m_ToolGroup_RemoteScreen.Location = new System.Drawing.Point(400, 0);
            this.m_ToolGroup_RemoteScreen.Name = "m_ToolGroup_RemoteScreen";
            this.m_ToolGroup_RemoteScreen.Size = new System.Drawing.Size(387, 116);
            this.m_ToolGroup_RemoteScreen.TabIndex = 57;
            this.m_ToolGroup_RemoteScreen.NumeriZoomChangeEvent += new EventHandler(this.OnRemoteScreen_ZoomChanged);
            this.m_ToolGroup_RemoteScreen.ChkDumpScreenChangeEvent += new EventHandler(this.OnRemoteScreen_EnabledChanged);
            this.m_ToolGroup_RemoteScreen.ChkDumpHeaderChangeEvent += new EventHandler(this.OnRemoteScreen_HeaderChanged);
            this.m_ToolGroup_RemoteScreen.SaveImageChangeEvent += new EventHandler(this.OnSaveImage_Click);
        }

        private void OnUserDefinedText_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == m_dlgUserDefinedText.ShowDialog(this))
            {
                m_sUserDefinedText = m_dlgUserDefinedText.m_comboText.Text;
                UpdateTitleText_Analyzer();
                m_GraphSpectrumAnalyzer.Refresh();
                if (m_MainTab.SelectedTab == m_tabRFGen)
                    m_GraphTrackingGenerator.Refresh();
                if (m_dlgUserDefinedText.m_comboText.Items.Count > 0)
                {
                    if (!m_dlgUserDefinedText.m_comboText.Items.Contains(m_sUserDefinedText))
                    {
                        m_dlgUserDefinedText.m_comboText.Items.Add(m_sUserDefinedText);
                    }
                }
                else
                {
                    m_dlgUserDefinedText.m_comboText.Items.Add(m_sUserDefinedText);
                }
            }
        }

        private void printDocument_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            //PaintScreen(e.Graphics, e.MarginBounds, true);
            e.HasMorePages = false;
        }

        private void OnRFConnections_Click(object sender, EventArgs e)
        {
            DisplayGroups();
        }

        private void CreateSettingsSchema()
        {
            try
            {
                DataTable objTableCommon = m_DataSettings.Tables.Add(_Common_Settings);

                objTableCommon.Columns.Add(new DataColumn(_Name, System.Type.GetType("System.String")));
                objTableCommon.Columns.Add(new DataColumn(_StartFreq, System.Type.GetType("System.Double")));
                objTableCommon.Columns.Add(new DataColumn(_StepFreq, System.Type.GetType("System.Double")));
                objTableCommon.Columns.Add(new DataColumn(_TopAmp, System.Type.GetType("System.Double")));
                objTableCommon.Columns.Add(new DataColumn(_BottomAmp, System.Type.GetType("System.Double")));
                objTableCommon.Columns.Add(new DataColumn(_Calculator, System.Type.GetType("System.UInt16")));
                objTableCommon.Columns.Add(new DataColumn(_ViewAvg, System.Type.GetType("System.Boolean")));
                objTableCommon.Columns.Add(new DataColumn(_ViewRT, System.Type.GetType("System.Boolean")));
                objTableCommon.Columns.Add(new DataColumn(_ViewMin, System.Type.GetType("System.Boolean")));
                objTableCommon.Columns.Add(new DataColumn(_ViewMax, System.Type.GetType("System.Boolean")));
                objTableCommon.Columns.Add(new DataColumn(_ViewMaxHold, System.Type.GetType("System.Boolean")));
                for (int nInd = 1; nInd <= MarkerCollection.MAX_MARKERS; nInd++)
                {
                    objTableCommon.Columns.Add(new DataColumn(_MarkerEnabledSA + nInd.ToString(), System.Type.GetType("System.Boolean")));
                    objTableCommon.Columns.Add(new DataColumn(_MarkerEnabledSNA + nInd.ToString(), System.Type.GetType("System.Boolean")));
                    if (nInd != 1)
                    {
                        objTableCommon.Columns.Add(new DataColumn(_MarkerFrequencySA + nInd.ToString(), System.Type.GetType("System.Double")));
                        objTableCommon.Columns.Add(new DataColumn(_MarkerFrequencySNA + nInd.ToString(), System.Type.GetType("System.Double")));
                    }
                    else
                    {
                        objTableCommon.Columns.Add(new DataColumn(_MarkerTrackSignalSA, System.Type.GetType("System.UInt16")));
                        objTableCommon.Columns.Add(new DataColumn(_MarkerTrackSignalSNA, System.Type.GetType("System.UInt16")));
                    }
                }

                DataRow objRow = objTableCommon.NewRow();
                objRow[_Name] = _Default;
                objRow[_StartFreq] = 430.000;
                objRow[_StepFreq] = 0.500;
                objRow[_TopAmp] = 5;
                objRow[_BottomAmp] = -120;
                objRow[_Calculator] = 10;
                objRow[_ViewAvg] = true;
                objRow[_ViewRT] = true;
                objRow[_ViewMin] = false;
                objRow[_ViewMax] = false;
                objRow[_ViewMaxHold] = false;
                for (int nInd = 1; nInd <= MarkerCollection.MAX_MARKERS; nInd++)
                {
                    objRow[_MarkerEnabledSA + nInd.ToString()] = true;
                    objRow[_MarkerEnabledSNA + nInd.ToString()] = true;
                    if (nInd != 1)
                    {
                        objRow[_MarkerFrequencySA + nInd.ToString()] = 1000.0;
                        objRow[_MarkerFrequencySNA + nInd.ToString()] = 1000.0;
                    }
                    else
                    {
                        objRow[_MarkerTrackSignalSA] = 0;
                        objRow[_MarkerTrackSignalSNA] = 0;
                    }
                }
                objTableCommon.Rows.Add(objRow);

                m_DataSettings.WriteXml(m_sSettingsFile, XmlWriteMode.WriteSchema);
            }
            catch (Exception objEx)
            {
                MessageBox.Show(objEx.Message);
            }
        }

        private void GetStringArrayFromStringList(string sListInput, out string[] arrStrings)
        {
            arrStrings = null;
            if (sListInput.Length > 0)
            {
                arrStrings = sListInput.Split(';');
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void menuMRU_DropDownOpening(object sender, EventArgs e)
        {
            int nCounter = 0;
            //foreach (ToolStripMenuItem menuItem in menuMRU.DropDownItems)
            //{
            //    menuItem.Dispose();
            //}
            menuMRU.DropDownItems.Clear();
            string[] arrMRU = null;
            string sUsedMRU = ""; //helper string to limit stored files to MAX_MRU_FILES

            GetStringArrayFromStringList(Properties.Settings.Default.MRUList, out arrMRU);

            foreach (string sFile in arrMRU)
            {
                ToolStripMenuItem menuNew = new ToolStripMenuItem(sFile);
                menuNew.Name = "MRUFile_" + nCounter;
                menuNew.Click += new System.EventHandler(this.OnMRU_Click);
                menuMRU.DropDownItems.Add(menuNew);
                nCounter++;
                //check to limit to MAX_MRU_FILES files
                if (sUsedMRU.Length > 0)
                {
                    sUsedMRU = sUsedMRU + ";" + sFile;
                }
                else
                {
                    sUsedMRU = sFile;
                }
                if (nCounter > MAX_MRU_FILES)
                {
                    //too many files, we limit them to MAX_MRU_FILES
                    Properties.Settings.Default.MRUList = sUsedMRU;
                    break;
                }
            }
        }

        private void OnMRU_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuClicked = (ToolStripMenuItem)sender;
            LoadFileRFE(menuClicked.Text);
        }

        private void OnSmoothSignals_Click(object sender, EventArgs e)
        {
            if (m_objRFEAnalyzer.HoldMode)
                DisplaySpectrumAnalyzerData();
            UpdateButtonStatus();
        }

        private void OnSignalFill_Click(object sender, EventArgs e)
        {
            if (m_objRFEAnalyzer.HoldMode)
                DisplaySpectrumAnalyzerData();
            UpdateButtonStatus();
        }

        private void OnLoadSettings_Click(object sender, EventArgs e)
        {
            RestoreSettingsXML(menuComboSavedOptions.Text);
        }

        DateTime m_StartDebugTime = DateTime.Now;
        private void OnDebug_CheckedChanged(object sender, EventArgs e)
        {
            m_StartDebugTime = DateTime.Now;
            m_objMainWaterfall.DrawFPS = m_ToolGroup_Commands.DebugTraces;
        }

        private void menuComboSavedOptions_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnDelSettings.Enabled = menuComboSavedOptions.Text != _Default;
        }

        private void OnSaveSettings_Click(object sender, EventArgs e)
        {
            string sItem = menuComboSavedOptions.Text;
            SaveSettingsXML(sItem);
            PopulateReadedSettings();
            menuComboSavedOptions.SelectedItem = sItem;
        }

        private void OnDeleteSettings_Click(object sender, EventArgs e)
        {
            string sItem = menuComboSavedOptions.Text;
            DeleteSettingsXML(sItem);
            menuComboSavedOptions.Items.Remove(sItem);
            menuComboSavedOptions.SelectedItem = _Default;
        }

        private void DefineGraphColors()
        {
            try
            {
                GraphPane myPaneAnalyzer = m_GraphSpectrumAnalyzer.GraphPane;
                GraphPane myPaneTracking = m_GraphTrackingGenerator.GraphPane;

                myPaneTracking.Chart.Border.Color = Color.Gray;
                myPaneTracking.Chart.Border.IsAntiAlias = true;
                myPaneAnalyzer.Chart.Border.Color = Color.Gray;
                myPaneAnalyzer.Chart.Border.IsAntiAlias = true;

                // Enable scrollbars if needed
                m_GraphSpectrumAnalyzer.IsAutoScrollRange = true;

                m_objMainWaterfall.DarkMode = menuDarkMode.Checked;
                m_objSAWaterfall.DarkMode = menuDarkMode.Checked;

                Fill FillColorAnalyzer = new Fill(Color.White, Color.LightYellow, 90.0f);
                Fill FillColorTracking = new Fill(Color.White, Color.LightYellow, 90.0f);

                Color FontColor = Color.Black;
                Color BackgroundColor = Color.LightYellow;
                Color ShadowColor = Color.LightGray;
                Color StatusTextColor = Color.DarkBlue;
                Color WarningTextColor = Color.DarkRed;

                m_ColorPanelText = Color.DarkBlue;
                m_ColorPanelTextDisabled = Color.DarkGray;
                m_ColorPanelTextHighlight = Color.DarkRed;
                m_ColorPanelBackground = Color.White;

                if (m_bPrintModeEnabled)
                {
                    FillColorAnalyzer = new Fill(Color.White);
                    FillColorTracking = new Fill(Color.White);
                    FontColor = Color.Black;
                    BackgroundColor = Color.White;
                    WarningTextColor = Color.DarkRed;
                }
                else if (menuDarkMode.Checked)
                {
                    FillColorAnalyzer = new Fill(Color.Black);
                    FillColorTracking = new Fill(Color.Black);
                    FontColor = Color.White;
                    BackgroundColor = Color.DarkGray;
                    ShadowColor = Color.DarkRed;
                    StatusTextColor = Color.LightGray;
                    WarningTextColor = Color.Red;

                    m_ColorPanelText = Color.White;
                    m_ColorPanelTextDisabled = Color.LightGray;
                    m_ColorPanelTextHighlight = Color.LightSalmon;
                    m_ColorPanelBackground = Color.Black;
                }

                this.BackColor = BackgroundColor;
                m_tabSpectrumAnalyzer.BackColor = BackgroundColor;
                m_tabReport.BackColor = BackgroundColor;
                m_tabRemoteScreen.BackColor = BackgroundColor;
                m_tabConfiguration.BackColor = BackgroundColor;
                m_tabWaterfall.BackColor = BackgroundColor;
                m_tabRFGen.BackColor = BackgroundColor;
                m_tabPowerChannel.BackColor = BackgroundColor;

                myPaneTracking.Fill = FillColorTracking;
                myPaneTracking.Chart.Fill = FillColorTracking;
                myPaneTracking.Title.FontSpec.FontColor = FontColor;
                myPaneTracking.YAxis.Title.FontSpec.FontColor = FontColor;
                myPaneTracking.XAxis.Title.FontSpec.FontColor = FontColor;
                myPaneTracking.YAxis.Scale.FontSpec.FontColor = FontColor;
                myPaneTracking.XAxis.Scale.FontSpec.FontColor = FontColor;
                myPaneTracking.Legend.FontSpec.FontColor = FontColor;
                myPaneTracking.Legend.Fill = FillColorTracking;

                myPaneAnalyzer.Fill = FillColorAnalyzer;
                myPaneAnalyzer.Chart.Fill = FillColorAnalyzer;
                myPaneAnalyzer.Title.FontSpec.FontColor = FontColor;
                myPaneAnalyzer.YAxis.Title.FontSpec.FontColor = FontColor;
                myPaneAnalyzer.XAxis.Title.FontSpec.FontColor = FontColor;
                myPaneAnalyzer.YAxis.Scale.FontSpec.FontColor = FontColor;
                myPaneAnalyzer.XAxis.Scale.FontSpec.FontColor = FontColor;
                myPaneAnalyzer.Legend.FontSpec.FontColor = FontColor;
                myPaneAnalyzer.Legend.Fill = FillColorAnalyzer;

                m_StatusGraphText_Analyzer.FontSpec.FontColor = StatusTextColor;
                m_StatusGraphText_Analyzer.FontSpec.DropShadowColor = ShadowColor;
                m_OverloadText.FontSpec.FontColor = WarningTextColor;
                m_TrackingStatus.FontSpec.Fill.Color = m_ColorPanelBackground;

                myPaneAnalyzer.YAxis.MajorGrid.Color = Color.Gray;
                myPaneAnalyzer.YAxis.MinorGrid.Color = Color.Gray;
                myPaneAnalyzer.XAxis.MajorGrid.Color = Color.Gray;
                myPaneAnalyzer.XAxis.MinorGrid.Color = Color.Gray;

                myPaneAnalyzer.YAxis.MajorTic.Color = Color.Gray;
                myPaneAnalyzer.YAxis.MinorTic.Color = Color.Gray;
                myPaneAnalyzer.XAxis.MajorTic.Color = Color.Gray;
                myPaneAnalyzer.XAxis.MinorTic.Color = Color.Gray;

                myPaneTracking.YAxis.MajorGrid.Color = Color.Gray;
                myPaneTracking.YAxis.MinorGrid.Color = Color.Gray;
                myPaneTracking.XAxis.MajorGrid.Color = Color.Gray;
                myPaneTracking.XAxis.MinorGrid.Color = Color.Gray;

                myPaneTracking.YAxis.MajorTic.Color = Color.Gray;
                myPaneTracking.YAxis.MinorTic.Color = Color.Gray;
                myPaneTracking.XAxis.MajorTic.Color = Color.Gray;
                myPaneTracking.XAxis.MinorTic.Color = Color.Gray;

                m_objPanelSAWaterfall.BackColor = m_ColorPanelBackground;

                // Update config and marker panels
                UpdateSAMarkerControlValues();
                UpdateSNAMarkerControlValues();
                UpdateConfigControlContents(m_panelSAConfiguration, m_objRFEAnalyzer);
                UpdateConfigControlContents(m_panelSNAConfiguration, m_objRFEGenerator);
            }
            catch (Exception obEx)
            {
                ReportLog(obEx.ToString(), true);
            }
        }

        private void InitializeControlArea()
        {
            //Define control bar
            m_tableLayoutControlArea.SuspendLayout();
            m_tableLayoutControlArea.AutoSize = true;
            m_tableLayoutControlArea.Font = new System.Drawing.Font("Tahoma", 8.25F);
            m_tableLayoutControlArea.ColumnCount = 15;
            for (int nIndCol = 0; nIndCol < m_tableLayoutControlArea.ColumnCount - 1; nIndCol++)
            {
                m_tableLayoutControlArea.ColumnStyles.Add(new ColumnStyle());
            }
            m_tableLayoutControlArea.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 10F)); //required to avoid wrong behavior of table layout when all columns are autosize
            m_tableLayoutControlArea.Location = new Point(6, 3);
            m_tableLayoutControlArea.Name = "m_tableLayoutControlArea";
            m_tableLayoutControlArea.RowCount = 1;
            m_tableLayoutControlArea.MinimumSize = new Size(10, 100);
            m_tableLayoutControlArea.Visible = true;

            m_tableLayoutControlArea.Controls.Clear();

            int nRowCount = 0;
            m_tableLayoutControlArea.Controls.Add(m_ToolGroup_COMPortAnalyzer, nRowCount++, 0);
            m_tableLayoutControlArea.Controls.Add(m_ToolGroup_COMPortGenerator, nRowCount++, 0);
            m_tableLayoutControlArea.Controls.Add(m_ToolGroup_AnalyzerDataFeed, nRowCount++, 0);
            m_tableLayoutControlArea.Controls.Add(m_ToolGroup_AnalyzerTraces, nRowCount++, 0);
            m_tableLayoutControlArea.Controls.Add(m_ToolGroup_AnalyzerFreqSettings, nRowCount++, 0);
            m_tableLayoutControlArea.Controls.Add(m_ToolGroup_Markers_SA, nRowCount++, 0);
            m_tableLayoutControlArea.Controls.Add(m_ToolGroup_RemoteScreen, nRowCount++, 0);
            m_tableLayoutControlArea.Controls.Add(m_ToolGroup_Commands, nRowCount++, 0);
            m_tableLayoutControlArea.Controls.Add(m_ToolGroup_RFGenCW, nRowCount++, 0);
            m_tableLayoutControlArea.Controls.Add(m_ToolGroupRFEGenFreqSweep, nRowCount++, 0);
            m_tableLayoutControlArea.Controls.Add(m_ToolGroupRFEGenAmplSweep, nRowCount++, 0);
            m_tableLayoutControlArea.Controls.Add(m_ToolGroup_RFEGenTracking, nRowCount++, 0);
            m_tableLayoutControlArea.Controls.Add(m_ToolGroup_Markers_SNA, nRowCount++, 0);
            //m_tableLayoutControlArea.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;

            m_ToolGroup_AnalyzerFreqSettings.RFCommunicator = m_objRFEAnalyzer;
            m_ToolGroup_AnalyzerFreqSettings.UpdateUniversalLayout();

            m_ToolGroup_AnalyzerDataFeed.RFEAnalyzer = m_objRFEAnalyzer;
            UpdateMinimumSize(m_ToolGroup_AnalyzerDataFeed);
            m_ToolGroup_AnalyzerDataFeed.UpdateUniversalLayout();

            m_ToolGroup_RFGenCW.RFEGenerator = m_objRFEGenerator;
            m_ToolGroup_RFGenCW.RFEAnalyzer = m_objRFEAnalyzer;
            UpdateMinimumSize(m_ToolGroup_RFGenCW);
            m_ToolGroup_RFGenCW.UpdateUniversalLayout();

            UpdateMinimumSize(m_ToolGroup_COMPortAnalyzer);
            m_ToolGroup_COMPortAnalyzer.UpdateUniversalLayout();

            UpdateMinimumSize(m_ToolGroup_COMPortGenerator);
            m_ToolGroup_COMPortGenerator.UpdateUniversalLayout();

            m_ToolGroup_RemoteScreen.ControlRemoteScreen = controlRemoteScreen;
            m_ToolGroup_RemoteScreen.Analyzer = m_objRFEAnalyzer;
            m_ToolGroup_RemoteScreen.Generator = m_objRFEGenerator;
            UpdateMinimumSize(m_ToolGroup_RemoteScreen);
            m_ToolGroup_RemoteScreen.UpdateUniversalLayout(m_ToolGroup_AnalyzerDataFeed.SweepIndexSize);

            UpdateMinimumSize(m_ToolGroup_AnalyzerTraces);
            m_ToolGroup_AnalyzerTraces.UpdateUniversalLayout();

            m_ToolGroup_Markers_SA.RFCommunicator = m_objRFEAnalyzer;
            UpdateMinimumSize(m_ToolGroup_Markers_SA);
            m_ToolGroup_Markers_SA.UpdateUniversalLayout();

            m_ToolGroup_Markers_SNA.RFCommunicator = m_objRFEGenerator;
            UpdateMinimumSize(m_ToolGroup_Markers_SNA);
            m_ToolGroup_Markers_SNA.UpdateUniversalLayout();

            m_ToolGroup_RFEGenTracking.RFEAnalyzer = m_objRFEAnalyzer;
            m_ToolGroup_RFEGenTracking.RFEGenerator = m_objRFEGenerator;
            UpdateMinimumSize(m_ToolGroup_RFEGenTracking);
            m_ToolGroup_RFEGenTracking.UpdateUniversalLayout();

            m_ToolGroup_Commands.RFEAnalyzer = m_objRFEAnalyzer;
            m_ToolGroup_Commands.RFEGenerator = m_objRFEGenerator;
            UpdateMinimumSize(m_ToolGroup_Commands);
            m_ToolGroup_Commands.UpdateUniversalLayout();

            m_ToolGroupRFEGenFreqSweep.RFEAnalyzer = m_objRFEAnalyzer;
            m_ToolGroupRFEGenFreqSweep.RFEGenerator = m_objRFEGenerator;
            UpdateMinimumSize(m_ToolGroupRFEGenFreqSweep);
            m_ToolGroupRFEGenFreqSweep.UpdateUniversalLayout();

            m_ToolGroupRFEGenAmplSweep.RFEGenerator = m_objRFEGenerator;
            UpdateMinimumSize(m_ToolGroupRFEGenAmplSweep);
            m_ToolGroupRFEGenAmplSweep.UpdateUniversalLayout();


            m_tableLayoutControlArea.ResumeLayout();
        }

        /// <summary>
        /// Define what minimum height would be based on reference size adjusted by m_ToolGroup_AnalyzerFreqSettings
        /// </summary>
        /// <param name="objToolGroup">ToolGroup to update</param>
        void UpdateMinimumSize(UserControl objToolGroup)
        {
#if TRACE
            string sReport = "UpdateMinimumSize (" + m_ToolGroup_AnalyzerFreqSettings.Height + "): " + objToolGroup.GetType().Name + " " + objToolGroup.Height;
#endif
            objToolGroup.Margin = m_ToolGroup_AnalyzerFreqSettings.Margin;
            //objToolGroup.BorderStyle = BorderStyle.FixedSingle; //only for debug
            objToolGroup.MinimumSize = new Size(objToolGroup.Width, m_ToolGroup_AnalyzerFreqSettings.Height);
            objToolGroup.Size = objToolGroup.MinimumSize;
#if TRACE
            Trace.WriteLine(sReport + " -> " + objToolGroup.Height);
#endif
        }

        private void InitializeSpectrumAnalyzerGraph()
        {
#if CALLSTACK
            Console.WriteLine("CALLSTACK:InitializeSpectrumAnalyzerGraph");
#endif
            m_tabSpectrumAnalyzer.Controls.Add(m_GraphSpectrumAnalyzer);
            m_GraphSpectrumAnalyzer.EditButtons = System.Windows.Forms.MouseButtons.Left;
            m_GraphSpectrumAnalyzer.IsAntiAlias = true;
            m_GraphSpectrumAnalyzer.IsEnableSelection = true;
            m_GraphSpectrumAnalyzer.Location = new System.Drawing.Point(8, 257);
            m_GraphSpectrumAnalyzer.Name = "zedSpectrumAnalyzer";
            m_GraphSpectrumAnalyzer.PanModifierKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.None)));
            m_GraphSpectrumAnalyzer.ScrollGrace = 0D;
            m_GraphSpectrumAnalyzer.ScrollMaxX = 0D;
            m_GraphSpectrumAnalyzer.ScrollMaxY = 0D;
            m_GraphSpectrumAnalyzer.ScrollMaxY2 = 0D;
            m_GraphSpectrumAnalyzer.ScrollMinX = 0D;
            m_GraphSpectrumAnalyzer.ScrollMinY = 0D;
            m_GraphSpectrumAnalyzer.ScrollMinY2 = 0D;
            m_GraphSpectrumAnalyzer.Size = new System.Drawing.Size(123, 54);
            m_GraphSpectrumAnalyzer.TabIndex = 49;
            m_GraphSpectrumAnalyzer.TabStop = false;
            m_GraphSpectrumAnalyzer.UseExtendedPrintDialog = true;
            m_GraphSpectrumAnalyzer.Visible = false;
            m_GraphSpectrumAnalyzer.ContextMenuBuilder += new ZedGraph.ZedGraphControl.ContextMenuBuilderEventHandler(this.objGraph_ContextMenuBuilder);
            m_GraphSpectrumAnalyzer.ZoomEvent += new ZedGraph.ZedGraphControl.ZoomEventHandler(this.zedSpectrumAnalyzer_ZoomEvent);

            // Get a reference to the GraphPane instance in the ZedGraphControl
            GraphPane myPane = m_GraphSpectrumAnalyzer.GraphPane;

            m_PointList_Realtime = new PointPairList();
            m_PointList_Max = new PointPairList();
            m_PointList_MaxHold = new PointPairList();
            m_PointList_Min = new PointPairList();
            m_PointList_Avg = new PointPairList();
            m_LimitLineAnalyzer_Max = new LimitLine();
            m_LimitLineAnalyzer_Min = new LimitLine();
            m_LimitLineAnalyzer_Overload = new LimitLine();

            m_GraphLimitLineAnalyzer_Max = m_GraphSpectrumAnalyzer.GraphPane.AddCurve("Limit Max", m_LimitLineAnalyzer_Max, Color.Magenta, SymbolType.Circle);
            m_GraphLimitLineAnalyzer_Max.Line.Width = 1;
            m_GraphLimitLineAnalyzer_Min = m_GraphSpectrumAnalyzer.GraphPane.AddCurve("Limit Min", m_LimitLineAnalyzer_Min, Color.DarkMagenta, SymbolType.Circle);
            m_GraphLimitLineAnalyzer_Min.Line.Width = 1;
            m_GraphLimitLineAnalyzer_Overload = m_GraphSpectrumAnalyzer.GraphPane.AddCurve("Overload", m_LimitLineAnalyzer_Overload, Color.DarkRed, SymbolType.None);
            m_GraphLimitLineAnalyzer_Overload.Line.Style = DashStyle.DashDot;
            m_GraphLimitLineAnalyzer_Overload.Line.Width = 1;

            m_MaxBar = m_GraphSpectrumAnalyzer.GraphPane.AddHiLowBar("Max", m_PointList_Max, Color.Red);
            m_MaxBar.Bar.Border.Color = Color.DarkRed;
            m_MaxBar.Bar.Border.Width = 3;
            m_GraphLine_Realtime = m_GraphSpectrumAnalyzer.GraphPane.AddCurve("Realtime", m_PointList_Realtime, m_MarkersSA.m_arrCurveColors[(int)RFECommunicator.RFExplorerSignalType.Realtime], SymbolType.None);
            m_GraphLine_Realtime.Line.Width = 4;
            m_GraphLine_Realtime.Line.SmoothTension = 0.2F;
            m_GraphLine_Min = m_GraphSpectrumAnalyzer.GraphPane.AddCurve("Min", m_PointList_Min, m_MarkersSA.m_arrCurveColors[(int)RFECommunicator.RFExplorerSignalType.Min], SymbolType.None);
            m_GraphLine_Min.Line.Width = 3;
            m_GraphLine_Min.Line.SmoothTension = 0.3F;
            m_GraphLine_Min.Line.Fill = new Fill(Color.DarkGreen, Color.LightGreen, 90);
            m_GraphLine_Avg = m_GraphSpectrumAnalyzer.GraphPane.AddCurve("Avg", m_PointList_Avg, m_MarkersSA.m_arrCurveColors[(int)RFECommunicator.RFExplorerSignalType.Average], SymbolType.None);
            m_GraphLine_Avg.Line.Width = 3;
            m_GraphLine_Avg.Line.SmoothTension = 0.3F;
            m_GraphLine_Avg.Line.Fill = new Fill(Color.Brown, Color.Salmon, 90);
            m_GraphLine_Max = m_GraphSpectrumAnalyzer.GraphPane.AddCurve("Max", m_PointList_Max, m_MarkersSA.m_arrCurveColors[(int)RFECommunicator.RFExplorerSignalType.MaxPeak], SymbolType.None);
            m_GraphLine_Max.Line.Width = 3;
            m_GraphLine_Max.Line.SmoothTension = 0.3F;
            m_GraphLine_Max.Line.Fill = new Fill(Color.Red, Color.Salmon, 90);
            m_GraphLine_MaxHold = m_GraphSpectrumAnalyzer.GraphPane.AddCurve("Max Hold", m_PointList_MaxHold, m_MarkersSA.m_arrCurveColors[(int)RFECommunicator.RFExplorerSignalType.MaxHold], SymbolType.None);
            m_GraphLine_MaxHold.Line.Width = 6;
            m_GraphLine_MaxHold.Line.SmoothTension = 0.3F;
            m_GraphLine_MaxHold.Line.Fill = new Fill(Color.Salmon, Color.LightSalmon, 90);

            foreach (CurveItem objCurve in m_GraphSpectrumAnalyzer.GraphPane.CurveList)
            {
                objCurve.IsVisible = false;
                objCurve.Label.IsVisible = false;
            }

            //Define button bar
            m_arrAnalyzerButtonList[0] = btnAutoscale;
            m_arrAnalyzerButtonList[1] = btnTop5plus;
            m_arrAnalyzerButtonList[2] = btnTop5minus;
            m_arrAnalyzerButtonList[3] = btnMoveFreqDecLarge;
            m_arrAnalyzerButtonList[4] = btnMoveFreqDecSmall;
            m_arrAnalyzerButtonList[5] = btnSpanInc;
            m_arrAnalyzerButtonList[6] = btnSpanMax;
            m_arrAnalyzerButtonList[7] = btnSpanDefault;
            m_arrAnalyzerButtonList[8] = btnCenterMark;
            m_arrAnalyzerButtonList[9] = btnSpanMin;
            m_arrAnalyzerButtonList[10] = btnSpanDec;
            m_arrAnalyzerButtonList[11] = btnMoveFreqIncLarge;
            m_arrAnalyzerButtonList[12] = btnMoveFreqIncSmall;
            m_arrAnalyzerButtonList[13] = btnBottom5plus;
            m_arrAnalyzerButtonList[14] = btnBottom5minus;

            btnCenterMark.Tag = (string)_OnlyIfConnected;
            btnSpanMin.Tag = (string)_OnlyIfConnected;
            btnSpanDefault.Tag = (string)_OnlyIfConnected;
            btnSpanMax.Tag = (string)_OnlyIfConnected;
            btnSpanDec.Tag = (string)_OnlyIfConnected;
            btnSpanInc.Tag = (string)_OnlyIfConnected;
            btnMoveFreqDecSmall.Tag = (string)_OnlyIfConnected;
            btnMoveFreqIncSmall.Tag = (string)_OnlyIfConnected;
            btnMoveFreqDecLarge.Tag = (string)_OnlyIfConnected;
            btnMoveFreqIncLarge.Tag = (string)_OnlyIfConnected;

            //Make all buttons same size, required for scaled text configurations
            for (int nInd = 0; nInd < m_arrAnalyzerButtonList.Length; nInd++)
            {
                m_arrAnalyzerButtonList[nInd].Width = btnBottom5plus.Width; //btnBottom5plus is currently the one with larger text
                m_arrAnalyzerButtonList[nInd].AutoSize = true;
                m_arrAnalyzerButtonList[nInd].AutoSizeMode = AutoSizeMode.GrowOnly;
            }

            CreateMarkerConfigPanel();

            //MessageBox.Show(btnSend.Width.ToString());

            // Set the titles and axis labels
            //myPane.Title.FontSpec.Size = 10;
            myPane.XAxis.Title.IsVisible = menuShowAxisLabels.Checked;
            myPane.XAxis.Title.Text = "Frequency (MHZ)";
            myPane.XAxis.Scale.MajorStep = 1.0;
            myPane.XAxis.Scale.MinorStep = 0.2;

            myPane.Margin.Left = 20;
            myPane.Margin.Right = -5;

            // Show the x axis grid
            myPane.XAxis.MajorGrid.IsVisible = true;
            myPane.XAxis.Type = AxisType.Linear;

            myPane.YAxis.Title.IsVisible = menuShowAxisLabels.Checked;
            // turn off the opposite tics so the Y tics don't show up on the Y2 axis
            myPane.YAxis.MajorTic.IsOpposite = false;
            myPane.YAxis.MinorTic.IsOpposite = false;
            myPane.YAxis.MajorGrid.IsVisible = true;
            // Don't display the Y zero line
            myPane.YAxis.MajorGrid.IsZeroLine = false;
            // Align the Y axis labels so they are flush to the axis
            myPane.YAxis.Scale.Align = AlignP.Inside;


            myPane.YAxis.Title.FontSpec.Size = 13;
            myPane.XAxis.Title.FontSpec.Size = 13;
            myPane.YAxis.Scale.FontSpec.Size = 10;
            myPane.XAxis.Scale.FontSpec.Size = 10;

            // Fill the axis background with a gradient
            myPane.Legend.IsHStack = true;
            myPane.Legend.FontSpec.Size = 12;

            m_GraphSpectrumAnalyzer.IsShowPointValues = true;
            m_GraphSpectrumAnalyzer.PointValueEvent += new ZedGraphControl.PointValueHandler(GraphPointValueHandler);

            myPane.BarSettings.Type = BarType.Overlay;
            myPane.BarSettings.MinBarGap = 0.1f;
            myPane.BarSettings.MinClusterGap = 0.1f;
            myPane.BarSettings.ClusterScaleWidthAuto = false;
            myPane.BarSettings.ClusterScaleWidth = 5.0f;

            m_StatusGraphText_Analyzer = new TextObj("RF Explorer DISCONNECTED", 0.01, 0.02, CoordType.ChartFraction);
            m_StatusGraphText_Analyzer.IsClippedToChartRect = true;
            //m_RFEConfig.ZOrder = 0;
            m_StatusGraphText_Analyzer.FontSpec.FontColor = Color.DarkGray;
            m_StatusGraphText_Analyzer.Location.AlignH = AlignH.Left;
            m_StatusGraphText_Analyzer.Location.AlignV = AlignV.Top;
            m_StatusGraphText_Analyzer.FontSpec.IsBold = false;
            m_StatusGraphText_Analyzer.FontSpec.Size = 10f;
            m_StatusGraphText_Analyzer.FontSpec.Border.IsVisible = false;
            m_StatusGraphText_Analyzer.FontSpec.Fill.IsVisible = false;
            m_StatusGraphText_Analyzer.FontSpec.StringAlignment = StringAlignment.Near;
            m_StatusGraphText_Analyzer.FontSpec.IsDropShadow = true;
            m_StatusGraphText_Analyzer.FontSpec.DropShadowOffset = 0.1f;
            m_StatusGraphText_Analyzer.FontSpec.Family = "Tahoma";
            myPane.GraphObjList.Add(m_StatusGraphText_Analyzer);

            m_OverloadText = new TextObj("RF LEVEL OVERLOAD - COMPRESSION", 0.99, 0.01, CoordType.ChartFraction);
            m_OverloadText.IsClippedToChartRect = true;
            //m_RFEConfig.ZOrder = 0;
            m_OverloadText.FontSpec.FontColor = Color.Red;
            m_OverloadText.Location.AlignH = AlignH.Right;
            m_OverloadText.Location.AlignV = AlignV.Top;
            m_OverloadText.FontSpec.IsBold = true;
            m_OverloadText.FontSpec.Size = 12f;
            m_OverloadText.FontSpec.Border.IsVisible = false;
            m_OverloadText.FontSpec.Fill.IsVisible = false;
            m_OverloadText.FontSpec.StringAlignment = StringAlignment.Center;
            m_OverloadText.FontSpec.IsDropShadow = false;
            m_OverloadText.FontSpec.Family = "Tahoma";
            m_OverloadText.IsVisible = false;
            myPane.GraphObjList.Add(m_OverloadText);

            m_arrWiFiBarText = new TextObj[13];
            m_arrWiFiBarText.Initialize();
            for (int nInd = 0; nInd < m_arrWiFiBarText.Length; nInd++)
            {
                m_arrWiFiBarText[nInd] = new TextObj("", 0, 0, CoordType.AxisXYScale);
                m_arrWiFiBarText[nInd].IsClippedToChartRect = true;
                m_arrWiFiBarText[nInd].Location.AlignH = AlignH.Center;
                m_arrWiFiBarText[nInd].Location.AlignV = AlignV.Bottom;
                m_arrWiFiBarText[nInd].FontSpec.Size = 7;
                m_arrWiFiBarText[nInd].FontSpec.Border.IsVisible = false;
                m_arrWiFiBarText[nInd].FontSpec.FontColor = Color.Red;
                m_arrWiFiBarText[nInd].FontSpec.StringAlignment = StringAlignment.Center;
                m_arrWiFiBarText[nInd].FontSpec.Fill.IsVisible = false;
                myPane.GraphObjList.Add(m_arrWiFiBarText[nInd]);
            }

            m_MarkersSA.ConnectToGraph(myPane);
        }

        private void OnAutoLCDOff_Click(object sender, EventArgs e)
        {
            if (menuAutoLCDOff.Checked)
            {
                m_objRFEAnalyzer.SendCommand_ScreenOFF();
                m_ToolGroup_RemoteScreen.CaptureDumpScreen = false;
            }
            else
            {
                m_objRFEAnalyzer.SendCommand_ScreenON();
            }
            UpdateButtonStatus();
        }

        private void OnPrintPreview_Click(object sender, EventArgs e)
        {
            m_bPrintModeEnabled = true;
            try
            {
                if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
                {
                    DefineGraphColors();
                    //zedSpectrumAnalyzer.DoPrintPreview() - not usable, it makes the dialog modeless and cannot set white color
                    printPreviewDialog.Document = m_GraphSpectrumAnalyzer.PrintDocument;
                }
                else if (m_MainTab.SelectedTab == m_tabRFGen)
                {
                    DefineGraphColors();
                    printPreviewDialog.Document = m_GraphTrackingGenerator.PrintDocument;
                }
                else
                {
                    printPreviewDialog.Document = printMainDocument;
                }

                if (printPreviewDialog.ShowDialog() == DialogResult.OK)
                {
                    printMainDocument.Print();
                }
            }
            catch (Exception obEx)
            {
                ReportLog(obEx.Message, false);
            }
            m_bPrintModeEnabled = false;
            DefineGraphColors();
        }

        /// <summary>
        /// A threaded version of the copy method to avoid crash with MTA
        /// </summary>         
        private void ClipboardCopyThread()
        {
            if (m_ClipboardBitmap != null)
            {
                Clipboard.SetDataObject(m_ClipboardBitmap, true);
                m_ClipboardBitmap.Dispose();
                m_ClipboardBitmap = null;
            }
        }

        private void OnClipboard_Click(object sender, EventArgs e)
        {
            //create bitmap
            SavePNG("");
            //Use thread to send bitmap to clipboard
            Thread ct = new Thread(new ThreadStart(this.ClipboardCopyThread));
            ct.SetApartmentState(ApartmentState.STA);
            ct.Start();
            ct.Join();
        }

        private void OnPrint_Click(object sender, EventArgs e)
        {
            m_bPrintModeEnabled = true;
            try
            {
                if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
                {
                    DefineGraphColors();
                    //zedSpectrumAnalyzer.DoPrintPreview() - not usable, it makes the dialog modeless and cannot set white color
                    m_GraphSpectrumAnalyzer.DoPrint();
                }
                else if (m_MainTab.SelectedTab == m_tabRFGen)
                {
                    DefineGraphColors();
                    m_GraphTrackingGenerator.DoPrint();
                }
            }
            catch (Exception obEx)
            {
                ReportLog(obEx.Message, false);
            }
            m_bPrintModeEnabled = false;
            DefineGraphColors();
        }

        private void OnPageSetup_Click(object sender, EventArgs e)
        {
            m_GraphSpectrumAnalyzer.DoPageSetup();
        }

        private string GetNewFilename(RFExplorerFileType FileType)
        {
            string sDate = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");

            string sResult = "";

            //New unique filename to store data based on date and time
            switch (FileType)
            {
                case RFExplorerFileType.None:
                case RFExplorerFileType.SweepDataFile:
                default:
                    m_sFilenameRFE = "RFExplorer_SweepData_" + sDate + RFECommunicator._RFE_File_Extension;
                    sResult = m_sFilenameRFE;
                    break;
                case RFExplorerFileType.WaterfallScreenshotFile:
                    sResult = "RFExplorer_Waterfall_" + sDate + RFECommunicator._PNG_File_Extension;
                    break;
                case RFExplorerFileType.RemoteScreenFile:
                    sResult = "RFExplorer_RemoteScreen_" + sDate + RFECommunicator._PNG_File_Extension;
                    break;
                case RFExplorerFileType.AnalyzerScreenshotFile:
                    sResult = "RFExplorer_SpectrumAnalyzer_" + sDate + RFECommunicator._PNG_File_Extension;
                    break;
                case RFExplorerFileType.PowerChannelScreenshotFile:
                    sResult = "RFExplorer_PowerChannelMeter_" + sDate + RFECommunicator._PNG_File_Extension;
                    break;
                case RFExplorerFileType.RemoteScreenRFSFile:
                    m_sFilenameRFS = "RFExplorer_RemoteScreen_" + sDate + RFECommunicator._RFS_File_Extension;
                    sResult = m_sFilenameRFS;
                    break;
                case RFExplorerFileType.CumulativeCSVDataFile:
                    sResult = "RFExplorer_MultipleSweepData_" + sDate + RFECommunicator._CSV_File_Extension;
                    break;
                case RFExplorerFileType.SimpleCSVDataFile:
                    sResult = "RFExplorer_SingleSweepData_" + sDate + RFECommunicator._CSV_File_Extension;
                    break;
                case RFExplorerFileType.LimitLineDataFile:
                    sResult = "RFExplorer_LimitLine_" + sDate + RFECommunicator._RFL_File_Extension;
                    break;
                case RFExplorerFileType.SNANormalizedDataFile:
                    sResult = "RFExplorer_SNANorm_" + sDate + RFECommunicator._SNANORM_File_Extension;
                    break;
                case RFExplorerFileType.SNATrackingCSVFile:
                    sResult = "RFExplorer_SNA_" + sDate + RFECommunicator._CSV_File_Extension;
                    break;
                case RFExplorerFileType.SNATrackingDataFile:
                    sResult = "RFExplorer_SNA_" + sDate + RFECommunicator._SNA_File_Extension;
                    break;
                case RFExplorerFileType.SNATrackingScreenshotFile:
                    sResult = "RFExplorer_SNA_" + sDate + RFECommunicator._PNG_File_Extension;
                    break;
                case RFExplorerFileType.SNATrackingS1PFile:
                    sResult = "RFExplorer_S1Port_" + sDate + RFECommunicator._S1P_File_Extension;
                    break;
            }

            return sResult;
        }

        private void InitializeOpenGL()
        {
            //Check OpenGL and disable Waterfall if not supported
            if (m_objMainWaterfall.OpenGL_Supported == false)
            {
                ReportLog(m_objMainWaterfall.InitializationError, true);
                ReportLog("ERROR: OpenGL 3D graphics are not supported in this system. Please check with your Graphics Card vendor. " + Environment.NewLine +
                    "Waterfall graphics require OpenGL. Waterfall graphics are disabled", false);
                m_objPanelMainWaterfall.Visible = false;
                m_objPanelMainWaterfall.Enabled = false;
                m_objPanelSAWaterfall.Visible = false;
                m_objPanelSAWaterfall.Enabled = false;
                System.Windows.Forms.Label warningLabel = new System.Windows.Forms.Label();
                warningLabel.Text = "Invalid graphics mode - 3D OpenGL not supported - check your video card";
                warningLabel.Location = m_objPanelMainWaterfall.Location;
                warningLabel.AutoSize = true;
                m_tabWaterfall.Controls.Add(warningLabel);
            }
            else
            {
                ReportLog("OpenGL 3D mode: " + m_objMainWaterfall.InternalOpenGLControl.RenderContextType +
                              " - version: " + m_objMainWaterfall.InternalOpenGLControl.OpenGL.Version +
                              " - vendor:" + m_objMainWaterfall.InternalOpenGLControl.OpenGL.Vendor +
                              " - renderer:" + m_objMainWaterfall.InternalOpenGLControl.OpenGL.Renderer
                         , false);
                //ReportLog("OpenGL supported extensions: " + controlWaterfall.InternalOpenGLControl.OpenGL.Extensions, true);
            }
            m_objMainWaterfall.Analyzer = m_objRFEAnalyzer;
            m_objSAWaterfall.Analyzer = m_objRFEAnalyzer;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void MainForm_Load(object sender, EventArgs e)
        {
#if CALLSTACK
            Console.WriteLine("CALLSTACK:MainForm_Load");
#endif
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                menuUseAmplitudeCorrection.Enabled = false; //disable this boy till a valid file is loaded

                toolStripMemory.Maximum = RFESweepDataCollection.MAX_ELEMENTS;
                toolStripMemory.Step = RFESweepDataCollection.MAX_ELEMENTS / 25;

                m_PenDarkBlue = new Pen(Color.DarkBlue, 1);
                m_PenRed = new Pen(Color.Red, 1);
                m_BrushDarkBlue = new SolidBrush(Color.DarkBlue);

                m_bIsWinXP = (Environment.OSVersion.Version.Major <= 5);

                btnLoadSettings.Left = menuComboSavedOptions.Control.Right + 5;
                btnSaveSettings.Left = btnLoadSettings.Right + 5;
                btnDelSettings.Left = btnSaveSettings.Right + 5;

                LoadProperties();
                m_ToolGroup_COMPortAnalyzer.GetConnectedPorts();
                m_ToolGroup_COMPortGenerator.GetConnectedPorts();

                InitializeOpenGL();
                InitializeControlArea();
                InitializeSpectrumAnalyzerGraph();
                InitializeTrackingGeneratorGraph();
                DefineGraphColors();

                SetupSpectrumAnalyzerAxis();

#if SUPPORT_EXPERIMENTAL
                InitializeRAWDecoderGraph();
#endif
                UpdateButtonStatus();
                m_bLayoutInitialized = true;
                DisplayGroups();

                try
                {
                    m_SoundPlayer.SoundLocation = m_sAppDataFolder + "\\notify.wav";
                    m_SoundPlayer.LoadAsync();
                }
                catch (Exception obSoundException)
                {
                    ReportLog("Error accessing sound alarm file " + m_SoundPlayer.SoundLocation, false);
                    ReportLog(obSoundException.ToString(), true);
                }

                chkHoldDecoder.Checked = !chkRunDecoder.Checked;
#if DEBUG
                menuDebug.Visible = true;
                menuDebug.Enabled = true;
#endif

                if (!String.IsNullOrEmpty(m_sStartFile))
                {
                    if (RFECommunicator.IsFileExtensionType(m_sStartFile, RFECommunicator._RFL_File_Extension))
                    {
                        m_sFilenameRFE = m_sStartFile;
                        LoadFileRFE(m_sFilenameRFE);
                    }
                    else if (RFECommunicator.IsFileExtensionType(m_sStartFile, RFECommunicator._RFS_File_Extension))
                    {
                        m_sFilenameRFS = m_sStartFile;
                        LoadFileRFS(m_sFilenameRFS);
                        m_MainTab.SelectedTab = m_tabRemoteScreen;
                    }
                }
                else
                {
                    //try to reconnect analyzer
                    m_ToolGroup_COMPortAnalyzer.ConnectPort();
                    m_ToolGroup_COMPortGenerator.ConnectPort();
                    if (!m_objRFEAnalyzer.PortConnected)
                    {
                        //To guarantee all markers are properly hidden when first load with no connection or data
                        DisplaySpectrumAnalyzerData();
                    }
                }
                m_timer_receive.Enabled = true;
            }
            catch (Exception obEx)
            {
                ReportLog("Error in MainForm_Load: " + obEx.ToString(), true);
            }

            if ((m_winAboutModeless != null) && (m_winAboutModeless.IsDisposed == false))
            {
                Thread.Sleep(500);
                m_winAboutModeless.Close();
                m_winAboutModeless.Dispose();
                m_winAboutModeless = null;
            }

            Cursor.Current = Cursors.Default;
        }

        private void UpdateButtonStatus()
        {
#if CALLSTACK
            Console.WriteLine("CALLSTACK:UpdateButtonStatus");
#endif
            try
            {
                m_ToolGroup_COMPortAnalyzer.UpdateButtonStatus();
                m_ToolGroup_COMPortGenerator.UpdateButtonStatus();
                m_ToolGroup_Markers_SA.UpdateButtonStatus();
                m_ToolGroup_Markers_SNA.UpdateButtonStatus();

                m_ToolGroup_Commands.UpdateButtonStatus();

                m_ToolGroup_RemoteScreen.UpdateButtonStatus(menuAutoLCDOff.Checked);

                m_ToolGroup_AnalyzerFreqSettings.UpdateButtonStatus(false);
                groupDemodulator.Enabled = m_objRFEAnalyzer.PortConnected;

                m_ToolGroup_AnalyzerDataFeed.UpdateButtonStatus();
                chkRunDecoder.Enabled = m_objRFEAnalyzer.PortConnected;
                chkHoldDecoder.Enabled = m_objRFEAnalyzer.PortConnected;

                m_ToolGroup_AnalyzerTraces.Realtime = menuRealtimeTrace.Checked;
                m_ToolGroup_AnalyzerTraces.Average = menuAveragedTrace.Checked;
                m_ToolGroup_AnalyzerTraces.MaxPeak = menuMaxTrace.Checked;
                m_ToolGroup_AnalyzerTraces.MaxHold = menuMaxHoldTrace.Checked;
                m_ToolGroup_AnalyzerTraces.Minimum = menuMinTrace.Checked;
                m_ToolGroup_AnalyzerTraces.FillTrace = menuSignalFill.Checked;
                m_ToolGroup_AnalyzerTraces.Smooth = menuSmoothSignals.Checked;
                m_ToolGroup_AnalyzerTraces.ThickTrace = menuThickTrace.Checked;
                m_ToolGroup_AnalyzerTraces.ShowGrid = menuShowGrid.Checked;
                m_ToolGroup_AnalyzerTraces.AxisLabels = menuShowAxisLabels.Checked;

                //m_panelSAConfiguration.Enabled = true;
                //calibration is available for all models but 2.4G
                groupCalibration.Enabled = m_objRFEAnalyzer.PortConnected && (m_objRFEAnalyzer.ActiveModel != RFECommunicator.eModel.MODEL_2400);

                if (controlRemoteScreen.RFExplorer == null)
                {
                    controlRemoteScreen.RFExplorer = m_objRFEAnalyzer;
                }

                for (int nInd = 0; nInd < m_arrAnalyzerButtonList.Length; nInd++)
                {
                    if (m_arrAnalyzerButtonList[nInd] != null)
                    {
                        if ((string)m_arrAnalyzerButtonList[nInd].Tag == (string)_OnlyIfConnected)
                        {
                            m_arrAnalyzerButtonList[nInd].Enabled = m_objRFEAnalyzer.PortConnected;
                        }
                        else
                        {
                            m_arrAnalyzerButtonList[nInd].Enabled = m_objRFEAnalyzer.PortConnected || (m_objRFEAnalyzer.SweepData.Count > 0);
                        }
                    }
                }
            }
            catch (Exception obEx)
            {
                ReportLog("ERROR UpdateButtonStatus: " + obEx.ToString(), true);
            }
        }

        private void DefineCommonFiles()
        {
            if (String.IsNullOrEmpty(m_sAppDataFolder))
            {
                //Configuring and loading default folders
                m_sAppDataFolder = Environment.GetEnvironmentVariable("APPDATA") + "\\RFExplorer";
                m_sAppDataFolder = m_sAppDataFolder.Replace("\\\\", "\\");

                if (Directory.Exists(m_sAppDataFolder) == false)
                {
                    //Create specific RF Explorer folders if they don't exist, alert with a message box if that fails
                    try
                    {
                        Directory.CreateDirectory(m_sAppDataFolder);
                    }
                    catch (Exception obEx)
                    {
                        MessageBox.Show(obEx.Message);
                    }
                }
            }

            if (String.IsNullOrEmpty(m_sDefaultDataFolder) || m_sDefaultDataFolder == "<new>")
            {
                m_sDefaultDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\RFExplorer";
                m_sDefaultDataFolder = m_sDefaultDataFolder.Replace("\\\\", "\\");
                edDefaultFilePath.Text = m_sDefaultDataFolder;
                m_sDefaultUserFolder = m_sDefaultDataFolder;
            }

            if (Directory.Exists(m_sDefaultDataFolder) == false)
            {
                //Create specific RF Explorer folders if they don't exist, alert with a message box if that fails
                try
                {
                    Directory.CreateDirectory(m_sDefaultDataFolder);
                }
                catch (Exception obEx)
                {
                    MessageBox.Show(obEx.Message);
                }
            }
        }

        private void LoadProperties()
        {
#if CALLSTACK
            Console.WriteLine("CALLSTACK:LoadProperties");
#endif
            try
            {
                //Load standard WinForm .NET properties
                m_ToolGroup_COMPortAnalyzer.DefaultCOMSpeed = Properties.Settings.Default.COMSpeed;
                m_ToolGroup_COMPortAnalyzer.DefaultCOMPort = Properties.Settings.Default.COMPort;
                m_ToolGroup_COMPortGenerator.DefaultCOMSpeed = Properties.Settings.Default.COMSpeedRFGen;
                m_ToolGroup_COMPortGenerator.DefaultCOMPort = Properties.Settings.Default.COMPortRFGen;

                menuSaveOnClose.Checked = Properties.Settings.Default.SaveOnClose;
                m_ToolGroup_RemoteScreen.ScreenZoom = Properties.Settings.Default.ScreenZoom;
                menuShowPeak.Checked = Properties.Settings.Default.ViewPeaks;
                menuShowControlArea.Checked = Properties.Settings.Default.ShowControlArea;
                menuDarkMode.Checked = Properties.Settings.Default.DarkMode;
                menuAutoLCDOff.Checked = Properties.Settings.Default.AutoLCDOff;
                menuContinuousLog.Checked = Properties.Settings.Default.ContinuousLog;
                string sTemp = Properties.Settings.Default.DefaultDataFolder;
                comboCSVFieldSeparator.SelectedIndex = (int)Properties.Settings.Default.CSVDelimiter;
                menuRFConnections.Checked = Properties.Settings.Default.ShowRFConnections;
                menuSmoothSignals.Checked = Properties.Settings.Default.SignalSmooth;
                menuThickTrace.Checked = Properties.Settings.Default.ThickTrace;
                menuShowGrid.Checked = Properties.Settings.Default.ShowGrid;
                menuSignalFill.Checked = Properties.Settings.Default.SignalFill;
                menuShowAxisLabels.Checked = Properties.Settings.Default.ShowAxisTitle;
                menuItemSoundAlarmLimitLine.Checked = Properties.Settings.Default.LimitLinesSoundAlarm;
                menuAutoLoadAmplitudeData.Checked = Properties.Settings.Default.AutoLoadCorrectionModel;
                menuRemoteAmplitudeUpdate.Checked = Properties.Settings.Default.AutoAmplitudeRemoteUpdate;
                menuRemoteMaxHold.Checked = Properties.Settings.Default.AutoMaxHold;
                if (m_objRFEAnalyzer != null)
                {
                    m_objRFEAnalyzer.UseMaxHold = menuRemoteMaxHold.Checked;
                }

                m_objMainWaterfall.SignalType = (RFECommunicator.RFExplorerSignalType)Properties.Settings.Default.WaterfallSignalType;
                m_objMainWaterfall.DrawFloor = Properties.Settings.Default.WaterfallFloor;
                m_objMainWaterfall.Transparent = Properties.Settings.Default.TransparentWaterfall;
                m_objMainWaterfall.PerspectiveModeView = (RFEWaterfallGL.SharpGLForm.WaterfallPerspectives)Properties.Settings.Default.WaterfallView;

                m_objSAWaterfall.SignalType = (RFECommunicator.RFExplorerSignalType)Properties.Settings.Default.WaterfallSignalTypeSA;
                m_objSAWaterfall.DrawFloor = Properties.Settings.Default.WaterfallFloorSA;
                m_objSAWaterfall.Transparent = Properties.Settings.Default.TransparentWaterfallSA;
                m_objSAWaterfall.PerspectiveModeView = (RFEWaterfallGL.SharpGLForm.WaterfallPerspectives)Properties.Settings.Default.WaterfallViewSA;
                UpdateAllWaterfallMenuItems();

                menuPlaceWaterfallAtBottom.Checked = Properties.Settings.Default.WaterfallBottom;
                menuPlaceWaterfallOnTheRight.Checked = Properties.Settings.Default.WaterfallRight;
                menuPlaceWaterfallNone.Checked = Properties.Settings.Default.WaterfallNoSA;
                m_ToolGroup_RFEGenTracking.Average = (UInt16)Properties.Settings.Default.TrackingAverage;

                if (!String.IsNullOrEmpty(sTemp))
                {
                    m_sDefaultUserFolder = sTemp;
                }
                DefineCommonFiles();

                m_sSettingsFile = m_sAppDataFolder + "\\" + _Filename_RFExplorer_Settings;
                try
                {
                    //Check if old settings file exists, and not the new one, thus move it to reuse it. This "old file" was
                    //created in v1.09.04 and has the problem of some users with limited access rights cannot create it.
                    string sOldSettingsFile = Assembly.GetExecutingAssembly().Location + ".Settings.xml";
                    if (File.Exists(sOldSettingsFile) && !File.Exists(m_sSettingsFile))
                    {
                        //We do not really move it as there may be restricted permissions and want to avoid any problem
                        File.Copy(sOldSettingsFile, m_sSettingsFile);
                        MessageBox.Show("NOTE: Old named-settings file has been migrated to the new location " + m_sSettingsFile);
                        //we try to delete it now, may fail but worth trying. We already copied it, anyway
                        File.Delete(sOldSettingsFile);
                    }
                }
                catch { };
                //open new settings file
                m_DataSettings = new DataSet("RF_Explorer_Settings");
                if (!File.Exists(m_sSettingsFile))
                {
                    CreateSettingsSchema();
                }

                //Load custom name saved properties
                if (m_DataSettings.Tables.Count == 0)
                {
                    //do not load it twice or records will be repeated
                    m_DataSettings.ReadXml(m_sSettingsFile);
                }
                PopulateReadedSettings();
                RestoreSettingsXML(_Default);

                edDefaultFilePath.Text = m_sDefaultUserFolder;

                if (Properties.Settings.Default.CustomCommandList.Length > 0)
                {
                    string[] arrCustomCommands = null;
                    GetStringArrayFromStringList(Properties.Settings.Default.CustomCommandList, out arrCustomCommands);
                    foreach (string sCmd in arrCustomCommands)
                    {
                        m_ToolGroup_Commands.CustomCommandAddItem = sCmd;
                    }
                }

                if (Properties.Settings.Default.UserDefinedTitleList.Length > 0)
                {
                    string[] arrStrings = null;
                    GetStringArrayFromStringList(Properties.Settings.Default.UserDefinedTitleList, out arrStrings);
                    foreach (string sText in arrStrings)
                    {
                        if (sText.Length > 0)
                        {
                            m_dlgUserDefinedText.m_comboText.Items.Add(sText);
                        }
                    }
                }

                menuItemDBM.Checked = true;

                m_bPropertiesReadOk = true;
            }
            catch (Exception obEx)
            {
                MessageBox.Show(obEx.ToString());
                ReportLog(obEx.ToString(), false);
            }
        }

        private void PopulateReadedSettings()
        {
            menuComboSavedOptions.Items.Clear();
            foreach (DataRow objRow in m_DataSettings.Tables[_Common_Settings].Rows)
            {
                menuComboSavedOptions.Items.Add((string)objRow[_Name]);
            }
            menuComboSavedOptions.SelectedItem = _Default;
        }

        private void SaveProperties()
        {
            if (!m_bPropertiesReadOk)
            {
                ReportLog("Application settings were not persisted due to an earlier error.", false);
                return;
            }

            try
            {
                //No need to save CustomCommands here, it is already saved in send button.
                //Properties.Settings.Default.CustomCommands

                if (m_ToolGroup_COMPortAnalyzer.IsCOMPortSelected)
                {
                    Properties.Settings.Default.COMPort = m_ToolGroup_COMPortAnalyzer.COMPortSelected;
                }
                if (m_ToolGroup_COMPortAnalyzer.IsCOMSpeedSelected)
                {
                    Properties.Settings.Default.COMSpeed = m_ToolGroup_COMPortAnalyzer.COMSpeedSelected;
                }
                if (m_ToolGroup_COMPortGenerator.IsCOMPortSelected)
                {
                    Properties.Settings.Default.COMPortRFGen = m_ToolGroup_COMPortGenerator.COMPortSelected;
                }
                if (m_ToolGroup_COMPortGenerator.IsCOMSpeedSelected)
                {
                    Properties.Settings.Default.COMSpeedRFGen = m_ToolGroup_COMPortGenerator.COMSpeedSelected;
                }

                Properties.Settings.Default.SaveOnClose = menuSaveOnClose.Checked;
                Properties.Settings.Default.ScreenZoom = m_ToolGroup_RemoteScreen.ScreenZoom;
                Properties.Settings.Default.ViewPeaks = menuShowPeak.Checked;
                Properties.Settings.Default.ShowControlArea = menuShowControlArea.Checked;
                Properties.Settings.Default.DarkMode = menuDarkMode.Checked;
                Properties.Settings.Default.AutoLCDOff = menuAutoLCDOff.Checked;
                Properties.Settings.Default.DefaultDataFolder = m_sDefaultUserFolder;
                Properties.Settings.Default.CSVDelimiter = (uint)comboCSVFieldSeparator.SelectedIndex;
                Properties.Settings.Default.ContinuousLog = menuContinuousLog.Checked;
                Properties.Settings.Default.ShowRFConnections = menuRFConnections.Checked;
                Properties.Settings.Default.SignalFill = menuSignalFill.Checked;
                Properties.Settings.Default.SignalSmooth = menuSmoothSignals.Checked;
                Properties.Settings.Default.ThickTrace = menuThickTrace.Checked;
                Properties.Settings.Default.ShowGrid = menuShowGrid.Checked;
                Properties.Settings.Default.ShowAxisTitle = menuShowAxisLabels.Checked;
                Properties.Settings.Default.LimitLinesSoundAlarm = menuItemSoundAlarmLimitLine.Checked;
                Properties.Settings.Default.AutoLoadCorrectionModel = menuAutoLoadAmplitudeData.Checked;
                Properties.Settings.Default.AutoMaxHold = menuRemoteMaxHold.Checked;
                Properties.Settings.Default.AutoAmplitudeRemoteUpdate = menuRemoteAmplitudeUpdate.Checked;

                Properties.Settings.Default.WaterfallView = (uint)m_objMainWaterfall.PerspectiveModeView;
                Properties.Settings.Default.TransparentWaterfall = m_objMainWaterfall.Transparent;
                Properties.Settings.Default.WaterfallFloor = m_objMainWaterfall.DrawFloor;
                Properties.Settings.Default.WaterfallSignalType = (int)m_objMainWaterfall.SignalType;

                Properties.Settings.Default.WaterfallViewSA = (uint)m_objSAWaterfall.PerspectiveModeView;
                Properties.Settings.Default.TransparentWaterfallSA = m_objSAWaterfall.Transparent;
                Properties.Settings.Default.WaterfallFloorSA = m_objSAWaterfall.DrawFloor;
                Properties.Settings.Default.WaterfallSignalTypeSA = (int)m_objSAWaterfall.SignalType;

                Properties.Settings.Default.WaterfallBottom = menuPlaceWaterfallAtBottom.Checked;
                Properties.Settings.Default.WaterfallRight = menuPlaceWaterfallOnTheRight.Checked;
                Properties.Settings.Default.WaterfallNoSA = menuPlaceWaterfallNone.Checked;

                Properties.Settings.Default.TrackingAverage = m_ToolGroup_RFEGenTracking.Average;

                Properties.Settings.Default.UserDefinedTitleList = "";
                foreach (string sText in m_dlgUserDefinedText.m_comboText.Items)
                {
                    Properties.Settings.Default.UserDefinedTitleList += ";" + sText.Replace(';', '-');
                }

                Properties.Settings.Default.Save();

                SaveSettingsXML(_Default);
            }
            catch (Exception obEx)
            {
                ReportLog("ERROR SaveProperties:" + obEx.Message, true);
            }
        }

        private void DeleteSettingsXML(string sSettingsName)
        {
            try
            {
                DataRow[] objRowCol = m_DataSettings.Tables[_Common_Settings].Select("Name='" + sSettingsName + "'");
                if (objRowCol.Length > 0)
                {
                    m_DataSettings.Tables[_Common_Settings].Rows.Remove(objRowCol[0]);
                    m_DataSettings.WriteXml(m_sSettingsFile, XmlWriteMode.WriteSchema);
                }
            }
            catch (Exception obEx)
            {
                ReportLog("ERROR DeleteSettingsXML:" + obEx.Message, true);
            }
        }

        private void RestoreSettingsXML(string sSettingsName)
        {
            try
            {
                m_MarkersSA.HideAllMarkers();
                m_MarkersSNA.HideAllMarkers();

                DataRow[] objRowCol = m_DataSettings.Tables[_Common_Settings].Select("Name='" + sSettingsName + "'");
                if (objRowCol.Length > 0)
                {
                    DataRow objRowDefault = objRowCol[0];
                    double fStartFrequencyMHZ = (double)objRowDefault[_StartFreq];
                    double fStepFrequencyMHZ = (double)objRowDefault[_StepFreq];
                    double fAmplitudeTop = (double)objRowDefault[_TopAmp];
                    double fAmplitudeBottom = (double)objRowDefault[_BottomAmp];
                    m_ToolGroup_AnalyzerDataFeed.Iterations = (UInt16)objRowDefault[_Calculator];
                    menuAveragedTrace.Checked = (bool)objRowDefault[_ViewAvg];
                    menuRealtimeTrace.Checked = (bool)objRowDefault[_ViewRT];
                    menuMinTrace.Checked = (bool)objRowDefault[_ViewMin];
                    menuMaxTrace.Checked = (bool)objRowDefault[_ViewMax];

                    if (m_DataSettings.Tables[_Common_Settings].Columns[_ViewMaxHold] == null)
                    {
                        //Introduced in v1.11.0.1307, may not exist before this date
                        m_DataSettings.Tables[_Common_Settings].Columns.Add(new DataColumn(_ViewMaxHold, System.Type.GetType("System.Boolean")));
                    }

                    try
                    {
                        if ((objRowDefault[_ViewMaxHold] != null) && (!String.IsNullOrEmpty(objRowDefault[_ViewMaxHold].ToString())))
                        {
                            menuMaxHoldTrace.Checked = (bool)objRowDefault[_ViewMaxHold];
                        }
                        else
                        {
                            objRowDefault[_ViewMaxHold] = menuMaxHoldTrace.Checked;
                        }
                    }
                    catch { }

                    if (m_DataSettings.Tables[_Common_Settings].Columns[_MarkerTrackSignalSA] == null)
                    {
                        //Introduced in v1.11.0.1402, may not exist before this date
                        for (int nInd = 1; nInd <= MarkerCollection.MAX_MARKERS; nInd++)
                        {
                            m_DataSettings.Tables[_Common_Settings].Columns.Add(new DataColumn(_MarkerEnabledSA + nInd.ToString(), System.Type.GetType("System.Boolean")));
                            if (nInd != 1)
                            {
                                m_DataSettings.Tables[_Common_Settings].Columns.Add(new DataColumn(_MarkerFrequencySA + nInd.ToString(), System.Type.GetType("System.Double")));
                            }
                            else
                            {
                                m_DataSettings.Tables[_Common_Settings].Columns.Add(new DataColumn(_MarkerTrackSignalSA, System.Type.GetType("System.UInt16")));
                            }
                        }
                    }

                    if (m_DataSettings.Tables[_Common_Settings].Columns[_MarkerTrackSignalSNA] == null)
                    {
                        //Introduced in v1.12.1507.2, may not exist before this date
                        for (int nInd = 1; nInd <= MarkerCollection.MAX_MARKERS; nInd++)
                        {
                            m_DataSettings.Tables[_Common_Settings].Columns.Add(new DataColumn(_MarkerEnabledSNA + nInd.ToString(), System.Type.GetType("System.Boolean")));
                            if (nInd != 1)
                            {
                                m_DataSettings.Tables[_Common_Settings].Columns.Add(new DataColumn(_MarkerFrequencySNA + nInd.ToString(), System.Type.GetType("System.Double")));
                            }
                            else
                            {
                                m_DataSettings.Tables[_Common_Settings].Columns.Add(new DataColumn(_MarkerTrackSignalSNA, System.Type.GetType("System.UInt16")));
                            }
                        }
                    }

                    try
                    {
                        //SA markers
                        if ((objRowDefault[_MarkerTrackSignalSA] != null) && (!String.IsNullOrEmpty(objRowDefault[_MarkerTrackSignalSA].ToString())))
                        {
                            for (int nInd = 1; nInd <= MarkerCollection.MAX_MARKERS; nInd++)
                            {
                                if ((bool)objRowDefault[_MarkerEnabledSA + nInd.ToString()])
                                {
                                    m_MarkersSA.EnableMarker(nInd - 1);
                                }
                                if (nInd != 1)
                                {
                                    m_MarkersSA.SetMarkerFrequency(nInd - 1, (double)objRowDefault[_MarkerFrequencySA + nInd.ToString()]);
                                }
                                else
                                {
                                    m_ToolGroup_Markers_SA.TrackSignalPeak = (RFECommunicator.RFExplorerSignalType)(UInt16)objRowDefault[_MarkerTrackSignalSA];
                                }
                            }
                        }
                        else
                        {
                            for (int nInd = 1; nInd <= MarkerCollection.MAX_MARKERS; nInd++)
                            {
                                objRowDefault[_MarkerEnabledSA + nInd.ToString()] = m_MarkersSA.IsMarkerEnabled(nInd - 1);
                                if (nInd != 1)
                                {
                                    objRowDefault[_MarkerFrequencySA + nInd.ToString()] = m_MarkersSA.GetMarkerFrequency(nInd - 1);
                                }
                                else
                                {
                                    objRowDefault[_MarkerTrackSignalSA] = 0;
                                }
                            }
                        }
                    }
                    catch { };

                    try
                    {
                        //SNA markers
                        if ((objRowDefault[_MarkerTrackSignalSNA] != null) && (!String.IsNullOrEmpty(objRowDefault[_MarkerTrackSignalSNA].ToString())))
                        {
                            for (int nInd = 1; nInd <= MarkerCollection.MAX_MARKERS; nInd++)
                            {
                                if ((bool)objRowDefault[_MarkerEnabledSNA + nInd.ToString()])
                                {
                                    m_MarkersSNA.EnableMarker(nInd - 1);
                                }
                                if (nInd != 1)
                                {
                                    m_MarkersSNA.SetMarkerFrequency(nInd - 1, (double)objRowDefault[_MarkerFrequencySNA + nInd.ToString()]);
                                }
                                else
                                {
                                    m_ToolGroup_Markers_SNA.TrackSignalPeak = (RFECommunicator.RFExplorerSignalType)(UInt16)objRowDefault[_MarkerTrackSignalSNA];
                                }
                            }
                        }
                        else
                        {
                            for (int nInd = 1; nInd <= MarkerCollection.MAX_MARKERS; nInd++)
                            {
                                objRowDefault[_MarkerEnabledSNA + nInd.ToString()] = m_MarkersSNA.IsMarkerEnabled(nInd - 1);
                                if (nInd != 1)
                                {
                                    objRowDefault[_MarkerFrequencySNA + nInd.ToString()] = m_MarkersSNA.GetMarkerFrequency(nInd - 1);
                                }
                                else
                                {
                                    objRowDefault[_MarkerTrackSignalSNA] = 0;
                                }
                            }
                        }
                    }
                    catch { };

                    if (m_DataSettings.Tables[_Common_Settings].Columns[_RFGenStartFreq] == null)
                    {
                        //Introduced in v1.12.1409, may not exist in all files
                        m_DataSettings.Tables[_Common_Settings].Columns.Add(new DataColumn(_RFGenStartFreq, System.Type.GetType("System.Double")));
                        m_DataSettings.Tables[_Common_Settings].Columns.Add(new DataColumn(_RFGenStopFreq, System.Type.GetType("System.Double")));
                        m_DataSettings.Tables[_Common_Settings].Columns.Add(new DataColumn(_RFGenSteps, System.Type.GetType("System.UInt16")));
                        m_DataSettings.Tables[_Common_Settings].Columns.Add(new DataColumn(_RFGenPower, System.Type.GetType("System.UInt16")));
                        m_DataSettings.Tables[_Common_Settings].Columns.Add(new DataColumn(_RFGenStepTime, System.Type.GetType("System.UInt16")));
                    }

                    try
                    {
                        if ((objRowDefault[_RFGenStartFreq] != null) && (!String.IsNullOrEmpty(objRowDefault[_RFGenStartFreq].ToString())))
                        {
                            m_ToolGroupRFEGenFreqSweep.Start = (double)objRowDefault[_RFGenStartFreq];
                            m_ToolGroupRFEGenFreqSweep.Stop = (double)objRowDefault[_RFGenStopFreq];
                            m_ToolGroupRFEGenFreqSweep.Steps = (UInt16)objRowDefault[_RFGenSteps];
                            //TODO: restore all RFGen settings from properties
                            UInt16 nRFGenPower = (UInt16)objRowDefault[_RFGenPower];
                            UInt16 nRFGenStepTime = (UInt16)objRowDefault[_RFGenStepTime];
                        }
                        else
                        {
                            objRowDefault[_RFGenStartFreq] = m_ToolGroupRFEGenFreqSweep.Start;
                            objRowDefault[_RFGenStopFreq] = m_ToolGroupRFEGenFreqSweep.Stop;
                            objRowDefault[_RFGenSteps] = m_ToolGroupRFEGenFreqSweep.Steps;
                            objRowDefault[_RFGenPower] = 0;
                            objRowDefault[_RFGenStepTime] = 0;
                        }
                    }
                    catch { };

                    UpdateMenuFromMarkerCollection(m_MainTab.SelectedTab == m_tabRFGen);
                    UpdateSAMarkerControlContents();
                    UpdateSNAMarkerControlContents();

                    if (m_objRFEAnalyzer.PortConnected == false)
                    {
                        //If device is disconnected, we just need to update visible parts of screen as otherwise it won't change
                        m_objRFEAnalyzer.AmplitudeTopDBM = fAmplitudeTop;
                        m_objRFEAnalyzer.AmplitudeBottomDBM = fAmplitudeBottom;

                        //Check to reinitiate buffer here, otherwise after changing it the receive function will not know the data was changed
                        if ((Math.Abs(m_objRFEAnalyzer.StartFrequencyMHZ - fStartFrequencyMHZ) >= 0.001) || (Math.Abs(m_objRFEAnalyzer.StepFrequencyMHZ - fStepFrequencyMHZ) >= 0.001))
                        {
                            m_objRFEAnalyzer.StartFrequencyMHZ = fStartFrequencyMHZ;
                            m_objRFEAnalyzer.StepFrequencyMHZ = fStepFrequencyMHZ;
                            CleanSweepData();
                        }
                        SetupSpectrumAnalyzerAxis(); //will update everything including the edit boxes
                    }
                    else
                    {
                        //if device is connected, we do not need to change anything: just ask the device to reconfigure and the new configuration will come back
                        SendNewConfig(fStartFrequencyMHZ, fStartFrequencyMHZ + fStepFrequencyMHZ * m_objRFEAnalyzer.FreqSpectrumSteps, fAmplitudeTop, fAmplitudeBottom);
                    }

                    m_sLastSettingsLoaded = sSettingsName;
                    UpdateTitleText_Analyzer();
                    UpdateButtonStatus();
                }
            }
            catch (Exception obEx)
            {
                ReportLog("ERROR RestoreSettingsXML:" + obEx.ToString(), true);
            }
        }

        private void SaveSettingsXML(string sSettingsName)
        {
            try
            {
                DataRow[] objRowCol = m_DataSettings.Tables[_Common_Settings].Select("Name='" + sSettingsName + "'");
                DataRow objRow = null;

                if (objRowCol.Length == 0)
                {
                    objRow = m_DataSettings.Tables[_Common_Settings].NewRow();
                    objRow[_Name] = sSettingsName;
                    m_DataSettings.Tables[_Common_Settings].Rows.Add(objRow);
                }
                else
                {
                    objRow = objRowCol[0];
                }

                objRow[_StartFreq] = m_objRFEAnalyzer.StartFrequencyMHZ;
                objRow[_StepFreq] = m_objRFEAnalyzer.StepFrequencyMHZ;
                objRow[_TopAmp] = m_objRFEAnalyzer.AmplitudeTopDBM;
                objRow[_BottomAmp] = m_objRFEAnalyzer.AmplitudeBottomDBM;
                objRow[_Calculator] = (int)m_ToolGroup_AnalyzerDataFeed.Iterations;
                objRow[_ViewAvg] = menuAveragedTrace.Checked;
                objRow[_ViewRT] = menuRealtimeTrace.Checked;
                objRow[_ViewMin] = menuMinTrace.Checked;
                objRow[_ViewMax] = menuMaxTrace.Checked;
                try
                {
                    //Introduced in Jun 2013, may not exist before this date. The column should have been created already in ReadXML function...
                    objRow[_ViewMaxHold] = menuMaxHoldTrace.Checked;
                }
                catch { };

                try
                {
                    //Marker SA
                    //Introduced in Feb 2014, may not exist before this date. The column should have been created already in ReadXML function...
                    UpdateMenuFromMarkerCollection(false);
                    for (int nInd = 1; nInd <= MarkerCollection.MAX_MARKERS; nInd++)
                    {
                        if (nInd != 1)
                        {
                            objRow[_MarkerFrequencySA + nInd.ToString()] = m_MarkersSA.GetMarkerFrequency(nInd - 1);
                        }
                        else
                        {
                            objRow[_MarkerTrackSignalSA] = (UInt16)m_ToolGroup_Markers_SA.TrackSignalPeak;
                        }
                        objRow[_MarkerEnabledSA + nInd.ToString()] = m_arrMarkersEnabledMenu[nInd - 1].Checked;
                    }
                }
                catch { };

                try
                {
                    //Marker SNA
                    //Introduced in Jul 2015, may not exist before this date. The column should have been created already in ReadXML function...
                    UpdateMenuFromMarkerCollection(true);
                    for (int nInd = 1; nInd <= MarkerCollection.MAX_MARKERS; nInd++)
                    {
                        if (nInd != 1)
                        {
                            objRow[_MarkerFrequencySNA + nInd.ToString()] = m_MarkersSNA.GetMarkerFrequency(nInd - 1);
                        }
                        else
                        {
                            objRow[_MarkerTrackSignalSNA] = (UInt16)m_ToolGroup_Markers_SNA.TrackSignalPeak;
                        }
                        objRow[_MarkerEnabledSNA + nInd.ToString()] = m_arrMarkersEnabledMenu[nInd - 1].Checked;
                    }
                }
                catch { };

                UpdateMenuFromMarkerCollection(m_MainTab.SelectedTab == m_tabRFGen); //restore after changes done in current menu

                try
                {
                    //Introduced in Sep 2014
                    objRow[_RFGenStartFreq] = m_ToolGroupRFEGenFreqSweep.Start;
                    objRow[_RFGenStopFreq] = m_ToolGroupRFEGenFreqSweep.Stop;
                    objRow[_RFGenSteps] = m_ToolGroupRFEGenFreqSweep.Steps;
                    objRow[_RFGenPower] = 0;
                    objRow[_RFGenStepTime] = 0;
                }
                catch { };

                m_DataSettings.WriteXml(m_sSettingsFile, XmlWriteMode.WriteSchema);

                if (!((sSettingsName == _Default) && (m_sLastSettingsLoaded != _Default)))
                {
                    //Only update screen text if
                    m_sLastSettingsLoaded = sSettingsName;
                    UpdateTitleText_Analyzer();
                }

                if (sSettingsName == _Default)
                {
                    //If we are saving the default value, that is because we are doing it automatically (e.g. when closing the port)
                    //Therefore select it as the default on screen too
                    menuComboSavedOptions.SelectedItem = _Default;
                }
            }
            catch (Exception obEx)
            {
                ReportLog("ERROR SaveSettingsXML:" + obEx.Message, true);
            }
        }

        private void DisplayRequiredFirmware()
        {
            try
            {
                if (m_objRFEAnalyzer.RFExplorerFirmwareDetected != m_objRFEAnalyzer.FirmwareCertified)
                {
                    UInt16 nMayorVerFound = Convert.ToUInt16(m_objRFEAnalyzer.RFExplorerFirmwareDetected.Substring(0, 2));
                    UInt16 nMinorVerFound = Convert.ToUInt16(m_objRFEAnalyzer.RFExplorerFirmwareDetected.Substring(3, 2));
                    UInt32 nVersionFound = (UInt32)(nMayorVerFound * 100 + nMinorVerFound);
                    UInt16 nMayorVerTested = Convert.ToUInt16(m_objRFEAnalyzer.FirmwareCertified.Substring(0, 2));
                    UInt16 nMinorVerTested = Convert.ToUInt16(m_objRFEAnalyzer.FirmwareCertified.Substring(3, 2));
                    UInt32 nVersionTested = (UInt32)(nMayorVerTested * 100 + nMinorVerTested);

                    if (nVersionFound > nVersionTested)
                    {
                        ReportLog("\r\nWARNING: Firmware version connected v" + m_objRFEAnalyzer.RFExplorerFirmwareDetected + " is newer than the one certified v" +
                                    m_objRFEAnalyzer.FirmwareCertified + " for this version of RF Explorer for Windows.\r\n" +
                                      "         However, it may be compatible but you should check www.rf-explorer.com website\r\n" +
                                      "         to double check if there is a newer version available.\r\n", false);
                    }
                    else
                    {
                        string sText = "RF Explorer device has an older firmware version " + m_objRFEAnalyzer.RFExplorerFirmwareDetected +
                            "\r\nPlease upgrade it to required version " + m_objRFEAnalyzer.FirmwareCertified +
                            "\r\nVisit www.rf-explorer/download to get required firmware.";
                        if (!m_bVersionAlerted)
                        {
                            m_bVersionAlerted = true;
                            MessageBox.Show(sText, "Firmware Warning");
                        }
                        ReportLog(sText, false);
                    }
                }
            }
            catch (Exception obEx)
            {
                ReportLog("Cannot check firmware code: " + m_objRFEAnalyzer.RFExplorerFirmwareDetected, false);
                ReportLog(obEx.ToString(), true);
            }
        }

        int m_nTimerCounter = 0;
        private void timer_receive_Tick(object sender, EventArgs e)
        {
            m_nTimerCounter++; //just used to count certain events, so we can do things less frequently when required

            try
            {
                bool bDraw = false;
                string sOut;

                if (m_objRFEAnalyzer.PortConnected)
                {
                    bDraw = m_objRFEAnalyzer.ProcessReceivedString(true, out sOut);
                }

                if (m_objRFEGenerator.PortConnected)
                {
                    m_objRFEGenerator.ProcessReceivedString(true, out sOut);
                }

                if (bDraw)
                {
#if CALLSTACK_REALTIME
                    Console.WriteLine("CALLSTACK:timer_receive_Tick");
#endif
                    if (m_objRFEAnalyzer.Mode == RFECommunicator.eMode.MODE_TRACKING)
                    {
                        if (m_MainTab.SelectedTab == m_tabRFGen)
                        {
                            DisplayTrackingData();
                        }
                    }
                    else
                    {
                        if ((m_MainTab.SelectedTab == m_tabSpectrumAnalyzer) || (m_MainTab.SelectedTab == m_tabPowerChannel))
                        {
                            DisplaySpectrumAnalyzerData();
                        }
                        if ((m_MainTab.SelectedTab == m_tabWaterfall) || IsWaterfallOnMainScreen())
                        {
                            UpdateWaterfall();
                        }
                    }
                }
                else
                {
                    if (m_objRFEAnalyzer.Mode == RFECommunicator.eMode.MODE_TRACKING)
                    {
                        if (m_MainTab.SelectedTab == m_tabRFGen)
                        {
                            DisplayTrackingData_TextProgress(true);
                        }
                    }
                }
            }
            catch (Exception obEx)
            {
                ReportLog("timer_receive_Tick: " + obEx.Message, true);
            }

            if (m_bFirstTick)
            {
                m_bFirstTick = false;
                ReportLog("", true);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            if ((menuSaveOnClose.Checked || menuContinuousLog.Checked) && (m_sFilenameRFE.Length == 0) && m_objRFEAnalyzer.IsAnalyzer())
            {
                GetNewFilename(RFExplorerFileType.SweepDataFile);
                SaveFileRFE(m_sFilenameRFE, true);
            }
            SaveProperties();
            m_objRFEAnalyzer.Close();
            m_objRFEGenerator.Close();
            Cursor.Current = Cursors.Default;
        }

        private void SetupSpectrumAnalyzerAxis()
        {
            if (m_objRFEAnalyzer == null)
                return;

            if (m_GraphSpectrumAnalyzer == null)
                return;

#if CALLSTACK
            Console.WriteLine("CALLSTACK:SetupSpectrumAnalyzerAxis");
#endif
            double fStart = m_objRFEAnalyzer.StartFrequencyMHZ;
            double fEnd = m_objRFEAnalyzer.CalculateEndFrequencyMHZ() - m_objRFEAnalyzer.StepFrequencyMHZ;
            double fMajorStep = 1.0;

            m_GraphSpectrumAnalyzer.GraphPane.XAxis.Scale.Min = fStart;
            m_GraphSpectrumAnalyzer.GraphPane.XAxis.Scale.Max = fEnd;

            if (m_objRFEAnalyzer.Mode == RFECommunicator.eMode.MODE_WIFI_ANALYZER)
            {
                //objGraph is a bar chart
                m_GraphSpectrumAnalyzer.GraphPane.XAxis.Scale.Min = fStart - 2.5f;
                m_GraphSpectrumAnalyzer.GraphPane.XAxis.Scale.Max = fEnd + 2.5f;
            }

            if ((fEnd - fStart) < 1.0)
            {
                fMajorStep = 0.1;
            }
            else if ((fEnd - fStart) < 10)
            {
                fMajorStep = 1.0;
            }
            else if ((fEnd - fStart) < 100)
            {
                fMajorStep = 10;
            }
            else if ((fEnd - fStart) < 500)
            {
                fMajorStep = 50;
            }
            else if ((fEnd - fStart) < 1000)
            {
                fMajorStep = 100;
            }

            m_GraphSpectrumAnalyzer.GraphPane.XAxis.Scale.MajorStep = fMajorStep;
            m_GraphSpectrumAnalyzer.GraphPane.XAxis.Scale.MinorStep = fMajorStep / 10.0;

            UpdateYAxis();

            m_ToolGroup_AnalyzerFreqSettings.UpdateButtonStatus(true);
        }

        /// <summary>
        /// If the user have configured a different unit scale (dBuV or Watt) this function will
        /// return the amplitude in those units (from dBm) input
        /// </summary>
        /// <param name="dAmplitudeDBM">standard amplitude dBm value</param>
        /// <returns></returns>
        private double ConvertToCurrentAmplitudeUnit(double dAmplitudeDBM)
        {
            if (menuItemWatt.Checked)
                return RFECommunicator.Convert_dBm_2_Watt(dAmplitudeDBM);
            else if (menuItemDBUV.Checked)
                return RFECommunicator.Convert_dBm_2_dBuV(dAmplitudeDBM);
            else
                return dAmplitudeDBM;
        }

        /// <summary>
        /// Returns amplitude in dBm from whatever dAmplitude current value is specified
        /// </summary>
        /// <param name="dAmplitude"></param>
        /// <returns></returns>
        private double ConvertFromCurrentAmplitudeUnit(double dAmplitude)
        {
            if (menuItemWatt.Checked)
                return RFECommunicator.Convert_Watt_2_dBm(dAmplitude);
            else if (menuItemDBUV.Checked)
                return RFECommunicator.Convert_dBuV_2_dBm(dAmplitude);
            else
                return dAmplitude;
        }

        private double ConvertFromCurrentAmplitudeUnit(string sAmplitude)
        {
            return ConvertFromCurrentAmplitudeUnit(Convert.ToDouble(sAmplitude));
        }

        private string GetCurrentAmplitudeUnitLabel()
        {
            if (m_MainTab.SelectedTab == m_tabRFGen)
            {
                if (m_ToolGroup_RFEGenTracking.ListSNAOptions.Contains("VSWR"))
                    return "";
                else
                    return "dB";
            }
            else
            //if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
            {
                if (menuItemWatt.Checked)
                    return "Watt";
                else if (menuItemDBUV.Checked)
                    return "dBuV";
                else
                    return "dBm";
            }
        }

        private RFECommunicator.eAmplitudeUnit GetCurrentAmplitudeEnum()
        {
            if (menuItemWatt.Checked)
                return RFECommunicator.eAmplitudeUnit.Watt;
            else if (menuItemDBUV.Checked)
                return RFECommunicator.eAmplitudeUnit.dBuV;
            else
                return RFECommunicator.eAmplitudeUnit.dBm;
        }

        private string GetCurrentAmplitudeUnitFormat()
        {
            if (menuItemWatt.Checked)
            {
                return "E3";
            }
            else
            {
                return "0.00";
            }
        }

        /// <summary>
        /// Based on the currently used amplitude unit, it will convert to the right string representation format
        /// </summary>
        /// <param name="dBm"></param>
        /// <returns></returns>
        private string ConvertToCurrentAmplitudeString(double dBm)
        {
            return ConvertToCurrentAmplitudeUnit(dBm).ToString(GetCurrentAmplitudeUnitFormat());
        }

        private void DisplaySpectrumAnalyzerData()
        {
#if CALLSTACK_REALTIME
            Console.WriteLine("CALLSTACK:DisplaySpectrumAnalyzerData");
#endif
            if (m_GraphSpectrumAnalyzer.GraphPane.CurveList.Count == 0)
                return;

            m_objRFEAnalyzer.PeakValueMHZ = 0.0;
            m_objRFEAnalyzer.PeakValueAmplitudeDBM = RFECommunicator.MIN_AMPLITUDE_DBM;
            foreach (CurveItem objCurve in m_GraphSpectrumAnalyzer.GraphPane.CurveList)
            {
                if (objCurve.Label.Text.Contains("Limit") || objCurve.Label.Text.Contains("Overload"))
                    continue;

                objCurve.Clear();
                objCurve.IsVisible = false;
                objCurve.Label.IsVisible = false;
                if (objCurve.IsLine)
                {
                    LineItem objLine = (LineItem)objCurve;
                    objLine.Line.IsSmooth = menuSmoothSignals.Checked;
                    if (menuThickTrace.Checked)
                    {
                        if (objLine.Label.Text == "Max Hold")
                            objLine.Line.Width = 6;
                        else
                            objLine.Line.Width = 3;
                    }
                    else
                    {
                        objLine.Line.Width = 1;
                    }
                    objLine.Line.FillFromBottom = menuSignalFill.Checked;
                    objLine.Line.Fill.IsVisible = menuSignalFill.Checked;
                }
            }
            m_GraphSpectrumAnalyzer.GraphPane.YAxis.MajorGrid.IsVisible = menuShowGrid.Checked;
            m_GraphSpectrumAnalyzer.GraphPane.YAxis.MinorGrid.IsVisible = menuShowGrid.Checked;
            m_GraphSpectrumAnalyzer.GraphPane.XAxis.MajorGrid.IsVisible = menuShowGrid.Checked;
            m_GraphSpectrumAnalyzer.GraphPane.XAxis.MinorGrid.IsVisible = menuShowGrid.Checked;
            m_GraphLine_Realtime.Line.Fill.IsVisible = false; //we never fill the realtime curve
            m_PointList_Realtime.Clear();
            m_PointList_Avg.Clear();
            m_PointList_Min.Clear();
            m_PointList_Max.Clear();
            m_PointList_MaxHold.Clear();
            m_MaxBar.Clear();

            m_MarkersSA.HideAllMarkers(); //hide markers and show them only based on menu settings
                                          //draw all marker except tracking 1
            UpdateMarkerCollectionFromMenuSA();
            //remove old text from all peak track markers
            m_MarkersSA.CleanAllMarkerText(0);

            if (m_objRFEAnalyzer.SweepData.Count == 0)
            {
                m_GraphSpectrumAnalyzer.Refresh();
                return; //nothing to paint
            }

            UInt32 nSweepIndex = m_ToolGroup_AnalyzerDataFeed.SweepIndex;
            m_nDrawingIteration++;

            UInt32 nTotalCalculatorIterations = m_ToolGroup_AnalyzerDataFeed.Iterations;
            if (m_bCalibrating)
            {
                nTotalCalculatorIterations = 5;
            }
            if (nTotalCalculatorIterations > nSweepIndex)
                nTotalCalculatorIterations = nSweepIndex;

            if ((m_nDrawingIteration & 0xf) == 0)
            {
                //Update screen status every 16 drawing iterations just to reduce overhead
                toolStripMemory.Value = (int)nSweepIndex;
                if (m_objRFEAnalyzer.PortConnected)
                    toolCOMStatus.Text = "Connected";
                else
                    toolCOMStatus.Text = "Disconnected";

                toolStripSamples.Text = "Total Samples in buffer: " + m_ToolGroup_AnalyzerDataFeed.SweepIndex + "/" + RFESweepDataCollection.MAX_ELEMENTS + " - " + (100 * (double)m_ToolGroup_AnalyzerDataFeed.SweepIndex / RFESweepDataCollection.MAX_ELEMENTS).ToString("0.0") + "%";
            }

            //Use the current data sweep item pointed out by the selected index
            RFESweepData objSweep = m_objRFEAnalyzer.SweepData.GetData(nSweepIndex);

            if (!m_objRFEAnalyzer.PortConnected || m_objRFEAnalyzer.HoldMode)
            {
                //If not connected, then rebuild the configuration text to properly show the capture date (if available)
                if (objSweep.CaptureTime.Year > 2000)
                {
                    m_StatusGraphText_Analyzer.Text = "Captured:" + objSweep.CaptureTime.ToString("yyyy-MM-dd HH:mm:ss\\.fff");
                }
                else
                {
                    m_StatusGraphText_Analyzer.Text = "Legacy file format - Unknown Capture Date";
                }
            }

            for (UInt16 nSweepPointInd = 0; objSweep != null && nSweepPointInd < m_objRFEAnalyzer.FreqSpectrumSteps; nSweepPointInd++)
            {
                if (nSweepPointInd < m_arrWiFiBarText.Length)
                    m_arrWiFiBarText[nSweepPointInd].Text = "";

                double dFreqStepMHZ = m_objRFEAnalyzer.StartFrequencyMHZ + m_objRFEAnalyzer.StepFrequencyMHZ * nSweepPointInd;
                double dRealtimeDBM = objSweep.GetAmplitudeDBM(nSweepPointInd);
                double dMaxDBM = dRealtimeDBM;
                double dMinDBM = dRealtimeDBM;
                double dAverageDBM = dRealtimeDBM;

                for (UInt32 nSweepIterator = nSweepIndex - nTotalCalculatorIterations; nSweepIterator < nSweepIndex; nSweepIterator++)
                {
                    //Calculate average over Calculator range
                    RFESweepData objSweepIter = m_objRFEAnalyzer.SweepData.GetData(nSweepIterator);
                    if (objSweepIter != null)
                    {
                        double dLocalAvgDBM = objSweepIter.GetAmplitudeDBM(nSweepPointInd);
                        dAverageDBM += dLocalAvgDBM;
                        if (dMinDBM > dLocalAvgDBM)
                        {
                            dMinDBM = dLocalAvgDBM;
                        }
                        if (dMaxDBM < dLocalAvgDBM)
                        {
                            dMaxDBM = dLocalAvgDBM;
                        }
                    }
                }

                if (menuUseAmplitudeCorrection.Checked || (menuRealtimeTrace.Checked && m_objRFEAnalyzer.Mode != RFECommunicator.eMode.MODE_WIFI_ANALYZER))
                {
                    //we always add realtime as may be needed for overload check, etc
                    m_PointList_Realtime.Add(dFreqStepMHZ, ConvertToCurrentAmplitudeUnit(dRealtimeDBM), RFECommunicator.MIN_AMPLITUDE_DBM);
                }
                if (menuMinTrace.Checked && m_objRFEAnalyzer.Mode != RFECommunicator.eMode.MODE_WIFI_ANALYZER)
                {
                    m_PointList_Min.Add(dFreqStepMHZ, ConvertToCurrentAmplitudeUnit(dMinDBM), RFECommunicator.MIN_AMPLITUDE_DBM);
                }
                if (menuMaxTrace.Checked || m_objRFEAnalyzer.Mode == RFECommunicator.eMode.MODE_WIFI_ANALYZER)
                {
                    m_PointList_Max.Add(dFreqStepMHZ, ConvertToCurrentAmplitudeUnit(dMaxDBM), RFECommunicator.MIN_AMPLITUDE_DBM);
                    if (m_objRFEAnalyzer.Mode == RFECommunicator.eMode.MODE_WIFI_ANALYZER && menuShowPeak.Checked)
                    {
                        if (nSweepPointInd < m_arrWiFiBarText.Length)
                        {
                            double fFreqMark = (m_objRFEAnalyzer.StartFrequencyMHZ + m_objRFEAnalyzer.StepFrequencyMHZ * nSweepPointInd);
                            m_arrWiFiBarText[nSweepPointInd].Text = "CH" + (nSweepPointInd + 1).ToString() + "\n" + fFreqMark.ToString("0") + "MHZ\n" + ConvertToCurrentAmplitudeString(dMaxDBM) + GetCurrentAmplitudeUnitLabel();
                            m_arrWiFiBarText[nSweepPointInd].Location.X = fFreqMark;
                            m_arrWiFiBarText[nSweepPointInd].Location.Y = ConvertToCurrentAmplitudeUnit(dMaxDBM);
                            m_arrWiFiBarText[nSweepPointInd].FontSpec.IsBold = false;
                            m_arrWiFiBarText[nSweepPointInd].FontSpec.FontColor = Color.Red;
                        }
                    }
                }
                if (menuMaxHoldTrace.Checked && (m_objRFEAnalyzer.SweepData.MaxHoldData != null) && m_objRFEAnalyzer.Mode != RFECommunicator.eMode.MODE_WIFI_ANALYZER)
                {
                    m_PointList_MaxHold.Add(dFreqStepMHZ, ConvertToCurrentAmplitudeUnit(m_objRFEAnalyzer.SweepData.MaxHoldData.GetAmplitudeDBM(nSweepPointInd)), RFECommunicator.MIN_AMPLITUDE_DBM);
                }
                if ((menuAveragedTrace.Checked && m_objRFEAnalyzer.Mode != RFECommunicator.eMode.MODE_WIFI_ANALYZER) || m_bCalibrating)
                {
                    dAverageDBM = dAverageDBM / (nTotalCalculatorIterations + 1);
                    m_PointList_Avg.Add(dFreqStepMHZ, ConvertToCurrentAmplitudeUnit(dAverageDBM), RFECommunicator.MIN_AMPLITUDE_DBM);
                }
            }

            m_OverloadText.IsVisible = false;
            if (!m_bCalibrating)
            {
                //limit lines
                bool bPlaySound = false;
                PointPairList listCheck = null;
                SelectSinglePointPairList(ref listCheck);
                m_GraphLimitLineAnalyzer_Max.Points = m_LimitLineAnalyzer_Max;
                m_GraphLimitLineAnalyzer_Max.IsVisible = m_LimitLineAnalyzer_Max.Count > 1;
                if (m_LimitLineAnalyzer_Max.Intersect(listCheck, true))
                {
                    bPlaySound = true;
                    m_GraphLimitLineAnalyzer_Max.Line.Width = 5;
                }
                else
                {
                    m_GraphLimitLineAnalyzer_Max.Line.Width = 1;
                }

                m_GraphLimitLineAnalyzer_Overload.Points = m_LimitLineAnalyzer_Overload;
                m_GraphLimitLineAnalyzer_Overload.IsVisible = (m_LimitLineAnalyzer_Overload.Count > 1) && menuUseAmplitudeCorrection.Checked;
                if (m_LimitLineAnalyzer_Overload.Intersect(m_PointList_Realtime, true))
                {
                    bPlaySound = true;
                    m_GraphLimitLineAnalyzer_Overload.Line.Width = 10;
                    m_OverloadText.IsVisible = true;
                }
                else
                {
                    m_GraphLimitLineAnalyzer_Overload.Line.Width = 1;
                }

                m_GraphLimitLineAnalyzer_Min.Points = m_LimitLineAnalyzer_Min;
                m_GraphLimitLineAnalyzer_Min.IsVisible = m_LimitLineAnalyzer_Min.Count > 1;
                if (m_LimitLineAnalyzer_Min.Intersect(listCheck, false))
                {
                    m_GraphLimitLineAnalyzer_Min.Line.Width = 5;
                    bPlaySound = true;
                }
                else
                {
                    m_GraphLimitLineAnalyzer_Min.Line.Width = 1;
                }
                if (bPlaySound && menuItemSoundAlarmLimitLine.Checked)
                {
                    PlayNotificationSound();
                }
                else
                {
                    StopNotificationSound();
                }

                if (m_objRFEAnalyzer.Mode == RFECommunicator.eMode.MODE_WIFI_ANALYZER)
                {
                    m_MaxBar.Points = m_PointList_Max;
                    m_MaxBar.IsVisible = true;
                }
                else
                {
                    UpdateMarkerCollectionFromMenuSA();

                    //Draw marker 1
                    double fTrackPeakMHZ = 0.0;
                    if (m_arrMarkersEnabledMenu[0].Checked)
                    {
                        double fTrackDBM = RFECommunicator.MIN_AMPLITUDE_DBM;
                        if ((m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Realtime) && menuRealtimeTrace.Checked)
                        {
                            fTrackPeakMHZ = m_PointList_Realtime[m_PointList_Realtime.GetIndexMax()].X;
                            fTrackDBM = m_PointList_Realtime[m_PointList_Realtime.GetIndexMax()].Y;
                        }
                        else if ((m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Average) && menuAveragedTrace.Checked)
                        {
                            fTrackPeakMHZ = m_PointList_Avg[m_PointList_Avg.GetIndexMax()].X;
                            fTrackDBM = m_PointList_Avg[m_PointList_Avg.GetIndexMax()].Y;
                        }
                        else if ((m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.MaxHold) && menuMaxHoldTrace.Checked)
                        {
                            fTrackPeakMHZ = m_PointList_MaxHold[m_PointList_MaxHold.GetIndexMax()].X;
                            fTrackDBM = m_PointList_MaxHold[m_PointList_MaxHold.GetIndexMax()].Y;
                        }
                        else if ((m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.MaxPeak) && menuMaxTrace.Checked)
                        {
                            fTrackPeakMHZ = m_PointList_Max[m_PointList_Max.GetIndexMax()].X;
                            fTrackDBM = m_PointList_Max[m_PointList_Max.GetIndexMax()].Y;
                        }
                        else if ((m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Min) && menuMinTrace.Checked)
                        {
                            fTrackPeakMHZ = m_PointList_Min[m_PointList_Min.GetIndexMax()].X;
                            fTrackDBM = m_PointList_Min[m_PointList_Min.GetIndexMax()].Y;
                        }
                        else
                        {
                            m_arrMarkersEnabledMenu[0].Checked = false;
                            m_MarkersSA.HideMarker(0);
                            m_ToolGroup_Markers_SA.UpdateButtonStatus();
                            UpdateSAMarkerControlContents();
                        }
                        m_MarkersSA.SetMarkerFrequency(0, fTrackPeakMHZ);
                        m_objRFEAnalyzer.PeakValueMHZ = fTrackPeakMHZ;
                        m_objRFEAnalyzer.PeakValueAmplitudeDBM = RFECommunicator.ConvertAmplitude(GetCurrentAmplitudeEnum(), fTrackDBM, RFECommunicator.eAmplitudeUnit.dBm);
                    }

                    //remove old text from all peak track markers
                    m_MarkersSA.CleanAllMarkerText(0);

                    //draw data curves
                    if (menuRealtimeTrace.Checked)
                    {
                        m_GraphLine_Realtime.Points = m_PointList_Realtime;
                        m_GraphLine_Realtime.IsVisible = true;
                        m_GraphLine_Realtime.Label.IsVisible = true;

                        if (m_arrMarkersEnabledMenu[0].Checked)
                        {
                            double dAmplitude = m_PointList_Realtime.InterpolateX(m_MarkersSA.GetMarkerFrequency(0));
                            m_MarkersSA.UpdateMarker(0, RFECommunicator.RFExplorerSignalType.Realtime, dAmplitude);
                            if ((m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Realtime) && menuShowPeak.Checked)
                            {
                                m_MarkersSA.SetMarkerText(0, RFECommunicator.RFExplorerSignalType.Realtime, m_MarkersSA.GetMarkerFrequency(0).ToString("0.000") + "MHZ\n" + dAmplitude.ToString(GetCurrentAmplitudeUnitFormat()) + GetCurrentAmplitudeUnitLabel());
                            }
                        }
                    }

                    if (menuAveragedTrace.Checked)
                    {
                        m_GraphLine_Avg.Points = m_PointList_Avg;
                        m_GraphLine_Avg.IsVisible = true;
                        m_GraphLine_Avg.Label.IsVisible = true;

                        if (m_arrMarkersEnabledMenu[0].Checked)
                        {
                            double dAmplitude = m_PointList_Avg.InterpolateX(m_MarkersSA.GetMarkerFrequency(0));
                            m_MarkersSA.UpdateMarker(0, RFECommunicator.RFExplorerSignalType.Average, dAmplitude);
                            if ((m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Average) && menuShowPeak.Checked)
                            {
                                m_MarkersSA.SetMarkerText(0, RFECommunicator.RFExplorerSignalType.Average, m_MarkersSA.GetMarkerFrequency(0).ToString("0.000") + "MHZ\n" + dAmplitude.ToString(GetCurrentAmplitudeUnitFormat()) + GetCurrentAmplitudeUnitLabel());
                            }
                        }
                    }

                    if (menuMaxTrace.Checked)
                    {
                        if (m_objRFEAnalyzer.Mode == RFECommunicator.eMode.MODE_SPECTRUM_ANALYZER)
                        {
                            m_GraphLine_Max.Points = m_PointList_Max;
                            m_GraphLine_Max.IsVisible = true;
                            m_GraphLine_Max.Label.IsVisible = true;

                            if (m_arrMarkersEnabledMenu[0].Checked)
                            {
                                double dAmplitude = m_PointList_Max.InterpolateX(m_MarkersSA.GetMarkerFrequency(0));
                                m_MarkersSA.UpdateMarker(0, RFECommunicator.RFExplorerSignalType.MaxPeak, dAmplitude);
                                if ((m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.MaxPeak) && menuShowPeak.Checked)
                                {
                                    m_MarkersSA.SetMarkerText(0, RFECommunicator.RFExplorerSignalType.MaxPeak, m_MarkersSA.GetMarkerFrequency(0).ToString("0.000") + "MHZ\n" + dAmplitude.ToString(GetCurrentAmplitudeUnitFormat()) + GetCurrentAmplitudeUnitLabel());
                                }
                            }
                        }
                    }
                    if (menuMinTrace.Checked)
                    {
                        m_GraphLine_Min.Points = m_PointList_Min;
                        m_GraphLine_Min.IsVisible = true;
                        m_GraphLine_Min.Label.IsVisible = true;
                        if (m_arrMarkersEnabledMenu[0].Checked)
                        {
                            double dAmplitude = m_PointList_Min.InterpolateX(m_MarkersSA.GetMarkerFrequency(0));
                            m_MarkersSA.UpdateMarker(0, RFECommunicator.RFExplorerSignalType.Min, dAmplitude);
                            if ((m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Min) && menuShowPeak.Checked)
                            {
                                m_MarkersSA.SetMarkerText(0, RFECommunicator.RFExplorerSignalType.Min, m_MarkersSA.GetMarkerFrequency(0).ToString("0.000") + "MHZ\n" + dAmplitude.ToString(GetCurrentAmplitudeUnitFormat()) + GetCurrentAmplitudeUnitLabel());
                            }
                        }
                    }
                    if (menuMaxHoldTrace.Checked)
                    {
                        m_GraphLine_MaxHold.Points = m_PointList_MaxHold;
                        m_GraphLine_MaxHold.IsVisible = true;
                        m_GraphLine_MaxHold.Label.IsVisible = true;
                        if (m_arrMarkersEnabledMenu[0].Checked)
                        {
                            double dAmplitude = m_PointList_MaxHold.InterpolateX(m_MarkersSA.GetMarkerFrequency(0));
                            m_MarkersSA.UpdateMarker(0, RFECommunicator.RFExplorerSignalType.MaxHold, dAmplitude);
                            if ((m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.MaxHold) && menuShowPeak.Checked)
                            {
                                m_MarkersSA.SetMarkerText(0, RFECommunicator.RFExplorerSignalType.MaxHold, m_MarkersSA.GetMarkerFrequency(0).ToString("0.000") + "MHZ\n" + dAmplitude.ToString(GetCurrentAmplitudeUnitFormat()) + GetCurrentAmplitudeUnitLabel());
                            }

                        }
                    }
                }

                UpdateSAMarkerControlValues();

                if (!m_bPrintModeEnabled)
                    m_GraphSpectrumAnalyzer.Refresh();

                if (m_MainTab.SelectedTab == m_tabPowerChannel)
                {
                    double fPowerDBM = 0.0f;
                    string sPowerMode = "Power Data Mode: ";

                    //Note: this is BETA and should be done by looking into sweep data collection, but we are not doing
                    //it here that way to avoid recalculation of max or average which will be costly.
                    if (menuAveragedTrace.Checked)
                    {
                        double fPowerTemp = 0.0f;
                        sPowerMode += "Average";

                        for (UInt16 nInd = 0; nInd < m_PointList_Avg.Count; nInd++)
                        {
                            fPowerTemp += RFECommunicator.ConvertAmplitude(GetCurrentAmplitudeEnum(), m_PointList_Avg[nInd].Y, RFECommunicator.eAmplitudeUnit.Watt);
                        }

                        if (fPowerTemp > 0.0f)
                        {
                            //add here actual RBW calculation in the future - currently we are assuming frequency step is the same
                            //as RBW which is not 100% accurate.
                            fPowerDBM = RFECommunicator.ConvertAmplitude(RFECommunicator.eAmplitudeUnit.Watt, fPowerTemp, RFECommunicator.eAmplitudeUnit.dBm);
                        }
                    }
                    else if (menuMaxTrace.Checked)
                    {
                        double fPowerTemp = 0.0f;
                        sPowerMode += "Max";

                        for (UInt16 nInd = 0; nInd < m_PointList_Max.Count; nInd++)
                        {
                            fPowerTemp += RFECommunicator.ConvertAmplitude(GetCurrentAmplitudeEnum(), m_PointList_Max[nInd].Y, RFECommunicator.eAmplitudeUnit.Watt);
                        }

                        if (fPowerTemp > 0.0f)
                        {
                            //add here actual RBW calculation in the future - currently we are assuming frequency step is the same
                            //as RBW which is not 100% accurate.
                            fPowerDBM = RFECommunicator.ConvertAmplitude(RFECommunicator.eAmplitudeUnit.Watt, fPowerTemp, RFECommunicator.eAmplitudeUnit.dBm);
                        }
                    }
                    else
                    {
                        sPowerMode += "Realtime";
                        fPowerDBM = objSweep.GetChannelPowerDBM();
                    }

                    double fPowerWatt = RFECommunicator.Convert_dBm_2_Watt(fPowerDBM);
                    double fPowerDensityDBM = RFECommunicator.Convert_Watt_2_dBm(fPowerWatt / (objSweep.GetFrequencySpanMHZ() * 1E6));
                    string sWatt = " Watt";
                    if (fPowerWatt < 0.5E-9)
                    {
                        fPowerWatt *= 1E12;
                        sWatt = " pW";
                    }
                    else if (fPowerWatt < 0.5E-6)
                    {
                        fPowerWatt *= 1E9;
                        sWatt = " nW";
                    }
                    else if (fPowerWatt < 0.5E-3)
                    {
                        fPowerWatt *= 1E6;
                        sWatt = " uW";
                    }
                    else if (fPowerWatt < 0.5f)
                    {
                        fPowerWatt *= 1E3;
                        sWatt = " mW";
                    }

                    m_PowerChannelNeedle.NeedleValue = fPowerDBM;

                    m_PowerChannelText.Text = sPowerMode +
                         "\nChannel Power: " + fPowerDBM.ToString("f1") + "dBm " + fPowerWatt.ToString("f1") + sWatt +
                         "\nChannel Power density: " + fPowerDensityDBM.ToString("f1") + " dBm/Hz" +
                         "\nChannel Center: " + objSweep.GetFrequencyMHZ((ushort)(objSweep.TotalSteps / 2)).ToString("f3") + " MHz" +
                         "\nChannel Bandwidth: " + objSweep.GetFrequencySpanMHZ().ToString("f3") + " MHz";

                    m_graphPowerChannel.Refresh();
                }
            }
            else
            {
                m_objRFEAnalyzer.PeakValueMHZ = m_PointList_Avg[m_PointList_Avg.GetIndexMax()].X;
                m_objRFEAnalyzer.PeakValueAmplitudeDBM = RFECommunicator.ConvertAmplitude(GetCurrentAmplitudeEnum(), m_PointList_Avg[m_PointList_Avg.GetIndexMax()].Y, RFECommunicator.eAmplitudeUnit.dBm);
            }
        }

        private void UpdateMarkerCollectionFromMenuSA()
        {
            if (m_arrMarkersEnabledMenu[0].Checked)
            {
                if (menuRealtimeTrace.Checked)
                    m_MarkersSA.UpdateMarker(0, RFECommunicator.RFExplorerSignalType.Realtime, RFECommunicator.MIN_AMPLITUDE_DBM);
                if (menuAveragedTrace.Checked)
                    m_MarkersSA.UpdateMarker(0, RFECommunicator.RFExplorerSignalType.Average, RFECommunicator.MIN_AMPLITUDE_DBM);
                if (menuMaxTrace.Checked)
                    m_MarkersSA.UpdateMarker(0, RFECommunicator.RFExplorerSignalType.MaxPeak, RFECommunicator.MIN_AMPLITUDE_DBM);
                if (menuMaxHoldTrace.Checked)
                    m_MarkersSA.UpdateMarker(0, RFECommunicator.RFExplorerSignalType.MaxHold, RFECommunicator.MIN_AMPLITUDE_DBM);
                if (menuMinTrace.Checked)
                    m_MarkersSA.UpdateMarker(0, RFECommunicator.RFExplorerSignalType.Min, RFECommunicator.MIN_AMPLITUDE_DBM);
            }

            for (int nMenuInd = 1; nMenuInd < m_arrMarkersEnabledMenu.Length; nMenuInd++)
            {
                if (m_arrMarkersEnabledMenu[nMenuInd].Checked)
                {
                    if (menuRealtimeTrace.Checked)
                    {
                        if (m_PointList_Realtime != null && m_PointList_Realtime.Count > 0)
                            m_MarkersSA.UpdateMarker(nMenuInd, RFECommunicator.RFExplorerSignalType.Realtime, m_PointList_Realtime.InterpolateX(m_MarkersSA.GetMarkerFrequency(nMenuInd)));
                        else
                            m_MarkersSA.UpdateMarker(nMenuInd, RFECommunicator.RFExplorerSignalType.Realtime, RFECommunicator.MIN_AMPLITUDE_DBM);
                    }
                    if (menuAveragedTrace.Checked)
                    {
                        if (m_PointList_Avg != null && m_PointList_Avg.Count > 0)
                            m_MarkersSA.UpdateMarker(nMenuInd, RFECommunicator.RFExplorerSignalType.Average, m_PointList_Avg.InterpolateX(m_MarkersSA.GetMarkerFrequency(nMenuInd)));
                        else
                            m_MarkersSA.UpdateMarker(nMenuInd, RFECommunicator.RFExplorerSignalType.Average, RFECommunicator.MIN_AMPLITUDE_DBM);
                    }
                    if (menuMaxTrace.Checked)
                    {
                        if (m_PointList_Max != null && m_PointList_Max.Count > 0)
                            m_MarkersSA.UpdateMarker(nMenuInd, RFECommunicator.RFExplorerSignalType.MaxPeak, m_PointList_Max.InterpolateX(m_MarkersSA.GetMarkerFrequency(nMenuInd)));
                        else
                            m_MarkersSA.UpdateMarker(nMenuInd, RFECommunicator.RFExplorerSignalType.MaxPeak, RFECommunicator.MIN_AMPLITUDE_DBM);
                    }
                    if (menuMaxHoldTrace.Checked)
                    {
                        if (m_PointList_MaxHold != null && m_PointList_MaxHold.Count > 0)
                            m_MarkersSA.UpdateMarker(nMenuInd, RFECommunicator.RFExplorerSignalType.MaxHold, m_PointList_MaxHold.InterpolateX(m_MarkersSA.GetMarkerFrequency(nMenuInd)));
                        else
                            m_MarkersSA.UpdateMarker(nMenuInd, RFECommunicator.RFExplorerSignalType.MaxHold, RFECommunicator.MIN_AMPLITUDE_DBM);
                    }
                    if (menuMinTrace.Checked)
                    {
                        if (m_PointList_Min != null && m_PointList_Min.Count > 0)
                            m_MarkersSA.UpdateMarker(nMenuInd, RFECommunicator.RFExplorerSignalType.Min, m_PointList_Min.InterpolateX(m_MarkersSA.GetMarkerFrequency(nMenuInd)));
                        else
                            m_MarkersSA.UpdateMarker(nMenuInd, RFECommunicator.RFExplorerSignalType.Min, RFECommunicator.MIN_AMPLITUDE_DBM);
                    }
                }
            }
        }

        private string GraphPointValueHandler(ZedGraphControl control, GraphPane pane, CurveItem curve, int iPt)
        {
            // Get the PointPair that is under the mouse
            PointPair pt = curve[iPt];
            return pt.X.ToString("f3") + "MHZ\r\n" + pt.Y.ToString(GetCurrentAmplitudeUnitFormat()) + " " + GetCurrentAmplitudeUnitLabel();
        }

        private void zedSpectrumAnalyzer_ZoomEvent(ZedGraphControl sender, ZoomState oldState, ZoomState newState)
        {

        }

        private void SendNewConfig(double fStartMHZ, double fEndMHZ, double fTopDBM, double fBottomDBM)
        {
            //#[32]C2-F:Sssssss,Eeeeeee,tttt,bbbb
            UInt32 nStartKhz = (UInt32)(fStartMHZ * 1000);
            UInt32 nEndKhz = (UInt32)(fEndMHZ * 1000);
            Int16 nTopDBM = (Int16)fTopDBM;
            Int16 nBottomDBM = (Int16)fBottomDBM;

            string sTopDBM = "";
            if (nTopDBM >= 0)
                sTopDBM = nTopDBM.ToString("D4");
            else
                sTopDBM = nTopDBM.ToString("D3");

            string sData = "C2-F:" +
                nStartKhz.ToString("D7") + "," + nEndKhz.ToString("D7") + "," +
                sTopDBM + "," + nBottomDBM.ToString("D3");
            m_objRFEAnalyzer.SendCommand(sData);

            ResetSettingsTitle();

            Thread.Sleep(500); //wait some time for the unit to process changes, otherwise may get a different command too soon
        }

        private void ResetSettingsTitle()
        {
            if (m_sLastSettingsLoaded != _Default)
            {
                m_sLastSettingsLoaded = _Default; //if we change the current configuration by hand, then it becomes _Default again
                UpdateTitleText_Analyzer();
            }
        }
        private void CleanSweepData()
        {
            m_objRFEAnalyzer.SweepData.CleanAll();
            m_WaterfallSweepMaxHold.CleanAll();
            m_sFilenameRFE = "";
            UpdateButtonStatus();
            if (m_objRFEAnalyzer.HoldMode)
            {
                UpdateFeedMode();
                SetupSpectrumAnalyzerAxis();
                DisplaySpectrumAnalyzerData();
                m_GraphSpectrumAnalyzer.Invalidate();
            }
            WaterfallClean();
            m_ToolGroup_AnalyzerDataFeed.SweepIndex = 0;
        }

        private void OnRunModeChanged(object sender, EventArgs e)
        {
            if (!m_objRFEAnalyzer.HoldMode && (m_objRFEAnalyzer.SweepData.IsFull()))
            {
                CleanSweepData();
                ReportLog("Buffer cleared.", false);
            }
            UpdateFeedMode();
        }

        private void OnHoldModeChanged(object sender, EventArgs e)
        {
            if (!m_objRFEAnalyzer.HoldMode)
            {
                Thread.Sleep(50);
            }
            UpdateFeedMode();
        }

        private void OnSweepIndexChanged(object sender, EventArgs e)
        {
            if ((m_MainTab.SelectedTab == m_tabSpectrumAnalyzer) || (m_MainTab.SelectedTab == m_tabPowerChannel))
            {
                DisplaySpectrumAnalyzerData();
                if (menuPlaceWaterfallAtBottom.Checked || menuPlaceWaterfallOnTheRight.Checked) //Split screen
                {
                    WaterfallClean();
                    UpdateWaterfall();
                }
            }
            else
            {
                WaterfallClean();
                UpdateWaterfall();
            }
        }

        private void WaterfallClean()
        {
            m_objMainWaterfall.CleanAll();
            m_objSAWaterfall.CleanAll();
        }

        private void OnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OnAbout_Click(object sender, EventArgs e)
        {
            using (About_RFExplorer myAbout = new About_RFExplorer())
            {
                myAbout.ShowDialog();
            }
        }

        private void MainMenuView_DropDownOpening(object sender, EventArgs e)
        {
            RFEWaterfallGL.SharpGLForm objWaterfall = null;
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
                objWaterfall = m_objSAWaterfall;
            else
                objWaterfall = m_objMainWaterfall;

            menuTransparentWaterfall.Checked = objWaterfall.Transparent;
            menuWaterfallFloor.Checked = objWaterfall.DrawFloor;

            menuLimitLines.Enabled = (m_MainTab.SelectedTab == m_tabRFGen) || (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer);
            menuMarkers.Enabled = (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer) || (m_MainTab.SelectedTab == m_tabRFGen);
            menuPrint.Enabled = (m_MainTab.SelectedTab == m_tabRFGen) || (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer);
            menuPrintPreview.Enabled = (m_MainTab.SelectedTab == m_tabRFGen) || (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer);
            menuSmoothSignals.Enabled = (m_MainTab.SelectedTab == m_tabRFGen) || (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer);
            menuCalculatorSignalModes.Enabled = (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer);
            menuSignalFill.Enabled = (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer);
            menuThickTrace.Enabled = (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer);
            menuShowPeak.Enabled = (m_MainTab.SelectedTab == m_tabRFGen) || (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer);
            menuAmplitudeUnits.Enabled = (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer);
            menuShowAxisLabels.Enabled = (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer);
            menuCleanReport.Enabled = (m_MainTab.SelectedTab == m_tabReport);
        }

        private void click_view_mode(object sender, EventArgs e)
        {
            CheckSomeTraceModeIsEnabled();
            if (menuShowPeak.Checked)
            {
                m_arrMarkersEnabledMenu[0].Checked = true;
                m_MarkersSA.EnableMarker(0);
            }
            UpdateSAMarkerControlContents();
            UpdateButtonStatus();

            if (m_objRFEAnalyzer.HoldMode)
                DisplaySpectrumAnalyzerData();
        }

        private void OnSaveAsRFE_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog MySaveFileDialog = new SaveFileDialog())
                {
                    MySaveFileDialog.Filter = _RFE_File_Selector;
                    MySaveFileDialog.FilterIndex = 1;
                    MySaveFileDialog.RestoreDirectory = false;
                    MySaveFileDialog.InitialDirectory = m_sDefaultUserFolder;

                    GetNewFilename(RFExplorerFileType.SweepDataFile);
                    MySaveFileDialog.FileName = m_sFilenameRFE;

                    if (MySaveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        SaveFileRFE(MySaveFileDialog.FileName, false);
                        m_sDefaultUserFolder = Path.GetDirectoryName(MySaveFileDialog.FileName);
                        edDefaultFilePath.Text = m_sDefaultUserFolder;
                    }
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
        }

        private void OnSaveCSV_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog MySaveFileDialog = new SaveFileDialog())
                {
                    MySaveFileDialog.Filter = _CSV_File_Selector;
                    MySaveFileDialog.FilterIndex = 1;
                    MySaveFileDialog.RestoreDirectory = false;
                    MySaveFileDialog.InitialDirectory = m_sDefaultUserFolder;

                    MySaveFileDialog.FileName = GetNewFilename(RFExplorerFileType.CumulativeCSVDataFile);

                    if (MySaveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        SaveFileCSV(MySaveFileDialog.FileName);
                        m_sDefaultUserFolder = Path.GetDirectoryName(MySaveFileDialog.FileName);
                        edDefaultFilePath.Text = m_sDefaultUserFolder;
                    }
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
        }

        private void OnSaveSimpleCSV_Click(object sender, EventArgs e)
        {
            try
            {
                PointPairList listCurrentPointList = null;
                int nSelectionCounter = SelectSinglePointPairList(ref listCurrentPointList);

                if (nSelectionCounter == 0)
                {
                    MessageBox.Show("Single Signal CSV export needs one active display curve on screen (Avg, Max, Min or Realtime)", "Single Curve CSV Export");
                    return;
                }
                else if (nSelectionCounter > 1)
                {
                    MessageBox.Show("Single Signal CSV export requires one active display curve only on screen (Avg, Max, Min or Realtime)", "Single Curve CSV Export");
                    return;
                }

                using (SaveFileDialog MySaveFileDialog = new SaveFileDialog())
                {
                    MySaveFileDialog.Filter = _CSV_File_Selector;
                    MySaveFileDialog.FilterIndex = 1;
                    MySaveFileDialog.RestoreDirectory = false;
                    MySaveFileDialog.InitialDirectory = m_sDefaultUserFolder;

                    MySaveFileDialog.FileName = GetNewFilename(RFExplorerFileType.SimpleCSVDataFile);

                    if (MySaveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        SaveSimpleCSV(MySaveFileDialog.FileName, listCurrentPointList);
                        m_sDefaultUserFolder = Path.GetDirectoryName(MySaveFileDialog.FileName);
                        edDefaultFilePath.Text = m_sDefaultUserFolder;
                    }
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
        }

        private char GetCSVDelimiter()
        {
            char cReturn = '\t';

            /*
                Comma (,)
                Division (|)
                Semicolon (;)
                Space ( )
                Tabulator (\t)
             */

            switch (comboCSVFieldSeparator.SelectedIndex)
            {
                case 0: cReturn = ','; break;
                case 1: cReturn = '|'; break;
                case 2: cReturn = ';'; break;
                case 3: cReturn = ' '; break;
                default:
                case 4: cReturn = '\t'; break;
            }

            return cReturn;
        }

        private void SaveSimpleCSV(string sFilename, PointPairList listCurrentPointList)
        {
            try
            {
                //if no file path was explicit, add the default folder
                if (sFilename.IndexOf("\\") < 0)
                {
                    sFilename = m_sDefaultUserFolder + "\\" + sFilename;
                    sFilename = sFilename.Replace("\\\\", "\\");
                }

                char cCSV = GetCSVDelimiter();

                using (StreamWriter myFile = new StreamWriter(sFilename, false))
                {
                    foreach (PointPair objPointPair in listCurrentPointList)
                    {
                        myFile.WriteLine(objPointPair.X.ToString("0.000") + cCSV + objPointPair.Y.ToString("0.00"));
                    }
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            try
            {
                menuSaveAsRFE.Enabled = (m_objRFEAnalyzer.SweepData.Count > 0) && (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer);
                menuSaveCSV.Enabled = (m_objRFEAnalyzer.SweepData.Count > 0) && (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer);
                menusSaveSimpleCSV.Enabled = menuSaveCSV.Enabled;
                menuLoadRFE.Enabled = m_MainTab.SelectedTab == m_tabSpectrumAnalyzer;

                menuSaveRemoteImage.Enabled = (controlRemoteScreen.RFExplorer.ScreenData.Count > 0) && (m_MainTab.SelectedTab == m_tabRemoteScreen);
                menuLoadRFS.Enabled = m_MainTab.SelectedTab == m_tabRemoteScreen;
                menuSaveRFS.Enabled = (controlRemoteScreen.RFExplorer.ScreenData.Count > 0) && (m_MainTab.SelectedTab == m_tabRemoteScreen);

                menuPrint.Enabled = m_MainTab.SelectedTab == m_tabSpectrumAnalyzer || m_MainTab.SelectedTab == m_tabRFGen; ;
                menuPrintPreview.Enabled = m_MainTab.SelectedTab == m_tabSpectrumAnalyzer || m_MainTab.SelectedTab == m_tabRFGen;
                menuPageSetup.Enabled = m_MainTab.SelectedTab == m_tabSpectrumAnalyzer || m_MainTab.SelectedTab == m_tabRFGen; ;

                menuSaveSNACSV.Enabled = m_PointList_Tracking_Avg.Count > 0;
                menuSaveS1P.Enabled = m_PointList_Tracking_Avg.Count > 0 && (m_ToolGroup_RFEGenTracking.ListSNAOptionsIndex != 2);
                menuSaveSNANormalization.Enabled = m_objRFEAnalyzer.PortConnected && m_objRFEGenerator.PortConnected && m_objRFEAnalyzer.IsTrackingNormalized();
                menuLoadSNANormalization.Enabled = m_objRFEAnalyzer.PortConnected && m_objRFEGenerator.PortConnected;

                if (Properties.Settings.Default.MRUList.Length == 0)
                {
                    menuMRU.Enabled = false;
                    menuMRU.Visible = false;
                }
                else
                {
                    menuMRU.Enabled = true;
                    menuMRU.Visible = true;

                    if (menuMRU.DropDownItems.Count == 0)
                    {
                        //add one sub item just to display it properly
                        ToolStripMenuItem menuNew = new ToolStripMenuItem(".");
                        menuNew.Name = "MRUFile_0";
                        menuNew.Click += new System.EventHandler(this.OnMRU_Click);
                        menuMRU.DropDownItems.Add(menuNew);
                    }
                }
            }
            catch (Exception obEx)
            {
                ReportLog(obEx.ToString(), true);
            }
        }

        private void UpdateTitleText_Analyzer()
        {
            if (m_objRFEAnalyzer.HoldMode)
            {
                if (m_sFilenameRFE.Length > 0)
                {
                    m_GraphSpectrumAnalyzer.GraphPane.Title.Text = "RF Explorer File data";
                }
                else
                {
                    m_GraphSpectrumAnalyzer.GraphPane.Title.Text = "RF Explorer ON HOLD";
                }
                zedRAWDecoder.GraphPane.Title.Text = "RF Explorer Decoder ON HOLD";
            }
            else
            {
                if (m_objRFEAnalyzer.PortConnected)
                {
                    m_GraphSpectrumAnalyzer.GraphPane.Title.Text = "RF Explorer Live data";
                    zedRAWDecoder.GraphPane.Title.Text = "RF Explorer Decoder Live Data";
                }
                else
                {
                    m_GraphSpectrumAnalyzer.GraphPane.Title.Text = "RF Explorer Disconnected";
                    zedRAWDecoder.GraphPane.Title.Text = "RF Explorer Disconnected";
                }
            }

            m_GraphSpectrumAnalyzer.GraphPane.Title.Text += " - " + m_sLastSettingsLoaded + Environment.NewLine + m_sUserDefinedText;
            m_GraphTrackingGenerator.GraphPane.Title.Text = _RFEGEN_TRACKING_TITLE + " - " + m_sLastSettingsLoaded + Environment.NewLine + m_sUserDefinedText;
        }

        private void UpdateFeedMode()
        {
            if (!m_objRFEAnalyzer.PortConnected)
            {
                m_objRFEAnalyzer.HoldMode = true;
            }

            m_ToolGroup_AnalyzerDataFeed.UpdateButtonStatus();
            chkRunDecoder.Checked = !m_objRFEAnalyzer.HoldMode;
            chkHoldDecoder.Checked = m_objRFEAnalyzer.HoldMode;
            if ((m_objRFEAnalyzer.HoldMode == false) || (m_objRFEAnalyzer.SweepData.Count == 0))
            {
                toolFile.Text = " - File: none";
                m_sFilenameRFE = "";
            }
            else if ((m_objRFEAnalyzer.HoldMode == true) && (m_objRFEAnalyzer.SweepData.Count > 0))
            {
                toolFile.Text = " - File: " + m_sFilenameRFE;
            }

            UpdateTitleText_Analyzer();

            m_GraphSpectrumAnalyzer.Refresh();
            zedRAWDecoder.Refresh();
        }

        /// <summary>
        /// This function save RFE format file
        /// </summary>
        /// <param name="sFilename">path where file will be saved</param>
        /// <param name="bAutosave">true if the file is saved automatically otherwise (saved by user) false</param>
        private void SaveFileRFE(string sFilename, bool bAutosave)
        {
            try
            {
                //if no file path was explicit, add the default folder
                if (sFilename.IndexOf("\\") < 0)
                {
                    if (bAutosave)
                        sFilename = m_sDefaultDataFolder + "\\" + sFilename;
                    else
                        sFilename = m_sDefaultUserFolder + "\\" + sFilename;
                    sFilename = sFilename.Replace("\\\\", "\\");
                }
                m_objRFEAnalyzer.SaveFileRFE(sFilename, menuUseAmplitudeCorrection.Checked);
            }
            catch (Exception obEx) { MessageBox.Show(obEx.ToString()); }
        }

        private void SaveFileCSV(string sFilename)
        {
            try
            {
                //if no file path was explicited, add the default folder
                if (sFilename.IndexOf("\\") < 0)
                {
                    sFilename = m_sDefaultUserFolder + "\\" + sFilename;
                    sFilename = sFilename.Replace("\\\\", "\\");
                }
                m_objRFEAnalyzer.SweepData.SaveFileCSV(sFilename, GetCSVDelimiter(), null);

            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
        }

        private void LoadFileRFE(string sFile)
        {
            Cursor.Current = Cursors.WaitCursor;
            if (m_objRFEAnalyzer.PortConnected)
            {
                m_ToolGroup_COMPortAnalyzer.ClosePort();
                m_ToolGroup_COMPortGenerator.ClosePort();
            }
            try
            {
                CleanSweepData();

                if (m_objRFEAnalyzer.LoadDataFile(sFile))
                {
                    menuUseAmplitudeCorrection.Checked = false;

                    CheckSomeTraceModeIsEnabled();
                    m_ToolGroup_AnalyzerDataFeed.UpdateNumericControls();

                    UpdateButtonStatus();
                    UpdateConfigControlContents(m_panelSAConfiguration, m_objRFEAnalyzer);

                    ReportLog("File " + sFile + " loaded with total of " + m_objRFEAnalyzer.SweepData.Count + " sweeps.", false);
                    m_sFilenameRFE = sFile;

                    UpdateFeedMode();

                    AutoLoadAmplitudeDataFile();

                    if (m_LimitLineAnalyzer_Overload.Count > 0)
                    {
                        //potentially offset may change
                        m_LimitLineAnalyzer_Overload.NewOffset(m_objRFEAnalyzer.AmplitudeOffsetDB);
                    }

                    SetupSpectrumAnalyzerAxis();
                    DisplaySpectrumAnalyzerData();
                    UpdateWaterfall();

                    AddMRUFile(m_sFilenameRFE);
                }
                else
                {
                    MessageBox.Show("Wrong or unknown file format");
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
            Cursor.Current = Cursors.Default;
        }

        private void AddMRUFile(string sFile)
        {
            string sStoredMRU = Properties.Settings.Default.MRUList;

            if (sStoredMRU.Contains(sFile))
            {
                //avoid the file to be included twice, but we want it to be added on top so added it even if it was before
                //remove it in all posible combinations it can be included
                sStoredMRU = sStoredMRU.Replace(sFile + ";", "");
                sStoredMRU = sStoredMRU.Replace(";" + sFile, "");
                sStoredMRU = sStoredMRU.Replace(sFile, "");
            }

            if (sStoredMRU.Length > 1)
            {
                sStoredMRU = sFile + ";" + sStoredMRU;
            }
            else
            {
                sStoredMRU = sFile;
            }

            //add the new file on top of the list
            Properties.Settings.Default.MRUList = sStoredMRU;
        }

        private void OnLoadFileRFE_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog MyOpenFileDialog = new OpenFileDialog())
                {
                    MyOpenFileDialog.Filter = _RFE_File_Selector;
                    MyOpenFileDialog.FilterIndex = 1;
                    MyOpenFileDialog.RestoreDirectory = false;
                    MyOpenFileDialog.InitialDirectory = m_sDefaultUserFolder;

                    if (MyOpenFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        LoadFileRFE(MyOpenFileDialog.FileName);
                        m_sDefaultUserFolder = Path.GetDirectoryName(MyOpenFileDialog.FileName);
                        edDefaultFilePath.Text = m_sDefaultUserFolder;
                    }
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
        }

        private void objGraph_ContextMenuBuilder(ZedGraphControl sender, ContextMenuStrip menuStrip, Point mousePt, ZedGraphControl.ContextMenuObjectState objState)
        {
            menuStrip.Items["save_as"].Click += new System.EventHandler(OnSaveImage_Click);
            menuStrip.Items["copy"].Click += new System.EventHandler(OnClipboard_Click);
        }

        protected void UpdateYAxis()
        {
#if CALLSTACK
            Console.WriteLine("CALLSTACK:UpdateYAxis");
#endif
            //check and fix absolute margins
            if (m_objRFEAnalyzer.AmplitudeBottomNormalizedDBM < RFECommunicator.MIN_AMPLITUDE_DBM)
            {
                m_objRFEAnalyzer.AmplitudeBottomNormalizedDBM = RFECommunicator.MIN_AMPLITUDE_DBM;
            }
            if (m_objRFEAnalyzer.AmplitudeTopNormalizedDBM > RFECommunicator.MAX_AMPLITUDE_DBM)
            {
                m_objRFEAnalyzer.AmplitudeTopNormalizedDBM = RFECommunicator.MAX_AMPLITUDE_DBM;
            }

            //Check and fix relative margins
            if (m_objRFEAnalyzer.AmplitudeTopNormalizedDBM < (RFECommunicator.MIN_AMPLITUDE_DBM + RFECommunicator.MIN_AMPLITUDE_RANGE_DBM))
            {
                m_objRFEAnalyzer.AmplitudeTopNormalizedDBM = RFECommunicator.MIN_AMPLITUDE_DBM + RFECommunicator.MIN_AMPLITUDE_RANGE_DBM;
            }
            if (m_objRFEAnalyzer.AmplitudeBottomNormalizedDBM >= m_objRFEAnalyzer.AmplitudeTopNormalizedDBM)
            {
                m_objRFEAnalyzer.AmplitudeBottomNormalizedDBM = m_objRFEAnalyzer.AmplitudeTopNormalizedDBM - RFECommunicator.MIN_AMPLITUDE_RANGE_DBM;
            }

            m_GraphSpectrumAnalyzer.GraphPane.YAxis.Scale.MajorStepAuto = false;
            m_GraphSpectrumAnalyzer.GraphPane.YAxis.Scale.MinorStepAuto = false;
            m_GraphSpectrumAnalyzer.GraphPane.YAxis.Title.Text = "Amplitude (" + GetCurrentAmplitudeUnitLabel() + ")";
            m_GraphSpectrumAnalyzer.GraphPane.YAxis.MinorGrid.IsVisible = menuItemWatt.Checked;

            if (menuItemWatt.Checked)
            {
                //Convert to Watt
                m_GraphSpectrumAnalyzer.GraphPane.YAxis.Type = AxisType.Log;
                m_GraphSpectrumAnalyzer.GraphPane.YAxis.Scale.MajorStep = 1;
                m_GraphSpectrumAnalyzer.GraphPane.YAxis.Scale.MinorStepAuto = true;
            }
            else
            {
                //valid for dBm and dBuV
                m_GraphSpectrumAnalyzer.GraphPane.YAxis.Type = AxisType.Linear;
                m_GraphSpectrumAnalyzer.GraphPane.YAxis.Scale.MajorStep = 10.0;
                if ((m_objRFEAnalyzer.AmplitudeTopDBM - m_objRFEAnalyzer.AmplitudeBottomDBM) > 30)
                {
                    m_GraphSpectrumAnalyzer.GraphPane.YAxis.Scale.MinorStep = 5.0;
                }
                else
                {
                    m_GraphSpectrumAnalyzer.GraphPane.YAxis.Scale.MinorStep = 1.0;
                }
            }
            m_GraphSpectrumAnalyzer.GraphPane.YAxis.Scale.Min = ConvertToCurrentAmplitudeUnit(m_objRFEAnalyzer.AmplitudeBottomDBM);
            m_GraphSpectrumAnalyzer.GraphPane.YAxis.Scale.Max = ConvertToCurrentAmplitudeUnit(m_objRFEAnalyzer.AmplitudeTopDBM);

            m_GraphSpectrumAnalyzer.Refresh();
        }

        private void OnCOMPortInfo_Click(object sender, EventArgs e)
        {
            m_objRFEAnalyzer.ListAllCOMPorts();
        }

        private bool IsDifferent(double d1, double d2, double dEpsilon = 0.001)
        {
            return (Math.Abs(d1 - d2) > dEpsilon);
        }

        private void OnSendAnalyzerConfiguration(object sender, EventArgs e)
        {
            UpdateYAxis();
        }

        private void OnMoveFreqDecLarge_Click(object sender, EventArgs e)
        {
            double fStartFreq = m_ToolGroup_AnalyzerFreqSettings.FreqStart;
            fStartFreq -= m_objRFEAnalyzer.CalculateFrequencySpanMHZ() * 0.5;
            m_ToolGroup_AnalyzerFreqSettings.FreqStart = fStartFreq;
            SaveProperties();
        }

        private void OnMoveFreqIncLarge_Click(object sender, EventArgs e)
        {
            double fStartFreq = m_ToolGroup_AnalyzerFreqSettings.FreqStart;
            fStartFreq += m_objRFEAnalyzer.CalculateFrequencySpanMHZ() * 0.5;
            m_ToolGroup_AnalyzerFreqSettings.FreqStart = fStartFreq;
            SaveProperties();
        }

        private void OnMoveFreqDecSmall_Click(object sender, EventArgs e)
        {
            double fStartFreq = m_objRFEAnalyzer.StartFrequencyMHZ;
            fStartFreq -= m_objRFEAnalyzer.CalculateFrequencySpanMHZ() / 10;
            m_ToolGroup_AnalyzerFreqSettings.FreqStart = fStartFreq;
            SaveProperties();
        }

        private void OnMoveFreqIncSmall_Click(object sender, EventArgs e)
        {
            double fStartFreq = m_objRFEAnalyzer.StartFrequencyMHZ;
            fStartFreq += m_objRFEAnalyzer.CalculateFrequencySpanMHZ() / 10;
            m_ToolGroup_AnalyzerFreqSettings.FreqStart = fStartFreq;
            SaveProperties();
        }

        private void OnSpanDec_Click(object sender, EventArgs e)
        {
            double fFreqSpan = m_ToolGroup_AnalyzerFreqSettings.FreqSpan;
            fFreqSpan -= fFreqSpan * 0.25;
            m_ToolGroup_AnalyzerFreqSettings.FreqSpan = fFreqSpan;
        }

        private void OnSpanInc_Click(object sender, EventArgs e)
        {
            double fFreqSpan = m_ToolGroup_AnalyzerFreqSettings.FreqSpan;
            fFreqSpan += fFreqSpan * 0.25;
            m_ToolGroup_AnalyzerFreqSettings.FreqSpan = fFreqSpan;
        }

        private void OnSpanMax_Click(object sender, EventArgs e)
        {
            double fFreqSpan = m_ToolGroup_AnalyzerFreqSettings.FreqSpan;
            fFreqSpan = 10000; //just a big number
            m_ToolGroup_AnalyzerFreqSettings.FreqSpan = fFreqSpan;
        }

        private void OnSpanDefault_Click(object sender, EventArgs e)
        {
            m_ToolGroup_AnalyzerFreqSettings.FreqSpan = 10;
        }

        private void OnSpanMin_Click(object sender, EventArgs e)
        {
            m_ToolGroup_AnalyzerFreqSettings.FreqSpan = 0; //just a very small number
        }

        private void IncreaseTopAmplitude(double dIncreaseAmplitudeDBM)
        {
            m_objRFEAnalyzer.AmplitudeTopDBM += dIncreaseAmplitudeDBM;
            m_ToolGroup_AnalyzerFreqSettings.SetNewAmplitude(m_objRFEAnalyzer.AmplitudeBottomDBM, m_objRFEAnalyzer.AmplitudeTopDBM);

            if (m_objRFEAnalyzer.PortConnected && menuRemoteAmplitudeUpdate.Checked && menuAutoLCDOff.Checked == false)
            {
                m_ToolGroup_AnalyzerFreqSettings.UpdateRemoteConfigData();
            }
            else
            {
                UpdateYAxis();
                DisplaySpectrumAnalyzerData();
            }
        }

        private void IncreaseBottomAmplitude(double dIncreaseAmplitudeDBM)
        {
            m_objRFEAnalyzer.AmplitudeBottomDBM += dIncreaseAmplitudeDBM;
            m_ToolGroup_AnalyzerFreqSettings.SetNewAmplitude(m_objRFEAnalyzer.AmplitudeBottomDBM, m_objRFEAnalyzer.AmplitudeTopDBM);

            if (m_objRFEAnalyzer.PortConnected && menuRemoteAmplitudeUpdate.Checked && menuAutoLCDOff.Checked == false)
            {
                m_ToolGroup_AnalyzerFreqSettings.UpdateRemoteConfigData();
            }
            else
            {
                UpdateYAxis();
                DisplaySpectrumAnalyzerData();
            }
        }

        private void OnAutoscale_Click(object sender, EventArgs e)
        {
            double fTop = ConvertToCurrentAmplitudeUnit(RFECommunicator.MIN_AMPLITUDE_DBM);
            double fBottom = ConvertToCurrentAmplitudeUnit(RFECommunicator.MAX_AMPLITUDE_DBM);
            int nVal1, nVal2;

            if (menuRealtimeTrace.Checked)
            {
                m_PointList_Realtime.GetMaxMinValues(ref fBottom, ref fTop, out nVal1, out nVal2);
            }
            if (menuAveragedTrace.Checked)
            {
                m_PointList_Avg.GetMaxMinValues(ref fBottom, ref fTop, out nVal1, out nVal2);
            }
            if (menuMaxTrace.Checked)
            {
                m_PointList_Max.GetMaxMinValues(ref fBottom, ref fTop, out nVal1, out nVal2);
            }
            if (menuMaxHoldTrace.Checked)
            {
                m_PointList_MaxHold.GetMaxMinValues(ref fBottom, ref fTop, out nVal1, out nVal2);
            }
            if (menuMinTrace.Checked)
            {
                m_PointList_Min.GetMaxMinValues(ref fBottom, ref fTop, out nVal1, out nVal2);
            }

            if (menuItemWatt.Checked)
            {
                fTop += fTop * 10; //add a range on top
                fBottom -= fBottom * 0.1; //add a range at the bottom
            }
            else
            {
                if ((fTop - fBottom) < 20)
                {
                    fTop += 10;
                    fBottom -= 10;
                }

                fTop += (fTop - fBottom) * 0.3; //add a 30% on top
                fBottom -= (fTop - fBottom) * 0.1; //add a 10% at the bottom
            }

            m_objRFEAnalyzer.AmplitudeTopDBM = ConvertFromCurrentAmplitudeUnit(fTop);
            m_objRFEAnalyzer.AmplitudeBottomDBM = ConvertFromCurrentAmplitudeUnit(fBottom);

            m_ToolGroup_AnalyzerFreqSettings.SetNewAmplitude(m_objRFEAnalyzer.AmplitudeBottomDBM, m_objRFEAnalyzer.AmplitudeTopDBM);

            if (m_objRFEAnalyzer.PortConnected && menuRemoteAmplitudeUpdate.Checked && menuAutoLCDOff.Checked == false)
            {
                m_ToolGroup_AnalyzerFreqSettings.UpdateRemoteConfigData();
            }

            m_GraphSpectrumAnalyzer.ZoomOutAll(m_GraphSpectrumAnalyzer.GraphPane);
            UpdateYAxis();
            DisplaySpectrumAnalyzerData();
        }

        private void OnTop5plus_Click(object sender, EventArgs e)
        {
            IncreaseTopAmplitude(5);
        }

        private void OnTop5minus_Click(object sender, EventArgs e)
        {
            IncreaseTopAmplitude(-5);
        }

        private void OnBottom5plus_Click(object sender, EventArgs e)
        {
            IncreaseBottomAmplitude(5);
        }

        private void OnBottom5minus_Click(object sender, EventArgs e)
        {
            IncreaseBottomAmplitude(-5);
        }

        private void OnCenterMark_Click(object sender, EventArgs e)
        {
            if (m_objRFEAnalyzer.PeakValueMHZ > 0.0f)
            {
                m_ToolGroup_AnalyzerFreqSettings.FreqCenter = m_objRFEAnalyzer.PeakValueMHZ;
            }
        }

        /// <summary>
        /// Function required to force a true redraw of the waterfall, otherwise some driver bug may not repaint the whole control.
        /// A way to reproduce this problem without this fix:
        /// * Open application with waterfall 2D at bottom in SA tab
        /// * Switch to Waterfall tab
        /// * Get back to SA tab
        /// * Change the waterfall control in SA tab to Perspective1 mode. Without this function call the control will not fully repaint from now on.
        /// </summary>
        void WaterfallSAInvalidate()
        {
            if (m_objSAWaterfall != null)
            {
                m_objPanelSAWaterfall.Visible = false;
                Size BackupSize = m_objPanelSAWaterfall.Size;
                m_objPanelSAWaterfall.Size = new Size(BackupSize.Width / 2, BackupSize.Height / 2);
                m_objPanelSAWaterfall.Size = BackupSize;
                m_objPanelSAWaterfall.Visible = true;
            }
        }

        void WaterfallMainInvalidate()
        {
            if (m_objMainWaterfall != null)
            {
                m_objPanelMainWaterfall.Visible = false;
                Size BackupSize = m_objPanelMainWaterfall.Size;
                m_objPanelMainWaterfall.Size = new Size(BackupSize.Width / 2, BackupSize.Height / 2);
                m_objPanelMainWaterfall.Size = BackupSize;
                m_objPanelMainWaterfall.Visible = true;
            }
        }

        private void tabSpectrumAnalyzer_Enter(object sender, EventArgs e)
        {
            UpdateAllWaterfallMenuItems();
            UpdateMenuFromMarkerCollection(false);
            DisplayGroups();
            if (IsWaterfallOnMainScreen())
            {
                WaterfallSAInvalidate();
                WaterfallClean(); 
                UpdateWaterfall();
            }
        }

        private void OnCleanReport_Click(object sender, EventArgs e)
        {
            m_ReportTextBox.Text = "Text cleared." + Environment.NewLine;
        }

        private void OnReinitializeData_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes == MessageBox.Show("Are you sure to reinitialize data buffer?", "Reinitialize data buffer", MessageBoxButtons.YesNo))
            {
                if (m_MainTab.SelectedTab == m_tabRemoteScreen)
                {
                    m_objRFEAnalyzer.CleanScreenData();
                    m_objRFEGenerator.CleanScreenData();
                    m_sFilenameRFS = "";
                    m_ToolGroup_RemoteScreen.ScreenIndex = 0;
                    MessageBox.Show("Remote Screen buffer cleared.");
                }
                else
                {
                    CleanSweepData();
                    MessageBox.Show("Data buffer cleared.");
                }
            }
        }

        private void OnShowControlArea_Click(object sender, EventArgs e)
        {
            DisplayGroups();
        }

        private void MainForm_Layout(object sender, LayoutEventArgs e)
        {
#if CALLSTACK
            Console.WriteLine("CALLSTACK:MainForm_Layout");
#endif
            DisplayGroups();
            tabRemoteScreen_UpdateZoomValues();
        }

        private Size m_SizePriorMainTab = new Size(-1, -1);
        private void MainTab_ClientSizeChanged(object sender, EventArgs e)
        {
            //This is looking for 2 difference in width because some sort of weird behavior makes it repaint more than needed for repeated 2 pixel changes
            //for some reason this does not happen in height but in width only
            if ((m_SizePriorMainTab.Width != m_MainTab.ClientSize.Width) || (m_SizePriorMainTab.Height != m_MainTab.ClientSize.Height))
            {
#if CALLSTACK
                Console.WriteLine("CALLSTACK:MainTab_ClientSizeChanged " + m_SizePriorMainTab.ToString() + " " + m_MainTab.ClientSize.ToString());
#endif
                //this is to fix the problem of Tabs not resizing their childs automatically
                m_MainTab.Refresh();
                m_objPanelMainWaterfall.Refresh();
                m_panelRemoteScreen.Refresh();
                m_SizePriorMainTab = m_MainTab.ClientSize;
            }
        }

        private void OnRFModuleSelectorConfig_HideControl(object sender, EventArgs e)
        {
            menuRFConnections.Checked = false;
            DisplayGroups();
        }

        bool m_bInsideDisplayGroups = false; //avoid recursive calls
        private void DisplayGroups()
        {
            if (!m_bLayoutInitialized)
                return;

            if (m_bInsideDisplayGroups)
            {
                //sanity check to avoid any potential nested call to this expensive redraw method
#if CALLSTACK
                Console.WriteLine("CALLSTACK:DisplayGroups nested");
#endif
                return;
            }

            m_bInsideDisplayGroups = true;
            try
            {
#if CALLSTACK
                Console.WriteLine("CALLSTACK:DisplayGroups");
#endif

                if (m_MainTab.Width != (Width - 16))
                {
                    //important: do not use anything smaller than 16 or may provoke unnecessary back and forth refresh
                    m_MainTab.Width = Width - 16;
                }
                if (m_MainTab.Height != Height - 64)
                {
                    m_MainTab.Height = Height - 64;
                }

                if (m_tableLayoutControlArea.Parent != m_MainTab.SelectedTab)
                {
                    m_tableLayoutControlArea.Parent = m_MainTab.SelectedTab;
                }
                m_tableLayoutControlArea.Visible = menuShowControlArea.Checked;

                int nTop = m_tableLayoutControlArea.Top;
                if (menuShowControlArea.Checked)
                {
                    nTop = m_tableLayoutControlArea.Bottom;
                }

                int nButtonPositions = 0;

                m_ToolGroup_COMPortAnalyzer.Visible = m_MainTab.SelectedTab != m_tabRFGen;
                m_ToolGroup_AnalyzerFreqSettings.Visible = m_MainTab.SelectedTab != m_tabRFGen;
                m_ToolGroup_AnalyzerDataFeed.Visible = m_MainTab.SelectedTab == m_tabSpectrumAnalyzer;
                m_ToolGroup_AnalyzerTraces.Visible = m_MainTab.SelectedTab == m_tabSpectrumAnalyzer;
                m_ToolGroup_Markers_SA.Visible = m_MainTab.SelectedTab == m_tabSpectrumAnalyzer;
                m_ToolGroup_COMPortGenerator.Visible = m_MainTab.SelectedTab == m_tabRFGen;
                m_ToolGroup_Commands.Visible = m_MainTab.SelectedTab == m_tabReport;
                m_ToolGroup_RemoteScreen.Visible = m_MainTab.SelectedTab == m_tabRemoteScreen;
                m_ToolGroupRFEGenFreqSweep.Visible = m_MainTab.SelectedTab == m_tabRFGen;
                m_ToolGroupRFEGenAmplSweep.Visible = m_MainTab.SelectedTab == m_tabRFGen;
                m_ToolGroup_RFGenCW.Visible = m_MainTab.SelectedTab == m_tabRFGen;
                m_ToolGroup_RFEGenTracking.Visible = m_MainTab.SelectedTab == m_tabRFGen;
                m_ToolGroup_Markers_SNA.Visible = m_MainTab.SelectedTab == m_tabRFGen;
                m_objMainWaterfall.DrawTitle = true;

                if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
                {
                    m_GraphSpectrumAnalyzer.Top = nTop + 5;
                    m_GraphSpectrumAnalyzer.Height = m_MainStatusBar.Top - m_GraphSpectrumAnalyzer.Top - 3;
                    m_GraphSpectrumAnalyzer.Width = Width - (int)(btnBottom5plus.Width * 1.3) - 16;
                    m_GraphSpectrumAnalyzer.Width -= (m_panelSAMarkers.Width + 8);
                    m_GraphSpectrumAnalyzer.Left = 6;

                    m_objPanelSAWaterfall.Visible = IsWaterfallOnMainScreen();

                    if (IsWaterfallOnMainScreen())
                    {
                        if (menuPlaceWaterfallAtBottom.Checked)
                        {
                            m_objSAWaterfall.DrawTitle = false;

                            int nSABottom = m_GraphSpectrumAnalyzer.Bottom;
                            m_GraphSpectrumAnalyzer.Height /= 2;
                            m_objPanelSAWaterfall.Left = m_GraphSpectrumAnalyzer.Left;
                            m_objPanelSAWaterfall.Width = m_GraphSpectrumAnalyzer.Width;
                            m_objPanelSAWaterfall.Top = m_GraphSpectrumAnalyzer.Bottom + 5;
                            m_objPanelSAWaterfall.Height = nSABottom - m_objPanelSAWaterfall.Top;
                        }
                        else
                        {
                            int nSAWidth = m_GraphSpectrumAnalyzer.Width;
                            m_GraphSpectrumAnalyzer.Width /= 2;
                            m_objPanelSAWaterfall.Left = m_GraphSpectrumAnalyzer.Right + 5;
                            m_objPanelSAWaterfall.Height = m_GraphSpectrumAnalyzer.Height;
                            m_objPanelSAWaterfall.Top = m_GraphSpectrumAnalyzer.Top;
                            m_objPanelSAWaterfall.Width = nSAWidth - m_objPanelSAWaterfall.Left;
                        }
                    }

                    UpdateConfigControlContents(m_panelSAConfiguration, m_objRFEAnalyzer);
                    UpdateSAMarkerControlContents();
                    nButtonPositions = m_panelSAMarkers.Right + 8;

                    if (menuRFConnections.Checked)
                    {
                        m_controlRFModuleSelectorConfig.AllowHideControl = true;
                        m_controlRFModuleSelectorConfig.RFExplorer = m_objRFEAnalyzer;
                        m_controlRFModuleSelectorConfig.SelectImageFromConfiguration();

                        int nWidth = 133;
                        if ((m_GraphSpectrumAnalyzer.Width > 800) && (m_GraphSpectrumAnalyzer.Height > 400))
                        {
                            int nExtraWidth = m_GraphSpectrumAnalyzer.Width - 800;
                            nWidth = nExtraWidth / 2;
                            if (nWidth < 133)
                                nWidth = 133;
                        }

                        m_panelRFConnections.Visible = true;
                        m_panelRFConnections.Dock = DockStyle.None;
                        m_panelRFConnections.Height = 20; //start with small size in order to grow for proper location
                        m_panelRFConnections.Width = 10;
                        m_panelRFConnections.Parent = m_tabSpectrumAnalyzer;
                        m_panelRFConnections.Height = m_GraphSpectrumAnalyzer.Height; //first do this to get the control calculate aspect ration
                        m_panelRFConnections.Width = nWidth - 10;
                        m_panelRFConnections.Height = m_controlRFModuleSelectorConfig.ActualPictureHeight; //later do this to reuse what it calculated as best height
                        m_panelRFConnections.Left = 6;
                        m_panelRFConnections.Top = m_GraphSpectrumAnalyzer.Top + (m_GraphSpectrumAnalyzer.Height - m_panelRFConnections.Height) / 2;

                        m_GraphSpectrumAnalyzer.Width -= m_controlRFModuleSelectorConfig.ActualPictureWidth;
                        m_GraphSpectrumAnalyzer.Left = m_controlRFModuleSelectorConfig.ActualPictureWidth;
                        m_GraphSpectrumAnalyzer.BringToFront();
                    }
                    else
                    {
                        m_panelRFConnections.Visible = false;
                    }
                }
                else if (m_MainTab.SelectedTab == m_tabWaterfall)
                {
                    m_objPanelMainWaterfall.Top = nTop + 5;
                    m_objPanelMainWaterfall.Height = m_MainStatusBar.Top - m_objPanelMainWaterfall.Top - 3;
                    m_objPanelMainWaterfall.Left = 6;
                    m_objPanelMainWaterfall.Width = Width - 35;
                    m_ToolGroup_AnalyzerDataFeed.Visible = true;
                }
                else if (m_MainTab.SelectedTab == m_tabRFGen)
                {
                    m_ToolGroup_AnalyzerFreqSettings.Visible = false;
                    m_ToolGroup_AnalyzerDataFeed.Visible = false;
                    m_ToolGroupRFEGenFreqSweep.Visible = true;
                    m_ToolGroupRFEGenAmplSweep.Visible = true;
                    m_ToolGroup_RFGenCW.Visible = true;
                    m_ToolGroup_RFEGenTracking.Visible = true;
                    m_ToolGroup_Markers_SNA.Visible = true;
                    m_ToolGroup_COMPortAnalyzer.Visible = false;
                    m_ToolGroup_COMPortGenerator.Visible = true;

                    m_GraphTrackingGenerator.Top = nTop + 5;
                    m_GraphTrackingGenerator.Height = m_MainStatusBar.Top - m_GraphTrackingGenerator.Top - 3;
                    m_GraphTrackingGenerator.Width = Width - 35;
                    m_GraphTrackingGenerator.Width -= (m_panelSNAMarkers.Width + 8);
                    m_GraphTrackingGenerator.Left = 6;

                    DisplayGroups_RFGen();
                }

                if (m_MainTab.SelectedTab == m_tabRemoteScreen)
                {
                    m_ToolGroup_RemoteScreen.Visible = true;
                    m_ToolGroup_AnalyzerDataFeed.Visible = false;
                    m_ToolGroup_RemoteScreen.Parent = m_tableLayoutControlArea;

                    m_ToolGroup_RemoteScreen.Dock = DockStyle.Top;

                    m_panelRemoteScreen.Top = nTop + 5;
                    m_panelRemoteScreen.Width = Width - 45;
                    m_panelRemoteScreen.Height = m_MainStatusBar.Top - nTop - 10;
                    m_panelRemoteScreen.BorderStyle = BorderStyle.FixedSingle;
                }

                if (m_MainTab.SelectedTab == m_tabPowerChannel)
                {
                    m_ToolGroup_AnalyzerDataFeed.Visible = true;

                    m_panelPowerChannel.Top = nTop + 5;
                    m_panelPowerChannel.Left = 6;
                    m_panelPowerChannel.Width = Width - 35;
                    m_panelPowerChannel.Height = m_MainStatusBar.Top - nTop - 10;
                    m_panelPowerChannel.BorderStyle = BorderStyle.FixedSingle;

                    double dMin = m_objRFEAnalyzer.AmplitudeBottomDBM;
                    double dMax = m_objRFEAnalyzer.AmplitudeTopDBM;

                    m_PowerChannelRegion_Low.MinValue = dMin;
                    m_PowerChannelRegion_Low.MaxValue = dMin + ((dMax - dMin) * 0.25);
                    m_PowerChannelRegion_High.MinValue = dMin + ((dMax - dMin) * 0.75);
                    m_PowerChannelRegion_High.MaxValue = dMax;

                    m_PowerChannelRegion_Medium.MinValue = m_PowerChannelRegion_Low.MaxValue;
                    m_PowerChannelRegion_Medium.MaxValue = m_PowerChannelRegion_High.MinValue;

                    m_panelPowerChannel.Left = 5;
                    //m_graphPowerChannel.Width = m_panelPowerChannel.Width - 10;
                }

                if (m_MainTab.SelectedTab == m_tabReport)
                {
                    m_ToolGroup_Commands.Visible = true;
                    m_ToolGroup_Commands.Parent = m_tableLayoutControlArea;

                    m_ToolGroup_AnalyzerDataFeed.Visible = false;
                    m_ToolGroup_AnalyzerFreqSettings.Visible = false;
                    m_ReportTextBox.Top = nTop + 5;
                    m_ReportTextBox.Width = Width - 35;
                    m_ReportTextBox.Height = m_MainStatusBar.Top - nTop - 10;
                }

                if (m_MainTab.SelectedTab == m_tabConfiguration)
                {
                    m_controlRFModuleSelectorConfig.AllowHideControl = false;

                    m_panelGeneralConfigTab.Left = m_tableLayoutControlArea.Left;
                    m_panelGeneralConfigTab.Top = nTop + 5;
                    m_controlRFModuleSelectorConfig.RFExplorer = m_objRFEAnalyzer;
                    m_controlRFModuleSelectorConfig.SelectImageFromConfiguration();

                    m_panelRFConnections.Visible = true;
                    m_panelRFConnections.Height = 20; //start with small size in order to grow for proper location
                    m_panelRFConnections.Width = 20;
                    m_panelRFConnections.Parent = m_tableConfiguration;
                    m_panelRFConnections.Dock = DockStyle.Fill;

                    m_ToolGroup_AnalyzerDataFeed.Visible = false;
                    m_ToolGroup_AnalyzerFreqSettings.Visible = false;
                }

                m_GraphSpectrumAnalyzer.Visible = true;
                RelocateRemoteControl();

                btnAutoscale.Top = m_GraphSpectrumAnalyzer.Top;
                btnAutoscale.Left = nButtonPositions;
                if (m_arrAnalyzerButtonList[0] != null)
                {
                    btnAutoscale.Visible = true;
                    for (int nInd = 1; nInd < m_arrAnalyzerButtonList.Length; nInd++)
                    {
                        m_arrAnalyzerButtonList[nInd].Top = m_arrAnalyzerButtonList[nInd - 1].Bottom + 3;
                        m_arrAnalyzerButtonList[nInd].Left = m_arrAnalyzerButtonList[0].Left;
                        m_arrAnalyzerButtonList[nInd].Visible = true;
                    }
                }
            }
            catch (Exception obEx)
            {
                ReportLog(obEx.ToString(), false);
            }
            m_bInsideDisplayGroups = false;
        }

        private bool IsWaterfallOnMainScreen()
        {
            return menuPlaceWaterfallAtBottom.Checked || menuPlaceWaterfallOnTheRight.Checked;
        }

        private void OnPlaceWaterfallAtBottom_Click(object sender, EventArgs e)
        {
            menuPlaceWaterfallNone.Checked = false;
            menuPlaceWaterfallOnTheRight.Checked = false;
            menuPlaceWaterfallAtBottom.Checked = true;
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
            {
                DisplayGroups();
                UpdateWaterfall();
            }
        }

        private void OnPlaceWaterfallOnTheRight_Click(object sender, EventArgs e)
        {
            menuPlaceWaterfallNone.Checked = false;
            menuPlaceWaterfallAtBottom.Checked = false;
            menuPlaceWaterfallOnTheRight.Checked = true;
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
            {
                DisplayGroups();
                UpdateWaterfall();
            }
        }

        private void OnWaterfallPlaceNone_Click(object sender, EventArgs e)
        {
            menuPlaceWaterfallOnTheRight.Checked = false;
            menuPlaceWaterfallAtBottom.Checked = false;
            menuPlaceWaterfallNone.Checked = true;
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
            {
                DisplayGroups();
            }
        }

        private void OnDarkMode_Click(object sender, EventArgs e)
        {
            DefineGraphColors();
            Invalidate();
            if ((m_MainTab.SelectedTab == m_tabWaterfall) || IsWaterfallOnMainScreen())
            {
                UpdateWaterfall();
            }
        }

        /// <summary>
        /// based on a predefined hierarchy, it will select the <param name="listCurrentPointList"> and
        /// will tell how many curves are displayed.
        /// </summary>
        /// <param name="listCurrentPointList"></param>
        /// <returns></returns>
        private int SelectSinglePointPairList(ref PointPairList listCurrentPointList)
        {
            if (m_MainTab.SelectedTab == m_tabRFGen)
            {
                if (m_PointList_Tracking_Avg != null && m_PointList_Tracking_Avg.Count > 0)
                {
                    listCurrentPointList = m_PointList_Tracking_Avg;
                    return 1;
                }
                else
                {
                    listCurrentPointList = null;
                    return 0;
                }
            }
            else
            {
                int nSelectionCounter = 0;
                if (menuAveragedTrace.Checked)
                {
                    nSelectionCounter++;
                    listCurrentPointList = m_PointList_Avg;
                }
                if (menuMaxHoldTrace.Checked)
                {
                    nSelectionCounter++;
                    listCurrentPointList = m_PointList_MaxHold;
                }
                if (menuMaxTrace.Checked)
                {
                    listCurrentPointList = m_PointList_Max;
                    nSelectionCounter++;
                }
                if (menuMinTrace.Checked)
                {
                    nSelectionCounter++;
                    listCurrentPointList = m_PointList_Min;
                }
                if (menuRealtimeTrace.Checked)
                {
                    listCurrentPointList = m_PointList_Realtime;
                    nSelectionCounter++;
                }
                return nSelectionCounter;
            }
        }

        private void OnShowAxisLabels_Click(object sender, EventArgs e)
        {
            m_GraphSpectrumAnalyzer.GraphPane.XAxis.Title.IsVisible = menuShowAxisLabels.Checked;
            m_GraphSpectrumAnalyzer.GraphPane.YAxis.Title.IsVisible = menuShowAxisLabels.Checked;
            m_GraphSpectrumAnalyzer.Refresh();
            UpdateButtonStatus();
        }

        private void OnItemAmplitudeUnit_Click(object sender, EventArgs e)
        {
            menuItemDBM.Checked = (sender.ToString() == menuItemDBM.Text);
            menuItemDBUV.Checked = (sender.ToString() == menuItemDBUV.Text);
            menuItemWatt.Checked = (sender.ToString() == menuItemWatt.Text);

            m_objRFEAnalyzer.CurrentAmplitudeUnit = GetCurrentAmplitudeEnum();

            //rescale limit lines according to new units
            m_LimitLineAnalyzer_Overload.AmplitudeUnits = GetCurrentAmplitudeEnum();
            m_LimitLineAnalyzer_Min.AmplitudeUnits = GetCurrentAmplitudeEnum();
            m_LimitLineAnalyzer_Max.AmplitudeUnits = GetCurrentAmplitudeEnum();

            m_ToolGroup_AnalyzerFreqSettings.UpdateButtonStatus(true);
            UpdateYAxis();
            DisplaySpectrumAnalyzerData();
        }

        private void PlayNotificationSound()
        {
            try
            {
                if (!m_bSoundPlaying)
                {
                    m_SoundPlayer.PlayLooping();
                    m_bSoundPlaying = true;
                }
            }
            catch (Exception obEx)
            {
                MessageBox.Show(obEx.Message);
                ReportLog(obEx.ToString(), false);
            }
        }

        private void StopNotificationSound()
        {
            try
            {
                if (m_bSoundPlaying)
                {
                    m_SoundPlayer.Stop();
                    m_bSoundPlaying = false;
                }
            }
            catch (Exception obEx)
            {
                MessageBox.Show(obEx.Message);
                ReportLog(obEx.ToString(), false);
            }
        }

        private void OnThickTrace_Click(object sender, EventArgs e)
        {
            if (m_objRFEAnalyzer.HoldMode)
                DisplaySpectrumAnalyzerData();
            UpdateButtonStatus();
        }

        private void OnShowGrid_Click(object sender, EventArgs e)
        {
            if (m_objRFEAnalyzer.HoldMode)
                DisplaySpectrumAnalyzerData();

            if (m_MainTab.SelectedTab == m_tabRFGen)
                DisplayTrackingData();
            UpdateButtonStatus();
        }
        #endregion

        #region Waterfall

        private void tabWaterfall_Enter(object sender, EventArgs e)
        {
            UpdateAllWaterfallMenuItems();
            DisplayGroups();
            WaterfallMainInvalidate();
            UpdateWaterfall();
        }

        /// <summary>
        /// Main drawing / iteration function for Waterfall display
        /// </summary>
        private void UpdateWaterfall()
        {
            RFEWaterfallGL.SharpGLForm objWaterfall = null;
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
            {
                if (!IsWaterfallOnMainScreen())
                    return;
                objWaterfall = m_objSAWaterfall;
            }
            else
                objWaterfall = m_objMainWaterfall;

            objWaterfall.DarkMode = menuDarkMode.Checked;

            if (m_objRFEAnalyzer.SweepData.Count == 0)
                return; //nothing to paint

            UInt32 nSourceSweepIndex = 0;
            switch (objWaterfall.SignalType)
            {
                default:
                case RFECommunicator.RFExplorerSignalType.Realtime:
                    nSourceSweepIndex = m_ToolGroup_AnalyzerDataFeed.SweepIndex - 1;
                    break;
                case RFECommunicator.RFExplorerSignalType.MaxHold:
                    nSourceSweepIndex = m_WaterfallSweepMaxHold.Count - 1;
                    break;
            }

            objWaterfall.UpdateWaterfallGL(m_WaterfallSweepMaxHold, nSourceSweepIndex, menuUseAmplitudeCorrection.Checked);
        }

        private void OnWaterfallContextRealtime_Click(object sender, EventArgs e)
        {
            RFEWaterfallGL.SharpGLForm objWaterfall = null;
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
                objWaterfall = m_objSAWaterfall;
            else
                objWaterfall = m_objMainWaterfall;

            objWaterfall.SignalType = RFECommunicator.RFExplorerSignalType.Realtime;
            UpdateAllWaterfallMenuItems();
            WaterfallClean();
            UpdateWaterfall();
            objWaterfall.Invalidate();
        }

        private void OnWaterfallContextMaxHold_Click(object sender, EventArgs e)
        {
            RFEWaterfallGL.SharpGLForm objWaterfall = null;
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
                objWaterfall = m_objSAWaterfall;
            else
                objWaterfall = m_objMainWaterfall;

            Cursor.Current = Cursors.WaitCursor;
            objWaterfall.SignalType = RFECommunicator.RFExplorerSignalType.MaxHold;
            UpdateAllWaterfallMenuItems();
            WaterfallClean();
            UpdateWaterfall();
            objWaterfall.Invalidate();
            Cursor.Current = Cursors.Default;
        }

        private void OnTransparentWaterfall_Click(object sender, EventArgs e)
        {
            RFEWaterfallGL.SharpGLForm objWaterfall = null;
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
                objWaterfall = m_objSAWaterfall;
            else
                objWaterfall = m_objMainWaterfall;
            ToolStripMenuItem objMenu = (ToolStripMenuItem)sender;
            objWaterfall.Transparent = objMenu.Checked;
            objWaterfall.Invalidate();
            UpdateAllWaterfallMenuItems();
        }

        private void OnWaterfallFloor_Click(object sender, EventArgs e)
        {
            RFEWaterfallGL.SharpGLForm objWaterfall = null;
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
                objWaterfall = m_objSAWaterfall;
            else
                objWaterfall = m_objMainWaterfall;
            ToolStripMenuItem objMenu = (ToolStripMenuItem)sender;
            objWaterfall.DrawFloor = objMenu.Checked;
            objWaterfall.Invalidate();
            UpdateAllWaterfallMenuItems();
        }

        private void UpdateAllWaterfallMenuItems()
        {
            RFEWaterfallGL.SharpGLForm objWaterfall = null;
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
                objWaterfall = m_objSAWaterfall;
            else
                objWaterfall = m_objMainWaterfall;

            menuWaterfallContextPerspective1.Checked = false;
            menuWaterfallContextPerspective2.Checked = false;
            menuWaterfallContextISO.Checked = false;
            menuWaterfallContext2D.Checked = false;
            menuWaterfallPerspective1.Checked = false;
            menuWaterfallPerspective2.Checked = false;
            menuWaterfallPerspective2D.Checked = false;
            menuWaterfallPerspectiveIso.Checked = false;
            switch (objWaterfall.PerspectiveModeView)
            {
                case RFEWaterfallGL.SharpGLForm.WaterfallPerspectives.Perspective1:
                    menuWaterfallContextPerspective1.Checked = true;
                    menuWaterfallPerspective1.Checked = true;
                    break;
                case RFEWaterfallGL.SharpGLForm.WaterfallPerspectives.Perspective2:
                    menuWaterfallContextPerspective2.Checked = true;
                    menuWaterfallPerspective2.Checked = true;
                    break;
                case RFEWaterfallGL.SharpGLForm.WaterfallPerspectives.PerspectiveISO:
                    menuWaterfallContextISO.Checked = true;
                    menuWaterfallPerspectiveIso.Checked = true;
                    break;
                case RFEWaterfallGL.SharpGLForm.WaterfallPerspectives.Perspective2D:
                    menuWaterfallContext2D.Checked = true;
                    menuWaterfallPerspective2D.Checked = true;
                    break;
            }

            menuWaterfallContextRealtime.Checked = false;
            menuWaterfallContextMaxHold.Checked = false;
            switch (objWaterfall.SignalType)
            {
                default:
                case RFECommunicator.RFExplorerSignalType.Realtime:
                    menuWaterfallContextRealtime.Checked = true;
                    break;
                case RFECommunicator.RFExplorerSignalType.MaxHold:
                    menuWaterfallContextMaxHold.Checked = true;
                    break;
            }

            menuWaterfallContextFloor.Checked = objWaterfall.DrawFloor;
            menuWaterfallContextTransparent.Checked = objWaterfall.Transparent;
        }

        private void OnWaterfallPerspective1_Click(object sender, EventArgs e)
        {
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
                m_objSAWaterfall.SetPerspective1();
            else
                m_objMainWaterfall.SetPerspective1();
            UpdateAllWaterfallMenuItems();
        }

        private void OnWaterfallPerspective2_Click(object sender, EventArgs e)
        {
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
                m_objSAWaterfall.SetPerspective2();
            else
                m_objMainWaterfall.SetPerspective2();
            UpdateAllWaterfallMenuItems();
        }

        private void OnWaterfallIsometric_Click(object sender, EventArgs e)
        {
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
                m_objSAWaterfall.SetPerspectiveISO();
            else
                m_objMainWaterfall.SetPerspectiveISO();
            UpdateAllWaterfallMenuItems();
        }

        private void OnWaterfall2D_Click(object sender, EventArgs e)
        {
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
                m_objSAWaterfall.SetPerspective2D();
            else
                m_objMainWaterfall.SetPerspective2D();
            UpdateAllWaterfallMenuItems();
        }
        #endregion

        #region Power Channel
        private void tabPowerChannel_Enter(object sender, EventArgs e)
        {
            if (m_graphPowerChannel == null)
            {
                m_graphPowerChannel = new ZedGraphControl();
                m_panelPowerChannel.Controls.Add(m_graphPowerChannel);
                m_graphPowerChannel.EditButtons = System.Windows.Forms.MouseButtons.Left;
                m_graphPowerChannel.IsAntiAlias = true;
                m_graphPowerChannel.IsEnableSelection = false;
                m_graphPowerChannel.Location = new System.Drawing.Point(5, 5);
                m_graphPowerChannel.Name = "zedPowerChannel";
                m_graphPowerChannel.ScrollGrace = 0D;
                m_graphPowerChannel.ScrollMaxX = 0D;
                m_graphPowerChannel.ScrollMaxY = 0D;
                m_graphPowerChannel.ScrollMaxY2 = 0D;
                m_graphPowerChannel.ScrollMinX = 0D;
                m_graphPowerChannel.ScrollMinY = 0D;
                m_graphPowerChannel.ScrollMinY2 = 0D;
                m_graphPowerChannel.Size = new System.Drawing.Size(600, 300);
                m_graphPowerChannel.TabIndex = 49;
                m_graphPowerChannel.TabStop = false;
                m_graphPowerChannel.UseExtendedPrintDialog = true;
                m_graphPowerChannel.Visible = true;
                m_graphPowerChannel.GraphPane.Title.Text = "RF Explorer Channel Power Meter";
                m_graphPowerChannel.GraphPane.Title.FontSpec.Size = 18f;
                m_graphPowerChannel.GraphPane.TitleGap = 6;
                m_graphPowerChannel.Dock = DockStyle.Fill;
                m_graphPowerChannel.ContextMenuBuilder += new ZedGraph.ZedGraphControl.ContextMenuBuilderEventHandler(this.objGraphPowerChannel_ContextMenuBuilder);
                //m_graphSpectrumAnalyzer.ZoomEvent += new ZedGraph.ZedGraphControl.ZoomEventHandler(this.zedSpectrumAnalyzer_ZoomEvent);

                m_graphPowerChannel.GraphPane.XAxis.IsVisible = false;
                m_graphPowerChannel.GraphPane.Y2Axis.IsVisible = false;
                m_graphPowerChannel.GraphPane.YAxis.IsVisible = false;
                m_graphPowerChannel.GraphPane.Border.IsVisible = false;

                //Define needles; can add more than one
                m_PowerChannelNeedle = new GasGaugeNeedle("Realtime", -30.0f, Color.Blue);
                m_PowerChannelNeedle.NeedleWidth = 10f;
                m_PowerChannelNeedle.NeedleColor = Color.Blue;
                m_PowerChannelNeedle.Label.IsVisible = false;
                m_PowerChannelNeedle.LineCap = LineCap.ArrowAnchor;
                m_graphPowerChannel.GraphPane.CurveList.Add(m_PowerChannelNeedle);

                //Define all regions
                m_PowerChannelRegion_Low = new GasGaugeRegion("Low", -120.0f, -90.0f, Color.Blue);
                m_PowerChannelRegion_Low.HasLabel = true;
                m_PowerChannelRegion_Medium = new GasGaugeRegion("Medium", -90.0f, -30.0f, Color.LightBlue);
                m_PowerChannelRegion_High = new GasGaugeRegion("High", -30.0f, 0.0f, Color.Red);
                m_PowerChannelRegion_High.HasLabel = true;
                m_PowerChannelRegion_Low.Label.IsVisible = false;
                m_PowerChannelRegion_Medium.Label.IsVisible = false;
                m_PowerChannelRegion_High.Label.IsVisible = false;

                //not working as intended
                //m_PowerChannelRegion_Low.RegionColorStart = Color.LightGreen;
                //m_PowerChannelRegion_Low.RegionColorEnd = Color.LightBlue;
                //m_PowerChannelRegion_Medium.RegionColorStart = Color.LightBlue;
                //m_PowerChannelRegion_Medium.RegionColorEnd = Color.Blue;
                //m_PowerChannelRegion_High.RegionColorStart = Color.Blue;
                //m_PowerChannelRegion_High.RegionColorEnd = Color.Red;

                m_PowerChannelText = new TextObj("No data available", 0.5, 0.25, CoordType.PaneFraction);
                m_PowerChannelText.IsClippedToChartRect = false;
                m_PowerChannelText.FontSpec.FontColor = Color.DarkBlue;
                m_PowerChannelText.Location.AlignH = AlignH.Center;
                m_PowerChannelText.Location.AlignV = AlignV.Center;
                m_PowerChannelText.FontSpec.IsBold = false;
                m_PowerChannelText.FontSpec.Size = 16f;
                m_PowerChannelText.FontSpec.Border.IsVisible = false;
                m_PowerChannelText.FontSpec.Fill.IsVisible = false;
                m_PowerChannelText.FontSpec.StringAlignment = StringAlignment.Center;
                m_PowerChannelText.FontSpec.Family = "Arial";
                m_graphPowerChannel.GraphPane.GraphObjList.Add(m_PowerChannelText);

                // Add the curves
                m_graphPowerChannel.GraphPane.CurveList.Add(m_PowerChannelRegion_Low);
                m_graphPowerChannel.GraphPane.CurveList.Add(m_PowerChannelRegion_Medium);
                m_graphPowerChannel.GraphPane.CurveList.Add(m_PowerChannelRegion_High);
                m_graphPowerChannel.GraphPane.Angle = 150;
                m_graphPowerChannel.GraphPane.GasGaugeRegionWidth = 25;
                m_graphPowerChannel.GraphPane.GasGaugeBorder = false;
                m_graphPowerChannel.GraphPane.ShowGasGaugeValueLabel = false; //needs better code to paint value + unit
                m_graphPowerChannel.GraphPane.HasLabel = false; //needs better code to paint values
                m_graphPowerChannel.GraphPane.Border.IsVisible = false;
                m_graphPowerChannel.GraphPane.Chart.Border.IsVisible = false;
                m_graphPowerChannel.GraphPane.Clockwise = true;
                m_graphPowerChannel.AxisChange();
            }

            DisplayGroups();
            m_tabPowerChannel.Invalidate();
        }

        private void objGraphPowerChannel_ContextMenuBuilder(ZedGraphControl sender, ContextMenuStrip menuStrip, Point mousePt, ZedGraphControl.ContextMenuObjectState objState)
        {
            menuStrip.Items["save_as"].Click += new System.EventHandler(OnSaveImage_Click);
            menuStrip.Items["copy"].Click += new System.EventHandler(OnClipboard_Click);
            menuStrip.Items["show_val"].Visible = false;
            menuStrip.Items["unzoom"].Visible = false;
            menuStrip.Items["undo_all"].Visible = false;
        }

        #endregion

        #region Remote screen

        private void tabRemoteScreen_Enter(object sender, EventArgs e)
        {
            UpdateButtonStatus();
            DisplayGroups();
            tabRemoteScreen_UpdateZoomValues();
            m_tabRemoteScreen.Invalidate();
        }

        private void tabRemoteScreen_UpdateZoomValues()
        {
            int nHeaderSize = 0;
            if (m_ToolGroup_RemoteScreen.DumpHeaderEnabled)
            {
                nHeaderSize = 20;
            }

            int nNewZoom = m_ToolGroup_RemoteScreen.ScreenZoom;
            int nLastGoodZoom = nNewZoom;

            do
            {
                nLastGoodZoom = nNewZoom;
                controlRemoteScreen.Size = new Size((int)(1.0 + m_fSizeX * (float)(nNewZoom)), (int)(1.0 + (m_fSizeY + nHeaderSize) * (float)(nNewZoom)));
                nNewZoom--;
            }
            while (((controlRemoteScreen.Size.Width > m_panelRemoteScreen.Size.Width) ||
                    (controlRemoteScreen.Size.Height > m_panelRemoteScreen.Size.Height))
                  && (nNewZoom > 1));

            if ((nLastGoodZoom > 0) && (nLastGoodZoom != m_ToolGroup_RemoteScreen.ScreenZoom))
            {
                m_ToolGroup_RemoteScreen.ScreenZoom = nLastGoodZoom;
            }
            else
            {

                controlRemoteScreen.UpdateZoom(m_ToolGroup_RemoteScreen.ScreenZoom);

                m_ToolGroup_RemoteScreen.SetBitmapSizeLabel();

                RelocateRemoteControl();
                controlRemoteScreen.Invalidate();
            }
            m_panelRemoteScreen.Refresh();
        }

        private void OnRemoteScreen_ZoomChanged(object sender, EventArgs e)
        {
            tabRemoteScreen_UpdateZoomValues();
            m_tabRemoteScreen.Invalidate();
        }

        private void OnRemoteScreen_EnabledChanged(object sender, EventArgs e)
        {
            UpdateButtonStatus();
        }

        private void OnRemoteScreen_HeaderChanged(object sender, EventArgs e)
        {
            tabRemoteScreen_UpdateZoomValues();
        }

        private void SavePNG(string sFilename)
        {
            //if no file path was explicit, add the default folder
            if (!String.IsNullOrEmpty(sFilename) && (sFilename.IndexOf("\\") < 0))
            {
                sFilename = m_sDefaultUserFolder + "\\" + sFilename;
                sFilename = sFilename.Replace("\\\\", "\\");
            }

            Rectangle rectArea;
            Control controlArea;

            if (m_MainTab.SelectedTab == m_tabRemoteScreen)
            {
                rectArea = controlRemoteScreen.ClientRectangle;
                controlArea = controlRemoteScreen;
            }
            else if (m_MainTab.SelectedTab == m_tabWaterfall)
            {
                rectArea = m_objPanelMainWaterfall.ClientRectangle;
                controlArea = m_objPanelMainWaterfall;
            }
            else if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
            {
                rectArea = new Rectangle(0, 0,
                    m_tabSpectrumAnalyzer.ClientRectangle.Width - (m_tabSpectrumAnalyzer.ClientRectangle.Right - btnAutoscale.Left),
                    m_tabSpectrumAnalyzer.ClientRectangle.Height - m_MainStatusBar.Height - 2);
                controlArea = m_tabSpectrumAnalyzer;
            }
            else if (m_MainTab.SelectedTab == m_tabRFGen)
            {
                rectArea = new Rectangle(0, 0, m_tabRFGen.ClientRectangle.Width,
                    m_tabRFGen.ClientRectangle.Height - m_MainStatusBar.Height - 2);
                controlArea = m_tabRFGen;
            }
            else if (m_MainTab.SelectedTab == m_tabPowerChannel)
            {
                rectArea = m_graphPowerChannel.ClientRectangle;
                controlArea = m_graphPowerChannel;
            }
            else
            {
                MessageBox.Show("Not supported on this screen.");
                return;
            }

            using (Bitmap objAppBmp = new Bitmap(rectArea.Width, rectArea.Height))
            {
                Point ptStart = new Point(rectArea.Left, rectArea.Top);
                Point ptScreen = controlArea.PointToScreen(ptStart);
                Graphics gGraphics = Graphics.FromImage(objAppBmp);
                gGraphics.CopyFromScreen(ptScreen, new Point(0, 0), rectArea.Size);

                if (String.IsNullOrEmpty(sFilename))
                {
                    if (m_ClipboardBitmap == null)
                    {
                        m_ClipboardBitmap = new Bitmap(rectArea.Width, rectArea.Height);
                        Graphics.FromImage(m_ClipboardBitmap).DrawImage(objAppBmp, rectArea);
                        ReportLog("Image sent to clipboard: " + rectArea.Width + "x" + rectArea.Height + " pixels", false);
                    }
                    else
                    {
                        ReportLog("Unable to capture clipboard: the resource was still in use", false);
                    }
                }
                else
                {
                    using (Bitmap objImage = new Bitmap(rectArea.Width, rectArea.Height))
                    {
                        Graphics.FromImage(objImage).DrawImage(objAppBmp, rectArea);

                        objImage.Save(sFilename, ImageFormat.Png);
                    }
                }
            }
        }

        private void OnSaveImage_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog MySaveFileDialog = new SaveFileDialog())
                {
                    MySaveFileDialog.Filter = _PNG_File_Selector;
                    MySaveFileDialog.FilterIndex = 1;
                    MySaveFileDialog.RestoreDirectory = false;
                    MySaveFileDialog.InitialDirectory = m_sDefaultUserFolder;

                    if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
                    {
                        MySaveFileDialog.FileName = GetNewFilename(RFExplorerFileType.AnalyzerScreenshotFile);
                    }
                    else if (m_MainTab.SelectedTab == m_tabRFGen)
                    {
                        MySaveFileDialog.FileName = GetNewFilename(RFExplorerFileType.SNATrackingScreenshotFile);
                    }
                    else if (m_MainTab.SelectedTab == m_tabWaterfall)
                    {
                        MySaveFileDialog.FileName = GetNewFilename(RFExplorerFileType.WaterfallScreenshotFile);
                    }
                    else if (m_MainTab.SelectedTab == m_tabRemoteScreen)
                    {
                        MySaveFileDialog.FileName = GetNewFilename(RFExplorerFileType.RemoteScreenFile);
                    }
                    else if (m_MainTab.SelectedTab == m_tabPowerChannel)
                    {
                        MySaveFileDialog.FileName = GetNewFilename(RFExplorerFileType.PowerChannelScreenshotFile);
                    }

                    if (MySaveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        Cursor.Current = Cursors.WaitCursor;
                        Thread.Sleep(2000); //fix for T0001
                        SavePNG(MySaveFileDialog.FileName);
                        m_sDefaultUserFolder = Path.GetDirectoryName(MySaveFileDialog.FileName);
                        edDefaultFilePath.Text = m_sDefaultUserFolder;
                        Cursor.Current = Cursors.Default;
                    }
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
        }

        private void SaveFileRFS(string sFilename)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                //if no file path was explicited, add the default folder
                if (sFilename.IndexOf("\\") < 0)
                {
                    sFilename = m_sDefaultUserFolder + "\\" + sFilename;
                    sFilename = sFilename.Replace("\\\\", "\\");
                }

                if (controlRemoteScreen.RFExplorer.ScreenData.SaveFile(sFilename))  
                {
                    ReportLog("File " + sFilename + " saved with total of " + controlRemoteScreen.RFExplorer.ScreenData.Count + " screen shots.", false);
                }
                else
                {
                    MessageBox.Show("Error saving file");
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
            Cursor.Current = Cursors.Default;
        }

        private void LoadFileRFS(string sFilename)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                if (controlRemoteScreen.RFExplorer.ScreenData.LoadFile(sFilename))
                {
                    m_ToolGroup_RemoteScreen.UpdateNumericControls();
                    UpdateButtonStatus();

                    ReportLog("File " + sFilename + " loaded with total of " + controlRemoteScreen.RFExplorer.ScreenData.Count + " screen shots.", false);
                }
                else
                {
                    MessageBox.Show("Wrong or unknown file format");
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
            Cursor.Current = Cursors.Default;
        }

        private void OnSaveRFS_Click(object sender, EventArgs e)
        {
            if (controlRemoteScreen.RFExplorer.ScreenData.Count > 0)
            {
                try
                {
                    using (SaveFileDialog MySaveFileDialog = new SaveFileDialog())
                    {
                        MySaveFileDialog.Filter = _RFS_File_Selector;
                        MySaveFileDialog.FilterIndex = 1;
                        MySaveFileDialog.RestoreDirectory = false;
                        MySaveFileDialog.InitialDirectory = m_sDefaultUserFolder;

                        GetNewFilename(RFExplorerFileType.RemoteScreenRFSFile);
                        MySaveFileDialog.FileName = m_sFilenameRFS;

                        if (MySaveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            SaveFileRFS(MySaveFileDialog.FileName);
                            m_sDefaultUserFolder = Path.GetDirectoryName(MySaveFileDialog.FileName);
                            edDefaultFilePath.Text = m_sDefaultUserFolder;
                        }
                    }
                }
                catch (Exception obEx) { MessageBox.Show(obEx.Message); }
            }
        }

        private void OnLoadRFS_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog MyOpenFileDialog = new OpenFileDialog())
            {
                MyOpenFileDialog.Filter = _RFS_File_Selector;
                MyOpenFileDialog.FilterIndex = 1;
                MyOpenFileDialog.RestoreDirectory = false;
                MyOpenFileDialog.InitialDirectory = m_sDefaultUserFolder;

                if (MyOpenFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadFileRFS(MyOpenFileDialog.FileName);
                    m_sDefaultUserFolder = Path.GetDirectoryName(MyOpenFileDialog.FileName);
                    edDefaultFilePath.Text = m_sDefaultUserFolder;
                }
            }
        }

        private void controlRemoteScreen_Load(object sender, EventArgs e)
        {
            
        }

        private void RelocateRemoteControl()
        {
            controlRemoteScreen.SuspendLayout();
            controlRemoteScreen.Top = (m_panelRemoteScreen.Height - controlRemoteScreen.Height) / 2;
            controlRemoteScreen.Left = (m_panelRemoteScreen.Width - controlRemoteScreen.Width) / 2;
            m_MainStatusBar.Parent = m_tableLayoutControlArea.Parent;
            controlRemoteScreen.ResumeLayout();
        }
        #endregion

        #region Report Window

        private void tabReport_Enter(object sender, EventArgs e)
        {
            DisplayGroups();
        }

        private void ReportLog(string sLine, bool bDetailed)
        {
            if (m_ReportTextBox.IsDisposed || m_ReportTextBox.Disposing)
                return;

            DefineCommonFiles();

            if (String.IsNullOrEmpty(m_sReportFilePath))
            {
                m_sReportFilePath = m_sAppDataFolder + "\\RFExplorerClient_report_" + DateTime.Now.ToString("yyyyMMdd") + ".log";
                m_sReportFilePath = m_sReportFilePath.Replace("\\\\", "\\");

                labelReportFile.Text = "Report file: " + m_sReportFilePath;

                m_ReportTextBox.AppendText("Welcome to RFExplorer Client - report being saved to: " + Environment.NewLine + m_sReportFilePath + Environment.NewLine);
            }
            else
                sLine = Environment.NewLine + sLine;

            if (m_ToolGroup_Commands.DebugTraces || !bDetailed)
            {
                m_ReportTextBox.AppendText(sLine);
            }

            using (StreamWriter sr = new StreamWriter(m_sReportFilePath, true))
            {
                if (m_bFirstText)
                {
                    sr.WriteLine(Environment.NewLine + Environment.NewLine +
                        "===========================================");
                    sr.WriteLine(
                        "RFExplorer client session " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
                    sr.WriteLine(
                        "===========================================" + Environment.NewLine);

                    sr.WriteLine("OS:         " + Environment.OSVersion.ToString());
                    sr.WriteLine("Runtime:    " + Environment.Version.ToString());
                    sr.WriteLine("Assembly:   " + Assembly.GetExecutingAssembly().ToString());
                    sr.WriteLine("File:       " + Assembly.GetExecutingAssembly().Location);
                    sr.WriteLine("");
                }
                sr.Write(sLine);
            }

            m_bFirstText = false;
        }

        /// <summary>
        /// Sends a custom command to analyzer or generator
        /// </summary>
        void OnCustomCommandProperties(object sender, EventArgs e)
        {
            string sCmd = m_ToolGroup_Commands.CustomCommandText;
            Properties.Settings.Default.CustomCommandList += ";" + sCmd.Replace(';', '-');
        }

        #endregion

        #region Configuration

        private void tabConfiguration_Enter(object sender, EventArgs e)
        {
            DisplayGroups();
        }
        private void OnOpenLog_Click(object sender, EventArgs e)
        {
            Process.Start(m_sReportFilePath);
        }

        private void edDefaultFilePath_Leave(object sender, EventArgs e)
        {
            m_sDefaultUserFolder = edDefaultFilePath.Text;
            SaveProperties();
        }

        private void WorkaroundBug_firmware_v1_11_switching_module()
        {
            if (menuAutoLCDOff.Checked == false)
            {
                Thread.Sleep(500);
                m_objRFEAnalyzer.SendCommand_ScreenOFF();
                Thread.Sleep(500);
                m_objRFEAnalyzer.SendCommand_ScreenON();
            }
        }

        private bool m_bInternetFWUpgradeOpenOnce = false; //used to open internet firmware upgrade page only once per session
        private bool CheckRequiredVersionForFeature(double fVersionRequired)
        {
            if (!m_objRFEAnalyzer.IsFirmwareSameOrNewer(fVersionRequired))
            {
                MessageBox.Show("This function can only be used with Firmware v" + fVersionRequired + " or later\nPlease upgrade your RF Explorer!", "Old firmware detected");
                if (!m_bInternetFWUpgradeOpenOnce)
                {
                    Process.Start("www.rf-explorer.com/upgrade");
                }
                m_bInternetFWUpgradeOpenOnce = true;
                ReportLog("Function required with firmware v" + fVersionRequired + " but found older " + m_objRFEAnalyzer.RFExplorerFirmwareDetected, true);
                return false;
            }

            return true;
        }

        private void OnEnableMainboard_Click(object sender, EventArgs e)
        {
            if (!CheckRequiredVersionForFeature(1.11))
                return;

            Cursor.Current = Cursors.WaitCursor;
            if (menuEnableMainboard.Checked == false)
            {
                ResetSettingsTitle();
                m_objRFEAnalyzer.SendCommand_EnableMainboard();
                WorkaroundBug_firmware_v1_11_switching_module();
                m_objRFEAnalyzer.ResetTrackingNormalizedData();
            }
            menuDevice_DropDownOpening(sender, e);
            Application.DoEvents();
            Thread.Sleep(1000); //this sleep will guarantee the user does not switch over and over very quick creating a stability problem
            Cursor.Current = Cursors.Default;
        }

        private void OnEnableExpansionBoard_Click(object sender, EventArgs e)
        {
            if (!CheckRequiredVersionForFeature(1.11))
                return;

            Cursor.Current = Cursors.WaitCursor;
            if (menuEnableExpansionBoard.Checked == false)
            {
                ResetSettingsTitle();
                m_objRFEAnalyzer.SendCommand_EnableExpansion();
                WorkaroundBug_firmware_v1_11_switching_module();
                m_objRFEAnalyzer.ResetTrackingNormalizedData();
            }
            menuDevice_DropDownOpening(sender, e);
            Application.DoEvents();
            Thread.Sleep(1000); //this sleep will guarantee the user does not switch over and over very quick creating a stability problem
            Cursor.Current = Cursors.Default;
        }

        private void menuDevice_DropDownOpening(object sender, EventArgs e)
        {
            menuEnableMainboard.Enabled = m_objRFEAnalyzer.PortConnected;
            menuRemoteMaxHold.Enabled = m_objRFEAnalyzer.PortConnected;
            menuRefreshRemoteMaxHold.Enabled = m_objRFEAnalyzer.PortConnected && menuRemoteMaxHold.Checked;
            menuEnableMainboard.Checked = (m_objRFEAnalyzer.ExpansionBoardActive == false);
            menuEnableExpansionBoard.Enabled = m_objRFEAnalyzer.PortConnected && (m_objRFEAnalyzer.ExpansionBoardModel != RFECommunicator.eModel.MODEL_NONE);
            if (menuEnableExpansionBoard.Enabled)
            {
                menuEnableExpansionBoard.Checked = m_objRFEAnalyzer.ExpansionBoardActive;
            }
        }
        private void menuRemoteMaxHold_Click(object sender, EventArgs e)
        {
            m_objRFEAnalyzer.UseMaxHold = menuRemoteMaxHold.Checked;
        }

        private void menuRefreshRemoteMaxHold_Click(object sender, EventArgs e)
        {
        }

        private void OnOnlineHelp_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.rf-explorer.com/windows");
        }

        private void OnFirmware_Click(object sender, EventArgs e)
        {
            Process.Start("http://j3.rf-explorer.com/download/sw/fw/FirmwareReleaseNotes.pdf");
        }

        private void OnDeviceManual_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.rf-explorer.com/manual");
        }

        private void OnWindowsReleaseNotes_Click(object sender, EventArgs e)
        {
            Process.Start("http://j3.rf-explorer.com/download/sw/win/WindowsClientReleaseNotes.pdf");
        }
        #endregion  //configuration

        #region Debug_only
        private void CreateWaterfallTestData()
        {
            m_WaterfallSweepMaxHold.CleanAll();

            m_objRFEAnalyzer.AmplitudeBottomDBM = -100;
            m_objRFEAnalyzer.AmplitudeTopDBM = -10;

            UInt16 nTotalSteps = 100;

            for (UInt16 nMaxHoldInd = 0; nMaxHoldInd < nTotalSteps; nMaxHoldInd++)
            {
                RFESweepData objNewSweep = new RFESweepData(100.0f, 10.0f / 112, 112);
                m_WaterfallSweepMaxHold.Add(objNewSweep);

                for (UInt16 nSweepDataInd = 0; nSweepDataInd < objNewSweep.TotalSteps; nSweepDataInd++)
                {
                    float fAmplitude = -100.0f;
                    //a mark on the 6th position
                    if (nSweepDataInd == 5)
                    {
                        fAmplitude = -30.0f;
                    }
                    //every of the 10 divisions (112/10)
                    if ((nSweepDataInd % 12) == 0)
                    {
                        fAmplitude = -40.0f;
                    }
                    //A 15 steps band for the first half of the sweep time
                    if (nMaxHoldInd < 50)
                    {
                        if (nSweepDataInd > 35 && nSweepDataInd < 50)
                        {
                            fAmplitude = -50.0f;
                        }
                    }
                    //every 5 sweeps interval
                    if ((nMaxHoldInd % 10) < 5)
                    {
                        if (nSweepDataInd == 65)
                        {
                            fAmplitude = -30.0f;
                        }
                    }
                    objNewSweep.SetAmplitudeDBM(nSweepDataInd, fAmplitude);
                }
                Thread.Sleep(10); //to force some time difference between samples
            }
            UpdateWaterfall();
        }

        private void OnDebug_datatest_1_Click(object sender, EventArgs e)
        {
            CreateWaterfallTestData();
        }

        private void DumpAllReceivedBytes()
        {
            string sString = m_objRFEAnalyzer.DebugAllReceivedBytes;
            m_objRFEAnalyzer.CleanReceivedBytes();
            if (String.IsNullOrEmpty(sString))
                return;

            string sText = "";
            char cPrevChar = ' ';
            foreach (char cChar in sString)
            {
                byte nChar = Convert.ToByte(cChar);
                if ((nChar < 0x20) || (cChar > 0x7D))
                {
                    sText += "[0x" + nChar.ToString("X2") + "]";
                }
                else
                {
                    sText += cChar;
                }
                if ((cPrevChar == '\r') && (cChar == '\n'))
                {
                    sText += Environment.NewLine;
                }

                cPrevChar = cChar;
            }
            ReportLog(sText, false);
        }
        #endregion

        #region Markers
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void CreateMarkerMenu()
        {
            m_arrMarkersEnabledMenu = new ToolStripMenuItem[MarkerCollection.MAX_MARKERS];
            m_arrMarkersFrequencyMenu = new ToolStripTextBox[MarkerCollection.MAX_MARKERS];

            for (int nInd = 0; nInd < MarkerCollection.MAX_MARKERS; nInd++)
            {
                ToolStripMenuItem objMarkerMenu = new ToolStripMenuItem((nInd + 1).ToString());
                objMarkerMenu.Name = "menuMarkerEnabled_ID" + (nInd + 1).ToString();
                objMarkerMenu.Size = new System.Drawing.Size(152, 22);
                menuMarkers.DropDownItems.Add(objMarkerMenu);
                ToolStripMenuItem objMarkerVisible = new ToolStripMenuItem("&Enable Marker");
                objMarkerVisible.Name = objMarkerMenu.Name + "_Enable";
                objMarkerVisible.ToolTipText = "Enable or Disable this marker on screen";
                objMarkerVisible.CheckOnClick = true;
                objMarkerVisible.Click += new System.EventHandler(OnMarkerVisible_Click);
                objMarkerMenu.DropDownItems.Add(objMarkerVisible);
                m_arrMarkersEnabledMenu[nInd] = objMarkerVisible;
                if (nInd != 0)
                {
                    ToolStripTextBox objFrequency = new ToolStripTextBox();
                    objFrequency.MaxLength = 12;
                    objFrequency.Name = "menuMarkerFrequency_ID" + (nInd + 1).ToString();
                    objFrequency.BorderStyle = BorderStyle.Fixed3D;
                    objFrequency.ToolTipText = "Specify frequency in MHZ for this marker (only numbers required, the MHZ is filled in automatically)";
                    objFrequency.LostFocus += new System.EventHandler(menuMarkerFrequency_Change);
                    //objFrequency.TextChanged += new System.EventHandler(menuMarkerFrequency_Change);
                    objMarkerMenu.DropDownItems.Add(objFrequency);
                    m_arrMarkersFrequencyMenu[nInd] = objFrequency;
                }
                else
                {
                    objMarkerMenu.DropDownOpening += new System.EventHandler(OnMarkerTrackPeak_DropDownOpening);

                    //Create specific items of marker 0
                    for (int nInd2 = 0; nInd2 < (int)RFECommunicator.RFExplorerSignalType.TOTAL_ITEMS; nInd2++)
                    {
                        ToolStripMenuItem objMarkerPeak = new ToolStripMenuItem("Track " + ((RFECommunicator.RFExplorerSignalType)nInd2).ToString() + " peak");
                        objMarkerPeak.Name = "menuTrackPeak_" + ((RFECommunicator.RFExplorerSignalType)nInd2).ToString();
                        objMarkerPeak.ToolTipText = "Marker track peak on this trace, all other traces will follow this one";
                        objMarkerPeak.CheckOnClick = false;
                        objMarkerPeak.Tag = nInd2;
                        objMarkerPeak.Click += new System.EventHandler(OnMarkerTrackPeak_Click);
                        objMarkerMenu.DropDownItems.Add(objMarkerPeak);
                    }
                }
            }

            UpdateMenuFromMarkerCollection(m_MainTab.SelectedTab == m_tabRFGen);
        }

        private void OnMarkerTrackPeak_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem objMarkerPeak = (ToolStripMenuItem)sender;
            ToolStripMenuItem objMarker1Menu = (ToolStripMenuItem)menuMarkers.DropDownItems[0];
            ((ToolStripMenuItem)objMarker1Menu.DropDownItems[0]).Checked = true;
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
            {
                m_ToolGroup_Markers_SA.TrackSignalPeak = (RFECommunicator.RFExplorerSignalType)objMarkerPeak.Tag;
                UpdateMenuMarkerPeakTrack();
                UpdateSAMarkerControlContents();
                DisplaySpectrumAnalyzerData(); //redisplay all for markers update
                m_ToolGroup_Markers_SA.UpdateButtonStatus();
            }
            else
            {
                m_ToolGroup_Markers_SNA.TrackSignalPeak = (RFECommunicator.RFExplorerSignalType)objMarkerPeak.Tag;
                UpdateMenuMarkerPeakTrack();
                UpdateSNAMarkerControlContents();
                DisplayTrackingData();
                m_ToolGroup_Markers_SNA.UpdateButtonStatus();
            }
        }

        private void OnMarkerTrackPeak_DropDownOpening(object sender, EventArgs e)
        {
            ToolStripMenuItem objMarkerPeak = (ToolStripMenuItem)sender;
            bool bRFGen = (m_MainTab.SelectedTab == m_tabRFGen);
            UpdateMenuMarkerPeakTrack();

            //Disable all but RT and Avg in SNA
            objMarkerPeak.DropDownItems[3].Enabled = !bRFGen;
            objMarkerPeak.DropDownItems[4].Enabled = !bRFGen;
            objMarkerPeak.DropDownItems[5].Enabled = !bRFGen;
        }

        private void OnMarkerVisible_Click(object sender, EventArgs e)
        {
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
            {
                UpdateSAMarkerControlContents();
                DisplaySpectrumAnalyzerData(); //redisplay all for markers update
                m_ToolGroup_Markers_SA.UpdateButtonStatus();
            }
            else
            {
                UpdateSNAMarkerControlContents();
                DisplayTrackingData();
                m_ToolGroup_Markers_SNA.UpdateButtonStatus();
            }
        }

        private void menuMarkerFrequency_Change(object sender, EventArgs e)
        {
            try
            {
                MarkerCollection objMarkers = null;
                if (m_MainTab.SelectedTab == m_tabRFGen)
                {
                    objMarkers = m_MarkersSNA;
                }
                else
                {
                    objMarkers = m_MarkersSA;
                }

                ToolStripTextBox objFrequency = (ToolStripTextBox)sender;

                string sFreq = objFrequency.Text.Replace("MHZ", "");
                sFreq = sFreq.Replace(" ", "");
                double dFreq = Convert.ToDouble(sFreq);
                int nMarker = Convert.ToInt32(objFrequency.Name.Replace("menuMarkerFrequency_ID", ""));

                if (nMarker > 1) //defensive code
                {
                    objMarkers.SetMarkerFrequency(nMarker - 1, dFreq);
                    objMarkers.EnableMarker(nMarker - 1);

                    if (m_MainTab.SelectedTab == m_tabRFGen)
                    {
                        UpdateMenuFromMarkerCollection(true);
                        UpdateSNAMarkerControlContents();
                        DisplayTrackingData(); //redisplay all for markers update
                        m_ToolGroup_Markers_SNA.UpdateButtonStatus();
                    }
                    else
                    {
                        UpdateMenuFromMarkerCollection(false);
                        UpdateSAMarkerControlContents();
                        DisplaySpectrumAnalyzerData();
                        m_ToolGroup_Markers_SA.UpdateButtonStatus();
                    }
                }
            }
            catch (Exception obEx)
            {
                ReportLog(obEx.Message, false);
                ReportLog(obEx.ToString(), true);
            }
        }

        private void UpdateMenuFromMarkerCollection(bool bRFGen)
        {
            MarkerCollection objMarkers = null;
            if (bRFGen)
            {
                objMarkers = m_MarkersSNA;
            }
            else
            {
                objMarkers = m_MarkersSA;
            }

            for (int nInd = 0; nInd < MarkerCollection.MAX_MARKERS; nInd++)
            {
                m_arrMarkersEnabledMenu[nInd].Checked = objMarkers.IsMarkerEnabled(nInd);
                if (nInd > 0)
                {
                    m_arrMarkersFrequencyMenu[nInd].Text = objMarkers.GetMarkerFrequency(nInd).ToString("0000.000 MHZ");
                }
                else
                {
                    UpdateMenuMarkerPeakTrack();
                }
            }
        }

        private void UpdateMenuMarkerPeakTrack()
        {
            ToolStripMenuItem objMarker1Menu = (ToolStripMenuItem)menuMarkers.DropDownItems[0];

            int nPeak = (int)m_ToolGroup_Markers_SNA.TrackSignalPeak;
            if (m_MainTab.SelectedTab == m_tabSpectrumAnalyzer)
            {
                nPeak = (int)m_ToolGroup_Markers_SA.TrackSignalPeak;
            }

            for (int nInd2 = 0; nInd2 < (int)RFECommunicator.RFExplorerSignalType.TOTAL_ITEMS; nInd2++)
            {
                ToolStripMenuItem objDropDown = (ToolStripMenuItem)objMarker1Menu.DropDownItems[nInd2 + 1];
                if (objDropDown.Enabled)
                {
                    if (nInd2 == nPeak)
                    {
                        objDropDown.Checked = true;
                    }
                    else
                    {
                        objDropDown.Checked = false;
                    }
                }
            }
        }

        private void CreateMarkerConfigPanel()
        {
            m_panelSAMarkers = new Panel();
            m_panelSAMarkers.Visible = true;
            m_panelSAMarkers.AutoSize = true;
            m_panelSAMarkers.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            m_panelSAMarkers.BorderStyle = BorderStyle.FixedSingle;
            m_panelSAMarkers.Font = new System.Drawing.Font("Tahoma", 8, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            m_panelSAMarkers.ForeColor = m_ColorPanelText;
            m_panelSAMarkers.Width = 120;

            m_panelSAConfiguration = new Panel();
            m_panelSAConfiguration.Visible = true;
            m_panelSAConfiguration.AutoSize = true;
            m_panelSAConfiguration.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            m_panelSAConfiguration.BorderStyle = BorderStyle.FixedSingle;
            m_panelSAConfiguration.Font = new System.Drawing.Font("Tahoma", 8, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            m_panelSAConfiguration.ForeColor = m_ColorPanelText;

            m_panelSNAMarkers = new Panel();
            m_panelSNAMarkers.Visible = true;
            m_panelSNAMarkers.AutoSize = true;
            m_panelSNAMarkers.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            m_panelSNAMarkers.BorderStyle = BorderStyle.FixedSingle;
            m_panelSNAMarkers.Font = new System.Drawing.Font("Tahoma", 8, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            m_panelSNAMarkers.ForeColor = m_ColorPanelText;
            m_panelSNAMarkers.Width = 120;

            m_panelSNAConfiguration = new Panel();
            m_panelSNAConfiguration.Visible = true;
            m_panelSNAConfiguration.AutoSize = true;
            m_panelSNAConfiguration.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            m_panelSNAConfiguration.BorderStyle = BorderStyle.FixedSingle;
            m_panelSNAConfiguration.Font = new System.Drawing.Font("Tahoma", 8, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            m_panelSNAConfiguration.ForeColor = m_ColorPanelText;

            m_tabSpectrumAnalyzer.Controls.Add(m_panelSAMarkers);
            m_tabSpectrumAnalyzer.Controls.Add(m_panelSAConfiguration);
            m_tabRFGen.Controls.Add(m_panelSNAConfiguration);
            m_tabRFGen.Controls.Add(m_panelSNAMarkers);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void UpdateConfigControlContents(Panel objPanelConfiguration, RFECommunicator objRFE)
        {
            if ((objPanelConfiguration == null) || (objRFE == null))
                return;

            if (objPanelConfiguration.Controls.Count == 0)
            {
                //create labels and initialize
                int nPosX = 3;
                int nPosY = 5;

                System.Windows.Forms.Label objLabelFirmwareNew = new System.Windows.Forms.Label();
                objLabelFirmwareNew.Name = _Firmware;
                objLabelFirmwareNew.Location = new Point(nPosX, nPosY);
                objLabelFirmwareNew.AutoSize = true;
                int nTextSizeHeight = objLabelFirmwareNew.Font.Height;
                nPosY += nTextSizeHeight;
                objPanelConfiguration.Controls.Add(objLabelFirmwareNew);

                System.Windows.Forms.Label objLabelMainboard = new System.Windows.Forms.Label();
                objLabelMainboard.Name = _MainBoard;
                objLabelMainboard.Location = new Point(nPosX, nPosY);
                objLabelMainboard.AutoSize = true;
                nPosY += nTextSizeHeight;
                objPanelConfiguration.Controls.Add(objLabelMainboard);

                System.Windows.Forms.Label objLabelExpansion = new System.Windows.Forms.Label();
                objLabelExpansion.Name = _ExpansionBoard;
                objLabelExpansion.Location = new Point(nPosX, nPosY);
                objLabelExpansion.AutoSize = true;
                nPosY += nTextSizeHeight;
                objPanelConfiguration.Controls.Add(objLabelExpansion);

                System.Windows.Forms.Label objLabelWindowsNew = new System.Windows.Forms.Label();
                objLabelWindowsNew.Name = _Windows;
                objLabelWindowsNew.Location = new Point(nPosX, nPosY);
                objLabelWindowsNew.AutoSize = true;
                nPosY += nTextSizeHeight;
                objLabelWindowsNew.Text = "Client: " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
                objPanelConfiguration.Controls.Add(objLabelWindowsNew);

                System.Windows.Forms.Label objLabelFreq = new System.Windows.Forms.Label();
                objLabelFreq.Name = _Freq;
                objLabelFreq.Location = new Point(nPosX, nPosY);
                objLabelFreq.AutoSize = true;
                nPosY += nTextSizeHeight;
                objPanelConfiguration.Controls.Add(objLabelFreq);

                System.Windows.Forms.Label objLabelSpan = new System.Windows.Forms.Label();
                objLabelSpan.Name = _Span;
                objLabelSpan.Location = new Point(nPosX, nPosY);
                objLabelSpan.AutoSize = true;
                nPosY += nTextSizeHeight;
                objPanelConfiguration.Controls.Add(objLabelSpan);

                System.Windows.Forms.Label objLabelStep = new System.Windows.Forms.Label();
                objLabelStep.Name = _Step;
                objLabelStep.Location = new Point(nPosX, nPosY);
                objLabelStep.AutoSize = true;
                nPosY += nTextSizeHeight;
                objPanelConfiguration.Controls.Add(objLabelStep);

                System.Windows.Forms.Label objLabelRBW = new System.Windows.Forms.Label();
                objLabelRBW.Name = _RBW;
                objLabelRBW.Location = new Point(nPosX, nPosY);
                objLabelRBW.AutoSize = true;
                nPosY += nTextSizeHeight;
                objPanelConfiguration.Controls.Add(objLabelRBW);

                System.Windows.Forms.Label objLabelOffsetDBNew = new System.Windows.Forms.Label();
                objLabelOffsetDBNew.Name = _OffsetDB;
                objLabelOffsetDBNew.Location = new Point(nPosX, nPosY);
                objLabelOffsetDBNew.AutoSize = true;
                nPosY += nTextSizeHeight;
                objPanelConfiguration.Controls.Add(objLabelOffsetDBNew);

                System.Windows.Forms.Label objLabelCalibration = new System.Windows.Forms.Label();
                objLabelCalibration.Name = _Cal;
                objLabelCalibration.Location = new Point(nPosX, nPosY);
                objLabelCalibration.AutoSize = true;
                nPosY += nTextSizeHeight;
                objPanelConfiguration.Controls.Add(objLabelCalibration);

                objPanelConfiguration.Height = nPosY + nTextSizeHeight / 2;
            }

            objPanelConfiguration.BackColor = m_ColorPanelBackground;
            objPanelConfiguration.ForeColor = m_ColorPanelText;
            objPanelConfiguration.Controls[_MainBoard].ForeColor = m_ColorPanelText;
            objPanelConfiguration.Controls[_ExpansionBoard].ForeColor = m_ColorPanelText;
            if (objRFE.MainBoardModel != RFECommunicator.eModel.MODEL_NONE)
            {
                if (objRFE.ExpansionBoardActive)
                {
                    objPanelConfiguration.Controls[_MainBoard].ForeColor = m_ColorPanelText;
                    objPanelConfiguration.Controls[_ExpansionBoard].ForeColor = m_ColorPanelTextHighlight;
                }
                else
                {
                    objPanelConfiguration.Controls[_MainBoard].ForeColor = m_ColorPanelTextHighlight;
                    objPanelConfiguration.Controls[_ExpansionBoard].ForeColor = m_ColorPanelText;
                }
            }

            objPanelConfiguration.Controls[_Firmware].Text = "Firmware: " + objRFE.RFExplorerFirmwareDetected;
            objPanelConfiguration.Controls[_MainBoard].Text = "RF Left: " + RFECommunicator.GetModelTextFromEnum(objRFE.MainBoardModel);
            objPanelConfiguration.Controls[_ExpansionBoard].Text = "RF Right: " + RFECommunicator.GetModelTextFromEnum(objRFE.ExpansionBoardModel);

            if (objPanelConfiguration == m_panelSAConfiguration)
            {
                objPanelConfiguration.Controls[_Freq].Text = "Center: " + m_objRFEAnalyzer.CalculateCenterFrequencyMHZ().ToString("f3") + "MHz";
                objPanelConfiguration.Controls[_Span].Text = "Span: " + m_objRFEAnalyzer.CalculateFrequencySpanMHZ().ToString("f3") + "MHz";
                objPanelConfiguration.Controls[_OffsetDB].Text = "Offset: " + m_objRFEAnalyzer.AmplitudeOffsetDB + "dB";
                objPanelConfiguration.Controls[_Step].Text = "Step: " + (1000 * m_objRFEAnalyzer.StepFrequencyMHZ).ToString("f3") + "KHz";
                objPanelConfiguration.Controls[_RBW].Text = "RBW: " + m_objRFEAnalyzer.RBW_KHZ + "KHz";
                objPanelConfiguration.Controls[_Cal].Text = "Cal: ";
                if (menuUseAmplitudeCorrection.Checked)
                {
                    objPanelConfiguration.Controls[_Cal].Text += "No";
                }
                else
                {
                    objPanelConfiguration.Controls[_Cal].Text += "Disabled";
                }

                if (objPanelConfiguration.Width < m_panelSAMarkers.Width)
                {
                    objPanelConfiguration.MinimumSize = new Size(m_panelSAMarkers.Width, objPanelConfiguration.Height);
                }
                else
                {
                    m_panelSAMarkers.MinimumSize = new Size(objPanelConfiguration.Width, m_panelSAMarkers.Height);
                }

                if (IsWaterfallOnMainScreen())
                {
                    objPanelConfiguration.Left = m_objPanelSAWaterfall.Right + 4;
                }
                else
                {
                    objPanelConfiguration.Left = m_GraphSpectrumAnalyzer.Right + 4;
                }

                objPanelConfiguration.Top = m_GraphSpectrumAnalyzer.Top;
            }
            else
            {
                if (objPanelConfiguration.Width < m_panelSNAMarkers.Width)
                {
                    objPanelConfiguration.MinimumSize = new Size(m_panelSNAMarkers.Width, objPanelConfiguration.Height);
                }
                else
                {
                    m_panelSNAMarkers.MinimumSize = new Size(objPanelConfiguration.Width, m_panelSNAMarkers.Height);
                }
                objPanelConfiguration.Top = m_GraphTrackingGenerator.Top;
                objPanelConfiguration.Left = m_GraphTrackingGenerator.Right + 4;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void UpdateSAMarkerControlContents()
        {
            if (m_panelSAMarkers == null)
                return;

            //remove any old control setting
            foreach (Control objControl in m_panelSAMarkers.Controls)
            {
                objControl.Dispose();
            }
            m_panelSAMarkers.Controls.Clear();

            m_panelSAMarkers.Top = m_panelSAConfiguration.Bottom + 2;
            m_panelSAMarkers.Left = m_panelSAConfiguration.Left;

            int nHeight = 0;
            if (menuPlaceWaterfallAtBottom.Checked)
            {
                nHeight = m_GraphSpectrumAnalyzer.Height + m_objPanelSAWaterfall.Height - m_panelSAConfiguration.Height - 6;
            }
            else
            {
                nHeight = m_GraphSpectrumAnalyzer.Height - m_panelSAConfiguration.Height - 2;
            }
            m_panelSAMarkers.MinimumSize = new Size(m_panelSAMarkers.MinimumSize.Width, nHeight);

            int nPosX = 3;
            int nPosY = 5;
            bool bSomeMarkerEnabled = false;
            for (int nInd = 0; nInd < MarkerCollection.MAX_MARKERS; nInd++)
            {
                if (m_arrMarkersEnabledMenu[nInd].Checked)
                {
                    if (nPosY <= (m_panelSAMarkers.Height - 65)) //Tested in the worst case - 175% with lot of traces modes selected
                    {
                        bSomeMarkerEnabled = true;
                        System.Windows.Forms.Label objLabelFreq = new System.Windows.Forms.Label();
                        objLabelFreq.Name = "M" + (nInd + 1).ToString() + "Freq";
                        objLabelFreq.Location = new Point(nPosX, nPosY);
                        objLabelFreq.AutoSize = true;
                        int nTextSizeHeight = objLabelFreq.Font.Height;
                        nPosY += nTextSizeHeight;
                        m_panelSAMarkers.Controls.Add(objLabelFreq);

                        if (menuRealtimeTrace.Checked)
                        {
                            System.Windows.Forms.Label objLabel = new System.Windows.Forms.Label();
                            objLabel.Name = "M" + (nInd + 1).ToString() + "RT";
                            objLabel.Location = new Point(nPosX + 3, nPosY);
                            objLabel.AutoSize = true;
                            nPosY += nTextSizeHeight;
                            m_panelSAMarkers.Controls.Add(objLabel);
                        }
                        if (menuAveragedTrace.Checked)
                        {
                            System.Windows.Forms.Label objLabel = new System.Windows.Forms.Label();
                            objLabel.Name = "M" + (nInd + 1).ToString() + "Avg";
                            objLabel.Location = new Point(nPosX + 3, nPosY);
                            objLabel.AutoSize = true;
                            nPosY += nTextSizeHeight;
                            m_panelSAMarkers.Controls.Add(objLabel);
                        }
                        if (menuMaxTrace.Checked)
                        {
                            System.Windows.Forms.Label objLabel = new System.Windows.Forms.Label();
                            objLabel.Name = "M" + (nInd + 1).ToString() + "Max";
                            objLabel.Location = new Point(nPosX + 3, nPosY);
                            objLabel.AutoSize = true;
                            nPosY += nTextSizeHeight;
                            m_panelSAMarkers.Controls.Add(objLabel);
                        }
                        if (menuMinTrace.Checked)
                        {
                            System.Windows.Forms.Label objLabel = new System.Windows.Forms.Label();
                            objLabel.Name = "M" + (nInd + 1).ToString() + "Min";
                            objLabel.Location = new Point(nPosX + 3, nPosY);
                            objLabel.AutoSize = true;
                            nPosY += nTextSizeHeight;
                            m_panelSAMarkers.Controls.Add(objLabel);
                        }
                        if (menuMaxHoldTrace.Checked)
                        {
                            System.Windows.Forms.Label objLabel = new System.Windows.Forms.Label();
                            objLabel.Name = "M" + (nInd + 1).ToString() + "MxH";
                            objLabel.Location = new Point(nPosX + 3, nPosY);
                            objLabel.AutoSize = true;
                            nPosY += nTextSizeHeight;
                            m_panelSAMarkers.Controls.Add(objLabel);
                        }
                        nPosY += 3;
                    }
                }
            }
            if (bSomeMarkerEnabled)
            {
                UpdateSAMarkerControlValues();
            }
        }

        private void OnToolGroupReport(object sender, EventArgs e)
        {
            EventReportInfo objArg = (EventReportInfo)e;
            string sLine = objArg.Data;
            ReportLog(sLine, false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void UpdateSNAMarkerControlContents()
        {
            if (m_panelSNAMarkers == null)
                return;

            //remove any old control setting
            foreach (Control objControl in m_panelSNAMarkers.Controls)
            {
                objControl.Dispose();
            }
            m_panelSNAMarkers.Controls.Clear();

            m_panelSNAMarkers.Top = m_panelSNAConfiguration.Bottom + 2;
            m_panelSNAMarkers.Left = m_panelSNAConfiguration.Left;

            int nHeight = m_GraphTrackingGenerator.Height - m_panelSNAConfiguration.Height - 6;
            m_panelSNAMarkers.MinimumSize = new Size(m_panelSNAMarkers.MinimumSize.Width, nHeight);

            int nPosX = 3;
            int nPosY = 5;
            bool bSomeMarkerEnabled = false;
            for (int nInd = 0; nInd < MarkerCollection.MAX_MARKERS; nInd++)
            {
                if (m_arrMarkersEnabledMenu[nInd].Checked)
                {
                    bSomeMarkerEnabled = true;
                    System.Windows.Forms.Label objLabelFreq = new System.Windows.Forms.Label();
                    objLabelFreq.Name = "M" + (nInd + 1).ToString() + "Freq";
                    objLabelFreq.Location = new Point(nPosX, nPosY);
                    objLabelFreq.AutoSize = true;
                    int nTextSizeHeight = objLabelFreq.Font.Height;
                    nPosY += nTextSizeHeight;
                    m_panelSNAMarkers.Controls.Add(objLabelFreq);

                    {
                        System.Windows.Forms.Label objLabel = new System.Windows.Forms.Label();
                        objLabel.Name = "M" + (nInd + 1).ToString() + "RT";
                        objLabel.Location = new Point(nPosX + 3, nPosY);
                        objLabel.AutoSize = true;
                        nPosY += nTextSizeHeight;
                        m_panelSNAMarkers.Controls.Add(objLabel);
                    }

                    {
                        System.Windows.Forms.Label objLabel = new System.Windows.Forms.Label();
                        objLabel.Name = "M" + (nInd + 1).ToString() + "Avg";
                        objLabel.Location = new Point(nPosX + 3, nPosY);
                        objLabel.AutoSize = true;
                        nPosY += nTextSizeHeight;
                        m_panelSNAMarkers.Controls.Add(objLabel);
                    }
                    nPosY += 3;
                }
            }
            if (bSomeMarkerEnabled)
            {
                UpdateSNAMarkerControlValues();
            }
        }

        private void UpdateSNAMarkerControlValues()
        {
            m_panelSNAMarkers.BackColor = m_ColorPanelBackground;
            m_panelSNAMarkers.ForeColor = m_ColorPanelText;

            if ((m_arrMarkersEnabledMenu == null) || (m_panelSNAMarkers.Controls.Count == 0))
                return;

            for (int nInd = 0; nInd < MarkerCollection.MAX_MARKERS; nInd++)
            {
                if (m_arrMarkersEnabledMenu[nInd].Checked)
                {
                    System.Windows.Forms.Label objLabelFreq = (System.Windows.Forms.Label)m_panelSNAMarkers.Controls["M" + (nInd + 1).ToString() + "Freq"];
                    if (objLabelFreq != null)
                    {
                        objLabelFreq.Text = "M" + (nInd + 1).ToString() + ": " + m_MarkersSNA.GetMarkerFrequency(nInd).ToString("0000.000 MHz");

                        //Check if the marker is indeed inside the frequency area
                        objLabelFreq.ForeColor = m_ColorPanelTextDisabled;
                        if (m_objRFEAnalyzer.TrackingData.Count > 0)
                        {
                            if ((m_MarkersSNA.GetMarkerFrequency(nInd) <= m_objRFEAnalyzer.TrackingData.GetData(0).EndFrequencyMHZ) &&
                                (m_MarkersSNA.GetMarkerFrequency(nInd) >= m_objRFEAnalyzer.TrackingData.GetData(0).StartFrequencyMHZ))
                            {
                                objLabelFreq.ForeColor = m_ColorPanelText;
                            }
                        }
                    }
                    {
                        System.Windows.Forms.Label objLabel = (System.Windows.Forms.Label)m_panelSNAMarkers.Controls["M" + (nInd + 1).ToString() + "RT"];
                        if (objLabel != null)
                        {
                            if (nInd == 0)
                            {
                                if (m_ToolGroup_Markers_SNA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Realtime)
                                    objLabel.ForeColor = m_ColorPanelTextHighlight;
                                else
                                    objLabel.ForeColor = m_ColorPanelText;
                            }
                            bool bEnabled = (objLabelFreq.ForeColor == m_ColorPanelText) && m_PointList_Tracking_Normal.Count > 0;
                            if (bEnabled)
                            {
                                objLabel.Text = "RT: " + m_MarkersSNA.GetMarkerAmplitude(nInd, RFECommunicator.RFExplorerSignalType.Realtime).ToString("0.00") + GetCurrentAmplitudeUnitLabel();
                                if (objLabel.ForeColor == m_ColorPanelTextDisabled)
                                    objLabel.ForeColor = m_ColorPanelText;
                            }
                            else
                            {
                                objLabel.Text = "RT: invalid";
                                objLabel.ForeColor = m_ColorPanelTextDisabled;
                            }
                            objLabelFreq.Enabled &= objLabel.Enabled;
                        }
                    }
                    {
                        System.Windows.Forms.Label objLabel = (System.Windows.Forms.Label)m_panelSNAMarkers.Controls["M" + (nInd + 1).ToString() + "Avg"];
                        if (objLabel != null)
                        {
                            if (nInd == 0)
                            {
                                if (m_ToolGroup_Markers_SNA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Average)
                                    objLabel.ForeColor = m_ColorPanelTextHighlight;
                                else
                                    objLabel.ForeColor = m_ColorPanelText;
                            }
                            bool bEnabled = (objLabelFreq.ForeColor == m_ColorPanelText) && m_PointList_Tracking_Avg.Count > 0;
                            if (bEnabled)
                            {
                                objLabel.Text = "Avg: " + m_MarkersSNA.GetMarkerAmplitude(nInd, RFECommunicator.RFExplorerSignalType.Average).ToString("0.00") + GetCurrentAmplitudeUnitLabel();
                                if (objLabel.ForeColor == m_ColorPanelTextDisabled)
                                    objLabel.ForeColor = m_ColorPanelText;
                            }
                            else
                            {
                                objLabel.Text = "Avg: invalid";
                                objLabel.ForeColor = m_ColorPanelTextDisabled;
                            }
                        }
                    }
                }
            }
        }

        private void UpdateSAMarkerControlValues()
        {
            m_panelSAMarkers.BackColor = m_ColorPanelBackground;
            m_panelSAMarkers.ForeColor = m_ColorPanelText;

            if ((m_arrMarkersEnabledMenu == null) || (m_panelSAMarkers.Controls.Count == 0))
                return;

            for (int nInd = 0; nInd < MarkerCollection.MAX_MARKERS; nInd++)
            {
                if (m_arrMarkersEnabledMenu[nInd].Checked)
                {
                    System.Windows.Forms.Label objLabelFreq = (System.Windows.Forms.Label)m_panelSAMarkers.Controls["M" + (nInd + 1).ToString() + "Freq"];
                    if (objLabelFreq != null)
                    {
                        objLabelFreq.Text = "M" + (nInd + 1).ToString() + ": " + m_MarkersSA.GetMarkerFrequency(nInd).ToString("0000.000 MHz");

                        //Check if the marker is indeed inside the frequency area
                        objLabelFreq.ForeColor = m_ColorPanelTextDisabled;
                        if (m_objRFEAnalyzer.SweepData.Count > 0)
                        {
                            if (m_objRFEAnalyzer.Mode != RFECommunicator.eMode.MODE_WIFI_ANALYZER)
                            {
                                if ((m_MarkersSA.GetMarkerFrequency(nInd) <= m_objRFEAnalyzer.StopFrequencyMHZ) &&
                                (m_MarkersSA.GetMarkerFrequency(nInd) >= m_objRFEAnalyzer.StartFrequencyMHZ))
                                {
                                    objLabelFreq.ForeColor = m_ColorPanelText;
                                }
                            }
                        }
                    }

                    if (menuRealtimeTrace.Checked)
                    {
                        System.Windows.Forms.Label objLabel = (System.Windows.Forms.Label)m_panelSAMarkers.Controls["M" + (nInd + 1).ToString() + "RT"];
                        if (objLabel != null)
                        {
                            if (nInd == 0)
                            {
                                if (m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Realtime)
                                    objLabel.ForeColor = m_ColorPanelTextHighlight;
                                else
                                    objLabel.ForeColor = m_ColorPanelText;
                            }
                            bool bEnabled = (objLabelFreq.ForeColor == m_ColorPanelText) && (m_PointList_Realtime.Count > 0);
                            if (bEnabled)
                            {
                                objLabel.Text = "RT: " + m_MarkersSA.GetMarkerAmplitude(nInd, RFECommunicator.RFExplorerSignalType.Realtime).ToString(GetCurrentAmplitudeUnitFormat()) + GetCurrentAmplitudeUnitLabel();
                                if (objLabel.ForeColor == m_ColorPanelTextDisabled)
                                    objLabel.ForeColor = m_ColorPanelText;
                            }
                            else
                            {
                                objLabel.Text = "RT: invalid";
                                objLabel.ForeColor = m_ColorPanelTextDisabled;
                            }
                            objLabelFreq.Enabled &= objLabel.Enabled;
                        }
                    }
                    if (menuAveragedTrace.Checked)
                    {
                        System.Windows.Forms.Label objLabel = (System.Windows.Forms.Label)m_panelSAMarkers.Controls["M" + (nInd + 1).ToString() + "Avg"];
                        if (objLabel != null)
                        {
                            if (nInd == 0)
                            {
                                if (m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Average)
                                    objLabel.ForeColor = m_ColorPanelTextHighlight;
                                else
                                    objLabel.ForeColor = m_ColorPanelText;
                            }
                            bool bEnabled = (objLabelFreq.ForeColor == m_ColorPanelText) && m_PointList_Avg.Count > 0;
                            if (bEnabled)
                            {
                                objLabel.Text = "Avg: " + m_MarkersSA.GetMarkerAmplitude(nInd, RFECommunicator.RFExplorerSignalType.Average).ToString(GetCurrentAmplitudeUnitFormat()) + GetCurrentAmplitudeUnitLabel();
                                if (objLabel.ForeColor == m_ColorPanelTextDisabled)
                                    objLabel.ForeColor = m_ColorPanelText;
                            }
                            else
                            {
                                objLabel.Text = "Avg: invalid";
                                objLabel.ForeColor = m_ColorPanelTextDisabled;
                            }
                        }
                    }
                    if (menuMaxTrace.Checked)
                    {
                        System.Windows.Forms.Label objLabel = (System.Windows.Forms.Label)m_panelSAMarkers.Controls["M" + (nInd + 1).ToString() + "Max"];
                        if (objLabel != null)
                        {
                            if (nInd == 0)
                            {
                                if (m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.MaxPeak)
                                    objLabel.ForeColor = m_ColorPanelTextHighlight;
                                else
                                    objLabel.ForeColor = m_ColorPanelText;
                            }
                            bool bEnabled = (objLabelFreq.ForeColor == m_ColorPanelText) && m_PointList_Max.Count > 0;
                            if (bEnabled)
                            {
                                objLabel.Text = "Max: " + m_MarkersSA.GetMarkerAmplitude(nInd, RFECommunicator.RFExplorerSignalType.MaxPeak).ToString(GetCurrentAmplitudeUnitFormat()) + GetCurrentAmplitudeUnitLabel();
                                if (objLabel.ForeColor == m_ColorPanelTextDisabled)
                                    objLabel.ForeColor = m_ColorPanelText;
                            }
                            else
                            {
                                objLabel.Text = "Max: invalid";
                                objLabel.ForeColor = m_ColorPanelTextDisabled;
                            }
                        }
                    }
                    if (menuMinTrace.Checked)
                    {
                        System.Windows.Forms.Label objLabel = (System.Windows.Forms.Label)m_panelSAMarkers.Controls["M" + (nInd + 1).ToString() + "Min"];
                        if (objLabel != null)
                        {
                            if (nInd == 0)
                            {
                                if (m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Min)
                                    objLabel.ForeColor = m_ColorPanelTextHighlight;
                                else
                                    objLabel.ForeColor = m_ColorPanelText;
                            }
                            bool bEnabled = (objLabelFreq.ForeColor == m_ColorPanelText) && m_PointList_Min.Count > 0;
                            if (bEnabled)
                            {
                                objLabel.Text = "Min: " + m_MarkersSA.GetMarkerAmplitude(nInd, RFECommunicator.RFExplorerSignalType.Min).ToString(GetCurrentAmplitudeUnitFormat()) + GetCurrentAmplitudeUnitLabel();
                                if (objLabel.ForeColor == m_ColorPanelTextDisabled)
                                    objLabel.ForeColor = m_ColorPanelText;
                            }
                            else
                            {
                                objLabel.Text = "Min: invalid";
                                objLabel.ForeColor = m_ColorPanelTextDisabled;
                            }
                        }
                    }
                    if (menuMaxHoldTrace.Checked)
                    {
                        System.Windows.Forms.Label objLabel = (System.Windows.Forms.Label)m_panelSAMarkers.Controls["M" + (nInd + 1).ToString() + "MxH"];
                        if (objLabel != null)
                        {
                            if (nInd == 0)
                            {
                                if (m_ToolGroup_Markers_SA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.MaxHold)
                                    objLabel.ForeColor = m_ColorPanelTextHighlight;
                                else
                                    objLabel.ForeColor = m_ColorPanelText;
                            }
                            bool bEnabled = (objLabelFreq.ForeColor == m_ColorPanelText) && m_PointList_MaxHold.Count > 0;
                            if (bEnabled)
                            {
                                objLabel.Text = "MxH: " + m_MarkersSA.GetMarkerAmplitude(nInd, RFECommunicator.RFExplorerSignalType.MaxHold).ToString(GetCurrentAmplitudeUnitFormat()) + GetCurrentAmplitudeUnitLabel();
                                if (objLabel.ForeColor == m_ColorPanelTextDisabled)
                                    objLabel.ForeColor = m_ColorPanelText;
                            }
                            else
                            {
                                objLabel.ForeColor = m_ColorPanelTextDisabled;
                                objLabel.Text = "MxH: invalid";
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Amplitude Calibration

        private void LoadFileRFA(string sFilename, bool bInteractiveWarning)
        {
            menuUseAmplitudeCorrection.Checked = false;
            if (m_objRFEAnalyzer.LoadFileRFA(sFilename))
            {
                m_LimitLineAnalyzer_Overload.Clear();
                m_LimitLineAnalyzer_Overload.AmplitudeUnits = GetCurrentAmplitudeEnum();
                menuUseAmplitudeCorrection.Enabled = true;
                menuUseAmplitudeCorrection.Checked = m_objRFEAnalyzer.PortConnected;
                DisplaySpectrumAnalyzerData();
            }
            else
            {
                string sWarning = "ERROR: Incorrect format or data - Unable to load file " + sFilename;
                ReportLog(sWarning, false);
                if (bInteractiveWarning)
                    MessageBox.Show(sWarning);
            }
        }

        private void OnLoadAmplitudeFile_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog MyOpenFileDialog = new OpenFileDialog())
                {
                    MyOpenFileDialog.Filter = _RFA_File_Selector;
                    MyOpenFileDialog.FilterIndex = 1;
                    MyOpenFileDialog.RestoreDirectory = false;
                    MyOpenFileDialog.InitialDirectory = m_sDefaultUserFolder;

                    if (MyOpenFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        LoadFileRFA(MyOpenFileDialog.FileName, true);
                        m_sDefaultUserFolder = Path.GetDirectoryName(MyOpenFileDialog.FileName);
                        edDefaultFilePath.Text = m_sDefaultUserFolder;
                    }
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
        }

        private void OnUseAmplitudeCorrection_Click(object sender, EventArgs e)
        {
            menuUseAmplitudeCorrection.Checked = !menuUseAmplitudeCorrection.Checked;
            UpdateConfigControlContents(m_panelSAConfiguration, m_objRFEAnalyzer);
            DisplaySpectrumAnalyzerData();
        }

        private string ModelAmplitudeFileName()
        {
            string sCalText = "";
            if ((m_objRFEAnalyzer != null) && m_objRFEAnalyzer.IsAnalyzerEmbeddedCal() && (m_objRFEAnalyzer.ActiveModel != RFECommunicator.eModel.MODEL_NONE))
            {
                sCalText = "_CAL";
            }
            return m_sAppDataFolder + "\\" + RFECommunicator.GetModelTextFromEnum(m_objRFEAnalyzer.ActiveModel) + sCalText + ".RFA";
        }

        private void AutoLoadAmplitudeDataFile()
        {
            if (menuAutoLoadAmplitudeData.Checked)
            {
                if (File.Exists(ModelAmplitudeFileName()))
                {
                    LoadFileRFA(ModelAmplitudeFileName(), false);
                    UpdateConfigControlContents(m_panelSAConfiguration, m_objRFEAnalyzer);
                    ReportLog("Automatic amplitude calibration data loaded: " + ModelAmplitudeFileName(), false);
                }
                else
                {
                    ReportLog("NO CALIBRATION DATA LOAD: Automatic amplitude calibration data not found: " + ModelAmplitudeFileName(), false);
                }
            }
        }

        private void menuAutoLoadAmplitudeData_Click(object sender, EventArgs e)
        {
            AutoLoadAmplitudeDataFile();
        }
        #endregion

        #region SNA Tracking
        private void menuSaveSNANormalization_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog MySaveFileDialog = new SaveFileDialog())
                {
                    MySaveFileDialog.Filter = _SNANORM_File_Selector;
                    MySaveFileDialog.FilterIndex = 1;
                    MySaveFileDialog.RestoreDirectory = false;
                    MySaveFileDialog.InitialDirectory = m_sDefaultUserFolder;

                    MySaveFileDialog.FileName = GetNewFilename(RFExplorerFileType.SNANormalizedDataFile); ;

                    if (MySaveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        ReportLog("Saving file " + MySaveFileDialog.FileName + " " + m_objRFEAnalyzer.TrackingNormalizedData.Dump(), false);
                        m_objRFEAnalyzer.SaveFileSNANormalization(MySaveFileDialog.FileName);
                        m_sDefaultUserFolder = Path.GetDirectoryName(MySaveFileDialog.FileName);
                        edDefaultFilePath.Text = m_sDefaultUserFolder;
                    }
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
        }

        private void menuLoadSNANormalization_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                CleanSweepData();

                using (OpenFileDialog MyOpenFileDialog = new OpenFileDialog())
                {
                    MyOpenFileDialog.Filter = _SNANORM_File_Selector;
                    MyOpenFileDialog.FilterIndex = 1;
                    MyOpenFileDialog.RestoreDirectory = false;
                    MyOpenFileDialog.InitialDirectory = m_sDefaultUserFolder;
                    if (MyOpenFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        m_objRFEAnalyzer.TrackingRFEGen = m_objRFEGenerator;

                        if (m_objRFEAnalyzer.LoadDataFile(MyOpenFileDialog.FileName))
                        {
                            m_ToolGroup_AnalyzerDataFeed.UpdateNumericControls();
                            UpdateConfigControlContents(m_panelSNAConfiguration, m_objRFEGenerator);
                            ReportLog("Normalization Data File " + MyOpenFileDialog.FileName + " loaded:" + Environment.NewLine + m_objRFEAnalyzer.TrackingNormalizedData.Dump(), false);
                            UpdateFeedMode();

                            SetupSpectrumAnalyzerAxis();
                            DisplaySpectrumAnalyzerData();
                            UpdateWaterfall();

                            m_ToolGroupRFEGenFreqSweep.UpdateRFGeneratorControlsFromObject(false);
                            m_ToolGroup_RFGenCW.UpdateRFGeneratorControlsFromObject();
                            
                            //we read it again here. The reason is the previous call UpdateRFGeneratorControlsFromObject may have reset TrackingNormalizedData to null due to changes
                            //in controls, so normalization data would have been lost to null. This recovers it and guarantees the controls are already in expected status.
                            m_objRFEAnalyzer.LoadDataFile(MyOpenFileDialog.FileName);
                            UpdateButtonStatus();
                            UpdateButtonStatus_RFGen();
                        }
                        else
                        {
                            m_objRFEAnalyzer.ResetTrackingNormalizedData();
                            MessageBox.Show("Normalization file cannot be loaded or is not valid for the connected models");
                        }
                        UpdateButtonStatus();

                        m_sDefaultUserFolder = Path.GetDirectoryName(MyOpenFileDialog.FileName);
                        edDefaultFilePath.Text = m_sDefaultUserFolder;
                    }
                }
            }
            catch (Exception obEx)
            {
                ReportLog(obEx.ToString(), false);
                MessageBox.Show(obEx.Message);
            }

            Cursor.Current = Cursors.Default;
        }

        private PointPairList CreatePointsFromSweepData(ref RFESweepData objSweep)
        {
            PointPairList objList = new PointPairList();
            for (UInt16 nInd = 0; nInd < objSweep.TotalSteps; nInd++)
            {
                objList.Add(objSweep.GetFrequencyMHZ(nInd), objSweep.GetAmplitudeDBM(nInd));
            }
            return objList;
        }

        private void m_btnCalibrate3G_Click(object sender, EventArgs e)
        {
        }

        private void m_btnCalibrate1G_Click(object sender, EventArgs e)
        {
        }

        private void m_btnCalibrate6G_Click(object sender, EventArgs e)
        {
        }

        private void btnCalibratePurge_Click(object sender, EventArgs e)
        {
        }
        #endregion
    }
}
