using Godot;

namespace ThousandYearsHome.Controls.WarmthBarControl
{
    public class WarmthBar : Control
    {
        private Tween _tween = null!;
        private ProgressBar _fill = null!;

        private float _currentPercent = 100;
        public float CurrentPercent
        {
            get => _currentPercent;
            set
            {
                float oldPercent = _currentPercent;
                _currentPercent = Mathf.Clamp(value, 0, 100);
                AnimateProgressBar(oldPercent, _currentPercent);
            }
        }

        public override void _Ready()
        {
            _tween = GetNode<Tween>("Tween");
            _fill = GetNode<ProgressBar>("VBoxContainer/BarBorder/BarMarginContainer/Fill");
        }

        private void AnimateProgressBar(float previousPercent, float currentPercent)
        {
            _fill.Value = previousPercent;
            _tween.StopAll();

            _tween.InterpolateProperty(_fill, "value", null, currentPercent, 1f);
            _tween.Start();
        }
    }
}

