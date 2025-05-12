using System.Collections.Generic;
using System.Linq;
using GraphToolsFSM.Runtime.Attributes;
using GraphToolsFSM.Runtime.Core;

namespace GraphToolsFSM.Runtime.Nodes {
    [FSMTarget]
    public class OrLogical : BaseLogical {
        public OrLogical(StateMachine stateMachine) : base(stateMachine) { }

        protected override bool Evaluate(List<bool> inputs)
        {
            return inputs.Any(value => value);
        }
    }
}