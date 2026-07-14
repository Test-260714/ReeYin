using SR7Link;
using System;
using System.Runtime.InteropServices;

namespace SRAPI
{
    /// <summary>
    /// SR7IFGetDataCallBack
    /// </summary>
    /// <param name="nProfileWidth">返回单条轮廓宽度数据 / Returns a single contour width data</param>
    /// <param name="nHighlen">返回的批处理行数 / The number of rows in the batch returned</param>
    /// <param name="nFlag">回调函数执行状态，根据不同模式自定义 / Callback function execution status, customized according to different modes</param>
    /// <param name="nStatusCode">错误码，0无错误 / Error code, 0 no error</param>
    /// <param name="ProfileBits">Sync callback 0:32bit data, 1:16bit data</param>
    public delegate void SR7IFGetDataCallBack(int nProfileWidth, int nHighlen, int nFlag, int nStatusCode, int ProfileBits);

    /// <summary>
    /// SR7 API 接口 / SR7 API
    /// </summary>
    public interface ISR7Api
    {
        /// <summary>
        /// Open
        /// </summary>
        /// <param name="lDeviceId">设备 ID 号，范围 0-63. / Device ID, range: 0-63.</param>
        /// <param name="strIP">相机IP / Camera IP</param>
        /// <param name="timeout">搜索时间 (ms)，最小值 100 / Search timeout (ms), minimum value: 100</param>
        /// <param name="pfErrCallBack">掉线回调函数/Disconnection callback function</param>
        /// <returns> <0:失败./Fail.  =0:成功./Success</returns>
        int Open(int lDeviceId, string strIP, int timeout, ErrConnectCallBack pfErrCallBack);


        /// <summary>
        /// Init
        /// </summary>
        /// <param name="dwProfileCnt">指定行数返回数据 / Return the specified number of rows</param>
        /// <param name="ProfileBits">0：返回32位高度数据。1：返回16位高度数据 / 0: Return 32-bit height data. 1: Return 16-bit height data.</param>
        /// <param name="nTimeout">获取当次回调行数超时时间，单位ms   <0:关闭超时，用于无限循环回调初始化
	    ///                       Get the timeout of the current callback row number, in ms < 0: turn off the timeout, used for infinite loop callback initialization</param>
        /// <param name="pfGetDataCallBack">相机数据回调函数 / Camera data callback function</param>
        /// <returns> <0:失败./Fail.  =0:成功./Success</returns>
        int Init(int dwProfileCnt, uint ProfileBits, int nTimeout, SR7IFGetDataCallBack pfGetDataCallBack);

        /// <summary>
        /// Start 开始批处理/ Start batch processing
        /// </summary>
        /// <param name="bIOTrigger">是否硬触发；0：软触发，1：硬触发 / Whether it is hard trigger; 0: soft trigger, 1: hard trigger</param>
        /// <param name="timeout">非循环获取时,超时时间(单位ms),-1为无限等待;循环模式该参数可设置为-1.
        ///                       Timeout period (in ms) for non-cyclic acquisition. -1 means infinite waiting. For cyclic mode, this parameter can be set to -1.</param>
        /// <returns> <0:失败./Fail.  =0:成功./Success</returns>
        int Start(bool bIOTrigger, int timeout);

        /// <summary>
        /// Stop 停止批处理 / Stop batch processing
        /// </summary>
        /// <param name="instantStop">0:等数据传输完成  1：立即停止，抛弃剩余未传完数据. 用于高速回调
        ///                           0: Wait for data transmission to complete 1: Stop immediately and discard the remaining untransmitted data. Used for high-speed callback</param>
        /// <returns> <0:失败./Fail.  =0:成功./Success</returns>
        int Stop(int instantStop);

        /// <summary>
        /// GetData 获取数据接口 / Get data interface
        /// </summary>
        /// <param name="pHighData">返回的高度数据 / height data returned</param>
        /// <param name="pGrayData">返回的灰度数据. / The returned grayscale data.</param>
        /// <param name="pEncoderData">返回的编码器数据 / Returned encoder data</param>
        /// <param name="nProfileWidth">单条轮廓宽度数据,用于申请内存大小 / Single contour width data, used to apply for memory size</param>
        /// <param name="nHighlen">批处理行数,用于申请内存大小 / The number of rows in a batch, used to apply for memory size</param>
        /// <param name="ABCam">AB相机; 0:A相机; 1:B相机 / AB camera; 0:A camera; 1:B camera</param>
        /// <returns> <0:失败./Fail.  =0:成功./Success</returns>
        int GetData(int[] pHighData, byte[] pGrayData, int[] pEncoderData, int nProfileWidth, int nHighlen, int ABCam);
        int GetData(short[] pHighData, byte[] pGrayData, int[] pEncoderData, int nProfileWidth, int nHighlen, int ABCam);
    }

    #region PinnedObject
    public sealed class PinnedObject : IDisposable
    {
        private GCHandle _handle;      // Garbage collector handle
        private bool _isDisposed;      // Track whether the object has been disposed
        
        /// <summary>
        /// Gets the address of the pinned object.
        /// </summary>
        public IntPtr Pointer
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(PinnedObject), "The object has been disposed.");

                return _handle.AddrOfPinnedObject();
            }
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="target">Target to protect from the garbage collector</param>
        public PinnedObject(object target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target), "Target object cannot be null.");

            _handle = GCHandle.Alloc(target, GCHandleType.Pinned);
        }
        
        /// <summary>
        /// Dispose method to free the resources held by GCHandle.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            _handle.Free();
            _isDisposed = true;
        }
    }
    #endregion

}
