using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nodify.FlowApp
{
    [BlackboardItem("Set Value")]
    public class SetKeyValueAction : IBlackboardAction
    {
        [BlackboardProperty(BlackboardKeyType.Object)]
        public BlackboardProperty Key { get; set; }

        [BlackboardProperty(BlackboardKeyType.Object, CanChangeType = true)]
        public BlackboardProperty Value { get; set; }

        public Task Execute(Blackboard blackboard)
        {
            var value = blackboard.GetValue<int>(Value);
            blackboard[Key] = value;

            return Task.CompletedTask;
        }
    }
}
