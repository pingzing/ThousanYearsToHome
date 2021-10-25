using Godot;
using System;

namespace ThousandYearsHome.Areas.Debug
{
    public class DialogueTest : Node2D
    {
        private RichTextLabel _label;
        private LineEdit _inputBox;

        public override void _Ready()
        {
            _label = GetNode<RichTextLabel>("ColorRect/RichTextLabel");
            _inputBox = GetNode<LineEdit>("LineEdit");
            _label.BbcodeEnabled = true;
        }

        public void AddTextPressed()
        {
            string text = _inputBox.Text;

            int oldLinesVisible = _label.GetVisibleLineCount();
            int oldLines = _label.GetLineCount();

            _label.AppendBbcode(text);

            int newLinesVisible = _label.GetVisibleLineCount();
            int newLines = _label.GetLineCount();

            _inputBox.Clear();

            GD.Print($"OldVisible: {oldLinesVisible}, OldLines: {oldLines}" +
                $"\n NewVisible: {newLinesVisible}, newLines: {newLines}");
        }

        public void ClearTextPressed()
        {
            _label.BbcodeText = "";
        }
    }
}

