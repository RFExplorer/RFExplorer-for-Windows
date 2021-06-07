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
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Collections;
using Microsoft.Win32;
using System.Diagnostics;
using System.Security;
using System.Runtime.InteropServices;
using System.Globalization;


namespace RFExplorerCommunicator
{
    /// <summary>
    /// Custom event class to report strings to external listeners
    /// </summary>
    public class EventReportInfo : EventArgs
    {
        string m_sData = "";
        public EventReportInfo(string sText)
        {
            m_sData = sText;
        }
        public string Data
        {
            get { return m_sData; }
            set { m_sData = value; }
        }
    }

    /// <summary>
    /// Main API class to support all basic low level operations with RF Explorer
    /// </summary>
    public partial class RFECommunicator : IDisposable
    {
        #region constants
        public const float MIN_AMPLITUDE_DBM = -140.0f;
        public const float MAX_AMPLITUDE_DBM = 50.0f;
        public const float MIN_AMPLITUDE_DBM_DEVICE = -120.0f;
        public const float MAX_AMPLITUDE_DBM_DEVICE = 35.0f;
        public const double MIN_AMPLITUDE_RANGE_DBM = 10;
        public const UInt16 MAX_SPECTRUM_STEPS = 65535;
        public const double MIN_AMPLITUDE_TRACKING_NORMALIZE = -80.0; //lower than this is considered too low for accurate measurement
        public const UInt16 NORMALIZING_AVG_PASSES = 3;
        public const UInt16 MAX_INTERNAL_CALIBRATED_DATA_SIZE = 162; //length of max embedded calibration device data in internal flash to 1G and 6G
        public const UInt16 MAX_INTERNAL_CALIBRATED_DATA_SIZE_MWSUB3G = 324; //length of max embedded calibration device data in internal flash to MSUB3G
        public const UInt16 POS_INTERNAL_CALIBRATED_6G = 134; //start position for 6G model
        public const UInt16 POS_INTERNAL_CALIBRATED_MWSUB3G = 0; //start position for MWSUB3G model
        public const UInt16 POS_INTERNAL_CALIBRATED_WSUB1G = 15;  //start position for WSUB1G model
        public const UInt16 POS_END_INTERNAL_CALIBRATED_WSUB1G = 58; //end position for WSUB1G, that is, POS_INTERNAL_CALIBRATED_WSUB1G = 15 + WSUB1G calibration data size = 43
        public const UInt16 POS_INTERNAL_CALIBRATED_WSUB1G_PLUS = 0;  //start position for WSUB1G+ model
        public const UInt16 POS_END_INTERNAL_CALIBRATED_WSUB1G_PLUS = 258; //end position for WSUB1G+, that is, POS_INTERNAL_CALIBRATED_WSUB1G_PLUS = 0 + WSUB1G calibration data size = 258 ((R1+R2+R3)86 x 3)
        public const UInt16 MAX_EXPANSION_CALIBRATED_DATA_SIZE = 512; //length of max embedded calibration expansion board data in external flash
        public const UInt16 FLASH_POS_INTERNAL_CALIBRATED_DATA = 1024;  //Start position of the calibration data in flash for expansion
        public const UInt16 FLASH_POS_END_INTERNAL_CALIBRATED_DATA = 2048;  //End position of the calibration data in flash for expansion
        public const UInt16 CALEXPANSION_RAM_TABLE_SIZE = 162;
        public const byte FLASH_FILE_CALIBRATION_DATE = 50;
        public const byte FLASH_FILE_EXPANSION_SERIALNUMBER = 10;
        public const UInt16 FLASH_FILE_CALIBRATION_CAP = 40;

        /// <summary>
        /// Internal clock used for sampling, required to calculate sample rate
        /// </summary>
        public const UInt32 FCY_CLOCK = 16 * 1000 * 1000;

        public const double RFGEN_MIN_FREQ_MHZ = 23.438;
        public const double RFGEN_MAX_FREQ_MHZ = 6000;
        public const double RFGENEXP_MIN_FREQ_MHZ = 0.100;
        public const double RFGENEXP_MAX_FREQ_MHZ = 6000;

        public const double RFGENEXP_MIN_CAL_DBM = -40f;
        public const double RFGENEXP_MAX_CAL_DBM = 10f;
        public const double RFGENEXP_MIN_UNCAL_DBM = -60f;
        public const double RFGENEXP_MAX_UNCAL_DBM = 15f;

        public const double RFE_SA_MIN_FREQ_MHZ = 0.050;
        public const double RFE_SA_MAX_FREQ_MHZ = 6100;
        public const double RFE_SA_MAX_SPAN_MHZ = 600;
        public const double RFE_SA_MIN_SPAN_MHZ = 0.112;
        public const Int32 RFE_MIN_SWEEP_POINTS = 112;
        public const Int32 RFE_MIN_SWEEP_STEPS = RFE_MIN_SWEEP_POINTS - 1;

        public const float MAX_FREQ_OFFSET_MHZ = 10000f;
        public const float MIN_FREQ_OFFSET_MHZ = -10000f;
        public const double MAX_AMP_OFFSET_DBM = 100;
        public const double MIN_AMP_OFFSET_DBM = -100;

        public const string _RFE_File_Extension = ".rfe";
        public const string _CSV_File_Extension = ".csv";
        public const string _SNIFFER_File_Extension = ".rfsniffer";
        public const string _SNANORM_File_Extension = ".snanorm";
        public const string _SNA_File_Extension = ".sna";
        public const string _RFS_File_Extension = ".rfs";
        public const string _PNG_File_Extension = ".png";
        public const string _RFL_File_Extension = ".rfl";
        public const string _RFA_File_Extension = ".rfa";
        public const string _S1P_File_Extension = ".s1p";
        public const string _SUPPORT_ZIP_Extension = ".zip";

        public const string _DEBUG_StringReport = "::DBG ";
        private const string _INTERNAL_PORTCLOSED_EVENT = "#RFEINT:PORTCLOSED";

        private const string _EEOT = "\xFF\xFE\xFF\xFE\x00"; //this indicates Early End Of Transmission, sent by devices with firmware > 1.27 and 3.10
        public const string _FIRMWARE_NOT_AVAILABLE = "N/A";
        private const string _DISCONNECTED = "DISCONNECTED";
        private const string _ACTIVE = "(ACTIVE)";
        private const string _Acknowldedge = "#ACK";
        private const string _ResetString = "(C) Ariel Rocholl ";

        private const string m_sAnalyzerStandard_FirmwareCertified = "01.28";           //Firmware version of Standard Analyzer which was tested and certified with this PC Client
        private const string m_sAnalyzerPlus_FirmwareCertified = "03.12";               //Firmware version of Plus Analizer models (WSUB1G+) which was tested and certified with this PC Client
        private const string m_sGeneratorStandard_FirmwareCertified = "01.31";          //Firmware version of Generator which was tested and certified with this PC Client
        private const string m_sRFExplorerFirmwareCertifiedAnalyzerAudioPro = "03.08"; //Firmware version of RF Explorer PLUS analyzer which was tested and certified with this PC Client
        private const string m_sAnalyzerIoT_FirmwareCertified = "01.15";                //Firmware version of IoT module Analyzer which was tested and certified with this PC Client

        public const double _Presets_StandardFirmwareRequired = 1.26; //Firmware version required for Standard Analyzer with support of Presets
        public const double _Presets_PlusFirmwareRequired = 3.06; //Firmware version required for Plus Analyzer with support of Presets

        public string FirmwareCertified
        {
            get
            {
                if (IsGenerator())
                    return m_sGeneratorStandard_FirmwareCertified;
                else
                {
                    if (IsMWSUB3G)
                        return m_sAnalyzerIoT_FirmwareCertified;
                    else if (MainBoardModel == eModel.MODEL_WSUB1G_PLUS)
                    {
                        return m_sAnalyzerPlus_FirmwareCertified;
                    }
                    else if (IsAudioPro)
                        return m_sRFExplorerFirmwareCertifiedAnalyzerAudioPro;
                    else
                    {
                        return m_sAnalyzerStandard_FirmwareCertified;
                    }
                }
            }
        }

        #endregion

        #region Member variables

        byte[] m_arrPreset = null;
        /// <summary>
        /// Array of bytes with one Preset Received from unit
        /// </summary>
        public byte[] PresetArray
        {
            get { return m_arrPreset; }
        }

        RFE6GEN_CalibrationData m_RFGenCal = new RFE6GEN_CalibrationData(); //Mainboard RFGen calibration data
        double[] m_arrRFGenExpansionCal = null; //Expansion RFGen calibration data

        UInt16 m_nFreqSpectrumSteps = RFECommunicator.RFE_MIN_SWEEP_STEPS;  //$S byte buffer by default.
        public UInt16 FreqSpectrumSteps
        {
            get { return m_nFreqSpectrumSteps; }
            set { m_nFreqSpectrumSteps = value; }
        }

        bool m_bUseByteBLOB = false; //enable or disable BLOB storage
        bool m_bUseStringBLOB = false; //enable or disable BLOB storage in string format
        bool g_bIsIOT = false; //This member indicates whether the HW is a IoT board, or a normal board. For IoT board, we do not scan USB ports.

        /// <summary>
        /// Display mode
        /// </summary>
        public enum RFExplorerSignalType
        {
            Realtime,
            Average,
            MaxPeak,
            Min,
            MaxHold,
            TOTAL_ITEMS
        };

        /// <summary>
        /// Available amplitude units
        /// </summary>
        public enum eAmplitudeUnit
        {
            dBm = 0,
            dBuV,
            Watt
        };

        eAmplitudeUnit m_eCurrentAmplitudeUnit;
        /// <summary>
        /// Get or set current amplitude units used externally
        /// </summary>
        public eAmplitudeUnit CurrentAmplitudeUnit
        {
            get { return m_eCurrentAmplitudeUnit; }
            set { m_eCurrentAmplitudeUnit = value; }
        }

        /// <summary>
        /// All possible RF Explorer model values
        /// </summary>
        public enum eModel
        {
            MODEL_433 = 0,  //0
            MODEL_868,      //1
            MODEL_915,      //2
            MODEL_WSUB1G,   //3
            MODEL_2400,     //4
            MODEL_WSUB3G,   //5
            MODEL_6G,       //6

            MODEL_WSUB1G_PLUS = 10,  //10
            MODEL_AUDIOPRO = 11,   //note this is converted internally to MODEL_WSUB3G to simplify code, but sets m_bAudioPro to true
            MODEL_2400_PLUS = 12,
            MODEL_4G_PLUS = 13,
            MODEL_6G_PLUS = 14,

            MODEL_RFGEN = 60, //60
            MODEL_RFGEN_EXPANSION = 61,  //61

            MODEL_NONE = 0xFF //0xFF
        }

        #region Device Model Detection

        /// <summary>
        /// Return true if the device mainboard is a PLUS model, regardless the expansion model connected (if any)
        /// </summary>
        public bool IsMainboardAnalyzerPlus
        {
            get { return MainBoardModel == eModel.MODEL_WSUB1G_PLUS || IsAudioPro; }
        }

        /// <summary>
        /// Property true if the WSUB3G connected is actually a MWSUB3G. This enables us to work with MWSUB3G same as WSUB3G
        /// except in specific places where MWSUB3G code is required. Use this function anytime to know if a WSUB3G is actually a MWSUB3G
        /// Note it also returns true for AudioPro as it includes a 3G in mainboard with same design as MWSUB3G
        /// </summary>
        public bool IsMWSUB3G
        {
            get { return (m_eMainBoardModel == eModel.MODEL_WSUB3G && m_eExpansionBoardModel == eModel.MODEL_NONE); }
        }

        /// <summary>
        /// Returns true if there is AudioPro board
        /// </summary>
        public bool IsAudioPro
        {
            get { return m_bAudioPro; }
        }

        /// <summary>
        /// Returns true if there is an expansion 4G+ or 6G+ board installed (note it may not be the active band)
        /// </summary>
        public bool IsExpansion4G6GPlus
        {
            get { return (m_eExpansionBoardModel == eModel.MODEL_4G_PLUS) || (m_eExpansionBoardModel == eModel.MODEL_6G_PLUS); }
        }

        /// <summary>
        /// Returns true if there is an expansion PLUS board installed (note it may not be the active band)
        /// </summary>
        public bool IsExpansionPlus
        {
            get { return (m_eExpansionBoardModel == eModel.MODEL_2400_PLUS) || IsExpansion4G6GPlus; }
        }

        /// <summary>
        /// Returns true if 4G+ or 6G+ boards are active
        /// </summary>
        public bool IsActive4G6GPlus
        {
            get { return m_eActiveModel == eModel.MODEL_4G_PLUS || m_eActiveModel == eModel.MODEL_6G_PLUS; }
        }
        #endregion

        #region Device features detection
        /// <summary>
        /// Returns true if devices allow high resolution mode for sweeps
        /// </summary>
        public bool IsHighResAvailable
        {
            get { return (m_eActiveModel == eModel.MODEL_WSUB1G_PLUS || IsActive4G6GPlus || IsAudioPro || IsMWSUB3G); }
        }

        /// <summary>
        /// Returns true if device allows choose input stage
        /// </summary>
        public bool IsInputStageAvailable
        {
            get { return (m_eActiveModel == eModel.MODEL_WSUB1G_PLUS || IsActive4G6GPlus || IsAudioPro || IsMWSUB3G); }
        }

        #endregion

        /// <summary>
        /// All possible DSP values
        /// </summary>
        public enum eDSP
        {
            DSP_AUTO = 0,
            DSP_FILTER,
            DSP_FAST,
            DSP_NO_IMG
        };

        eDSP m_eDSP = eDSP.DSP_AUTO;

        /// <summary>
        /// Get or set DSP mode
        /// </summary>
        public eDSP DSP
        {
            get { return m_eDSP; }
            set
            {
                SendCommand("Cp" + Convert.ToByte(value).ToString());
            }
        }
        /// <summary>
        /// Values used to compensate input stage data sent by device, which is defined at 8bits limit
        /// </summary>
        float[] m_arrInputStageOffsetDB = { 0f, 30f, -25f, 60f };

        /// <summary>
        /// Get Attenuation in dB according to input stage 
        /// </summary>
        public float InputStageAttenuationDB
        {
            get { return (m_arrInputStageOffsetDB[Convert.ToByte(m_eInputStage)]); }
        }

        public enum eInputStage
        {
            Direct = 0,
            Attenuator_30dB,
            LNA_25dB,
            Attenuator_60dB
        };

        eInputStage m_eInputStage = eInputStage.Direct;
        /// <summary>        /// Get or set InputStage in available models (do not use from incompatible models)
        /// </summary>
        public eInputStage InputStage
        {
            get { return m_eInputStage; }
            set
            {
                SendCommand("a" + (Convert.ToByte(value)).ToString());
            }
        }

        //Device temperature in degress centigrade, only available in certain models
        double m_fDeviceTemperature = 25;
        public double DeviceTemperature
        {
            get { return m_fDeviceTemperature; }
        }

        static int m_nMainThreadId = Thread.CurrentThread.ManagedThreadId; //Main thread ID to detect when working from a separated thread

        bool m_bAutoClose = false;
        /// <summary>
        /// Set this property to true to automatically close the COM port when the device present a connection failure
        /// set to false otherwise
        /// </summary>
        public bool AutoClose
        {
            get { return m_bAutoClose; }
            set { m_bAutoClose = value; }
        }

        /// <summary>
        /// Get the minimum threshold for amplitude according to input stage
        /// </summary>
        public float MinAmplitudeNormalizedDBM
        {
            get
            {
                return (MIN_AMPLITUDE_DBM_DEVICE + InputStageAttenuationDB);
            }
        }

        /// <summary>
        /// Get the maximum threshold for amplitude according to input stage
        /// </summary>
        public float MaxAmplitudeNormalizedDBM
        {
            get
            {
                return (MAX_AMPLITUDE_DBM_DEVICE + InputStageAttenuationDB);
            }
        }

        public enum eCalculator
        {
            NORMAL = 0,
            MAX,
            AVG,
            OVERWRITE,
            MAX_HOLD,
            MAX_HISTORICAL,
            UNKNOWN = 0xff
        };
        eCalculator m_eCalculator;
        /// <summary>
        /// Get the currently configured calculator in the device
        /// </summary>
        public eCalculator Calculator
        {
            get { return m_eCalculator; }
        }

        //GPS variables (only suitable for OEM code in MWSUB3G and similar modules)
        public string m_sGPSTimeUTC = "";
        public string m_sGPSLongitude = "";
        public string m_sGPSLattitude = "";

        //variables will look into system to know if it is Unix, Linux, etc
        bool m_bUnix = false;
        bool m_bWine = false;

        //used to create readable text together with eModel enum
        static string[] arrModels = null;

        /// <summary>
        /// Use this enum to know if a platform has been recognised and only check this once
        /// </summary>
        enum ePlatformChecked
        {
            FALSE = 0x0,
            TRUE,
            UNKNOWN = 0xF
        };

        /// <summary>
        ///  Determine if app is running or not in MacOs,
        ///  if this checking is not done, it set UNKNOWN state
        /// </summary>
        static ePlatformChecked m_eIsMacOS = ePlatformChecked.UNKNOWN;

        /// <summary>
        ///  Determine if app is running or not in RaspberryPi,
        ///  if this checking is not done, it set UNKNOWN state
        /// </summary>
        static ePlatformChecked m_eIsRaspberry = ePlatformChecked.UNKNOWN;


        private void InitializeModels()
        {
            arrModels = new string[256];
            for (int nInd = 0; nInd < arrModels.Length; nInd++)
            {
                arrModels[nInd] = "UNKWN";
            }

            arrModels[(int)eModel.MODEL_433] = "433M";
            arrModels[(int)eModel.MODEL_868] = "868M";
            arrModels[(int)eModel.MODEL_915] = "915M";
            arrModels[(int)eModel.MODEL_WSUB1G] = "WSUB1G";
            arrModels[(int)eModel.MODEL_2400] = "2.4G";
            arrModels[(int)eModel.MODEL_WSUB3G] = "WSUB3G";
            arrModels[(int)eModel.MODEL_6G] = "6G";
            arrModels[(int)eModel.MODEL_WSUB1G_PLUS] = "WSUB1G_PLUS";
            arrModels[(int)eModel.MODEL_AUDIOPRO] = "PROAUDIO";
            arrModels[(int)eModel.MODEL_2400_PLUS] = "2.4G_PLUS";
            arrModels[(int)eModel.MODEL_4G_PLUS] = "4G_PLUS";
            arrModels[(int)eModel.MODEL_6G_PLUS] = "6G_PLUS";
            arrModels[(int)eModel.MODEL_RFGEN] = "RFE6GEN";
            arrModels[(int)eModel.MODEL_RFGEN_EXPANSION] = "RFEGEN_COMBO";
            arrModels[(int)eModel.MODEL_NONE] = "NONE";
        }

        /// <summary>
        /// Returns a human readable and normalized identifier text for the model specified in the enum
        /// </summary>
        /// <param name="model">RFExplorer model</param>
        /// <returns>model text identifier such as 433M or WSUB1G</returns>
        public static string GetModelTextFromEnum(eModel model)
        {
            return arrModels[(int)model];
        }

        /// <summary>
        /// Returns a human readable and normalized identifier text for the model specified in the enum
        /// </summary>
        /// <param name="model">RFExplorer model</param>
        /// <returns>Compressed model text identifier such as 3G or 1G+</returns>
        public static string GetCompressedModelFromEnum(eModel eModel, bool bIsAudioPro, bool bIsIoTModule, bool bDeviceConnected)
        {
            string sCompressedName = "NONE";
            if (bDeviceConnected)
            {
                switch (eModel)
                {
                    case eModel.MODEL_433:
                        sCompressedName = "433";
                        break;
                    case eModel.MODEL_868:
                        sCompressedName = "868";
                        break;
                    case eModel.MODEL_915:
                        sCompressedName = "915";
                        break;
                    case eModel.MODEL_WSUB1G:
                        sCompressedName = "1G";
                        break;
                    case eModel.MODEL_2400:
                        sCompressedName = "24G";
                        break;
                    case eModel.MODEL_WSUB3G:
                        sCompressedName = "3G";
                        if (bIsAudioPro)
                            sCompressedName = "ProAudio";
                        else if (bIsIoTModule)
                            sCompressedName = "3G+ IoT";
                        break;
                    case eModel.MODEL_6G:
                        sCompressedName = "6G";
                        break;
                    case eModel.MODEL_WSUB1G_PLUS:
                        sCompressedName = "1G+";
                        break;
                    case eModel.MODEL_AUDIOPRO:
                        sCompressedName = "PROAUDIO";
                        break;
                    case eModel.MODEL_2400_PLUS:
                        sCompressedName = "24G+";
                        break;
                    case eModel.MODEL_4G_PLUS:
                        sCompressedName = "4G+";
                        break;
                    case eModel.MODEL_6G_PLUS:
                        sCompressedName = "6G+";
                        break;
                    case eModel.MODEL_RFGEN:
                        sCompressedName = "RFGEN";
                        break;
                    case eModel.MODEL_RFGEN_EXPANSION:
                        sCompressedName = "RFGENCOMBO";
                        break;
                    default:
                        break;
                }
            }
            return sCompressedName;
        }

        /// <summary>
        /// Returns model enumerator based on text provided
        /// </summary>
        /// <param name="sText">One of "433M", "868M", "915M", "WSUB1G", "2.4G", "WSUB3G", "6G"</param>
        /// <returns>Return valid model enumerator or will set to MODEL_NONE if not found</returns>
        public eModel GetModelEnumFromText(string sText)
        {
            eModel eReturn = eModel.MODEL_NONE;

            for (int nInd = 0; nInd < arrModels.Length; nInd++)
            {
                if (sText.ToUpper() == arrModels[nInd])
                {
                    eReturn = (eModel)nInd;
                    break;
                }
            }

            return eReturn;
        }

        public string GetModelTypeText(eModel eMainBoard, eModel eExpansionBoard, bool bIsAudioPro)
        {
            string sModelName = "";
            string sModelTechnology = eMainBoard.ToString();
            if (eExpansionBoard != RFECommunicator.eModel.MODEL_NONE)
                sModelTechnology += " " + eExpansionBoard.ToString();
            sModelTechnology = sModelTechnology.Replace("MODEL_", "");
            sModelTechnology = sModelTechnology.Replace("_", " ");
            switch (sModelTechnology)
            {
                case "WSUB1G 2400": sModelName = "ISM Combo"; break;
                case "WSUB1G WSUB3G": sModelName = "3G Combo"; break;
                case "6G WSUB3G": sModelName = "6G Combo"; break;
                case "6G 2400": sModelName = "WiFi Combo"; break;
                case "6G": sModelName = "6G"; break;
                case "2400 WSUB3G": sModelName = "2.4G-3G Combo"; break;
                case "WSUB1G": sModelName = "WSUB1G"; break;
                case "WSUB1G PLUS": sModelName = "WSUB1G PLUS"; break;
                case "433": sModelName = " 433M "; break;
                case "868": sModelName = "868M "; break;
                case "915": sModelName = " 915M "; break;
                case "2400": sModelName = "2.4G "; break;
                case "RFGEN": sModelName = "RFE6GEN "; break;
                case "RFGEN EXPANSION": sModelName = "RFEGEN Combo "; break;
                case "WSUB3G":
                    if (bIsAudioPro)
                        sModelName = "ProAudio";
                    else
                        sModelName = "3G+ IoT";
                    break;
                case "WSUB1G PLUS 2400": sModelName = "ISM Combo Plus Limited"; break;
                case "WSUB1G PLUS WSUB3G": sModelName = "3G Combo Plus Limited"; break;
                case "WSUB1G PLUS 2400 PLUS": sModelName = "ISM Combo Plus"; break;
                case "WSUB1G PLUS 4G PLUS": sModelName = "4G Combo Plus"; break;
                case "WSUB1G PLUS 6G PLUS": sModelName = "6G Combo Plus"; break;

                default: sModelName = "Custom Model"; break;
            }
            return sModelName;
        }

        enum eModulation
        {
            MODULATION_OOK_RAW,         //0
            MODULATION_PSK_RAW,         //1
            MODULATION_OOK_STD,         //2
            MODULATION_PSK_STD,         //3
            MODULATION_NONE = 0xFF  //0xFF
        };

        //offset values read from spectrum analyzer calibration
        bool m_bMainboardInternalCalibrationAvailable = false;
        bool m_bExpansionBoardInternalCalibrationAvailable = false;
        float[] m_arrSpectrumAnalyzerEmbeddedCalibrationOffsetDB = null;
        float[] m_arrSpectrumAnalyzerExpansionCalibrationOffsetDB = null;
        //Counter indicating how many times the calibration has been requested to limit retries
        int m_nRetriesCalibration = 0;


        /// <summary>
        /// As a function of expansion or mainboard being currently selected, returns true if there is internal
        /// calibration data available, or false if not.
        /// IMPORTANT: the calibration data is not returned immediately after connection and that may make think
        /// the calibration is not available. 
        /// </summary>
        /// <returns></returns>
        public bool IsAnalyzerEmbeddedCal()
        {
            if (ExpansionBoardActive)
            {
                return m_bExpansionBoardInternalCalibrationAvailable;
            }
            else
            {
                return m_bMainboardInternalCalibrationAvailable;
            }
        }

        public static string DecorateSerialNumberRAWString(string sRAWSerialNumber)
        {
            if (!String.IsNullOrEmpty(sRAWSerialNumber) && sRAWSerialNumber.Length >= 16)
            {
                return (sRAWSerialNumber.Substring(0, 4) + "-" + sRAWSerialNumber.Substring(4, 4) + "-" + sRAWSerialNumber.Substring(8, 4) + "-" + sRAWSerialNumber.Substring(12, 4));
            }
            else
                return "";
        }

        string m_sExpansionSerialNumber = "";

        /// <summary>
        /// Serial number for the expansion board, if any.
        /// </summary>
        public string ExpansionSerialNumber
        {
            get
            {
                if (!m_bPortConnected)
                    m_sExpansionSerialNumber = "";

                return DecorateSerialNumberRAWString(m_sExpansionSerialNumber);
            }
        }

        string m_sSerialNumber = "";
        /// <summary>
        /// Serial number for the device (main board)
        /// </summary>
        public string SerialNumber
        {
            get
            {
                if (!m_bPortConnected)
                    m_sSerialNumber = "";

                return DecorateSerialNumberRAWString(m_sSerialNumber);
            }
        }

        //if available, points to a RF Explorer Signal Generator to be used for tracking. It is only meaningful and used when the current object is a spectrum analyzer
        private RFECommunicator m_objRFEGen;
        /// <summary>
        /// Connected tracking generator linked to the current spectrum analyzer object.
        /// </summary>
        public RFECommunicator TrackingRFEGen
        {
            get { return m_objRFEGen; }
            set { m_objRFEGen = value; }
        }

        bool m_bUseMaxHold = true;
        public bool UseMaxHold
        {
            get { return m_bUseMaxHold; }
            set
            {
                if (value != m_bUseMaxHold)
                {
                    if (value)
                    {
                        SendCommand_SetMaxHold();
                    }
                    else
                    {
                        if (Calculator != RFECommunicator.eCalculator.NORMAL)
                            SendCommand_Realtime(); //avoid sending it again if already in normal mode
                    }
                }
                m_bUseMaxHold = value;
            }
        }

        /// <summary>
        /// Returns the dBuV value assuming 50ohm
        /// </summary>
        /// <param name="dBm"></param>
        /// <returns></returns>
        public static double Convert_dBm_2_dBuV(double dBm)
        {
            return (dBm + 107.0f);
        }

        public static double Convert_dBuV_2_dBm(double dBuV)
        {
            return (dBuV - 107.0f);
        }

        public static double Convert_dBm_2_mW(double dBm)
        {
            return (Math.Pow(10, dBm / 10.0));
        }

        public static double Convert_dBm_2_Watt(double dBm)
        {
            return (Convert_dBm_2_mW(dBm) / 1000.0f);
        }

        public static double Convert_mW_2_dBm(double mW)
        {
            return (10.0f * Math.Log10(mW));
        }

        public static double Convert_Watt_2_dBm(double Watt)
        {
            return (30.0f + Convert_mW_2_dBm(Watt));
        }

        /// <summary>
        /// Will convert from eFrom amplitude unit to eTo amplitude unit
        /// </summary>
        /// <param name="eFrom"></param>
        /// <param name="dFromAmplitude">amplitude value to convert from, in eFrom units</param>
        /// <param name="eTo"></param>
        /// <returns>amplitude value in eTo units</returns>
        public static double ConvertAmplitude(eAmplitudeUnit eFrom, double dFromAmplitude, eAmplitudeUnit eTo)
        {
            if (eTo == eFrom)
                return dFromAmplitude;

            if (eFrom == eAmplitudeUnit.dBm)
            {
                if (eTo == eAmplitudeUnit.dBuV)
                    return Convert_dBm_2_dBuV(dFromAmplitude);
                else
                    return Convert_dBm_2_Watt(dFromAmplitude);
            }
            else if (eFrom == eAmplitudeUnit.dBuV)
            {
                if (eTo == eAmplitudeUnit.dBm)
                    return Convert_dBuV_2_dBm(dFromAmplitude);
                else
                    return Convert_dBm_2_Watt(Convert_dBuV_2_dBm(dFromAmplitude));
            }
            else
            {
                if (eTo == eAmplitudeUnit.dBm)
                    return Convert_Watt_2_dBm(dFromAmplitude);
                else
                    return Convert_dBm_2_dBuV(Convert_Watt_2_dBm(dFromAmplitude));
            }
        }

        /// <summary>
        /// Returns power channel over the selected bandwith captured. The power is instantaneous real time
        /// </summary>
        /// <param name="nSweepIndex">Number of selected SweepData</param>
        /// <param name="fStartMHZ">Initial Frequency of Channel</param>
        /// <param name="fEndMHZ">Final Frequency of Channel</param>
        /// <param name="eSignalType">Enum of Sygnal Type</param>
        /// <param name="eAmplitudeUnit">Enum of current unit of Amplitude</param>
        /// <returns>Channel power in currentUnit/span</returns>
        public double CalculatePowerChannel(UInt16 nSweepIndex, double fStartMHZ, double fEndMHZ, RFExplorerSignalType eSignalType, eAmplitudeUnit eAmplitudeUnit)
        {
            double fPowerChannel = 0.0f;
            double fPowerChannelTemp = 0.0f;

            if (eAmplitudeUnit == eAmplitudeUnit.dBm)
            {
                fPowerChannel = CalculatePowerChannelDBM(nSweepIndex, fStartMHZ, fEndMHZ, eSignalType);
            }
            if (eAmplitudeUnit == eAmplitudeUnit.dBuV)
            {
                if (eSignalType == RFExplorerSignalType.Realtime)
                {
                    RFESweepData objSweepData = this.SweepData.GetData(nSweepIndex);

                    for (UInt16 nDataPoint = 0; nDataPoint < m_objPartialSweep.TotalDataPoints; nDataPoint++)//m_objPartialSweep has same points as objSweepData
                    {
                        if (nDataPoint < m_objPartialSweep.AvailableDataPoints)
                        {
                            if ((m_objPartialSweep.GetFrequencyMHZ(nDataPoint) >= fStartMHZ) && (m_objPartialSweep.GetFrequencyMHZ(nDataPoint) < fEndMHZ))
                                fPowerChannelTemp += RFECommunicator.Convert_dBm_2_Watt(m_objPartialSweep.GetAmplitudeDBM(nDataPoint));
                        }
                        else if (objSweepData != null && (objSweepData.GetFrequencyMHZ(nDataPoint) >= fStartMHZ) && (objSweepData.GetFrequencyMHZ(nDataPoint) < fEndMHZ))
                            fPowerChannelTemp += RFECommunicator.Convert_dBm_2_Watt(objSweepData.GetAmplitudeDBM(nDataPoint));
                    }
                }
                if (fPowerChannelTemp > 0.0f)
                {
                    fPowerChannel = Convert_dBm_2_dBuV(Convert_Watt_2_dBm(fPowerChannelTemp));
                }
                else
                    fPowerChannel = Convert_dBm_2_dBuV(MinAmplitudeNormalizedDBM);

            }
            if (eAmplitudeUnit == eAmplitudeUnit.Watt)
            {
                if (eSignalType == RFExplorerSignalType.Realtime)
                {
                    RFESweepData objSweepData = this.SweepData.GetData(nSweepIndex);

                    for (UInt16 nDataPoint = 0; nDataPoint < m_objPartialSweep.TotalDataPoints; nDataPoint++)//m_objPartialSweep has same points as objSweepData
                    {
                        if (nDataPoint < m_objPartialSweep.AvailableDataPoints)
                        {
                            if ((m_objPartialSweep.GetFrequencyMHZ(nDataPoint) >= fStartMHZ) && (m_objPartialSweep.GetFrequencyMHZ(nDataPoint) < fEndMHZ))
                                fPowerChannelTemp += RFECommunicator.Convert_dBm_2_Watt(m_objPartialSweep.GetAmplitudeDBM(nDataPoint));
                        }
                        else if (objSweepData != null && (objSweepData.GetFrequencyMHZ(nDataPoint) >= fStartMHZ) && (objSweepData.GetFrequencyMHZ(nDataPoint) < fEndMHZ))
                            fPowerChannelTemp += RFECommunicator.Convert_dBm_2_Watt(objSweepData.GetAmplitudeDBM(nDataPoint));
                    }
                }

                if (fPowerChannelTemp > 0.0f)
                    fPowerChannel = fPowerChannelTemp;
                else
                    fPowerChannel = Convert_dBm_2_mW(MinAmplitudeNormalizedDBM);

            }

            return fPowerChannel;
        }


        /// <summary>
        /// Returns power channel over the selected bandwith captured. The power is instantaneous real time
        /// </summary>
        /// <param name="nSweepIndex">Number of selected SweepData</param>
        /// <param name="fStartMHZ">Initial Frequency of Channel</param>
        /// <param name="fEndMHZ">Final Frequency of Channel</param>
        /// <param name="eSignalType">Enum of Sygnal Type</param>
        /// <returns>channel power in dBm/span</returns>
        public double CalculatePowerChannelDBM(UInt16 nSweepIndex, double fStartMHZ, double fEndMHZ, RFExplorerSignalType eSignalType)
        {
            double fPowerChannelDBM = 0.0f;
            double fPowerChannelTemp = 0.0f;

            if (eSignalType == RFExplorerSignalType.Realtime)
            {
                RFESweepData objSweepData = this.SweepData.GetData(nSweepIndex);

                for (UInt16 nDataPoint = 0; nDataPoint < m_objPartialSweep.TotalDataPoints; nDataPoint++)//m_objPartialSweep has same points as objSweepData
                {
                    if (nDataPoint < m_objPartialSweep.AvailableDataPoints)
                    {
                        if ((m_objPartialSweep.GetFrequencyMHZ(nDataPoint) >= fStartMHZ) && (m_objPartialSweep.GetFrequencyMHZ(nDataPoint) < fEndMHZ))
                            fPowerChannelTemp += RFECommunicator.Convert_dBm_2_Watt(m_objPartialSweep.GetAmplitudeDBM(nDataPoint));
                    }
                    else if (objSweepData != null && (objSweepData.GetFrequencyMHZ(nDataPoint) >= fStartMHZ) && (objSweepData.GetFrequencyMHZ(nDataPoint) < fEndMHZ))
                        fPowerChannelTemp += RFECommunicator.Convert_dBm_2_Watt(objSweepData.GetAmplitudeDBM(nDataPoint));
                }
            }

            if (fPowerChannelTemp > 0.0f)
                fPowerChannelDBM = Convert_Watt_2_dBm(fPowerChannelTemp);
            else
                fPowerChannelDBM = MinAmplitudeNormalizedDBM;

            return fPowerChannelDBM;
        }

        /// <summary>
        /// Returns power channel over the selected points captured. 
        /// </summary>
        /// <param name="listPoints">List with PointF to contain amplitude of signal</param>
        /// <param name="nStart">Sample to start</param>
        /// <param name="nEnd">Sample to finish</param>
        /// <returns>channel power in dBm/span</returns>
        public double CalculatePowerChannelDBM(List<System.Drawing.PointF> listPoints, UInt16 nStart, UInt16 nEnd)
        {
            double fPowerChannelDBM = 0.0f;
            double fPowerChannelTemp = 0.0f;

            for (UInt16 nInd = nStart; nInd < nEnd; nInd++)
            {
                fPowerChannelTemp += RFECommunicator.ConvertAmplitude(CurrentAmplitudeUnit, listPoints[nInd].Y, RFECommunicator.eAmplitudeUnit.Watt);
            }

            if (fPowerChannelTemp > 0.0f)
                fPowerChannelDBM = Convert_Watt_2_dBm(fPowerChannelTemp);
            else
                fPowerChannelDBM = MinAmplitudeNormalizedDBM;

            return fPowerChannelDBM;
        }

        //The RF model installed in main board
        eModel m_eMainBoardModel = eModel.MODEL_NONE;
        public eModel MainBoardModel
        {
            get { return m_eMainBoardModel; }
        }

        //The RF model installed in the expansion board
        eModel m_eExpansionBoardModel = eModel.MODEL_NONE;
        public eModel ExpansionBoardModel
        {
            get { return m_eExpansionBoardModel; }
        }

        //The model active, regardless being main or expansion board
        eModel m_eActiveModel = eModel.MODEL_NONE;
        public eModel ActiveModel
        {
            get { return m_eActiveModel; }
        }

        //True when the expansion board is active, false otherwise
        bool m_bExpansionBoardActive = false;
        public bool ExpansionBoardActive
        {
            get { return m_bExpansionBoardActive; }
        }

        UInt32 m_nBaudrate = 0;
        /// <summary>
        /// Set or get the baudrate for modulation modes such as Sniffer. Note it may be actual baudrate or sample rate depending on modulation type
        /// </summary>
        public UInt32 BaudRate
        {
            get { return m_nBaudrate; }
            set { m_nBaudrate = value; }
        }

        string m_sRFExplorerFirmware;
        /// <summary>
        /// Detected Firmware
        /// </summary>
        public string RFExplorerFirmwareDetected
        {
            get
            {
                if (String.IsNullOrEmpty(m_sRFExplorerFirmware) || !m_bPortConnected)
                    return _FIRMWARE_NOT_AVAILABLE;
                else
                    return m_sRFExplorerFirmware;
            }
        }
        public bool IsFirmwareSameOrNewer(double fVersionWanted)
        {
            bool bReturn = false;

            try
            {
                if (!String.IsNullOrEmpty(m_sRFExplorerFirmware))
                {
                    //From october 2017 we always save data with "en-US" settings
                    double fDetected = Double.Parse(m_sRFExplorerFirmware, CultureInfo.InvariantCulture);
                    if (fDetected >= fVersionWanted)
                        bReturn = true;
                }
            }
            catch (Exception objEx)
            {
                ReportLog(objEx.ToString(), true);
            }

            return bReturn;
        }

        /// <summary>
        /// Human readable text with current HW/firmware configuration received from device or file
        /// </summary>
        public string FullModelText
        {
            get
            {
                string sModelText = _DISCONNECTED;
                if (PortConnected || (SweepData.Count > 0))
                {
                    string sModel = arrModels[(int)m_eMainBoardModel];
                    if (m_eActiveModel != m_eExpansionBoardModel)
                        sModel += _ACTIVE;
                    string sExpansion;
                    if (m_eExpansionBoardModel == eModel.MODEL_NONE)
                        sExpansion = " - No Expansion Module found";
                    else
                    {
                        sExpansion = " - Expansion Module:" + arrModels[(int)m_eExpansionBoardModel];
                        if (m_eActiveModel == m_eExpansionBoardModel)
                            sExpansion += _ACTIVE;
                    }

                    sModelText = "Client v" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + " - Firmware v" + m_sRFExplorerFirmware +
                    " - Model:" + sModel + sExpansion +
                    " - Active range:" + m_fMinFreqMHZ.ToString() + "-" + m_fMaxFreqMHZ.ToString() + "MHz";
                }
                return sModelText;
            }
        }

        /// <summary>
        /// Human readable text with current running configuration received from device or file
        /// </summary>
        private string ConfigurationText
        {
            get
            {
                string sConfiguration = _DISCONNECTED;
                //From october 2017 we always save data with "en-US" settings
                CultureInfo objInvCul = CultureInfo.InvariantCulture;
                if (IsAnalyzer())
                {
                    if (PortConnected || (SweepData.Count > 0))
                    {
                        sConfiguration = "Start: " + StartFrequencyNormalizedMHZ.ToString("f3", objInvCul) + "MHz - Stop:" + StopFrequencyNormalizedMHZ.ToString("f3", objInvCul) +
                            "MHz - Center:" + CalculateCenterFrequencyNormalizedMHZ().ToString("f3", objInvCul) + "MHz - Span:" + CalculateFrequencySpanMHZ().ToString("f3", objInvCul) +
                            "MHz - Sweep Step:" + (m_fStepFrequencyMHZ * 1000.0).ToString("f0", objInvCul) + "KHz";

                        if (m_fRBWKHZ > 0.0)
                        {
                            sConfiguration += " - RBW:" + m_fRBWKHZ.ToString("f0", objInvCul) + "KHz";
                        }
                        sConfiguration += " - Amp Offset:" + m_fOffset_dB.ToString("f0", objInvCul) + "dBm";
                        //This comment applies only for normalization files.
                        //This offset could be for analyzer as well as generator. We just want to know the freq offset value to save. 
                        //Then, when restoring data we will know who own frequency offset depending on freq offset logic (A/G)
                        sConfiguration += " - Freq Offset:" + m_fOffset_MHZ.ToString("f3", objInvCul) + "MHz";
                        if (m_bFreqOffsetInAnalyzer)
                        {
                            sConfiguration += " - FreqOffsetLogic:A";
                        }
                        else
                        {
                            sConfiguration += " - FreqOffsetLogic:G";
                        }
                    }
                }
                else
                {
                    if (PortConnected)
                    {
                        sConfiguration = "CW: " + m_fRFGenCWFrequencyMHZ.ToString("f3", objInvCul) + "MHz - Start:" + m_fRFGenStartFrequencyMHZ.ToString("f3", objInvCul) + "MHz - Stop:" +
                            m_fRFGenStopFrequencyMHZ.ToString("f3", objInvCul) + "MHz - Step:" + (RFGenStepMHZ().ToString("f3", objInvCul)) + "MHz - PowerLevel:" +
                            m_nRFGenPowerLevel.ToString("D3") + " - HighPowerSwitch:" + m_bRFGenHighPowerSwitch.ToString() + " - SweepSteps:" +
                            m_nRFGenSweepSteps.ToString("D4") + " - StepWait:" + m_nRFGenStepWaitMS.ToString("D5") + "ms";
                        //This comment applies only for normalization files.
                        //This offset could be for analyzer as well as generator. We just want to know the freq offset value to save. 
                        //Then, when restoring data we will know who own frequency offset depending on freq offset logic (A/G)
                        sConfiguration += " - Freq Offset:" + m_fOffset_MHZ.ToString("f3", objInvCul) + "MHz";
                        if (m_bFreqOffsetInAnalyzer)
                        {
                            sConfiguration += " - FreqOffsetLogic:A";
                        }
                        else
                        {
                            sConfiguration += " - FreqOffsetLogic:G";
                        }
                    }
                }

                return sConfiguration;
            }
        }

        DateTime m_LastCaptureTime;     //Time where prior capture was received. Used to calculate sweep elapsed time.
        string m_sSweepInfoText;        //Human readable text with time of last capture as well as average sweep time and sweeps / second
        public string SweepInfoText
        {
            get { return m_sSweepInfoText; }
        }

        string m_sSweepDate;        //Human readable text with time of last capture 
        /// <summary>
        /// Time of last sweep capture
        /// </summary>
        public string SweepDate
        {
            get { return m_sSweepDate; }
        }

        string m_sSweepSpeed;        //Human readable text with  average sweep time and sweeps / second and sweep data points resolution
        /// <summary>
        /// Average sweep time and sweeps / second and sweep data points resolution
        /// </summary>
        public string SweepSpeed
        {
            get { return m_sSweepSpeed; }
        }




        //The current operational mode
        public enum eMode
        {
            MODE_SPECTRUM_ANALYZER = 0,
            MODE_TRANSMITTER = 1,
            MODE_WIFI_ANALYZER = 2,
            MODE_TRACKING = 5,
            MODE_SNIFFER = 6,

            MODE_GEN_CW = 60,
            MODE_GEN_SWEEP_FREQ = 61,
            MODE_GEN_SWEEP_AMP = 62,

            MODE_NONE = 0xFF
        };

        eMode m_eMode = eMode.MODE_SPECTRUM_ANALYZER;
        public eMode Mode
        {
            get { return m_eMode; }
        }

        //Initializer for 433MHz model, will change later based on settings
        double m_fMinSpanMHZ = RFE_SA_MIN_SPAN_MHZ;       //Min valid span in MHZ for connected model
        public double MinSpanMHZ
        {
            get { return m_fMinSpanMHZ; }
            set { m_fMinSpanMHZ = value; }
        }

        double m_fMaxSpanMHZ = 100.0;       //Max valid span in MHZ for connected model
        public double MaxSpanMHZ
        {
            get { return m_fMaxSpanMHZ; }
            set { m_fMaxSpanMHZ = value; }
        }

        double m_fMinFreqMHZ = 430.0;       //Min valid frequency in MHZ for connected model
        public double MinFreqMHZ
        {
            get { return m_fMinFreqMHZ; }
            set { m_fMinFreqMHZ = value; }
        }
        double m_fMinFreqNormalizedMHZ = 430.0;  //Min valid normalized frequency in MHZ for connected model when frequency is set
        public double MinFreqNormalizedMHZ
        {
            get
            {
                m_fMinFreqNormalizedMHZ = m_fMinFreqMHZ + FrequencyOffsetMHZ;
                if (m_fMinFreqNormalizedMHZ < 0)
                    m_fMinFreqNormalizedMHZ = 0;

                return m_fMinFreqNormalizedMHZ;
            }
            set { m_fMinFreqNormalizedMHZ = value; }
        }

        double m_fMaxFreqMHZ = 440.0;       //Max valid frequency in MHZ for connected model
        public double MaxFreqMHZ
        {
            get { return m_fMaxFreqMHZ; }
            set { m_fMaxFreqMHZ = value; }
        }
        double m_fMaxFreqNormalizedMHZ = 440.0;       //Max valid normalized frequency in MHZ for connected model when frequency offset is set
        public double MaxFreqNormalizedMHZ
        {
            get
            {
                m_fMaxFreqNormalizedMHZ = MaxFreqMHZ + FrequencyOffsetMHZ;
                return m_fMaxFreqNormalizedMHZ;
            }
            set { m_fMaxFreqNormalizedMHZ = value; }
        }

        double m_fPeakValueMHZ = 0.0f;      //Last drawing iteration peak value MHZ read
        public double PeakValueMHZ
        {
            get { return m_fPeakValueMHZ; }
            set { m_fPeakValueMHZ = value; }
        }

        double m_fPeakValueAmp = -120.0f;   //Last drawing iteration peak value dBm read
        public double PeakValueAmplitudeDBM
        {
            get { return m_fPeakValueAmp; }
            set { m_fPeakValueAmp = value; }
        }

        double m_fAmplitudeTopDBM = MAX_AMPLITUDE_DBM_DEVICE;       //dBm for top graph limit
        /// <summary>
        /// This is the highest value that should be selected for display, includes Offset dBm
        /// </summary>
        public double AmplitudeTopDBM
        {
            get { return m_fAmplitudeTopDBM; }
            set { m_fAmplitudeTopDBM = value; }
        }

        /// <summary>
        /// AmplitudeTop property includes the offset dBm, the normalized one does not
        /// </summary>
        public double AmplitudeTopNormalizedDBM
        {
            get { return m_fAmplitudeTopDBM + m_fOffset_dB + InputStageAttenuationDB; }
            set { m_fAmplitudeTopDBM = value - m_fOffset_dB - InputStageAttenuationDB; }
        }

        double m_fAmplitudeBottomDBM = MIN_AMPLITUDE_DBM_DEVICE;   //dBm for bottom graph limit
        /// <summary>
        /// This is the lowest value that should be selected for display, includes Offset dBm
        /// </summary>
        public double AmplitudeBottomDBM
        {
            get { return m_fAmplitudeBottomDBM; }
            set { m_fAmplitudeBottomDBM = value; }
        }

        /// <summary>
        /// AmplitudeBottom property in dBm includes the offset dB and input stage attenuation in dB,  the normalized one does not
        /// </summary>
        public double AmplitudeBottomNormalizedDBM
        {
            get { return m_fAmplitudeBottomDBM + m_fOffset_dB + InputStageAttenuationDB; }
            set { m_fAmplitudeBottomDBM = value - m_fOffset_dB - InputStageAttenuationDB; }
        }

        bool m_bAcknowledge = false;        //Acknowledge used for checking synchronous messages
        public bool Acknowledged            //Everytime we check the acknowledge, it reset itself to false
        {
            get
            {
                bool bTemp = m_bAcknowledge;
                m_bAcknowledge = false;
                return bTemp;
            }
        }

        /// <summary>
        /// Auto configure is true by default and is used for the communicator to auto request config data to RFE upon port connection
        /// </summary>
        bool m_bAutoConfigure = true;
        public bool AutoConfigure
        {
            get { return m_bAutoConfigure; }
            set { m_bAutoConfigure = value; }
        }

        double m_fRBWKHZ = 0.0;             //RBW in use
        /// <summary>
        /// RBW in KHZ currently in use, both in analyzer and sniffer
        /// </summary>
        public double RBW_KHZ
        {
            get { return m_fRBWKHZ; }
        }

        float m_fThresholdDBM;
        /// <summary>
        /// Threshold in dBm used for alarm, sniffer capture, etc
        /// </summary>
        public float ThresholdDBM
        {
            get { return m_fThresholdDBM; }
            set { m_fThresholdDBM = value; }
        }

        float m_fOffset_dB = 0.0f;
        /// <summary>
        /// Manual offset of the amplitude reading to compensate external adjustments
        /// </summary>
        public float AmplitudeOffsetDB
        {
            get { return m_fOffset_dB; }
            set { m_fOffset_dB = value; }
        }

        double m_fOffset_MHZ = 0.0f;
        /// <summary>
        /// Manual offset of the frequency reading to compensate external adjustments. Only available in RF Explorer client
        /// </summary>
        public double FrequencyOffsetMHZ
        {
            get { return m_fOffset_MHZ; }
            set { m_fOffset_MHZ = value; }
        }

        bool m_bFreqOffsetInAnalyzer = true;
        /// <summary>
        /// Set True if frequency offset is applied to analyzer and False to generator
        /// </summary>
        public bool FreqOffsetInAnalyzer
        {
            set { m_bFreqOffsetInAnalyzer = value; }
        }

        //Calibration variables
        Byte m_nCalibrationCapSi4x;
        public Byte CalibrationCapSi4x
        {
            get { return m_nCalibrationCapSi4x; }
            set { m_nCalibrationCapSi4x = value; }
        }
        Byte m_nCalibrationCapMixer;
        public Byte CalibrationCapMixer
        {
            get { return m_nCalibrationCapMixer; }
            set { m_nCalibrationCapMixer = value; }
        }
        float m_fCalibrationCC2500OffsetKHZ;
        public float CalibrationCC2500OffsetKHZ
        {
            get { return m_fCalibrationCC2500OffsetKHZ; }
        }

        bool m_bPortConnected = false;
        /// <summary>
        /// Will be true while COM port is connected, as Serial.IsOpen() is not reliable
        /// </summary>
        public bool PortConnected
        {
            get { return m_bPortConnected; }
        }
        /// <summary>
        /// String for name of COM Port
        /// </summary>
        public string PortName
        {
            get { return m_serialPortObj.PortName; }
        }

        private string m_sExternalPort = null;
        /// <summary>
        /// String for COM port name of the other connected device.
        /// </summary>
        public string PortNameExternal
        {
            get { return m_sExternalPort; }
            set { m_sExternalPort = value; }
        }

        /// <summary>
        /// The main data collection for all Tracking mode accumulated data (except normalized response)
        /// </summary>
        RFESweepDataCollection m_TrackingDataContainer;
        public RFESweepDataCollection TrackingData
        {
            get { return m_TrackingDataContainer; }
        }

        RFEBinaryPacketDataCollection m_SnifferBinaryDataContainer = new RFEBinaryPacketDataCollection();
        /// <summary>
        /// The sniffer packet data collection
        /// </summary>
        public RFEBinaryPacketDataCollection SnifferBinaryData
        {
            get
            {
                return m_SnifferBinaryDataContainer;
            }
        }

        RFESweepDataCollection m_SweepDataContainer;
        /// <summary>
        /// The main and only data collection with all the Sweep accumulated data
        /// </summary>
        public RFESweepDataCollection SweepData
        {
            get { return m_SweepDataContainer; }
        }

        RFEScreenDataCollection m_ScreenDataContainer;
        /// <summary>
        /// The main and only collection of screen data
        /// </summary>
        public RFEScreenDataCollection ScreenData
        {
            get { return m_ScreenDataContainer; }
        }

        UInt16 m_nScreenIndex = 0;                  //Index pointing to the latest Dump screen received
        /// <summary>
        /// Current remote screen data position
        /// </summary>
        public UInt16 ScreenIndex
        {
            get { return m_nScreenIndex; }
            set
            {
                if (value > m_ScreenDataContainer.Count)
                {
                    m_nScreenIndex = (UInt16)m_ScreenDataContainer.Count;
                }
                else
                {
                    m_nScreenIndex = value;
                }
            }
        }

        bool m_bCaptureRemoteScreen = false;
        /// <summary>
        /// True only if we want to capture remote screen data
        /// </summary>
        public bool CaptureRemoteScreen
        {
            get { return (m_bCaptureRemoteScreen && !m_bHoldMode); }
            set { m_bCaptureRemoteScreen = value; }
        }

        bool m_bGetAllPorts = false;                //set to true to capture all possible ports regardless OS or versions

        string[] m_arrConnectedPorts;               //Collection of available COM ports
        string[] m_arrValidCP2102Ports;             //Collection of true CP2102 COM ports
                                                    /// <summary>
                                                    /// Internal list of valid ports matching Silabs CP2102, note some of these ports may be already used and not really available
                                                    /// This port names corresponding with complete name in each OS
                                                    /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ValidCP2101Ports
        {
            get { return m_arrValidCP2102Ports; }
        }

        //spectrum analyzer configuration
        double m_fStartFrequencyMHZ = 0.0;
        /// <summary>
        /// Frequency in MHZ for the span start position, used to calculate all other positions together with StepFrequencyMHZ
        /// </summary>
        public double StartFrequencyMHZ
        {
            get { return m_fStartFrequencyMHZ; }
            set { m_fStartFrequencyMHZ = value; }
        }
        /// <summary>
        /// Include offset, it is the frequency in MHZ for the span start position, used to calculate all other positions together with StepFrequencyMHZ
        /// </summary>
        public double StartFrequencyNormalizedMHZ
        {
            get
            {
                if ((m_fStartFrequencyMHZ + m_fOffset_MHZ) < 0)
                {
                    return 0;
                }

                else
                {
                    return m_fStartFrequencyMHZ + m_fOffset_MHZ;
                }
            }
            set
            {
                m_fStartFrequencyMHZ = value - m_fOffset_MHZ;
            }
        }

        double m_fStepFrequencyMHZ = 0.0;
        /// <summary>
        /// Analyzer sweep frequency step in MHZ
        /// </summary>
        public double StepFrequencyMHZ
        {
            get { return m_fStepFrequencyMHZ; }
            set { m_fStepFrequencyMHZ = value; }
        }

        /// <summary>
        /// Calculated span stop frequency in MHZ
        /// </summary>
        public double StopFrequencyMHZ
        {
            get { return StartFrequencyMHZ + StepFrequencyMHZ * FreqSpectrumSteps; }

        }
        /// <summary>
        /// Include frequency offset. Calculates the END or STOP frequency of the span, based on Start / Step values.
        /// </summary>
        /// <returns></returns>
        public double StopFrequencyNormalizedMHZ
        {
            get
            {
                return StopFrequencyMHZ + FrequencyOffsetMHZ;
            }

        }

        /// <summary>
        /// Calculate frequency span, based on Step frequency MHz / Freq Spectrum steps
        /// </summary>
        /// <returns></returns>
        public double CalculateFrequencySpanMHZ()
        {
            return StepFrequencyMHZ * FreqSpectrumSteps;
        }

        /// <summary>
        /// Calculates the center frequency of the span, based on Start / span values.
        /// </summary>
        /// <returns></returns>
        public double CalculateCenterFrequencyMHZ()
        {
            return StartFrequencyMHZ + CalculateFrequencySpanMHZ() / 2.0;
        }
        /// <summary>
        /// Include frequency start offset.Calculates the center frequency of the span, based on Start / span values.
        /// </summary>
        /// <returns></returns>
        public double CalculateCenterFrequencyNormalizedMHZ()
        {
            if (StartFrequencyNormalizedMHZ == 0)
                return StartFrequencyNormalizedMHZ + CalculateFrequencySpanMHZ() / 2.0;
            else
                return CalculateCenterFrequencyMHZ() + FrequencyOffsetMHZ;
        }

        double m_fRefFrequencyMHZ = 0.0;
        /// <summary>
        /// Reference frequency used for sniffer decoder and other zero span functions
        /// </summary>
        public double RefFrequencyMHZ
        {
            get { return m_fRefFrequencyMHZ; }
            set { m_fRefFrequencyMHZ = value; }
        }

        //Signal generator configuration
        double m_fRFGenStartFrequencyMHZ = 0.0;
        /// <summary>
        /// Get/Set Signal Generator sweep start frequency in MHZ.
        /// </summary>
        public double RFGenStartFrequencyMHZ
        {
            get { return m_fRFGenStartFrequencyMHZ; }
            set { m_fRFGenStartFrequencyMHZ = value; }
        }
        double m_fRFGenCWFrequencyMHZ = 0.0;
        /// <summary>
        /// Get/Set Signal Generator CW frequency in MHZ.
        /// </summary>
        public double RFGenCWFrequencyMHZ
        {
            get { return m_fRFGenCWFrequencyMHZ; }
            set { m_fRFGenCWFrequencyMHZ = Math.Round(1000 * value) / 1000.0; }
        }
        double m_fRFGenStopFrequencyMHZ = 0.0;
        /// <summary>
        /// Get/Set Signal Generator sweep stop frequency in MHZ.
        /// </summary>
        public double RFGenStopFrequencyMHZ
        {
            get { return m_fRFGenStopFrequencyMHZ; }
            set { m_fRFGenStopFrequencyMHZ = value; }
        }

        /// <summary>
        /// Get/Set Signal Generator sweep start frequency in MHZ including frequency offset.
        /// </summary>
        public double RFGenStartFrequencyNormalizedMHZ
        {
            get
            {
                if ((m_fRFGenStartFrequencyMHZ + m_fOffset_MHZ) < 0)
                    return 0;
                else
                    return (m_fRFGenStartFrequencyMHZ + m_fOffset_MHZ);
            }
            set { m_fRFGenStartFrequencyMHZ = value - m_fOffset_MHZ; }
        }
        /// <summary>
        /// Get/Set Signal Generator CW frequency in MHZ.
        /// </summary>
        public double RFGenCWFrequencyNormalizedMHZ
        {
            get
            {
                if ((m_fRFGenCWFrequencyMHZ + m_fOffset_MHZ) < 0)
                    return RFECommunicator.RFGEN_MIN_FREQ_MHZ;
                else
                    return (m_fRFGenCWFrequencyMHZ + m_fOffset_MHZ);
            }
            set { m_fRFGenCWFrequencyMHZ = value - m_fOffset_MHZ; }
        }
        /// <summary>
        /// Get/Set Signal Generator sweep stop frequency in MHZ.
        /// </summary>
        public double RFGenStopFrequencyNormalizedMHZ
        {
            get { return m_fRFGenStopFrequencyMHZ + m_fOffset_MHZ; }
            set { m_fRFGenStopFrequencyMHZ = value - m_fOffset_MHZ; }
        }

        UInt16 m_nRFGenSweepSteps = 1;
        /// <summary>
        /// Get/Set Signal Generator sweep steps with valid values in 2-9999.
        /// </summary>
        public UInt16 RFGenSweepSteps
        {
            get { return m_nRFGenSweepSteps; }
            set { m_nRFGenSweepSteps = value; }
        }
        UInt16 m_nRFGenStepWaitMS = 0;
        /// <summary>
        /// Get/Set Signal Generator sweep step wait delay in Milliseconds, with a limit of 65,535 max.
        /// </summary>
        public UInt16 RFGenStepWaitMS
        {
            get { return m_nRFGenStepWaitMS; }
            set { m_nRFGenStepWaitMS = value; }
        }
        bool m_bRFGenHighPowerSwitch = false;
        /// <summary>
        /// Get/Set Signal Generator High Power switch. 
        /// This is combined with RFGenHighPowerSwitch in order to define power level for a CW or Sweep command
        /// </summary>
        public bool RFGenHighPowerSwitch
        {
            get { return m_bRFGenHighPowerSwitch; }
            set { m_bRFGenHighPowerSwitch = value; }
        }
        byte m_nRFGenPowerLevel = 0;
        /// <summary>
        /// Get/Set Signal Generator power level status (0-3). 
        /// This is combined with RFGenHighPowerSwitch in order to define power level for a CW or Sweep command
        /// </summary>
        public byte RFGenPowerLevel
        {
            get { return m_nRFGenPowerLevel; }
            set { m_nRFGenPowerLevel = value; }
        }
        bool m_bRFGenPowerON = false;
        /// <summary>
        /// Get/Set Signal Generator power on status.
        /// </summary>
        public bool RFGenPowerON
        {
            get { return m_bRFGenPowerON; }
        }
        bool m_bRFGenStopHighPowerSwitch = false;
        /// <summary>
        /// Get/Set amplitude sweep stop value for Signal Generator High Power Switch
        /// </summary>
        public bool RFGenStopHighPowerSwitch
        {
            get { return m_bRFGenStopHighPowerSwitch; }
            set { m_bRFGenStopHighPowerSwitch = value; }
        }
        byte m_nRFGenStopPowerLevel = 0;
        /// <summary>
        /// Get/Set amplitude sweep stop value for Signal Generator Power Level (0-3)
        /// </summary>
        public byte RFGenStopPowerLevel
        {
            get { return m_nRFGenStopPowerLevel; }
            set { m_nRFGenStopPowerLevel = value; }
        }

        bool m_bRFGenStartHighPowerSwitch = false;
        /// <summary>
        /// Get/Set amplitude sweep start value for Signal Generator High Power Switch
        /// </summary>
        public bool RFGenStartHighPowerSwitch
        {
            get { return m_bRFGenStartHighPowerSwitch; }
            set { m_bRFGenStartHighPowerSwitch = value; }
        }
        byte m_nRFGenStartPowerLevel = 0;
        /// <summary>
        /// Get/Set amplitude sweep start value for Signal Generator Power Level (0-3)
        /// </summary>
        public byte RFGenStartPowerLevel
        {
            get { return m_nRFGenStartPowerLevel; }
            set { m_nRFGenStartPowerLevel = value; }
        }

        double m_fRFGenExpansionPowerDBM = -100;
        /// <summary>
        /// Get/Set amplitude power level status for generator expansion
        /// </summary>
        public double RFGenExpansionPowerDBM
        {
            get { return m_fRFGenExpansionPowerDBM; }
            set { m_fRFGenExpansionPowerDBM = value; }
        }

        double m_fRFGenExpansionPowerStepDB = 0.25;
        /// <summary>
        /// Get/Set amplitude sweep step for Signal Generator expansion
        /// </summary>
        public double RFGenExpansionPowerStepDB
        {
            get { return m_fRFGenExpansionPowerStepDB; }
            set { m_fRFGenExpansionPowerStepDB = value; }
        }

        double m_fRFGenExpansionPowerStartDBM = -100;
        /// <summary>
        /// Get/Set amplitude sweep start value for Signal Generator expansion
        /// </summary>
        public double RFGenExpansionPowerStartDBM
        {
            get { return m_fRFGenExpansionPowerStartDBM; }
            set { m_fRFGenExpansionPowerStartDBM = value; }
        }

        double m_fRFGenExpansionPowerStopDBM = 15;
        /// <summary>
        /// Get/Set amplitude sweep stop value for Signal Generator expansion
        /// </summary>
        public double RFGenExpansionPowerStopDBM
        {
            get { return m_fRFGenExpansionPowerStopDBM; }
            set { m_fRFGenExpansionPowerStopDBM = value; }
        }

        Queue m_arrReceivedData;         //Queue of strings received from COM port

        Mutex m_ReceivedBytesMutex = new Mutex();
        string m_sDebugAllReceivedBytes = "";         //Debug string for all received bytes record.

        /// <summary>
        /// Debug string collection for all bytes received from device
        /// </summary>
        public string DebugAllReceivedBytes
        {
            get
            {
                m_ReceivedBytesMutex.WaitOne();
                string sReturn = m_sDebugAllReceivedBytes;
                m_ReceivedBytesMutex.ReleaseMutex();
                return sReturn;
            }
        }
        /// <summary>
        /// Clean and reset all debug internal received data bytes
        /// </summary>
        public void CleanReceivedBytes()
        {
            m_ReceivedBytesMutex.WaitOne();
            m_sDebugAllReceivedBytes = "";
            m_ReceivedBytesMutex.ReleaseMutex();
        }

        Thread m_ReceiveThread;    //Thread to process received RS232 activity
        volatile bool m_bRunReceiveThread;          //Run thread (true) or temporarily stop it (false)

        bool m_bHoldMode = false;                   //True when HOLD is active

        public bool HoldMode
        {
            get { return m_bHoldMode; }
            set { m_bHoldMode = value; }
        }

        bool m_bDataFromFile = true;    //True because if there is not decice connected neither a file has been load, app does not try to save RFE file on close
        /// <summary>
        /// False when data is provided from device, true when load RFE file
        /// </summary>
        public bool DataFromFile
        {
            get
            {
                return m_bDataFromFile;
            }
        }


        SerialPort m_serialPortObj;                 //serial port object

        volatile bool m_bDebugTracesSent = false;
        /// <summary>
        /// True when commands sent to RFE must be displayed
        /// </summary>
        public bool DebugSentTracesEnabled
        {
            get
            {
                return m_bDebugTracesSent;
            }

            set
            {
                m_bDebugTracesSent = value;
            }
        }

        volatile bool m_bDebugTraces = false;
        /// <summary>
        /// True when the low level detailed debug traces should be included too
        /// </summary>
        public bool DebugTracesEnabled
        {
            get { return m_bDebugTraces; }
            set { m_bDebugTraces = value; }
        }

        bool m_bDebugTracesGPS = false;
        public bool DebugGPS
        {
            get { return m_bDebugTracesGPS; }
            set { m_bDebugTracesGPS = value; }
        }

        public RFEMemoryBlock[] m_arrFLASH = new RFEMemoryBlock[512];
        public RFEMemoryBlock[] m_arrRAM1 = new RFEMemoryBlock[8];
        public RFEMemoryBlock[] m_arrRAM2 = new RFEMemoryBlock[8];

        bool m_bPartialSweepReceived = false;
        /// <summary>
        /// True when a partial sweep has been received, false when is a complete sweep
        /// </summary>
        public bool PartialSweepReceived
        {
            get
            {
                return m_bPartialSweepReceived;
            }

            set
            {
                m_bPartialSweepReceived = value;
            }
        }
        RFESweepDataPartial m_objPartialSweep;
        /// <summary>
        /// Stores sweep points of a sweep to draw until the full sweep has arrived from the device
        /// </summary>                                 
        public RFESweepDataPartial PartialSweep
        {
            get
            {
                return m_objPartialSweep;
            }

            set
            {
                m_objPartialSweep = value;
            }
        }

        RFESweepDataPartial m_objMaxHoldPartialSweep;
        /// <summary>
        /// Stores sweep points of a max hold sweep to draw until the full sweep has arrived from the device
        /// </summary>                                 
        public RFESweepDataPartial MaxHoldPartialSweep
        {
            get
            {
                return m_objMaxHoldPartialSweep;
            }
        }

        #endregion

        #region Main code

        /// <summary>
        /// Standard Dispose resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool m_bDisposed = false;
        /// <summary>
        /// Local dispose method
        /// </summary>
        /// <param name="bDisposing">if disposing is required</param>
        protected virtual void Dispose(bool bDisposing)
        {
            Close();

            if (!m_bDisposed)
            {
                if (bDisposing)
                {
                    if (m_serialPortObj != null)
                    {
                        if (m_serialPortObj.IsOpen)
                        {
                            m_serialPortObj.Close();
                        }
                        m_serialPortObj.Dispose();
                        m_serialPortObj = null;
                    }
                    if (m_ReceivedBytesMutex != null)
                    {
                        m_ReceivedBytesMutex.Dispose();
                        m_ReceivedBytesMutex = null;
                    }
                }
                m_bDisposed = true;
            }
        }

        /// <summary>
        /// Returns true if the platform is a Unix-like OS (such as Linux, MacOS, etc) - Returns false if it is Windows
        /// </summary>
        public static bool IsUnixLike()
        {
            return Environment.OSVersion.VersionString.Contains("Unix");
        }

        /// <summary>
        /// Constructs a new communicator object. 
        /// </summary>
        /// <param name="bIntendedAnalyzer">True if the intended use is an analyzer, false if is a generator</param>
        public RFECommunicator(bool bIntendedAnalyzer, bool bIoT = false)
        {
            m_bIntendedAnalyzer = bIntendedAnalyzer;
            g_bIsIOT = bIoT;
            InitializeModels();

            if (IsUnixLike())
            {
                m_bUnix = true;
                ReportLog("Running on Unix-like OS computer");
            }
            else
            {
                RegistryKey regWine = Registry.LocalMachine.OpenSubKey("Software\\Wine");
                if (regWine != null)
                {
                    m_bWine = true;
                    ReportLog("Running on Wine - not officially supported");
                }
            }
            if (m_bUnix || m_bWine)
            {
                if (IsRaspberryPlatform())
                    ReportLog("Running on a Raspberry Pi board");
                else if (IsMacOSPlatform())
                {
                    ReportLog("Running on a MacOS computer");
                }
            }

            StoreSweep = true;

            CurrentAmplitudeUnit = eAmplitudeUnit.dBm;

            m_LastCaptureTime = new DateTime(2000, 1, 1);

            m_SweepDataContainer = new RFESweepDataCollection(100 * 1024, true);
            m_TrackingDataContainer = new RFESweepDataCollection(1024, true);
            m_ScreenDataContainer = new RFEScreenDataCollection();
            m_objPartialSweep = new RFESweepDataPartial();
            m_objMaxHoldPartialSweep = new RFESweepDataPartial();

            m_nScreenIndex = 0;

            m_arrReceivedData = new Queue();

            m_bRunReceiveThread = true;
            ThreadStart threadDelegate = new ThreadStart(ReceiveThreadfunc);
            m_ReceiveThread = new Thread(threadDelegate);
            m_ReceiveThread.Start();

            ReportLog("RFECommunicator library started.");

            for (int nInd = 0; nInd < m_arrFLASH.Length; nInd++)
            {
                m_arrFLASH[nInd] = new RFEMemoryBlock();
                m_arrFLASH[nInd].Address = (UInt32)(nInd * RFEMemoryBlock.MAX_BLOCK_SIZE);
            }
            for (int nInd = 0; nInd < m_arrRAM1.Length; nInd++)
            {
                m_arrRAM1[nInd] = new RFEMemoryBlock();
                m_arrRAM1[nInd].Address = (UInt32)(nInd * RFEMemoryBlock.MAX_BLOCK_SIZE);
                m_arrRAM2[nInd] = new RFEMemoryBlock();
                m_arrRAM2[nInd].Address = (UInt32)(nInd * RFEMemoryBlock.MAX_BLOCK_SIZE);
            }
            try
            {
                m_serialPortObj = new SerialPort();
            }
            catch (Exception obEx)
            {
                ReportLog("Error in RFECommunicator constructor: " + obEx.ToString());
            }
        }

        /// <summary>
        /// Close device resources including internal thread and USB RS232 port connection if still alive
        /// </summary>
        public void Close()
        {
            if (m_bRunReceiveThread)
            {
                m_bRunReceiveThread = false;
                Thread.Sleep(1000);
                m_ReceiveThread.Abort();
            }
            ClosePort(m_bAutoConfigure); //send close commands only for autoconfigure mode
        }

        ~RFECommunicator()
        {
            Dispose(false);
        }

        /// <summary>
        /// will return true if the last chars of the file name are same as file extension, regardless capitals
        /// </summary>
        /// <param name="sFilename"></param>
        /// <param name="sFileExtension"></param>
        /// <returns></returns>
        public static bool IsFileExtensionType(string sFilename, string sFileExtension)
        {
            return (sFilename.EndsWith(sFileExtension, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Calculates the END or STOP frequency of the span, based on Start / Step values.
        /// </summary>
        /// <returns></returns>
        public double CalculateEndFrequencyMHZ()
        {
            return StartFrequencyMHZ + StepFrequencyMHZ * FreqSpectrumSteps;
        }

        /// <summary>
        /// Send a new configuration to the connected device
        /// </summary>
        /// <param name="fStartMHZ">New start frequency, in MHZ, must be in valid range for the device</param>
        /// <param name="fEndMHZ">New stop frequency, in MHZ, must be in valid range for the device</param>
        /// <param name="fTopDBM">Optional, only impact visual not real data</param>
        /// <param name="fBottomDBM">Optional, only impact visual not real data</param>
        /// <param name="fRBW_KHZ"></param>
        public void UpdateDeviceConfig(double fStartMHZ, double fEndMHZ, double fTopDBM = 0, double fBottomDBM = -120, double fRBW_KHZ = 0.0f)
        {
            if (m_bPortConnected)
            {
                //#[32]C2-F:Sssssss,Eeeeeee,tttt,bbbb
                UInt32 nStartKhz = (UInt32)(fStartMHZ * 1000);
                UInt32 nEndKhz = (UInt32)(fEndMHZ * 1000);
                Int16 nTopDBM = (Int16)(fTopDBM);
                Int16 nBottomDBM = (Int16)(fBottomDBM);

                string sTopDBM = nTopDBM.ToString("D3");
                string sBottomDBM = nBottomDBM.ToString("D3");
                if (sTopDBM.Length < 4)
                    sTopDBM = "0" + sTopDBM;
                if (sBottomDBM.Length < 4)
                    sBottomDBM = "0" + sBottomDBM;
                string sData = "C2-F:" +
                    nStartKhz.ToString("D7") + "," + nEndKhz.ToString("D7") + "," + sTopDBM + "," + sBottomDBM;
                if (fRBW_KHZ > 0 && fRBW_KHZ >= 3.0f && fRBW_KHZ <= 670.0f)
                {
                    UInt32 nSteps = Convert.ToUInt32(Math.Round((fEndMHZ - fStartMHZ) * 1000.0f / fRBW_KHZ));
                    if (nSteps < RFECommunicator.RFE_MIN_SWEEP_STEPS)
                        nSteps = RFECommunicator.RFE_MIN_SWEEP_STEPS;
                    if (nSteps > MAX_SPECTRUM_STEPS)
                        nSteps = MAX_SPECTRUM_STEPS;
                    fRBW_KHZ = Math.Round((fEndMHZ - fStartMHZ) * 1000.0f) / nSteps;
                    if (fRBW_KHZ >= 3.0f && fRBW_KHZ <= 620.0f)
                        sData += "," + nSteps.ToString("D5");
                    else
                        ReportLog("Ignored RBW " + fRBW_KHZ + "Khz");
                }

                SendCommand(sData);

                Thread.Sleep(500); //wait some time for the unit to process changes, otherwise may get a different command too soon
            }
        }

        /// <summary>
        /// The secondary thread used to get data from USB/RS232 COM port
        /// </summary>
        private void ReceiveThreadfunc()
        {
            //this is the object used to keep current configuration data
            RFEConfiguration objCurrentConfiguration = null;
            RFESweepData objSweepTracking = null;
            m_bThreadTrackingEnabled = false;
            int nTrackingDataPointRetry = 0;

            while (m_bRunReceiveThread)
            {


                string strReceived = "";
                while (m_bPortConnected && m_bRunReceiveThread)
                {
                    string sNewText = "";

                    try
                    {
                        Monitor.Enter(m_serialPortObj);
                        if (m_serialPortObj.IsOpen)
                        {
                            if (m_serialPortObj.BytesToRead > 0)
                                sNewText = m_serialPortObj.ReadExisting();
                        }
                        else
                        {
                            if (m_bPortConnected && m_bAutoClose)
                            {
                                //this means the serial port closed unexpectedly, so need to cleanly close the RFE port
                                Monitor.Enter(m_arrReceivedData);
                                m_arrReceivedData.Enqueue(_INTERNAL_PORTCLOSED_EVENT); //internal communication code to close port
                                Monitor.Exit(m_arrReceivedData);
                            }
                        }
                    }

#if !UNIX_LIKE
                    catch (IOException) { }
#endif
                    catch (TimeoutException) { }
                    catch (Exception obEx)
                    {
                        bool bSerialPortOpen = false;
#if !UNIX_LIKE
                        bSerialPortOpen = m_serialPortObj.IsOpen;
#endif
                        Monitor.Enter(m_arrReceivedData);
                        m_arrReceivedData.Enqueue(obEx);
                        Monitor.Exit(m_arrReceivedData);
                        Monitor.Enter(m_serialPortObj);
                        if (m_bAutoClose && !bSerialPortOpen)
                        {
                            Monitor.Enter(m_arrReceivedData);
                            m_arrReceivedData.Enqueue(_INTERNAL_PORTCLOSED_EVENT); //internal communication code to close port
                            Monitor.Exit(m_arrReceivedData);
                        }
                        Monitor.Exit(m_serialPortObj);
                    }
                    finally { Monitor.Exit(m_serialPortObj); }

                    if (sNewText.Length > 0)
                    {
                        if (m_bDebugTraces)
                        {
                            //Debug only, do not enable this in production
                            m_ReceivedBytesMutex.WaitOne();
                            m_sDebugAllReceivedBytes += sNewText;
                            m_ReceivedBytesMutex.ReleaseMutex();
                        }
                        strReceived += sNewText;
                        sNewText = "";
                    }
                    if (strReceived.Length > 66 * 1024)
                    {
                        //Safety code, some error prevented the string from being processed in several loop cycles. Reset it.
                        if (m_bDebugTraces)
                        {
                            Monitor.Enter(m_arrReceivedData);
                            m_arrReceivedData.Enqueue("Received string truncated (" + strReceived.Length + ")");
                            Monitor.Exit(m_arrReceivedData);
                        }
                        strReceived = "";
                    }
                    if (strReceived.Length > 0)
                    {
                        if (strReceived[0] == '#')
                        {
                            int nEndPos = strReceived.IndexOf("\r\n");
                            if (nEndPos >= 0)
                            {
                                string sNewLine = strReceived.Substring(0, nEndPos);
                                string sLeftOver = strReceived.Substring(nEndPos + 2);
                                strReceived = sLeftOver;

                                RFEConfiguration objNewConfiguration = null;
                                if ((sNewLine.Length > 2) && sNewLine.StartsWith("#K1"))
                                {
                                    if (m_bThreadTrackingEnabled == false)
                                    {
                                        //if we are starting tracking here, send the request for first step right away
                                        m_objRFEGen.SendCommand_TrackingDataPoint(0);
                                        Thread.Sleep(100);
                                        SendCommand_TrackingDataPoint(0);
                                    }
                                    m_bThreadTrackingEnabled = true;
                                    nTrackingDataPointRetry = 0;

                                    Monitor.Enter(m_arrReceivedData);
                                    m_arrReceivedData.Enqueue(sNewLine);
                                    Monitor.Exit(m_arrReceivedData);
                                }
                                if ((sNewLine.Length > 2) && sNewLine.StartsWith("#K0"))
                                {
                                    m_bThreadTrackingEnabled = false;

                                    Monitor.Enter(m_arrReceivedData);
                                    m_arrReceivedData.Enqueue(sNewLine);
                                    Monitor.Exit(m_arrReceivedData);
                                }
                                else if ((sNewLine.Length > 5) &&
                                        (sNewLine.StartsWith("#C2-F:") || sNewLine.StartsWith("#C2-f:") ||
                                        (sNewLine.StartsWith("#C3-") && (sNewLine[4] != 'M')) || sNewLine.StartsWith("#C4-F:")) ||
                                        sNewLine.StartsWith("#C5-"))
                                {
                                    m_bThreadTrackingEnabled = false;

                                    if (m_bDebugTraces)
                                    {
                                        Monitor.Enter(m_arrReceivedData);
                                        m_arrReceivedData.Enqueue("Received Config:" + strReceived.Length.ToString("D5"));
                                        Monitor.Exit(m_arrReceivedData);
                                    }

                                    //Standard configuration expected
                                    objNewConfiguration = new RFEConfiguration();
                                    if (objNewConfiguration.ProcessReceivedString(sNewLine))
                                    {
                                        objCurrentConfiguration = new RFEConfiguration(objNewConfiguration);
                                        Monitor.Enter(m_arrReceivedData);
                                        m_arrReceivedData.Enqueue(objNewConfiguration);
                                        Monitor.Exit(m_arrReceivedData);
                                        //When received new configuration, partialSweep must be reinitialized
                                        m_objPartialSweep.SetNewConfiguration(objCurrentConfiguration.fStartMHZ, objCurrentConfiguration.fStepMHZ, (ushort)(objCurrentConfiguration.FreqSpectrumSteps + 1));
                                        m_objMaxHoldPartialSweep.SetNewConfiguration(objCurrentConfiguration.fStartMHZ, objCurrentConfiguration.fStepMHZ, (ushort)(objCurrentConfiguration.FreqSpectrumSteps + 1));
                                    }
                                }
                                else
                                {
                                    Monitor.Enter(m_arrReceivedData);
                                    m_arrReceivedData.Enqueue(sNewLine);
                                    Monitor.Exit(m_arrReceivedData);
                                }
                            }
                        }
                        else if (strReceived[0] == '$')
                        {
                            if ((strReceived.Length > 4) && (strReceived[1] == 'C'))
                            {
                                UInt16 nSize = 2; //account for CR+LF

                                switch (strReceived[2])
                                {
                                    case 'd':
                                    case 'c':
                                        {
                                            //Calibration data
                                            nSize += (byte)(Convert.ToByte(strReceived[3]) + 4);
                                            break;
                                        }
                                    case 'b':
                                        {
                                            //Memory data dump
                                            nSize += (UInt16)((Convert.ToByte(strReceived[4]) + 1) * 16 + 10);
                                            break;
                                        }
                                }
                                //This is standard for all 'C' data dumps
                                if ((nSize > 2) && (strReceived.Length >= nSize))
                                {
                                    string sNewLine = strReceived.Substring(0, nSize);
                                    string sLeftOver = strReceived.Substring(nSize);
                                    strReceived = sLeftOver;
                                    Monitor.Enter(m_arrReceivedData);
                                    m_arrReceivedData.Enqueue(sNewLine);
                                    Monitor.Exit(m_arrReceivedData);
                                }
                            }
                            else if ((strReceived.Length > 3) && ((strReceived[1] == 'q') || (strReceived[1] == 'Q')))
                            {
                                //this is internal calibration data dump
                                ushort nReceivedLength = (byte)strReceived[2];
                                int nExtraLength = 3;
                                if (strReceived[1] == 'Q')
                                {
                                    nReceivedLength += (ushort)(0x100 * (byte)strReceived[3]);
                                    nExtraLength = 4;
                                }
                                UInt16 nLengthCal = (UInt16)(nExtraLength + nReceivedLength + 2);
                                bool bLengthOK = (strReceived.Length >= nLengthCal);
                                if (bLengthOK)
                                {
                                    Monitor.Enter(m_arrReceivedData);
                                    m_arrReceivedData.Enqueue(strReceived.Substring(0, nLengthCal));
                                    Monitor.Exit(m_arrReceivedData);
                                    strReceived = strReceived.Substring(nLengthCal);
                                }
                            }
                            else if ((strReceived.Length > 1) && (strReceived[1] == 'D'))
                            {
                                //This is dump screen data
                                if (m_bDebugTraces)
                                {
                                    Monitor.Enter(m_arrReceivedData);
                                    m_arrReceivedData.Enqueue("Received $D" + strReceived.Length.ToString("D5"));
                                    Monitor.Exit(m_arrReceivedData);
                                }

                                if (strReceived.Length >= (4 + 128 * 8))
                                {
                                    string sNewLine = "$D" + strReceived.Substring(2, 128 * 8);
                                    string sLeftOver = strReceived.Substring(4 + 128 * 8);
                                    strReceived = sLeftOver;
                                    RFEScreenData objData = new RFEScreenData();
                                    if (objData.ProcessReceivedString(sNewLine))
                                    {
                                        Monitor.Enter(m_arrReceivedData);
                                        m_arrReceivedData.Enqueue(objData);
                                        Monitor.Exit(m_arrReceivedData);
                                    }
                                    else
                                    {
                                        Monitor.Enter(m_arrReceivedData);
                                        m_arrReceivedData.Enqueue(sNewLine);
                                        Monitor.Exit(m_arrReceivedData);
                                    }
                                }
                            }
                            else if ((strReceived.Length > 100) && (strReceived[1] == 'Z'))
                            {
                                //This is zero span data
                                if (m_bDebugTraces)
                                {
                                    Monitor.Enter(m_arrReceivedData);
                                    m_arrReceivedData.Enqueue("Received $Z" + strReceived.Length.ToString("D5"));
                                    Monitor.Exit(m_arrReceivedData);
                                }

                                if (strReceived.Length >= (100 + 4))
                                {
                                    string sNewLine = "Zero span: ";
                                    for (int nInd = 0; nInd < 100; nInd++)
                                    {
                                        byte nData = Convert.ToByte(strReceived[2 + nInd]);
                                        double dDBM = Convert.ToDouble(nData / -2.0);
                                        sNewLine += " " + dDBM.ToString("f1");
                                    }
                                    string sLeftOver = strReceived.Substring(100 + 5);
                                    strReceived = sLeftOver;

                                    Monitor.Enter(m_arrReceivedData);
                                    m_arrReceivedData.Enqueue(sNewLine);
                                    Monitor.Exit(m_arrReceivedData);
                                }
                            }
                            else if ((strReceived.Length > 3) && ((strReceived[1] == 'S') || (strReceived[1] == 's') || (strReceived[1] == 'z')))
                            {
                                //Standard spectrum analyzer data
                                ushort nReceivedLength = (byte)strReceived[2];
                                int nSizeChars = 3;
                                if (strReceived[1] == 's')
                                {
                                    if (nReceivedLength == 0)
                                        nReceivedLength = 256;
                                    nReceivedLength = (ushort)(nReceivedLength * 16);
                                }
                                else if (strReceived[1] == 'z')
                                {
                                    nReceivedLength *= 256;
                                    nReceivedLength += (byte)strReceived[3];
                                    nSizeChars++;
                                }

                                if (m_bDebugTraces)
                                {
                                    Monitor.Enter(m_arrReceivedData);
                                    m_arrReceivedData.Enqueue("Received $S " + nReceivedLength.ToString("D4") + " " + strReceived.Length.ToString("D4"));
                                    Monitor.Exit(m_arrReceivedData);
                                }

                                bool bLengthOK = (strReceived.Length >= (nSizeChars + nReceivedLength + 2));
                                bool bEEOT = false;
                                if (!bLengthOK)
                                {
                                    //Check if not all bytes were received but EEOT is detected.
                                    if (strReceived.Contains(_EEOT))
                                    {
                                        bEEOT = true;
                                        if (m_bDebugTraces)
                                        {
                                            Monitor.Enter(m_arrReceivedData);
                                            m_arrReceivedData.Enqueue("EEOT detected");
                                            Monitor.Exit(m_arrReceivedData);
                                        }
                                        //If EEOT detected, remove from received string so we ignore the partially received data
                                        strReceived = strReceived.Substring(strReceived.IndexOf(_EEOT) + _EEOT.Length);
                                    }
                                }
                                bool bFullStringOK = false;
                                if (bLengthOK && (strReceived.Substring(nSizeChars + nReceivedLength, 2) == "\r\n"))
                                {
                                    bFullStringOK = true;
                                }

                                if (m_bDebugTraces)
                                {
                                    Monitor.Enter(m_arrReceivedData);
                                    m_arrReceivedData.Enqueue("bFullStringOK:" + bFullStringOK.ToString() + " bLengthOK:" + bLengthOK.ToString());
                                    Monitor.Exit(m_arrReceivedData);
                                }

                                if (bFullStringOK)
                                {
                                    //So we are here because received the full set of chars expected, and all them are apparently of valid characters
                                    if (nReceivedLength <= MAX_SPECTRUM_STEPS)
                                    {
                                        string sNewLine = "$S" + strReceived.Substring(nSizeChars, nReceivedLength);
                                        if (objCurrentConfiguration != null)
                                        {
                                            UInt16 nSweepDataPoints = (UInt16)(objCurrentConfiguration.FreqSpectrumSteps + 1);
                                            if (m_bThreadTrackingEnabled)
                                            {
                                                nSweepDataPoints = nReceivedLength;
                                            }

                                            double fConfigurationStartMHZ = objCurrentConfiguration.fStartMHZ + m_fOffset_MHZ; //Not allowed frequencies below zero MHz
                                            if (fConfigurationStartMHZ < 0)
                                                fConfigurationStartMHZ = 0;
                                            RFESweepData objSweep = new RFESweepData(fConfigurationStartMHZ, objCurrentConfiguration.fStepMHZ, nSweepDataPoints);
                                            //If using LNA or Attenuator, add required adjust to compensate from offset value sent from device
                                            Int32 nInputStageOffset = 0;

                                            if ((m_eInputStage != eInputStage.Direct) && IsAnalyzerEmbeddedCal() && (!IsMWSUB3G))
                                                nInputStageOffset = Convert.ToInt32(InputStageAttenuationDB);

                                            if (objSweep.ProcessReceivedString(sNewLine, (objCurrentConfiguration.fOffset_dB + nInputStageOffset), m_bUseByteBLOB, m_bUseStringBLOB))
                                            {
                                                if (!m_bThreadTrackingEnabled)
                                                {
                                                    if (m_bDebugTraces)
                                                    {
                                                        Monitor.Enter(m_arrReceivedData);
                                                        m_arrReceivedData.Enqueue(objSweep.Dump());
                                                        Monitor.Exit(m_arrReceivedData);
                                                    }
                                                    if (nSweepDataPoints > 5) //check this is not an incomplete scan (perhaps from a stopped SNA tracking step)
                                                    {
                                                        //Normal spectrum analyzer sweep data
                                                        Monitor.Enter(m_arrReceivedData);
                                                        m_arrReceivedData.Enqueue(objSweep);
                                                        Monitor.Exit(m_arrReceivedData);
                                                    }
                                                }
                                                else
                                                {
                                                    if (m_nRFGenTracking_CurrentSweepDataPoint == 0)
                                                    {
                                                        //This frequency offset is for analyzer. We have to use TotalDataPoint in the RFESweepData constructor for making SNA tracking completely (if we use TotalSteps, tracking would have one point less)
                                                        objSweepTracking = new RFESweepData(m_objRFEGen.RFGenStartFrequencyMHZ + FrequencyOffsetMHZ, m_objRFEGen.RFGenStepMHZ(), (UInt16)(m_objRFEGen.RFGenSweepSteps + 1));
                                                    }
                                                    if (objSweep.TotalDataPoints == 3) //print cases where the mid point is not the highest value which may indicate tuning problem
                                                    {
                                                        if (objSweep.GetAmplitudeDBM(0) > objSweep.GetAmplitudeDBM(1) || objSweep.GetAmplitudeDBM(2) > objSweep.GetAmplitudeDBM(1))
                                                        {
                                                            m_arrReceivedData.Enqueue("Data point " + m_nRFGenTracking_CurrentSweepDataPoint + ": " + objSweep.Dump());
                                                        }
                                                    }
                                                    float fMaxDB = objSweep.GetAmplitudeDBM(objSweep.GetPeakDataPoint());
                                                    objSweepTracking.SetAmplitudeDBM(m_nRFGenTracking_CurrentSweepDataPoint, fMaxDB);

                                                    if (!m_bTrackingNormalizing || (fMaxDB > MIN_AMPLITUDE_TRACKING_NORMALIZE + AmplitudeOffsetDB + nInputStageOffset) || !m_bTrackingAllowed)
                                                    {
                                                        //if we are normalizing, make sure the value read is correct or either do not increase data point
                                                        m_nRFGenTracking_CurrentSweepDataPoint++;
                                                    }
                                                    else
                                                    {
                                                        nTrackingDataPointRetry++;
                                                        if (m_bTrackingNormalizing && nTrackingDataPointRetry > ((UInt16)(m_objRFEGen.RFGenSweepSteps + 1) / 5))
                                                        {
                                                            //if we retried about the same number of data points the sweep have, then something is really wrong
                                                            m_objRFEGen.SendCommand_GeneratorRFPowerOFF();
                                                            m_bThreadTrackingEnabled = false; //be done with thread tracking activity, so main thread knows
                                                            Monitor.Enter(m_arrReceivedData);
                                                            m_arrReceivedData.Enqueue("Too many retries normalizing data. Review your setup and restart Spectrum Analyzer");
                                                            m_arrReceivedData.Enqueue(objSweepTracking); //send whatever we have, we will detect it outside the thread
                                                            Monitor.Exit(m_arrReceivedData);
                                                        }
                                                    }
                                                    if (m_bThreadTrackingEnabled)
                                                    {
                                                        ushort nGenSweepDataPoints = 0;
                                                        //We are using DataPoints at this point
                                                        nGenSweepDataPoints = (UInt16)(m_objRFEGen.RFGenSweepSteps + 1);

                                                        if (m_nRFGenTracking_CurrentSweepDataPoint < nGenSweepDataPoints)
                                                        {
                                                            if (m_bTrackingAllowed)
                                                            {
                                                                m_objRFEGen.SendCommand_TrackingDataPoint(m_nRFGenTracking_CurrentSweepDataPoint);
                                                                SendCommand_TrackingDataPoint(m_nRFGenTracking_CurrentSweepDataPoint);
                                                            }
                                                            else
                                                            {
                                                                //we manually stopped tracking before a full capture completed, so stop right now
                                                                m_objRFEGen.SendCommand_GeneratorRFPowerOFF();
                                                                m_bThreadTrackingEnabled = false; //be done with thread tracking activity, so main thread knows
                                                            }
                                                        }
                                                        else
                                                        {
                                                            //we are done with a tracking sweep capture objSweepTracking, make it available
                                                            m_nTrackingNormalizingPass++;
                                                            m_nTrackingPass++;

                                                            Monitor.Enter(m_arrReceivedData);
                                                            m_arrReceivedData.Enqueue(objSweepTracking);
                                                            Monitor.Exit(m_arrReceivedData);

                                                            //If we need to restart, do it from first Data point
                                                            m_nRFGenTracking_CurrentSweepDataPoint = 0;
                                                            if ((m_bTrackingNormalizing && (m_nTrackingNormalizingPass > m_nAutoStopSNATrackingCounter)) ||
                                                                 !m_bTrackingAllowed ||
                                                                 (!m_bTrackingNormalizing && (m_nAutoStopSNATrackingCounter != 0) && (m_nTrackingPass >= m_nAutoStopSNATrackingCounter))
                                                               )
                                                            {
                                                                //if normalizing is completed, or if we have finished tracking manually or automatically, we are done with RF power
                                                                m_objRFEGen.SendCommand_GeneratorRFPowerOFF();
                                                                m_bThreadTrackingEnabled = false; //be done with thread tracking activity, so main thread knows
                                                            }
                                                            else
                                                            {
                                                                m_objRFEGen.SendCommand_TrackingDataPoint(0);
                                                                Thread.Sleep(50);
                                                                SendCommand_TrackingDataPoint(0);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Monitor.Enter(m_arrReceivedData);
                                                m_arrReceivedData.Enqueue(sNewLine);
                                                Monitor.Exit(m_arrReceivedData);
                                            }
                                        }
                                        else
                                        {
                                            if (m_bDebugTraces)
                                            {
                                                Monitor.Enter(m_arrReceivedData);
                                                m_arrReceivedData.Enqueue("Configuration not available yet. $S string ignored.");
                                                Monitor.Exit(m_arrReceivedData);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Monitor.Enter(m_arrReceivedData);
                                        m_arrReceivedData.Enqueue("Ignored $S of size " + nReceivedLength.ToString() + " expected " + (FreqSpectrumSteps + 1).ToString());
                                        Monitor.Exit(m_arrReceivedData);
                                    }
                                    strReceived = strReceived.Substring(nSizeChars + nReceivedLength + 2);
                                    if (m_bDebugTraces)
                                    {
                                        string sText = "New String: ";
                                        int nLength = strReceived.Length;
                                        if (nLength > 10)
                                            nLength = 10;
                                        if (nLength > 0)
                                            sText += strReceived.Substring(0, nLength);
                                        Monitor.Enter(m_arrReceivedData);
                                        m_arrReceivedData.Enqueue(sText);
                                        Monitor.Exit(m_arrReceivedData);
                                    }
                                }
                                else if (bLengthOK)
                                {
                                    //So we are here because the string doesn't end with the expected chars, but has the right length. 
                                    //The most likely cause is a truncated string was received, and some chars are from next string, not this one
                                    //therefore we truncate the line to avoid being much larger, and start over again next time.
                                    int nPosNextLine = strReceived.IndexOf("\r\n");
                                    if (nPosNextLine >= 0)
                                    {
                                        strReceived = strReceived.Substring(nPosNextLine + 2);
                                    }
                                }
                                else if (!bEEOT)
                                {
                                    //Here we are in the case that we have received a partial sweep, the end of received string is not \r\n
                                    m_bPartialSweepReceived = false;
                                    if (nReceivedLength <= MAX_SPECTRUM_STEPS)
                                    {
                                        string sNewPartialLine = "$S" + strReceived.Substring(nSizeChars, strReceived.Length - nSizeChars);
                                        if (objCurrentConfiguration != null)
                                        {
                                            double fConfigurationStartMHZ = objCurrentConfiguration.fStartMHZ + m_fOffset_MHZ; //Not allowed frequencies below zero MHz
                                            if (fConfigurationStartMHZ < 0)
                                                fConfigurationStartMHZ = 0;

                                            //If using LNA or Attenuator, add required adjust to compensate from offset value sent from device
                                            Int32 nInputStageOffset = 0;

                                            if ((m_eInputStage != eInputStage.Direct) && IsAnalyzerEmbeddedCal() && (!IsMWSUB3G))
                                                nInputStageOffset = Convert.ToInt32(InputStageAttenuationDB);
                                            m_objPartialSweep.AvailableDataPoints = (ushort)(sNewPartialLine.Length - 2);

                                            if (m_objPartialSweep.ProcessReceivedPartialString(sNewPartialLine, (objCurrentConfiguration.fOffset_dB + nInputStageOffset), m_objPartialSweep.AvailableDataPoints))
                                            {
                                                m_objMaxHoldPartialSweep.AddMaxHoldData(m_objPartialSweep);
                                                m_bPartialSweepReceived = true;
                                            }
                                        }
                                    }
                                }
                            }
                            else if ((strReceived.Length > 10) && (strReceived[1] == 'R'))
                            {
                                //Raw data
                                int nSize = strReceived[2] + strReceived[3] * 0x100;
                                if (strReceived.Length >= (nSize + 6))
                                {
                                    Monitor.Enter(m_arrReceivedData);
                                    m_arrReceivedData.Enqueue("Received RAW data " + nSize.ToString());
                                    m_arrReceivedData.Enqueue("$R" + strReceived.Substring(4, nSize));
                                    Monitor.Exit(m_arrReceivedData);
                                    strReceived = strReceived.Substring(nSize + 6);
                                }
                            }
                        }
                        else
                        {
                            int nEndPos = strReceived.IndexOf("\r\n");
                            if (nEndPos >= 0)
                            {
                                string sNewLine = strReceived.Substring(0, nEndPos);
                                string sLeftOver = strReceived.Substring(nEndPos + 2);
                                strReceived = sLeftOver;
                                Monitor.Enter(m_arrReceivedData);
                                m_arrReceivedData.Enqueue(sNewLine);
                                Monitor.Exit(m_arrReceivedData);
                            }
                            else
                            {
                                //diagnosis only
                                if (m_bDebugTraces)
                                {
                                    Monitor.Enter(m_arrReceivedData);
                                    m_arrReceivedData.Enqueue("DEBUG partial:" + strReceived);
                                    Monitor.Exit(m_arrReceivedData);
                                }
                            }
                        }
                    }
                    if (m_eMode != eMode.MODE_TRACKING)
                        Thread.Sleep(10);
                    else
                        Thread.Sleep(2); //in tracking mode we want to be as fast as possible
                }

                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Manage number of sweeps to calculate average time between 10 sweeps,
        /// it increase when a valid sweep is received
        /// </summary>
        int m_nAverageSweepSpeedIterator = 0;

        /// <summary>
        /// Store capture time between consecutive sweeps
        /// </summary>
        TimeSpan m_spanAverageSpeedAcumulator = TimeSpan.Zero;

        double m_fAverageSweepSeconds = 0.0;
        /// <summary>
        /// Provide average sweep/seconds information over first two sweeps when device starts to capture data and 
        /// 10 sweeps for remaining captures or over all available sweeps for data from file
        /// </summary>
        public double AverageSweepSeconds
        {
            get
            {
                return m_fAverageSweepSeconds;
            }
        }

        /// <summary>
        /// Processes all strings received and queued by the ReceiveThreadFunc
        /// </summary>
        /// <param name="bProcessAllEvents">If bProcessAllEvents==false then only one event will be processed, otherwise will do all that are waiting on the queue</param>
        /// <param name="sReceivedString">sReceivedString will have the last processed string from the queue</param>
        /// <returns>Returns true if an event was received requiring redraw</returns>
        public bool ProcessReceivedString(bool bProcessAllEvents, out string sReceivedString)
        {
            bool bDraw = false;
            sReceivedString = "";

            if (m_bPortConnected)
            {
                try
                {
                    do
                    {
                        bool bWrongFormat = false;
                        object objNew = null;
                        long nCount = 0;

                        try
                        {
                            Monitor.Enter(m_arrReceivedData);
                            nCount = m_arrReceivedData.Count;

                            if (nCount == 0)
                                break;
                            objNew = m_arrReceivedData.Dequeue();
                        }
                        catch (Exception obEx)
                        {
                            ReportLog("m_arrReceivedStrings processing: " + obEx.ToString());
                        }
                        finally
                        {
                            Monitor.Exit(m_arrReceivedData);
                        }

                        if (objNew.GetType() == typeof(RFEConfiguration))
                        {
                            RFEConfiguration objConfiguration = (RFEConfiguration)objNew;
                            ReportLog("Received configuration: " + objConfiguration.sLineString);

                            if (IsGenerator())
                            {
                                //it is a signal generator
                                if (m_RFGenCal.GetCalSize() < 0)
                                {
                                    //request internal calibration data, if available
                                    if (m_nRetriesCalibration < 3)
                                    {
                                        SendCommand("Cq");
                                        m_nRetriesCalibration++;
                                    }
                                }

                                //signal generator
                                m_eMode = objConfiguration.eMode;
                                m_bRFGenPowerON = objConfiguration.bRFEGenPowerON;
                                double fStepMHz = 0.0f; //For normalization file older than v004, we do not use step but start/stop. Here we are using to calculate stop value
                                switch (m_eMode)
                                {
                                    case eMode.MODE_GEN_CW:
                                        RFGenCWFrequencyNormalizedMHZ = objConfiguration.fRFEGenCWFreqMHZ;
                                        fStepMHz = objConfiguration.fStepMHZ;
                                        if (m_bExpansionBoardActive)
                                        {
                                            //Fix to 0.25 multiple, as the code coming from RFGEN is not including last char and miss 0.25 or 0.75 and would display as 0.20 or 0.70
                                            double fDecimal = Math.Abs(objConfiguration.fRFEGenExpansionPowerDBM - Math.Truncate(objConfiguration.fRFEGenExpansionPowerDBM));
                                            if (fDecimal > 0.01)
                                            {
#pragma warning disable 642
                                                if ((fDecimal - 0.2) < 0.01)
                                                    fDecimal = 0.25;
                                                else if ((fDecimal - 0.5) < 0.01)
                                                    ; //nothing to adjust
                                                else if ((fDecimal - 0.7) < 0.01)
                                                    fDecimal = 0.75;
#pragma warning restore 642
                                            }
                                            if (objConfiguration.fRFEGenExpansionPowerDBM < 0)
                                                RFGenExpansionPowerDBM = Math.Truncate(objConfiguration.fRFEGenExpansionPowerDBM) - fDecimal;
                                            else
                                                RFGenExpansionPowerDBM = Math.Truncate(objConfiguration.fRFEGenExpansionPowerDBM) + fDecimal;
                                        }
                                        else
                                        {
                                            RFGenPowerLevel = objConfiguration.nRFEGenPowerLevel;
                                            RFGenHighPowerSwitch = objConfiguration.bRFEGenHighPowerSwitch;
                                        }
                                        break;
                                    case eMode.MODE_GEN_SWEEP_FREQ:
                                        RFGenStartFrequencyNormalizedMHZ = objConfiguration.fStartMHZ;
                                        fStepMHz = objConfiguration.fStepMHZ;
                                        RFGenSweepSteps = objConfiguration.FreqSpectrumSteps;
                                        RFGenStopFrequencyNormalizedMHZ = RFGenStartFrequencyNormalizedMHZ + RFGenSweepSteps * fStepMHz;
                                        if (m_bExpansionBoardActive)
                                        {
                                            RFGenExpansionPowerDBM = objConfiguration.fRFEGenExpansionPowerDBM;
                                        }
                                        else
                                        {
                                            RFGenPowerLevel = objConfiguration.nRFEGenPowerLevel;
                                            RFGenHighPowerSwitch = objConfiguration.bRFEGenHighPowerSwitch;
                                        }
                                        RFGenStepWaitMS = objConfiguration.nRFEGenSweepWaitMS;
                                        break;
                                    case eMode.MODE_GEN_SWEEP_AMP:
                                        RFGenCWFrequencyNormalizedMHZ = objConfiguration.fRFEGenCWFreqMHZ;
                                        RFGenStepWaitMS = objConfiguration.nRFEGenSweepWaitMS;
                                        if (m_bExpansionBoardActive)
                                        {
                                            RFGenExpansionPowerStepDB = objConfiguration.fRFEGenExpansionPowerStepDBM;
                                            RFGenExpansionPowerStartDBM = objConfiguration.fRFEGenExpansionPowerStartDBM;
                                            RFGenExpansionPowerStopDBM = objConfiguration.fRFEGenExpansionPowerStopDBM;
                                        }
                                        else
                                        {
                                            RFGenStartHighPowerSwitch = objConfiguration.bRFEGenStartHighPowerSwitch;
                                            RFGenStartPowerLevel = objConfiguration.nRFEGenStartPowerLevel;
                                            RFGenStopHighPowerSwitch = objConfiguration.bRFEGenStopHighPowerSwitch;
                                            RFGenStopPowerLevel = objConfiguration.nRFEGenStopPowerLevel;
                                        }
                                        break;
                                    case eMode.MODE_NONE:
                                        if (objConfiguration.fStartMHZ > 0)
                                        {
                                            //if eMode.MODE_NONE and fStartMHZ has some meaningful value, it means
                                            //we are receiving a C3-* full status update
                                            RFGenCWFrequencyNormalizedMHZ = objConfiguration.fRFEGenCWFreqMHZ;
                                            RFGenHighPowerSwitch = objConfiguration.bRFEGenHighPowerSwitch;
                                            RFGenStartFrequencyNormalizedMHZ = objConfiguration.fStartMHZ;
                                            fStepMHz = objConfiguration.fStepMHZ;
                                            RFGenSweepSteps = objConfiguration.FreqSpectrumSteps;
                                            RFGenStopFrequencyNormalizedMHZ = RFGenStartFrequencyNormalizedMHZ + RFGenSweepSteps * fStepMHz;
                                            if (m_bExpansionBoardActive)
                                            {
                                                RFGenExpansionPowerDBM = objConfiguration.fRFEGenExpansionPowerDBM;
                                                RFGenExpansionPowerStepDB = objConfiguration.fRFEGenExpansionPowerStepDBM;
                                                RFGenExpansionPowerStartDBM = objConfiguration.fRFEGenExpansionPowerStartDBM;
                                                RFGenExpansionPowerStopDBM = objConfiguration.fRFEGenExpansionPowerStopDBM;
                                            }
                                            else
                                            {
                                                RFGenPowerLevel = objConfiguration.nRFEGenPowerLevel;
                                                RFGenStartHighPowerSwitch = objConfiguration.bRFEGenStartHighPowerSwitch;
                                                RFGenStartPowerLevel = objConfiguration.nRFEGenStartPowerLevel;
                                                RFGenStopHighPowerSwitch = objConfiguration.bRFEGenStopHighPowerSwitch;
                                                RFGenStopPowerLevel = objConfiguration.nRFEGenStopPowerLevel;
                                            }
                                            RFGenStepWaitMS = objConfiguration.nRFEGenSweepWaitMS;
                                        }
                                        else
                                            ReportLog("Unknown Signal Generator configuration received");
                                        break;
                                    default:
                                        break;
                                }
                                objConfiguration.fStepMHZ = 0; //From here, it is not actually used. We will use Start/Stop value.

                                //We know what board is active when C3-M is recieved
                                if (m_bExpansionBoardActive)
                                {
                                    m_eActiveModel = m_eExpansionBoardModel;
                                    MinFreqMHZ = RFECommunicator.RFGENEXP_MIN_FREQ_MHZ;
                                }
                                else
                                {
                                    m_eActiveModel = m_eMainBoardModel;
                                    MinFreqMHZ = RFECommunicator.RFGEN_MIN_FREQ_MHZ;
                                }
                                MaxFreqMHZ = RFECommunicator.RFGEN_MAX_FREQ_MHZ;

                                OnReceivedConfigurationData(new EventArgs());
                            }
                            else
                            {
                                //it is an spectrum analyzer

                                m_eMode = objConfiguration.eMode;
                                //spectrum analyzer
                                if (m_eMode != RFECommunicator.eMode.MODE_SNIFFER)
                                {
                                    if ((Math.Abs(StartFrequencyMHZ - objConfiguration.fStartMHZ) >= 0.001) || (Math.Abs(StepFrequencyMHZ - objConfiguration.fStepMHZ) >= 0.000001))
                                    {
                                        StartFrequencyMHZ = objConfiguration.fStartMHZ;
                                        StepFrequencyMHZ = objConfiguration.fStepMHZ;
                                        ReportLog("New Freq range - buffer cleared.");
                                    }
                                    AmplitudeTopDBM = objConfiguration.fAmplitudeTopDBM;
                                    AmplitudeBottomDBM = objConfiguration.fAmplitudeBottomDBM;
                                    FreqSpectrumSteps = objConfiguration.FreqSpectrumSteps;
                                }
                                m_bExpansionBoardActive = objConfiguration.bExpansionBoardActive;
                                if (m_bExpansionBoardActive)
                                {
                                    m_eActiveModel = m_eExpansionBoardModel;
                                }
                                else
                                {
                                    m_eActiveModel = m_eMainBoardModel;
                                }

                                m_eMode = objConfiguration.eMode;
                                m_bExpansionBoardActive = objConfiguration.bExpansionBoardActive;
                                if (m_bExpansionBoardActive)
                                {
                                    m_eActiveModel = m_eExpansionBoardModel;
                                }
                                else
                                {
                                    m_eActiveModel = m_eMainBoardModel;
                                }

                                if (objConfiguration.eMode == eMode.MODE_SNIFFER)
                                {
                                    m_nBaudrate = objConfiguration.nBaudrate;
                                    m_fThresholdDBM = objConfiguration.fThresholdDBM;
                                    m_fRefFrequencyMHZ = objConfiguration.fStartMHZ;
                                }
                                else
                                {
                                    //spectrum analyzer
                                    if ((Math.Abs(StartFrequencyMHZ - objConfiguration.fStartMHZ) >= 0.001) || (Math.Abs(StepFrequencyMHZ - objConfiguration.fStepMHZ) >= 0.000001))
                                    {
                                        StartFrequencyMHZ = objConfiguration.fStartMHZ;
                                        StepFrequencyMHZ = objConfiguration.fStepMHZ;
                                        ReportLog("New Freq range - buffer cleared.");
                                    }

                                    if (!IsMWSUB3G)
                                    {
                                        AmplitudeTopDBM = objConfiguration.fAmplitudeTopDBM;
                                        AmplitudeBottomDBM = objConfiguration.fAmplitudeBottomDBM;
                                    }

                                    UpdateCalculatorMode(objConfiguration.eCalculator, true);

                                    MinFreqMHZ = objConfiguration.fMinFreqMHZ;
                                    MaxFreqMHZ = objConfiguration.fMaxFreqMHZ;
                                    MaxSpanMHZ = objConfiguration.fMaxSpanMHZ;

                                    m_fOffset_dB = objConfiguration.fOffset_dB;
                                    m_fRBWKHZ = objConfiguration.fRBWKHZ; //TODO T0028: may be needed in sniffer mode too
                                    FreqSpectrumSteps = objConfiguration.FreqSpectrumSteps;

                                    if ((m_eActiveModel == eModel.MODEL_2400) || (m_eActiveModel == eModel.MODEL_6G))
                                    {
                                        MinSpanMHZ = 2.0;
                                    }
                                    else
                                    {
                                        if (FreqSpectrumSteps <= RFECommunicator.RFE_MIN_SWEEP_POINTS)  //Keep using 112 for backward visual compatibility
                                        {
                                            MinSpanMHZ = 0.112;
                                        }
                                        else
                                        {
                                            MinSpanMHZ = 0.001 * FreqSpectrumSteps;
                                        }
                                    }
                                }
                                m_LastCaptureTime = new DateTime(2000, 1, 1);
                                m_nAverageSweepSpeedIterator = 0;
                                m_spanAverageSpeedAcumulator = TimeSpan.Zero;
                                m_fAverageSweepSeconds = 0.0;
                                m_sSweepSpeed = "";
                                OnReceivedConfigurationData(new EventArgs());
                            }
                        }
                        else if (objNew.GetType() == typeof(RFESweepData))
                        {
                            if (m_eMode == eMode.MODE_TRACKING)
                            {
                                RFESweepData objSweep = (RFESweepData)objNew;

                                if (m_bTrackingNormalizing)
                                {
                                    if (m_SweepTrackingNormalizedContainer == null)
                                        m_SweepTrackingNormalizedContainer = new RFESweepDataCollection(3, true);

                                    m_SweepTrackingNormalizedContainer.Add(objSweep);
                                    bool bWrongData = objSweep.GetAmplitudeDBM(objSweep.GetMinDataPoint()) <= (MIN_AMPLITUDE_TRACKING_NORMALIZE + AmplitudeOffsetDB + (Convert.ToInt32(InputStageAttenuationDB)));

                                    if (bWrongData || ((m_nAutoStopSNATrackingCounter != 0) && (m_SweepTrackingNormalizedContainer.Count >= m_nAutoStopSNATrackingCounter)))
                                    {
                                        StopTracking();

                                        if (bWrongData)
                                            //invalid data, end so it can be restarted
                                            m_SweepTrackingNormalized = objSweep;
                                        else
                                            //if all samples collected, end and get average among them
                                            m_SweepTrackingNormalized = m_SweepTrackingNormalizedContainer.GetAverage(0, m_SweepTrackingNormalizedContainer.Count - 1);

                                        OnUpdateDataTrakingNormalization(new EventArgs());
                                    }
                                }
                                else
                                {
                                    m_TrackingDataContainer.Add(objSweep);
                                    bDraw = true;
                                    OnUpdateDataTraking(new EventArgs());
                                    if ((m_nAutoStopSNATrackingCounter != 0) && (m_nTrackingPass >= m_nAutoStopSNATrackingCounter))
                                    {
                                        StopTracking();
                                    }
                                }
                            }
                            else
                            {
                                if (!HoldMode)
                                {
                                    RFESweepData objSweep = (RFESweepData)objNew;
                                    if (!StoreSweep)
                                    {
                                        m_SweepDataContainer.CleanAll();
                                    }

                                    m_SweepDataContainer.Add(objSweep);

                                    bDraw = true;
                                    if (m_SweepDataContainer.IsFull())
                                    {
                                        HoldMode = true;
                                        OnUpdateFeedMode(new EventArgs());
                                        ReportLog("RAM Buffer is full.");
                                    }

                                    m_sSweepDate = objSweep.CaptureTime.ToString("yyyy-MM-dd HH:mm:ss\\.fff");
                                    m_sSweepSpeed = objSweep.TotalDataPoints + "pts";
                                    m_sSweepInfoText = m_sSweepDate + " - " + m_sSweepSpeed;
                                    TimeSpan objSpan = objSweep.CaptureTime - m_LastCaptureTime;
                                    if (objSpan.TotalSeconds < 60)  //TODO: Adjust this span time depending on current sweep point resolution, currently the sweep that takes logest to receive is 29.3 secs for 4096 points
                                    {
                                        //If time between captures is less than 60 seconds, we can assume we are getting realtime data
                                        //and therefore can provide average sweep/seconds information, otherwise we were in hold or something
                                        //and data could not be used for these calculations.
                                        m_nAverageSweepSpeedIterator++;
                                        m_spanAverageSpeedAcumulator += objSpan;

                                        if (m_nAverageSweepSpeedIterator >= 10 || (m_nAverageSweepSpeedIterator >= 1 && m_fAverageSweepSeconds <= 0.0))
                                        {
                                            m_fAverageSweepSeconds = m_spanAverageSpeedAcumulator.TotalSeconds / m_nAverageSweepSpeedIterator;
                                            m_nAverageSweepSpeedIterator = 0;
                                            m_spanAverageSpeedAcumulator = TimeSpan.Zero;   //Set it to zero and start average all over again
                                        }

                                        if (m_fAverageSweepSeconds > 0.0)
                                        {
                                            m_sSweepInfoText += "\nSwp time: " + m_fAverageSweepSeconds.ToString("f1") + " (s)";
                                            if (m_fAverageSweepSeconds < 1.0)
                                            {
                                                m_sSweepSpeed = "Swp " + (m_fAverageSweepSeconds * 1000).ToString("f0") + "ms  " + m_sSweepSpeed;
                                                m_sSweepInfoText += " - Avg Sweeps/second: " + (1.0 / m_fAverageSweepSeconds).ToString("f1"); //Add this only for fast, short duration scans
                                            }
                                            else
                                                m_sSweepSpeed = "Swp " + (m_fAverageSweepSeconds).ToString("f1") + "s  " + m_sSweepSpeed;
                                        }
                                    }
                                    else
                                    {
                                        m_nAverageSweepSpeedIterator = 0;
                                        m_spanAverageSpeedAcumulator = TimeSpan.Zero;
                                        m_fAverageSweepSeconds = 0.0;
                                    }
                                    m_LastCaptureTime = objSweep.CaptureTime;

                                    OnUpdateData(new EventArgs());
                                }
                                else
                                {
                                    //if in hold mode, we just record last time came here to make sure we start from most reliable point in time
                                    m_LastCaptureTime = DateTime.Now;
                                }
                            }
                        }
                        else if (objNew.GetType() == typeof(RFEScreenData))
                        {
                            if ((CaptureRemoteScreen) && (m_ScreenDataContainer.IsFull() == false))
                            {
                                RFEScreenData objScreen = (RFEScreenData)objNew;
                                objScreen.Model = ActiveModel;
                                m_ScreenDataContainer.Add(objScreen);
                                ScreenIndex = (UInt16)m_ScreenDataContainer.UpperBound;
                                OnUpdateRemoteScreen(new EventArgs());
                            }
                            else
                            {
                                //receiving Screen Dump data but it was intended to be disabled, resend a disable command now
                                SendCommand_DisableScreenDump();
                            }
                        }
                        else
                        {
                            //received a string, so use it along to parse parameters
                            string sLine = (string)objNew;
                            sReceivedString = sLine;

                            if ((sLine.Length > 3) && (sLine.StartsWith(_Acknowldedge)))
                            {
                                m_bAcknowledge = true;
                            }
                            else if ((sLine.Length > 4) && (sLine.StartsWith(_INTERNAL_PORTCLOSED_EVENT)))
                            {
                                ClosePort(false);
                            }
                            else if ((sLine.Length > 4) && (sLine.StartsWith("DSP:")))
                            {
                                m_eDSP = (eDSP)Convert.ToByte(sLine.Substring(4, 1));
                                ReportLog("DSP mode: " + m_eDSP.ToString());
                            }
                            else if ((sLine.Length > 4) && (sLine.StartsWith("#\t")))
                            {
                                //Dump binary buffer from RF Explorer device
                                string sLineDump = "[0000] ";
                                string sLineASCII = "";
                                int nCharInd = 0;
                                int nAddress = 0;
                                foreach (char cData in sLine.Substring(2))
                                {
                                    sLineDump += "[" + Convert.ToByte(cData).ToString("X2") + "]";
                                    if ((cData >= ' ') && (cData < 127))
                                        sLineASCII += cData;
                                    else
                                        sLineASCII += '.';

                                    nCharInd++;
                                    nAddress++;
                                    if (nCharInd >= 16)
                                    {
                                        nCharInd = 0;
                                        sLineDump += "    " + sLineASCII + Environment.NewLine;
                                        sLineDump += "[" + nAddress.ToString("X4") + "] ";
                                        sLineASCII = "";
                                    }
                                }
                                ReportLog(RFECommunicator._DEBUG_StringReport + Environment.NewLine + sLineDump);
                            }
                            else if ((sLine.Length > 16) && (sLine.StartsWith("#Sn")))
                            {
                                m_sSerialNumber = sLine.Substring(3, 16);
                                ReportLog("Device serial number: " + SerialNumber);
                            }
                            else if ((sLine.Length > 2) && ((sLine.StartsWith("$q")) || (sLine.StartsWith("$Q"))))
                            {
                                //calibration data
                                UInt16 nSourceStringSize = Convert.ToUInt16(sLine[2]);
                                if (sLine[1] == 'Q')
                                {
                                    nSourceStringSize += (ushort)(0x100 * (byte)sLine[3]);
                                }

                                if (IsGenerator())
                                {
                                    //signal generator uses a different approach for storing absolute amplitude value offset over an ideal -30dBm response
                                    if ((m_RFGenCal.GetCalSize() < 0) || (m_RFGenCal.GetCalSize() != nSourceStringSize))
                                    {
                                        string sData = "";
                                        m_RFGenCal.InitializeCal(nSourceStringSize, sLine, out sData);
                                        ReportLog("Embedded calibration Signal Generator data received: " + sData, true);
                                    }
                                }
                                else
                                {
                                    if (m_eActiveModel == eModel.MODEL_6G || m_eActiveModel == eModel.MODEL_WSUB1G || m_eActiveModel == eModel.MODEL_WSUB1G_PLUS || IsMWSUB3G)
                                    {
                                        string sData = "Embedded calibration Spectrum Analyzer data received:";
                                        bool bAllZero = true;
                                        UInt16 nStartPositionCalData = 0;
                                        UInt16 nStopPositionCalData = nSourceStringSize;
                                        switch (m_eActiveModel)
                                        {
                                            case eModel.MODEL_6G:
                                                nStartPositionCalData = POS_INTERNAL_CALIBRATED_6G;
                                                break;
                                            case eModel.MODEL_WSUB1G:
                                                nStartPositionCalData = POS_INTERNAL_CALIBRATED_WSUB1G;
                                                nStopPositionCalData = POS_END_INTERNAL_CALIBRATED_WSUB1G;
                                                break;
                                            case eModel.MODEL_WSUB1G_PLUS:
                                                nStartPositionCalData = POS_INTERNAL_CALIBRATED_WSUB1G_PLUS;
                                                nStopPositionCalData = POS_END_INTERNAL_CALIBRATED_WSUB1G_PLUS;
                                                break;
                                            case eModel.MODEL_WSUB3G: //this is mainboard, so is actually a IoT or AudioPro
                                                nStartPositionCalData = POS_INTERNAL_CALIBRATED_MWSUB3G;
                                                break;
                                        }

                                        if ((m_arrSpectrumAnalyzerEmbeddedCalibrationOffsetDB == null) || (m_arrSpectrumAnalyzerEmbeddedCalibrationOffsetDB.Length != (nStopPositionCalData - nStartPositionCalData)))
                                        {
                                            m_arrSpectrumAnalyzerEmbeddedCalibrationOffsetDB = new float[nStopPositionCalData - nStartPositionCalData];
                                            for (int nInd = 0; nInd < (nStopPositionCalData - nStartPositionCalData); nInd++)
                                            {
                                                m_arrSpectrumAnalyzerEmbeddedCalibrationOffsetDB[nInd] = 0.0f;
                                            }
                                        }

                                        int nAdjustSize = 3;
                                        if (sLine[1] == 'Q')
                                        {
                                            nAdjustSize = 4; //this accounts for extra byte sent in $Q for size
                                        }

                                        for (int nInd = nStartPositionCalData, nInd2 = 0; nInd < nStopPositionCalData; nInd++, nInd2++)
                                        {
                                            if (((nInd2 % 16) == 0) && !sData.EndsWith(Environment.NewLine))
                                            {
                                                sData += Environment.NewLine;
                                                nInd2 = 0;
                                            }
                                            else if (IsMWSUB3G && ((nInd % 81) == 0) && !sData.EndsWith(Environment.NewLine))
                                            {
                                                sData += Environment.NewLine;
                                                nInd2 = 0;
                                            }

                                            int nVal = Convert.ToInt32(sLine[nInd + nAdjustSize]);
                                            if (nVal > 127)
                                                nVal = -(256 - nVal); //get the right sign
                                            if (nVal != 0)
                                                bAllZero = false;
                                            m_arrSpectrumAnalyzerEmbeddedCalibrationOffsetDB[nInd - nStartPositionCalData] = nVal / 2.0f; //split by two to get dB
                                            sData += m_arrSpectrumAnalyzerEmbeddedCalibrationOffsetDB[nInd - nStartPositionCalData].ToString("00.0");
                                            if (nInd < nStopPositionCalData - 1)
                                                sData += ",";
                                        }
                                        sData += Environment.NewLine;
                                        ReportLog(sData, true);
                                        if (bAllZero)
                                            ReportLog("ERROR: the device internal calibration data is missing! contact support at www.rf-explorer.com/contact");
                                    }
                                }
                            }
                            else if ((sLine.Length > 18) && (sLine.StartsWith(_ResetString)))
                            {
                                //RF Explorer device was reset for some reason, reconfigure client based on new configuration
                                OnDeviceResetEvent(new EventArgs());
                            }
                            else if ((sLine.Length > 5) && sLine.StartsWith("#C2-M:"))
                            {
                                ReportLog("Received RF Explorer device model info:" + sLine);
                                m_eMainBoardModel = (eModel)Convert.ToUInt16(sLine.Substring(6, 3));
                                if (m_eMainBoardModel == eModel.MODEL_AUDIOPRO)
                                {
                                    m_bAudioPro = true;
                                    m_eMainBoardModel = eModel.MODEL_WSUB3G;
                                    ReportLog("Audio Pro model found, converted to MWSUB3G", true);
                                }
                                m_eExpansionBoardModel = (eModel)Convert.ToUInt16(sLine.Substring(10, 3));
                                m_sRFExplorerFirmware = (sLine.Substring(14, 5));
                                OnReceivedDeviceModel(new EventArgs());
                            }
                            else if ((sLine.Length > 5) && sLine.StartsWith("#C3-M:"))
                            {
                                ReportLog("Received RF Explorer Generator device info:" + sLine);
                                m_eMainBoardModel = (eModel)Convert.ToUInt16(sLine.Substring(6, 3));
                                m_eExpansionBoardModel = (eModel)Convert.ToUInt16(sLine.Substring(10, 3));
                                m_bExpansionBoardActive = (m_eExpansionBoardModel == eModel.MODEL_RFGEN_EXPANSION);
                                m_sRFExplorerFirmware = (sLine.Substring(14, 5));


                                OnReceivedDeviceModel(new EventArgs());
                            }
                            else if ((sLine.Length > 6) && sLine.StartsWith("#CAL:"))
                            {
                                m_bMainboardInternalCalibrationAvailable = (sLine[5] == '1');
                                m_bExpansionBoardInternalCalibrationAvailable = (sLine[6] == '1');
                            }
                            else if ((sLine.Length > 2) && sLine.StartsWith("#K1"))
                            {
                                ReportLog("RF Explorer is now in TRACKING mode.");
                                m_eMode = eMode.MODE_TRACKING;
                                OnReceivedConfigurationData(new EventArgs());
                            }
                            else if ((sLine.Length > 2) && sLine.StartsWith("#K0"))
                            {
                                ReportLog("RF Explorer is now in ANALYZER mode.");
                                m_eMode = eMode.MODE_SPECTRUM_ANALYZER;
                            }
                            else if ((sLine.Length > 2) && sLine.StartsWith("#a"))
                            {
                                eInputStage ePreviousInputSatge = m_eInputStage;
                                m_eInputStage = (eInputStage)Convert.ToByte(sLine[2] - 0x30);
                                ReportLog("Input stage changed to " + m_eInputStage.ToString());
                                if (m_eInputStage != ePreviousInputSatge)
                                    OnReceivedDeviceInputStage(new EventArgs());
                            }
                            else if ((sLine.Length > 3) && sLine.StartsWith("#C+"))
                            {

                                UpdateCalculatorMode((eCalculator)Convert.ToByte(sLine[3]), false);
                                ReportLog("Calculator mode changed to " + m_eCalculator.ToString());
                            }
                            else if ((sLine.Length > 2) && sLine.StartsWith("#G:"))
                            {
                                if (DebugGPS)
                                    ReportLog(sLine);
                                if (sLine.Length < 5)
                                {
                                    m_sGPSTimeUTC = "";
                                    m_sGPSLongitude = "";
                                    m_sGPSLattitude = "";
                                    ReportLog("GPS data unavailable");
                                }
                                else
                                {
                                    try
                                    {
                                        string[] arrGPS = sLine.Split(',');
                                        m_sGPSTimeUTC = arrGPS[1];
                                        m_sGPSLattitude = arrGPS[2].Substring(0, 10);
                                        m_sGPSLongitude = arrGPS[2].Substring(10, 11);
                                        ReportLog("GPS Time: " + m_sGPSTimeUTC);
                                        ReportLog("GPS Location: " + m_sGPSLattitude + " " + m_sGPSLongitude);
                                    }
                                    catch
                                    {
                                        ReportLog(sLine);
                                    }
                                }
                                OnUpdateGPSData(new EventArgs());
                            }
                            else if ((sLine.Length > 2) && (sLine.StartsWith("$S")) && (StartFrequencyMHZ > 0.01))
                            {
                                bWrongFormat = true;
                            }
                            else if ((sLine.Length > 2) && sLine.StartsWith("$R"))
                            {
                                if (!HoldMode)
                                {
                                    if (!m_SnifferBinaryDataContainer.IsFull())
                                    {
                                        RFEBinaryPacketData objData = new RFEBinaryPacketData(m_fRefFrequencyMHZ, m_fRBWKHZ, (UInt16)(8 * (sLine.Length - 2)), m_nBaudrate, m_fThresholdDBM);
                                        objData.LoadRAWSnifferString(sLine.Substring(2));
                                        m_SnifferBinaryDataContainer.Add(objData);
                                        OnRawSnifferData(new EventArgs());
                                    }
                                    else
                                    {
                                        HoldMode = true;
                                        ReportLog("Buffer is full.");
                                    }
                                }
                            }
                            else if ((sLine.Length > 5) && (sLine.StartsWith("#C4-F:")))
                            {
                                bWrongFormat = true; //parsed on the thread
                            }
                            else if ((sLine.Length > 5) && (sLine.StartsWith("#C2-F:")))
                            {
                                bWrongFormat = true; //parsed on the thread
                            }
                            else if ((sLine.Length > 5) && (sLine.StartsWith("#C1-F:")))
                            {
                                bWrongFormat = true; //obsolete firmware
                            }
                            else
                            {
                                ReportLog(sLine, true); //report any line we don't understand - it is likely a human readable message
                            }

                            if (bWrongFormat)
                            {
                                ReportLog("Received unexpected data from RFExplorer device:" + sLine);
                                ReportLog("Please update your RF Explorer to a recent firmware version and");
                                ReportLog("make sure you are using the latest version of RF Explorer software.");
                                ReportLog("Visit http://www.rf-explorer/download for latest firmware updates.");

                                OnWrongFormatData(new EventArgs());
                            }
                        }

                    } while (bProcessAllEvents && (m_arrReceivedData.Count > 0));
                }
                catch (Exception obEx)
                {
                    ReportLog("ProcessReceivedString: " + sReceivedString + Environment.NewLine + obEx.ToString());
                }
            }

            return bDraw;
        }

        private void SwitchDeviceToNormalResolution()
        {
            if (m_nFreqSpectrumSteps > RFE_MIN_SWEEP_STEPS)
                SendCommand_SweepDataPoints(RFE_MIN_SWEEP_POINTS);
        }

        bool m_bAudioPro = false;

        //this variable stores the intended use of this object, note the real one may be different once connected
        bool m_bIntendedAnalyzer = true;

        /// <summary>
        /// True if the connected object is a Signal Generator model
        /// </summary>
        /// <param name="bCheckModelAvailable">Use this to "true" in case you want to check actual model, not intended model if still not known
        /// - By default you will want this on "false"</param>
        /// <returns>true if connected device is a generator, false otherwise</returns>
        public bool IsGenerator(bool bCheckModelAvailable = false)
        {
            if (!bCheckModelAvailable)
            {
                if (MainBoardModel == eModel.MODEL_NONE)
                    return !m_bIntendedAnalyzer;
                else
                    return MainBoardModel == eModel.MODEL_RFGEN;
            }
            else
                return MainBoardModel == eModel.MODEL_RFGEN;
        }

        /// <summary>
        /// Check if the connected object is a Spectrum Analyzer device
        /// </summary>
        /// <param name="bCheckModelAvailable">Use this to "true" in case you want to check actual model, not intended model if still not known
        /// - By default you will want this on "false"</param>
        /// <returns>true if connected device is an analyzer, false otherwise</returns>
        public bool IsAnalyzer(bool bCheckModelAvailable = false)
        {
            if (!bCheckModelAvailable)
            {
                return !IsGenerator();
            }
            else
            {
                return (MainBoardModel != eModel.MODEL_NONE);
            }
        }

        UInt16 m_nAutoStopSNATrackingCounter = 0;
        /// <summary>
        /// For SNA tracking mode, this setting will indicate how many tracking passes should be done before stop, or 0 for infinite
        /// </summary>
        public UInt16 AutoStopSNATrackingCounter
        {
            get { return m_nAutoStopSNATrackingCounter; }
            set { m_nAutoStopSNATrackingCounter = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sConfigurationString"></param>
        /// <param name="sModel"></param>
        private bool UpdateOfflineConfigurationParameters_Generator(string sConfigurationString, string sModel)
        {
            if (String.IsNullOrEmpty(sConfigurationString))
                return false;

            if (String.IsNullOrEmpty(sModel))
                return false;

            string sValues = sConfigurationString.Replace(" - ", "@").ToLower(); //get a reliable separator field
            sValues = sValues.Replace("from file:", "");
            sValues = sValues.Replace("mhz", "");
            sValues = sValues.Replace("start:", "");
            sValues = sValues.Replace("stop:", "");
            sValues = sValues.Replace("stepwait:", "");
            sValues = sValues.Replace("sweepsteps:", "");
            sValues = sValues.Replace("step:", "");
            sValues = sValues.Replace("highpowerswitch:", "");
            sValues = sValues.Replace("powerlevel:", "");
            sValues = sValues.Replace("cw:", "");
            sValues = sValues.Replace("ms", "");
            sValues = sValues.Replace("freq offset:", "");
            sValues = sValues.Replace("freqoffsetlogic:", "");
            string[] arrValues = sValues.Split('@');

            //Due to duplicity reason we no longer use "Step" value from v17.1708.05. From now, we only use "Stop" value so this variable has not effect
            double fGenStepFrequencyMHZ = 0.0f;
            //From october 2017 we always save data with "en-US" settings
            m_objRFEGen.RFGenCWFrequencyNormalizedMHZ = Double.Parse(arrValues[0], CultureInfo.InvariantCulture);
            m_objRFEGen.RFGenStartFrequencyNormalizedMHZ = Double.Parse(arrValues[1], CultureInfo.InvariantCulture);
            m_objRFEGen.RFGenStopFrequencyNormalizedMHZ = Double.Parse(arrValues[2], CultureInfo.InvariantCulture);
            fGenStepFrequencyMHZ = Double.Parse(arrValues[3], CultureInfo.InvariantCulture);
            m_objRFEGen.RFGenPowerLevel = Convert.ToByte(arrValues[4]);
            m_objRFEGen.RFGenHighPowerSwitch = (arrValues[5] == "true");
            m_objRFEGen.RFGenSweepSteps = Convert.ToUInt16(arrValues[6]);
            m_objRFEGen.RFGenStepWaitMS = Convert.ToUInt16(arrValues[7]);
            //Initialize in the case of old files
            m_objRFEGen.FrequencyOffsetMHZ = 0.0f;
            FrequencyOffsetMHZ = 0.0f;
            m_bFreqOffsetInAnalyzer = true;
            if (arrValues.Length >= 9)
            {
                double fTempFreqOffset = 0.0;
                if (arrValues.Length >= 9)
                    fTempFreqOffset = Double.Parse(arrValues[8], CultureInfo.InvariantCulture);
                if (arrValues.Length >= 10)
                    m_bFreqOffsetInAnalyzer = arrValues[9].Equals("a");

                if (m_bFreqOffsetInAnalyzer)
                    FrequencyOffsetMHZ = fTempFreqOffset;
                else
                    m_objRFEGen.FrequencyOffsetMHZ = fTempFreqOffset;
            }

            return true;
        }

        /// <summary>
        /// This method will parse Spectrum Analyzer configuration and model string read from data file and will update current configuration to match that.
        /// Note: for this to work the device must be disconnected, you cannot change runtime parameters if a device is connected
        /// Rather than parsing, a better way since the beginning would have been to store each and everyone of the parameters separately in the file but,
        /// given that was not the case and to keep backward compatibility with files, we keep the string human readable format
        /// and parse it here for machine usability.
        /// </summary>
        private void UpdateOfflineConfigurationParameters_Analyzer(string sConfigurationString, string sModel)
        {
            if (PortConnected)
                return; //we do this only if device is offline

            if (!String.IsNullOrEmpty(sConfigurationString))
            {
                string sValues = sConfigurationString.Replace(" - ", "@").ToLower(); //get a reliable separator field
                sValues = sValues.Replace("from file:", "");
                sValues = sValues.Replace("mhz", "");
                sValues = sValues.Replace("khz", "");
                sValues = sValues.Replace("dbm", "");
                sValues = sValues.Replace("start:", "");
                sValues = sValues.Replace("stop:", "");
                sValues = sValues.Replace("center:", "");
                sValues = sValues.Replace("span:", "");
                sValues = sValues.Replace("sweep step:", "");
                sValues = sValues.Replace("rbw:", "");
                sValues = sValues.Replace("amp offset:", "");
                sValues = sValues.Replace("freq offset:", "");
                sValues = sValues.Replace("freqoffsetlogic:", "");
                sValues = sValues.Replace(" ", "");
                string[] arrValues = sValues.Split('@');

                //note, we do not use many of these fiels because already came from sweep data in binary format
                //Initialize in the case of old files
                m_fRBWKHZ = 0.0f;
                m_fOffset_dB = 0.0f;
                m_fOffset_MHZ = 0.0f;
                m_bFreqOffsetInAnalyzer = true;
                if (arrValues.Length >= 6)
                {
                    //From october 2017, we always save files with "en-US" settings
                    if (arrValues.Length >= 6)
                        m_fRBWKHZ = Double.Parse(arrValues[5], CultureInfo.InvariantCulture);
                    if (arrValues.Length >= 7)
                        m_fOffset_dB = (float)Double.Parse(arrValues[6], CultureInfo.InvariantCulture);
                    if (arrValues.Length >= 8)
                        m_fOffset_MHZ = Double.Parse(arrValues[7], CultureInfo.InvariantCulture);
                    if (arrValues.Length >= 9)
                        m_bFreqOffsetInAnalyzer = arrValues[8].Equals("a");
                }
            }

            if (!String.IsNullOrEmpty(sModel))
            {
                string sValues = sModel.Replace(" - ", "@").ToLower(); //get a reliable separator field
                sValues = sValues.Replace("-", "@");
                sValues = sValues.Replace("from file:", "");
                sValues = sValues.Replace("expansion module:", "");
                sValues = sValues.Replace("client v", "");
                sValues = sValues.Replace("firmware v", "");
                sValues = sValues.Replace("model:", "");
                sValues = sValues.Replace("active range:", "");
                sValues = sValues.Replace("mhz", "");
                sValues = sValues.Replace("no expansion module found", eModel.MODEL_NONE.ToString().ToLower());
                sValues = sValues.Replace(" ", "");
                string[] arrValues = sValues.Split('@');

                m_sRFExplorerFirmware = "";
                m_eMainBoardModel = eModel.MODEL_NONE;
                m_eExpansionBoardModel = eModel.MODEL_NONE;
                m_eActiveModel = eModel.MODEL_NONE;
                m_bExpansionBoardActive = false;
                if (arrValues.Length > 2)
                {
                    //Update model enumerators from text
                    string sModelMainBoard = arrValues[2];
                    string sModelExpansion = arrValues[3];
                    m_bExpansionBoardActive = false;

                    //First determine what is the active board
                    if (sModelExpansion.Contains(_ACTIVE.ToLower()))
                    {
                        m_bExpansionBoardActive = true;
                    }
                    sModelMainBoard = sModelMainBoard.Replace(_ACTIVE.ToLower(), "");
                    sModelExpansion = sModelExpansion.Replace(_ACTIVE.ToLower(), "");

                    //Now get each board model
                    m_eMainBoardModel = GetModelEnumFromText(sModelMainBoard);
                    m_eExpansionBoardModel = GetModelEnumFromText(sModelExpansion);
                    if (m_bExpansionBoardActive)
                    {
                        m_eActiveModel = m_eExpansionBoardModel;
                    }
                    else
                    {
                        m_eActiveModel = m_eMainBoardModel;
                    }

                    //Get firmware
                    m_sRFExplorerFirmware = arrValues[1];
                    //Get max min frequency
                    MinFreqMHZ = Double.Parse(arrValues[4], CultureInfo.InvariantCulture);
                    MaxFreqMHZ = Double.Parse(arrValues[5], CultureInfo.InvariantCulture);
                    MaxSpanMHZ = 0.0; //Unknown span, not saved in file format
                }
            }
        }

        /// <summary>
        /// Set Calculator mode when it is received in a new device configuration or
        /// changing calculator mode only
        /// </summary>
        /// <param name="eCurrentCalculator">Calculator mode in device</param>
        /// <param name="bForce">True to force update calculator mode in a new device configuration,
        /// false to not force when calculator mode is changed in device</param>
        private void UpdateCalculatorMode(eCalculator eCurrentCalculator, bool bForce)
        {
            //If it is a MODEL_WSUB3G/PLUS, make sure we use the MAX HOLD mode to account for proper DSP
            m_eCalculator = eCurrentCalculator; //Always update Calculator mode

            if (bForce && (m_eActiveModel == RFECommunicator.eModel.MODEL_WSUB3G || IsMainboardAnalyzerPlus))
            {
                if (m_bUseMaxHold)
                {
                    if (m_eCalculator != eCalculator.MAX_HOLD)
                    {
                        ReportLog("Updated remote mode to Max Hold for reliable DSP calculations with fast signals");
                        Thread.Sleep(500);
                        SendCommand_SetMaxHold();
                    }
                }
                else
                {
                    if (m_eCalculator == eCalculator.MAX_HOLD)
                    {
                        ReportLog("Remote mode is not Max Hold, some fast signals may not be detected");
                        Thread.Sleep(500);
                        SendCommand_Realtime();
                    }
                }
            }
        }
        /// <summary>
        /// Data point from 0-9999 to set the tracking configuration
        /// </summary>
        /// <param name="nDataPoint">Data point to select analyzer or tracking generator to work on momentarily</param>
        public void SendCommand_TrackingDataPoint(UInt16 nDataPoint)
        {
            byte nByte1 = Convert.ToByte(nDataPoint >> 8);
            byte nByte2 = Convert.ToByte(nDataPoint & 0x00ff);

            SendCommand("k" + Convert.ToChar(nByte1) + Convert.ToChar(nByte2));
        }

        /// <summary>
        /// Request RF Explorer Spectrum Analyzer to enter tracking mode
        /// </summary>
        public void SendCommand_TrackingConfig(double fStartFrequency, double fStepFrequency)
        {
            SendCommand("C3-K:" + ((UInt32)(fStartFrequency * 1000)).ToString("D7") + "," + ((UInt32)(fStepFrequency * 1000)).ToString("D7"));
        }

        /// <summary>
        /// Request RF Explorer device to send configuration data and start sending feed back
        /// </summary>
        public void SendCommand_RequestConfigData()
        {
            SendCommand("C0");
        }

        /// <summary>
        /// Enable mainboard module in the RF Explorer SA
        /// </summary>
        public void SendCommand_EnableMainboard()
        {
            SendCommand("CM\x0");
        }

        /// <summary>
        /// Ask RF Explorer SA device to hold
        /// </summary>
        public void SendCommand_Hold()
        {
            SendCommand("CH");
        }

        /// <summary>
        /// Enable expansion module in the RF Explorer SA
        /// </summary>
        public void SendCommand_EnableExpansion()
        {
            SendCommand("CM\x1");
        }

        /// <summary>
        /// Enable LCD and backlight on device screen (according to internal device configuration settings)
        /// </summary>
        public void SendCommand_ScreenON()
        {
            SendCommand("L1");
        }

        /// <summary>
        /// Disable LCD and backlight on device screen
        /// </summary>
        public void SendCommand_ScreenOFF()
        {
            SendCommand("L0");
        }

        /// <summary>
        /// Disable device LCD screen dump
        /// </summary>
        public void SendCommand_DisableScreenDump()
        {
            SendCommand("D0");
        }

        /// <summary>
        /// Enable device LCD screen dump
        /// </summary>
        public void SendCommand_EnableScreenDump()
        {
            SendCommand("D1");
        }

        /// <summary>
        /// Define RF Explorer SA sweep data points range 112-65536
        /// </summary>
        /// <param name="nDataPoints">a value in the range of 112-65536, note a value multiple of 2</param>
        public void SendCommand_SweepDataPointsEx(int nDataPoints)
        {
            SendCommand("Cj" + Convert.ToChar((nDataPoints & 0xFF00) >> 8) + Convert.ToChar(nDataPoints & 0xFF));
        }

        /// <summary>
        /// Define RF Explorer SA sweep data points range 112-4096
        /// </summary>
        /// <param name="nDataPoints">a value in the range of 16-4096, note a value multiple of 16 will be used, so any other number will be truncated to nearest 16 multiple</param>
        public void SendCommand_SweepDataPoints(int nDataPoints)
        {
            SendCommand("CJ" + Convert.ToChar((nDataPoints - 16) / 16));
        }

        /// <summary>
        /// Set RF Explorer SA device in Calculator:MaxHold, this is useful to capture fast transient signals even if the actual Windows application is representing other trace modes
        /// </summary>
        public void SendCommand_SetMaxHold()
        {
            SendCommand("C+\x04");
        }

        /// <summary>
        /// Set RF Explorer SA devince in Calculator:Normal, this is useful to minimize spikes and spurs produced by unwanted signals
        /// </summary>
        public void SendCommand_Realtime()
        {
            SendCommand("C+\x00");
        }

        /// <summary>
        /// Set RF Explorer RFGen device RF power output to OFF
        /// </summary>
        public void SendCommand_GeneratorRFPowerOFF()
        {
            if (IsGenerator())
            {
                m_bRFGenPowerON = false;
                SendCommand("CP0");
            }
        }

        /// <summary>
        /// Set RF Explorer RFGen device RF power output to ON
        /// </summary>
        public void SendCommand_GeneratorRFPowerON()
        {
            if (IsGenerator())
            {
                m_bRFGenPowerON = true;
                SendCommand("CP1");
            }
        }

        /// <summary>
        /// Set a new amplitude offset in dB. The firmware v1.16 and older does not return configuration so a #C0 is required 
        /// </summary>
        /// <param name="nbOffsetDB"></param>
        public void SendCommand_AmplitudeOffsetDB(double nbOffsetDB)
        {
            byte bOffsetDB = 0;
            if (nbOffsetDB < 0)
                bOffsetDB = (byte)(256 + nbOffsetDB);
            else
                bOffsetDB = (byte)nbOffsetDB;

            string sData = "CO" + Convert.ToChar(bOffsetDB);
            SendCommand(sData);

            if (!IsFirmwareSameOrNewer(1.17))
            {
                Thread.Sleep(100); //wait some time for the unit to process changes, otherwise may get a different command too soon
                SendCommand_RequestConfigData();   //This is no necessary with v1.17 or newer
            }
        }

        /// <summary>
        /// Format and send command - for instance to reboot just use "r", the '#' decorator and byte length char will be included within
        /// </summary>
        /// <param name="sData">unformatted command from http://code.google.com/p/rfexplorer/wiki/RFExplorerRS232Interface </param>
        public void SendCommand(string sData)
        {

#if DEBUG
            if (sData.Contains("C3-K:"))
            {
                ReportLog("Analyzer Tracking command:" + sData.ToString());
                Console.WriteLine("Analyzer Tracking command:" + sData.ToString());
            }
#endif

            if (m_bDebugTracesSent)
                ReportLog("DEBUG SendCommand " + sData[0], true);

            if (!m_bPortConnected)
                return;

            if (m_bDebugTracesSent)
                ReportLog("DEBUG SendCommand entering lock...", true);
            try
            {
                Monitor.Enter(m_serialPortObj);
                m_serialPortObj.Write("#" + Convert.ToChar(sData.Length + 2) + sData);
            }
            catch (Exception obEx)
            {
                ReportLog("SendCommand error: " + obEx.Message);
            }
            finally
            {
                Monitor.Exit(m_serialPortObj);
            }
            if (m_bDebugTraces || m_bDebugTracesSent)
            {
                string sText = "";
                foreach (char cChar in sData)
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
                }

                ReportLog("DEBUG Sent to RFE: " + "#[0x" + (sData.Length + 2).ToString("X2") + "]" + sText);
            }
        }

        /// <summary>
        /// Raw basic data write to Serial Port - use only if you know what you are doing, otherwise use SendCommand
        /// </summary>
        public void WriteRAW(byte[] arrData, int nSize)
        {
            if (!m_bPortConnected)
                return;

            try
            {
                Monitor.Enter(m_serialPortObj);
                m_serialPortObj.Write(arrData, 0, nSize);
            }
            catch (Exception obEx)
            {
                ReportLog("WriteRAW error: " + obEx.Message, true);
            }
            finally
            {
                Monitor.Exit(m_serialPortObj);
            }
        }

        private void ReportLog(string sLine, bool bHidden = false)
        {
            if (bHidden)
                OnReportInfoAdded(new EventReportInfo(_DEBUG_StringReport + sLine));
            else
                OnReportInfoAdded(new EventReportInfo(sLine));
        }

        /// <summary>
        /// Save RF Explorer SA sweep data into .rfe data file
        /// </summary>
        /// <param name="sFilename">file name with path</param>
        /// <param name="bUseCorrection">true to use external calibration correction, false otherwise</param>
        /// <returns></returns>
        public bool SaveFileRFE(string sFilename, bool bUseCorrection)
        {
            if (bUseCorrection)
                return SweepData.SaveFile(sFilename, FullModelText, ConfigurationText, m_FileAmplitudeCalibration);
            else
                return SweepData.SaveFile(sFilename, FullModelText, ConfigurationText, null);

        }

        /// <summary>
        /// Save SNA tracking data into a data file
        /// </summary>
        /// <param name="sFilename">file path for SNA normalization data file</param>
        /// <returns>true if succesfully saved, false otherwise</returns>
        public bool SaveFileSNANormalization(string sFilename)
        {
            if (!IsTrackingNormalized())
                return false;

            if (IsGenerator())
                return false;

            RFESweepDataCollection objCollection = new RFESweepDataCollection(1, true);
            objCollection.Add(TrackingNormalizedData);

            double fFreqOffsetMHzGenBackUp = m_objRFEGen.FrequencyOffsetMHZ; //store freq offset
            if (m_bFreqOffsetInAnalyzer)
            {
                //Use analyzer freq offset temporaly for generator due to Configuration text property cannot know analyzer properties inside.
                //After that, we will restore it
                m_objRFEGen.FrequencyOffsetMHZ = FrequencyOffsetMHZ;
            }
            objCollection.SaveFile(sFilename, m_objRFEGen.FullModelText, m_objRFEGen.ConfigurationText, null);
            m_objRFEGen.FrequencyOffsetMHZ = fFreqOffsetMHzGenBackUp;   //restore freq offset

            return true;
        }

        /// <summary>
        /// load a normalization SNA file and reconfigures m_SweepTrackingNormalized based on that
        /// </summary>
        /// <param name="sFilename">file path for SNA normalization data file</param>
        /// <returns>true if succesfully loaded, false otherwise</returns>
        private bool LoadFileSNANormalization(string sFilename)
        {
            bool bOk = false;

            RFESweepDataCollection objCollection = new RFESweepDataCollection(1, true);
            objCollection.RFEDataType = RFESweepDataCollection.RFEFileDataType.Normalization;
            string sModel, sConfig;
            if (objCollection.LoadFile(sFilename, out sModel, out sConfig))
            {
                //For normalization file older than v004, step value could be wrong so we have to fix step value in function of start/stop value after.
                bOk = UpdateOfflineConfigurationParameters_Generator(sConfig, sModel);
                objCollection.GetData(0).StepFrequencyMHZ = m_objRFEGen.RFGenStepMHZ();
                m_SweepTrackingNormalized = objCollection.GetData(0);
            }

            return bOk;
        }

        //This variable contains the latest correction file loaded
        public RFEAmplitudeTableData m_FileAmplitudeCalibration = new RFEAmplitudeTableData();
        /// <summary>
        /// Use this to load a correction file (will replace any prior file loaded)
        /// </summary>
        /// <param name="sFilename">amplitude correction data file path</param>
        /// <returns>true if succesfully loaded, false otherwise</returns>
        public bool LoadFileRFA(string sFilename)
        {
            return m_FileAmplitudeCalibration.LoadFile(sFilename);
        }

        /// <summary>
        /// Returns the current correction amplitude value for a given MHZ frequency
        /// </summary>
        /// <param name="nMHz">frequency reference in MHZ to get correction data from</param>
        /// <returns>Amplitude correction data in dB</returns>
        public float GetAmplitudeCorrectionDB(int nMHz)
        {
            return m_FileAmplitudeCalibration.GetAmplitudeCalibration(nMHz);
        }

        /// <summary>
        /// Loads a sweep data file, it can be a .RFE sweep data file, a .SNA tracking file or a .SNANORM normalization tracking file
        /// This is only valid for analyzer objects. A tracking generator will be updated from SNA if linked to the analyzer, but never call this method
        /// from a generator object itself
        /// </summary>
        /// <param name="sFilename"></param>
        /// <returns></returns>
        public bool LoadDataFile(string sFilename)
        {
            string sConfiguration = "";
            string sModel = "";

            if (IsGenerator())
                return false; //only valid for analyzer

            if (IsFileExtensionType(sFilename, _RFE_File_Extension))
            {
                SweepData.RFEDataType = RFESweepDataCollection.RFEFileDataType.Spectrum_analyzer;
                FrequencyOffsetMHZ = 0.0; //must be zero, otherwise accumulate offset twice
                AmplitudeOffsetDB = 0.0f; //must be zero, otherwise accumulate offset twice
                //normal sweep data file
                if (SweepData.LoadFile(sFilename, out sModel, out sConfiguration))
                {
                    HoldMode = true;
                    m_bDataFromFile = true;

                    double fAmplitudeTop, fAmplitudeBottom;
                    SweepData.GetTopBottomDataRange(out fAmplitudeTop, out fAmplitudeBottom, m_FileAmplitudeCalibration);

                    //Get offset, RBW and other parameters not saved as individual variables
                    UpdateOfflineConfigurationParameters_Analyzer(sConfiguration, sModel);

                    //To draw axis correctly
                    AmplitudeBottomNormalizedDBM = fAmplitudeBottom - 5;
                    AmplitudeTopNormalizedDBM = fAmplitudeTop + 15;
                    StartFrequencyNormalizedMHZ = SweepData.GetData(0).StartFrequencyMHZ;
                    StepFrequencyMHZ = SweepData.GetData(0).StepFrequencyMHZ;
                    FreqSpectrumSteps = (UInt16)(SweepData.GetData(0).TotalDataPoints - 1);

                    //TODO: store mode in data file to restore actual mode, not based on number of samples
                    if (SweepData.GetData(0).TotalDataPoints == 13)
                    {
                        m_eMode = RFECommunicator.eMode.MODE_WIFI_ANALYZER;
                    }
                    else
                    {
                        m_eMode = RFECommunicator.eMode.MODE_SPECTRUM_ANALYZER;
                    }

                    m_sSweepSpeed = SweepData.GetData(0).TotalDataPoints + "pts";
                    if (SweepData.Count > 1) //We can get m_fAverageSweepSeconds only is available more than one sweep
                    {
                        m_fAverageSweepSeconds = 0.0;  //Reset value when a file is loaded
                        TimeSpan timeTotalSweepsCaptured = SweepData.GetData(SweepData.Count - 1).CaptureTime - SweepData.GetData(0).CaptureTime;
                        //Average sweep time in seconds, last sweep is ignored due to Capture time is set when sweep is received
                        m_fAverageSweepSeconds = (double)((timeTotalSweepsCaptured.TotalHours * 3600 / (SweepData.Count - 1)));

                        if (m_fAverageSweepSeconds < 1.0)
                        {
                            m_sSweepSpeed = "Swp " + (m_fAverageSweepSeconds * 1000).ToString("f0") + "ms  " + m_sSweepSpeed;
                        }
                        else
                            m_sSweepSpeed = "Swp " + (m_fAverageSweepSeconds).ToString("f1") + "s  " + m_sSweepSpeed;
                    }
                }
                else
                    return false;
            }
            else if (IsFileExtensionType(sFilename, _SNANORM_File_Extension))
            {
                if ((m_objRFEGen == null) || (!m_objRFEGen.PortConnected))
                    return false; //we can load and update connected generator only

                return LoadFileSNANormalization(sFilename);
            }
            else
                return false;

            return true;
        }

        /// <summary>
        /// Clean all screen data and reinitialize internal index counter
        /// </summary>
        public void CleanScreenData()
        {
            ScreenData.CleanAll();
            ScreenIndex = 0;
        }


        #endregion

        #region Tracking Generator
        bool m_bTrackingNormalizing = false; //true if tracking and normalizing

        UInt16 m_nTrackingNormalizingPass = 0;
        /// <summary>
        /// number of normalization tracking pass completed
        /// </summary>
        public UInt16 TrackingNormalizingPass
        {
            get { return m_nTrackingNormalizingPass; }
        }

        private bool m_bTrackingAllowed = false; //true if main thread allows secondary thread to use tracking
        private bool m_bThreadTrackingEnabled = false;

        UInt16 m_nTrackingPass = 0;
        /// <summary>
        /// number of tracking pass completed
        /// </summary>
        public UInt16 TrackingPass
        {
            get { return m_nTrackingPass; }
        }

        /// <summary>
        /// true if the current tracking mode is for normalization response
        /// </summary>
        public bool IsTrackingNormalizing
        {
            get { return m_bTrackingNormalizing; }
        }

        UInt16 m_nRFGenTracking_CurrentSweepDataPoint = 0; //step used dynamically while doing tracking
        /// <summary>
        /// Current tracking step being measured within the sweep
        /// </summary>
        public UInt16 RFGenTrackingCurrentStep
        {
            get { return m_nRFGenTracking_CurrentSweepDataPoint; }
        }

        public double GetSignalGeneratorEstimatedAmplitude(double dFrequencyMHZ)
        {
            return m_RFGenCal.GetEstimatedAmplitude(dFrequencyMHZ, m_bRFGenHighPowerSwitch, m_nRFGenPowerLevel);
        }

        /// <summary>
        /// Based on actual power limits, return allowed power level
        /// </summary>
        /// <param name="fPowerDBM">input power to adjust in dBm</param>
        /// <param name="bUncal">will return true if the value returned is uncalibrated level, false if calibrated. Suitable for bCalibratedOnly=false</param>
        /// <param name="bCalibratedOnly">true to allow calibrated values only, false to allow all values even if uncalibrated</param>
        /// <returns></returns>
        public double GetSignalGeneratorExpansionAdjustedAmplitude(double fPowerDBM, bool bCalibratedOnly, out bool bUncal)
        {
            bUncal = false;

            if (fPowerDBM < RFGENEXP_MIN_UNCAL_DBM)
                fPowerDBM = RFGENEXP_MIN_UNCAL_DBM;
            if (fPowerDBM < RFGENEXP_MIN_CAL_DBM)
            {
                if (bCalibratedOnly)
                    fPowerDBM = RFGENEXP_MIN_CAL_DBM;
                else
                    bUncal = true;
            }

            if (RFGenCWFrequencyMHZ <= 1f)
            {
                if (fPowerDBM > 0f)
                {
                    if (bCalibratedOnly)
                        fPowerDBM = 0f;
                    else
                        bUncal = true;
                }
            }
            else if (RFGenCWFrequencyMHZ <= 10f)
            {
                if (fPowerDBM > 5f)
                {
                    if (bCalibratedOnly)
                        fPowerDBM = 5f;
                    else
                        bUncal = true;
                }
            }
            else if (RFGenCWFrequencyMHZ <= 3200f)
            {
                if (fPowerDBM > 15f)
                    fPowerDBM = 15f;
            }
            else if (RFGenCWFrequencyMHZ <= 3650f)
            {
                if (fPowerDBM > 10f)
                {
                    if (bCalibratedOnly)
                        fPowerDBM = 10f;
                    else
                        bUncal = true;
                }
            }
            else if (RFGenCWFrequencyMHZ <= 5200f)
            {
                if (fPowerDBM > 15f)
                    fPowerDBM = 15f;
            }
            else
            {
                if (fPowerDBM > 0f)
                {
                    if (bCalibratedOnly)
                        fPowerDBM = 0f;
                    else
                        bUncal = true;
                }
            }
            if (fPowerDBM > 10f)
            {
                if (bCalibratedOnly)
                    fPowerDBM = 10f;
                else
                    bUncal = true;
            }

            return fPowerDBM;
        }

        /// <summary>
        /// Returns whether the RF Generator expansion has calibration data or not
        /// </summary>
        /// <returns>true if calibration is available</returns>
        public bool IsRFGenExpansionCalAvailable()
        {
            return (m_arrRFGenExpansionCal != null) && (m_arrRFGenExpansionCal.Length > 0);
        }

        /// <summary>
        /// Returns calibration data for RF Generator mainboard
        /// </summary>
        /// <returns>RFE6GEN_CalibrationData object array</returns>
        public RFE6GEN_CalibrationData GetRFE6GENCal()
        {
            return m_RFGenCal;
        }

        /// <summary>
        /// Returns current power setting in string data format H,P or +/-NN.Y depending on mainboard or expansion enabled
        /// </summary>
        /// <returns></returns>
        private string GetRFGenPowerString()
        {
            string sPower = ",";
            if (m_bExpansionBoardActive)
            {
                sPower += RFGenExpansionPowerDBM.ToString("+00.0;-00.0");
            }
            else
            {
                if (RFGenHighPowerSwitch)
                    sPower += "1,";
                else
                    sPower += "0,";
                sPower += RFGenPowerLevel;
            }

            return sPower;
        }

        //used to temporarily store the configuration of the analyzer before it goes to tracking mode
        double m_BackupStartMHZ, m_BackupStopMHZ, m_BackupTopDBM, m_BackupBottomDBM;

        /// <summary>
        /// Start and completes asynchronous tracking sequence, this action is performed on the Analyzer and will internally
        /// drive and handle the Generator.
        /// </summary>
        /// <param name="bNormalize">If true, the sequence will be saved as normalization sequence</param>
        /// <returns>true if sequence started correctly, false otherwise</returns>
        public bool StartTrackingSequence(bool bNormalize)
        {
            bool bOk = true;

            if (IsGenerator())
            {
                ReportLog("Invalid command sent to RF Explorer Signal Generator (StartTrackingSequence)");
                return false; //This can only be used in the analyzer
            }

            if ((m_objRFEGen == null) || (!m_objRFEGen.PortConnected))
            {
                //Signal Generator not connected or available
                ReportLog("RF Explorer Signal Generator not connected");
                return false;
            }

            //enable normalization and save prior analyzer configuration
            m_nTrackingNormalizingPass = 0;
            m_nTrackingPass = 0;
            m_bTrackingNormalizing = bNormalize;
            if (bNormalize)
            {
                ResetTrackingNormalizedData();
            }

            //Backup current configuration
            m_BackupStartMHZ = StartFrequencyMHZ;
            m_BackupStopMHZ = StopFrequencyMHZ;
            m_BackupTopDBM = AmplitudeTopDBM;
            m_BackupBottomDBM = AmplitudeBottomDBM;

            //start actual tracking
            m_bTrackingAllowed = true; //tell thread we allow tracking being enabled
            m_nRFGenTracking_CurrentSweepDataPoint = 0;

            //Only reach this code if RFE6GEN generator is being used
            m_objRFEGen.SendCommand_GeneratorSweepFreq(true);
            m_objRFEGen.m_bRFGenPowerON = true;
            Thread.Sleep(500); //wait for the Generator to stabilize power.
            //Set frequency configuration of the analyzer as frequency of the generator + offset.
            SendCommand("C3-K:" + ((UInt32)((m_objRFEGen.RFGenStartFrequencyMHZ + FrequencyOffsetMHZ) * 1000)).ToString("D7") + "," + ((UInt32)(m_objRFEGen.RFGenStepMHZ() * 1000)).ToString("D7"));

            return bOk;
        }

        /// <summary>
        /// Start CW generation using current configuration setting values - only valid for Signal Generator models
        /// </summary>
        public void SendCommand_GeneratorCW()
        {
            if (IsGenerator())
            {
                if (m_bExpansionBoardActive)
                    SendCommand("C5-F:" + ((UInt32)(RFGenCWFrequencyNormalizedMHZ * 1000)).ToString("D7") + "," + RFGenExpansionPowerDBM.ToString("+00.0;-00.0"));
                else
                    SendCommand("C3-F:" + ((UInt32)(RFGenCWFrequencyNormalizedMHZ * 1000)).ToString("D7") + GetRFGenPowerString());
            }
        }

        /// <summary>
        /// Start Sweep Freq generation using current configuration setting values - only valid for Signal Generator models
        /// </summary>
        /// <param name="bTracking">default is false to work in sweep mode, set it to 'true' to enable SNA tracking mode in generator</param>
        public void SendCommand_GeneratorSweepFreq(bool bTracking = false)
        {
            if (IsGenerator())
            {
                string sSteps = "," + RFGenSweepSteps.ToString("D4") + ",";

                double dStepMHZ = RFGenStepMHZ();
                if (dStepMHZ < 0)
                {
                    return;
                }

                string sCommand = "C3-";
                if (m_bExpansionBoardActive)
                {
                    sCommand = "C5-";
                }
                if (bTracking)
                {
                    sCommand += 'T';
                    m_eMode = eMode.MODE_NONE;
                }
                else
                    sCommand += 'F';
                sCommand += ":" + ((UInt32)(RFGenStartFrequencyNormalizedMHZ * 1000)).ToString("D7") + GetRFGenPowerString() + sSteps +
                    ((UInt32)(dStepMHZ * 1000)).ToString("D7") + "," + RFGenStepWaitMS.ToString("D5");

#if DEBUG
                if (bTracking)
                {
                    ReportLog("Generator Tracking command:" + sCommand.ToString());
                    Console.WriteLine("Generator Tracking command:" + sCommand.ToString());
                }
#endif

                SendCommand(sCommand);
            }
        }

        /// <summary>
        /// Start Sweep Amplitude generation using current configuration setting values - only valid for Signal Generator models
        /// </summary>
        public void SendCommand_GeneratorSweepAmplitude()
        {
            if (IsGenerator())
            {
                string sCommand = "C3-A:";
                string sSteps = RFGenSweepSteps.ToString("D4");

                if (m_bExpansionBoardActive)
                {
                    sCommand = "C5-A:" + ((UInt32)(RFGenCWFrequencyNormalizedMHZ * 1000)).ToString("D7") + "," +
                        RFGenExpansionPowerStartDBM.ToString("+00.0;-00.0") + "," +
                        RFGenExpansionPowerStepDB.ToString("+00.0;-00.0") + "," +
                        RFGenExpansionPowerStopDBM.ToString("+00.0;-00.0") + "," +
                        RFGenStepWaitMS.ToString("D5");
                }
                else
                {
                    string sStartPower = ",";
                    if (RFGenStartHighPowerSwitch)
                        sStartPower += "1,";
                    else
                        sStartPower += "0,";
                    sStartPower += RFGenStartPowerLevel + ",";

                    string sStopPower = ",";
                    if (RFGenStopHighPowerSwitch)
                        sStopPower += "1,";
                    else
                        sStopPower += "0,";
                    sStopPower += RFGenStopPowerLevel + ",";

                    sCommand += ((UInt32)(RFGenCWFrequencyNormalizedMHZ * 1000)).ToString("D7") + sStartPower + sSteps +
                        sStopPower + RFGenStepWaitMS.ToString("D5");
                }

                SendCommand(sCommand);
            }
        }

        /// <summary>
        /// Configured tracking step size in MHZ
        /// </summary>
        /// <returns></returns>
        public double RFGenStepMHZ()
        {
            return (RFGenStopFrequencyMHZ - RFGenStartFrequencyMHZ) / RFGenSweepSteps;
        }

        public void StopTracking()
        {
            //use backed up configuration to start back in analyzer mode
            m_bTrackingAllowed = false; //tell thread the tracking must stop
            m_bTrackingNormalizing = false;

            m_objRFEGen.SendCommand_GeneratorRFPowerOFF();

            int nWaitInd = 0;
            while (m_bThreadTrackingEnabled)
            {
                //wait till tracking sweep is done before changing unit to spectrum analyzer mode
                Thread.Sleep(100);
                nWaitInd++;
                if (nWaitInd > 100)
                {
                    //too much to keep waiting
                    m_bThreadTrackingEnabled = false; //force end of tracking
                    break;
                }
            }
            UpdateDeviceConfig(m_BackupStartMHZ, m_BackupStopMHZ, m_BackupTopDBM, m_BackupBottomDBM);
        }

        #endregion

        #region COM port low level details
        private void GetPortNames()
        {
            m_arrConnectedPorts = System.IO.Ports.SerialPort.GetPortNames();

            for (int nInd = 0; nInd < m_arrConnectedPorts.Length; nInd++)
            {
                string[] arrText = m_arrConnectedPorts[nInd].Split('\0');
                m_arrConnectedPorts[nInd] = arrText[0];
            }
        }

        public bool GetConnectedPorts()
        {
            if (IsRaspberryPlatform() && g_bIsIOT)
                return true;
            try
            {
                GetValidCOMPorts();
                if (m_arrValidCP2102Ports != null && m_arrValidCP2102Ports.Length > 0)
                {
                    string sPorts = "";
                    foreach (string sValue in m_arrValidCP2102Ports)
                    {
                        sPorts += sValue + " ";
                    }
                    ReportLog("RF Explorer Valid Ports found: " + sPorts.Trim());
                    return true;
                }
                else
                {
                    ReportLog("ERROR: No valid RF Explorer COM ports available\r\nConnect RFExplorer and click on [*]");
                }
            }
            catch (Exception obEx)
            {
                ReportLog("Error scanning COM ports: " + obEx.Message);
            }
            return false;
        }


        /// <summary>
        /// call stty Unix command to setup baudrate. This is the best way for Mono to support custom baudrates not supported by libraries.
        /// </summary>
        /// <param name="portName">port name such as /dev/tty0</param>
        /// <param name="baudRate">baud rate in bps</param>
        void ForceSetBaudRate(string portName, int baudRate)
        {
            try
            {
                string sArguments = String.Format("-F {0} speed {1}", portName, baudRate);

                ReportLog("stty command: " + sArguments, true);
                var proc = new Process
                {
                    EnableRaisingEvents = false,
                    StartInfo = { FileName = @"stty", Arguments = sArguments }
                };
                proc.Start();
                proc.WaitForExit();
            }
            catch (Exception objEx)
            {
                ReportLog(objEx.Message, false);
                ReportLog(objEx.ToString(), true);
            }
        }


        /// <summary>
        /// Connect serial port and start init sequence if AutoConfigure property is set
        /// </summary>
        /// <param name="PortName">serial port name, can take any form accepted by OS</param>
        /// <param name="nBaudRate">usually 500000 or 2400, can be -1 to not define it and take default setting</param>
        /// <param name="bUnix">Default to false. If enabled, will do a Unix call to setup baudrate, required on Linux and Raspbian system, not required in MacOS</param>
        /// <param name="bForceBaudrate">Default to false. If enabled, will do a Unix call to setup baudrate, required on Linux and Raspbian system required when using -1 in baudrate</param>
        public void ConnectPort(string PortName, int nBaudRate, bool bUnix = false, bool bForceBaudrate = false)
        {

            m_bPortConnected = false;
            try
            {
                Monitor.Enter(m_serialPortObj);
                if ((nBaudRate != -1) && !bForceBaudrate)
                    m_serialPortObj.BaudRate = nBaudRate;
                m_serialPortObj.DataBits = 8;
                m_serialPortObj.StopBits = StopBits.One;
                m_serialPortObj.Parity = Parity.None;
                m_serialPortObj.PortName = PortName;
                m_serialPortObj.ReadTimeout = 100;
                m_serialPortObj.WriteBufferSize = 1024;
                m_serialPortObj.ReadBufferSize = 8192;
                m_serialPortObj.Open();
                m_serialPortObj.Handshake = Handshake.None;
                m_serialPortObj.Encoding = Encoding.GetEncoding(28591); //this is the great trick to use ASCII and binary together

                m_bPortConnected = true;

                HoldMode = false;
                m_bDataFromFile = false;
                //Issue found in 1.23.1711.1 testing. We need to initialize input stage to Direct by default
                //so all models starts in a known status.
                m_eInputStage = eInputStage.Direct;

                ReportLog("Connected: " + m_serialPortObj.PortName.ToString() + ", " + m_serialPortObj.BaudRate.ToString() + " bauds");
                if (!bForceBaudrate)
                    OnPortConnected(new EventArgs());

                Thread.Sleep(500);
                if (m_bAutoConfigure && !bForceBaudrate)
                {
                    SendCommand_RequestConfigData();
                    Thread.Sleep(500);
                }
            }
            catch (Exception obException)
            {
                ReportLog("ERROR ConnectPort: " + obException.Message);
            }
            finally
            {
                Monitor.Exit(m_serialPortObj);
            }

            if (m_bPortConnected && ((nBaudRate > 115200) || bForceBaudrate) && bUnix)
            {
                //For unix we now force to value we want in the actual OS
                ForceSetBaudRate(PortName, nBaudRate);
                OnPortConnected(new EventArgs());
                Thread.Sleep(500);
                SendCommand_RequestConfigData();
                Thread.Sleep(500);
                if (m_bDebugTraces)
                {
                    ReportLog("Used Unix stty to setup baudrate to " + nBaudRate);
                }
            }
        }

        /// <summary>
        /// Close serial port connection
        /// </summary>
        /// <param name="bSendCommands">send commands to device while closing, or false to ignore</param>
        public void ClosePort(bool bSendCommand = true)
        {
            try
            {
                Monitor.Enter(m_serialPortObj);
                if (m_serialPortObj.IsOpen)
                {
                    try
                    {
                        if (m_nMainThreadId == Thread.CurrentThread.ManagedThreadId)
                            OnPortClosing(new EventArgs());
                    }
                    catch (Exception objEx)
                    {
                        ReportLog(objEx.ToString(), true);
                    }

                    if (bSendCommand)
                    {
                        Thread.Sleep(200);
                        if (IsAnalyzer())
                        {
                            if (m_eMode == eMode.MODE_SNIFFER)
                            {
                                //Force device to configure in Analyzer mode if disconnected - C0 will be ignored so we send full config again
                                SendCommand("C2-F:" + (StartFrequencyMHZ * 1000.0f).ToString() + "," + (StopFrequencyMHZ * 1000.0f).ToString() + "," +
                                AmplitudeTopDBM.ToString() + "," + AmplitudeBottomDBM.ToString());
                            }
                            if (m_eMode != eMode.MODE_SPECTRUM_ANALYZER && m_eMode != eMode.MODE_SNIFFER)
                            {
                                //If current mode is not analyzer, send C0 to force it
                                SendCommand_RequestConfigData();
                            }

                            Thread.Sleep(200);
                            SendCommand_Hold(); //Switch data dump to off
                            Thread.Sleep(200);
                            if (m_serialPortObj.BaudRate < 115200)
                                Thread.Sleep(2000);
                        }
                        else
                        {
                            SendCommand_GeneratorRFPowerOFF();
                            Thread.Sleep(200);
                        }
                        Thread.Sleep(200);
                        SwitchDeviceToNormalResolution();
                        SendCommand_ScreenON();
                        SendCommand_DisableScreenDump();
                    }
                    //Close the port
                    ReportLog("Disconnected.");
                    m_serialPortObj.Close();

                    Monitor.Enter(m_arrReceivedData);
                    m_arrReceivedData.Clear();
                    Monitor.Exit(m_arrReceivedData);
                }
                m_bPortConnected = false; //do this here so the external event has the right port status
                if (m_nMainThreadId == Thread.CurrentThread.ManagedThreadId)
                    OnPortClosed(new EventArgs());
            }
            catch { }
            finally
            {
                Monitor.Exit(m_serialPortObj);
            }
            m_bPortConnected = false; //to be double safe in case of exception
            m_eMainBoardModel = eModel.MODEL_NONE;
            m_eExpansionBoardModel = eModel.MODEL_NONE;
            m_eActiveModel = eModel.MODEL_NONE;
            //Restore input stage when device is disconnected to not consider InputStage attenuation
            m_eInputStage = eInputStage.Direct;

            m_LastCaptureTime = new DateTime(2000, 1, 1);

            m_sSerialNumber = "";
            m_sExpansionSerialNumber = "";
            m_bAudioPro = false;
            m_sRFExplorerFirmware = _FIRMWARE_NOT_AVAILABLE;
            m_nRetriesCalibration = 0;
            m_arrSpectrumAnalyzerEmbeddedCalibrationOffsetDB = null;
            m_arrSpectrumAnalyzerExpansionCalibrationOffsetDB = null;
            m_arrRFGenExpansionCal = null;
            //T0081: Case close port RFGen do not Reset Tracking Normalize Data in object Analyzer
            ResetTrackingNormalizedData();
            m_RFGenCal.DeleteCal();

            if (m_nMainThreadId == Thread.CurrentThread.ManagedThreadId)
                GetConnectedPorts();
        }

        /// <summary>
        /// Report data log for all connected compatible serial ports found in the system (valid for Windows only)
        /// </summary>
        public void ListAllCOMPorts()
        {
            string csSubkey = "SYSTEM\\CurrentControlSet\\Control\\Class\\{4D36E978-E325-11CE-BFC1-08002BE10318}";
            RegistryKey regPortKey = Registry.LocalMachine.OpenSubKey(csSubkey, RegistryKeyPermissionCheck.ReadSubTree, System.Security.AccessControl.RegistryRights.QueryValues | System.Security.AccessControl.RegistryRights.EnumerateSubKeys);

            string[] arrPortIndexes = regPortKey.GetSubKeyNames();
            ReportLog("Found total ports: " + arrPortIndexes.Length.ToString());

            //List all configured ports and driver versions for CP210x
            foreach (string sPortIndex in arrPortIndexes)
            {
                try
                {
                    RegistryKey regPort = regPortKey.OpenSubKey(sPortIndex, RegistryKeyPermissionCheck.ReadSubTree, System.Security.AccessControl.RegistryRights.QueryValues);

                    ReportLog("COM port index: " + sPortIndex);
                    if (regPort != null)
                    {
                        Object obDriverDesc = regPort.GetValue("DriverDesc");
                        string sDriverDesc = obDriverDesc.ToString();
                        ReportLog("   DriverDesc: " + sDriverDesc);
                        if (!sDriverDesc.Contains("CP210x"))
                            continue; //if it is not a Silicon Labs CP2102, ignore next steps
                        Object obCOMID = regPort.GetValue("AssignedPortForQCDevice");
                        if (obCOMID != null)
                            ReportLog("   AssignedPortForQCDevice: " + obCOMID.ToString());
                        Object obDriverVersion = regPort.GetValue("DriverVersion");
                        ReportLog("   DriverVersion: " + obDriverVersion.ToString());
                        Object obDriverDate = regPort.GetValue("DriverDate");
                        ReportLog("   DriverDate: " + obDriverDate.ToString());
                        Object obMatchingDeviceId = regPort.GetValue("MatchingDeviceId");
                        ReportLog("   MatchingDeviceId: " + obMatchingDeviceId.ToString());
                    }
                }
                catch (SecurityException) { }
                catch (Exception obEx) { ReportLog(obEx.ToString()); };
            }
        }

        private bool IsConnectedPort(string sPortName)
        {
            foreach (string sPort in m_arrConnectedPorts)
            {
                if (sPort == sPortName)
                    return true;
            }
            return false;
        }

        private bool IsRepeatedPort(string sPortName)
        {
            if (m_arrValidCP2102Ports == null)
                return false;

            foreach (string sPort in m_arrValidCP2102Ports)
            {
                if (sPort == sPortName)
                    return true;
            }
            return false;
        }

        bool m_bShowDetailedCOMPortInfo = true;
        /// <summary>
        /// Get/set level of detail when scanning for serial ports
        /// </summary>
        public bool ShowDetailedCOMPortInfo
        {
            get { return m_bShowDetailedCOMPortInfo; }
            set { m_bShowDetailedCOMPortInfo = value; }
        }

        void GetValidCOMPorts()
        {
            GetPortNames();

            if (GetAllPorts)
            {
                m_arrValidCP2102Ports = m_arrConnectedPorts;
            }
            else
            {
                if (m_bUnix)
                {
                    if (IsMacOSPlatform())
                        GetValidCOMPorts_Mac();
                    else
                        GetValidCOMPorts_Unix();
                }
                else if (m_bWine)
                {
                    GetValidCOMPorts_Wine();
                }
                else
                {
                    GetValidCOMPorts_Windows();
                }
            }
        }

        /// <summary>
        /// Returns true if the system is found to be a Raspberry Pi
        /// </summary>
        public static bool IsRaspberryPlatform()
        {
            //This code only get if is RPi platform once, and avoid continuous exception when GetConnectedPorts() is called
            if (m_eIsRaspberry == ePlatformChecked.FALSE)
                return false;
            else if (m_eIsRaspberry == ePlatformChecked.TRUE)
                return true;

            //Platform not recognised yet
            bool bIsRPi = false;
            try
            {
                if (File.Exists("/proc/device-tree/model")) //First check if file exists to avoid exception
                {
                    using (StreamReader objFile = new StreamReader("/proc/device-tree/model"))
                    {
                        string sVersion = objFile.ReadToEnd();
                        bIsRPi = sVersion.Contains("Raspberry");
                    }
                }
            }
            catch
            {
                bIsRPi = false;
            }
            finally
            {
                if (bIsRPi)
                    m_eIsRaspberry = ePlatformChecked.TRUE;
                else
                    m_eIsRaspberry = ePlatformChecked.FALSE;
            }
            return bIsRPi;
        }

        //From Managed.Windows.Forms/XplatUI
        [DllImport("libc")]
        static extern int uname(IntPtr buf);

        /// <summary>
        /// Returns true if it is a MacOS computer (internally identified as Darwin in Mono... that's right)
        /// </summary>
        public static bool IsMacOSPlatform()
        {
            if (m_eIsMacOS == ePlatformChecked.FALSE)
                return false;
            else if (m_eIsMacOS == ePlatformChecked.TRUE)
                return true;

            //Platform not recognised yet
            IntPtr buf = IntPtr.Zero;
            bool bIsMacOS = false;
            try
            {
                buf = Marshal.AllocHGlobal(8192);
                // This is a hacktastic way of getting sysname from uname ()
                if (uname(buf) == 0)
                {
                    string os = Marshal.PtrToStringAnsi(buf);
                    if (os == "Darwin")
                    {
                        bIsMacOS = true;
                    }
                }
            }
            catch { }
            finally
            {
                if (buf != IntPtr.Zero)
                    Marshal.FreeHGlobal(buf);
                if (bIsMacOS)
                    m_eIsMacOS = ePlatformChecked.TRUE;
                else
                    m_eIsMacOS = ePlatformChecked.FALSE;
            }
            return bIsMacOS;
        }

        bool IsValidCP210x_Unix(string sPort)
        {
            bool bReturn = false;

            try
            {
                //sPort comes as /dev/ttyUSB0 and we need ttyUSB0 only
                string[] arrParameters = sPort.Split('/');
                string sPortName = arrParameters[2];

                //Open a shell query to check if it is a CP210x device
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "ls"; // Specify exe name.
                start.Arguments = " -al /sys/class/tty/" + sPortName + "//device/driver";
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                //
                // Start the process.
                //
                using (Process process = Process.Start(start))
                {
                    // Read in all the text from the process with the StreamReader.
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        ReportLog(result);
                        if (result.Contains("cp210x"))
                            bReturn = true;
                    }
                }
            }
            catch { bReturn = false; }

            return bReturn;
        }

        void GetValidCOMPorts_Wine()
        {
            try
            {
                //we cannot really filter by CP2102 port name under Wine as the driver is not visible
                m_arrValidCP2102Ports = SerialPort.GetPortNames();
            }
            catch (Exception obEx)
            {
                ReportLog("Error looking for COM ports under Wine" + Environment.NewLine);
                ReportLog(obEx.ToString());
            }
            string sTotalPortsFound = "0";
            if (m_arrValidCP2102Ports != null)
                sTotalPortsFound = m_arrValidCP2102Ports.Length.ToString();
            ReportLog("Total ports found (may not be all from RF Explorer): " + sTotalPortsFound + Environment.NewLine);
        }

        void GetValidCOMPorts_Mac()
        {
            m_arrValidCP2102Ports = null;

            List<string> listValidPorts = new List<string>();

            //only check this in a true Mac box
            foreach (string sPortName in m_arrConnectedPorts)
            {
                if (sPortName.Length > 0 && sPortName.Contains("USB"))
                {
                    if (sPortName.Contains("SLAB_USB"))
                    {
                        listValidPorts.Add(sPortName);
                        ReportLog("Valid USB port: " + sPortName);
                    }
                }
            }

            m_arrValidCP2102Ports = listValidPorts.ToArray();
        }

        void GetValidCOMPorts_Unix()
        {
            m_arrValidCP2102Ports = null;

            List<string> listValidPorts = new List<string>();
            if (IsRaspberryPlatform() && g_bIsIOT)
            {
                //If it is IOT mode, check for AMA port
                foreach (string sPortName in m_arrConnectedPorts)
                {
                    if (sPortName.Contains("AMA"))
                        listValidPorts.Add(sPortName);
                }
            }
            else
            {
                //only check this in a true linux box or a Raspberry not using IOT mode
                foreach (string sPortName in m_arrConnectedPorts)
                {
                    if (sPortName.Length > 0 && sPortName.Contains("USB"))
                    {
                        if (IsValidCP210x_Unix(sPortName))
                        {
                            listValidPorts.Add(sPortName);
                            ReportLog("Valid USB port: " + sPortName);
                        }
                    }
                }
            }
            m_arrValidCP2102Ports = listValidPorts.ToArray();
        }

        /// <summary>
        /// This function checks in registry to detect if Silbas CP210x driver was installed
        /// It does not guarantee driver dll is actually available or correctly installed
        /// </summary>
        /// <returns>true if driver was found, false otherwise</returns>
        public static bool IsDriverInstalledWindows()
        {
            RegistryKey regUSBKey = null;
            try
            {
                string csSubkey = "SYSTEM\\CurrentControlSet\\Enum\\USB\\VID_10C4&PID_EA60";
                regUSBKey = Registry.LocalMachine.OpenSubKey(csSubkey, RegistryKeyPermissionCheck.ReadSubTree, System.Security.AccessControl.RegistryRights.QueryValues | System.Security.AccessControl.RegistryRights.EnumerateSubKeys);
            }
            catch { }

            return (regUSBKey != null);
        }

        void GetValidCOMPorts_Windows()
        {
            m_arrValidCP2102Ports = null;

            string csSubkey = "SYSTEM\\CurrentControlSet\\Enum\\USB\\VID_10C4&PID_EA60";
            RegistryKey regUSBKey = Registry.LocalMachine.OpenSubKey(csSubkey, RegistryKeyPermissionCheck.ReadSubTree, System.Security.AccessControl.RegistryRights.QueryValues | System.Security.AccessControl.RegistryRights.EnumerateSubKeys);

            if (regUSBKey == null)
            {
                ReportLog("Found no CP210x registry entries");
                return;
            }

            string[] arrDeviceCP210x = regUSBKey.GetSubKeyNames();
            if (m_bShowDetailedCOMPortInfo)
                ReportLog("Found total CP210x entries: " + arrDeviceCP210x.Length.ToString());
            //Iterate all driver for CP210x and get those with a valid connected COM port
            foreach (string sUSBIndex in arrDeviceCP210x)
            {
                try
                {
                    RegistryKey regUSBID = regUSBKey.OpenSubKey(sUSBIndex, RegistryKeyPermissionCheck.ReadSubTree, System.Security.AccessControl.RegistryRights.QueryValues | System.Security.AccessControl.RegistryRights.EnumerateSubKeys);
                    if (regUSBID != null)
                    {
                        Object obFriendlyName = regUSBID.GetValue("FriendlyName");
                        if (obFriendlyName != null)
                        {
                            if (m_bShowDetailedCOMPortInfo)
                                ReportLog("   FriendlyName: " + obFriendlyName.ToString());
                            RegistryKey regDevice = regUSBID.OpenSubKey("Device Parameters", RegistryKeyPermissionCheck.ReadSubTree, System.Security.AccessControl.RegistryRights.QueryValues);
                            if (regDevice != null)
                            {
                                object obPortName = regDevice.GetValue("PortName");
                                string sPortName = obPortName.ToString();
                                if (m_bShowDetailedCOMPortInfo)
                                    ReportLog("   PortName: " + sPortName);
                                if (IsConnectedPort(sPortName) && !IsRepeatedPort(sPortName))
                                {
                                    if (m_bShowDetailedCOMPortInfo)
                                        ReportLog(sPortName + " is a valid available port.");
                                    if (m_arrValidCP2102Ports == null)
                                    {
                                        m_arrValidCP2102Ports = new string[] { sPortName };
                                    }
                                    else
                                    {
                                        Array.Resize(ref m_arrValidCP2102Ports, m_arrValidCP2102Ports.Length + 1);
                                        m_arrValidCP2102Ports[m_arrValidCP2102Ports.Length - 1] = sPortName;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (SecurityException) { }
                catch (Exception obEx) { ReportLog(obEx.ToString()); };
            }
            long nTotalPortsFound = 0;
            if (m_arrValidCP2102Ports != null)
                nTotalPortsFound = m_arrValidCP2102Ports.Length;
            ReportLog("Total ports found: " + nTotalPortsFound);
        }
        #endregion

        #region OS Methods
#if !UNIX_LIKE
        /// <summary>
        /// Code to determine specific version of Windows
        /// </summary>
        /// <returns> Version of Windows that the system has </returns>
        public static string GetWinVersionString()
        {
            System.OperatingSystem osInfo = System.Environment.OSVersion;
            string osVersion = "Unknown";

            //Code to determine specific version of Windows NT 3.51,  
            //Windows NT 4.0, Windows 2000, or Windows XP. 
            switch (osInfo.Version.Major)
            {
                case 3:
                    osVersion = "Windows NT 3.51";
                    break;
                case 4:
                    osVersion = "Windows NT 4.0";
                    break;
                case 5:
                    switch (osInfo.Version.Minor)
                    {
                        case 0:
                            osVersion = "Windows 2000";
                            break;
                        case 1:
                            osVersion = "Windows XP";
                            break;
                        case 2:
                            osVersion = "Windows 2003";
                            break;
                    }
                    break;
                case 6:
                    switch (osInfo.Version.Minor)
                    {
                        case 0:
                            osVersion = "Windows Vista";
                            break;
                        case 1:
                            osVersion = "Windows 7";
                            break;
                        case 2:
                            osVersion = "Windows 8";
                            break;
                        case 3:
                            osVersion = "Windows 8.1";
                            break;
                    }
                    break;
                case 10:
                    osVersion = "Windows 10";
                    break;
            }
            return osVersion;
        }
#else
        /// <summary>
        /// Code to determine specific release version of MacOS
        /// </summary>
        /// <returns> Version of MacOS that the system has </returns>
        public static string GetMacOSReleaseVersionString()
        {
            OperatingSystem osInfo = Environment.OSVersion;
            string osVersion = "Unknown";
            //Code to determine specific version MacOS 
            switch (osInfo.Version.Major)
            {
                case 15:
                    osVersion = "macOS El Capitan v10.11";
                    switch (osInfo.Version.Minor)
                    {
                        case 0:
                            osVersion += ".0";
                            break;
                        case 6:
                            osVersion += ".6";
                            break;
                    }
                    break;
                case 16:
                    osVersion = "macOS Sierra v10.12";
                    switch (osInfo.Version.Minor)
                    {
                        case 0:
                            osVersion += ".0";
                            break;
                        case 5:
                            osVersion += ".4";
                            break;
                        case 6:
                            osVersion += ".6";
                            break;
                    }
                    break;
                case 17:
                    osVersion = "macOS High Sierra v10.13";
                    switch (osInfo.Version.Minor)
                    {
                        case 0:
                            osVersion += ".0";
                            break;
                        case 5:
                            osVersion += ".4";
                            break;
                        case 6:
                            osVersion += ".5";
                            break;
                        case 7:
                            osVersion += ".6";
                            break;
                    }
                    break;
                case 18:
                    osVersion = "macOS Mojave v10.14";
                    switch (osInfo.Version.Minor)
                    {
                        case 0:
                            osVersion += ".0";
                            break;
                        case 2:
                            osVersion += ".1";
                            break;
                    }
                    break;
                case 19:
                    osVersion = "macOS Catalina v10.15";
                    break;
            }
            return osVersion;
        }
#endif
        #endregion

        #region Events
        /// <summary>
        /// Optional
        /// An internal memory block is updated
        /// </summary>
        public event EventHandler MemoryBlockUpdateEvent;
        private void OnMemoryBlockUpdate(EventReportInfo eventArgs)
        {
            if (MemoryBlockUpdateEvent != null)
            {
                MemoryBlockUpdateEvent(this, eventArgs);
            }
        }

        /// <summary>
        /// Optional
        /// This event will fire everytime there is some information human readable to display
        /// </summary>
        public event EventHandler ReportInfoAddedEvent;
        private EventReportInfo[] m_arrInitialLogStrings = new EventReportInfo[100];
        private int m_nInitialLogStringsCounter = 0;
        private void OnReportInfoAdded(EventReportInfo eventArgs)
        {
            if (ReportInfoAddedEvent != null)
            {
                if (m_arrInitialLogStrings != null)
                {
                    //report waiting log entries from initialization before event callback was ready
                    for (int nInd = 0; nInd <= m_nInitialLogStringsCounter; nInd++)
                    {
                        EventReportInfo objEvent = m_arrInitialLogStrings[nInd];
                        if (objEvent != null)
                            ReportInfoAddedEvent(this, objEvent);
                        m_arrInitialLogStrings[nInd] = null;
                    }
                    m_arrInitialLogStrings = null;
                    m_nInitialLogStringsCounter = 0;
                }
                ReportInfoAddedEvent(this, eventArgs);
            }
            else
            {
                if ((m_arrInitialLogStrings != null) && (m_nInitialLogStringsCounter < (m_arrInitialLogStrings.Length - 1)))
                {
                    m_arrInitialLogStrings[m_nInitialLogStringsCounter] = eventArgs;
                    m_nInitialLogStringsCounter++;
                }
            }
        }

        /// <summary>
        /// Optional
        /// This event will fire when a wrong format response is received from RFE
        /// </summary>
        public event EventHandler WrongFormatDataEvent;
        private void OnWrongFormatData(EventArgs eventArgs)
        {
            if (WrongFormatDataEvent != null)
            {
                WrongFormatDataEvent(this, eventArgs);
            }
        }

        /// <summary>
        /// Optional
        /// This event will fire when a string identified as reset is received from RFE
        /// </summary>
        public event EventHandler DeviceReset;
        private void OnDeviceResetEvent(EventArgs eventArgs)
        {
            if (DeviceReset != null)
            {
                DeviceReset(this, eventArgs);
            }
        }

        /// <summary>
        /// Required
        /// This event will fire when RFE is sending its configuration back. This always come before any data dump starts.
        /// </summary>
        public event EventHandler ReceivedConfigurationDataEvent;
        private void OnReceivedConfigurationData(EventArgs eventArgs)
        {
            if (ReceivedConfigurationDataEvent != null)
            {
                ReceivedConfigurationDataEvent(this, eventArgs);
            }
        }

        /// <summary>
        /// Required
        /// This event will fire when RFE is sending global configuration back.
        /// </summary>
        public event EventHandler ReceivedDeviceModelEvent;
        private void OnReceivedDeviceModel(EventArgs eventArgs)
        {
            if (ReceivedDeviceModelEvent != null)
            {
                ReceivedDeviceModelEvent(this, eventArgs);
            }
        }

        /// <summary>
        /// Required
        /// This event will fire when RFE device is sending changes in Input Stage.
        /// </summary>
        public event EventHandler ReceivedDeviceInputStageEvent;
        private void OnReceivedDeviceInputStage(EventArgs eventArgs)
        {
            if (ReceivedDeviceInputStageEvent != null)
            {
                ReceivedDeviceInputStageEvent(this, eventArgs);
            }
        }

        /// <summary>
        /// Sniffer RF RAW data decoder packet has been received
        /// </summary>
        public event EventHandler SnifferRawDataEvent;
        private void OnRawSnifferData(EventArgs eventArgs)
        {
            if (SnifferRawDataEvent != null)
            {
                SnifferRawDataEvent(this, eventArgs);
            }
        }

        /// <summary>
        /// A Preset has been received
        /// </summary>
        public event EventHandler ReceivedPresetEvent;
        private void OnReceivedPreset(EventArgs eventArgs)
        {
            if (ReceivedPresetEvent != null)
            {
                ReceivedPresetEvent(this, eventArgs);
            }
        }


        /// <summary>
        /// Required
        /// This event indicates new data dump has been received from RF Explorer an is ready to be used
        /// </summary>
        public event EventHandler UpdateDataEvent;
        protected virtual void OnUpdateData(EventArgs e)
        {
            if (UpdateDataEvent != null)
            {
                UpdateDataEvent(this, e);
            }
        }

        private RFESweepDataCollection m_SweepTrackingNormalizedContainer;
        private RFESweepData m_SweepTrackingNormalized;
        /// <summary>
        /// Sweep data with values of last normalized tracking scan, valid for current configuration
        /// </summary>
        public RFESweepData TrackingNormalizedData
        {
            get { return m_SweepTrackingNormalized; }
            set { m_SweepTrackingNormalized = value; }
        }

        /// <summary>
        /// set to true to capture all possible COM ports regardless OS or versions
        /// </summary>
        public bool GetAllPorts
        {
            get
            {
                return m_bGetAllPorts;
            }

            set
            {
                m_bGetAllPorts = value;
            }
        }

        /// <summary>
        /// Get or set action to enable/disable storage of string BLOB for later use
        /// </summary>
        public bool UseStringBLOB
        {
            get
            {
                return m_bUseStringBLOB;
            }

            set
            {
                m_bUseStringBLOB = value;
            }
        }

        /// <summary>
        /// Get or set action to enable/disable storage of byte array BLOB for later use
        /// </summary>
        public bool UseByteBLOB
        {
            get
            {
                return m_bUseByteBLOB;
            }

            set
            {
                m_bUseByteBLOB = value;
            }
        }

        bool m_bStoreSweep = true;
        /// <summary>
        /// It gets or sets the capacity to store multiple historical sweep data in the <see cref>SweepData</ref> collection
        /// </summary>
        public bool StoreSweep
        {
            get { return m_bStoreSweep; }
            set { m_bStoreSweep = value; }
        }

        /// <summary>
        /// returns true if the normalization data is available and no item is lower than MIN_AMPLITUDE_TRACKING_NORMALIZE (considered too low for any valid normalization setup)
        /// </summary>
        /// <returns></returns>
        public bool IsTrackingNormalized()
        {
            bool bReturn = false;

            if (m_SweepTrackingNormalized != null)
            {
                bReturn = (m_SweepTrackingNormalized.TotalDataPoints > 0) && m_SweepTrackingNormalized.GetAmplitudeDBM(m_SweepTrackingNormalized.GetMinDataPoint()) > MIN_AMPLITUDE_TRACKING_NORMALIZE + AmplitudeOffsetDB + (Convert.ToInt32(InputStageAttenuationDB));
            }

            return bReturn;
        }

        /// <summary>
        /// removes any prior loaded normalization data
        /// </summary>
        public void ResetTrackingNormalizedData()
        {
            m_SweepTrackingNormalized = null;
            m_SweepTrackingNormalizedContainer = null;
        }

        /// <summary>
        /// Optional
        /// This event indicates Tracking Normalization data dump has been received from RF Explorer an is ready to be used
        /// </summary>
        public event EventHandler UpdateDataTrakingNormalizationEvent;
        protected virtual void OnUpdateDataTrakingNormalization(EventArgs e)
        {
            if (UpdateDataTrakingNormalizationEvent != null)
            {
                UpdateDataTrakingNormalizationEvent(this, e);
            }
        }

        /// <summary>
        /// Optional
        /// This event indicates Tracking data dump has been received from RF Explorer an is ready to be used
        /// </summary>
        public event EventHandler UpdateGPSDataEvent;
        protected virtual void OnUpdateGPSData(EventArgs e)
        {
            if (UpdateGPSDataEvent != null)
            {
                UpdateGPSDataEvent(this, e);
            }
        }

        /// <summary>
        /// Optional
        /// This event indicates Tracking data dump has been received from RF Explorer an is ready to be used
        /// </summary>
        public event EventHandler UpdateDataTrakingEvent;
        protected virtual void OnUpdateDataTraking(EventArgs e)
        {
            if (UpdateDataTrakingEvent != null)
            {
                UpdateDataTrakingEvent(this, e);
            }
        }

        /// <summary>
        /// Optional
        /// This event indicates new screen dump bitmap has been received from RF Explorer
        /// </summary>
        public event EventHandler UpdateRemoteScreenEvent;
        protected virtual void OnUpdateRemoteScreen(EventArgs e)
        {
            if (UpdateRemoteScreenEvent != null)
            {
                UpdateRemoteScreenEvent(this, e);
            }
        }

        /// <summary>
        /// This event will fire when the feed mode (real time or hold) has changed
        /// </summary>
        public event EventHandler UpdateFeedModeEvent;
        protected virtual void OnUpdateFeedMode(EventArgs e)
        {
            if (UpdateFeedModeEvent != null)
            {
                UpdateFeedModeEvent(this, e);
            }
        }

        /// <summary>
        /// This event will fire in the event of a communication port is connected
        /// </summary>
        public event EventHandler PortConnectedEvent;
        protected virtual void OnPortConnected(EventArgs e)
        {
            if (PortConnectedEvent != null)
            {
                PortConnectedEvent(this, e);
            }
        }

        /// <summary>
        /// This event will fire in the event of a communication port is about to be closed, either by manual user intervention or by a link error
        /// </summary>
        public event EventHandler PortClosingEvent;
        protected virtual void OnPortClosing(EventArgs e)
        {
            if (PortClosingEvent != null)
            {
                PortClosingEvent(this, e);
            }
        }

        /// <summary>
        /// This event will fire in the event of a communication port is closed, either by manual user intervention or by a link error
        /// </summary>
        public event EventHandler PortClosedEvent;
        protected virtual void OnPortClosed(EventArgs e)
        {
            if (PortClosedEvent != null)
            {
                PortClosedEvent(this, e);
            }
        }
        #endregion
    }
}
