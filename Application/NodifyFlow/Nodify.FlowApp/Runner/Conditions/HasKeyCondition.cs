using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nodify.FlowApp
{
    [BlackboardItem("Has Key")]
    public class HasKeyCondition : IBlackboardCondition
    {
        [BlackboardProperty("Key Name", BlackboardKeyType.String)]
        public BlackboardProperty Key { get; set; }

        public Task<bool> Evaluate(Blackboard blackboard)
        {
            var keyName = blackboard.GetObject<string>(Key);

            if (keyName != null)
            {
                return Task.FromResult(blackboard.HasKey(keyName));
            }

            return Task.FromResult(false);
        }
    }
}
