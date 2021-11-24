using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ThousandYearsHome.Controls.Dialogue
{
    public class DialogueSegmenter
    {
        private static char[] _openingTagArgSepartors = new[] { ' ', '=' };
        private readonly Font _dialogueBoxFont;
        private Stack<BBTag> _tagStack = new Stack<BBTag>();

        private List<string> _unassignedOpeningTags = new List<string>();
        private List<string> _unassignedClosingTags = new List<string>();

        public DialogueSegmenter(Font dialogueBoxFont)
        {
            _dialogueBoxFont = dialogueBoxFont;
        }

        private bool _insideOpeningTag = false;
        private bool _insideClosingTag = false;
        private StringBuilder _textBuilder = new StringBuilder();
        private StringBuilder _tagBuilder = new StringBuilder();
        public IEnumerable<DialogueSegment> SegmentText(string text)
        {
            // Clear out possible stale values form previous runs.
            _tagStack.Clear();
            _unassignedClosingTags.Clear();
            _unassignedClosingTags.Clear();
            _insideOpeningTag = false;
            _insideClosingTag = false;
            _textBuilder.Clear();
            _tagBuilder.Clear();

            string? previousChar = null;
            int textStartIndex = -1;

            // Iterate through the string, saving each chunk of non-tag text into a separate DialogueSegment.
            var stringEnumerator = StringInfo.GetTextElementEnumerator(text);
            while (stringEnumerator.MoveNext())
            {
                string curr = stringEnumerator.GetTextElement();
                if (curr == "[")
                {
                    // This could be either an opening tag or a closing tag, we don't know yet.

                    // Save in-progress string, if any
                    if (_textBuilder.Length > 0)
                    {
                        yield return CreateSegment(_textBuilder, _tagStack, textStartIndex, _unassignedOpeningTags, _unassignedClosingTags);
                        textStartIndex = -1;
                        _textBuilder.Clear();
                    }

                    previousChar = curr;
                    continue;
                }

                if (previousChar == "[")
                {
                    // We're currently in *some* kind of tag, let's find out which kind:
                    if (curr == "/")
                    {
                        _insideClosingTag = true;
                    }
                    else
                    {
                        _insideOpeningTag = true;
                        _tagBuilder.Append(curr);
                    }

                    previousChar = curr;
                    continue;
                }

                if (curr == "]")
                {
                    if (_insideOpeningTag)
                    {
                        string tagText = _tagBuilder.ToString();
                        BBTag tag = new BBTag
                        {
                            FullTagText = tagText,
                            TagName = tagText.Split(_openingTagArgSepartors)[0],
                        };

                        _tagBuilder.Clear();
                        _tagStack.Push(tag);
                        _unassignedOpeningTags.Add($"[{tagText}]");
                        _insideOpeningTag = false;
                    }
                    if (_insideClosingTag)
                    {
                        Debug.Assert(_tagStack.Count > 0, "ERROR: About to try to pop an empty tag stack in DialogueParser. Probably the result of an unbalanced tag!");
                        _tagBuilder.Clear();
                        BBTag poppedTag = _tagStack.Pop();
                        _unassignedClosingTags.Add($"[/{poppedTag.TagName}]");
                        _insideClosingTag = false;
                    }

                    previousChar = curr;
                    continue;
                }

                if (_insideOpeningTag || _insideClosingTag)
                {
                    _tagBuilder.Append(curr);
                    previousChar = curr;
                    continue;
                }

                // If the last char was a space or a newline, yield return what we've built up so far, because we've built a full word.
                if (previousChar == " " || previousChar == "\n")
                {
                    yield return CreateSegment(_textBuilder, _tagStack, textStartIndex, _unassignedOpeningTags, _unassignedClosingTags);
                    textStartIndex = -1;
                    _textBuilder.Clear();
                }
                if (textStartIndex == -1)
                {
                    textStartIndex = stringEnumerator.ElementIndex;
                }
                _textBuilder.Append(curr);
                previousChar = curr;
            }

            // Yield return one last time for anything outside of BBCode at the end.
            if (_textBuilder.Length > 0)
            {
                yield return CreateSegment(_textBuilder, _tagStack, textStartIndex, _unassignedOpeningTags, _unassignedClosingTags);
            }

            // If we have any remaining unclosed tags, return a special segment with all of them, and a width of zero.
            if (_unassignedClosingTags.Count > 0)
            {
                yield return new DialogueSegment
                {
                    DisplayWidth = 0,
                    StartIndex = -1,
                    Tags = null,
                    Text = string.Join("", _unassignedClosingTags),
                };
                _unassignedClosingTags.Clear();
            }
        }

        private DialogueSegment CreateSegment(StringBuilder textBuilder, Stack<BBTag> tags, int startIndex, List<string> openingTags, List<string> closingTags)
        {
            string text = textBuilder.ToString();
            float displayWidth = _dialogueBoxFont.GetStringSize(text).x;

            text = string.Concat(
                string.Join("", closingTags), // Any closing tags from the PREVIOUS segment
                string.Join("", openingTags),
                text
            );
            openingTags.Clear();
            closingTags.Clear();

            return new DialogueSegment
            {
                Text = text,
                Tags = tags.Count > 0 ? tags.ToList() : null,
                DisplayWidth = displayWidth,
                StartIndex = startIndex
            };
        }
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class DialogueSegment
    {
        public int StartIndex { get; set; }
        public float DisplayWidth { get; set; }
        public string Text { get; set; } = null!;

        /// <summary>
        /// The list of tags that apply to this substring, in order from innermost to outermost.
        /// </summary>
        public List<BBTag>? Tags { get; set; }

        private string DebuggerDisplay
        {
            get
            {
                string? tagString = Tags != null ? string.Join(", ", Tags?.Select(x => $"[{x.TagName}]")) : null;
                return $"{Text}, Index: {StartIndex}, Width: {DisplayWidth}, Tags: {tagString}";
            }
        }
    }

    [DebuggerDisplay("[{TagName}], FullText: [{FullTagText}]")]
    public class BBTag
    {
        public string TagName { get; set; } = null!;
        public string FullTagText { get; set; } = null!;
    }
}
