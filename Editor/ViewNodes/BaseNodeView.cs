using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using GraphToolsFSM.Editor.Core;
using GraphToolsFSM.Editor.Helpers;
using GraphToolsFSM.Runtime.Attributes;
using GraphToolsFSM.Runtime.DataNodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphToolsFSM.Editor.ViewNodes {
    public abstract class BaseNodeView : Node {
        public string GUID;
        public Action OnModified;
        private Vector2 _lastPosition;
        
        private readonly Dictionary<string, Image> _icons = new();

        protected BaseNodeView(string nodeType) {
            NodeType = nodeType;
            title = GetPrettyTypeName(nodeType);
            AddToClassList("base-node");
            GenerateDynamicFields();

            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        protected string NodeType { get; }

        public virtual BaseNodeData ToData() {
            return new BaseNodeData {
                Guid = GUID,
                Position = GetPosition().position,
                NodeType = NodeType,
                CustomFields = CollectCustomFields()
            };
        }
        
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
            base.BuildContextualMenu(evt);
            
            // Add Transition
            evt.menu.AppendAction("Add Continued Transition", _ => {
                var graphView = GetFirstAncestorOfType<FSMGraphView>();
                if (graphView == null) return;
                graphView.StartTransitionFrom(this, TransitionType.Continued);
            });
            
            // Duplicate
            evt.menu.AppendAction("Duplicate", _ =>
            {
                Duplicate(new Vector2(30, 30));
            });
        }

        public virtual void Duplicate(Vector2 offset)
        {
            var graphView = GetFirstAncestorOfType<FSMGraphView>();
            if (graphView == null) return;
            
            var data = ToData();
            data.Position += offset;

            var copy = NodeViewFactory.CreateFromData(data);
            if (copy == null) return;
                
            graphView.AddElement(copy);
            graphView.AddToSelection(copy);
            graphView.RemoveFromSelection(this);
        }

        private void OnMouseDown(MouseDownEvent evt) {
            if (evt.button != 0) return;
            var graphView = GetFirstAncestorOfType<FSMGraphView>();
            if (graphView == null) return;
            if (graphView.IsTransitionPending) {
                graphView.CompleteTransitionTo(this);
                evt.StopPropagation();
            }
        }

        private string GetPrettyTypeName(string assemblyQualifiedName) {
            if (string.IsNullOrEmpty(assemblyQualifiedName)) return "Unassigned";
            var type = Type.GetType(assemblyQualifiedName);
            return type != null ? ObjectNames.NicifyVariableName(type.Name) : "Unknown";
        }

        protected void CreateIcons(Dictionary<string, Color> iconMap)
        {
            var titleLabel = titleContainer.Q<Label>(title);
            var titleParent = titleLabel?.parent ?? titleContainer;

            foreach (var (iconClass, tint) in iconMap)
            {
                var image = new Image
                {
                    name = $"icon-{iconClass}",
                    pickingMode = PickingMode.Ignore
                };
                image.AddToClassList("node-icon");
                image.style.display = DisplayStyle.None;

                var texture = AssetLoader.LoadIcon($"icon_{iconClass}");
                if (texture != null)
                {
                    image.style.backgroundImage = new StyleBackground(texture);
                    image.style.unityBackgroundImageTintColor = tint;
                }

                titleParent.Insert(0, image);
                _icons[iconClass] = image;
            }
        }

        protected void ShowOnlyIcon(string activeClass)
        {
            foreach (var kvp in _icons)
                kvp.Value.style.display = kvp.Key == activeClass ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void GenerateDynamicFields() {
            if (NodeType == null) return;

            var runtimeType = Type.GetType(NodeType);
            if (runtimeType == null) return;

            // Fields
            foreach (var field in runtimeType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                if (field.GetCustomAttribute<FSMFieldAttribute>() == null) continue;

                CreateFieldUI(field.Name, field.FieldType);
            }

            // Properties
            foreach (var prop in runtimeType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                if (prop.GetCustomAttribute<FSMFieldAttribute>() == null) continue;
                if (!prop.CanRead || !prop.CanWrite) continue;

                CreateFieldUI(prop.Name, prop.PropertyType);
            }

            RefreshExpandedState();
        }

        private void CreateFieldUI(string name, Type type) {
            if (type == typeof(float)) {
                var field = new FloatField(ObjectNames.NicifyVariableName(name)) {
                    value = 0f,
                    name = name
                };
                field.RegisterValueChangedCallback(_ => OnModified?.Invoke());
                extensionContainer.Add(field);
            }
            else if (type == typeof(int)) {
                var field = new IntegerField(ObjectNames.NicifyVariableName(name)) {
                    value = 0,
                    name = name
                };
                field.RegisterValueChangedCallback(_ => OnModified?.Invoke());
                extensionContainer.Add(field);
            }
            else if (type == typeof(bool)) {
                var field = new Toggle(ObjectNames.NicifyVariableName(name)) {
                    value = false,
                    name = name
                };
                field.RegisterValueChangedCallback(_ => OnModified?.Invoke());
                extensionContainer.Add(field);
            }

            // More types here
        }

        private List<FieldPair> CollectCustomFields() {
            var result = new List<FieldPair>();

            foreach (var child in extensionContainer.Children()) {
                switch (child) {
                    case FloatField floatField:
                        result.Add(new FieldPair {
                            Key = floatField.name,
                            Value = floatField.value.ToString(CultureInfo.InvariantCulture)
                        });
                        break;
                    case IntegerField intField:
                        result.Add(new FieldPair {
                            Key = intField.name,
                            Value = intField.value.ToString()
                        });
                        break;
                    case Toggle toggle:
                        result.Add(new FieldPair {
                            Key = toggle.name,
                            Value = toggle.value.ToString()
                        });
                        break;
                }
            }

            return result;
        }

        public void ApplyCustomFields(List<FieldPair> fields) {
            foreach (var pair in fields) {
                if (extensionContainer.Q<FloatField>(pair.Key) is FloatField floatField &&
                    float.TryParse(pair.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatVal)) {
                    floatField.value = floatVal;
                } 
                else if (extensionContainer.Q<IntegerField>(pair.Key) is IntegerField intField &&
                         int.TryParse(pair.Value, out var intVal)) {
                    intField.value = intVal;
                }
                else if (extensionContainer.Q<Toggle>(pair.Key) is Toggle toggle &&
                         bool.TryParse(pair.Value, out var boolVal)) {
                    toggle.value = boolVal;
                }
            }
        }
    }
}