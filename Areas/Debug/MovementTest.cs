using Godot;
using System;
using ThousandYearsHome.Controls;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome.Areas.Debug
{
    public class MovementTest : Node2D
    {
        private HUD _hud = null!;


        public override void _Ready()
        {
            _hud = GetNode<HUD>("CanvasLayer/HUD");
        }

        public void OnDebugUpdateState(PlayerStateKind newState, float velX, float velY)
        {
            _hud.Debug_SetStateLabel(newState);
            _hud.Debug_SetVelocity(new Vector2(velX, velY));
        }
    }
}
