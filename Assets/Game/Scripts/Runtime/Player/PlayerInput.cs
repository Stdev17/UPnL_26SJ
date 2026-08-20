using UnityEngine;
using UnityEngine.InputSystem;
using UPnL.SignalRush.Run;

namespace UPnL.SignalRush.Player
{
    public sealed class PlayerInput : MonoBehaviour
    {
        [SerializeField] private InputActionReference _move;
        [SerializeField] private InputActionReference _jump;
        [SerializeField] private InputActionReference _attack;
        [SerializeField] private PlayerMotor2D _motor;
        [SerializeField] private PlayerCombat _combat;
        [SerializeField] private RunController _runController;

        private void OnEnable()
        {
            var move = _move == null ? null : _move.action;
            if (move != null)
            {
                move.performed += HandleMove;
                move.canceled += HandleMoveCanceled;
                move.Enable();
            }

            var jump = _jump == null ? null : _jump.action;
            if (jump != null)
            {
                jump.performed += HandleJump;
                jump.Enable();
            }

            var attack = _attack == null ? null : _attack.action;
            if (attack != null)
            {
                attack.performed += HandleAttack;
                attack.Enable();
            }
        }

        private void OnDisable()
        {
            _motor?.ClearMoveInput();

            var move = _move == null ? null : _move.action;
            if (move != null)
            {
                move.performed -= HandleMove;
                move.canceled -= HandleMoveCanceled;
                move.Disable();
            }

            var jump = _jump == null ? null : _jump.action;
            if (jump != null)
            {
                jump.performed -= HandleJump;
                jump.Disable();
            }

            var attack = _attack == null ? null : _attack.action;
            if (attack != null)
            {
                attack.performed -= HandleAttack;
                attack.Disable();
            }
        }

        public void HandleAttack()
        {
            if (_runController == null)
                return;

            if (_runController.Phase == RunPhase.Finished)
                _runController.Restart();
            else if (_runController.Phase == RunPhase.Running)
                _combat?.RequestAttack();
        }

        private void HandleMove(InputAction.CallbackContext context)
        {
            _motor?.SetMoveInput(context.ReadValue<float>());
        }

        private void HandleMoveCanceled(InputAction.CallbackContext context)
        {
            _motor?.ClearMoveInput();
        }

        private void HandleJump(InputAction.CallbackContext context)
        {
            if (_runController != null && _runController.Phase == RunPhase.Running)
                _motor?.RequestJump();
        }

        private void HandleAttack(InputAction.CallbackContext context)
        {
            HandleAttack();
        }
    }
}
