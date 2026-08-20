using System;
using UnityEngine;
using UPnL.SignalRush.Tuning;

namespace UPnL.SignalRush.Player
{
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _body;
        [SerializeField] private SignalRushTuning _tuning;
        [SerializeField] private PlayerStatus _status;
        [SerializeField] private Transform _groundProbe;
        [SerializeField] private float _groundProbeRadius = 0.1f;
        [SerializeField] private LayerMask _groundLayers;

        private float _moveInput;
        private float _speedMultiplier = 1f;

        public bool IsGrounded { get; private set; }
        public Vector2 Position => _body.position;
        public Vector2 SafePosition { get; private set; }

        public event Action<bool> GroundedChanged;

        private void FixedUpdate()
        {
            Simulate(ProbeGrounded(), Time.fixedDeltaTime);
        }

        public void SetMoveInput(float horizontal)
        {
            if (IsControlLocked)
                return;

            _moveInput = Mathf.Clamp(horizontal, -1f, 1f);
        }

        public void ClearMoveInput()
        {
            _moveInput = 0f;
        }

        public void RequestJump()
        {
            if (IsControlLocked || !IsGrounded)
                return;

            var velocity = _body.linearVelocity;
            velocity.y = _tuning.JumpVelocity;
            _body.linearVelocity = velocity;
            SetGrounded(false);
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Clamp(multiplier, 1f, _tuning.MaxRunSpeed / _tuning.BaseRunSpeed);
        }

        public void Respawn(Vector2 position)
        {
            _body.linearVelocity = Vector2.zero;
            _body.position = position;
            SafePosition = position;
        }

        public void Simulate(bool grounded, float deltaTime)
        {
            grounded &= _body.linearVelocity.y <= 0f;
            SetGrounded(grounded);
            if (grounded)
                SafePosition = _body.position;

            var velocity = _body.linearVelocity;
            var correction = IsControlLocked ? 0f : _moveInput;
            velocity.x = _tuning.BaseRunSpeed * _speedMultiplier + correction * _tuning.HorizontalCorrectionSpeed;
            if (velocity.y < 0f && deltaTime > 0f)
                velocity.y += Physics2D.gravity.y * (_tuning.FallGravityMultiplier - 1f) * deltaTime;
            _body.linearVelocity = velocity;
        }

        private bool IsControlLocked => _status != null && _status.IsControlLocked;

        private bool ProbeGrounded()
        {
            return _groundProbe != null && Physics2D.OverlapCircle(_groundProbe.position, _groundProbeRadius, _groundLayers) != null;
        }

        private void SetGrounded(bool grounded)
        {
            if (IsGrounded == grounded)
                return;

            IsGrounded = grounded;
            GroundedChanged?.Invoke(grounded);
        }
    }
}
