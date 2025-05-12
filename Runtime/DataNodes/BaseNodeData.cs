using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraphToolsFSM.Runtime.DataNodes {
    [Serializable]
    public class FieldPair {
        public string Key;
        public string Value;
    }

    [Serializable]
    public class BaseNodeData {
        public string Guid;
        public string NodeType;
        public Vector2 Position;
        public List<FieldPair> CustomFields = new();
    }
}