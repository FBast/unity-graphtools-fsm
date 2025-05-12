using System;
using System.Collections.Generic;
using GraphToolsFSM.Editor.Core;
using GraphToolsFSM.Runtime.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace GraphToolsFSM.Editor.ViewNodes {
    public class LogicalNodeView : BaseNodeView {
        public LogicalNodeView(string nodeType, Port.Capacity inputCapacity, Port.Capacity outputCapacity) : base(nodeType) {
            GUID = Guid.NewGuid().ToString();

            AddToClassList("logical-node");
            
            var input = CategorizedPort.Create(
                Orientation.Horizontal,
                Direction.Input,
                inputCapacity,
                typeof(bool),
                PortCategory.Logical
            );
            input.portName = "In";
            inputContainer.Add(input);

            var output = CategorizedPort.Create(
                Orientation.Horizontal,
                Direction.Output,
                outputCapacity,
                typeof(bool),
                PortCategory.Logical | PortCategory.State
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
        
        public void UpdateSignalVisual(BaseLogical logical) {
            ShowOnlyIcon(logical.LastSignal ? "check" : "cross");
        }

        public void ClearSignalVisual() {
            ShowOnlyIcon("question");
        }
    }
}