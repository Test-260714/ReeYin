using Prism.Events;

namespace ReeYin_V.Core.Events
{
    /// <summary>
    /// 解决方案操作事件
    /// 用于新建、打开、保存、释放解决方案的事件通知
    /// 传递操作类型字符串："新建"、"打开"、"文件打开"、"保存"、"释放"
    /// </summary>
    public class SolutionOperationEvent : PubSubEvent<string>
    {
    }
}
