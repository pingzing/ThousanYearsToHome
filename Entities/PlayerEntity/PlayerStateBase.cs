using Godot;
using System.Threading.Tasks;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    public abstract class PlayerStateBase : Node2D
    {
        public abstract PlayerStateKind StateKind { get; }
        public abstract string DefaultAnimation { get; }

        public virtual Task Enter(Player player)
        {
            player.AnimatePose(DefaultAnimation);
            return Task.CompletedTask;
        }

        public abstract PlayerStateKind? Run(Player player);

        public virtual Task Exit(Player player)
        {
            player.ResetPoseAnimation();
            return Task.CompletedTask;

        }
    }
}
