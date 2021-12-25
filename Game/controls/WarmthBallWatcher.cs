using Godot;
using System;
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
                var cameraRectLoc = -_viewport.CanvasTransform.origin + (_playerCamera.CurrentRect.End);
                float angleRad = cameraCenter.AngleToPoint(ballPos.Value) - Mathf.Pi;
                Vector2? intersection = GetScreenEdgeIntersection(cameraRectLoc, ballPos.Value);
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

        public void BallEnteredScreen(ulong ballId)
        {
            // Update its entry in the list so we stop drawing its arrow
        }

        public void BallExitedScreen(ulong ballId)
        {
            // Update its entry in the list so we resume drawing its arrow
        }

        private Vector2? GetScreenEdgeIntersection(Vector2 cameraPos, Vector2 ballPos)
        {
            var vpRect = _viewport.GetVisibleRect();
            var vpWorldCoords = -_viewport.CanvasTransform.origin;

            // Find the four corners of the viewport rect in world space
            Vector2 tl = new Vector2(vpWorldCoords);
            Vector2 tr = new Vector2(vpWorldCoords.x + vpRect.Size.x, vpWorldCoords.y);
            Vector2 bl = new Vector2(vpWorldCoords.x, vpWorldCoords.y + vpRect.Size.y);
            Vector2 br = new Vector2(vpWorldCoords + vpRect.Size);

            object intersection;
            intersection = Geometry.SegmentIntersectsSegment2d(cameraPos, ballPos, tr, br);
            if (intersection == null)
            {
                intersection = Geometry.SegmentIntersectsSegment2d(cameraPos, ballPos, tl, tr);
            }
            if (intersection == null)
            {
                intersection = Geometry.SegmentIntersectsSegment2d(cameraPos, ballPos, tl, bl);
            }
            if (intersection == null)
            {
                intersection = Geometry.SegmentIntersectsSegment2d(cameraPos, ballPos, bl, br);
            }

            if (intersection == null)
            {
                return null;
            }

            // Convert from world space to screen space
            var intersectionVec = (Vector2)intersection;
            var vec = _viewport.CanvasTransform * (intersectionVec);
            return vec;
        }
    }
}