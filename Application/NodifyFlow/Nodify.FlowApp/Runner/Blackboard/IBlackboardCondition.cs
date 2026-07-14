using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nodify.FlowApp
{
    public interface IBlackboardCondition
    {
        Task<bool> Evaluate(Blackboard blackboard);
    }
}
