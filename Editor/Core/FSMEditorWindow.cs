using GraphToolsFSM.Editor.Edges;
using GraphToolsFSM.Editor.ViewNodes;
using GraphToolsFSM.Runtime.DataNodes;
using GraphToolsFSM.Runtime.Nodes;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using StateMachine = GraphToolsFSM.Runtime.Core.StateMachine;

namespace GraphToolsFSM.Editor.Core {
    public class FSMEditorWindow : EditorWindow {
        [SerializeField] private FSMGraphData _currentGraph;
        private FSMGraphView _graphView;
        private Button _saveBtn, _saveAsBtn;

        [OnOpenAsset(1)]
        public static bool OpenWithAsset(int instanceID, int line) {
            if (EditorUtility.InstanceIDToObject(instanceID) is FSMGraphData graphData) {
                var window = GetWindow<FSMEditorWindow>(graphData.name);
                window.LoadGraph(graphData);
                return true;
            }
            return false;
        }

        private void CreateGUI() {
            rootVisualElement.Clear();

            rootVisualElement.Add(GenerateToolbar());

            _graphView = new FSMGraphView {
                name = "FSM Graph",
                CurrentGraph = _currentGraph,
                style = { flexGrow = 1 }
            };
            rootVisualElement.Add(_graphView);

            if (_currentGraph != null)
                _graphView.LoadGraph(_currentGraph);
        }

        private Toolbar GenerateToolbar() {
            var toolbar = new Toolbar();

            _saveBtn = new Button(SaveCurrentGraph) { text = "Save" };
            _saveAsBtn = new Button(SaveGraphAs) { text = "Save As" };

            toolbar.Add(_saveBtn);
            toolbar.Add(_saveAsBtn);

            return toolbar;
        }
        
        private void Update() {
            if (_graphView == null || _currentGraph == null) return;
            
            var isDirty = EditorUtility.IsDirty(_currentGraph);
            _saveBtn?.SetEnabled(isDirty);
            _saveAsBtn?.SetEnabled(isDirty);
            
            UpdateWindowTitle(isDirty);
            UpdateNodesVisual();
            UpdateTransitionsVisual();
        }

        private void UpdateWindowTitle(bool isDirty = false)
        {
            var baseName = _currentGraph != null ? _currentGraph.name : "FSM Editor";
            titleContent.text = isDirty ? $"{baseName}*" : baseName;

            if (titleContent.image != null) return;
            var icon = EditorGUIUtility.Load("Icons/d_UnityEditor.Graphs.AnimatorControllerTool.png") as Texture2D;
            if (icon != null)
            {
                titleContent.image = icon;
            }
            else
            {
                Debug.LogWarning("[FSM] Impossible de charger une icône pour la fenêtre.");
            }
        }
        
        private void UpdateNodesVisual()
        {
            if (!Application.isPlaying || Selection.activeGameObject == null)
            {
                ResetAllNodesVisual();
                return;
            }

            var fsm = Selection.activeGameObject.GetComponent<StateMachine>();
            if (fsm == null)
            {
                ResetAllNodesVisual();
                return;
            }

            var currentState = fsm.GetCurrentState();

            foreach (var node in _graphView.nodes)
            {
                switch (node)
                {
                    case StateNodeView stateNode:
                        var state = fsm.GetNodeByGuid<BaseState>(stateNode.GUID);
                        if (state == null) continue;

                        bool isActive = currentState == state;
                        float progress = isActive ? currentState.Progress : 1f;

                        stateNode.UpdateStateVisual(isActive, progress);
                        break;

                    case ConditionNodeView conditionNode:
                        var condition = fsm.GetNodeByGuid<BaseCondition>(conditionNode.GUID);
                        if (condition == null) continue;

                        conditionNode.UpdateSignalVisual(condition);
                        break;
                    
                    case LogicalNodeView logicalNode:
                        var logical = fsm.GetNodeByGuid<BaseLogical>(logicalNode.GUID);
                        if (logical == null) continue;

                        logicalNode.UpdateSignalVisual(logical);
                        break;
                }
            }
        }
        
        private void ResetAllNodesVisual()
        {
            foreach (var node in _graphView.nodes)
            {
                switch (node)
                {
                    case StateNodeView stateNode:
                        stateNode.ResetVisual();
                        break;
                    case ConditionNodeView conditionNode:
                        conditionNode.ClearSignalVisual();
                        break;
                    case LogicalNodeView logicalNode:
                        logicalNode.ClearSignalVisual();
                        break;
                }
            }
        }
        
        private void UpdateTransitionsVisual()
        {
            if (!Application.isPlaying || Selection.activeGameObject == null)
            {
                ResetAllTransitionsVisual();
                return;
            }

            var fsm = Selection.activeGameObject.GetComponent<StateMachine>();
            if (fsm == null)
            {
                ResetAllTransitionsVisual();
                return;
            }

            var currentState = fsm.GetCurrentState();
            if (currentState == null)
            {
                ResetAllTransitionsVisual();
                return;
            }

            foreach (var edge in _graphView.edges)
            {
                if (edge is not TransitionEdge transitionEdge)
                    continue;

                if (transitionEdge.output.node is not StateNodeView outputStateNode)
                    continue;

                var outputState = fsm.GetNodeByGuid<BaseState>(outputStateNode.GUID);
                transitionEdge.Active = outputState == currentState;
                transitionEdge.UpdateEdgeColor();
            }
        }
        
        private void ResetAllTransitionsVisual()
        {
            foreach (var edge in _graphView.edges)
            {
                if (edge is TransitionEdge transitionEdge)
                {
                    transitionEdge.Active = false;
                    transitionEdge.UpdateEdgeColor();
                }
            }
        }


        private void SaveCurrentGraph() {
            if (_currentGraph == null) {
                SaveGraphAs();
                return;
            }

            _graphView.SaveGraph(_currentGraph);
            EditorUtility.SetDirty(_currentGraph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void SaveGraphAs() {
            var path = EditorUtility.SaveFilePanelInProject("Save FSM As", "NewFSM", "asset", "Choose new save location");
            if (string.IsNullOrEmpty(path)) return;

            var newGraph = CreateInstance<FSMGraphData>();
            _graphView.SaveGraph(newGraph);
            AssetDatabase.CreateAsset(newGraph, path);
            AssetDatabase.SaveAssets();

            LoadGraph(newGraph);
        }

        private void LoadGraph(FSMGraphData graphData) {
            _currentGraph = graphData;
            UpdateWindowTitle();
            
            if (_graphView != null) {
                _graphView.CurrentGraph = graphData;
                _graphView.LoadGraph(graphData);
            }
        }
    }
}