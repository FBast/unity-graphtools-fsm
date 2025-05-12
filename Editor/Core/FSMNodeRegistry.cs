using System;
using System.Collections.Generic;
using System.Reflection;
using GraphToolsFSM.Runtime.Attributes;
using GraphToolsFSM.Runtime.Nodes;
using UnityEditor;

namespace GraphToolsFSM.Editor.Core {
    public static class FSMNodeRegistry {
        private const string UniversalKey = "__universal__";
        private static readonly Dictionary<string, List<Type>> StatesByFSM;
        private static readonly Dictionary<string, List<Type>> ConditionsByFSM;
        private static readonly Dictionary<string, List<Type>> LogicalsByFSM;

        static FSMNodeRegistry() {
            StatesByFSM = new Dictionary<string, List<Type>>();
            ConditionsByFSM = new Dictionary<string, List<Type>>();
            LogicalsByFSM = new Dictionary<string, List<Type>>();

            foreach (var type in TypeCache.GetTypesWithAttribute<FSMTargetAttribute>()) {
                var attr = type.GetCustomAttribute<FSMTargetAttribute>();
                if (attr == null) continue;

                var key = attr.TargetFSM ?? UniversalKey;

                if (typeof(BaseState).IsAssignableFrom(type)) {
                    if (!StatesByFSM.TryGetValue(key, out var list))
                        StatesByFSM[key] = list = new List<Type>();
                    list.Add(type);
                }
                else if (typeof(BaseCondition).IsAssignableFrom(type)) {
                    if (!ConditionsByFSM.TryGetValue(key, out var list))
                        ConditionsByFSM[key] = list = new List<Type>();
                    list.Add(type);
                }
                else if (typeof(BaseLogical).IsAssignableFrom(type)) {
                    if (!LogicalsByFSM.TryGetValue(key, out var list))
                        LogicalsByFSM[key] = list = new List<Type>();
                    list.Add(type);
                }
            }
        }

        public static List<Type> GetStateNodeTypesFor(string fsmName) {
            var result = new List<Type>();

            if (StatesByFSM.TryGetValue(UniversalKey, out var universal))
                result.AddRange(universal);

            if (StatesByFSM.TryGetValue(fsmName, out var specific))
                result.AddRange(specific);

            return result;
        }

        public static List<Type> GetConditionNodeTypesFor(string fsmName) {
            var result = new List<Type>();

            if (ConditionsByFSM.TryGetValue(UniversalKey, out var universal))
                result.AddRange(universal);

            if (ConditionsByFSM.TryGetValue(fsmName, out var specific))
                result.AddRange(specific);

            return result;
        }
        
        public static List<Type> GetLogicalNodeTypesFor(string fsmName) {
            var result = new List<Type>();

            if (LogicalsByFSM.TryGetValue(UniversalKey, out var universal))
                result.AddRange(universal);

            if (LogicalsByFSM.TryGetValue(fsmName, out var specific))
                result.AddRange(specific);

            return result;
        }
    }
}