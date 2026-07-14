using GTN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogolMotion
{
    /// <summary>
    /// 输出一些信息
    /// </summary>
    public partial class GoogolGTMotion
    {
        // 定义错误码与描述的映射关系（错误码 → 错误描述模板）
        private static readonly Dictionary<short, string> _errorMessages = new Dictionary<short, string>
        {
            {1, "{0}:指令执行错误\r\n1. 检查当前指令的执行条件是否满足"},
            {2, "{0}:license 不支持\r\n1. 如果需要此功能，请与生产厂商联系"},
            {7, "{0}:指令参数错误\r\n1. 检查当前指令输入参数的取值"},
            {-1, "{0}:主机和运动控制器通讯失败\r\n1. 是否正确安装运动控制器驱动程序\r\n2. 检查运动控制器是否接插牢靠\r\n3. 更换主机\r\n4. 更换控制器\r\n5. 运动控制器的金手指是否干净"},
            {-6, "{0}:打开控制器失败\r\n1. 是否正确安装运动控制器驱动程序\r\n2. 是否调用了 2 次 GTN_Open 指令\r\n3. 其他程序是否已经打开运动控制器，或进程中是否还驻留着打开控制器的程序"},
            {-7, "{0}:运动控制器没有响应\r\n1. 更换运动控制器"},
            {-10, "{0}:主机和运动控制器通讯失败\r\n1.PCI-E 松动，需要重新加载驱动\r\n2.或者重启电脑、插紧主卡并拧上固定螺丝"},
            {-13, "{0}:编码器初始化失败\r\n1.需要检测 core1 所连模块是否工作正常\r\n2.对于拿云系列产品需要检测驱动专用处理器是否工作正常"},
            {-14, "{0}:编码器初始化失败\r\n1.需要检测 core2 所连模块是否工作正常\r\n2.对于拿云系列产品需要检测驱动专用处理器是否工作正常"},
            {-15, "{0}:动态库版本不匹配\r\n1、无法正常工作，需要更新 gt_rn.dll"},
            {15, "{0}:动态库版本不匹配\r\n1、某些功能不能正常使用，需要更新 gt_rn.dll"},
            {16, "{0}:不具备版本匹配功能\r\n1、固件版本比较老，建议更新专用处理器固件"}
        };

        // 简化后的错误信息处理方法
        public string ErrMessage(short sRtn)
        {
            // 从字典获取对应模板，若不存在则使用默认信息
            string messageTemplate = _errorMessages.TryGetValue(sRtn, out var template)
                ? template
                : "{0}:未知返回值,请联系固高官方!";

            // 格式化消息（{0} 会替换为错误码 sRtn）
            string msg = string.Format(messageTemplate, sRtn);

            Console.WriteLine(msg);
            return msg;
        }

        public void ErrMessage(string message)
        {
            Console.WriteLine(message);
        }

        public void OutputMessage(string message)
        {
            Console.WriteLine(message);
        }



        /// <summary>
        /// 清除所有轴报警
        /// </summary>
        /// <returns></returns>
        public bool CleanAlarm(short core = 2)
        {
            short sRtn;
            sRtn = mc.GTN_ClrSts(core, 1, _axisCount);
            if (sRtn != 0)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 清除单轴报警
        /// </summary>
        /// <param name="axisId"></param>
        /// <returns></returns>
        public bool CleanAlarm(short axisId, short core = 2)
        {
            short sRtn;
            sRtn = mc.GTN_ClrSts(core, axisId, 1);
            if (sRtn != 0)
            {
                Console.WriteLine(ErrMessage(sRtn));
                return false;
            }
            return true;
        }
    }
}
