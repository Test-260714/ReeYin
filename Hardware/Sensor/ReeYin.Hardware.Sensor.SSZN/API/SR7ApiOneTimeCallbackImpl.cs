using SR7Link;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SRAPI
{
    /// <summary>
    /// SR7 API 一次性回调实现 / SR7 API one-time callback implementation
    /// </summary>
    public class SR7ApiOneTimeCallbackImpl : SR7ApiBase
    {
        private SR7IF_BatchOneTimeCallBack BatchOneTimeDelegate; //一次回调

        public override int Open(int lDeviceId, string strIP, int timeout, ErrConnectCallBack pfErrCallBack)
        {
            ControllerID = lDeviceId;

            string[] ipTmp = strIP.Split('.');
            if (ipTmp.Length != 4)
            {
                throw new SRAPIException($"Failed to resolve IP ");
            }

            SR7IF_ETHERNET_CONFIG ethernetConfig;
            ethernetConfig.abyIpAddress = new Byte[]
            {
                    Convert.ToByte(ipTmp[0]),
                    Convert.ToByte(ipTmp[1]),
                    Convert.ToByte(ipTmp[2]),
                    Convert.ToByte(ipTmp[3])
            };

            int openRet = SR7LinkFunc.SR7IF_EthernetOpenExt(ControllerID, ref ethernetConfig, 2000, pfErrCallBack);
            if (openRet != 0)
                return openRet;
            int nCameraB = SR7LinkFunc.SR7IF_GetOnlineCameraB(ControllerID);
            CameraAOnline = true;
            CameraBOnline = (nCameraB==0);
            return openRet; 
        }
        
        public override int Init(int dwProfileCnt, uint ProfileBits, int nTimeout, SR7IFGetDataCallBack pfGetDataCallBack)
        {
            GetDataCallBack = pfGetDataCallBack;
            BatchOneTimeDelegate = new SR7IF_BatchOneTimeCallBack(BatchOneTimeCallBack);
            int ret = SR7LinkFunc.SR7IF_SetBatchOneTimeDataHandler(ControllerID, BatchOneTimeDelegate);
            if (ret != 0)
                return ret;

            ret = SR7LinkFunc.SR7IF_StartMeasureWithCallback(ControllerID, 1);
            return ret;
        }
        
        public override int Start(bool bIOTrigger, int timeout)
        {
            int ret = 0;
            if (!bIOTrigger)
                ret = SR7LinkFunc.SR7IF_TriggerOneBatch(ControllerID);
            return ret;
        }
        
        public override int Stop(int instantStop)
        {
            int ret = SR7LinkFunc.SR7IF_StopMeasure(ControllerID);
            return ret;
        }
        /// <summary>
        /// 一次回调函数 / One Time callback function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="data"></param>
        public void BatchOneTimeCallBack(IntPtr info, IntPtr data)
        {
            Console.WriteLine("BatchOneTimeCallBack");
            SR7IF_STR_CALLBACK_INFO conInfo = new SR7IF_STR_CALLBACK_INFO();
            conInfo = (SR7IF_STR_CALLBACK_INFO)Marshal.PtrToStructure(info, typeof(SR7IF_STR_CALLBACK_INFO));

            if ( conInfo.returnStatus == (int)SR7IF_ERROR.SR7IF_OK)
            {
                int camIndex = conInfo.HeadNumber;//Number of cameras
                for (int i = 0; i < camIndex; i++)
                {
                    ImgBuff32[i] = SR7LinkFunc.SR7IF_GetBatchProfilePoint(data, i);//获取相机32bit高度数据 / Get the camera's 32-bit height data

                    GrayBuff[i] = SR7LinkFunc.SR7IF_GetBatchIntensityPoint(data, i);//获取相机灰度数据 / Get camera grayscale data

                    Encoder[i] = SR7LinkFunc.SR7IF_GetBatchEncoderPoint(data, i);//获取相机编码器数据 / Get camera encoder data
                }
               GetDataCallBack(conInfo.xPoints, conInfo.BatchPoints, 1, conInfo.returnStatus,0);
            }
            else
            {
                GetDataCallBack(0, 0, 1, conInfo.returnStatus,0);
            }
        }

    }
}
