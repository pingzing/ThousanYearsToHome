using Godot;
using System;
using ThousandYearsHome.Controls.WarmthBarControl;
using ThousandYearsHome.Entities;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome.Controls
{
    public class HUD : Control
    {
        private Label _debugStateLabel = null!;
        private Label _debugXVelLabel = null!;
        private Label _debugYVelLabel = null!;
        private Label _debugXPosLabel = null!;
        private Label _debugYPosLabel = null!;
        private WarmthBar _warmthBar = null!;

        private PlayerSignalBus _playerSignals = null!;

        public override void _Ready()
        {
            _playerSignals = GetNode<PlayerSignalBus>("/root/PlayerSignalBus");

            _debugStateLabel = GetNode<Label>("DEBUG_CurrentStateLabel");
            _debugXVelLabel = GetNode<Label>("DEBUG_VelocityContainer/DEBUG_XVelLabel");
            _debugYVelLabel = GetNode<Label>("DEBUG_VelocityContainer/DEBUG_YVelLabel");
            _debugXPosLabel = GetNode<Label>("DEBUG_PositionContainer/DEBUG_XPosLabel");
            _debugYPosLabel = GetNode<Label>("DEBUG_PositionContainer/DEBUG_YPosLabel");
            _warmthBar = GetNode<WarmthBar>("WarmthBar");
            _warmthBar.OverchargeColor = Color.Color8(153, 228, 80);

            _playerSignals.Connect(nameof(PlayerSignalBus.StateChanged), this, nameof(Debug_SetStateLabel));
            _playerSignals.Connect(nameof(PlayerSignalBus.PositionChanged), this, nameof(Debug_SetPosition));
            _playerSignals.Connect(nameof(PlayerSignalBus.VelocityChanged), this, nameof(Debug_SetVelocity));
            _playerSignals.Connect(nameof(PlayerSignalBus.WarmthChanged), this, nameof(WarmthChanged));
            _playerSignals.Connect(nameof(PlayerSignalBus.ExcessWarmthChanged), this, nameof(ExcessWarmthChanged));
        }

        public void Debug_SetStateLabel(PlayerStateKind oldState, PlayerStateKind newState)
        {
            _debugStateLabel.Text = newState.ToString();
        }

        public void Debug_SetVelocity(Vector2 oldVel, Vector2 newVel)
        {
            _debugXVelLabel.Text = $"XVel: {newVel.x}";
            _debugYVelLabel.Text = $"YVel: {newVel.y}";
        }

        public void Debug_SetPosition(Vector2 oldPos, Vector2 newPos)
        {
            _debugXPosLabel.Text = $"X: {newPos.x}";
            _debugYPosLabel.Text = $"Y: {newPos.y}";
        }

        public void WarmthChanged(float oldWarmth, float newWarmth)
        {
            _warmthBar.UpdateWarmth(oldWarmth, newWarmth);
        }

        private void ExcessWarmthChanged(float oldExcess, float newExcess)
        {
            _warmthBar.UpdateExcessWarmth(oldExcess, newExcess);
        }

        public void HideWarmthBar()
        {
            _warmthBar.Hide();
        }

        public void ShowWarmthBar()
        {
            _warmthBar.Show();
        }
    }
}


