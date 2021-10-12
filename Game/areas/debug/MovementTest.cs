using Godot;
using System;
using ThousandYearsHome.Controls;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome.Areas.Debug
{
    public class MovementTest : Node2D
    {
        private HUD _hud = null!;
        private Node2D _cameraTarget = null!;
        private Player _player = null!;

        public override void _Ready()
        {
            _hud = GetNode<HUD>("CanvasLayer/HUD");
            _cameraTarget = GetNode<Node2D>("Player/CameraTarget/Camera2D");
            _player = GetNode<Player>("Player");
        }

        public override void _Process(float delta)
        {
            //_cameraTarget.Position = new Vector2(_player.Position.x + 50, _cameraTarget.Position.y);
        }

        public void OnDebugUpdateState(PlayerStateKind newState, float velX, float velY)
        {
            _hud.Debug_SetStateLabel(newState);
            _hud.Debug_SetVelocity(new Vector2(velX, velY));
        }

    }
}
