using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nodify.FlowApp
{
    public class State
    {
        public Guid Id { get; }
        public IBlackboardAction? Action { get; }

        public State(Guid id, IEnumerable<Transition> transitions, IBlackboardAction? action = default)
        {
            Id = id;
            Action = action;
            Transitions = new List<Transition>(transitions);
        }

        public IReadOnlyList<Transition> Transitions { get; }

        public virtual Task Activate(Blackboard blackboard)
            => Action?.Execute(blackboard) ?? Task.CompletedTask;
    }
}
