using System;
using System.Collections.Generic;
using System.Linq;
using GraphToolsFSM.Editor.Edges;
using GraphToolsFSM.Editor.Helpers;
using GraphToolsFSM.Editor.ViewNodes;
using GraphToolsFSM.Runtime.DataNodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphToolsFSM.Editor.Core {
    public class FSMGraphView : GraphView {
        private EntryNodeView _entryNode;
        private BaseNodeView _pendingTransitionSource;
        private TransitionEdge _ghostEdge;
        private Vector2 _lastMousePosition;
        private readonly List<(BaseNodeData data, Vector2 offset)> _clipboard = new();
        private Vector2 _clipboardOffset;

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
            RegisterCallback<KeyDownEvent>(OnKeyDown);

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

        private void OnKeyDown(KeyDownEvent evt)
        {
            // Ctrl + D = Duplicate
            if (evt.ctrlKey && evt.keyCode == KeyCode.D) {
                foreach (var node in selection.OfType<BaseNodeView>().ToList())
                {
                    node.Duplicate(new Vector2(30, 30));
                }

                MarkGraphAsDirty();
                evt.StopPropagation();
                return;
            }

            // Ctrl + C = Copy
            if (evt.ctrlKey && evt.keyCode == KeyCode.C)
            {
                _clipboard.Clear();

                var selected = selection.OfType<BaseNodeView>().ToList();
                if (selected.Count == 0) return;

                _clipboardOffset = this.ChangeCoordinatesTo(contentViewContainer, Event.current.mousePosition);

                foreach (var view in selected)
                {
                    var data = view.ToData();
                    var offset = data.Position - _clipboardOffset;

                    data.Guid = Guid.NewGuid().ToString();
                    _clipboard.Add((data, offset));
                }

                evt.StopPropagation();

            }

            // Ctrl + V = Paste
            if (evt.ctrlKey && evt.keyCode == KeyCode.V && _clipboard.Count > 0)
            {
                Vector2 pasteMousePos = this.ChangeCoordinatesTo(contentViewContainer, Event.current.mousePosition);

                foreach (var (data, offset) in _clipboard)
                {
                    data.Position = pasteMousePos + offset;

                    var view = NodeViewFactory.CreateFromData(data);
                    if (view == null) continue;

                    AddElement(view);
                    AddToSelection(view);
                }

            }
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

        public void CreateNode(Type type, Vector2 position)
        {
            var node = NodeViewFactory.CreateFromType(type, position);
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
                var view = NodeViewFactory.CreateFromData(nodeData);
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
