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

    [Export] public float WallJumpVelX { get; set; } = 270;
    [Export] public float WallJumpVelY { get; set; } = -325f;
    [Export] public float WallJumpGravity { get; set; } = 10f;
    
    public override Task Enter(Player player)
    {
        player.StartWallJumpLockoutTimer();
        int sign = -player.HorizontalUnit;
        player.VelX = WallJumpVelX * sign;
        player.VelY = WallJumpVelY;
        return base.Enter(player);
    }

    public override PlayerStateKind? Run(Player player)
    {
        player.Move();
        if (!player.IsWallJumpLocked)
        {
            return PlayerStateKind.InAir;
        }

        player.ApplyGravity(WallJumpGravity);
        return null;
    }
}
