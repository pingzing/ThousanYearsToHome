using Godot;
using System.Threading.Tasks;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    [Tool]
    public class StateKicking : PlayerStateBase
    {
        public const string AnimationName = "Kick";

        public override PlayerStateKind StateKind => PlayerStateKind.Kicking;

        public override string DefaultAnimation => AnimationName;

        private Timer _kickTimer = null!;

        public override void _Ready()
        {
            _kickTimer = GetParent().GetParent<Player>().GetNode<Timer>("KickTimer");
        }

        public override Task Enter(Player player)
        {
            base.Enter(player);
            _kickTimer.Start();
            return Task.CompletedTask;
        }

        public override PlayerStateKind? Run(Player player)
        {
            if (player.IsKicking)
            {
                return null;
            }
            else
            {
                return PlayerStateKind.Idle;
            }
        }
    }
}


