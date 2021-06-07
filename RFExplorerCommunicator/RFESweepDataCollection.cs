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
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace RFExplorerCommunicator
{
    /// <summary>
    /// Class support a full sweep of data from RF Explorer, and it is used in the RFESweepDataCollection container
    /// </summary>
    public class RFESweepData
    {
        #region Data Members & Properties
        //variable used to internall store byte array received if is used externally
        byte[] m_arrBLOB = null;
        //variable used to internall store byte array in string format received if is used externally
        string m_sBLOBString = "";
        //variable used to internall store offset dB applied to sweep data
        private float m_fOffsetDB = 0;

        protected double m_fStartFrequencyMHZ;
        /// <summary>
        /// Start frequency
        /// </summary>
        public double StartFrequencyMHZ
        {
            get { return m_fStartFrequencyMHZ; }
        }

        /// <summary>
        /// End frequency
        /// </summary>
        public double EndFrequencyMHZ
        {
            get { return GetFrequencyMHZ((UInt16)(m_nTotalDataPoints - 1)); }
        }

        protected double m_fStepFrequencyMHZ;
        /// <summary>
        /// Step frequency between each sweep step
        /// </summary>
        public double StepFrequencyMHZ
        {
            get { return m_fStepFrequencyMHZ; }
            set { m_fStepFrequencyMHZ = value; }
        }

        protected UInt16 m_nTotalDataPoints;
        /// <summary>
        /// Total number of sweep steps captured
        /// </summary>
        public UInt16 TotalSteps
        {
            get { return (UInt16)(m_nTotalDataPoints - 1); }
        }

        /// <summary>
        /// Total number of sweep data points captured (same as TotalSteps+1)
        /// </summary>
        public UInt16 TotalDataPoints
        {
            get { return m_nTotalDataPoints; }
        }

        /// <summary>
        /// The actual data container, a consecutive set of dBm amplitude values, one entry per data point
        /// </summary>
        protected float[] m_arrAmplitude;

        DateTime m_Time;
        /// <summary>
        /// The time when this data sweep was created, it should match as much as possible the real data capture
        /// </summary>
        public DateTime CaptureTime
        {
            get { return m_Time; }
            set { m_Time = value; }
        }
        #endregion

        /// <summary>
        /// Create a sweep object by default
        /// </summary>
        public RFESweepData()
        {
        }

        /// <summary>
        /// Create a sweep object with specific frequency settings and default value of amplitude
        /// </summary>
        /// <param name="StartFreqMHZ">Start frequency in MHZ</param>
        /// <param name="StepFreqMHZ">Step frequency in MHZ</param>
        /// <param name="nTotalDataPoints">Data points used in the sweep, same as (TotalSteps + 1)</param>
        public RFESweepData(double StartFreqMHZ, double StepFreqMHZ, UInt16 nTotalDataPoints)
        {
            m_Time = DateTime.Now;
            m_nTotalDataPoints = nTotalDataPoints;

            //We need truncate 3 position of decimal value to avoid acumulative issues
            m_fStartFrequencyMHZ = ((double)((int)Math.Round(StartFreqMHZ * 1000.0))) / 1000.0;
            m_fStepFrequencyMHZ = ((double)((int)Math.Round(StepFreqMHZ * 1000000.0))) / 1000000.0;   //Received data in Hz from device

            m_arrAmplitude = new float[m_nTotalDataPoints];
            for (int nDataPoint = 0; nDataPoint < m_nTotalDataPoints; nDataPoint++)
                m_arrAmplitude[nDataPoint] = RFECommunicator.MIN_AMPLITUDE_DBM - 100;
        }

        #region Public Functions
        /// <summary>
        /// This function will process a received, full consistent string received from remote device
        /// and fill it in all data
        /// </summary>
        /// <param name="sLine">string received from device, previously parsed and validated</param>
        /// <param name="fOffsetDB">currently specified offset in DB</param>
        /// <param name="bBLOB">if true the internal BLOB object will be filled in for later use in GetBLOB</param>
        /// <param name="bString">if true the internal string object will be filled in for later use in GetBLOBString</param>
        public bool ProcessReceivedString(string sLine, float fOffsetDB, bool bBLOB = false, bool bString = false)
        {
            bool bOk = true;
            //Update offset with data from offset dB or/and Input Stage.
            m_fOffsetDB = fOffsetDB;
            try
            {
                if ((sLine.Length > 2) && (sLine.StartsWith("$S")))
                {
                    if (bBLOB)
                        m_arrBLOB = new byte[TotalDataPoints];
                    if (bString)
                        m_sBLOBString = sLine.Substring(2, TotalDataPoints);
                    for (ushort nDataPoint = 0; nDataPoint < TotalDataPoints; nDataPoint++)
                    {
                        byte nVal = Convert.ToByte(sLine[2 + nDataPoint]);
                        float fVal = nVal / -2.0f;
                        if (bBLOB)
                            m_arrBLOB[nDataPoint] = nVal;
                        SetAmplitudeDBM(nDataPoint, fVal + m_fOffsetDB);
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
        /// This function will process a received partial string from remote device
        /// and fill it in all data
        /// </summary>
        /// <param name="sPartialLine"> Partial string received from device, previously parsed and validated</param>
        /// <param name="fOffsetDB">currently specified offset in DB</param>
        /// <param name="bBLOB">if true the internal BLOB object will be filled in for later use in GetBLOB</param>
        /// <param name="bString">if true the internal string object will be filled in for later use in GetBLOBString</param>
        public bool ProcessReceivedPartialString(string sPartialLine, float fOffsetDB, UInt16 nAvailableDataPoints)
        {
            bool bOk = true;
            //Update offset with data from offset dB or/and Input Stage.
            m_fOffsetDB = fOffsetDB;

            try
            {

                if ((sPartialLine.Length > 2) && (sPartialLine.StartsWith("$S")) && (!sPartialLine.Contains("\r\n")))
                {

                    if (nAvailableDataPoints < m_nTotalDataPoints)
                    {
                        for (ushort nDataPoint = 0; nDataPoint < nAvailableDataPoints; nDataPoint++)
                        {
                            byte nVal = Convert.ToByte(sPartialLine[2 + nDataPoint]);
                            float fVal = nVal / -2.0f;
                            SetAmplitudeDBM(nDataPoint, fVal + m_fOffsetDB);
                        }
                    }
                    else
                        bOk = false;
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
        /// <param name="nDataPoint">Internal frequency data point to read data from</param>
        /// <returns>Value in dBm</returns>
        public float GetAmplitudeDBM(UInt16 nDataPoint)
        {
            return GetAmplitudeDBM(nDataPoint, null, false);
        }

        /// <summary>
        /// Get the data point index closer to a specific frequency in MHZ assuming is inside the valid frequency range of the sweep
        /// </summary>
        /// <param name="fFreqMHZ">frequency in MHZ inside the valid range</param>
        /// <returns>0 based index value, or 65535 if invalid</returns>
        public UInt16 GetFrequencyDataPoint(double fFreqMHZ)
        {
            UInt16 nMinDataPoint = 65535;

            double fOffsetMHZ = fFreqMHZ - StartFrequencyMHZ;
            if (fOffsetMHZ >= 0)
            {
                nMinDataPoint = Convert.ToUInt16(Math.Round(fOffsetMHZ / StepFrequencyMHZ));
            }

            return nMinDataPoint;
        }

        /// <summary>
        /// This function add a specific dB offset to received data from analyzer. This is a total offset which is the sum of offset dB and Input Stage offset.
        /// </summary>
        /// <param name="nDBOffset">dB offset</param>
        public void AddDBOffset(int nDBOffset)
        {
            for (int nInd = 0; nInd < m_arrAmplitude.Length; nInd++)
            {
                m_arrAmplitude[nInd] += nDBOffset;
            }
            m_fOffsetDB += (float)Convert.ToDouble(nDBOffset);
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
                arrBLOB = new byte[TotalDataPoints];
                m_arrBLOB.CopyTo(arrBLOB, 0);
            }
            catch (Exception obEx)
            {
                Trace.WriteLine(obEx.ToString());
                return false;
            }

            return true;
        }
        /// <summary>
        /// If selected bString in ProcessReceivedString() then raw scan data is available here in string format
        /// </summary>
        /// <param name="bCreateIfNeeded">
        /// Produce a BLOB from standard amplitude float data. This may be required if BLOB was not captured initially but is required later
        /// </param>
        /// <returns>BLOB string if data available or empty string if not</returns>
        public string GetBLOBString(bool bCreateIfNeeded = false)
        {
            if (String.IsNullOrEmpty(m_sBLOBString) && bCreateIfNeeded)
            {
                m_sBLOBString = "";
                foreach (float fVal in m_arrAmplitude)
                {
                    try
                    {
                        byte nVal = Convert.ToByte(Math.Round(fVal * -2f));
                        m_sBLOBString += Convert.ToChar(nVal);
                    }
                    catch
                    {
                        m_sBLOBString += Convert.ToChar(240); //use -120 as the default value if something goes wrong
                    }
                }
            }
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

            for (UInt16 nDataPoint = 0; nDataPoint < TotalDataPoints; nDataPoint++)
            {
                //normal realtime
                float dDB = GetAmplitudeDBM(nDataPoint) - objNormalizedAmplitudeReference.GetAmplitudeDBM(nDataPoint);
                SetAmplitudeDBM(nDataPoint, dDB);
            }

            return true;
        }

        /// <summary>
        /// Returns amplitude data in dBm. This is the value as it was read from the device or from a file
        /// so it is not adjusted by offset or additionally compensated in any way. If the value was read from a device,
        /// it may already be an adjusted value including device configured offset.
        /// </summary>
        /// <param name="nDataPoint">Internal frequency data point to read data from</param>
        /// <param name="AmplitudeCorrection">Optional parameter, can be null. If different than null, use the amplitude correction table</param>
        /// <param name="bUseCorrection">If the AmplitudeCorrection is not null, this boolean will tell whether to use it or not</param>
        /// <returns>Value in dBm</returns>
        public float GetAmplitudeDBM(UInt16 nDataPoint, RFEAmplitudeTableData AmplitudeCorrection, bool bUseCorrection)
        {
            if (nDataPoint < m_nTotalDataPoints)
            {
                if ((AmplitudeCorrection != null) && bUseCorrection)
                {
                    return m_arrAmplitude[nDataPoint] + AmplitudeCorrection.GetAmplitudeCalibration((int)GetFrequencyMHZ(nDataPoint));
                }
                else
                {
                    return m_arrAmplitude[nDataPoint];
                }
            }
            else
                return RFECommunicator.MIN_AMPLITUDE_DBM + m_fOffsetDB;
        }

        /// <summary>
        /// Set new amplitude in dBm in specific data point
        /// </summary>
        /// <param name="nDataPoint">where set new amplitude</param>
        /// <param name="fDBM">amplitude value in dBm</param>
        public void SetAmplitudeDBM(UInt16 nDataPoint, float fDBM)
        {
            if (nDataPoint < m_nTotalDataPoints)
                m_arrAmplitude[nDataPoint] = fDBM;
        }

        /// <summary>
        /// Return frequency value according to the data point given
        /// </summary>
        /// <param name="nDataPoint">Data point which corresponds to specific frequency</param>
        /// <returns>frequency in MHz</returns>
        public double GetFrequencyMHZ(UInt16 nDataPoint)
        {
            if (nDataPoint < m_nTotalDataPoints)
                return m_fStartFrequencyMHZ + (m_fStepFrequencyMHZ * nDataPoint);
            else
                return 0.0f;
        }

        /// <summary>
        /// Return frequency span
        /// </summary>
        /// <returns>frequency span in MHz</returns>
        public double GetFrequencySpanMHZ()
        {
            return (m_fStepFrequencyMHZ * TotalSteps);
        }

        /// <summary>
        /// Returns the data point of the lowest amplitude value found
        /// </summary>
        /// <returns>data point of the lowest amplitude value found</returns>
        public UInt16 GetMinDataPoint()
        {
            UInt16 nMinDataPoint = 0;
            float fMin = RFECommunicator.MAX_AMPLITUDE_DBM + m_fOffsetDB;

            for (UInt16 nDataPoint = 0; nDataPoint < m_nTotalDataPoints; nDataPoint++)
            {
                if (fMin > m_arrAmplitude[nDataPoint])
                {
                    fMin = m_arrAmplitude[nDataPoint];
                    nMinDataPoint = nDataPoint;
                }
            }
            return nMinDataPoint;
        }

        /// <summary>
        /// Returns the data point of the highest amplitude value found
        /// </summary>
        /// <returns>data point of the highest amplitude value found</returns>
        public UInt16 GetPeakDataPoint()
        {
            UInt16 nPeakDataPoint = 0;
            float fPeak = RFECommunicator.MIN_AMPLITUDE_DBM + m_fOffsetDB;

            for (UInt16 nDataPoint = 0; nDataPoint < m_nTotalDataPoints; nDataPoint++)
            {
                if (fPeak < m_arrAmplitude[nDataPoint])
                {
                    fPeak = m_arrAmplitude[nDataPoint];
                    nPeakDataPoint = nDataPoint;
                }
            }
            return nPeakDataPoint;
        }

        /// <summary>
        /// Compare the configuration of the two sweep data object
        /// </summary>
        /// <param name="objOther">sweep data object to compare its configuration</param>
        /// <returns>true if it is the same configuration, otherwise false</returns>
        public bool IsSameConfiguration(RFESweepData objOther)
        {
            return (Math.Abs(objOther.StartFrequencyMHZ - StartFrequencyMHZ) < 0.001 && Math.Abs(objOther.StepFrequencyMHZ - StepFrequencyMHZ) < 0.001 && (objOther.TotalSteps == TotalSteps));
        }

        /// <summary>
        /// Makes an exact copy of the sweep data object
        /// </summary>
        /// <returns>duplicate sweep data object</returns>
        public RFESweepData Duplicate()
        {
            RFESweepData objSweep = new RFESweepData(m_fStartFrequencyMHZ, m_fStepFrequencyMHZ, m_nTotalDataPoints);

            Array.Copy(m_arrAmplitude, objSweep.m_arrAmplitude, m_nTotalDataPoints);

            return objSweep;
        }

        /// <summary>
        /// Returns power channel over the full span being captured. The power is instantaneous real time
        /// For average power channel use the collection method GetAverageChannelPower().
        /// </summary>
        /// <returns>channel power in dBm/span</returns>
        public double GetChannelPowerDBM()
        {
            double fChannelPower = RFECommunicator.MIN_AMPLITUDE_DBM + m_fOffsetDB;
            double fPowerTemp = 0.0f;

            for (UInt16 nDataPoint = 0; nDataPoint < m_nTotalDataPoints; nDataPoint++)
            {
                fPowerTemp += RFECommunicator.Convert_dBm_2_mW(m_arrAmplitude[nDataPoint]);
            }

            if (fPowerTemp > 0.0f)
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
            sResult = "Sweep data " + m_fStartFrequencyMHZ.ToString("f3") + "MHz " + m_fStepFrequencyMHZ.ToString("f3") + "MHz " + m_nTotalDataPoints + "DataPoints: ";

            try
            {
                for (UInt16 nDataPoint = 0; nDataPoint < TotalDataPoints; nDataPoint++)
                {
                    if (nDataPoint > 0)
                    {
                        sResult += ",";
                    }
                    if ((nDataPoint % 16) == 0)
                    {
                        sResult += Environment.NewLine;
                    }
                    sResult += GetAmplitudeDBM(nDataPoint).ToString("00.0");
                }
            }
            catch { }

            return sResult;
        }

        /// <summary>
        /// Save a CSV file using one frequency point/dBm value per line
        /// </summary>
        /// <param name="sFilename">full path filename</param>
        /// <param name="cCSVDelimiter">comma delimiter to use</param>
        /// <param name="AmplitudeCorrection"></param>
        public void SaveFileCSV(string sFilename, char cCSVDelimiter, RFEAmplitudeTableData AmplitudeCorrection)
        {
            using (StreamWriter myFile = new StreamWriter(sFilename, false))
            {
                for (UInt16 nDataPoint = 0; nDataPoint < TotalDataPoints; nDataPoint++)
                {
                    myFile.Write(GetFrequencyMHZ(nDataPoint).ToString("f3"));
                    myFile.Write(cCSVDelimiter);
                    myFile.Write(GetAmplitudeDBM(nDataPoint, AmplitudeCorrection, AmplitudeCorrection != null).ToString("f1"));
                    myFile.Write(Environment.NewLine);
                }
            }
        }

        public void SaveFileXML(string sFilename, char cCSVDelimiter, RFEAmplitudeTableData AmplitudeCorrection)
        {
            //TODO: create a self-contained structure, including all member variables, and a list of all amplitude values in a single element <ScanData>y1,y2,...</ScanData>
        }

        public void CleanSweepData(UInt16 nTotalDataPoints)
        {

            for (int nDataPoint = 0; nDataPoint < nTotalDataPoints; nDataPoint++)
                m_arrAmplitude[nDataPoint] = RFECommunicator.MIN_AMPLITUDE_DBM - 100;
        }
        #endregion
    }

    /// <summary>
    /// A collection of RFESweepData objects, each one with independent Sweep configuration and data points
    /// </summary>
    public class RFESweepDataCollection
    {
        #region Constants & Data Members
        private const string _RFEGEN_FILE_MODEL_Mark = "[*]RFEGen:";
        public const int MAX_ELEMENTS = (10 * 1000 * 1000);    //This is the absolute max size that can be allocated
        const byte FILE_VERSION = 5;         //File format constant indicates the latest known and supported file format
        RFESweepData[] m_arrData;            //Collection of available spectrum data items
        RFESweepData m_MaxHoldData = null;   //Single data set, defined for the whole collection and updated with Add, to keep the Max Hold values
        public RFESweepData MaxHoldData
        {
            get { return m_MaxHoldData; }
        }

        int m_nUpperBound = -1;              //Max value for index with available data
        UInt32 m_nInitialCollectionSize = 0;

        bool m_bAutogrow;                   //true if the array bounds may grow up to MAX_ELEMENTS, otherwise will be limited to initial collection size


        private RFEFileDataType m_eRFEDataType;
        public RFEFileDataType RFEDataType
        {
            get { return m_eRFEDataType; }
            set { m_eRFEDataType = value; }
        }
        /// <summary>
        /// RF Explorer file formats
        /// </summary>
        public enum RFEFileDataType
        {
            Normalization,
            Spectrum_analyzer
        };

        #endregion

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

        #region Public Functions
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
        /// Return the data pointed by the zero-starting index
        /// </summary>
        /// <param name="nIndex">Indix to get sweep data object</param>
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
                    m_MaxHoldData = new RFESweepData(SweepData.StartFrequencyMHZ, SweepData.StepFrequencyMHZ, SweepData.TotalDataPoints);
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

                for (UInt16 nDataPoint = 0; nDataPoint < SweepData.TotalDataPoints; nDataPoint++)
                {
                    if (SweepData.GetAmplitudeDBM(nDataPoint, null, false) > m_MaxHoldData.GetAmplitudeDBM(nDataPoint, null, false))
                    {
                        m_MaxHoldData.SetAmplitudeDBM(nDataPoint, SweepData.GetAmplitudeDBM(nDataPoint, null, false));
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes all data older than nHours
        /// </summary>
        /// <param name="nHours">number of hours previous to keep, for instance =2 will delete whatever is older than 2hs</param>
        public void CleanPrevHours(int nHours)
        {
            int nIndexHours = (int)GetFirstSweepByTimeDifference(new TimeSpan(nHours, 1, 0)); //use the time +1 minute
            if (nIndexHours > 0 && nIndexHours < m_nUpperBound && m_arrData != null)
            {
                for (int nIndTarget = nIndexHours, nIndSource = 0; nIndTarget <= m_nUpperBound; nIndTarget++, nIndSource++)
                {
                    m_arrData[nIndSource] = m_arrData[nIndTarget];
                }
                m_nUpperBound -= nIndexHours;
            }
        }

        public void CleanAll()
        {
            m_arrData = new RFESweepData[m_nInitialCollectionSize];
            m_MaxHoldData = null;
            m_nUpperBound = -1;
        }

        public string Dump()
        {
            string sDump = "";
            foreach (RFESweepData objSweep in m_arrData)
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
                objReturn = new RFESweepData(m_arrData[nEnd].StartFrequencyMHZ, m_arrData[nEnd].StepFrequencyMHZ, m_arrData[nEnd].TotalDataPoints);

                for (UInt16 nDataPoint = 0; nDataPoint < objReturn.TotalDataPoints; nDataPoint++)
                {
                    //sDebugText += "[" + nSweepInd + "]:";
                    float fSweepValue = 0f;
                    float[] arrSweepValues = new float[nTotalIterations];

                    for (UInt32 nIterationInd = nStart; nIterationInd <= nEnd; nIterationInd++)
                    {
                        if (nDataPoint == 0)
                        {
                            //check all the sweeps use the same configuration, but only in first loop to reduce overhead
                            if (!m_arrData[nIterationInd].IsSameConfiguration(objReturn))
                                return null;
                        }
                        arrSweepValues[nIterationInd - nStart] = m_arrData[nIterationInd].GetAmplitudeDBM(nDataPoint, null, false);
                        //sDebugText += m_arrData[nIterationInd].GetAmplitudeDBM(nSweepInd).ToString("f2") + ",";
                    }
                    Array.Sort(arrSweepValues);
                    fSweepValue = arrSweepValues[nTotalIterations / 2];
                    //sDebugText += "(" + fSweepValue.ToString("f2") + ")";
                    objReturn.SetAmplitudeDBM(nDataPoint, fSweepValue);
                }
            }
            catch
            {
                objReturn = null;
            }
            return objReturn;
        }

        /// <summary>
        /// Given a time difference with current time expressed in objTimeSpan, it finds the first item in the collection with time same or newer that fits in objTimeSpan;
        /// </summary>
        /// <returns>The value of the first sweep within the time span, or 0 if not found or captured sweeps are not enough</returns>
        public UInt32 GetFirstSweepByTimeDifference(TimeSpan objTimeSpan)
        {
            if (m_nUpperBound < 0)
                return 0;

            UInt32 nItem = 0;

            DateTime objFirstTime = DateTime.Now - objTimeSpan;
            //Console.WriteLine("GetFirstSweepByTimeDifference: " + objFirstTime.ToString("yyyy-MM-dd HH:mm:ss") + " , " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " , " + objTimeSpan.ToString());

            for (int nInd = 0; nInd <= m_nUpperBound; nInd++)
            {
                if (objFirstTime < m_arrData[nInd].CaptureTime)
                {
                    //Console.WriteLine(" - Found index " + nInd);
                    nItem = (UInt32)nInd;
                    break;
                }
            }

            return nItem;
        }

        /// <summary>
        /// Calculates the average value for a group of sweeps
        /// </summary>
        /// <param name="nStart">First sweep inclusive to include in calculations</param>
        /// <param name="nEnd">Last sweep inclusive</param>
        /// <returns>null if invalid arguments or no data avialable, the avg sweep object otherwise</returns>
        public RFESweepData GetAverage(UInt32 nStart, UInt32 nEnd)
        {
            RFESweepData objReturn = null;

            if (m_nUpperBound < 0 || nStart > m_nUpperBound || nStart > nEnd)
            {
                return null;
            }

            //string sDebugText = "";

            if (nEnd > m_nUpperBound)
                nEnd = (UInt32)m_nUpperBound;

            try
            {
                objReturn = new RFESweepData(m_arrData[nEnd].StartFrequencyMHZ, m_arrData[nEnd].StepFrequencyMHZ, m_arrData[nEnd].TotalDataPoints);

                for (UInt16 nDataPoint = 0; nDataPoint < objReturn.TotalDataPoints; nDataPoint++)
                {
                    //sDebugText += "[" + nSweepInd + "]:";
                    float fSweepValue = 0f;
                    for (UInt32 nIterationInd = nStart; nIterationInd <= nEnd; nIterationInd++)
                    {
                        if (nDataPoint == 0)
                        {
                            //check all the sweeps use the same configuration, but only in first loop to reduce overhead
                            if (!m_arrData[nIterationInd].IsSameConfiguration(objReturn))
                                return null;
                        }

                        fSweepValue += m_arrData[nIterationInd].GetAmplitudeDBM(nDataPoint, null, false);
                        //sDebugText += m_arrData[nIterationInd].GetAmplitudeDBM(nSweepInd).ToString("f2") + ",";
                    }
                    fSweepValue = fSweepValue / (nEnd - nStart + 1);
                    //sDebugText += "(" + fSweepValue.ToString("f2") + ")";
                    objReturn.SetAmplitudeDBM(nDataPoint, fSweepValue);
                }
            }
            catch
            {
                objReturn = null;
            }
            return objReturn;
        }


        /// <summary>
        /// Calculates the absolute max value for a group of sweeps
        /// </summary>
        /// <param name="nStart">First sweep inclusive to include in calculations</param>
        /// <param name="nEnd">Last sweep inclusive</param>
        /// <returns>null if invalid arguments or no data avialable, the Max sweep object otherwise</returns>
        public RFESweepData GetMax(UInt32 nStart, UInt32 nEnd)
        {
            RFESweepData objMaxSweep = null;

            if (m_nUpperBound < 0 || nStart > m_nUpperBound || nStart > nEnd)
            {
                return null;
            }

            //string sDebugText = "";

            if (nEnd > m_nUpperBound)
                nEnd = (UInt32)m_nUpperBound;

            try
            {
                objMaxSweep = new RFESweepData(m_arrData[nEnd].StartFrequencyMHZ, m_arrData[nEnd].StepFrequencyMHZ, m_arrData[nEnd].TotalDataPoints);

                for (UInt16 nSweepInd = 0; nSweepInd < objMaxSweep.TotalDataPoints; nSweepInd++)    //Calculate from 0 to TotalDataPoints-1
                {
                    //sDebugText += "[" + nSweepInd + "]:";
                    float fSweepValue = RFECommunicator.MIN_AMPLITUDE_DBM;
                    for (UInt32 nIterationInd = nStart; nIterationInd <= nEnd; nIterationInd++)
                    {
                        if (nSweepInd == 0)
                        {
                            //check all the sweeps use the same configuration, but only in first loop to reduce overhead
                            if (!m_arrData[nIterationInd].IsSameConfiguration(objMaxSweep))
                                return null;
                        }

                        float fLocalValue = m_arrData[nIterationInd].GetAmplitudeDBM(nSweepInd, null, false);
                        if (fSweepValue < fLocalValue)
                            fSweepValue = fLocalValue;
                        //sDebugText += m_arrData[nIterationInd].GetAmplitudeDBM(nSweepInd).ToString("f2") + ",";
                    }
                    //sDebugText += "(" + fSweepValue.ToString("f2") + ")";
                    objMaxSweep.SetAmplitudeDBM(nSweepInd, fSweepValue);
                }
            }
            catch
            {
                objMaxSweep = null;
            }
            return objMaxSweep;
        }
        /// <summary>
        /// Will write large, complex, multi-sweep CSV file
        /// </summary>
        /// <param name="sFilename"></param>
        /// <param name="cCSVDelimiter"></param>
        public void SaveFileCSV(string sFilename, char cCSVDelimiter, RFEAmplitudeTableData AmplitudeCorrection)
        {
            if (m_nUpperBound < 0)
            {
                return;
            }

            RFESweepData objFirst = m_arrData[0];

            using (StreamWriter myFile = new StreamWriter(sFilename, false))
            {
                myFile.WriteLine("RF Explorer CSV data file: " + FileHeaderVersioned());
                myFile.WriteLine("Start Frequency: " + objFirst.StartFrequencyMHZ.ToString() + "MHZ" + Environment.NewLine +
                    "Step Frequency: " + (objFirst.StepFrequencyMHZ * 1000).ToString() + "KHZ" + Environment.NewLine +
                    "Total data entries: " + (m_nUpperBound + 1).ToString() + Environment.NewLine +
                    "Data points per entry: " + objFirst.TotalDataPoints.ToString());

                string sHeader = "Sweep" + cCSVDelimiter + "Date" + cCSVDelimiter + "Time" + cCSVDelimiter + "Milliseconds";

                for (UInt16 nDataPoint = 0; nDataPoint < objFirst.TotalDataPoints; nDataPoint++)
                {
                    double dFrequency = objFirst.StartFrequencyMHZ + nDataPoint * (objFirst.StepFrequencyMHZ);
                    sHeader += cCSVDelimiter + dFrequency.ToString("0000.000");
                }

                myFile.WriteLine(sHeader);

                for (int nSweepInd = 0; nSweepInd <= m_nUpperBound; nSweepInd++)
                {
                    myFile.Write(nSweepInd.ToString() + cCSVDelimiter);

                    myFile.Write(m_arrData[nSweepInd].CaptureTime.ToShortDateString() + cCSVDelimiter +
                        m_arrData[nSweepInd].CaptureTime.ToString("HH:mm:ss") + cCSVDelimiter +
                        m_arrData[nSweepInd].CaptureTime.ToString("\\.fff") + cCSVDelimiter);

                    if (!m_arrData[nSweepInd].IsSameConfiguration(objFirst))
                        break;

                    for (UInt16 nDataPoint = 0; nDataPoint < objFirst.TotalDataPoints; nDataPoint++)
                    {
                        myFile.Write(m_arrData[nSweepInd].GetAmplitudeDBM(nDataPoint, AmplitudeCorrection, AmplitudeCorrection != null));
                        if (nDataPoint != (objFirst.TotalDataPoints - 1))
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
        /// <returns> False if there is no data in file</returns>
        public bool SaveFile(string sFilename, string sModelText, string sConfigurationText, RFEAmplitudeTableData AmplitudeCorrection)
        {
            bool bSaveFileOk = false;
            if (m_nUpperBound < 0)
            {
                return bSaveFileOk;
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

                    binStream.Write((UInt16)objFirst.TotalDataPoints);
                    binStream.Write((string)sConfigurationText);
                    binStream.Write((string)sModelText);

                    var memoryStream = new MemoryStream();
                    using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                    {
                        int nUncompressedSize = 0;
                        using (BinaryWriter ZipStream = new BinaryWriter(gZipStream))
                        {
                            //Save all the sweeps consecutively
                            for (int nSweepInd = 0; nSweepInd <= m_nUpperBound; nSweepInd++)
                            {
                                if (!m_arrData[nSweepInd].IsSameConfiguration(objFirst))
                                    break;

                                //new in v002 - save date/time for each captured sweep
                                string sTime = m_arrData[nSweepInd].CaptureTime.ToString("o");
                                ZipStream.Write((Int32)sTime.Length);
                                byte[] arrText = Encoding.ASCII.GetBytes(sTime); //From v003 we encode string to byte for Date/time data
                                ZipStream.Write(arrText, 0, arrText.Length);
                                nUncompressedSize += arrText.Length + sizeof(System.Int32);

                                nTotalSweepsActuallySaved++;
                                for (UInt16 nDataPoint = 0; nDataPoint < objFirst.TotalDataPoints; nDataPoint++)
                                {
                                    ZipStream.Write((double)m_arrData[nSweepInd].GetAmplitudeDBM(nDataPoint, AmplitudeCorrection, AmplitudeCorrection != null));
                                }
                                nUncompressedSize += sizeof(System.Double) * objFirst.TotalDataPoints;
                            }
                        }
                        memoryStream.Position = 0;

                        byte[] arrCompressedBuffer = new byte[memoryStream.Length];
                        memoryStream.Read(arrCompressedBuffer, 0, arrCompressedBuffer.Length);

                        binStream.Write(nUncompressedSize);
                        binStream.Write(arrCompressedBuffer.Length);
                        binStream.Write(arrCompressedBuffer, 0, arrCompressedBuffer.Length);
                    }

                    //Save file fields again (will overwrite old ones), just to make sure nTotalSweepsActuallySaved is properly saved with actual value used
                    myFile.Seek(0, SeekOrigin.Begin);
                    binStream.Write((string)FileHeaderVersioned());
                    binStream.Write((double)objFirst.StartFrequencyMHZ);
                    binStream.Write((double)objFirst.StepFrequencyMHZ);
                    binStream.Write((Int32)nTotalSweepsActuallySaved);
                    bSaveFileOk = true;
                }
            }
            finally
            {
                if (myFile != null)
                    myFile.Dispose();
            }
            return bSaveFileOk;
        }

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
                    if ((sHeader != FileHeaderVersioned()) && (sHeader != FileHeaderVersioned_001() && sHeader != FileHeaderVersioned_002()
                        && sHeader != FileHeaderVersioned_003() && sHeader != FileHeaderVersioned_004()))
                    {
                        //unknown format
                        return false;
                    }

                    double fStartFrequencyMHZ = binStream.ReadDouble();
                    double fStepFrequencyMHZ = binStream.ReadDouble(); //For normalization file older than v004, we do not use step but start/stop so this variable has not effect
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

                    UInt16 nTotalDataPoints = binStream.ReadUInt16();

                    if (sHeader != FileHeaderVersioned_001())
                    {
                        sConfigurationText = "From file: " + binStream.ReadString();
                        sModelText = "From file: " + binStream.ReadString();

                        if (sHeader == FileHeaderVersioned_002() || sHeader == FileHeaderVersioned_003())
                        {
                            //Configuration string previous v004 has [*]RFEGen mark, so it is necessary remove it.
                            int nIndRFEGen = sModelText.IndexOf(_RFEGEN_FILE_MODEL_Mark);
                            sModelText = sModelText.Substring(nIndRFEGen + 1 + _RFEGEN_FILE_MODEL_Mark.Length);
                            nIndRFEGen = sConfigurationText.IndexOf(_RFEGEN_FILE_MODEL_Mark);
                            sConfigurationText = sConfigurationText.Substring(nIndRFEGen + 1 + _RFEGEN_FILE_MODEL_Mark.Length);
                        }
                    }

                    if ((sHeader == FileHeaderVersioned_001() || sHeader == FileHeaderVersioned_002()
                        || sHeader == FileHeaderVersioned_003() || sHeader == FileHeaderVersioned_004()) && (sConfigurationText.Contains(",")))
                    {
                        //From format v005, we always use "en-US" settings. User should create new file.
                        return false;
                    }

                    //We initialize internal data only if the file was ok and of the right format
                    CleanAll();
                    m_arrData = new RFESweepData[nMaxDataIndex];

                    using (MemoryStream StreamDecompressed = new MemoryStream())
                    {
                        int nUncompressedSize = 0; //total bytes after unzip file, or 0 if was never compressed
                        BinaryReader objBinReader = binStream; //by default use file stream, but may change to memory if comes compressed
                        byte[] arrUncompressedBuffer = null;
                        if ((sHeader != FileHeaderVersioned_001()) && (sHeader != FileHeaderVersioned_002()))
                        {
                            nUncompressedSize = (int)binStream.ReadInt32();
                            int nCompressedSize = (int)binStream.ReadInt32();
                            arrUncompressedBuffer = new byte[nUncompressedSize];

                            using (MemoryStream ms = new MemoryStream())
                            {
                                //if we are in version 3 or higher, data comes compressed, so we have to decompress first
                                byte[] gzBuffer = binStream.ReadBytes(nCompressedSize);
                                ms.Write(gzBuffer, 0, nCompressedSize);

                                ms.Position = 0;
                                using (GZipStream zip = new GZipStream(ms, CompressionMode.Decompress))
                                {
                                    zip.Read(arrUncompressedBuffer, 0, nUncompressedSize);
                                }
                                StreamDecompressed.Write(arrUncompressedBuffer, 0, nUncompressedSize);
                                StreamDecompressed.Position = 0;
                                objBinReader = new BinaryReader(StreamDecompressed);
                            }
                        }
                        //recreate all sweep data objects
                        for (UInt32 nSweepInd = 0; nSweepInd < nMaxDataIndex; nSweepInd++)
                        {
                            //Versions older than v004 need to add data point extra
                            UInt16 nTotalDataPointsFile = nTotalDataPoints;
                            if (m_eRFEDataType == RFEFileDataType.Normalization)
                            {
                                if (sHeader == FileHeaderVersioned_001() || sHeader == FileHeaderVersioned_002() || sHeader == FileHeaderVersioned_003())
                                {
                                    nTotalDataPoints++;
                                }
                            }
                            RFESweepData objRead = new RFESweepData((float)fStartFrequencyMHZ, (float)fStepFrequencyMHZ, nTotalDataPoints);

                            if (sHeader == FileHeaderVersioned_001())
                            {
                                objRead.CaptureTime = new DateTime(2000, 1, 1); //year 2000 means no actual valid date-time was captured
                            }
                            else
                            {
                                //Starting in version 002, load sweep capture time too
                                int nLength = (int)objBinReader.ReadInt32();
                                string sTime = "";
                                if ((sHeader == FileHeaderVersioned_001()) || (sHeader == FileHeaderVersioned_002()))
                                {
                                    sTime = (string)objBinReader.ReadString();
                                }
                                else
                                {
                                    //From v003 we need to decode byte to string for Date/time data
                                    byte[] arrText = objBinReader.ReadBytes(nLength);
                                    sTime = Encoding.ASCII.GetString(arrText);
                                }
                                if ((sTime.Length == nLength) && (nLength > 0))
                                {
                                    objRead.CaptureTime = DateTime.Parse(sTime);
                                }
                            }
                            float fLastPointValue = 0f;
                            for (UInt16 nDataPoint = 0; nDataPoint < nTotalDataPointsFile; nDataPoint++)
                            {
                                fLastPointValue = (float)objBinReader.ReadDouble();
                                objRead.SetAmplitudeDBM(nDataPoint, fLastPointValue);
                            }
                            if (m_eRFEDataType == RFEFileDataType.Normalization)
                            {
                                //Starting in v004. Fix the last point issue, we will add one point extra with the same amplitude value than previous one.
                                if (sHeader == FileHeaderVersioned_001() || sHeader == FileHeaderVersioned_002() || sHeader == FileHeaderVersioned_003())
                                {
                                    objRead.SetAmplitudeDBM((UInt16)(nTotalDataPoints - 1), fLastPointValue);
                                }
                            }
                            Add(objRead);
                        }
                        if (objBinReader != binStream)
                            objBinReader.Dispose();
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
            dTopRangeDBM = RFECommunicator.MIN_AMPLITUDE_DBM - 100f;
            dBottomRangeDBM = RFECommunicator.MAX_AMPLITUDE_DBM + 100f;

            //Modified <= condition due to when only one sweep data is available, m_nUpperBound = 0 and the below calculation is not done
            //Changed loop condition to if (m_nUpperBound < 0) to get amplitude limits for one sweep, if it fails 
            if (m_nUpperBound < 0)
                return;

            for (UInt32 nIndSample = 0; nIndSample <= m_nUpperBound; nIndSample++)
            {
                for (UInt16 nIndDataPoint = 0; nIndDataPoint < m_arrData[0].TotalDataPoints; nIndDataPoint++)
                {
                    double dValueDBM = m_arrData[nIndSample].GetAmplitudeDBM(nIndDataPoint, AmplitudeCorrection, AmplitudeCorrection != null);
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
        #endregion

        #region Private Functions
        private string FileHeaderVersioned_001()
        {
            return "RFExplorer PC Client - Format v001";
        }

        private string FileHeaderVersioned_002()
        {
            return "RFExplorer PC Client - Format v002";
        }

        private string FileHeaderVersioned_003()
        {
            return "RFExplorer PC Client - Format v003";
        }

        private string FileHeaderVersioned_004()
        {
            return "RFExplorer PC Client - Format v004";
        }

        private string FileHeaderVersioned()
        {
            return "RFExplorer PC Client - Format v" + FILE_VERSION.ToString("D3");
        }

        void CompressMemoryBuffer(ref byte[] arrMemory, out byte[] gZipBuffer)
        {
            byte[] buffer = arrMemory;
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;

            var compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
        }
        #endregion
    }

    /// <summary>
    /// Class support a partial sweep of data from RF Explorer, and it is used for display data when is received in each moment
    /// </summary>
    public class RFESweepDataPartial : RFESweepData
    {
        private const int MAX_AMPLITUDE_ELEMENTS = 65535;
        private UInt16 m_nAvailableDataPoints;
        /// <summary>
        /// Store the actual number of points received in a partial sweep. 
        /// </summary>
        public ushort AvailableDataPoints
        {
            get
            {
                return m_nAvailableDataPoints;
            }

            set
            {
                m_nAvailableDataPoints = value;
            }
        }

        public RFESweepDataPartial()
        {
            m_arrAmplitude = new float[MAX_AMPLITUDE_ELEMENTS];
            for (int nDataPoint = 0; nDataPoint < MAX_AMPLITUDE_ELEMENTS; nDataPoint++)
                m_arrAmplitude[nDataPoint] = RFECommunicator.MIN_AMPLITUDE_DBM - 100;
        }

        /// <summary>
        /// Clean and update sweep configuration
        /// </summary>
        /// <param name="fStartFreq">New start frequency recived</param>
        /// <param name="fStepFreq">New Step frequency received</param>
        /// <param name="nTotalPoints">Expected total data points</param>
        public void SetNewConfiguration(double fStartFreq, double fStepFreq, UInt16 nTotalPoints)
        {
            CleanSweepData(nTotalPoints);
            m_fStartFrequencyMHZ = fStartFreq;
            m_fStepFrequencyMHZ = fStepFreq;
            m_nTotalDataPoints = nTotalPoints;

        }
        /// <summary>
        /// Check max amplitude values and add it to MaxHold amplitude array
        /// </summary>
        /// <param name="objSweep">Source Partial sweep</param>
        /// <returns></returns>
        public bool AddMaxHoldData(RFESweepDataPartial objSweep)
        {
            try
            {
                for (UInt16 nDataPoint = 0; nDataPoint < objSweep.AvailableDataPoints; nDataPoint++)
                {
                    if (objSweep.GetAmplitudeDBM(nDataPoint, null, false) > GetAmplitudeDBM(nDataPoint, null, false))
                    {
                        SetAmplitudeDBM(nDataPoint, objSweep.GetAmplitudeDBM(nDataPoint, null, false));
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
