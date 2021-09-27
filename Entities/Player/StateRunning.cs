using Godot;

namespace ThousandYearsHome.Entities.Player
{
    public class StateRunning : PlayerStateBase
    {
        [Export] private PlayerState _stateVariant = PlayerState.Running;
        public override PlayerState StateVariant => _stateVariant;

        [Export] private string _defaultAnimation = "Walk";
        public override string DefaultAnimation => _defaultAnimation;

        [Export] private float RunSpeed = 200;

        public override PlayerState? Run(Player player)
        {
            if (player.VerticalUnit > 0)
            {
                // example does some layer shenanigans for 1-way platforms
            }

            player.VelX = player.HoriztaonlUnit * RunSpeed;
            player.ApplyGravity(player.Gravity);
            player.Move();

            if (!player.IsOnFloor())
            {
                return PlayerState.InAir;
            }
            if (player.VelY > 0)
            {
                player.VelY = 0;
            }
            if (player.Jumping)
            {
                return PlayerState.Jumping;
            }
            if (player.HoriztaonlUnit == 0)
            {
                return PlayerState.Idle;
            }

            return null;
        }
    }
}
