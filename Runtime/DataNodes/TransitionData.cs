using System;

namespace GraphToolsFSM.Runtime.DataNodes {
    [Serializable]
    public class TransitionData {
        public string FromGuid;
        public string ToGuid;
        public TransitionType Type;
    }
}