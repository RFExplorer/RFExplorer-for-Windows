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
using System.Linq;
using System.Text;
using System.IO;

namespace RFExplorerCommunicator
{
    /// <summary>
    /// Class support a full sweep of data from RF Explorer, and it is used in the RFESweepDataCollection container
    /// </summary>
    public class RFESweepData
    {
        /// <summary>
        /// Start frequency
        /// </summary>
        double m_fStartFrequencyMHZ;
        public double StartFrequencyMHZ
        {
            get { return m_fStartFrequencyMHZ; }
        }

        public double EndFrequencyMHZ
        {
            get { return GetFrequencyMHZ((ushort)(m_nTotalSteps - 1)); }
        }

        /// <summary>
        /// Step frequency between each sweep step
        /// </summary>
        double m_fStepFrequencyMHZ;
        public double StepFrequencyMHZ
        {
            get { return m_fStepFrequencyMHZ; }
            set { m_fStepFrequencyMHZ = value; }
        }

        /// <summary>
        /// Total number of sweep steps captured
        /// </summary>
        UInt16 m_nTotalSteps;
        public UInt16 TotalSteps
        {
            get { return m_nTotalSteps; }
        }

        /// <summary>
        /// The actual data container, a consecutive set of dBm amplitude values
        /// </summary>
        float[] m_arrAmplitude;

        /// <summary>
        /// The time when this data sweep was created, it should match as much as possible the real data capture
        /// </summary>
        DateTime m_Time;
        public DateTime CaptureTime
        {
            get { return m_Time; }
            set { m_Time = value; }
        }

        public RFESweepData(double StartFreqMHZ, double StepFreqMHZ, UInt16 nTotalSteps)
        {
            m_Time = DateTime.Now;
            m_nTotalSteps = nTotalSteps;
            m_fStartFrequencyMHZ = StartFreqMHZ;
            m_fStepFrequencyMHZ = StepFreqMHZ;
            m_arrAmplitude = new float[m_nTotalSteps];
            for (int nInd = 0; nInd < m_nTotalSteps; nInd++)
                m_arrAmplitude[nInd] = RFECommunicator.MIN_AMPLITUDE_DBM;
        }

        //variable used to internall store byte array received if is used externally
        byte[] m_arrBLOB=null;
        //variable used to internall store byte array in string format received if is used externally
        string m_sBLOBString = "";

        /// <summary>
        /// This function will process a received, full consistent string received from remote device
        /// and fill it in all data
        /// </summary>
        /// <param name="sLine">string received from device, previously parsed and validated</param>
        /// <param name="fOffsetDB">currently specified offset in DB</param>
        /// <param name="bBLOB">if true the internal BLOB object will be filled in for later use in GetBLOB</param>
        /// <param name="bString">if true the internal string object will be filled in for later use in GetBLOBString</param>
        public bool ProcessReceivedString(string sLine, float fOffsetDB, bool bBLOB=false, bool bString=false)
        {
            bool bOk = true;

            try
            {
                if ((sLine.Length > 2) && (sLine.Substring(0, 2) == "$S"))
                {
                    if (bBLOB)
                        m_arrBLOB = new byte[TotalSteps];
                    RFESweepData objSweep = new RFESweepData((float)StartFrequencyMHZ, (float)StepFrequencyMHZ, TotalSteps);
                    objSweep.CaptureTime = DateTime.Now;
                    if (bString)
                        m_sBLOBString = sLine.Substring(2, TotalSteps);
                    for (ushort nInd = 0; nInd < TotalSteps; nInd++)
                    {
                        byte nVal = Convert.ToByte(sLine[2 + nInd]);
                        float fVal = nVal / -2.0f;
                        if (bBLOB)
                            m_arrBLOB[nInd] = nVal;
                        SetAmplitudeDBM(nInd, fVal + fOffsetDB);
                    }
                }
                else
                    bOk = false;
            }
            catch (Exception)
            {
                bOk = false;
            }

            return bOk;
        }

        /// <summary>
        /// Returns amplitude data in dBm. This is the value as it was read from the device or from a file
        /// so it is not adjusted by offset or additionally compensated in any way. If the value was read from a device,
        /// it may already be an adjusted value including device configured offset.
        /// </summary>
        /// <param name="nStep">Internal frequency step or bucket to read data from</param>
        /// <returns>Value in dBm</returns>
        public float GetAmplitudeDBM(UInt16 nStep)
        {
            return GetAmplitudeDBM(nStep, null, false);
        }

        /// <summary>
        /// If selected bBLOB in ProcessReceivedString() then raw scan data is available here in byte array format
        /// </summary>
        /// <param name="arrBLOB">
        /// Returns a byte array compatibly with BLOB on a DB or other byte array uses.
        /// Expected to come as NULL, will initialize memory and fill in with data bytes. Will be null if no data available
        /// </param>
        /// <returns>Returns false if no data available</returns>
        public bool GetBLOB(ref byte[] arrBLOB)
        {            
            if (m_arrBLOB == null || m_arrBLOB.Length == 0)
                return false;

            try
            {
                arrBLOB = new byte[TotalSteps];
                m_arrBLOB.CopyTo(arrBLOB, 0);
            }
            catch (Exception obEx)
            {
                Console.WriteLine(obEx.ToString());
                return false;
            }

            return true;
        }
        /// <summary>
        /// If selected bString in ProcessReceivedString() then raw scan data is available here in string format
        /// </summary>
        /// <returns>BLOB string in if available or empty string if not</returns>
        public string GetBLOBString()
        {
            return m_sBLOBString;
        }

        /// <summary>
        /// Internally adjust the sweep amplitude based on normalized amplitude objNormalizedAmplitudeReference provided
        /// </summary>
        /// <returns></returns>
        public bool NormalizeAmplitude(RFESweepData objNormalizedAmplitudeReference)
        {
            if (!IsSameConfiguration(objNormalizedAmplitudeReference))
                return false;

            for (UInt16 nInd = 0; nInd < TotalSteps; nInd++)
            {
                //normal realtime
                float dDB = GetAmplitudeDBM(nInd) - objNormalizedAmplitudeReference.GetAmplitudeDBM(nInd);
                SetAmplitudeDBM(nInd, dDB);
            }

            return true;
        }

        /// <summary>
        /// Returns amplitude data in dBm. This is the value as it was read from the device or from a file
        /// so it is not adjusted by offset or additionally compensated in any way. If the value was read from a device,
        /// it may already be an adjusted value including device configured offset.
        /// </summary>
        /// <param name="nStep">Internal frequency step or bucket to read data from</param>
        /// <param name="AmplitudeCorrection">Optional parameter, can be null. If different than null, use the amplitude correction table</param>
        /// <param name="bUseCorrection">If the AmplitudeCorrection is not null, this boolean will tell whether to use it or not</param>
        /// <returns>Value in dBm</returns>
        public float GetAmplitudeDBM(UInt16 nStep, RFEAmplitudeTableData AmplitudeCorrection, bool bUseCorrection)
        {
            if (nStep < m_nTotalSteps)
            {
                if ((AmplitudeCorrection != null) && bUseCorrection)
                {
                    return m_arrAmplitude[nStep] + AmplitudeCorrection.GetAmplitudeCalibration((int)GetFrequencyMHZ(nStep));
                }
                else
                {
                    return m_arrAmplitude[nStep];
                }
            }
            else
                return RFECommunicator.MIN_AMPLITUDE_DBM;
        }

        public void SetAmplitudeDBM(UInt16 nStep, float fDBM)
        {
            if (nStep < m_nTotalSteps)
                m_arrAmplitude[nStep] = fDBM;
        }

        public double GetFrequencyMHZ(UInt16 nStep)
        {
            if (nStep < m_nTotalSteps)
                return m_fStartFrequencyMHZ + (m_fStepFrequencyMHZ * nStep);
            else
                return 0.0f;
        }

        public double GetFrequencySpanMHZ()
        {
            return (m_fStepFrequencyMHZ * (m_nTotalSteps - 1));
        }

        /// <summary>
        /// Returns the step of the lowest amplitude value found
        /// </summary>
        /// <returns></returns>
        public UInt16 GetMinStep()
        {
            UInt16 nStep = 0;
            float fMin = RFECommunicator.MAX_AMPLITUDE_DBM;

            for (UInt16 nInd = 0; nInd < m_nTotalSteps; nInd++)
            {
                if (fMin > m_arrAmplitude[nInd])
                {
                    fMin = m_arrAmplitude[nInd];
                    nStep = nInd;
                }
            }
            return nStep;
        }

        /// <summary>
        /// Returns the step of the highest amplitude value found
        /// </summary>
        /// <returns></returns>
        public UInt16 GetPeakStep()
        {
            UInt16 nStep = 0;
            float fPeak = RFECommunicator.MIN_AMPLITUDE_DBM;

            for (UInt16 nInd = 0; nInd < m_nTotalSteps; nInd++)
            {
                if (fPeak < m_arrAmplitude[nInd])
                {
                    fPeak = m_arrAmplitude[nInd];
                    nStep = nInd;
                }
            }
            return nStep;
        }

        public bool IsSameConfiguration(RFESweepData objOther)
        {
            return (Math.Abs(objOther.StartFrequencyMHZ - StartFrequencyMHZ) < 0.001 && Math.Abs(objOther.StepFrequencyMHZ - StepFrequencyMHZ) < 0.001 && (objOther.TotalSteps == TotalSteps));
        }

        public RFESweepData Duplicate()
        {
            RFESweepData objSweep = new RFESweepData(m_fStartFrequencyMHZ, m_fStepFrequencyMHZ, m_nTotalSteps);

            Array.Copy(m_arrAmplitude, objSweep.m_arrAmplitude, m_nTotalSteps);

            return objSweep;
        }

        /// <summary>
        /// Returns power channel over the full span being captured. The power is instantaneous real time
        /// For average power channel use the collection method GetAverageChannelPower().
        /// </summary>
        /// <returns>channel power in dBm/span</returns>
        public double GetChannelPowerDBM()
        {
            double fChannelPower = RFECommunicator.MIN_AMPLITUDE_DBM;
            double fPowerTemp=0.0f;

            for (UInt16 nInd = 0; nInd < m_nTotalSteps; nInd++)
            {
                fPowerTemp += RFECommunicator.Convert_dBm_2_mW(m_arrAmplitude[nInd]);
            }

            if (fPowerTemp>0.0f)
            {
                //add here actual RBW calculation in the future - currently we are assuming frequency step is the same
                //as RBW which is not 100% accurate.
                fChannelPower = RFECommunicator.Convert_mW_2_dBm(fPowerTemp); 
            }

            return fChannelPower;
        }

        /// <summary>
        /// Dump a CSV string line with sweep data
        /// </summary>
        /// <returns></returns>
        public string Dump()
        {
            string sResult;
            sResult= "Sweep data " + m_fStartFrequencyMHZ.ToString("f3") + "MHz " + m_fStepFrequencyMHZ.ToString("f3") + "MHz " + m_nTotalSteps + "Steps: ";

            for (UInt16 nStep = 0; nStep < TotalSteps; nStep++)
            {
                if (nStep > 0)
                {
                    sResult += ",";
                }
                if ((nStep % 16) == 0)
                {
                    sResult += Environment.NewLine;
                }
                sResult += GetAmplitudeDBM(nStep).ToString("00.0");
            }
            return sResult;
        }
    }

    /// <summary>
    /// A collection of RFESweepData objects, each one with independent Sweep configuration and data points
    /// </summary>
    public class RFESweepDataCollection
    {
        public const int MAX_ELEMENTS = (10 * 1000 * 1000);    //This is the absolute max size that can be allocated
        const byte FILE_VERSION = 2;         //File format constant indicates the latest known and supported file format
        RFESweepData[] m_arrData;            //Collection of available spectrum data items
        RFESweepData m_MaxHoldData = null;   //Single data set, defined for the whole collection and updated with Add, to keep the Max Hold values
        public RFESweepData MaxHoldData
        {
            get { return m_MaxHoldData; }
        }
        int m_nUpperBound = -1;              //Max value for index with available data
        UInt32 m_nInitialCollectionSize = 0;

        bool m_bAutogrow;                   //true if the array bounds may grow up to MAX_ELEMENTS, otherwise will be limited to initial collection size

        /// <summary>
        /// Returns the total of elements with actual data allocated.
        /// </summary>
        public UInt32 Count
        {
            get { return ((UInt32)(m_nUpperBound + 1)); }
        }

        /// <summary>
        /// Returns the highest valid index of elements with actual data allocated.
        /// </summary>
        public int UpperBound
        {
            get { return m_nUpperBound; }
        }

        /// <summary>
        /// Allocates up to nCollectionSize elements to start with the container.
        /// </summary>
        /// <param name="nCollectionSize">Upper limit is RFESweepDataCollection.MAX_ELEMENTS</param>
        public RFESweepDataCollection(UInt32 nCollectionSize, bool bAutogrow)
        {
            if (nCollectionSize > MAX_ELEMENTS)
                nCollectionSize = MAX_ELEMENTS;

            m_bAutogrow = bAutogrow;

            m_nInitialCollectionSize = nCollectionSize;

            CleanAll();
        }

        /// <summary>
        /// Return the data pointed by the zero-starting index
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns>returns null if no data is available with this index</returns>
        public RFESweepData GetData(UInt32 nIndex)
        {
            if (nIndex <= m_nUpperBound)
            {
                return m_arrData[nIndex];
            }
            else
                return null;
        }

        /// <summary>
        /// True when the absolute maximum of allowed elements in the container is allocated
        /// </summary>
        /// <returns></returns>
        public bool IsFull()
        {
            return (m_nUpperBound >= MAX_ELEMENTS);
        }

        public bool Add(RFESweepData SweepData)
        {
            try
            {
                if (IsFull())
                    return false;

                if (m_MaxHoldData == null)
                {
                    m_MaxHoldData = new RFESweepData(SweepData.StartFrequencyMHZ, SweepData.StepFrequencyMHZ, SweepData.TotalSteps);
                }

                if (m_nUpperBound >= (m_arrData.Length - 1))
                {
                    if (m_bAutogrow)
                    {
                        ResizeCollection(10 * 1000); //add 10K samples more
                    }
                    else
                    {
                        //move all items one position down, lose the older one at position 0
                        m_nUpperBound = m_arrData.Length - 2;
                        m_arrData[0] = null;
                        for (int nInd = 0; nInd <= m_nUpperBound; nInd++)
                        {
                            m_arrData[nInd] = m_arrData[nInd + 1];
                        }
                    }
                }

                m_nUpperBound++;
                m_arrData[m_nUpperBound] = SweepData;

                for (UInt16 nInd = 0; nInd < SweepData.TotalSteps; nInd++)
                {
                    if (SweepData.GetAmplitudeDBM(nInd,null,false) > m_MaxHoldData.GetAmplitudeDBM(nInd,null,false))
                    {
                        m_MaxHoldData.SetAmplitudeDBM(nInd, SweepData.GetAmplitudeDBM(nInd,null,false));
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public void CleanAll()
        {
            if (m_arrData != null)
            {
                Array.Clear(m_arrData, 0, m_arrData.Length);
            }
            m_arrData = new RFESweepData[m_nInitialCollectionSize];
            m_MaxHoldData = null;
            m_nUpperBound = -1;
        }

        public string Dump()
        {
            string sDump = "";
            foreach(RFESweepData objSweep in m_arrData)
            {
                if (!String.IsNullOrEmpty(sDump))
                {
                    sDump += Environment.NewLine;
                }
                if (objSweep != null)
                {
                    sDump += objSweep.Dump();
                }
                else
                    sDump += "Sweep {null}";
            }
            return sDump;
        }

        public RFESweepData GetMedianAverage(UInt32 nStart, UInt32 nEnd)
        {
            RFESweepData objReturn = null;

            //string sDebugText = "";

            if (nStart > m_nUpperBound || nEnd > m_nUpperBound || nStart > nEnd)
            {
                return null;
            }

            UInt32 nTotalIterations = nEnd - nStart + 1;
            try
            {
                objReturn = new RFESweepData(m_arrData[nEnd].StartFrequencyMHZ, m_arrData[nEnd].StepFrequencyMHZ, m_arrData[nEnd].TotalSteps);

                for (UInt16 nSweepInd = 0; nSweepInd < objReturn.TotalSteps; nSweepInd++)
                {
                    //sDebugText += "[" + nSweepInd + "]:";
                    float fSweepValue = 0f;
                    float[] arrSweepValues = new float[nTotalIterations];

                    for (UInt32 nIterationInd = nStart; nIterationInd <= nEnd; nIterationInd++)
                    {
                        if (nSweepInd == 0)
                        {
                            //check all the sweeps use the same configuration, but only in first loop to reduce overhead
                            if (!m_arrData[nIterationInd].IsSameConfiguration(objReturn))
                                return null;
                        }
                        arrSweepValues[nIterationInd- nStart]= m_arrData[nIterationInd].GetAmplitudeDBM(nSweepInd, null, false);
                        //sDebugText += m_arrData[nIterationInd].GetAmplitudeDBM(nSweepInd).ToString("f2") + ",";
                    }
                    Array.Sort(arrSweepValues);
                    fSweepValue = arrSweepValues[nTotalIterations / 2];
                    //sDebugText += "(" + fSweepValue.ToString("f2") + ")";
                    objReturn.SetAmplitudeDBM(nSweepInd, fSweepValue);
                }
            }
            catch
            {
                objReturn = null;
            }
            return objReturn;
        }

        public RFESweepData GetAverage(UInt32 nStart, UInt32 nEnd)
        {
            RFESweepData objReturn = null;

            //string sDebugText = "";

            if (nStart > m_nUpperBound || nEnd > m_nUpperBound || nStart > nEnd)
            {
                return null;
            }

            try
            {
                objReturn = new RFESweepData(m_arrData[nEnd].StartFrequencyMHZ, m_arrData[nEnd].StepFrequencyMHZ, m_arrData[nEnd].TotalSteps);

                for (UInt16 nSweepInd = 0; nSweepInd < objReturn.TotalSteps; nSweepInd++)
                {
                    //sDebugText += "[" + nSweepInd + "]:";
                    float fSweepValue = 0f;
                    for (UInt32 nIterationInd = nStart; nIterationInd <= nEnd; nIterationInd++)
                    {
                        if (nSweepInd == 0)
                        {
                            //check all the sweeps use the same configuration, but only in first loop to reduce overhead
                            if (!m_arrData[nIterationInd].IsSameConfiguration(objReturn))
                                return null;
                        }

                        fSweepValue += m_arrData[nIterationInd].GetAmplitudeDBM(nSweepInd,null,false);
                        //sDebugText += m_arrData[nIterationInd].GetAmplitudeDBM(nSweepInd).ToString("f2") + ",";
                    }
                    fSweepValue = fSweepValue / (nEnd - nStart + 1);
                    //sDebugText += "(" + fSweepValue.ToString("f2") + ")";
                    objReturn.SetAmplitudeDBM(nSweepInd, fSweepValue);
                }
            }
            catch
            {
                objReturn = null;
            }
            return objReturn;
        }

        private string FileHeaderVersioned_001()
        {
            return "RFExplorer PC Client - Format v001";
        }

        private string FileHeaderVersioned()
        {
            return "RFExplorer PC Client - Format v" + FILE_VERSION.ToString("D3");
        }

        /// <summary>
        /// Will write large, complex, multi-sweep CSV file
        /// </summary>
        /// <param name="sFilename"></param>
        /// <param name="cCSVDelimiter"></param>
        public void SaveFileCSV(string sFilename, char cCSVDelimiter, RFEAmplitudeTableData AmplitudeCorrection)
        {
            if (m_nUpperBound <= 0)
            {
                return;
            }

            RFESweepData objFirst = m_arrData[0];

            using (StreamWriter myFile = new StreamWriter(sFilename, false))
            {
                myFile.WriteLine("RF Explorer CSV data file: " + FileHeaderVersioned());
                myFile.WriteLine("Start Frequency: " + objFirst.StartFrequencyMHZ.ToString() + "MHZ" + Environment.NewLine +
                    "Step Frequency: " + (objFirst.StepFrequencyMHZ * 1000).ToString() + "KHZ" + Environment.NewLine +
                    "Total data entries: " + m_nUpperBound.ToString() + Environment.NewLine +
                    "Steps per entry: " + objFirst.TotalSteps.ToString());

                string sHeader = "Sweep" + cCSVDelimiter + "Date" + cCSVDelimiter + "Time" + cCSVDelimiter + "Milliseconds";

                for (UInt16 nStep = 0; nStep < objFirst.TotalSteps; nStep++)
                {
                    double dFrequency = objFirst.StartFrequencyMHZ + nStep * (objFirst.StepFrequencyMHZ);
                    sHeader += cCSVDelimiter + dFrequency.ToString("0000.000");
                }

                myFile.WriteLine(sHeader);

                for (int nSweepInd = 0; nSweepInd < m_nUpperBound; nSweepInd++)
                {
                    myFile.Write(nSweepInd.ToString() + cCSVDelimiter);

                    myFile.Write(m_arrData[nSweepInd].CaptureTime.ToShortDateString() + cCSVDelimiter +
                        m_arrData[nSweepInd].CaptureTime.ToString("HH:mm:ss") + cCSVDelimiter +
                        m_arrData[nSweepInd].CaptureTime.ToString("\\.fff") + cCSVDelimiter);

                    if (!m_arrData[nSweepInd].IsSameConfiguration(objFirst))
                        break;

                    for (UInt16 nStep = 0; nStep < objFirst.TotalSteps; nStep++)
                    {
                        myFile.Write(m_arrData[nSweepInd].GetAmplitudeDBM(nStep, AmplitudeCorrection, AmplitudeCorrection!=null));
                        if (nStep != (objFirst.TotalSteps - 1))
                            myFile.Write(cCSVDelimiter);
                    }
                    myFile.Write(Environment.NewLine);
                }
            }
        }

        /// <summary>
        /// Saves a file in RFE standard format. Note it will not handle exceptions so the calling application can deal with GUI details
        /// Note: if there are sweeps with different start/stop frequencies, only the first one will be saved to disk
        /// </summary>
        /// <param name="sFilename"></param>
        public void SaveFile(string sFilename, string sModelText, string sConfigurationText, RFEAmplitudeTableData AmplitudeCorrection)
        {
            if (m_nUpperBound < 0)
            {
                return;
            }

            RFESweepData objFirst = m_arrData[0];
            int nTotalSweepsActuallySaved = 0;

            //Save file
            FileStream myFile = null;

            try
            {
                myFile = new FileStream(sFilename, FileMode.Create);

                using (BinaryWriter binStream = new BinaryWriter(myFile))
                {
                    binStream.Write((string)FileHeaderVersioned());
                    binStream.Write((double)objFirst.StartFrequencyMHZ);
                    binStream.Write((double)objFirst.StepFrequencyMHZ);
                    //NOTE: if we have different values for start/stop, we are saying we have more than we actually saved
                    //This is why we will save these parameters later again with nTotalSweepsActuallySaved
                    binStream.Write((UInt32)m_nUpperBound);

                    binStream.Write((UInt16)objFirst.TotalSteps);
                    binStream.Write((string)sConfigurationText);
                    binStream.Write((string)sModelText);

                    for (int nSweepInd = 0; nSweepInd <= m_nUpperBound; nSweepInd++)
                    {
                        if (!m_arrData[nSweepInd].IsSameConfiguration(objFirst))
                            break;

                        //new in v002 - save date/time for each captured sweep
                        string sTime = m_arrData[nSweepInd].CaptureTime.ToString("o");
                        binStream.Write((Int32)sTime.Length);
                        binStream.Write((string)sTime);

                        nTotalSweepsActuallySaved++;
                        for (UInt16 nStep = 0; nStep < objFirst.TotalSteps; nStep++)
                        {
                            binStream.Write((double)m_arrData[nSweepInd].GetAmplitudeDBM(nStep,AmplitudeCorrection,AmplitudeCorrection!=null));
                        }
                    }

                    //Save file fields again (will overwrite old ones), just to make sure nTotalSweepsActuallySaved is properly saved with actual value used
                    myFile.Seek(0, SeekOrigin.Begin);
                    binStream.Write((string)FileHeaderVersioned());
                    binStream.Write((double)objFirst.StartFrequencyMHZ);
                    binStream.Write((double)objFirst.StepFrequencyMHZ);
                    binStream.Write((Int32)nTotalSweepsActuallySaved);
                }
            }
            finally
            {
                if (myFile != null)
                    myFile.Dispose();
            }

            try
            {
                myFile = new FileStream(sFilename, FileMode.Open);

                using (BinaryWriter binStream = new BinaryWriter(myFile))
                {
                    myFile = null;

                    binStream.Write(FileHeaderVersioned());
                    binStream.Write(objFirst.StartFrequencyMHZ);
                    binStream.Write(objFirst.StepFrequencyMHZ);
                    binStream.Write(nTotalSweepsActuallySaved);
                }
            }
            finally
            {
                if (myFile != null)
                    myFile.Dispose();
            }
        }

        /// <summary>
        /// 
        /// 
        /// 
        /// </summary>
        /// <param name="sFile"></param>
        /// <returns></returns>
        //

        /// <summary>
        /// Will load a RFE standard file from disk. If the file format is incorrect (unknown) will return false but will not invalidate the internal container
        /// If there are file exceptions, will be received by the caller so should react with appropriate error control
        /// If file is successfully loaded, all previous container data is lost and replaced by data from file
        /// </summary>
        /// <param name="sFile">File name to load</param>
        /// <param name="sModelText">model data text. If it is a normal sweep file, then this comes from spectrum analyzer. If it is tracking or normalization 
        /// then this is from both signal generator and spectrum analyzer</param>
        /// <param name="sConfigurationText">configuration text. If it is a normal sweep file, then this comes from spectrum analyzer. If it is tracking or normalization 
        /// then this is from Signal Generator and some required parameters from spectrum analyzer too.</param>
        /// <returns></returns>
        public bool LoadFile(string sFile, out string sModelText, out string sConfigurationText)
        {
            sConfigurationText = "Configuration info Unknown - Old file format";
            sModelText = "Model Unknown - Old file format";

            FileStream myFile = null;

            try
            {
                myFile = new FileStream(sFile, FileMode.Open);

                using (BinaryReader binStream = new BinaryReader(myFile))
                {
                    myFile = null;

                    string sHeader = binStream.ReadString();
                    if ((sHeader != FileHeaderVersioned()) && (sHeader != FileHeaderVersioned_001()))
                    {
                        //unknown format
                        return false;
                    }

                    double fStartFrequencyMHZ = binStream.ReadDouble();
                    double fStepFrequencyMHZ = binStream.ReadDouble();
                    UInt32 nMaxDataIndex = 0;
                    if (sHeader == FileHeaderVersioned_001())
                    {
                        //in version 001 we saved a 16 bits integer
                        nMaxDataIndex = binStream.ReadUInt16();
                    }
                    else
                    {
                        nMaxDataIndex = binStream.ReadUInt32();
                    }
                    UInt16 nTotalSteps = binStream.ReadUInt16();

                    if (sHeader != FileHeaderVersioned_001())
                    {
                        sConfigurationText = "From file: " + binStream.ReadString();
                        sModelText = "From file: " + binStream.ReadString();
                    }

                    //We initialize internal data only if the file was ok and of the right format
                    CleanAll();
                    m_arrData = new RFESweepData[nMaxDataIndex];

                    for (UInt32 nSweepInd = 0; nSweepInd < nMaxDataIndex; nSweepInd++)
                    {
                        RFESweepData objRead = new RFESweepData((float)fStartFrequencyMHZ, (float)fStepFrequencyMHZ, nTotalSteps);

                        if (sHeader == FileHeaderVersioned_001())
                        {
                            objRead.CaptureTime = new DateTime(2000, 1, 1); //year 2000 means no actual valid date-time was captured
                        }
                        else
                        {
                            //Starting in version 002, load sweep capture time too
                            int nLength = (int)binStream.ReadInt32();
                            string sTime = (string)binStream.ReadString();
                            if ((sTime.Length == nLength) && (nLength > 0))
                            {
                                objRead.CaptureTime = DateTime.Parse(sTime);
                            }
                        }

                        for (UInt16 nStep = 0; nStep < nTotalSteps; nStep++)
                        {
                            objRead.SetAmplitudeDBM(nStep, (float)binStream.ReadDouble());
                        }
                        Add(objRead);
                    }
                }
            }
            finally
            {
                if (myFile != null)
                    myFile.Dispose();
            }

            return true;
        }

        public void GetTopBottomDataRange(out double dTopRangeDBM, out double dBottomRangeDBM, RFEAmplitudeTableData AmplitudeCorrection)
        {
            dTopRangeDBM = RFECommunicator.MIN_AMPLITUDE_DBM;
            dBottomRangeDBM = RFECommunicator.MAX_AMPLITUDE_DBM;

            if (m_nUpperBound <= 0)
                return;

            for (UInt32 nIndSample = 0; nIndSample < m_nUpperBound; nIndSample++)
            {
                for (UInt16 nIndStep = 0; nIndStep < m_arrData[0].TotalSteps; nIndStep++)
                {
                    double dValueDBM = m_arrData[nIndSample].GetAmplitudeDBM(nIndStep,AmplitudeCorrection,AmplitudeCorrection!=null);
                    if (dTopRangeDBM < dValueDBM)
                        dTopRangeDBM = dValueDBM;
                    if (dBottomRangeDBM > dValueDBM)
                        dBottomRangeDBM = dValueDBM;
                }
            }
        }

        public void ResizeCollection(int nSizeToAdd)
        {
            Array.Resize(ref m_arrData, m_arrData.Length + nSizeToAdd);
        }
    }
}
