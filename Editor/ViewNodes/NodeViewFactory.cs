using System;
using GraphToolsFSM.Runtime.DataNodes;
using GraphToolsFSM.Runtime.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace GraphToolsFSM.Editor.ViewNodes {
    public static class NodeViewFactory {
        public static BaseNodeView CreateFromData(BaseNodeData data, Vector2 defaultSize) {
            var type = Type.GetType(data.NodeType);
            if (type == null) return null;

            BaseNodeView view = type switch {
                _ when type == typeof(EntryNode) => new EntryNodeView(data.NodeType),
                _ when typeof(BaseState).IsAssignableFrom(type) => new StateNodeView(data.NodeType),
                _ when typeof(BaseCondition).IsAssignableFrom(type) => new ConditionNodeView(data.NodeType),
                _ when type == typeof(AndLogical) => new LogicalNodeView(data.NodeType, Port.Capacity.Multi, Port.Capacity.Single),
                _ when type == typeof(OrLogical) => new LogicalNodeView(data.NodeType, Port.Capacity.Multi, Port.Capacity.Single),
                _ when type == typeof(NotLogical) => new LogicalNodeView(data.NodeType, Port.Capacity.Single, Port.Capacity.Single),
                _ => null
            };

            if (view == null) return null;
            view.GUID = data.Guid;
            view.SetPosition(new Rect(data.Position, defaultSize));
            view.ApplyCustomFields(data.CustomFields);
            return view;
        }
    }
}