using GraphToolsFSM.Runtime.Core;

namespace GraphToolsFSM.Runtime.Nodes {
    public abstract class BaseNode {
        protected readonly StateMachine StateMachine;

        protected BaseNode(StateMachine stateMachine) {
            StateMachine = stateMachine;
        }

        public string Guid { get; set; }
    }
}