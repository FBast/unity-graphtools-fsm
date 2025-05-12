using System.Collections.Generic;
using System.Linq;
using GraphToolsFSM.Runtime.Attributes;
using GraphToolsFSM.Runtime.Core;

namespace GraphToolsFSM.Runtime.Nodes {
    [FSMTarget]
    public class AndLogical : BaseLogical {
        public AndLogical(StateMachine stateMachine) : base(stateMachine) { }

        protected override bool Evaluate(List<bool> inputs)
        {
            return inputs.All(value => value);
        }
    }
}