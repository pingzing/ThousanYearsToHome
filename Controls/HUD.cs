using Godot;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome.Controls
{
    public class HUD : Control
    {
        private Label _debugStateLabel = null!;
        private Label _debugXVelLabel = null!;
        private Label _debugYVelLabel = null!;

        public override void _Ready()
        {
            _debugStateLabel = GetNode<Label>("DEBUG_CurrentStateLabel");
            _debugXVelLabel = GetNode<Label>("DEBUG_VelocityContainer/DEBUG_XVelLabel");
            _debugYVelLabel = GetNode<Label>("DEBUG_VelocityContainer/DEBUG_YVelLabel");
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
    }
}


