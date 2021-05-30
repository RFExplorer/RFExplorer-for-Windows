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
    public partial class RFECommunicator : IDisposable
    {
        private class RFEConfiguration
        {
            public double fStartMHZ;
            public double fStepMHZ;
            public double fAmplitudeTopDBM;
            public double fAmplitudeBottomDBM;
            public UInt16 nFreqSpectrumSteps;
            public bool bExpansionBoardActive;
            public eMode eMode;
            public double fMinFreqMHZ;
            public double fMaxFreqMHZ;
            public double fMaxSpanMHZ;
            public double fRBWKHZ;
            public float fOffset_dB;
            public string sLineString;
            public eCalculator eCalculator;

            public bool bRFEGenHighPowerSwitch;
            public byte nRFEGenPowerLevel;
            public double fRFEGenCWFreqMHZ;
            public UInt16 nRFEGenSweepWaitMS;
            public bool bRFEGenPowerON;

            public bool bRFEGenStartHighPowerSwitch;
            public bool bRFEGenStopHighPowerSwitch;
            public byte nRFEGenStartPowerLevel;
            public byte nRFEGenStopPowerLevel;
            public UInt16 nRFGenSweepPowerSteps;

            public RFEConfiguration()
            {
                fStartMHZ = 0.0;
                fStepMHZ = 0.0;
                fAmplitudeTopDBM = 0.0;
                fAmplitudeBottomDBM = 0.0;
                nFreqSpectrumSteps = 0;
                bExpansionBoardActive = false;
                eMode = eMode.MODE_NONE;
                fMinFreqMHZ = 0.0;
                fMaxFreqMHZ = 0.0;
                fMaxSpanMHZ = 0.0;
                fRBWKHZ = 0.0;
                fOffset_dB = 0.0f;

                nRFEGenSweepWaitMS = 0;
                bRFEGenHighPowerSwitch=false;
                nRFEGenPowerLevel = 0;
                fRFEGenCWFreqMHZ = 0.0;
                bRFEGenPowerON = false;

                bRFEGenStartHighPowerSwitch = false;
                bRFEGenStopHighPowerSwitch = false;
                nRFEGenStartPowerLevel = 0;
                nRFEGenStopPowerLevel = 1;
                nRFGenSweepPowerSteps = 0;

                eCalculator = eCalculator.UNKNOWN;
            }

            public RFEConfiguration(RFEConfiguration objSource)
            {
                fStartMHZ = objSource.fStartMHZ;
                fStepMHZ = objSource.fStepMHZ;
                fAmplitudeTopDBM = objSource.fAmplitudeTopDBM;
                fAmplitudeBottomDBM = objSource.fAmplitudeBottomDBM;
                nFreqSpectrumSteps = objSource.nFreqSpectrumSteps;
                bExpansionBoardActive = objSource.bExpansionBoardActive;
                eMode = objSource.eMode;
                fMinFreqMHZ = objSource.fMinFreqMHZ;
                fMaxFreqMHZ = objSource.fMaxFreqMHZ;
                fMaxSpanMHZ = objSource.fMaxSpanMHZ;
                fRBWKHZ = objSource.fRBWKHZ;
                fOffset_dB = objSource.fOffset_dB;
                eCalculator = objSource.eCalculator;

                bRFEGenHighPowerSwitch = objSource.bRFEGenHighPowerSwitch;
                nRFEGenPowerLevel = objSource.nRFEGenPowerLevel;
                fRFEGenCWFreqMHZ = objSource.fRFEGenCWFreqMHZ;
                nRFEGenSweepWaitMS = objSource.nRFEGenSweepWaitMS;
                bRFEGenPowerON = objSource.bRFEGenPowerON;

                bRFEGenStartHighPowerSwitch = objSource.bRFEGenStartHighPowerSwitch;
                bRFEGenStopHighPowerSwitch = objSource.bRFEGenStopHighPowerSwitch;
                nRFEGenStartPowerLevel = objSource.nRFEGenStartPowerLevel;
                nRFEGenStopPowerLevel = objSource.nRFEGenStopPowerLevel;
                nRFGenSweepPowerSteps = objSource.nRFGenSweepPowerSteps;
            }

            public bool ProcessReceivedString(string sLine)
            {
                bool bOk = true;

                try
                {
                    sLineString = sLine;

                    if ((sLine.Length >= 60) && (sLine.Substring(0, 6) == "#C2-F:"))
                    {
                        //Spectrum Analyzer mode
                        fStartMHZ = Convert.ToInt32(sLine.Substring(6, 7)) / 1000.0; //note it comes in KHZ
                        fStepMHZ = Convert.ToInt32(sLine.Substring(14, 7)) / 1000000.0;  //Note it comes in HZ
                        fAmplitudeTopDBM = Convert.ToInt32(sLine.Substring(22, 4));
                        fAmplitudeBottomDBM = Convert.ToInt32(sLine.Substring(27, 4));
                        nFreqSpectrumSteps = Convert.ToUInt16(sLine.Substring(32, 4));
                        bExpansionBoardActive = (sLine[37] == '1');
                        eMode = (eMode)Convert.ToUInt16(sLine.Substring(39, 3));
                        fMinFreqMHZ = Convert.ToInt32(sLine.Substring(43, 7)) / 1000.0;
                        fMaxFreqMHZ = Convert.ToInt32(sLine.Substring(51, 7)) / 1000.0;
                        fMaxSpanMHZ = Convert.ToInt32(sLine.Substring(59, 7)) / 1000.0;

                        if (sLine.Length > 66)
                        {
                            fRBWKHZ = Convert.ToInt32(sLine.Substring(67, 5));
                        }
                        if (sLine.Length > 72)
                        {
                            fOffset_dB = Convert.ToInt32(sLine.Substring(73, 4));
                        }
                        if (sLine.Length > 76)
                        {
                            eCalculator = (eCalculator)Convert.ToUInt16(sLine.Substring(78,3));
                        }
                    }
                    else if ((sLine.Length >= 29) && (sLine.Substring(0, 4) == "#C3-"))
                    {
                        //Signal generator CW, SweepFreq and SweepAmp modes
                        switch(sLine[4])
                        {
                            case '*':
                                {
                                    fStartMHZ = Convert.ToInt32(sLine.Substring(6, 7)) / 1000.0; //note it comes in KHZ
                                    fRFEGenCWFreqMHZ = Convert.ToInt32(sLine.Substring(14, 7)) / 1000.0;  //Note it comes in KHZ
                                    nFreqSpectrumSteps = Convert.ToUInt16(sLine.Substring(22, 4));
                                    fStepMHZ = Convert.ToInt32(sLine.Substring(27, 7)) / 1000.0;  //Note it comes in KHZ
                                    bRFEGenHighPowerSwitch = (sLine[35] == '1');
                                    nRFEGenPowerLevel = Convert.ToByte(sLine[37] - 0x30);
                                    nRFGenSweepPowerSteps = Convert.ToUInt16(sLine.Substring(39, 4));
                                    bRFEGenStartHighPowerSwitch = (sLine[44] == '1');
                                    nRFEGenStartPowerLevel = Convert.ToByte(sLine[46] - 0x30);
                                    bRFEGenStopHighPowerSwitch = (sLine[48] == '1');
                                    nRFEGenStopPowerLevel = Convert.ToByte(sLine[50] - 0x30);
                                    bRFEGenPowerON = (sLine[52] == '1');
                                    nRFEGenSweepWaitMS = Convert.ToUInt16(sLine.Substring(54, 5));
                                    eMode = eMode.MODE_NONE;
                                    break;
                                }
                            case 'A':
                                {
                                    //Sweep Amplitude mode
                                    fRFEGenCWFreqMHZ = Convert.ToInt32(sLine.Substring(6, 7)) / 1000.0; //note it comes in KHZ
                                    nRFGenSweepPowerSteps = Convert.ToUInt16(sLine.Substring(14, 4));
                                    bRFEGenStartHighPowerSwitch = (sLine[19] == '1');
                                    nRFEGenStartPowerLevel = Convert.ToByte(sLine[21] - 0x30);
                                    bRFEGenStopHighPowerSwitch = (sLine[23] == '1');
                                    nRFEGenStopPowerLevel = Convert.ToByte(sLine[25] - 0x30);
                                    bRFEGenPowerON = (sLine[27] == '1');
                                    nRFEGenSweepWaitMS = Convert.ToUInt16(sLine.Substring(29, 5));
                                    eMode = RFECommunicator.eMode.MODE_GEN_SWEEP_AMP;
                                    break;
                                }
                            case 'F':
                                {
                                    //Sweep Frequency mode
                                    fStartMHZ = Convert.ToInt32(sLine.Substring(6, 7)) / 1000.0; //note it comes in KHZ
                                    nFreqSpectrumSteps = Convert.ToUInt16(sLine.Substring(14, 4));
                                    fStepMHZ = Convert.ToInt32(sLine.Substring(19, 7)) / 1000.0;  //Note it comes in KHZ
                                    bRFEGenHighPowerSwitch = (sLine[27] == '1');
                                    nRFEGenPowerLevel = Convert.ToByte(sLine[29] - 0x30);
                                    bRFEGenPowerON = (sLine[31] == '1');
                                    nRFEGenSweepWaitMS = Convert.ToUInt16(sLine.Substring(33, 5));
                                    eMode = RFECommunicator.eMode.MODE_GEN_SWEEP_FREQ;
                                    break;
                                }
                            case 'G':
                                {
                                    //Normal CW mode
                                    fRFEGenCWFreqMHZ = Convert.ToInt32(sLine.Substring(14, 7)) / 1000.0;  //Note it comes in KHZ
                                    nFreqSpectrumSteps = Convert.ToUInt16(sLine.Substring(22, 4));
                                    fStepMHZ = Convert.ToInt32(sLine.Substring(27, 7)) / 1000.0;  //Note it comes in KHZ
                                    bRFEGenHighPowerSwitch = (sLine[35] == '1');
                                    nRFEGenPowerLevel = Convert.ToByte(sLine[37] - 0x30);
                                    bRFEGenPowerON = (sLine[39] == '1');
                                    eMode = RFECommunicator.eMode.MODE_GEN_CW;
                                    break;
                                }
                            default:
                                eMode = eMode.MODE_NONE;
                                bOk = false;
                                break;
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
        }

    }
}
