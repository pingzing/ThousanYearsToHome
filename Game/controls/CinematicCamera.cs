using Godot;
using System;
using ThousandYearsHome.Extensions;

namespace ThousandYearsHome.Controls
{
    public class CinematicCamera : Camera2D
    {

        private float _duration = 0f;
        private float _periodMs = 0f;
        private float _amplitude = 0f;
        private float _timeRemaining = 0f;
        private float _lastShookTimer = 0f;
        private float _previousX = 0f;
        private float _previousY = 0f;
        private Vector2 _lastOffset = Vector2.Zero;


        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            SetProcess(true);
        }

        public override void _Process(float delta)
        {
            if (_timeRemaining == 0)
            {
                return;
            }

            // Only shake on certain frames
            _lastShookTimer = _lastShookTimer + delta;

            while (_lastShookTimer >= _periodMs)
            {
                _lastShookTimer = _lastShookTimer -_periodMs;

                // Lerp between amplitude and 0.0 intensity based on remaining time
                float intensity = _amplitude * (1 - ((_duration - _timeRemaining) / _duration));

                // Add a bit of noise (courtesy of http://jonny.morrill.me/blog/view/14 )
                float newX = Numerology.RandRange(-1f, 1f);
                float xComponent = intensity * (_previousX + (delta * (newX - _previousX)));

                float newY = Numerology.RandRange(-1f, 1f);
                float yComponent = intensity * (_previousY + (delta * (newY - _previousY)));
                _previousX = newX;
                _previousY = newY;

                // Track how much offset has moved
                var newOffset = new Vector2(xComponent, yComponent);
                Offset = Offset - _lastOffset + newOffset;
                _lastOffset = newOffset;
            }

            _timeRemaining -= delta;
            if (_timeRemaining <= 0)
            {
                _timeRemaining = 0;
                Offset -= _lastOffset;
            }
        }

        public void Shake(float durationSeconds, float frequencyHz, float amplitude)
        {
            _duration = durationSeconds;
            _timeRemaining = durationSeconds;
            _periodMs = 1f / frequencyHz;
            _amplitude = amplitude;
            _previousX = Numerology.RandRange(-1f, 1f);
            _previousY = Numerology.RandRange(-1f, 1f);

            // Reset previous offset, if any
            Offset = Offset - _lastOffset;
            _lastOffset = Vector2.Zero;
        }
    }
}

