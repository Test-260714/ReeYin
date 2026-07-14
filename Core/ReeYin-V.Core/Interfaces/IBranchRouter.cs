using System.Collections.Generic;

namespace ReeYin_V.Core.Interfaces
{
    /// <summary>
    /// 分支路由接口，流程引擎通过该接口只执行命中的后续节点。
    /// </summary>
    public interface IBranchRouter
    {
        IReadOnlyCollection<int> SelectedNextSerials { get; }
    }
}
