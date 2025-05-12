using System;
using System.Collections.Generic;
using GraphToolsFSM.Editor.Edges;
using GraphToolsFSM.Editor.Helpers;
using GraphToolsFSM.Editor.ViewNodes;
using GraphToolsFSM.Runtime.DataNodes;
using GraphToolsFSM.Runtime.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphToolsFSM.Editor.Core {
    public class FSMGraphView : GraphView {
        public readonly Vector2 DefaultNodeSize = new(150, 200);

        private EntryNodeView _entryNode;
        private BaseNodeView _pendingTransitionSource;
        private TransitionEdge _ghostEdge;
        private Vector2 _lastMousePosition;

        public bool IsTransitionPending => _pendingTransitionSource != null;
        public FSMGraphData CurrentGraph { get; set; }

        public FSMGraphView() {
            style.flexGrow = 1;
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));

            Insert(0, new GridBackground { name = "grid" });

            var graphViewStyle = AssetLoader.LoadStyleSheetByName("FSMGraphView");
            if (graphViewStyle != null) styleSheets.Add(graphViewStyle);
            else
                Debug.LogWarning("FSMGraphView: Unable to find or load FSMGraphView.uss");

            var nodesStyle = AssetLoader.LoadStyleSheetByName("FSMNodes");
            if (nodesStyle != null)
                styleSheets.Add(nodesStyle);
            else
                Debug.LogWarning("FSMGraphView: Unable to find or load FSMNodes.uss");

            
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);

            graphViewChanged += OnGraphViewChanged;
        }

        private void OnMouseUp(MouseUpEvent evt) {
            if (evt.button != 1) return;

            _lastMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);

            if (IsTransitionPending) {
                CancelPendingTransition();
                evt.StopPropagation();
            }
        }

        private void OnMouseMove(MouseMoveEvent evt) {
            if (_ghostEdge == null) return;
        
            var mousePos = contentViewContainer.WorldToLocal(evt.mousePosition);
            _ghostEdge.edgeControl.to = mousePos;
            _ghostEdge.edgeControl.UpdateLayout();
            _ghostEdge.MarkDirtyRepaint();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change) {
            if (change.edgesToCreate?.Count > 0 || change.elementsToRemove != null) {
                MarkGraphAsDirty();
            }
            return change;
        }

        public void StartTransitionFrom(BaseNodeView fromNode, TransitionType transitionType) {
            _pendingTransitionSource = fromNode;

            if (fromNode.outputContainer[0] is not Port output) return;

            _ghostEdge = new TransitionEdge(transitionType) {
                output = output,
                isGhostEdge = true
            };

            AddElement(_ghostEdge);
        }

        public void CancelPendingTransition() {
            _pendingTransitionSource = null;

            if (_ghostEdge != null) {
                RemoveElement(_ghostEdge);
                _ghostEdge = null;
            }
        }

        public void CompleteTransitionTo(BaseNodeView toNode) {
            if (_pendingTransitionSource == null || toNode == null) {
                _pendingTransitionSource = null;
                return;
            }

            if (_pendingTransitionSource == toNode) {
                Debug.LogWarning("[FSM] Impossible de créer une transition vers soi-même.");
                _pendingTransitionSource = null;
                return;
            }

            if (_pendingTransitionSource.outputContainer[0] is not Port output) return;
            if (toNode.inputContainer[0] is not Port input) return;

            if (output is not CategorizedPort outCatPort || input is not CategorizedPort inCatPort)
                return;

            bool isValidConnection = (outCatPort.Category & inCatPort.Category) != 0;

            if (!isValidConnection) {
                Debug.LogWarning($"[FSM] Connexion invalide : {outCatPort.Category} -> {inCatPort.Category}");
                _pendingTransitionSource = null;
                return;
            }

            var edge = new TransitionEdge(_ghostEdge.TransitionType) {
                output = output,
                input = input
            };

            edge.output.Connect(edge);
            edge.input.Connect(edge);
            AddElement(edge);

            MarkGraphAsDirty();
            _pendingTransitionSource = null;
            
            if (_ghostEdge != null) {
                
                RemoveElement(_ghostEdge);
                _ghostEdge = null;
            }
        }
        
        private void BuildContextMenu(ContextualMenuPopulateEvent evt) {
            var fsmName = CurrentGraph?.GetType().Name.Replace("GraphData", "");
            if (fsmName == null) return;
            
            foreach (var type in FSMNodeRegistry.GetStateNodeTypesFor(fsmName)) {
                evt.menu.AppendAction($"Add State/{type.Name}", _ => CreateNode(type, _lastMousePosition));
            }

            foreach (var type in FSMNodeRegistry.GetConditionNodeTypesFor(fsmName)) {
                evt.menu.AppendAction($"Add Condition/{type.Name}", _ => CreateNode(type, _lastMousePosition));
            }
            
            foreach (var type in FSMNodeRegistry.GetLogicalNodeTypesFor(fsmName)) {
                evt.menu.AppendAction($"Add Logical/{type.Name}", _ => CreateNode(type, _lastMousePosition));
            }
        }

        private void CreateNode(Type type, Vector2 position) {
            BaseNodeView node = type switch {
                _ when typeof(BaseState).IsAssignableFrom(type) => new StateNodeView(type.AssemblyQualifiedName),
                _ when typeof(BaseCondition).IsAssignableFrom(type) => new ConditionNodeView(type.AssemblyQualifiedName),
                _ when type == typeof(AndLogical) => new LogicalNodeView(type.AssemblyQualifiedName, Port.Capacity.Multi, Port.Capacity.Single),
                _ when type == typeof(OrLogical) => new LogicalNodeView(type.AssemblyQualifiedName, Port.Capacity.Multi, Port.Capacity.Single),
                _ when type == typeof(NotLogical) => new LogicalNodeView(type.AssemblyQualifiedName, Port.Capacity.Single, Port.Capacity.Single),
                _ => null
            };

            if (node == null) {
                Debug.LogError($"[FSM] Type non supporté pour la création de node : {type.FullName}");
                return;
            }

            node.GUID = Guid.NewGuid().ToString();
            node.SetPosition(new Rect(position, DefaultNodeSize));
            node.OnModified += MarkGraphAsDirty;
            AddElement(node);
            MarkGraphAsDirty();
        }

        private void MarkGraphAsDirty() {
            if (CurrentGraph != null) EditorUtility.SetDirty(CurrentGraph);
        }
        
        public void SaveGraph(FSMGraphData asset) {
            asset.Nodes.Clear();
            asset.Transitions.Clear();

            foreach (var element in graphElements) {
                if (element is TransitionEdge edge &&
                    edge.output.node is BaseNodeView fromNode &&
                    edge.input.node is BaseNodeView toNode) {

                    asset.Transitions.Add(new TransitionData {
                        FromGuid = fromNode.GUID,
                        ToGuid = toNode.GUID,
                        Type = edge.TransitionType
                    });
                }
                else if (element is BaseNodeView node) {
                    asset.Nodes.Add(node.ToData());
                }
            }
            
            asset.ViewPosition = contentViewContainer.transform.position;
            asset.ViewScale = contentViewContainer.transform.scale.x;
        }

        public void LoadGraph(FSMGraphData data) {
            DeleteElements(graphElements);

            var nodeLookup = new Dictionary<string, BaseNodeView>();
            
            foreach (var nodeData in data.Nodes) {
                var view = NodeViewFactory.CreateFromData(nodeData, DefaultNodeSize);
                if (view == null) {
                    Debug.LogWarning($"[FSM] Type non supporté ou inconnu : {nodeData.NodeType}");
                    continue;
                }

                AddElement(view);
                nodeLookup[view.GUID] = view;
            }

            foreach (var transition in data.Transitions) {
                if (!nodeLookup.TryGetValue(transition.FromGuid, out var from) ||
                    !nodeLookup.TryGetValue(transition.ToGuid, out var to)) continue;

                var output = from.outputContainer[0] as Port;
                var input = to.inputContainer[0] as Port;

                if (output != null && input != null) {
                    var edge = new TransitionEdge(transition.Type) {
                        output = output,
                        input = input
                    };
                    edge.output.Connect(edge);
                    edge.input.Connect(edge);
                    AddElement(edge);
                }
            }
            
            UpdateViewTransform(data.ViewPosition, Vector3.one * data.ViewScale);
        }
    }
}
