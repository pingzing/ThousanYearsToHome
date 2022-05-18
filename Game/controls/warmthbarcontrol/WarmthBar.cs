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
        private float _overwarmthThreshold = Player.OverWarmthTheshold; // Semi-hardcoded for now
        private StyleBoxFlat? _progressBorderStyleBox = null;
        
        [Export(PropertyHint.ColorNoAlpha)]
        public Color OverchargeColor
        {
            get => _progressBorder.SelfModulate;
            set => _progressBorder.SelfModulate = value;
        }        

        public void UpdateWarmth(float oldWarmth, float newWarmth)
        {
            float warmth = Mathf.Clamp(newWarmth, 0, _overwarmthThreshold);
            _fill.Value = warmth;
        }

        public void UpdateExcessWarmth(float oldExcess, float newExcess)
        {
            float excess = Mathf.Clamp(newExcess, 0, 100f); // Hard-coded to 100 for now here and in Player.cs

            // Lazy way to handle the edge case where we're near max Excess warmth,
            // and want to show the right edge drawing overtop the right side of the bar underneath.
            if (_progressBorderStyleBox != null)
            {
                if (excess >= _overwarmthThreshold)
                {
                    _progressBorderStyleBox.BorderWidthRight = 2;
                }
                else
                {
                    _progressBorderStyleBox.BorderWidthRight = 0;
                }
            }
            _progressBorder.Value = excess;
        }

        public override void _Ready()
        {
            _tween = GetNode<Tween>("Tween");
            _fill = GetNode<ProgressBar>("VBoxContainer/BarContainer/ProgressBorder/BarMarginContainer/Fill");
            _progressBorder = GetNode<ProgressBar>("VBoxContainer/BarContainer/ProgressBorder");
            _progressBorderStyleBox = _progressBorder.Get("custom_styles/fg") as StyleBoxFlat;
        }
    }
}

