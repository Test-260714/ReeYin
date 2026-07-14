using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.Hyperson.API
{
    public class ShareObjects
    {

        public int controlerHandle = 0;
        public int HDR_index = 0;

        public LCF_DeviceControlInfo_t[] deviceList;
        public UInt32 deviceNumber;

        public static int width;/*横坐标像素点个数*/
        public static int height;/*纵坐标像素点个数*/
        public static int FRAMESIZE = width * height;/*像素点总数*/
        public static int LOW_AMPLITUDE = 65300; /*振幅太低*/
        public static int SATURATION = 65400;    /*饱和位饱和*/
        public static int ADC_OVERFLOW = 65500;  /*ADC溢出*/
        public static int INVALID_DATA = 65530;   /*无效数据*/
        public static int ColorNumber = 65535;
        public static int Abscissa;//等待ui图像横坐标
        public static int Ordinate;//等待ui图像纵坐标
        static ShareObjects instance = null;

        static ShareObjects()
        {
            instance = new ShareObjects();
        }

        ShareObjects()
        {


        }

        public static ShareObjects getInstance()
        {
            return instance;
        }


    }
}
