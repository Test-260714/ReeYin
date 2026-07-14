using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SR7Link;

namespace SRAPI
{
    class SR7ApiSyncCallbackImpl : SR7ApiBase
    {
        private ErrConnectCallBack ConnectErrCallBack;
        private SR7IF_AsyncCALLBACK SyncDataCallBack;
        private SR7IF_AsyncErrCallBack AsyncErrCallBack;

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
            ConnectErrCallBack = pfErrCallBack;

            AsyncErrCallBack = new SR7IF_AsyncErrCallBack(SR7IAsyncErrCallBack);
            int openRet = SR7LinkFunc.SR7IF_AsyncEthernetOpen(ControllerID, ref ethernetConfig, 2000, AsyncErrCallBack);
            if (openRet != 0)
                return openRet;
            int nCameraB = SR7LinkFunc.SR7IF_GetOnlineCameraB(ControllerID);
            CameraAOnline = true;
            CameraBOnline = (nCameraB == 0);
            return openRet;
        }

        public void SR7IAsyncErrCallBack(int DeviciId, int ErrCode)
        {
            ConnectErrCallBack(DeviciId, ErrCode);
        }

        public override int Init(int dwProfileCnt, uint ProfileBits, int nTimeout, SR7IFGetDataCallBack pfGetDataCallBack)
        {
            GetDataCallBack = pfGetDataCallBack;

            if (SyncDataCallBack == null)
                SyncDataCallBack = new SR7IF_AsyncCALLBACK(SR7IFAsyncCallBack);
            int ret = SR7LinkFunc.SR7IF_AsyncCallBackInitalize(ControllerID, SyncDataCallBack, IntPtr.Zero, ProfileBits, (uint)dwProfileCnt);
            return ret;
        }
        public void SR7IFAsyncCallBack(IntPtr SR7IF_Data, IntPtr pUserData, uint state, int dwDeviceId)
        {
            AsyncCallBackInfo coninfo = new AsyncCallBackInfo();
            coninfo = (AsyncCallBackInfo)Marshal.PtrToStructure(SR7IF_Data, typeof(AsyncCallBackInfo));

             int camIndex = (coninfo.bCamB == 1  ? 2 : 1);
             for (int i = 0; i < camIndex; i++)
             {
                 //Get height data
                 if (coninfo.ProfileBits == 0)//32bit
                     ImgBuff32[i] = SR7LinkFunc.SR7IF_GetAsyncProfilePointData(SR7IF_Data, i);
                 else if (coninfo.ProfileBits == 1)//16bit
                     ImgBuff16[i] = SR7LinkFunc.SR7IF_GetAsyncProfilePointData16Bit(SR7IF_Data, i);
                 //Get grayscale data
                 if (coninfo.bGray==1)
                     GrayBuff[i] = SR7LinkFunc.SR7IF_GetAsyncIntensityContiuneData(SR7IF_Data, i);
                 //Get encoder data
                 Encoder[i] = SR7LinkFunc.SR7IF_GetAsyncEncoderContiune(SR7IF_Data, i);
             }

             GetDataCallBack(coninfo.profileWidth, coninfo.profileNum, 1, (int)state, coninfo.ProfileBits);
        }

        public override int Start(bool bIOTrigger, int timeout)
        {
            int ret = 0;
            if (!bIOTrigger)
                ret = SR7LinkFunc.SR7IF_AsyncSoftStartBatch(ControllerID);
            return ret;
        }

        public override int Stop(int instantStop)
        {
            int ret = SR7LinkFunc.SR7IF_AsyncSoftStopBatch(ControllerID);
            return ret;
        }
    }
}
