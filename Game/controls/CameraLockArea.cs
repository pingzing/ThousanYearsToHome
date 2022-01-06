using Godot;
using System;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome.Controls
{
    public class CameraLockArea : Area2D
    {
        // Exports
        private NodePath? _playerCameraPath = null;
        [Export]
        public NodePath? PlayerCameraPath
        {
            get => _playerCameraPath;
            set => _playerCameraPath = value;
        }
        private PlayerCamera? _playerCamera = null;

        private bool _lockXAxis = false;
        [Export]
        public bool LockXAxis
        {
            get => _lockXAxis;
            set => _lockXAxis = value;
        }

        private bool _lockYAxis = false;
        [Export]
        public bool LockYAxis
        {
            get => _lockYAxis;
            set => _lockYAxis = value;
        }

        // Local nodes
        private Area2D _cameraLockArea = null!;
        private CollisionShape2D _cameraLockCollisionShape = null!;
        private Position2D _lockXPosition = null!;
        private Position2D _lockYPosition = null!;

        public override void _Ready()
        {
            if (_playerCameraPath != null)
            {
                _playerCamera = GetNode<PlayerCamera>(_playerCameraPath);
            }

            _lockXPosition = GetNode<Position2D>("LockXPosition");
            _lockYPosition = GetNode<Position2D>("LockYPosition");
        }

        public async void CameraLockAreaBodyEntered(Node body)
        {
            if (!(body is Player) || _playerCamera == null)
            {
                return;
            }

            float idleRectYPos = PlayerCamera.ResolutionHeight * .6f;
            if (LockXAxis)
            {
                await _playerCamera.LockXAxis(Position.x + _lockXPosition.Position.x);
                // If locking the X axis, keep the player centered a little lower than normally.
                idleRectYPos = PlayerCamera.ResolutionHeight * .7f;
            }

            if (LockYAxis)
            {
                throw new NotImplementedException("We haven't implemented Y-axis locking yet!");
            }

            _playerCamera.IdleRect = new Rect2(64, idleRectYPos, 40, 16);            
        }

        public void CameraLockAreaBodyExited(Node body)
        {
            if (!(body is Player) || _playerCamera == null)
            {
                return;
            }

            if (LockXAxis)
            {
                _playerCamera.UnlockXAxis();
                _playerCamera.IdleRect = PlayerCamera.DefaultIdleRect;
            }

            if (LockYAxis)
            {
                throw new NotImplementedException("We haven't implemented Y-axis locking yet!");
            }
        }
    }
}

