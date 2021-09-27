using Godot;

namespace ThousandYearsHome.Entities.Player
{
    public class StateIdle : PlayerStateBase
    {
        [Export] private PlayerState _stateVariant = PlayerState.Idle;
        public override PlayerState StateVariant => _stateVariant;

        [Export] private string _defaultAnimation = "Stand";
        public override string DefaultAnimation => _defaultAnimation;

        public override PlayerState? Run(Player player)
        {
            if (player.VelY > 0)
            {
                player.VelY = 0f;
            }

            if (player.HoriztaonlUnit == 0 && player.VerticalUnit > 0)
            {
                player.ApplyGravity(player.Gravity);
                player.Move();
            }

            if (player.Grounded != true)
            {
                return PlayerState.InAir; // May need to have a separate state for just being in the air, but not rising
            }

            if (player.Jumping)
            {
                return PlayerState.Jumping;
            }

            if (player.HoriztaonlUnit != 0)
            {
                return PlayerState.Running;
            }

            return null;
        }
    }
}
