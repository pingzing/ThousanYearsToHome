using Godot;
using System;
using System.Threading.Tasks;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    public class StateInAir : PlayerStateBase
    {

        [Export] private PlayerStateKind _stateKind = PlayerStateKind.InAir;
        public override PlayerStateKind StateKind => _stateKind;

        [Export] private string _defaultAnimation = "InAir";
        public override string DefaultAnimation => _defaultAnimation;

        [Export] private float _inAirSpeed = 200f;

        public override async Task Enter(Player player)
        {
            if (player.CurrentAnimationName == "Jump")
            {
                await player.WaitForCurrentPoseAnimationAsync();
                // TODO: wait for animation to finish then call base.Enter()
            }
            else
            {
                await base.Enter(player);
            }
        }

        public override PlayerStateKind? Run(Player player)
        {
            player.VelX = player.HorizontalUnit * _inAirSpeed;
            player.ApplyGravity(player.FallGravity);
            player.Move();

            if (player.IsOnFloor())
            {
                return player.HorizontalUnit == 0
                    ? PlayerStateKind.Idle
                    : PlayerStateKind.Running;
            }

            if (player.Grounded && player.Jumping) // Allows jumping for a split second after beginning to fall
            {
                return PlayerStateKind.Jumping;
            }

            return null;
        }
    }
}
