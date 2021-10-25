using Godot;
using System;
using ThousandYearsHome.Controls.DialogueEngine;

namespace ThousandYearsHome.Areas.Debug
{
    public class DialogueTest : Node2D
    {
        private RichTextLabel _label;
        private LineEdit _inputBox;

        private DialogueEngine _dialogueEngine;

        public override void _Ready()
        {
            _label = GetNode<RichTextLabel>("ColorRect/RichTextLabel");
            _inputBox = GetNode<LineEdit>("LineEdit");
            _label.BbcodeEnabled = true;

            _dialogueEngine = GetNode<DialogueEngine>("DialogueEngine");
        }

        public void AddTextPressed()
        {
            string text = _inputBox.Text;

            _label.AppendBbcode(text);
            _dialogueEngine.QueueText("I am a test.", 0.1f);
            _dialogueEngine.SetState(DialogueEngineState.Outputting);

            _inputBox.Clear();
        }

        public void ClearTextPressed()
        {
            _label.BbcodeText = "";
        }
    }
}

