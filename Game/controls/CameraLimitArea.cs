using Godot;
using System;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome.Controls
{
    [Tool]
    public class CameraLimitArea : Area2D
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

        private float _limitLeft;
        [Export(PropertyHint.Range, "-9999999, 9999999, 1")]
        public float LimitLeft
        {
            get => _limitLeft;
            set { _limitLeft = value; Update(); }
        }

        private float _limitRight;
        [Export(PropertyHint.Range, "-9999999, 9999999, 1")]
        public float LimitRight
        {
            get => _limitRight;
            set { _limitRight = value; Update(); }
        }

        private float _limitTop;
        [Export(PropertyHint.Range, "-9999999, 9999999, 1")]
        public float LimitTop
        {
            get => _limitTop;
            set { _limitTop = value; Update(); }
        }

        private float _limitBottom;
        [Export(PropertyHint.Range, "-9999999, 9999999, 1")]
        public float LimitBottom
        {
            get => _limitBottom;
            set { _limitBottom = value; Update(); }
        }

        // Local nodes
        private Area2D _cameraLimitArea = null!;
        private CollisionShape2D _cameraLimitShape = null!;
        private Tween _tweener = null!;

        public override void _Ready()
        {
            if (_playerCameraPath != null)
            {
                _playerCamera = GetNode<PlayerCamera>(_playerCameraPath);
            }

            _cameraLimitArea = GetNode<Area2D>("CameraLimitArea");
            _cameraLimitShape = GetNode<CollisionShape2D>("CameraLimitShape");
            _tweener = GetNode<Tween>("Tweener");
        }

        public void CameraLimitAreaEntered(Node body)
        {
            if (!(body is Player) || _playerCamera == null)
            {
                return;
            }

            // TODO
            // Store limit before entering so we can restore it when we leave
            // Tween camera limits to new set of limits

        }

        public void CameraLimitAreaExited(Node body)
        {
            if (!(body is Player) || _playerCamera == null)
            {
                return;
            }

            // TODO:
            // Cancel enter tweens
            // Tween camera limits to stored limits

        }

        public override void _Notification(int what)
        {
            // Make dragging the node around force the limits to redraw, because they're not relative to the node
            if (what == NotificationTransformChanged)
            {
                Update();
            }
        }

        public override void _Draw()
        {            
            if (!Engine.EditorHint && !GetTree().DebugCollisionsHint)
            {
                return;
            }

            var vertMiddle = (LimitTop + LimitBottom) / 2;
            var horizMiddle = (LimitLeft + LimitRight) / 2;

            //Left
            DrawLine(
                new Vector2(LimitLeft, vertMiddle - 10) - GlobalPosition,
                new Vector2(LimitLeft, vertMiddle + 10) - GlobalPosition,
                Colors.GreenYellow, 
                5);

            // Right
            DrawLine(
                new Vector2(LimitRight, vertMiddle - 10) - GlobalPosition,
                new Vector2(LimitRight, vertMiddle + 10) - GlobalPosition,
                Colors.GreenYellow,
                5);

            // Top
            DrawLine(
                new Vector2(horizMiddle - 10, LimitTop) - GlobalPosition,
                new Vector2(horizMiddle + 10, LimitTop) - GlobalPosition,
                Colors.GreenYellow,
                5);

            // Bottom
            DrawLine(
                new Vector2(horizMiddle - 10, LimitBottom) - GlobalPosition,
                new Vector2(horizMiddle + 10, LimitBottom) - GlobalPosition,
                Colors.GreenYellow,
                5);
        }

    }
}


