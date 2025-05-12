using System.Collections.Generic;
using GraphToolsFSM.Runtime.DataNodes;
using GraphToolsFSM.Runtime.Nodes;
using UnityEngine;

namespace GraphToolsFSM.Runtime.Core {
    public class StateMachine : MonoBehaviour {
        private BaseState _currentState;

        private readonly Dictionary<BaseNode, List<(BaseNode target, TransitionType type)>> _connections = new();
        private readonly Dictionary<string, BaseNode> _nodesByGuid = new();
        private readonly HashSet<BaseNode> _visited = new();
        private readonly Queue<(BaseNode node, bool signal)> _queue = new();

        public void SetState(BaseState newState) {
            if (_currentState != null) {
                _currentState.Exit();
            }

            _currentState = newState;
            _currentState.Enter();
        }

        public BaseState GetCurrentState() => _currentState;

        public void AddNode(BaseNode node) {
            if (!_nodesByGuid.ContainsKey(node.Guid)) {
                _nodesByGuid[node.Guid] = node;
            }
        }

        public void AddConnection(BaseNode from, BaseNode to, TransitionType transitionType) {
            if (!_connections.ContainsKey(from)) {
                _connections[from] = new List<(BaseNode, TransitionType)>();
            }
            _connections[from].Add((to, transitionType));
        }

        public T GetNodeByGuid<T>(string guid) where T : BaseNode {
            return _nodesByGuid.TryGetValue(guid, out var node) && node is T typed
                ? typed
                : null;
        }

        
        private void Update() {
            if (_currentState == null)
                return;

            _currentState.Update();

            if (_currentState.IsDone) {
                PropagateFrom(_currentState, TransitionType.Completed);
            }
            
            PropagateFrom(_currentState, TransitionType.Continued);
        }

        private void FixedUpdate() {
            _currentState?.FixedUpdate();
        }

        private void PropagateFrom(BaseNode origin, TransitionType filter) {
            _visited.Clear();
            _queue.Clear();

            _queue.Enqueue((origin, true));
            _visited.Add(origin);

            while (_queue.Count > 0) {
                var (node, signal) = _queue.Dequeue();

                if (!_connections.TryGetValue(node, out var targets))
                    continue;

                foreach (var (target, type) in targets) {
                    if (type != TransitionType.Continued && type != filter) continue;

                    switch (target) {
                        case BaseState nextState:
                            if (signal) {
                                SetState(nextState);
                                return;
                            }
                            break;

                        case BaseCondition condition:
                            bool output = condition.Propagate();
                            _queue.Enqueue((condition, output));
                            _visited.Add(condition);
                            break;

                        case BaseLogical logical:
                            logical.InputBuffer.Add(signal);

                            if (logical.InputBuffer.Count >= logical.ExpectedInputs) {
                                bool logicalOutput = logical.Propagate();
                                _queue.Enqueue((logical, logicalOutput));
                                _visited.Add(logical);
                            }
                            break;
                    }
                }
            }
        }
    }
}