using Godot;

namespace ThousandYearsHome.Entities.Player
{
    public abstract class PlayerStateBase : Node2D
    {
        public abstract PlayerState StateVariant { get; }
        public abstract string DefaultAnimation { get; }

        public override void _Ready()
        {

        }

        public virtual void Enter(Player player)
        {
            player.AnimatePose(DefaultAnimation);
        }

        public abstract PlayerState? Run(Player player);

        public virtual void Exit(Player player)
        {
            player.ClearPoseAnimationQueue();
        }
    }
}
