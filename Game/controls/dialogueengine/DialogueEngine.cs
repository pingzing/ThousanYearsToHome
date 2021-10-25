using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
        private bool _isPayloadAtStart = true;
        private uint _maxLines = 0;
        private int _currentCharacterCount = 0;

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

            _isPayloadAtStart = true;
            Turbo = false;
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
            // TODO: If we're on a Break, listen for the break key.
            base._Input(@event);
        }

        public override void _PhysicsProcess(float delta)
        {
            if (!_characterTickTimer.IsStopped())
            {
                // Currently printing, don't process any payloads.
                return;
            }
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
                        ProcessTextPayload(text);
                        break;
                    case DialogueBreakPayload brk:
                        ProcessBreakPayload(brk);
                        break;
                    default:
                        throw new Exception("Unhandled kind of IDialoguePayload.");
                }
            }
        }

        private void ProcessTextPayload(DialogueTextPayload text)
        {
            if (text.Tag != "" && _isPayloadAtStart)
            {
                EmitSignal(nameof(TagEncountered), text.Tag);
            }

            // Hide all characters before we begin filling up the textbox.
            if (_isPayloadAtStart)
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
                AddToLabel(text.PayloadString, 0);
                _label.VisibleCharacters = -1;
            }
            else
            {
                AddToLabel(text.PayloadString, text.Speed);
                _characterTickTimer.Start(text.Speed); // TODO: Handle speeds faster than per-frame by adding more characters per-frame.
            }
        }

        private void ProcessBreakPayload(DialogueBreakPayload brk)
        {
            if (brk.Tag != null && _isPayloadAtStart)
            {
                EmitSignal(nameof(TagEncountered), brk.Tag);
            }
            _isPayloadAtStart = false;
            if (Turbo)
            {
                // Ignore the break if we're in turbo.
                _buffer.RemoveAt(0);
            }
            else
            {
                EmitSignal(nameof(BreakEntered));
                _isBreak = true;
            }
        }

        public void CharacterTickTimeout()
        {
            _label.VisibleCharacters += 1;
            if (_label.PercentVisible >= 1.0)
            {
                _characterTickTimer.Stop();
            }
        }

        private void AddToLabel(string text, float speed)
        {
            // TODO: Get length of each word (not counting length of BBCode tags)
            // Check to see if it will fit on the line
            // If not, bump it to the next line
            // If we're out of lines, insert a Break, and a text payload with what remains
            // (any remaining spillover can be handled on the next payload process)
            // If we're forced to split in the middle of a BBCode tag, add a closing tag to the
            // end of what we can fit, and an opening tag to the beginning of the new payload we insert after
            // the break.
            // TODO: Do this one segment at a time instead of all at once, then all at once again.
            var textSegments = _parser.SegmentText(text);
            List<DialogueSegment> segmentsToShow = new List<DialogueSegment>();
            for (int i = 0; i < textSegments.Count; i++)
            {
                bool lineAdded = TryAddDialogueSubstring(segmentsToShow, textSegments[i]);
                if (!lineAdded)
                {
                    _currentLineIndex++;
                    if (_currentLineIndex >= _maxLines)
                    {
                        // We're overflowing to the next page. Cut things where needed, and make sure we glue up sliced-in-half BBCode tags.
                        List<DialogueSegment> nextPageSubstrings = segmentsToShow.GetRange(i, segmentsToShow.Count - i);
                        var prevSubstring = segmentsToShow.Last();
                        if (prevSubstring.Tags != null && prevSubstring.Tags.Any())
                        {
                            StringBuilder openingTags = new StringBuilder();
                            StringBuilder closingTags = new StringBuilder();
                            foreach (BBTag tag in prevSubstring.Tags)
                            {
                                openingTags.Insert(0, $"[{tag.FullTagText}]");
                                closingTags.Append($"[/{tag.TagName}]");
                            }
                            nextPageSubstrings.First().Text += openingTags.ToString();
                            prevSubstring.Text += closingTags.ToString();
                        }

                        // Insert a break, and then whatever segments we couldn't fit.
                        QueueBreak(pushFront: true);
                        var overflowText = string.Join("", nextPageSubstrings);
                        QueueText(overflowText, speed, pushFront: true);
                    }
                    else
                    {
                        // If we've moved to a new line, but not beyond the last line, 
                        // glue on a newline, to ensure that the RichTextLabel *actually* breaks where we want it to.
                        segmentsToShow.Last().Text += "\n";

                        lineAdded = TryAddDialogueSubstring(segmentsToShow, textSegments[i]);
                        if (!lineAdded)
                        {
                            // If we've still failed, we somehow have a segment that's longer than an entire line. We have nowhere sane to break it. Just blow up.
                            throw new Exception($"Failed to find space for the text segment '{textSegments[i].Text}', in the overall dialogue text: '{text}'");
                        }
                    }
                }
            }

            foreach (var segment in segmentsToShow)
            {
                _label.AppendBbcode(segment.Text);
            }
        }

        private bool TryAddDialogueSubstring(List<DialogueSegment> toShow, DialogueSegment substring)
        {
            float spaceRemaining = _lineSpaceRemaining[_currentLineIndex];
            if (substring.DisplayWidth > spaceRemaining)
            {
                return false;
            }

            _lineSpaceRemaining[_currentLineIndex] -= substring.DisplayWidth;
            toShow.Add(substring);
            return true;
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


