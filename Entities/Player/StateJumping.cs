using Godot;

namespace ThousandYearsHome.Entities.Player
{
    public class StateJumping : PlayerStateBase
    {
        [Export] private PlayerState _stateVariant = PlayerState.Jumping;
        public override PlayerState StateVariant => _stateVariant;

        [Export] private string _defaultAnimation = "Jump";
        public override string DefaultAnimation => _defaultAnimation;

        [Export] private float JumpSpeed = 600f;

        public override PlayerState? Run(Player player)
        {
            player.VelY = -JumpSpeed;
            player.Move();
            return PlayerState.InAir;
        }
    }
}
