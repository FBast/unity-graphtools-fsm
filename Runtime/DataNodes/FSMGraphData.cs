using System.Collections.Generic;
using GraphToolsFSM.Runtime.Nodes;
using UnityEngine;

namespace GraphToolsFSM.Runtime.DataNodes {
    public abstract class FSMGraphData : ScriptableObject {
        [SerializeReference] public List<BaseNodeData> Nodes = new();
        public List<TransitionData> Transitions = new();
        public Vector3 ViewPosition;
        public float ViewScale = 1f;
        
        protected virtual void OnEnable()
        {
            if (Nodes == null) Nodes = new List<BaseNodeData>();

            bool hasEntry = Nodes.Exists(node => node.NodeType == typeof(EntryNode).AssemblyQualifiedName);

            if (!hasEntry)
            {
                Nodes.Add(new BaseNodeData
                {
                    Guid = "ENTRY",
                    Position = new Vector2(-300, 0),
                    NodeType = typeof(EntryNode).AssemblyQualifiedName,
                    CustomFields = new List<FieldPair>()
                });
            }
        }
    }
}