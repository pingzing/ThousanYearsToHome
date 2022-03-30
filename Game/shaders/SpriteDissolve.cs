using System;
using Godot;

public class SpriteDissolve : Particles2D
{
    private bool _running = false;
    float _time = 0f;
    private ShaderMaterial _shader = null!;

    public override void _Process(float delta)
    {
        if (_running)
        {
            _time += delta;
            _shader.SetShaderParam("time", _time);
            if (_time > Lifetime * 3)
            {
                _running = false;
                _time = 0;
            }
        }
    }

    public void Initialize(Texture sprite, Vector3? direction = null, float riseSpeed = 20f, float riseSpeedRandomness = 0f, float scale = 1f, float scaleRandomness = 0f, float lifetimeRandomness = 0f)
    {
        if (!(ProcessMaterial is ShaderMaterial shaderMaterial))
        {
            throw new ArgumentException("Somehow, this SpriteDissolve doesn't have a ShaderMaterial.");
        }

        _shader = shaderMaterial;

        if (direction == null)
        {
            direction = new Vector3(0, -1, 0);
        }
                
        // An approximation at best. This will be about enough for textures with lots of tranparency, and will be too much
        // for those with none.
        Amount = (int)(sprite.GetWidth() * sprite.GetHeight() * 2);

        _shader.SetShaderParam("emission_box_extents", new Vector3(sprite.GetWidth() / 2f, sprite.GetHeight() / 2f, 1f));
        _shader.SetShaderParam("sprite", sprite);

        _shader.SetShaderParam("direction", direction);
        _shader.SetShaderParam("rise_speed", riseSpeed);
        _shader.SetShaderParam("rise_speed_randomness", riseSpeedRandomness);
        _shader.SetShaderParam("scale", scale);
        _shader.SetShaderParam("scale_random", scaleRandomness);
        _shader.SetShaderParam("lifetime_randomness", lifetimeRandomness);

        Emitting = true;
        _running = true;
    }
}
