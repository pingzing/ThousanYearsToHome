using Godot;

namespace ThousandYearsHome.Entities.GlowingBatteryEntity
{
    public class GlowingBattery : Node2D
    {
        private AnimationPlayer _animationPlayer = null!;
        
        public override void _Ready()
        {
            _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        }

        public void StartGlow()
        {
            _animationPlayer.Play("GlowLight");
        }

        public void StopGlow()
        {
            _animationPlayer.Stop(reset: true);
        }
    }
}