using Godot;
using System;
using System.Threading.Tasks;
using ThousandYearsHome.Entities.PlayerEntity;

[Tool]
public class StateWallJumping : PlayerStateBase
{
    [Export] private PlayerStateKind _stateKind = PlayerStateKind.WallJumping;
    public override PlayerStateKind StateKind => _stateKind;

    [Export] private string _defaultAnimation = "WallJump";
    public override string DefaultAnimation => _defaultAnimation;

    // TODO: Prevent single-wall climbing, somehow
    public override Task Enter(Player player)
    {
        player.StartWallJumpLockoutTimer();
        int sign = player.FlipH ? 1 : -1; // todo: make this smarter so we go away from the collision point instead of opposite of player facing
        player.VelX = 250 * sign;
        player.VelY = -225f;
        return base.Enter(player);
    }

    public override PlayerStateKind? Run(Player player)
    {
        player.Move();
        if (!player.IsWallJumpLocked)
        {
            return PlayerStateKind.InAir;
        }

        player.ApplyGravity(0);
        return null;
    }
}
