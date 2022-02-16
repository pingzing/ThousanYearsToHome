using Godot;
using System.Threading.Tasks;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    [Tool]
    public class StateKicking : PlayerStateBase
    {
        public const string AnimationName = "Kick";
        public const string FrozenKickName = "FrozenKick";

        public override PlayerStateKind StateKind => PlayerStateKind.Kicking;

        public override string DefaultAnimation => AnimationName;

        private float? _defaultKickAnimationDuration = null;
        // Controls the IsKicking property over in Player.
        private Timer _kickTimer = null!;

        public override void _Ready()
        {
            _kickTimer =  GetParent().GetParent<Player>().GetNode<Timer>("KickTimer");
        }

        public override async Task Enter(Player player)
        {
            player.EmitSignal(nameof(Player.KickEntered));
            if (player.EnterKickOverride != null)
            {
                await player.EnterKickOverride(player, _kickTimer);
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
                player.EmitSignal(nameof(Player.KickExited));
                return PlayerStateKind.Idle;
            }
        }

        public void PreKick(NodePath playerNode)
        {
            Player player = GetNode<Player>(playerNode);
            player.EmitSignal(nameof(Player.PreKicking), player);
        }
    }
}


