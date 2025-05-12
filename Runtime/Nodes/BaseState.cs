using GraphToolsFSM.Runtime.Attributes;
using GraphToolsFSM.Runtime.Core;
using UnityEngine;

namespace GraphToolsFSM.Runtime.Nodes {
    public abstract class BaseState : BaseNode {
        protected BaseState(StateMachine stateMachine) : base(stateMachine) { }
        
        [FSMField] public float Duration { get; set; }

        private float _timer;
        private bool _isPaused;

        public bool IsDone => _timer >= Duration;

        public float Progress => Duration > 0 ? Mathf.Clamp01(_timer / Duration) : 1f;

        public void Enter()
        {
            _timer = 0f;
            _isPaused = false;
            OnEnter();
        }

        public void Restart()
        {
            _timer = 0f;
        }

        public void Update()
        {
            if (!_isPaused)
                _timer += Time.deltaTime;

            OnUpdate();
        }

        public void FixedUpdate() => OnFixedUpdate();

        public void Exit() => OnExit();

        public void Pause() => _isPaused = true;

        public void Resume() => _isPaused = false;

        protected virtual void OnEnter() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnFixedUpdate() { }
        protected virtual void OnExit() { }
    }
}