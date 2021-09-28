using Godot;

namespace ThousandYearsHome.Entities.Player
{
    public abstract class PlayerStateBase : Node2D
    {
        public abstract PlayerStateKind StateKind { get; }
        public abstract string DefaultAnimation { get; }

        public virtual void Enter(Player player)
        {
            player.AnimatePose(DefaultAnimation);
        }

        public abstract PlayerStateKind? Run(Player player);

        public virtual void Exit(Player player)
        {
            player.ClearPoseAnimationQueue();
        }
    }
}
