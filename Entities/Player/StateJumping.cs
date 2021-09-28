using Godot;

namespace ThousandYearsHome.Entities.Player
{
    public class StateJumping : PlayerStateBase
    {
        [Export] private PlayerStateKind _stateKind = PlayerStateKind.Jumping;
        public override PlayerStateKind StateKind => _stateKind;

        [Export] private string _defaultAnimation = "Jump";
        public override string DefaultAnimation => _defaultAnimation;

        [Export] private float JumpSpeed = 600f;

        public override PlayerStateKind? Run(Player player)
        {
            player.VelY = -JumpSpeed;
            player.Move();
            return PlayerStateKind.InAir;
        }
    }
}
