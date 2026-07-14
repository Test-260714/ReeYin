using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nodify.FlowApp
{
    [BlackboardItem("Has Value")]
    public class HasValueCondition : IBlackboardCondition
    {
        [BlackboardProperty(BlackboardKeyType.Object)]
        public BlackboardKey Key { get; set; }

        public Task<bool> Evaluate(Blackboard blackboard)
            => Task.FromResult(blackboard.GetObject(Key) != null);
    }
}
