using Godot;
using System.Collections.Generic;
using ThousandYearsHome.Entities.PlayerEntity;
using ThousandYearsHome.Entities.WarmthBallEntity;

namespace ThousandYearsHome.Entities
{
    public class WarmthBallWatcher : Control
    {
        private Dictionary<ulong, Vector2> _trackedBalls = new Dictionary<ulong, Vector2>();
        private Godot.Collections.Dictionary<ulong, Sprite> _ballArrows = new Godot.Collections.Dictionary<ulong, Sprite>();

        private Viewport _viewport = null!;
        private PackedScene _arrowScene = null!;
        private PlayerCamera _playerCamera = null!;

        public override void _EnterTree()
        {
            AddToGroup(WarmthBall.WarmthBallGroup);
        }

        public override void _ExitTree()
        {
            RemoveFromGroup(WarmthBall.WarmthBallGroup);
        }

        public void Init(PlayerCamera playerCamera)
        {
            _playerCamera = playerCamera;
        }

        public override void _Ready()
        {
            _arrowScene = GD.Load<PackedScene>("res://controls/WarmthBallWatcherArrow.tscn");
            _viewport = GetViewport();
        }

        public override void _Process(float delta)
        {
            foreach (var ballPos in _trackedBalls)
            {
                if (!_ballArrows.TryGetValue(ballPos.Key, out Sprite arrow))
                {
                    continue;
                }

                var viewportRect = GetViewportRect();
                var cameraCenter = -_viewport.CanvasTransform.origin + (viewportRect.Size / 2);
                float angleRad = cameraCenter.AngleToPoint(ballPos.Value) - Mathf.Pi;

                float viewportTop = -_viewport.CanvasTransform.origin.y;
                float viewportBottom = -_viewport.CanvasTransform.origin.y + _viewport.GetVisibleRect().Size.y;
                
                // Don't just a draw a line from the player directly to the ball--otherwise, if it's too far away,
                // the player doesn't get an accurate idea of how high up the ball is. Instead, draw a line directly
                // horizontally that intersects the camera viewport rect.
                float referenceY = Mathf.Clamp(ballPos.Value.y, viewportTop + 1, viewportBottom - 1);

                Vector2? intersection = GetScreenEdgeIntersection(new Vector2(cameraCenter.x, referenceY), ballPos.Value);
                if (intersection == null)
                {
                    arrow.Visible = false;
                    continue;
                }

                arrow.Rotation = angleRad;
                arrow.Position = intersection.Value;
                arrow.Visible = true;
            }
        }

        // ---- Signal handlers -----

        public void BallPositionUpdated(ulong ballId, Vector2 position)
        {
            _trackedBalls[ballId] = position;
            if (!_ballArrows.ContainsKey(ballId))
            {
                Sprite sprite = _arrowScene.Instance<Sprite>();
                sprite.Visible = false; // Hide it initially, so we don't get a frame of the arrow being somewhere silly
                AddChild(sprite);
                _ballArrows[ballId] = sprite;
            }
        }

        public void BallDeactivated(ulong ballId)
        {
            _trackedBalls.Remove(ballId);
            if (_ballArrows.ContainsKey(ballId))
            {
                _ballArrows[ballId].QueueFree();
                _ballArrows.Remove(ballId);
            }
        }
        
        // --- Utilities ---

        private Vector2? GetScreenEdgeIntersection(Vector2 visiblePos, Vector2 offscreenPos)
        {
            var vpRect = _viewport.GetVisibleRect();
            var vpWorldCoords = -_viewport.CanvasTransform.origin;

            // Find the four corners of the viewport rect in world space
            Vector2 tl = new Vector2(vpWorldCoords);
            Vector2 tr = new Vector2(vpWorldCoords.x + vpRect.Size.x, vpWorldCoords.y);
            Vector2 bl = new Vector2(vpWorldCoords.x, vpWorldCoords.y + vpRect.Size.y);
            Vector2 br = new Vector2(vpWorldCoords + vpRect.Size);

            object intersection;
            intersection = Geometry.SegmentIntersectsSegment2d(visiblePos, offscreenPos, tr, br);
            if (intersection == null)
            {
                intersection = Geometry.SegmentIntersectsSegment2d(visiblePos, offscreenPos, tl, tr);
            }
            if (intersection == null)
            {
                intersection = Geometry.SegmentIntersectsSegment2d(visiblePos, offscreenPos, tl, bl);
            }
            if (intersection == null)
            {
                intersection = Geometry.SegmentIntersectsSegment2d(visiblePos, offscreenPos, bl, br);
            }

            if (intersection == null)
            {
                return null;
            }

            // Convert from world space to screen space            
            var vec = _viewport.CanvasTransform * (Vector2)intersection;
            return vec;
        }
    }
}