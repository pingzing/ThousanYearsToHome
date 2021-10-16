using Godot;
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

        public override void _Ready()
        {
            _debugStateLabel = GetNode<Label>("DEBUG_CurrentStateLabel");
            _debugXVelLabel = GetNode<Label>("DEBUG_VelocityContainer/DEBUG_XVelLabel");
            _debugYVelLabel = GetNode<Label>("DEBUG_VelocityContainer/DEBUG_YVelLabel");
            _debugXPosLabel = GetNode<Label>("DEBUG_PositionContainer/DEBUG_XPosLabel");
            _debugYPosLabel = GetNode<Label>("DEBUG_PositionContainer/DEBUG_YPosLabel");
        }

        public void Debug_SetStateLabel(PlayerStateKind newState)
        {
            _debugStateLabel.Text = newState.ToString();
        }

        public void Debug_SetVelocity(Vector2 velocity)
        {
            _debugXVelLabel.Text = $"XVel: {velocity.x}";
            _debugYVelLabel.Text = $"YVel: {velocity.y}";
        }

        public void Debug_SetPosition(Vector2 position)
        {
            _debugXPosLabel.Text = $"X: {position.x}";
            _debugYPosLabel.Text = $"Y: {position.y}";
        }
    }
}


