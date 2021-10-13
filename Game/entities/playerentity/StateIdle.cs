using Godot;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    [Tool]
    public class StateIdle : PlayerStateBase
    {
        [Export] private PlayerStateKind _stateKind = PlayerStateKind.Idle;
        public override PlayerStateKind StateKind => _stateKind;

        [Export] private string _defaultAnimation = "Idle";
        public override string DefaultAnimation => _defaultAnimation;

        public override PlayerStateKind? Run(Player player)
        {
            if (player.VelY > 0)
            {
                player.VelY = 0f;
            }

            if (player.HorizontalUnit == 0 && player.VerticalUnit > 0)
            {
                player.ApplyGravity(Player.IdleGravity);
                player.Move();
            }

            if (player.Grounded != true)
            {
                return PlayerStateKind.InAir;
            }

            if (player.Jumping)
            {
                return PlayerStateKind.Jumping;
            }

            if (player.HorizontalUnit != 0)
            {
                return PlayerStateKind.Running;
            }

            if (player.Crouching)
            {
                return PlayerStateKind.Crouching;
            }

            return null;
        }
    }
}
