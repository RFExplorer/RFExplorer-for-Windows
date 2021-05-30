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

namespace RFExplorerCommunicator
{
    /// <summary>
    /// This class represents a basic block of memory, up to 4096 bytes length, with an address within the available memory space, 
    /// a total length and a raw memory container
    /// </summary>
    public class RFEMemoryBlock
    {
        public const UInt16 MAX_BLOCK_SIZE = 4096;

        /// <summary>
        /// Memory container, values out of range are initialized to 0xFF
        /// </summary>
        byte[] m_arrBytes = new byte[MAX_BLOCK_SIZE];
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public byte[] DataBytes
        {
            get { return m_arrBytes; }
        }
        
        /// <summary>
        /// Memory type available in RFE devices
        /// </summary>
        public enum eMemoryType
        {
            MEM_FLASH=0,
            MEM_RAM1,
            MEM_RAM2
        }

        private eMemoryType m_eType;
        /// <summary>
        /// Memory type available in RFE devices
        /// </summary>
        public eMemoryType MemoryType
        {
            get { return m_eType; }
            set { m_eType = value; }
        }

        /// <summary>
        /// Valid address within the memory space this object is defined. For instance the external FLASH has a range of 2MB
        /// </summary>
        UInt32 m_nAddress = 0;
        public UInt32 Address
        {
            get { return m_nAddress; }
            set { m_nAddress = value; }
        }

        /// <summary>
        /// Size of the block in bytes, being MAX_BLOCK_SIZE the maximum value
        /// </summary>
        UInt16 m_nSize = 0;
        public UInt16 Size
        {
            get { return m_nSize; }
            set { m_nSize = value; }
        }

        public RFEMemoryBlock()
        {
            m_eType = eMemoryType.MEM_FLASH;
            for (int nInd = 0; nInd < MAX_BLOCK_SIZE; nInd++)
            {
                m_arrBytes[nInd] = 0xff; //initialize with same values to imitate internal memory status
            }
        }
    }
}
