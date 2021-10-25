using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThousandYearsHome.controls.dialogueengine;

namespace ThousandYearsHome.Controls.DialogueEngine
{
    public class DialogueEngine : ReferenceRect
    {
        // Members

        private static Regex _bbCodeRegex = new Regex(@"\[*.\b[^][]*\]", RegexOptions.Compiled);
        private static Regex _bbCodeOpen = new Regex(@"\[([^\/]*?)\b\]", RegexOptions.Compiled);
        private static Regex _bbCodeClose = new Regex(@"\[\/(.*?)\b\]", RegexOptions.Compiled);

        private List<IDialoguePayload> _buffer = new List<IDialoguePayload>();
        private DialogueEngineState _currState = DialogueEngineState.Waiting;
        private bool _isBreak = false;
        private bool _isMaxLinesReached = false;
        private bool _isBuffAtStart = true;
        private uint _maxLines = 0;

        private int _currentLineIndex = 0;
        private float[] _lineSpaceRemaining = null!;
        private DialogueParser _parser = null!;

        // Exported properties

        /// <summary>
        /// If the text output pauses waiting for the user when reaching the maximum number of lines.
        /// </summary>
        [Export] public bool BreakOnMaxLines = true;

        /// <summary>
        /// The font to use in the backing RichTextLabel.
        /// </summary>
        [Export] public Font? Font = null;

        /// <summary>
        /// Whether or not to print input to console.
        /// </summary>
        [Export] public bool PrintInput = true;

        // Signals

        /// <summary>
        /// Fired when the buffer has run out of things to output.
        /// </summary>
        [Signal] public delegate void BufferEmptied();

        /// <summary>
        /// When the engine's state changes.
        /// </summary>
        /// <param name="state">The new state.</param>
        [Signal] public delegate void StateChanged(DialogueEngineState state);

        /// <summary>
        /// Fired when the engine encounters a Break.
        /// </summary>
        [Signal] public delegate void BreakEntered();

        /// <summary>
        /// Fired when the engine resumes after a break.
        /// </summary>
        [Signal] public delegate void BreakExited();

        /// <summary>
        /// Fired when the buffer encounters a pyaload with a tag.
        /// </summary>
        /// <param name="tag">The tag that was encountered.</param>
        [Signal] public delegate void TagEncountered(string tag);

        /// <summary>
        /// Fired when the buffer is cleared of all text.
        /// </summary>
        [Signal] public delegate void BufferCleared();

        // Nodes
        private RichTextLabel _label = null!;
        private Timer _characterTickTimer = null!;

        public override void _Ready()
        {
            SetPhysicsProcess(true);
            SetProcessInput(true);
            _label = GetNode<RichTextLabel>("Label");
            _characterTickTimer = GetNode<Timer>("CharacterTickTimer");

            if (Font != null)
            {
                _label.AddFontOverride("normal_font", Font);
            }
            else
            {
                Font = _label.GetFont("normal_font");
            }

            float fontHeight = Font.GetHeight();
            _maxLines = (uint)Mathf.Floor(RectSize.y / fontHeight + _label.GetConstant("line_separation"));
            _lineSpaceRemaining = new float[_maxLines];
            _label.RectSize = RectSize;
            for (int i = 0; i < _lineSpaceRemaining.Length; i++)
            {
                _lineSpaceRemaining[i] = _label.RectSize.x;
            }
            _label.BbcodeEnabled = true;
            _parser = new DialogueParser(Font);
        }

        // API-like methods

        /// <summary>
        /// Enqueues text to add to the dialogue box. Supports BBCode.
        /// </summary>
        /// <param name="text">The text to add.</param>
        /// <param name="speed">Number of seconds to take to print each character.</param>        
        public void QueueText(string text, float speed = 0f, string tag = "", bool pushFront = false)
        {
            var payload = new DialogueTextPayload
            {
                PayloadString = text,
                Speed = speed,
                Tag = tag
            };
            if (pushFront)
            {
                _buffer.Insert(0, payload);
            }
            else
            {
                _buffer.Add(payload);
            }
        }

        /// <summary>
        /// Enqueues a "pause output for <paramref name="length"/> seconds" instruction.
        /// </summary>
        /// <param name="length">Number of seconds to pause.</param>
        public void QueueSilence(float length, string tag = "", bool pushFront = false)
        {
            var payload = new DialogueSilencePayload
            {
                Length = length,
                Tag = tag
            };
            if (pushFront)
            {
                _buffer.Insert(0, payload);
            }
            else
            {
                _buffer.Add(payload);
            }
        }

        /// <summary>
        /// Enqueues a "stop output until the player hits the "next" key" instruction.
        /// </summary>
        public void QueueBreak(string tag = "", bool pushFront = false)
        {
            var payload = new DialogueBreakPayload
            {
                Tag = tag
            };
            if (pushFront)
            {
                _buffer.Insert(0, payload);
            }
            else
            {
                _buffer.Add(payload);
            }
        }

        /// <summary>
        /// Enqueues a "begin taking input from player" instruction.
        /// </summary>
        public void QueueInput(string tag = "", bool pushFront = false)
        {
            var payload = new DialogueInputPayload
            {
                Tag = tag
            };
            if (pushFront)
            {
                _buffer.Insert(0, payload);
            }
            else
            {
                _buffer.Add(payload);
            }
        }

        /// <summary>
        /// Enqueues a "clear all text" instruction.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="pushFront"></param>
        public void QueueClear(string tag = "", bool pushFront = false)
        {
            var payload = new DialogueClearPayload
            {
                Tag = tag
            };
            if (pushFront)
            {
                _buffer.Insert(0, payload);
            }
            else
            {
                _buffer.Add(payload);
            }
        }

        /// <summary>
        /// Deletes <em>ALL</em> text on the label.
        /// </summary>
        public void ClearText()
        {
            _label.BbcodeText = "";
            _label.VisibleCharacters = -1;
        }

        /// <summary>
        /// Clears all pending payloads from the internal buffer.
        /// </summary>
        public void ClearBuffer()
        {
            _isBreak = false;
            SetState(DialogueEngineState.Waiting);
            _buffer.Clear();

            for (int i = 0; i < _lineSpaceRemaining.Length; i++)
            {
                _lineSpaceRemaining[i] = _label.RectSize.x;
            }

            _isBuffAtStart = true;
            Turbo = false;
            _isMaxLinesReached = false;
        }

        public void Reset()
        {
            ClearText();
            ClearBuffer();
        }

        /// <summary>
        /// Returns the (BBCode) text the label is currently displaying.
        /// </summary>
        public string Text => _label.BbcodeText;

        /// <summary>
        /// Enable or disable turbo mode (i.e. all text gets printed instantly).
        /// </summary>
        /// <param name="enabled"></param>
        public bool Turbo { get; set; } = false;

        public void SetState(DialogueEngineState newState)
        {
            EmitSignal(nameof(StateChanged), newState);
            _currState = newState;
        }

        public KeyList BreakKey { get; set; } = KeyList.Z;

        // Internal methods

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
        }

        // Note: RichTextBox has autowrap always on.
        // GetVisibleLines() returns the number of lines actually displayed to the user, but not WHERE the lines were broken.
        public override void _PhysicsProcess(float delta)
        {
            if (_currState == DialogueEngineState.Outputting)
            {
                if (_buffer.Count == 0)
                {
                    _currState = DialogueEngineState.Waiting;
                    EmitSignal(nameof(BufferEmptied));
                    return;
                }

                IDialoguePayload payload = _buffer[0];

                switch (payload)
                {
                    case DialogueTextPayload text:
                        HandleTextPayload(text);

                        break;
                    default:
                        throw new Exception("Unhandled kind of IDialoguePayload.");
                }
            }
        }

        private void HandleTextPayload(DialogueTextPayload text)
        {
            if (text.Tag != "" && _isBuffAtStart)
            {
                EmitSignal(nameof(TagEncountered), text.Tag);
            }

            // Hide all characters before we begin filling up the textbox.
            if (_isBuffAtStart)
            {
                _label.VisibleCharacters = 0;
            }

            if (Turbo)
            {
                text.Speed = 0;
            }

            // If speed is 0, display everything all at once
            if (text.Speed == 0)
            {
                AddToLabel(text.PayloadString);
                _label.VisibleCharacters = -1;
            }
            else
            {

            }
        }

        public void CharacterTickTimeout()
        {

        }

        private void AddToLabel(string text)
        {
            float spaceRemaining = _lineSpaceRemaining[_currentLineIndex];
            // TODO: Get length of each word (not counting length of BBCode tags)
            // Check to see if it will fit on the line
            // If not, bump it to the next line
            // If we're out of lines, insert a Break, and a text payload with what remains
            // (any remaining spillover can be handled on the next payload process)
            // If we're forced to split in the middle of a BBCode tag, add a closing tag to the
            // end of what we can fit, and an opening tag to the beginning of the new payload we insert after
            // the break.
            var textSegments = _parser.GetStringWidths(text);
            foreach (DialogueSubstring segment in textSegments)
            {
                if (spaceRemaining < segment.DisplayWidth)
                {
                    _currentLineIndex++;
                }

                if (_currentLineIndex > _maxLines)
                {
                    // TODO:
                    // 1) Glue closing tags for the last segment's tags to the end of it
                    // 2) Insert a break at the front of the _buffer
                    // 3) Insert a text payload after the new break that contains a) opening tags for all the closing tags we just inserted, and b) whatever text remains.
                }
            }

            _label.AppendBbcode(text);
        }
    }

    public interface IDialoguePayload
    {
        public string Tag { get; set; }
    }

    public class DialogueTextPayload : IDialoguePayload
    {
        public string Tag { get; set; } = null!;
        public string PayloadString { get; set; } = null!;
        public float Speed { get; set; }
    }

    public class DialogueSilencePayload : IDialoguePayload
    {
        public string Tag { get; set; } = null!;
        public float Length { get; set; }
    }

    public class DialogueBreakPayload : IDialoguePayload
    {
        public string Tag { get; set; } = null!;
    }

    public class DialogueInputPayload : IDialoguePayload
    {
        public string Tag { get; set; } = null!;
    }

    public class DialogueClearPayload : IDialoguePayload
    {
        public string Tag { get; set; } = null!;
    }

    public enum DialogueEngineState
    {
        Waiting = 1,
        Outputting = 2,
    }
}


