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

//#define TESTMODE
//#define TEST_WATERFALL //used for T0007, this trace enables a detailed dump of waterfall data for testing

using System;
using System.Collections.Generic;
using System.Diagnostics;
using RFExplorerCommunicator;

namespace RFEWaterfallGL
{
    /// <summary>
    /// Contain sweep data and sweep time objects
    /// </summary>
    internal class WaterfallDataContainer
    {
        private List<float[]> m_listSweepData = null;           //List to draw data graph
        private List<float[]> m_listSweepData_Temp = null;      //Auxiliar list to sort data
        private List<DateTime> m_listSweepTime = null;          //List to draw time graph
        private List<DateTime> m_listSweepTime_Temp = null;     //Auxiliar list to sort time
        SharpGLForm m_objGLForm = null;

        private int m_nTotalDrawingSweeps = 0;

        internal WaterfallDataContainer(SharpGLForm objGLForm)
        {
            m_objGLForm = objGLForm;
            m_listSweepData = new List<float[]>(objGLForm.TotalDrawingSweeps);
            m_listSweepTime = new List<DateTime>(objGLForm.TotalDrawingSweeps);
            m_nTotalDrawingSweeps = objGLForm.TotalDrawingSweeps;
        }

        /// <summary>
        /// Recive a Sweep object and store data and time sweep in a corresponding temp list
        /// </summary>
        public void AddSweepInTempList(RFESweepData objSweep, bool menuUseAmplitudeCorrection)
        {
            IsTempListNull();

            float[] arrSweepData = new float[objSweep.TotalSteps];

#if TRACE && TEST_WATERFALL
            Trace.WriteLine("-- AddSweep " + objSweep.CaptureTime.ToString("HH.mm.ss.fff")  + " count:" + m_listSweepTime_Temp.Count);
#endif

            if (m_listSweepTime.Contains(objSweep.CaptureTime))
                return;

            for (UInt16 nStep = 0; nStep < objSweep.TotalSteps; nStep++)
            {
                double fAmplitudeDBM = objSweep.GetAmplitudeDBM(nStep);

                //This is for debug only -> it writes all the data points in the "Report" window. It takes a lot to print a large data set so use with care
                //double fFrequencyMHZ = objSweep.GetFrequencyMHZ(nStep);
                //ReportLog("[x,y,z]=[" +  fFrequencyMHZ + "," + fAmplitudeDBM + "," + nSourceSweepIndex + "]",false);
                //Console.WriteLine("[x,y,z]=[{0},{1},{2}]", fFrequencyMHZ, fAmplitudeDBM, nSourceSweepIndex);

#if !TESTMODE
                if (nStep >= m_objGLForm.SweepSteps)
                    return; //we do not currently support more than hardwired limit sweep steps

                if (m_objGLForm.RangeSet)
                {
                    double value = Math.Round((fAmplitudeDBM - m_objGLForm.MinAmplitude) / ((m_objGLForm.MaxAmplitude - m_objGLForm.MinAmplitude) / (double)(m_objGLForm.TotalDrawingSweeps - 1)));
                    arrSweepData[nStep] = (float)value;
                }
                else
                    throw new ArgumentOutOfRangeException("Spectrum range not set, out of range");
#endif
            }

            if (m_listSweepData.Count > m_nTotalDrawingSweeps)
            {
                m_listSweepData_Temp.Insert(0, arrSweepData);
                m_listSweepTime_Temp.Insert(0, objSweep.CaptureTime);
#if TRACE && TEST_WATERFALL
                //This is for debug only -> Time added is displayed if the list has more than 100 elements (always postion 0)
                Trace.WriteLine("TEMP inserted at 0 " + objSweep.CaptureTime.ToString("HH.mm.ss.fff") + " count:" + m_listSweepTime_Temp.Count);
#endif

                m_listSweepData_Temp.RemoveAt(m_nTotalDrawingSweeps - 1);
                m_listSweepTime_Temp.RemoveAt(m_nTotalDrawingSweeps - 1);
#if TRACE && TEST_WATERFALL
                //This is for debug only -> Time removed is displayed  if the list has more than 100 elements (always position 99)
                Trace.WriteLine("TEMP Removed at 99 " + objSweep.CaptureTime.ToString("HH.mm.ss.fff") + " count:" + m_listSweepTime_Temp.Count);
#endif
            }
            else
            {
                m_listSweepData_Temp.Add(arrSweepData);
                m_listSweepTime_Temp.Add(objSweep.CaptureTime);
#if TRACE && TEST_WATERFALL
                //This is for debug only -> Time added is displayed if the list has less than 100 elements (the position depends on the size of the list)
                Trace.WriteLine("TEMP inserted at " + (m_listSweepData_Temp.Count-1).ToString() + " " + objSweep.CaptureTime.ToString("HH.mm.ss.fff") + " count:" + m_listSweepTime_Temp.Count);
#endif
            }

            return;
        }

        /// <summary>
        /// Get amplitude Y value based on X,Z coordinates
        /// </summary>
        /// <returns>Amplitude level</returns>
        internal float GetY(int nX, int nZ)
        {
            if (nZ + 1 > m_listSweepData.Count)
                return 0.0f;
            else
                return m_listSweepData[nZ][nX];
        }

        /// <summary>
        /// Get total step of Z coordinate (time)
        /// </summary>
        /// <returns>Total number of steps</returns>
        internal UInt16 GetTotalSteps()
        {
            if (m_listSweepData.Count == 0)
                return 0;
            else
                return (UInt16)m_listSweepData[0].Length;
        }

        /// <summary>
        /// Get sweep date
        /// </summary>
        /// <returns>date</returns>
        internal string GetDateStamp(int nIndex)
        {
            if (nIndex > m_listSweepTime.Count - 1)
                return "00:00:00.000";    
            else
                return m_listSweepTime[nIndex].ToString("HH:mm:ss\\.fff");
        }

        /// <summary>
        /// Clean data and time list
        /// </summary>
        internal void CleanAll()
        {
            m_listSweepData.Clear();
            m_listSweepTime.Clear();
        }

        /// <summary>
        /// Add temp list to list which is used to draw graph
        /// </summary>
        internal void JoinTempList()
        {
            IsTempListNull();

            m_listSweepData.InsertRange(0, m_listSweepData_Temp); 
            m_listSweepTime.InsertRange(0, m_listSweepTime_Temp);

#if TRACE && TEST_WATERFALL
            //This is for debug only -> It shows list before to add temp list, temp list and amplitud level
            Trace.WriteLine("inserted at 0 : FINAL count data " + m_listSweepData.Count + " - TEMP count data " + m_listSweepData_Temp.Count); 
            Trace.WriteLine("inserted at 0 : FINAL count time " + m_listSweepTime.Count + " - TEMP count time " + m_listSweepTime_Temp.Count); 

            Trace.WriteLine("\n------------------------" + "\n List before\n" + "------------------------");
            foreach (DateTime time in m_listSweepTime)
                Trace.WriteLine("time " + m_listSweepTime.IndexOf(time) + " -> " + time.ToString("HH:mm:ss\\.fff"));
            Trace.WriteLine("------------------------");

            Trace.WriteLine("\n------------------------" + "\n Temp List \n" + "------------------------");
            foreach (DateTime time_temp in m_listSweepTime_Temp)
                Trace.WriteLine("time " + m_listSweepTime_Temp.IndexOf(time_temp) + " -> " + time_temp.ToString("HH:mm:ss\\.fff"));
            Trace.WriteLine("------------------------");
#endif

            m_listSweepData_Temp.Clear();
            m_listSweepTime_Temp.Clear();

            if (m_listSweepData.Count > m_nTotalDrawingSweeps) 
            {
                m_listSweepData.RemoveRange(m_nTotalDrawingSweeps, (m_listSweepData.Count - m_nTotalDrawingSweeps));

#if TRACE && TEST_WATERFALL
                Trace.WriteLine("FINAL removed at 99 - count data " + m_listSweepData.Count);
#endif
            }

            if (m_listSweepTime.Count > m_nTotalDrawingSweeps)
            {
                m_listSweepTime.RemoveRange(m_nTotalDrawingSweeps, (m_listSweepTime.Count - m_nTotalDrawingSweeps));

#if TRACE && TEST_WATERFALL
                Trace.WriteLine("FINAL removed at 99 - count time " + m_listSweepTime.Count + Environment.NewLine);
#endif
            }

#if TRACE && TEST_WATERFALL
            //This is for debug only -> List to draw and amplitud level are displayed
            Trace.WriteLine("\n------------------------" + "\n List after\n" + "------------------------");
            foreach (DateTime time in m_listSweepTime)
                Trace.WriteLine("Time " + m_listSweepTime.IndexOf(time) + " -> " + time.ToString("HH:mm:ss\\.fff"));
            Trace.WriteLine("------------------------");

            Trace.WriteLine("\n------------------------" + "\n Amplitud level\n" + "------------------------");
            foreach (float[] arrData in m_listSweepData)
            {
                Trace.WriteLine("Sweep " + m_listSweepData.IndexOf(arrData) + " -> " + string.Join(",", arrData));
            }
            Trace.WriteLine("------------------------");
#endif

        }

        /// <summary>
        /// Initialize temp list whether is null
        /// </summary>
        private void IsTempListNull()
        {
            if (m_listSweepData_Temp == null)
                m_listSweepData_Temp = new List<float[]>(m_nTotalDrawingSweeps);

            if (m_listSweepTime_Temp == null)
                m_listSweepTime_Temp = new List<DateTime>(m_nTotalDrawingSweeps);
        }
    }
}
