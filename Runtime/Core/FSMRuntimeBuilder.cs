using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using GraphToolsFSM.Runtime.Attributes;
using GraphToolsFSM.Runtime.DataNodes;
using GraphToolsFSM.Runtime.Nodes;
using UnityEngine;

namespace GraphToolsFSM.Runtime.Core {
    public static class FSMRuntimeBuilder {
        private const string EntryGuid = "ENTRY";

        public static void InjectFromAsset(StateMachine stateMachine, FSMGraphData data) {
            var nodeMap = new Dictionary<string, BaseNode>();

            // Instantiate nodes
            foreach (var nodeData in data.Nodes) {
                var type = Type.GetType(nodeData.NodeType);
                if (type == null || !typeof(BaseNode).IsAssignableFrom(type)) {
                    Debug.LogError($"[FSM] Type introuvable ou invalide : {nodeData.NodeType}");
                    continue;
                }

                var ctor = type.GetConstructor(new[] { typeof(StateMachine) });
                var instance = ctor != null
                    ? (BaseNode)ctor.Invoke(new object[] { stateMachine })
                    : Activator.CreateInstance(type) as BaseNode;

                if (instance == null) {
                    Debug.LogError($"[FSM] Impossible d’instancier : {nodeData.NodeType}");
                    continue;
                }

                instance.Guid = nodeData.Guid;
                InjectCustomFields(instance, nodeData.CustomFields);
                nodeMap[nodeData.Guid] = instance;
                stateMachine.AddNode(instance);
            }

            // Create transitions
            foreach (var transition in data.Transitions) {
                if (!nodeMap.TryGetValue(transition.FromGuid, out var from))
                    continue;
                if (!nodeMap.TryGetValue(transition.ToGuid, out var to))
                    continue;

                stateMachine.AddConnection(from, to, transition.Type);
                
                if (to is BaseLogical logical) {
                    logical.ExpectedInputs++;
                }
            }

            // Start from entry
            var entry = data.Transitions.Find(t => t.FromGuid == EntryGuid);
            if (entry != null && nodeMap.TryGetValue(entry.ToGuid, out var entryTarget) && entryTarget is BaseState initialState) {
                stateMachine.SetState(initialState);
            } else {
                Debug.LogWarning("[FSM] Aucun état initial défini ou incorrect.");
            }
        }

        private static void InjectCustomFields(object instance, List<FieldPair> fields) {
            if (fields == null) return;

            var type = instance.GetType();

            foreach (var pair in fields) {
                // Champs
                var field = type.GetField(pair.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.GetCustomAttribute<FSMFieldAttribute>() != null) {
                    try {
                        object parsed = ParsePrimitive(pair.Value, field.FieldType);
                        field.SetValue(instance, parsed);
                        continue;
                    } catch (Exception ex) {
                        Debug.LogWarning($"[FSM] Erreur d’injection (champ) sur {pair.Key} : {ex.Message}");
                        continue;
                    }
                }

                // Propriétés
                var prop = type.GetProperty(pair.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null && prop.GetCustomAttribute<FSMFieldAttribute>() != null && prop.CanWrite) {
                    try {
                        object parsed = ParsePrimitive(pair.Value, prop.PropertyType);
                        prop.SetValue(instance, parsed);
                    } catch (Exception ex) {
                        Debug.LogWarning($"[FSM] Erreur d’injection (propriété) sur {pair.Key} : {ex.Message}");
                    }
                }
            }
        }
        
        private static object ParsePrimitive(string strValue, Type targetType) {
            if (targetType == typeof(float))
                return float.Parse(strValue, CultureInfo.InvariantCulture);
            if (targetType == typeof(int))
                return int.Parse(strValue, CultureInfo.InvariantCulture);
            if (targetType == typeof(bool))
                return bool.Parse(strValue);

            throw new NotSupportedException($"Type non supporté : {targetType.Name}");
        }
    }
}
