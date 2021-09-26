using Godot;
using System;
using ThousandYearsHome.Entities;

namespace ThousandYearsHome.Areas
{
    public class StartBlizzard : Node
    {
        private Node _sceneRoot = null!;
        private Player _player = null!;
        private AnimationPlayer _animator = null!;
        private AnimationPlayer _fadeAnimator = null!;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _sceneRoot = GetTree().Root.GetNode<Node>("StartBlizzard");
            var startPos = GetNode<Position2D>("SceneCanvas/StartPosition").Position;
            _player = GetNode<Player>("SceneCanvas/Player");
            _player.Spawn(startPos);

            // Lock player's input, because we're gonna animate them cutscene style
            _player.InputLocked = true;
            _animator = GetNode<AnimationPlayer>("SceneCanvas/AnimationPlayer");
            _fadeAnimator = GetNode<AnimationPlayer>("SceneCanvas/FadePlayer");
        }

        public void WaitStartTimerTimeout()
        {
            _animator.Play("StaggerForward");
        }

        public void OnAnimationStarted(string name)
        {
            if (name == "PassOut")
            {
                _fadeAnimator.Play("FadeScene");
            }
        }

        public void OnAnimationFinished(string name)
        {
            if (name == "StaggerForward")
            {
                _animator.Play("Shiver");
            }
            if (name == "Shiver")
            {
                _animator.Play("PassOut");
            }
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(float delta)
        {

        }
    }
}


