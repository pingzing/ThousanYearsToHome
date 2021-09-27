using Godot;
using System;

namespace ThousandYearsHome.Entities.Player
{
    public class StateInAir : PlayerStateBase
    {

        [Export] private PlayerState _stateVariant = PlayerState.InAir;
        public override PlayerState StateVariant => _stateVariant;

        [Export] private string _defaultAnimation = "InAir";
        public override string DefaultAnimation => _defaultAnimation;

        [Export] private float _inAirSpeed = 300f;

        public override void Enter(Player player)
        {
            if (player.CurrentAnimationName == "Jump")
            {
                // TODO: wait for animation to finish then call base.Enter()
            }
            else
            {
                base.Enter(player);
            }
        }

        public override PlayerState? Run(Player player)
        {
            player.VelX = player.HoriztaonlUnit * _inAirSpeed;
            player.ApplyGravity(player.Gravity);
            player.Move();

            if (player.IsOnFloor())
            {
                return player.HoriztaonlUnit == 0
                    ? PlayerState.Idle
                    : PlayerState.Running;
            }

            if (player.Grounded && player.Jumping) // TODO: ??? how's this possible? some weird edge state just after leaving the ground?
            {
                return PlayerState.Jumping;
            }

            return null;
        }
    }
}
