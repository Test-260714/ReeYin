
/******************************************************************************
 * @file    SR7ApiBase.h
 * @brief   封装相机图API / Encapsulate camera image API
 * @author  
 * @date    2025-03-17
 * @version 1.0.0.0
 * @note    
 *
 * 封装取图API，使得调用相机库更简单、直观
 * Encapsulate the image acquisition API to make calling the camera library simpler and more intuitive
 * -查找相机 / Find camera
 * -打开相机 / Open camera
 * -初始化相机 / Initialize camera
 * -获取数据 / Get data
 * -设置相机参数 / Set camera parameters
 * -关闭相机 / Turn off camera
 *
 * @history
 * <Date>      <Author>    <Version>    <Description>
 * 2025-04-01     GK         v1.0.0       初始版本/initial version
 *
 * @copyright Copyright (c) 2014-2025 SinceVision. All rights reserved.
 ******************************************************************************/
using SR7Link;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SRAPI
{
    public class SRAPIException : Exception
    {
        public SRAPIException(string message) : base(message) { }
    }


    // 定义用户数据指针
    //using SR7IF_UserData = System.IntPtr;
    /// <summary>
    /// SR7 API 抽象基类
    /// </summary>
    public abstract class SR7ApiBase : ISR7Api
    {
        // 设备控制器ID，默认不可更改
        public int ControllerID { get; set; } = 0;
        //扫描行数 / Number of scan lines
        public int BatchPoints { get; set; } = 0;
        //无限循环回调行数 / Infinite loop callback line number
        //public int CallBackPoints { get; set; } = 0;

        public bool CameraAOnline { get; set; } = false;
        public bool CameraBOnline { get; set; } = false;

        //数据临时缓存 / Data temporary cache
        public IntPtr[] ImgBuff32 = new IntPtr[2];
        public IntPtr[] ImgBuff16 = new IntPtr[2];
        public IntPtr[] GrayBuff = new IntPtr[2];
        public IntPtr[] Encoder = new IntPtr[2];

        public SR7IFGetDataCallBack GetDataCallBack;

        //method
        public abstract int Open(int lDeviceId, string strIP, int timeout, ErrConnectCallBack pfErrCallBack);

        /// <summary>
        /// 初始化设备
        /// </summary>
        public abstract int Init(int dwProfileCnt, uint ProfileBits, int nTimeout, SR7IFGetDataCallBack pfGetDataCallBack);

        /// <summary>
        /// 启动设备
        /// </summary>
        public abstract int Start(bool bIOTrigger, int timeout);

        /// <summary>
        /// 停止设备（提供默认参数的重载）
        /// </summary>
        public int Stop() => Stop(0);
        public abstract int Stop(int instantStop);

        /// <summary>
        /// Close() 断开与相机的连接./Disconnect from the camera.
        /// </summary>
        /// <returns> <0:失败./Fail.  =0:成功./Success</returns>
        public virtual int Close()
        {
            int nRet = Stop();
            nRet = SR7LinkFunc.SR7IF_CommClose(ControllerID);
            CameraAOnline = false;
            CameraBOnline = false;
            return nRet;
        }

        public virtual int GetData(int[] pHighData, byte[] pGrayData, int[] pEncoderData, int nProfileWidth, int nHighlen, int ABCam)
        {
            if (ABCam < 0 || ABCam >= 2)
                return -1;

            int totalPoints = nProfileWidth * nHighlen;

            if (pHighData != null && ImgBuff32[ABCam] != IntPtr.Zero)
                Marshal.Copy(ImgBuff32[ABCam], pHighData, 0, Math.Min(totalPoints, pHighData.Length));

            if (pGrayData != null && GrayBuff[ABCam] != IntPtr.Zero)
                Marshal.Copy(GrayBuff[ABCam], pGrayData, 0, Math.Min(totalPoints, pGrayData.Length));

            if (pEncoderData != null && Encoder[ABCam] != IntPtr.Zero)
                Marshal.Copy(Encoder[ABCam], pEncoderData, 0, Math.Min(nHighlen, pEncoderData.Length));

            return 0;
        }

        public virtual int GetData(short[] pHighData, byte[] pGrayData, int[] pEncoderData, int nProfileWidth, int nHighlen, int ABCam)
        {
            if (ABCam < 0 || ABCam >= 2)
                return -1;

            int totalPoints = nProfileWidth * nHighlen;

            if (pHighData != null && ImgBuff16[ABCam] != IntPtr.Zero)
                Marshal.Copy(ImgBuff16[ABCam], pHighData, 0, Math.Min(totalPoints, pHighData.Length));

            if (pGrayData != null && GrayBuff[ABCam] != IntPtr.Zero)
                Marshal.Copy(GrayBuff[ABCam], pGrayData, 0, Math.Min(totalPoints, pGrayData.Length));

            if (pEncoderData != null && Encoder[ABCam] != IntPtr.Zero)
                Marshal.Copy(Encoder[ABCam], pEncoderData, 0, Math.Min(nHighlen, pEncoderData.Length));

            return 0;
        }
        /// <summary>
        /// 搜索可用摄像头IP（基类提供默认实现，但不强制子类实现）
        /// </summary>
        public virtual bool SearchCameraIP(out List<string> cameraIPs)
        {
            cameraIPs = new List<string>();
            int[] readNum = new int[1];

            try
            {
                // 使用传统的 using 语法
                using (var pin = new PinnedObject(readNum))
                {
                    IntPtr str_Setting = SR7Link.SR7LinkFunc.SR7IF_SearchOnline(pin.Pointer, 3000);

                    int deviceCount = readNum[0];
                    if (deviceCount <= 0)
                    {
                        return false;
                    }

                    byte[][] ipAddressList = new byte[deviceCount][];

                    for (int i = 0; i < deviceCount; i++)
                    {
                        byte[] ip = new byte[4];
                        Marshal.Copy(str_Setting + 4 * i, ip, 0, 4);
                        ipAddressList[i] = ip;
                        cameraIPs.Add(string.Join(".", ip));
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new SRAPIException($"Failed to search camera IPs: {ex.Message}");
            }
        }

        public int GetProfileDataWidth()
        {
            return SR7LinkFunc.SR7IF_ProfileDataWidth(ControllerID, IntPtr.Zero);
        }

        public int GetProfilePointSetCount()
        {
            return SR7LinkFunc.SR7IF_ProfilePointSetCount(ControllerID, IntPtr.Zero);
        }

        public double GetProfileData_XPitch()
        {
            return SR7LinkFunc.SR7IF_ProfileData_XPitch(ControllerID, IntPtr.Zero);
        }
        public int SR7IFGet25DSingleProfile(int[] pHighData, uint[] pEncoder)
        {
            return SR7LinkFunc.SR7IF_GetSingleProfile((uint)ControllerID, pHighData, pEncoder);
        }
        public int SR7IFGet3DSingleProfile(int[] pHighData)
        {
            return SR7LinkFunc.SR7IF_GetSingleProfile3DMode((uint)ControllerID, pHighData);
        }

        ///
		/// \brief SetParams  SetSetting.
		/// \param nAB               [in]        相机A为0，B为1 / Camera A is 0, B is 1
		/// \param nConfigNum        [in]        配方编号 -1：当前配方，0-63指定配方 / Recipe number -1: current recipe, 0-63 specifies the recipe
		/// \param nParamItemEnums   [in]        参数项枚举,参考SRSettingHeader.h SR7IF_SETTING_ITEM中定义
		///                                      Parameter item enumeration, refer to the definition in SR7IF_SETTING_ITEM in SRSettingHeader.h
		/// \param nSetData          [in]        设置数据 / Set data
		/// \return
		///     <0:                     失败./Fail.
		///     =0:                     成功./Success
		///
		public int SetParams(int nAB, int nSavePowerOff, int nConfigNum, SR7IF_SETTING_ITEM nParamItemEnums, int nSetData)
        {
            int nCategory = 0x00;
            int nItem = 0x00;
            int nDataSize = 1;
            if (GetParamsCategory(nParamItemEnums,ref nCategory,ref nItem,ref nDataSize) != 0)
            {
                return -2;
            }
            if (nConfigNum != -1)
                nConfigNum += 0x10;

            //强制小端模式传数据 / Force little-endian mode to transfer data
            byte[] pData = new byte[nDataSize];

            for (int i = 0; i < nDataSize; i++)
            {
                pData[i] = (byte)((nSetData >> (i * 8)) & 0xFF);
            }

            int nRet = 0;
            using (PinnedObject pin = new PinnedObject(pData))
            {
                nRet = SetParams(ControllerID, nSavePowerOff, nConfigNum, nCategory, nItem, nAB, pin, nDataSize);
            }

            
            return nRet;
        }
        ///
        /// \brief SetParams 设置参数 SetSetting.
        /// \param nDeviceId      [in]        控制器ID. / Controller ID
        /// \param nDepth         [in]        参数是否断电保存，0x01断电不保存，0x02断电保存
        ///                                   Whether the parameters are saved when the power is off, 0x01 is not saved when the power is off, 0x02 is saved when the power is off
        /// \param nFormulaNum    [in]        配方编号 -1：当前配方，0-63指定配方 / Recipe number -1: current recipe, 0-63 specifies the recipe
        /// \param nCategory      [in]        对应Edgeimage软件中，指定参数页，0：触发设定页面；1：拍摄设定页面；2：轮廓设定页面
        ///                                   Corresponding to the Edgeimage software, specify the parameter page, 0: trigger setting page; 1: shooting setting page; 2: contour setting page
        /// \param nItem          [in]        指定发送 / 接收 nCategory 指定项中的设定项 / Specifies to send/receive the setting items in the specified item of nCategory
        /// \param nAB            [in]        相机A为0，B为1 / Camera A is 0, B is 1
        /// \param pData          [in]        写入数据 / Write data
        /// \param nDataSize      [in]        写入数据的长度 / The length of the written data
        /// \return
        ///     <0:                     失败./Fail.
        ///     =0:                     成功./Success
        ///
        public int SetParams(int nDeviceId, int nSavePowerOff, int nFormulaNum, int nCategory, int nItem, int nAB, PinnedObject pData, int nDataSize)
        {
            int[] arrayTarget = new int[4] { nAB, 0, 0, 0 };
            int nRet = SR7LinkFunc.SR7IF_SetSetting((uint)ControllerID, nSavePowerOff, nFormulaNum, nCategory, nItem, arrayTarget, pData.Pointer, nDataSize);
            return nRet;

        }
        ///
        /// \brief GetParams 获取参数 GetSetting.
        /// \param nABCamera            [in]        相机A为0，B为1 / Camera A is 0, B is 1
        /// \param nConfigNum           [in]        配方编号 -1：当前配方，0-63指定配方 / Recipe number -1: current recipe, 0-63 specifies the recipe
        /// \param nParamItemEnums      [in]        参数项枚举,参考SRSettingHeader.h SR7IF_SETTING_ITEM中定义
        ///											Parameter item enumeration, refer to the definition in SR7IF_SETTING_ITEM in SRSettingHeader.h
        /// \param nOutData             [out]       获取参数值. / Get parameter value.
        /// \return
        ///     <0:                     失败./Fail.
        ///     =0:                     成功./Success
        ///
        public int GetParams(int nABCamera, int nConfigNum, SR7IF_SETTING_ITEM nParamItemEnums, out int nOutData)
        {
            nOutData = 0;
            int nCategory = 0x00;
            int nItem = 0x00;
            int nDataSize = 1;
            if (GetParamsCategory(nParamItemEnums,ref nCategory,ref nItem,ref nDataSize) != 0)
            {
                return -3;
            }
            if (nConfigNum != -1)
                nConfigNum += 0x10;

            int[] arrayTarget = new int[4] { nABCamera, 0, 0, 0 };
            
            byte[] pData = new byte[nDataSize];
            using (PinnedObject pin = new PinnedObject(pData))
            {
                int nRet = SR7LinkFunc.SR7IF_GetSetting((uint)ControllerID, nConfigNum, nCategory, nItem, arrayTarget, pin.Pointer, nDataSize);

                if (0 != nRet)
                {
                    return nRet;
                }
                //Get Data
                for (int i = 0; i < pData.Length; i++)
                {
                    nOutData += pData[i] * (int)Math.Pow(256, i);
                }
            }
            return 0;
        }

        public int GetParamsCategory(SR7IF_SETTING_ITEM nParamItemEnums, ref int nCategory, ref int nItem, ref int nDataSize)
        {
            switch (nParamItemEnums)
            {
                //00
                case SR7IF_SETTING_ITEM.TRIG_MODE://触发模式 / Trigger mode
                    nCategory = 0x00;
                    nItem = 0x01;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.BATCH_OUTPUT://批处理输出 / Batch processing output
                    nCategory = 0x00;
                    nItem = 0x21;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.SAMPLED_CYCLE://采样周期 / Sampling cycle
                    nCategory = 0x00;
                    nItem = 0x02;
                    nDataSize = 4;
                    break;

                case SR7IF_SETTING_ITEM.BATCH_ON_OFF://批处理开关 / Batch processing switch
                    nCategory = 0x00;
                    nItem = 0x03;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.ENCODER_TYPE://编码器类型 / Encoder type
                    nCategory = 0x00;
                    nItem = 0x0b;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.ENCODER_INPUTMODE://编码器输入模式 / Encoder input mode
                    nCategory = 0x00;
                    nItem = 0x07;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.REFINING_POINTS://细化点数（触发间隔） / Refining points
                    nCategory = 0x00;
                    nItem = 0x09;
                    nDataSize = 2;
                    break;

                case SR7IF_SETTING_ITEM.BATCH_POINT://批处理点数 / Batch processing points
                    nCategory = 0x00;
                    nItem = 0x0a;
                    nDataSize = 2;
                    break;

                case SR7IF_SETTING_ITEM.CYCLICAL_PATTERN://循环模式 0 关闭 1 打开 / Cyclical pattern (0: Off, 1: On)
                    nCategory = 0x00;
                    nItem = 0x10;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.SEGMENT_BUFER:// 分段存储 0 关闭 1 打开 / Segmented buffer (0: Off, 1: On)
                    nCategory = 0x00;
                    nItem = 0x11;
                    nDataSize = 1;
                    break;
                //01
                case SR7IF_SETTING_ITEM.Z_MEASURING_RANGE://Z方向测量范围 / Z-axis measuring range
                    nCategory = 0x01;
                    nItem = 0x03;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.SENSITIVITY://感光灵敏度 / Sensitivity
                    nCategory = 0x01;
                    nItem = 0x05;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.EXP_TIME://曝光时间 / Exposure time
                    nCategory = 0x01;
                    nItem = 0x06;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.LIGHT_CONTROL://光亮控制 / Light control
                    nCategory = 0x01;
                    nItem = 0x0B;
                    nDataSize = 1;
                    break;
                case SR7IF_SETTING_ITEM.LIGHT_MAX://激光亮度上限 / Laser brightness upper limit
                    nCategory = 0x01;
                    nItem = 0x0C;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.LIGHT_MIN://激光亮度下限 / Laser brightness lower limit
                    nCategory = 0x01;
                    nItem = 0x0D;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.PEAK_SENSITIVITY://峰值灵敏度 / Peak sensitivity
                    nCategory = 0x01;
                    nItem = 0x0F;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.PEAK_SELECT://峰值选择 / Peak selection
                    nCategory = 0x01;
                    nItem = 0x11;
                    nDataSize = 1;
                    break;
                //02
                case SR7IF_SETTING_ITEM.X_SAMPLING://X轴压缩设定 / X-axis compression setting
                    nCategory = 0x02;
                    nItem = 0x02;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.FILTER_X_MEDIAN://X轴中位数滤波 / X-axis median filter
                    nCategory = 0x02;
                    nItem = 0x0A;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.FILTER_Y_MEDIAN://Y轴(时间轴)中位数滤波 / Y-axis (time axis) median filter
                    nCategory = 0x02;
                    nItem = 0x0C;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.FILTER_X_SMOOTH://X轴平滑滤波 / X-axis smooth filter
                    nCategory = 0x02;
                    nItem = 0x0B;
                    nDataSize = 1;
                    break;

                case SR7IF_SETTING_ITEM.FILTER_Y_SMOOTH://Y轴平滑滤波（平均滤波） / Y-axis smooth filter (mean filter)
                    nCategory = 0x02;
                    nItem = 0x0D;
                    nDataSize = 2;
                    break;
                //30
                case SR7IF_SETTING_ITEM.CHANGE_3D_25D://3D/2.5D模式切换 / 3D/2.5D switch (In 2.5D mode, X-axis compression setting changes to default automatically)
                    nCategory = 0x30;
                    nItem = 0x00;
                    nDataSize = 1;
                    break;

                default:
                    nCategory = 0x00;
                    nItem = 0x00;
                    nDataSize = 1;
                    return -1;

            }
            return 0;
        }
        ///
        /// \brief SR7IF_Get16BitScale          获取16bit高度数据物理单位 / Get 16-bit height data in physical units
        /// \param Scale				[out]	16bit高度数据 单位(mm) / 16bit height data Unit (mm)
        /// \return
        ///     <0:                     失败./Fail.
        ///     =0:                     成功./Success
        ///
        public int Get16BitScale(out float Scale)
        {
            float[] scale1 = new float[1];
            int ret = 0;
            using (PinnedObject pin_size = new PinnedObject(scale1))
            {
                ret = SR7LinkFunc.SR7IF_Get16BitScale(0, pin_size.Pointer);
            }
            Scale = scale1[0];
            return ret;
        }
    }
}
