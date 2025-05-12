using System;
using System.Collections.Generic;
using GraphToolsFSM.Editor.Core;
using GraphToolsFSM.Runtime.DataNodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphToolsFSM.Editor.ViewNodes {
    public class StateNodeView : BaseNodeView {
        private const float PulseDuration = 1f;
        
        private readonly VisualElement _progressBar;
        private readonly StyleColor _titleContainerStyleColor;
        private float _lastActiveTime;
        
        public StateNodeView(string nodeType) : base(nodeType) {
            GUID = Guid.NewGuid().ToString();
            
            AddToClassList("state-node");
            
            var input = CategorizedPort.Create(
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Multi,
                typeof(bool),
                PortCategory.State
            );
            input.portName = "In";
            inputContainer.Add(input);

            var output = CategorizedPort.Create(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Multi,
                typeof(bool),
                PortCategory.State | PortCategory.Condition
            );
            output.portName = "Out";
            outputContainer.Add(output);
            
            _progressBar = new VisualElement();
            _progressBar.AddToClassList("progress-bar");
            extensionContainer.Add(_progressBar);
            
            CreateIcons(new Dictionary<string, Color> {
                { "play", Color.green },
                { "stop", Color.gray }
            });
            
            RefreshExpandedState();
            RefreshPorts();
        }
        
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
            base.BuildContextualMenu(evt);
            evt.menu.AppendAction("Add Completed Transition", _ => {
                var graphView = GetFirstAncestorOfType<FSMGraphView>();
                if (graphView == null) return;
                graphView.StartTransitionFrom(this, TransitionType.Completed);
            });
        }
        
        public void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            _progressBar.style.width = Length.Percent(value * 100f);
            _progressBar.RemoveFromClassList("hidden");
        }

        public void HideProgressBar() {
            _progressBar.AddToClassList("hidden");
        }
        
        public void UpdateStateVisual(bool isActive, float progress)
        {
            if (isActive)
            {
                _lastActiveTime = Time.realtimeSinceStartup;

                SetProgress(progress);
                ShowOnlyIcon("play");
                UpdatePulseColor(0f);
            }
            else
            {
                HideProgressBar();
                ShowOnlyIcon("stop");

                float elapsed = Time.realtimeSinceStartup - _lastActiveTime;

                if (elapsed < PulseDuration)
                {
                    float t = Mathf.Clamp01(elapsed / PulseDuration);
                    UpdatePulseColor(t);
                }
                else
                {
                    ResetBackgroundColor();
                }
            }
        }
        
        public void ResetVisual()
        {
            ResetBackgroundColor();
            HideProgressBar();
            ShowOnlyIcon("stop");
        }
        
        private void UpdatePulseColor(float t)
        {
            Color startColor = new Color(153f / 255f, 51f / 255f, 0f / 255f);
            Color endColor = new Color(30f / 255f, 30f / 255f, 30f / 255f);

            float easedT = 1f - Mathf.Pow(1f - t, 2);
            Color lerpedColor = Color.Lerp(startColor, endColor, easedT);

            var titleElement = this.Q<VisualElement>("title");
            if (titleElement != null)
                titleElement.style.backgroundColor = lerpedColor;
        }

        private void ResetBackgroundColor()
        {
            var titleElement = this.Q<VisualElement>("title");
            if (titleElement != null)
                titleElement.style.backgroundColor = new Color(30f / 255f, 30f / 255f, 30f / 255f);
        }
    }
}