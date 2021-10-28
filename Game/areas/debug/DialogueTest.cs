using Godot;
using ThousandYearsHome.Controls.Dialogue;

namespace ThousandYearsHome.Areas.Debug
{
    public class DialogueTest : Node2D
    {
        private RichTextLabel _label = null!;
        private LineEdit _inputBox = null!;

        private DialogueEngine _dialogueEngine = null!;

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
            _dialogueEngine.QueueText(_inputBox.Text, 0.02f);
            _dialogueEngine.SetState(DialogueEngineState.Outputting);

            _inputBox.Clear();
        }

        public void ClearTextPressed()
        {
            _label.BbcodeText = "";
            _dialogueEngine.ClearText();
        }
    }
}

