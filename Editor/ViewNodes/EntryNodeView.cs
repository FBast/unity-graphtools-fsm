using GraphToolsFSM.Editor.Core;
using UnityEditor.Experimental.GraphView;

namespace GraphToolsFSM.Editor.ViewNodes {
    public class EntryNodeView : BaseNodeView {
        public EntryNodeView(string nodeType) : base(nodeType) {
            title = "Entry";
            GUID = "ENTRY";

            capabilities &= ~Capabilities.Deletable;
            capabilities &= ~Capabilities.Copiable;

            AddToClassList("entry-node");
            
            var output = CategorizedPort.Create(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Single,
                typeof(bool),
                PortCategory.State
            );
            output.portName = "Out";
            outputContainer.Add(output);
            
            RefreshExpandedState();
            RefreshPorts();
        }
    }
}