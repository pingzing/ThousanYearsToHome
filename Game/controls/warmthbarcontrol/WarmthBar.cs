using Godot;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome.Controls.WarmthBarControl
{
    public class WarmthBar : Control
    {
        // Local nodes
        private Tween _tween = null!;
        private ProgressBar _fill = null!;
        private ProgressBar _progressBorder = null!;
        private float _overwarmthThreshold;
        
        [Export(PropertyHint.ColorNoAlpha)]
        public Color OverchargeColor
        {
            get => _progressBorder.SelfModulate;
            set => _progressBorder.SelfModulate = value;
        }        

        public void UpdateWarmth(float oldWarmth, float newWarmth)
        {
            // Update overwarmth
            float overWarmth = newWarmth - _overwarmthThreshold;
            overWarmth = Mathf.Clamp(overWarmth, 0, 25);
            _progressBorder.Value = overWarmth;

            // Update regular warmth
            float warmth = newWarmth - overWarmth;
            _fill.Value = warmth;
        }

        public override void _Ready()
        {
            _overwarmthThreshold = Player.OverWarmthTheshold;
            _tween = GetNode<Tween>("Tween");
            _fill = GetNode<ProgressBar>("VBoxContainer/BarContainer/ProgressBorder/BarMarginContainer/Fill");
            _progressBorder = GetNode<ProgressBar>("VBoxContainer/BarContainer/ProgressBorder");            
        }
    }
}

