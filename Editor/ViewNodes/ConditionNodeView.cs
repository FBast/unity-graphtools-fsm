using System;
using System.Collections.Generic;
using GraphToolsFSM.Editor.Core;
using GraphToolsFSM.Runtime.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace GraphToolsFSM.Editor.ViewNodes {
    public class ConditionNodeView : BaseNodeView {
        public ConditionNodeView(string nodeType) : base(nodeType) {
            GUID = Guid.NewGuid().ToString();

            AddToClassList("condition-node");
            
            var input = CategorizedPort.Create(
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Single,
                typeof(bool),
                PortCategory.Condition
            );
            input.portName = "In";
            inputContainer.Add(input);

            var output = CategorizedPort.Create(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Single,
                typeof(bool),
                PortCategory.State | PortCategory.Logical
            );
            output.portName = "Out";
            outputContainer.Add(output);

            CreateIcons(new Dictionary<string, Color> {
                { "check", Color.green },
                { "cross", Color.red },
                { "question", Color.gray }
            });
            
            RefreshExpandedState();
            RefreshPorts();
        }
        
        public void UpdateSignalVisual(BaseCondition condition) {
            ShowOnlyIcon(condition.LastSignal ? "check" : "cross");
        }

        public void ClearSignalVisual() {
            ShowOnlyIcon("question");
        }
    }
}