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
    /// Class support a single LCD screen data dump. Currently supports 128x8 case only
    /// </summary>
    public class RFEScreenData
    {
        /// <summary>
        /// The time when this data was created, it should match as much as possible the real data capture
        /// </summary>
        DateTime m_Time;
        public DateTime CaptureTime
        {
            get { return m_Time; }
            set { m_Time = value; }
        }

        /// <summary>
        /// RF Explorer module enabled when this screen was captured
        /// </summary>
        RFECommunicator.eModel m_eModel;
        public RFECommunicator.eModel Model
        {
            get { return m_eModel; }
            set { m_eModel = value; }
        }

        /// <summary>
        /// Data for a single screen to draw
        /// </summary>
        public byte[] m_arrRemoteScreenData;
        public byte GetByte(int nIndex)
        {
            return m_arrRemoteScreenData[nIndex];
        }

        void CreateScreen(int nBytesX, int nBytesY)
        {
            m_arrRemoteScreenData = new byte[nBytesX * nBytesY];
            m_arrRemoteScreenData.Initialize();
            for (int nInd = 0; nInd < m_arrRemoteScreenData.Length; nInd++)
            {
                m_arrRemoteScreenData[nInd] = 0;
            }
        }

        /// <summary>
        /// Initialize a monochrome 128x64 LCD of the size pointed by the sLine text received
        /// </summary>
        /// <param name="sLine">Text as received from the device realtime data</param>
        public bool ProcessReceivedString(string sLine)
        {
            bool bOk = true;

            try
            {
                m_Time = DateTime.Now;
                Model = RFECommunicator.eModel.MODEL_NONE;
                //Capture only if we are inside bounds
                int nTotalSize = sLine.Length - 2; //we discard the first two chars as they are $D
                if (nTotalSize >= 128 * 8)
                {
                    CreateScreen(128, 8);
                    for (int nInd = 0; nInd < nTotalSize; nInd++)
                    {
                        m_arrRemoteScreenData[nInd] = Convert.ToByte(sLine[nInd + 2]);
                    }
                }
            }
            catch (Exception)
            {
                bOk = false;
            }

            return bOk;
        }

        /// <summary>
        /// Initialize a monochrome LCD with standard X * Y size, in bytes. For instance a 128x64 LCD has x=128 bytes and y=(64/8)=8 bytes
        /// </summary>
        public RFEScreenData(int nBytesX, int nBytesY)
        {
            m_Time = DateTime.Now;
            Model = RFECommunicator.eModel.MODEL_NONE;
            CreateScreen(nBytesX, nBytesY);
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public RFEScreenData()
        {
            m_Time = DateTime.Now;
            Model = RFECommunicator.eModel.MODEL_NONE;
        }
    }

    /// <summary>
    /// A collection of consecutive RFEScreenData screens
    /// </summary>
    public class RFEScreenDataCollection
    {
        public const UInt16 MAX_ELEMENTS = (UInt16)64000;    //This is the absolute max size that can be allocated
        RFEScreenData[] m_arrScreenContainer;   //collection of screens
        const byte FILE_VERSION = 2;         //File format constant indicates the latest known and supported file format
        UInt16 m_nUpperBound = 0;            //Max value for index with available data

        void InitializeCollection()
        {
            m_arrScreenContainer = new RFEScreenData[MAX_ELEMENTS];
            RFEScreenData objFirst = new RFEScreenData(128, 8);
            Add(objFirst);
            m_nUpperBound = 0;
        }

        public RFEScreenDataCollection()
        {
            InitializeCollection();
        }

        /// <summary>
        /// Returns the total of elements with actual data allocated.
        /// </summary>
        public UInt16 Count
        {
            get { return ((UInt16)(m_nUpperBound + 1)); }
        }

        /// <summary>
        /// Returns the highest valid index of elements with actual data allocated.
        /// </summary>
        public UInt16 UpperBound
        {
            get {
                if (m_nUpperBound > MAX_ELEMENTS)
                    m_nUpperBound = 0;
                return m_nUpperBound; }
        }

        /// <summary>
        /// Return the data pointed by the zero-starting index
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns>returns null if no data is available with this index</returns>
        public RFEScreenData GetData(UInt16 nIndex)
        {
            if (nIndex <= m_nUpperBound)
            {
                return m_arrScreenContainer[nIndex];
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

        public void ResizeCollection(int nSizeToAdd)
        {
            Array.Resize(ref m_arrScreenContainer, m_arrScreenContainer.Length + nSizeToAdd);
        }

        public void Add(RFEScreenData ScreenData)
        {
            if (IsFull())
                return;

            m_nUpperBound++;
            m_arrScreenContainer[m_nUpperBound] = ScreenData;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.GC.Collect")]
        internal void CleanAll()
        {
            Array.Clear(m_arrScreenContainer, 0, m_arrScreenContainer.Length);
            InitializeCollection();
            GC.Collect(); //This is the right time to force a GC, just because the file load is a comparatively slow operation and the container changed
        }

        private string FileHeaderVersioned()
        {
            return "RF Explorer RFS screen file: RFExplorer PC Client - Format v" + FILE_VERSION.ToString("D3");
        }

        private string FileHeaderVersioned_001()
        {
            return "RF Explorer RFS screen file: RFExplorer PC Client - Format v001";
        }

        /// <summary>
        /// Will load a RFS standard file from disk. If the file format is incorrect (unknown) will return false but will not invalidate the internal container
        /// If there are file exceptions, will be received by the caller so should react with appropriate error control
        /// If file is successfully loaded, all previous container data is lost and replaced by data from file
        /// </summary>
        /// <param name="sFile"></param>
        /// <returns></returns>
        public bool LoadFile(string sFile)
        {
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
                        return false;
                    }

                    CleanAll();
                    m_nUpperBound = binStream.ReadUInt16();

                    //We start at page 1, page 0 is always null
                    for (UInt16 nPageInd = 1; nPageInd <= m_nUpperBound; nPageInd++)
                    {
                        binStream.ReadUInt16(); //page number, can be ignored here
                        RFEScreenData objScreen = new RFEScreenData(128, 8);
                        m_arrScreenContainer[nPageInd] = objScreen;

                        if (sHeader == FileHeaderVersioned_001())
                        {
                            objScreen.CaptureTime = new DateTime(2000, 1, 1); //year 2000 means no actual valid date-time was captured
                        }
                        else
                        {
                            //Starting in version 002, load sweep capture time too
                            int nLength = (int)binStream.ReadInt32();
                            string sTime = (string)binStream.ReadString();
                            if ((sTime.Length == nLength) && (nLength > 0))
                            {
                                objScreen.CaptureTime = DateTime.Parse(sTime);
                            }
                            objScreen.Model = (RFECommunicator.eModel)binStream.ReadByte();
                        }

                        for (int nIndY = 0; nIndY < 8; nIndY++)
                        {
                            for (int nIndX = 0; nIndX < 128; nIndX++)
                            {
                                byte nData = binStream.ReadByte();
                                objScreen.m_arrRemoteScreenData[nIndX + 128 * nIndY] = nData;
                            }
                        }
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

        /// <summary>
        /// Saves a file in RFS standard format. Note it will not handle exceptions so the calling application can deal with GUI details
        /// </summary>
        /// <param name="sFilename"></param>
        public bool SaveFile(string sFilename)
        {
            if (m_nUpperBound <= 0)
            {
                return false;
            }

            FileStream myFile = null;

            try
            {
                myFile = new FileStream(sFilename, FileMode.Create);

                using (BinaryWriter binStream = new BinaryWriter(myFile))
                {
                    myFile = null;

                    binStream.Write(FileHeaderVersioned());
                    binStream.Write(m_nUpperBound);

                    //We start at page 1 because page 0 is always null
                    for (UInt16 nPageInd = 1; nPageInd <= m_nUpperBound; nPageInd++)
                    {
                        binStream.Write(nPageInd);

                        //new in v002 - save date/time for each captured sweep
                        string sTime = GetData(nPageInd).CaptureTime.ToString("o");
                        binStream.Write((Int32)sTime.Length);
                        binStream.Write((string)sTime);

                        //new in v002 - save model when this was captured
                        binStream.Write((byte)GetData(nPageInd).Model);

                        //dump all bitmap bytes
                        for (int nIndY = 0; nIndY < 8; nIndY++)
                        {
                            for (int nIndX = 0; nIndX < 128; nIndX++)
                            {
                                byte nData = GetData(nPageInd).GetByte(nIndX + 128 * nIndY);
                                binStream.Write(nData);
                            }
                        }
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
    }
}
