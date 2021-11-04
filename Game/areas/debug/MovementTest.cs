using Godot;
using System;
using ThousandYearsHome.Controls;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome.Areas.Debug
{
    public class MovementTest : Node2D
    {
        private HUD _hud = null!;
        private Player _player = null!;

        public override void _Ready()
        {
            _hud = GetNode<HUD>("UICanvas/HUD");
            _player = GetNode<Player>("Player");
        }

        public void OnPlayerDebugUpdateState(PlayerStateKind newState, float velX, float velY, Vector2 position)
        {
            _hud.Debug_SetStateLabel(newState);
            _hud.Debug_SetVelocity(new Vector2(velX, velY));
            _hud.Debug_SetPosition(position);
        }

    }
}
