using Godot;

namespace ThousandYearsHome.Entities.Player
{
    public class StateRunning : PlayerStateBase
    {
        [Export] private PlayerStateKind _stateKind = PlayerStateKind.Running;
        public override PlayerStateKind StateKind => _stateKind;

        [Export] private string _defaultAnimation = "Walk";
        public override string DefaultAnimation => _defaultAnimation;

        [Export] private float RunSpeed = 200;

        public override PlayerStateKind? Run(Player player)
        {
            if (player.VerticalUnit > 0)
            {
                // example does some layer shenanigans for 1-way platforms
            }

            player.VelX = player.HorizontalUnit * RunSpeed;
            player.ApplyGravity(player.Gravity);
            player.Move();

            if (!player.IsOnFloor())
            {
                return PlayerStateKind.InAir;
            }
            if (player.VelY > 0)
            {
                player.VelY = 0;
            }
            if (player.Jumping)
            {
                return PlayerStateKind.Jumping;
            }
            if (player.HorizontalUnit == 0)
            {
                return PlayerStateKind.Idle;
            }

            return null;
        }
    }
}
