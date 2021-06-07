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
//=============================================================================

//#define CALLSTACK_REALTIME
//#define CALLSTACK
//#define SUPPORT_EXPERIMENTAL
//#define DEBUG

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using ZedGraph;
using System.IO.Ports;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Collections;
using Microsoft.Win32;
using System.Diagnostics;
using RFExplorerCommunicator;
using SharpGL;
using SharpGL.Enumerations;
using System.Media;
using RFEClientControls;

namespace RFExplorerClient
{
    public partial class MainForm : Form
    {
        #region RFGen
        private System.Windows.Forms.TabPage m_tabRFGen;

        private void CreateRFGenTab()
        {
            this.m_tabRFGen = new System.Windows.Forms.TabPage();
            this.m_tabRFGen.SuspendLayout();
            this.m_MainTab.Controls.Add(this.m_tabRFGen);

            // 
            // m_tabRFGen
            // 
            //this.m_tabRFGen.Controls.Add(this.m_groupControl_Commands);
            //this.m_tabRFGen.Controls.Add(this.m_ReportTextBox);
            this.m_tabRFGen.Location = new System.Drawing.Point(4, 26);
            this.m_tabRFGen.Name = "m_tabRFGen";
            this.m_tabRFGen.Padding = new System.Windows.Forms.Padding(3);
            this.m_tabRFGen.Size = new System.Drawing.Size(1084, 510);
            this.m_tabRFGen.TabIndex = 1;
            this.m_tabRFGen.Text = "Signal Generator";
            this.m_tabRFGen.UseVisualStyleBackColor = true;
            this.m_tabRFGen.Enter += new System.EventHandler(this.OnTabRFGen_Enter);

            m_GraphTrackingGenerator = new ZedGraph.ZedGraphControl();
            m_tabRFGen.Controls.Add(m_GraphTrackingGenerator);
        }

        private void OnTabRFGen_Enter(object sender, EventArgs e)
        {
            UpdateMenuFromMarkerCollection(true);
            DisplayGroups();
        }

        private void UpdateButtonStatus_RFGen()
        {
            if (m_objRFEGenerator.RFGenPowerON)
            {
                m_StatusGraphText_Tracking.FontSpec.FontColor = Color.DarkRed;
            }
            else
            {
                m_StatusGraphText_Tracking.FontSpec.FontColor = Color.DarkGray;
            }

            m_ToolGroupRFEGenFreqSweep.UpdateButtonStatus();
            m_ToolGroupRFEGenAmplSweep.UpdateButtonStatus();

            m_ToolGroup_RFEGenTracking.UpdateButtonStatus(m_objRFEGenerator.RFGenPowerON);
            m_ToolGroup_RFGenCW.UpdateButtonStatus();
        }

        private void DisplayGroups_RFGen()
        {
            bool bRFGenConnected = m_objRFEGenerator.PortConnected && m_objRFEGenerator.IsGenerator();

            UpdateConfigControlContents(m_panelSNAConfiguration, m_objRFEGenerator);
            UpdateSNAMarkerControlContents();

            m_ToolGroup_RFEGenTracking.Enabled = bRFGenConnected && m_objRFEAnalyzer.PortConnected;

            m_ToolGroupRFEGenFreqSweep.DisplayGroups();
            m_ToolGroupRFEGenAmplSweep.DisplayGroups();

            UpdateButtonStatus_RFGen();

            UpdateTrackingStatusText();
        }

        private void OnStartFreqSweep_Click(object sender, EventArgs e)
        {
            CleanSweepData();
            ClearTrackingGraph();
            HideNormalizatingMessage();

            m_ToolGroup_RFGenCW.UpdateDevicePower();

            m_ToolGroupRFEGenFreqSweep.UpdateDeviceFrequency();

            m_objRFEGenerator.SendCommand_GeneratorSweepFreq();

            DisplayTrackingData_TextProgress(true);

            DisplayGroups_RFGen();
            
            Cursor.Current = Cursors.Default;
        }

        private void OnStartAmpSweep_Click(object sender, EventArgs e)
        {
            CleanSweepData();
            ClearTrackingGraph();
            HideNormalizatingMessage();

            m_ToolGroup_RFGenCW.SetFrequency();

            m_ToolGroupRFEGenAmplSweep.UpdateDevicePower(); 

            m_objRFEGenerator.SendCommand_GeneratorSweepAmplitude();

            DisplayTrackingData_TextProgress(true);

            DisplayGroups_RFGen();

            Cursor.Current = Cursors.Default;
        }

        private void OnStopSweep_Click(object sender, EventArgs e)
        {
            HideNormalizatingMessage();
            DisplayGroups_RFGen();
            DisplayTrackingData_TextProgress(true);
        }

        private void OnRFGenFreqSweepStart_Leave(object sender, EventArgs e)
        {
            DisplayGroups_RFGen();
        }

        private void OnRFGenFreqSweepStop_Leave(object sender, EventArgs e)
        {
            DisplayGroups_RFGen();
        }

        private void OnRFGenFreqSweepSteps_Leave(object sender, EventArgs e)
        {
            DisplayGroups_RFGen();
        }

        private void m_ListSNAOptions_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                //VSWR selected so delete older data from graph as will be wrong
                if (m_MainTab.SelectedTab == m_tabRFGen)
                {
                    m_PointList_Tracking_Avg.Clear();
                    m_PointList_Tracking_Normal.Clear();

                    UpdateTrackingYAxisType(false);
                    DisplayTrackingData();
                }
                //m_GraphTrackingGenerator.Refresh();
            }
            catch (Exception obEx)
            {
                ReportLog(obEx.ToString(), true);
            }
        }  

        private void UpdateTrackingYAxisType(bool bInitialize = true)
        {
            m_ToolGroup_RFEGenTracking.InitializeSNAOptions(bInitialize);

            string sText = m_ToolGroup_RFEGenTracking.ListSNAOptions;

            m_GraphTrackingGenerator.GraphPane.YAxis.Title.Text = sText;
            m_GraphTrackingGenerator.Refresh();
        }

        private void InitializeTrackingGeneratorGraph()
        {
#if CALLSTACK
            Console.WriteLine("CALLSTACK:InitializeTrackingGeneratorGraph");
#endif

            m_GraphTrackingGenerator.IsAutoScrollRange = true;
            m_GraphTrackingGenerator.EditButtons = System.Windows.Forms.MouseButtons.Left;
            m_GraphTrackingGenerator.IsAntiAlias = true;
            m_GraphTrackingGenerator.IsEnableSelection = true;
            m_GraphTrackingGenerator.Location = new System.Drawing.Point(8, 257);
            m_GraphTrackingGenerator.Name = "zedTracking";
            m_GraphTrackingGenerator.PanModifierKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.None)));
            m_GraphTrackingGenerator.ScrollGrace = 0D;
            m_GraphTrackingGenerator.ScrollMaxX = 0D;
            m_GraphTrackingGenerator.ScrollMaxY = 0D;
            m_GraphTrackingGenerator.ScrollMaxY2 = 0D;
            m_GraphTrackingGenerator.ScrollMinX = 0D;
            m_GraphTrackingGenerator.ScrollMinY = 0D;
            m_GraphTrackingGenerator.ScrollMinY2 = 0D;
            m_GraphTrackingGenerator.Size = new System.Drawing.Size(123, 54);
            m_GraphTrackingGenerator.TabIndex = 49;
            m_GraphTrackingGenerator.TabStop = false;
            m_GraphTrackingGenerator.UseExtendedPrintDialog = true;
            m_GraphTrackingGenerator.Visible = true;
            m_GraphTrackingGenerator.ContextMenuBuilder += new ZedGraph.ZedGraphControl.ContextMenuBuilderEventHandler(this.objGraph_ContextMenuBuilder);
            //m_graphTrackingGenerator.ZoomEvent += new ZedGraph.ZedGraphControl.ZoomEventHandler(this.zedSpectrumAnalyzer_ZoomEvent);

            // Get a reference to the GraphPane instance in the ZedGraphControl
            GraphPane myPane = m_GraphTrackingGenerator.GraphPane;

            m_LimitLineGenerator_Max = new LimitLine();
            m_LimitLineGenerator_Min = new LimitLine();
            m_GraphLimitLineGenerator_Max = myPane.AddCurve("Limit Max", m_LimitLineGenerator_Max, Color.Magenta, SymbolType.Circle);
            m_GraphLimitLineGenerator_Max.Line.Width = 1;
            m_GraphLimitLineGenerator_Min = myPane.AddCurve("Limit Min", m_LimitLineGenerator_Min, Color.DarkMagenta, SymbolType.Circle);
            m_GraphLimitLineGenerator_Min.Line.Width = 1;

            m_PointList_Tracking_Normal = new PointPairList();
            m_GraphLine_Tracking_Normal = myPane.AddCurve("Realtime", m_PointList_Tracking_Normal, Color.Blue, SymbolType.None);
            m_GraphLine_Tracking_Normal.Line.Width = 1;
            m_GraphLine_Tracking_Normal.Line.SmoothTension = 0.2F;

            m_PointList_Tracking_Avg = new PointPairList();
            m_GraphLine_Tracking_Avg = myPane.AddCurve("Average", m_PointList_Tracking_Avg, Color.DarkRed, SymbolType.None);
            m_GraphLine_Tracking_Avg.Line.Width = 4;
            m_GraphLine_Tracking_Avg.Line.SmoothTension = 0.3F;

            foreach (CurveItem objCurve in myPane.CurveList)
            {
                objCurve.IsVisible = false;
                objCurve.Label.IsVisible = false;
            }

            // Set the titles and axis labels
            //myPane.Title.FontSpec.Size = 10;
            myPane.XAxis.Title.IsVisible = true;
            myPane.XAxis.Title.Text = "Frequency (MHZ)";
            myPane.XAxis.Scale.MajorStepAuto = true;
            myPane.XAxis.Scale.MinorStepAuto = true;
            myPane.XAxis.Type = AxisType.Linear;
            myPane.Margin.Left = 20;
            myPane.Margin.Right = -5;

            m_GraphTrackingGenerator.IsShowPointValues = true;

            // Show the x axis grid
            myPane.XAxis.MajorGrid.IsVisible = true;
            myPane.XAxis.Type = AxisType.Linear;

            myPane.YAxis.Title.IsVisible = true;
            // turn off the opposite tics so the Y tics don't show up on the Y2 axis
            myPane.YAxis.MajorTic.IsOpposite = false;
            myPane.YAxis.MinorTic.IsOpposite = false;
            myPane.YAxis.MajorGrid.IsVisible = true;
            // Don't display the Y zero line
            myPane.YAxis.MajorGrid.IsZeroLine = false;
            // Align the Y axis labels so they are flush to the axis
            myPane.YAxis.Scale.Align = AlignP.Inside;
            UpdateTrackingYAxisType();

            myPane.Title.Text = _RFEGEN_TRACKING_TITLE;

            m_GraphTrackingGenerator.IsShowPointValues = true;
            m_GraphTrackingGenerator.PointValueEvent += new ZedGraphControl.PointValueHandler(GraphPointGeneratorValueHandler);

            m_StatusGraphText_Tracking = new TextObj("Signal Generator DISCONNECTED", 0.01, 0.02, CoordType.ChartFraction);
            m_StatusGraphText_Tracking.IsClippedToChartRect = true;
            m_StatusGraphText_Tracking.FontSpec.FontColor = Color.DarkGray;
            m_StatusGraphText_Tracking.Location.AlignH = AlignH.Left;
            m_StatusGraphText_Tracking.Location.AlignV = AlignV.Top;
            m_StatusGraphText_Tracking.FontSpec.IsBold = false;
            m_StatusGraphText_Tracking.FontSpec.Size = 10f;
            m_StatusGraphText_Tracking.FontSpec.Border.IsVisible = false;
            m_StatusGraphText_Tracking.FontSpec.Fill.IsVisible = false;
            m_StatusGraphText_Tracking.FontSpec.StringAlignment = StringAlignment.Near;
            m_StatusGraphText_Tracking.FontSpec.IsDropShadow = true;
            m_StatusGraphText_Tracking.FontSpec.DropShadowOffset = 0.1f;
            m_StatusGraphText_Tracking.FontSpec.DropShadowColor = Color.LightBlue;
            m_StatusGraphText_Tracking.FontSpec.Family = "Tahoma";
            myPane.GraphObjList.Add(m_StatusGraphText_Tracking);

            m_TrackingStatus = new TextObj("Tracking Normalization in Progress, \nplease wait...", 0.5, 0.5, CoordType.ChartFraction);
            m_TrackingStatus.Location.AlignH = AlignH.Center;
            m_TrackingStatus.Location.AlignV = AlignV.Center;
            m_TrackingStatus.FontSpec.Size = 20;
            m_TrackingStatus.FontSpec.FontColor = Color.DarkRed;
            m_TrackingStatus.FontSpec.IsDropShadow = true;
            m_TrackingStatus.FontSpec.DropShadowOffset = 0.05f;
            m_TrackingStatus.IsVisible = false;
            myPane.GraphObjList.Add(m_TrackingStatus);

            m_TrackingProgressText = new TextObj("Tracking step: 0/0 0%", 0.01, 0.08, CoordType.ChartFraction);
            m_TrackingProgressText.IsClippedToChartRect = true;
            m_TrackingProgressText.FontSpec.FontColor = Color.DarkBlue;
            m_TrackingProgressText.Location.AlignH = AlignH.Left;
            m_TrackingProgressText.Location.AlignV = AlignV.Top;
            m_TrackingProgressText.FontSpec.IsBold = false;
            m_TrackingProgressText.FontSpec.Size = 8f;
            m_TrackingProgressText.FontSpec.Border.IsVisible = false;
            m_TrackingProgressText.FontSpec.Fill.IsVisible = false;
            m_TrackingProgressText.FontSpec.StringAlignment = StringAlignment.Near;
            m_TrackingProgressText.FontSpec.IsDropShadow = true;
            m_TrackingProgressText.FontSpec.DropShadowColor = Color.LightBlue;
            m_TrackingProgressText.FontSpec.DropShadowOffset = 0.1f;
            m_TrackingProgressText.FontSpec.Family = "Tahoma";
            m_TrackingProgressText.IsVisible = false;
            myPane.GraphObjList.Add(m_TrackingProgressText);

            this.m_tabRFGen.ResumeLayout(false);
            this.m_tabRFGen.PerformLayout();

            m_MarkersSNA.ConnectToGraph(myPane);
        }

        private string GraphPointGeneratorValueHandler(ZedGraphControl control, GraphPane pane, CurveItem curve, int iPt)
        {
            // Get the PointPair that is under the mouse
            PointPair pt = curve[iPt];
            if (m_ToolGroup_RFEGenTracking.ListSNAOptionsIndex != 2)
                return pt.X.ToString("f3") + "MHZ\r\n" + pt.Y.ToString("f1") + " dB";
            else
                return pt.X.ToString("f3") + "MHZ\r\n" + pt.Y.ToString("f2");
        }

        private void OnGeneratorButtons_PortConnected(object sender, EventArgs e)
        {
            //nothing to do, already handled in OnRFE_PortConnected
        }

        private void OnGeneratorButtons_PortClosed(object sender, EventArgs e)
        {
            //nothing to do, already handled in OnRFE_PortClosed
        }

        UInt16 m_nNormalizationRetries = 0; //used to retry once if normalization fails
        private void OnNormalizeTrackingStartChanged(object sender, EventArgs e)
        {
            m_nNormalizationRetries = 0;
            Cursor.Current = Cursors.WaitCursor;

            CleanSweepData();
            ClearTrackingGraph();
            ShowNormalizatingMessage();

            m_objRFEAnalyzer.TrackingRFEGen = m_objRFEGenerator;
            //note: for normalization we always use the value, regardless the checkbox used for tracking
            m_objRFEAnalyzer.AutoStopSNATrackingCounter = m_ToolGroup_RFEGenTracking.Average;

            m_ToolGroupRFEGenFreqSweep.UpdateDeviceFrequency();
            m_ToolGroup_RFGenCW.UpdateDevicePower();

            m_objRFEAnalyzer.StartTrackingSequence(true);
            UpdateButtonStatus();
            UpdateButtonStatus_RFGen();

            Cursor.Current = Cursors.Default;
        }

        private void ClearTrackingGraph()
        {
            m_objRFEAnalyzer.TrackingData.CleanAll();
            m_GraphLine_Tracking_Normal.Clear();
            m_PointList_Tracking_Normal.Clear();
            m_GraphLine_Tracking_Avg.Clear();
            m_PointList_Tracking_Avg.Clear();
            m_GraphTrackingGenerator.Refresh();
        }

        
        private void OnTrackingStartChanged(object sender, EventArgs e)
        {  
            if (DialogResult.Cancel == MessageBox.Show("Connect cables to DUT to start tracking", "RF Explorer SNA Tracking", MessageBoxButtons.OKCancel))
                return;

            Cursor.Current = Cursors.WaitCursor;
            ClearTrackingGraph();
            CleanSweepData();
            HideNormalizatingMessage();

            m_objRFEAnalyzer.TrackingRFEGen = m_objRFEGenerator;

            if (m_ToolGroup_RFEGenTracking.SNAAutoStop) 
                m_objRFEAnalyzer.AutoStopSNATrackingCounter = m_ToolGroup_RFEGenTracking.Average;
            else
                m_objRFEAnalyzer.AutoStopSNATrackingCounter = 0;

            m_ToolGroupRFEGenFreqSweep.UpdateDeviceFrequency();

            m_ToolGroup_RFGenCW.UpdateDevicePower();

            m_objRFEAnalyzer.StartTrackingSequence(false);
            Application.DoEvents();
            UpdateButtonStatus();
            UpdateButtonStatus_RFGen();

            Cursor.Current = Cursors.Default;
        }

        private void OnTrackingStopChanged(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            HideNormalizatingMessage();
            m_objRFEAnalyzer.StopTracking();
            UpdateButtonStatus_RFGen();
            //DisplayTrackingData_TextProgress(true);
            m_TrackingProgressText.IsVisible = false;
            Cursor.Current = Cursors.Default;
        }

        private void UpdateTrackingStatusText()
        {
            if (!m_objRFEGenerator.PortConnected)
            {
                m_StatusGraphText_Tracking.Text = "Signal Generator DISCONNECTED";
                m_StatusGraphText_Tracking.FontSpec.FontColor = Color.DarkGray;
                m_StatusGraphText_Tracking.FontSpec.IsBold = false;
            }
            else
            {
                if (m_objRFEGenerator.RFGenPowerON)
                {
                    m_StatusGraphText_Tracking.Text = "RF POWER ON";
                    m_StatusGraphText_Tracking.FontSpec.FontColor = Color.Red;
                    m_StatusGraphText_Tracking.FontSpec.IsBold = true;
                }
                else
                {
                    m_StatusGraphText_Tracking.Text = "Signal Generator IDLE";
                    m_StatusGraphText_Tracking.FontSpec.FontColor = Color.DarkBlue;
                    m_StatusGraphText_Tracking.FontSpec.IsBold = false;
                }
            }
            m_GraphTrackingGenerator.Invalidate();
        }

        private void HideNormalizatingMessage()
        {
            m_TrackingStatus.IsVisible = false;
            m_GraphTrackingGenerator.Refresh();
        }

        private void ShowNormalizatingMessage()
        {
            m_TrackingStatus.IsVisible = true;
            m_GraphTrackingGenerator.Refresh();
        }

        private void SaveSNAS1P(string sFilename)
        {
            try
            {
                //if no file path was explicited, add the default folder
                if (sFilename.IndexOf("\\") < 0)
                {
                    sFilename = m_sDefaultUserFolder + "\\" + sFilename;
                    sFilename = sFilename.Replace("\\\\", "\\");
                }

                char cCSV = GetCSVDelimiter();

                using (StreamWriter myFile = new StreamWriter(sFilename, false))
                {
                    myFile.WriteLine("! RF Explorer SNA Tracking - S1P S-parameter data file");
                    myFile.WriteLine("! RF Explorer Spectrum Analyzer SN: " + m_objRFEAnalyzer.SerialNumber);
                    myFile.WriteLine("! RF Explorer Signal Generator SN: " + m_objRFEGenerator.SerialNumber);
                    myFile.WriteLine("!");
                    myFile.WriteLine("#  MHZ  S  DB  R  50");
                    myFile.WriteLine("!");
                    myFile.WriteLine("! Note SNA only contains dB magnitude, angle is always zero (not available)");
                    myFile.WriteLine("! symbol  freq-unit  parameter-type  data-format  keyword  impedance-ohms");
                    myFile.WriteLine("! Sx1 in dB will be S21 if you are measuring gain, or S11 if you are measuring reflection");
                    myFile.WriteLine("! [freq]   [Sx1 DB]   [angSx1]");

                    foreach (PointPair objPointPair in m_PointList_Tracking_Avg)
                    {
                        myFile.WriteLine(objPointPair.X.ToString("0.000") + "  " + objPointPair.Y.ToString("0.00") + "   0.0");
                    }
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
        }

        private void SaveSNACSV(string sFilename)
        {
            try
            {

                //if no file path was explicited, add the default folder
                if (sFilename.IndexOf("\\") < 0)
                {
                    sFilename = m_sDefaultUserFolder + "\\" + sFilename;
                    sFilename = sFilename.Replace("\\\\", "\\");
                }

                char cCSV = GetCSVDelimiter();

                using (StreamWriter myFile = new StreamWriter(sFilename, false))
                {
                    foreach (PointPair objPointPair in m_PointList_Tracking_Avg)
                    {
                        myFile.WriteLine(objPointPair.X.ToString("0.000") + cCSV + objPointPair.Y.ToString("0.00"));
                    }
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
        }

        private void menuSaveSNACSV_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog MySaveFileDialog = new SaveFileDialog())
                {
                    MySaveFileDialog.Filter = _CSV_File_Selector;
                    MySaveFileDialog.FilterIndex = 1;
                    MySaveFileDialog.RestoreDirectory = false;
                    MySaveFileDialog.InitialDirectory = m_sDefaultUserFolder;

                    MySaveFileDialog.FileName = GetNewFilename(RFExplorerFileType.SNATrackingCSVFile);

                    if (MySaveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        SaveSNACSV(MySaveFileDialog.FileName);
                        m_sDefaultUserFolder = Path.GetDirectoryName(MySaveFileDialog.FileName);
                        edDefaultFilePath.Text = m_sDefaultUserFolder;
                    }
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
        }

        private void menuSaveS1P_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog MySaveFileDialog = new SaveFileDialog())
                {
                    MySaveFileDialog.Filter = _S1P_File_Selector;
                    MySaveFileDialog.FilterIndex = 1;
                    MySaveFileDialog.RestoreDirectory = false;
                    MySaveFileDialog.InitialDirectory = m_sDefaultUserFolder;

                    MySaveFileDialog.FileName = GetNewFilename(RFExplorerFileType.SNATrackingS1PFile);

                    if (MySaveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        SaveSNAS1P(MySaveFileDialog.FileName);
                        m_sDefaultUserFolder = Path.GetDirectoryName(MySaveFileDialog.FileName);
                        edDefaultFilePath.Text = m_sDefaultUserFolder;
                    }
                }
            }
            catch (Exception obEx) { MessageBox.Show(obEx.Message); }
        }

        private void DisplayTrackingData_TextProgress(bool bRefresh)
        {
            if (m_objRFEAnalyzer.Mode == RFECommunicator.eMode.MODE_TRACKING)
            {
                if (!m_TrackingProgressText.IsVisible)
                    m_TrackingProgressText.IsVisible = true;

                if (m_objRFEAnalyzer.IsTrackingNormalizing)
                {
                    m_TrackingProgressText.Text = "Normalization progress (#" + (m_objRFEAnalyzer.TrackingNormalizingPass + 1).ToString() + "): " +
                    (100.0 * (m_objRFEAnalyzer.RFGenTrackingCurrentStep + m_objRFEAnalyzer.TrackingNormalizingPass * m_objRFEGenerator.RFGenSweepSteps) /
                    (m_objRFEGenerator.RFGenSweepSteps * (double)m_ToolGroup_RFEGenTracking.Average)).ToString("f0") + "%"; 
                }
                else
                {
                    string sTrackingStep = "Tracking step (#" + (m_objRFEAnalyzer.TrackingData.Count + 1).ToString() + "): " + m_objRFEAnalyzer.RFGenTrackingCurrentStep + "/" + m_objRFEGenerator.RFGenSweepSteps;
                    string sStepPercentage = (100.0 * m_objRFEAnalyzer.RFGenTrackingCurrentStep / m_objRFEGenerator.RFGenSweepSteps).ToString("f0") + "%";
                    m_TrackingProgressText.Text = sTrackingStep + " " + sStepPercentage;
                }
            }
            else
            {
                if (m_TrackingProgressText.IsVisible)
                    m_TrackingProgressText.IsVisible = false;
            }
            if (bRefresh)
                m_GraphTrackingGenerator.Refresh();
        }

        private double ConvertDB2VSWR(double fDB, double fClampVSWR = 0)
        {
            if (fDB > -0.01)
                fDB = -0.01; //clamp to real values that are not really caused by noise
            double fVSWR = (Math.Pow(10, (-fDB / 20)) + 1) / (Math.Pow(10, (-fDB / 20)) - 1);
            if (fClampVSWR > 1.0)
            {
                if (fVSWR > fClampVSWR)
                    fVSWR = fClampVSWR;
            }

            return fVSWR;
        }

        private void DisplayTrackingData()
        {
#if CALLSTACK_REALTIME
            Console.WriteLine("CALLSTACK:DisplayTrackingData");
#endif
            bool bPlaySound = false;

            bool bReinitGraphLimits = (m_PointList_Tracking_Avg.Count == 0);

            m_PointList_Tracking_Normal.Clear();
            m_GraphLine_Tracking_Normal.Clear();
            m_GraphLine_Tracking_Normal.Line.IsSmooth = menuSmoothSignals.Checked;
            m_PointList_Tracking_Avg.Clear();
            m_GraphLine_Tracking_Avg.Clear();
            m_GraphLine_Tracking_Avg.Line.IsSmooth = menuSmoothSignals.Checked;

            m_GraphTrackingGenerator.GraphPane.YAxis.MajorGrid.IsVisible = menuShowGrid.Checked;
            m_GraphTrackingGenerator.GraphPane.XAxis.MajorGrid.IsVisible = menuShowGrid.Checked;

            DisplayTrackingData_TextProgress(false);

            if (m_objRFEAnalyzer.IsTrackingNormalizing != m_TrackingStatus.IsVisible)
            {
                m_TrackingStatus.IsVisible = m_objRFEAnalyzer.IsTrackingNormalizing;
                m_GraphTrackingGenerator.Refresh();
            }

            m_MarkersSNA.HideAllMarkers();
            //draw all marker except tracking 1
            UpdateMarkerCollectionFromMenuSNA();
            //remove old text from all peak track markers and redraw
            m_MarkersSNA.CleanAllMarkerText(0);

            if (m_objRFEAnalyzer.TrackingData.Count == 0)
            {
                m_GraphTrackingGenerator.Refresh();
                return; //nothing to paint
            }

            //Use the latest data loaded
            uint nLastSample = m_objRFEAnalyzer.TrackingData.Count - 1;
            uint nAvailableAverageSamples = 1;
            uint nAverageIterations = m_ToolGroup_RFEGenTracking.Average; 
            if (nAverageIterations == 0)
                nAverageIterations = 1;
            if (nLastSample >= (nAverageIterations - 1))
                nAvailableAverageSamples = nAverageIterations;
            else
                nAvailableAverageSamples = nLastSample + 1;

            RFESweepData objSweep = m_objRFEAnalyzer.TrackingData.GetData(nLastSample);
            RFESweepData objSweepAvg = m_objRFEAnalyzer.TrackingData.GetMedianAverage(nLastSample + 1 - nAvailableAverageSamples, nLastSample);
            objSweep.StepFrequencyMHZ = m_objRFEGenerator.RFGenStepMHZ(); //step tracking frequency is not known by analyzer

            double dMinDB = 100;
            double dMaxDB = -120;
            double dMarginMinDB = 10;
            double dMarginMaxDB = 10;

            for (UInt16 nInd = 0; nInd < objSweep.TotalSteps; nInd++)
            {
                //normal realtime
                double dDB = objSweep.GetAmplitudeDBM(nInd) - m_objRFEAnalyzer.TrackingNormalizedData.GetAmplitudeDBM(nInd);
                if (dDB > dMaxDB)
                    dMaxDB = dDB;
                if (dDB < dMinDB)
                    dMinDB = dDB;
                m_PointList_Tracking_Normal.Add(objSweep.GetFrequencyMHZ(nInd), dDB, RFECommunicator.MIN_AMPLITUDE_DBM);
                //average
                if (objSweepAvg != null)
                {
                    dDB = objSweepAvg.GetAmplitudeDBM(nInd) - m_objRFEAnalyzer.TrackingNormalizedData.GetAmplitudeDBM(nInd);
                    if (dDB > dMaxDB)
                        dMaxDB = dDB;
                    if (dDB < dMinDB)
                        dMinDB = dDB;
                    m_PointList_Tracking_Avg.Add(objSweepAvg.GetFrequencyMHZ(nInd), dDB, RFECommunicator.MIN_AMPLITUDE_DBM);
                }
            }

            bool bUseVSWR = (m_ToolGroup_RFEGenTracking.ListSNAOptionsIndex == 2);

            if (bUseVSWR)
            {
                m_PointList_Tracking_Normal.ConvertToVSWR(100);
                m_PointList_Tracking_Avg.ConvertToVSWR(100);
            }

            m_GraphLine_Tracking_Normal.Points = m_PointList_Tracking_Normal;
            m_GraphLine_Tracking_Normal.IsVisible = true;
            m_GraphLine_Tracking_Normal.Label.IsVisible = true;

            m_GraphLine_Tracking_Avg.Points = m_PointList_Tracking_Avg;
            m_GraphLine_Tracking_Avg.IsVisible = true;
            m_GraphLine_Tracking_Avg.Label.IsVisible = true;

            if (!bUseVSWR)
            {
                //Limit lines are not supported in VSWR mode
                if (m_LimitLineGenerator_Max.Count > 1)
                {
                    PointPairList listCheck = null;
                    SelectSinglePointPairList(ref listCheck);
                    if (listCheck != null)
                    {
                        m_GraphLimitLineGenerator_Max.Points = m_LimitLineGenerator_Max;
                        m_GraphLimitLineGenerator_Max.IsVisible = true;
                        if (m_LimitLineGenerator_Max.Intersect(listCheck, true))
                        {
                            bPlaySound = true;
                            m_GraphLimitLineGenerator_Max.Line.Width = 5;
                        }
                        else
                        {
                            m_GraphLimitLineGenerator_Max.Line.Width = 1;
                        }

                        int nDummy;
                        m_LimitLineGenerator_Max.GetMaxMinValues(ref dMinDB, ref dMaxDB, out nDummy, out nDummy);
                        dMarginMaxDB = 5;
                    }
                    else
                    {
                        m_GraphLimitLineGenerator_Max.IsVisible = false;
                    }
                }

                if (m_LimitLineGenerator_Min.Count > 1)
                {
                    PointPairList listCheck = null;
                    SelectSinglePointPairList(ref listCheck);
                    if (listCheck != null)
                    {
                        m_GraphLimitLineGenerator_Min.Points = m_LimitLineGenerator_Min;
                        m_GraphLimitLineGenerator_Min.IsVisible = true;
                        if (m_LimitLineGenerator_Min.Intersect(listCheck, false))
                        {
                            bPlaySound = true;
                            m_GraphLimitLineGenerator_Min.Line.Width = 5;
                        }
                        else
                        {
                            m_GraphLimitLineGenerator_Min.Line.Width = 1;
                        }

                        int nDummy;
                        m_LimitLineGenerator_Min.GetMaxMinValues(ref dMinDB, ref dMaxDB, out nDummy, out nDummy);
                        dMarginMinDB = 5;
                    }
                    else
                    {
                        m_GraphLimitLineGenerator_Min.IsVisible = false;
                    }
                }
            }
            else
            {
                m_GraphLimitLineGenerator_Min.IsVisible = false;
                m_GraphLimitLineGenerator_Max.IsVisible = false;
            }

            m_GraphTrackingGenerator.GraphPane.XAxis.Scale.Min = objSweep.StartFrequencyMHZ;
            m_GraphTrackingGenerator.GraphPane.XAxis.Scale.Max = objSweep.EndFrequencyMHZ;
            double dGraphMin = m_GraphTrackingGenerator.GraphPane.YAxis.Scale.Min;
            if (bReinitGraphLimits)
                dGraphMin = -1E6;
            if (bUseVSWR || (dGraphMin < dMinDB - (dMarginMinDB * 1.5)) || (dGraphMin > dMinDB - 5))
            {
                if (bUseVSWR)
                    m_GraphTrackingGenerator.GraphPane.YAxis.Scale.Min = 1;
                else
                    m_GraphTrackingGenerator.GraphPane.YAxis.Scale.Min = dMinDB - dMarginMinDB;
            }
            double dGraphMax = m_GraphTrackingGenerator.GraphPane.YAxis.Scale.Max;
            if (bReinitGraphLimits)
                dGraphMax = 1E6;
            if (bUseVSWR || (dGraphMax > dMaxDB + (dMarginMaxDB * 1.5)) || (dGraphMax < dMaxDB + 5))
            {
                if (bUseVSWR)
                {
                    m_GraphTrackingGenerator.GraphPane.YAxis.Scale.Max = ConvertDB2VSWR(0.95 * dMaxDB, 105);
                }
                else
                    m_GraphTrackingGenerator.GraphPane.YAxis.Scale.Max = dMaxDB + dMarginMaxDB;
            }

            if (bPlaySound && menuItemSoundAlarmLimitLine.Checked)
            {
                PlayNotificationSound();
            }
            else
            {
                StopNotificationSound();
            }


            //draw marker 1
            double fTrackPeakMHZ = 0.0;
            if (m_arrMarkersEnabledMenu[0].Checked)
            {
                int nNormalIndex = -1;
                int nAvgIndex = -1;

                if (m_ToolGroup_RFEGenTracking.ListSNAOptions.Contains("Return Loss")) 
                {
                    nNormalIndex = m_PointList_Tracking_Normal.GetIndexMin();
                    nAvgIndex = m_PointList_Tracking_Avg.GetIndexMin();
                }
                else
                {
                    nNormalIndex = m_PointList_Tracking_Normal.GetIndexMax();
                    nAvgIndex = m_PointList_Tracking_Avg.GetIndexMax();
                }

                double fTrackDBM = RFECommunicator.MIN_AMPLITUDE_DBM;
                if (m_ToolGroup_Markers_SNA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Realtime)
                {
                    fTrackPeakMHZ = m_PointList_Tracking_Normal[nNormalIndex].X;
                    fTrackDBM = m_PointList_Tracking_Normal[nNormalIndex].Y;
                }
                else if (m_ToolGroup_Markers_SNA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Average)
                {
                    fTrackPeakMHZ = m_PointList_Tracking_Avg[nAvgIndex].X;
                    fTrackDBM = m_PointList_Tracking_Avg[nAvgIndex].Y;
                }
                else
                {
                    m_arrMarkersEnabledMenu[0].Checked = false;
                    UpdateSNAMarkerControlContents();
                }
                m_MarkersSNA.SetMarkerFrequency(0, fTrackPeakMHZ);
            }

            if (m_arrMarkersEnabledMenu[0].Checked)
            {
                double dAmplitude = m_PointList_Tracking_Normal.InterpolateX(m_MarkersSNA.GetMarkerFrequency(0));
                m_MarkersSNA.UpdateMarker(0, RFECommunicator.RFExplorerSignalType.Realtime, dAmplitude);
                if ((m_ToolGroup_Markers_SNA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Realtime) && menuShowPeak.Checked)
                {
                    m_MarkersSNA.SetMarkerText(0, RFECommunicator.RFExplorerSignalType.Realtime, m_MarkersSNA.GetMarkerFrequency(0).ToString("0.000") + "MHZ\n" + dAmplitude.ToString("0.00") + GetCurrentAmplitudeUnitLabel());
                }

                dAmplitude = m_PointList_Tracking_Avg.InterpolateX(m_MarkersSNA.GetMarkerFrequency(0));
                m_MarkersSNA.UpdateMarker(0, RFECommunicator.RFExplorerSignalType.Average, dAmplitude);
                if ((m_ToolGroup_Markers_SNA.TrackSignalPeak == RFECommunicator.RFExplorerSignalType.Average) && menuShowPeak.Checked)
                {
                    m_MarkersSNA.SetMarkerText(0, RFECommunicator.RFExplorerSignalType.Average, m_MarkersSNA.GetMarkerFrequency(0).ToString("0.000") + "MHZ\n" + dAmplitude.ToString("0.00") + GetCurrentAmplitudeUnitLabel());
                }
            }
            UpdateMarkerCollectionFromMenuSNA();
            //remove old text from all peak track markers and redraw
            m_MarkersSNA.CleanAllMarkerText(0);

            UpdateSNAMarkerControlValues();
            m_GraphTrackingGenerator.AxisChange();
            m_GraphTrackingGenerator.Refresh();
        }

        private void UpdateMarkerCollectionFromMenuSNA()
        {
            for (int nMenuInd = 0; nMenuInd < m_arrMarkersEnabledMenu.Length; nMenuInd++)
            {
                if (m_arrMarkersEnabledMenu[nMenuInd].Checked)
                {
                    if (menuRealtimeTrace.Checked)
                    {
                        if (m_PointList_Tracking_Normal != null && m_PointList_Tracking_Normal.Count > 0)
                            m_MarkersSNA.UpdateMarker(nMenuInd, RFECommunicator.RFExplorerSignalType.Realtime, m_PointList_Tracking_Normal.InterpolateX(m_MarkersSNA.GetMarkerFrequency(nMenuInd)));
                        else
                            m_MarkersSNA.UpdateMarker(nMenuInd, RFECommunicator.RFExplorerSignalType.Realtime, RFECommunicator.MIN_AMPLITUDE_DBM);
                    }
                    if (menuAveragedTrace.Checked)
                    {
                        if (m_PointList_Tracking_Avg != null && m_PointList_Tracking_Avg.Count > 0)
                            m_MarkersSNA.UpdateMarker(nMenuInd, RFECommunicator.RFExplorerSignalType.Average, m_PointList_Tracking_Avg.InterpolateX(m_MarkersSNA.GetMarkerFrequency(nMenuInd)));
                        else
                            m_MarkersSNA.UpdateMarker(nMenuInd, RFECommunicator.RFExplorerSignalType.Average, RFECommunicator.MIN_AMPLITUDE_DBM);
                    }
                }
            }
        }

        #endregion
    }
}
