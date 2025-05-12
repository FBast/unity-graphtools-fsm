using System;

namespace GraphToolsFSM.Runtime.Attributes {
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class FSMTargetAttribute : Attribute {
        public string TargetFSM { get; }

        public FSMTargetAttribute(string fsmName = null) {
            TargetFSM = fsmName;
        }
    }
    

    namespace FiniteStateMachine.Runtime.Core { }

}