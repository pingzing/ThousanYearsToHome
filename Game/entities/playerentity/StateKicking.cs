using Godot;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    [Tool]
    public class StateKicking : PlayerStateBase
    {
        public const string AnimationName = "Kick";

        public override PlayerStateKind StateKind => PlayerStateKind.Kicking;

        public override string DefaultAnimation => AnimationName;

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


