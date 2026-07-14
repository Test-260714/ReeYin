using System;
using System.Runtime.InteropServices;
using SR7Link;

namespace SRAPI
{
    class SR7ApiInfiniteLoopCallbackImpl : SR7ApiBase
    {
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
            CameraBOnline = (nCameraB == 0);
            return openRet;
        }

        public override int Init(int dwProfileCnt, uint ProfileBits, int nTimeout, SR7IFGetDataCallBack pfGetDataCallBack)
        {
            GetDataCallBack = pfGetDataCallBack;
            return 0;
        }

        public override int GetData(int[] pHighData, byte[] pGrayData, int[] pEncoderData, int nProfileWidth, int nHighlen, int ABCam)
        {
            return -1;
        }

        public override int Start(bool bIOTrigger, int timeout)
        {
            uint setProfilePoint = 0;
            if (BatchPoints >= 15000 && BatchPoints <= 65535)
            {
                setProfilePoint = (uint)BatchPoints;
            }

            int callbackPointRet = SR7LinkFunc.SR7IF_SetBatchRollProfilePoint(ControllerID, IntPtr.Zero, setProfilePoint);
            if (callbackPointRet != 0)
            {
                return callbackPointRet;
            }
            int ret = 0;
            if (bIOTrigger)//hard trigger
                ret = SR7LinkFunc.SR7IF_StartIOTriggerMeasure(ControllerID, timeout, 0);
            else//Soft trigger
                ret = SR7LinkFunc.SR7IF_StartMeasure(ControllerID, timeout);
            return ret;
        }

        public override int Stop(int instantStop)
        {
            int ret = SR7LinkFunc.SR7IF_StopMeasure(ControllerID);
            return ret;
        }

        public int GetBatchRollData(int[] pHighData, byte[] pGrayData, uint[] pEncoderData, long[] pFrameId, uint[] pFrameLoss, int getCnt)
        {
            using (PinnedObject pinHeight = new PinnedObject(pHighData))
            using (PinnedObject pinGray = new PinnedObject(pGrayData))
            using (PinnedObject pinEncoder = new PinnedObject(pEncoderData))
            using (PinnedObject pinFrameId = new PinnedObject(pFrameId))
            using (PinnedObject pinFrameLoss = new PinnedObject(pFrameLoss))
            {
                return SR7LinkFunc.SR7IF_GetBatchRollData(
                    ControllerID,
                    IntPtr.Zero,
                    pinHeight.Pointer,
                    pinGray.Pointer,
                    pinEncoder.Pointer,
                    pinFrameId.Pointer,
                    pinFrameLoss.Pointer,
                    getCnt);
            }
        }

        public int GetBatchRollError(out int ethErrCnt, out int userErrCnt)
        {
            ethErrCnt = 0;
            userErrCnt = 0;
            int[] ethErrors = new int[1];
            int[] userErrors = new int[1];
            using (PinnedObject pinEth = new PinnedObject(ethErrors))
            using (PinnedObject pinUser = new PinnedObject(userErrors))
            {
                int ret = SR7LinkFunc.SR7IF_GetBatchRollError(ControllerID, pinEth.Pointer, pinUser.Pointer);
                if (ret != 0)
                {
                    return ret;
                }
            }

            ethErrCnt = ethErrors[0];
            userErrCnt = userErrors[0];
            return 0;
        }
    }
}
