using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ThousandYearsHome.Controls.DialogueEngine
{
    /// <summary>
    /// Class in charge of parsing Dialogue with game directives stripped from it,
    /// but still containing BBCode.
    /// </summary>
    public class DialogueParser
    {
        private readonly Font _dialogueBoxFont;
        private Stack<BBTag> _tagStack = new Stack<BBTag>();

        public DialogueParser(Font dialogueBoxFont)
        {
            _dialogueBoxFont = dialogueBoxFont;
        }

        private bool _insideOpeningTag = false;
        private bool _insideClosingTag = false;
        private StringBuilder _textBuilder = new StringBuilder();
        private StringBuilder _tagBuilder = new StringBuilder();
        // TODO: This is mangling BBCode tags, somehow. [center] is one particular culprit.
        public IEnumerable<DialogueSegment> SegmentText(string text)
        {
            string? previousChar = null;
            int startIndex = -1;

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
                        yield return CreateSegment(_textBuilder, _tagStack, startIndex);
                        startIndex = -1;
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
                            TagName = tagText.Split(' ')[0]
                        };
                        _tagStack.Push(tag);
                        _insideOpeningTag = false;
                    }
                    if (_insideClosingTag)
                    {
                        _tagStack.Pop();
                        _insideClosingTag = false;
                    }

                    previousChar = curr;
                    continue;
                }

                if (_insideClosingTag || _insideClosingTag)
                {
                    _tagBuilder.Append(curr);
                    previousChar = curr;
                    continue;
                }

                // If the last char was a space or a newline, yield return what we've built up so far, because we've built a full word.
                if (previousChar == " " || previousChar == "\n")
                {
                    yield return CreateSegment(_textBuilder, _tagStack, startIndex);
                    startIndex = -1;
                    _textBuilder.Clear();
                }
                if (startIndex == -1)
                {
                    startIndex = stringEnumerator.ElementIndex;
                }
                _textBuilder.Append(curr);
                previousChar = curr;
            }

            // Yield return one last time for anything outside of BBCode at the end.
            if (_textBuilder.Length > 0)
            {
                yield return CreateSegment(_textBuilder, _tagStack, startIndex);
            }

            _tagBuilder.Clear();
            _textBuilder.Clear();
            _tagStack.Clear();
        }

        private DialogueSegment CreateSegment(StringBuilder textBuilder, Stack<BBTag> tags, int startIndex)
        {
            string text = textBuilder.ToString();
            return new DialogueSegment
            {
                Text = text,
                Tags = tags.Count > 0 ? tags.ToList() : null,
                DisplayWidth = _dialogueBoxFont.GetStringSize(text).x,
                StartIndex = startIndex
            };
        }
    }

    public class DialogueSegment
    {
        public int StartIndex { get; set; }
        public float DisplayWidth { get; set; }
        public string Text { get; set; } = null!;

        /// <summary>
        /// The list of tags that apply to this substring, in order from innermost to outermost.
        /// </summary>
        public List<BBTag>? Tags { get; set; }
    }

    public class BBTag
    {
        public string TagName { get; set; } = null!;
        public string FullTagText { get; set; } = null!;
    }
}
