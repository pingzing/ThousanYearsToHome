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
        private float? _defaultKickAnimationDuration = null;

        public override void _Ready()
        {
            _kickTimer = GetParent().GetParent<Player>().GetNode<Timer>("KickTimer");
        }

        public override async Task Enter(Player player)
        {
            if (player.KickOverride != null)
            {
                await player.KickOverride(player, _kickTimer);
            }
            else
            {
                if (_defaultKickAnimationDuration == null)
                {
                    _defaultKickAnimationDuration = player.GetPoseAnimationDuration(AnimationName);
                }
                await base.Enter(player);
                _kickTimer.Start(_defaultKickAnimationDuration.Value);
            }
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


