using Godot;
using System.Collections.Generic;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    /// <summary>
    /// Centralized point of contact for both user-driven input,
    /// and simulated input. Allows disabling of user input, while
    /// still honoring simulated input.
    /// </summary>    
    public class PlayerInputService : Node
    {
        private readonly Dictionary<string, PlayerInputAction> _inputMapToPlayerInput = new Dictionary<string, PlayerInputAction>
        {
            { "ui_left", PlayerInputAction.Left },
            { "ui_right", PlayerInputAction.Right },
            { "ui_up", PlayerInputAction.Up },
            { "ui_down", PlayerInputAction.Down },
            { "ui_accept", PlayerInputAction.Accept },
            { "ui_cancel", PlayerInputAction.Cancel },
        };

        private readonly Dictionary<PlayerInputAction, InputInfo> _inputInfo = new Dictionary<PlayerInputAction, InputInfo>()
        {
            { PlayerInputAction.Accept, new InputInfo() },
            { PlayerInputAction.Cancel, new InputInfo() },
            { PlayerInputAction.Down, new InputInfo() },
            { PlayerInputAction.Left, new InputInfo() },
            { PlayerInputAction.Right, new InputInfo() },
            { PlayerInputAction.Up, new InputInfo() },
        };

        public bool InputLocked { get; set; } = false;

        public override void _Ready()
        {
            // Defalt priority is 0. Make sure this gets processed first, as it everything else relies on it.
            ProcessPriority = -1;
        }

        // Handles input from the user. Honors InputLocked.
        public override void _UnhandledKeyInput(InputEventKey @event)
        {
            if (InputLocked)
            {
                return;
            }

            InputInfo? info = null;
            foreach (KeyValuePair<string, PlayerInputAction> kv in _inputMapToPlayerInput)
            {
                if (@event.IsAction(kv.Key))
                {
                    info = _inputInfo[kv.Value];
                    break;
                }
            }

            if (info != null)
            {
                info.IsPressed = @event.Pressed;
                info.Frame = @event.Echo ? info.Frame : Engine.GetPhysicsFrames();
            }
        }

        // Public API methods and such

        public bool IsPressed(PlayerInputAction action)
        {
            return _inputInfo[action].IsPressed;
        }

        public bool IsJustPressed(PlayerInputAction action)
        {
            return _inputInfo[action].Frame == Engine.GetPhysicsFrames()
                && _inputInfo[action].IsPressed;
        }

        public bool IsJustReleased(PlayerInputAction action)
        {
            return _inputInfo[action].Frame == Engine.GetPhysicsFrames()
                && _inputInfo[action].IsPressed == false;
        }

        /// <summary>
        /// Reset all buttons to pressed = false.
        /// </summary>
        public void ClearInputs()
        {
            ulong currFrame = Engine.GetPhysicsFrames();
            foreach (var kv in _inputInfo)
            {
                kv.Value.IsPressed = false;
                kv.Value.Frame = currFrame;
            }
        }

        /// <summary>
        /// Used for simulating action presses. Use <see cref="ReleaseAction(PlayerInputAction)"/>
        /// to release the "button".
        /// </summary>
        /// <param name="action"></param>
        public void PressAction(PlayerInputAction action)
        {
            _inputInfo[action].IsPressed = true;
            _inputInfo[action].Frame = Engine.GetPhysicsFrames();
        }

        /// <summary>
        /// Used for simulating button releases. Corresponds to <see cref="PressAction(PlayerInputAction)"/>.
        /// </summary>
        /// <param name="action"></param>
        public void ReleaseAction(PlayerInputAction action)
        {
            _inputInfo[action].IsPressed = false;
            _inputInfo[action].Frame = Engine.GetPhysicsFrames();
        }

        private class InputInfo
        {
            public bool IsPressed { get; set; }
            public ulong Frame { get; set; }
        }
    }

    public enum PlayerInputAction
    {
        Left,
        Right,
        Up,
        Down,
        Accept,
        Cancel
    }
}
