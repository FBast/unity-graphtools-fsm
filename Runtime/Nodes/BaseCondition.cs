using GraphToolsFSM.Runtime.Core;

namespace GraphToolsFSM.Runtime.Nodes {
    public abstract class BaseCondition : BaseNode {
        public bool LastSignal { get; private set; }

        protected BaseCondition(StateMachine stateMachine) : base(stateMachine) { }

        public bool Propagate() {
            LastSignal = Evaluate();
            
            return LastSignal;
        }
        
        protected abstract bool Evaluate();
    }
}