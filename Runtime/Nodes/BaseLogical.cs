using System.Collections.Generic;
using GraphToolsFSM.Runtime.Core;

namespace GraphToolsFSM.Runtime.Nodes {
    public abstract class BaseLogical : BaseNode {
        public bool LastSignal { get; private set; }

        public readonly List<bool> InputBuffer = new();

        public int ExpectedInputs { get; set; }

        protected BaseLogical(StateMachine stateMachine) : base(stateMachine) { }
        
        public bool Propagate() {
            LastSignal = false;

            if (InputBuffer.Count < ExpectedInputs)
                return false;

            bool result = Evaluate(InputBuffer);
            InputBuffer.Clear();
            LastSignal = result;
            return LastSignal;
        }

        protected abstract bool Evaluate(List<bool> inputs);
    }
}