/***
*
*	Copyright (c) 1996-2001, Valve LLC. All rights reserved.
*	
*	This product contains software technology licensed from Id 
*	Software, Inc. ("Id Technology").  Id Technology (c) 1996 Id Software, Inc. 
*	All Rights Reserved.
*
*   This source code contains proprietary and confidential information of
*   Valve LLC and its suppliers.  Access to this code is restricted to
*   persons who have executed a written SDK license with Valve.  Any access,
*   use or distribution of this code by or to any unlicensed person is illegal.
*
****/

using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpLife.Utility.Text
{
    /// <summary>
    /// Tokenizes a string
    /// </summary>
    public sealed class Tokenizer
    {
        public sealed class CommentDefinition
        {
            public readonly string StartingDelimiter;

            public readonly string EndingDelimiter;

            /// <summary>
            /// Invoked when this comment is encountered, passing this CommentDefinition and the comment string excluding delimiters
            /// </summary>
            public readonly Action<CommentDefinition, string> Callback;

            /// <summary>
            /// Creates a new comment definition
            /// </summary>
            /// <param name="startingDelimiter">The starting delimiter of the comment. Must contain valid characters</param>
            /// <param name="endingDelimiter">The ending delimiter of the comment. Must contain valid characters. If null, the comment ends after the newline</param>
            /// <param name="callback">Optional callback to invoke when this comment is encountered</param>
            public CommentDefinition(string startingDelimiter, string endingDelimiter = null, Action<CommentDefinition, string> callback = null)
            {
                if (startingDelimiter == null)
                {
                    throw new ArgumentNullException(nameof(startingDelimiter));
                }

                if (string.IsNullOrWhiteSpace(startingDelimiter))
                {
                    throw new ArgumentException("Starting delimiter must be valid and contain valid characters", nameof(startingDelimiter));
                }

                if (endingDelimiter != null && string.IsNullOrWhiteSpace(endingDelimiter))
                {
                    throw new ArgumentException("Ending delimiter must be valid and contain valid characters", nameof(endingDelimiter));
                }

                StartingDelimiter = startingDelimiter;

                //The end is either as specified or the end of the line
                EndingDelimiter = endingDelimiter ?? StringUtils.NewlineFormat.Unix;

                Callback = callback;
            }
        }

        /// <summary>
        /// Strings to treat as their own tokens
        /// </summary>
        public static readonly IReadOnlyList<string> DefaultWords =
        new[]{
            "{",
            "}",
            "(",
            ")",
            "\'",
            ","
        };

        public static readonly IEnumerable<CommentDefinition> NoCommentDefinitions = Enumerable.Empty<CommentDefinition>();

        public static readonly CommentDefinition CStyleComment = new CommentDefinition("//");

        public static readonly CommentDefinition CPPStyleComment = new CommentDefinition("/*", "*/");

        private readonly string _data;

        private IEnumerable<string> _words = DefaultWords;

        public IEnumerable<string> Words
        {
            get => _words;

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value.Any(string.IsNullOrEmpty))
                {
                    throw new ArgumentException("A word must be non-null and contain at least one character", nameof(value));
                }

                _words = value;
            }
        }

        private IEnumerable<CommentDefinition> _commentDefinitions = NoCommentDefinitions;

        public IEnumerable<CommentDefinition> CommentDefinitions
        {
            get => _commentDefinitions;
            set => _commentDefinitions = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// If true, newlines will be left as separate tokens
        /// </summary>
        public bool LeaveNewLines { get; set; }

        public string Token { get; private set; } = string.Empty;

        public int Index { get; private set; }

        /// <summary>
        /// Whether there is data left to be read
        /// </summary>
        public bool HasNext => Index < _data.Length;

        /// <summary>
        /// Returns true if additional data is waiting to be processed on this line
        /// </summary>
        public bool TokenWaiting
        {
            get
            {
                var index = Index;

                while (index < Token.Length && StringUtils.NewlineFormat.Unix.Equals(Token, index))
                {
                    if (!char.IsWhiteSpace(Token[index]) || char.IsLetterOrDigit(Token[index]))
                    {
                        return true;
                    }

                    ++index;
                }

                return false;
            }
        }

        /// <summary>
        /// Creates a tokenizer that uses the default words
        /// </summary>
        /// <param name="data"></param>
        public Tokenizer(string data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));

            //Preprocess to leave only \n as newlines
            _data = _data.NormalizeNewlines();
        }

        private void SkipWhitespace()
        {
            while (Index < _data.Length)
            {
                if ((LeaveNewLines && StringUtils.NewlineFormat.Unix.Equals(_data, Index)) || !char.IsWhiteSpace(_data[Index]))
                {
                    break;
                }

                ++Index;
            }
        }

        private bool SkipCommentLine()
        {
            foreach (var definition in _commentDefinitions)
            {
                if (definition.StartingDelimiter.Equals(_data, Index))
                {
                    var endIndex = _data.IndexOf(definition.EndingDelimiter, Index + definition.StartingDelimiter.Length);

                    if (definition.Callback != null)
                    {
                        //Pass the comment contents only
                        var commentStartIndex = Index + definition.StartingDelimiter.Length;

                        var commentData = _data.Substring(commentStartIndex, endIndex - commentStartIndex);

                        definition.Callback(definition, commentData);
                    }

                    //Comment didn't end, treat rest of input as comment
                    if (endIndex == -1)
                    {
                        Index = _data.Length;
                        return false;
                    }

                    //Point to past the ending delimiter
                    Index = endIndex + definition.EndingDelimiter.Length;

                    //Leave the last newline as the next token
                    //This can happen when comment end delimiters end with a newline, or consists only out of a newline
                    if (LeaveNewLines && definition.EndingDelimiter.EndsWith(StringUtils.NewlineFormat.Unix))
                    {
                        --Index;
                    }

                    return true;
                }
            }

            return false;
        }

        public bool Next()
        {
            Token = string.Empty;

            if (!HasNext)
            {
                return false;
            }

            bool checkComments;

            do
            {
                checkComments = false;

                SkipWhitespace();

                //Leave newlines as separate tokens
                if (LeaveNewLines && StringUtils.NewlineFormat.Unix.Equals(_data, Index))
                {
                    Token = StringUtils.NewlineFormat.Unix;
                    ++Index;
                    return true;
                }

                if (!HasNext)
                {
                    return false;
                }

                if (SkipCommentLine())
                {
                    checkComments = true;
                }
                else if (!HasNext)
                {
                    return false;
                }
            }
            while (checkComments);

            // handle quoted strings specially
            if (_data[Index] == '\"')
            {
                ++Index;

                var startIndex = Index;

                while (HasNext)
                {
                    if (_data[Index] == '\"')
                    {
                        break;
                    }

                    ++Index;
                }

                Token = _data.Substring(startIndex, Index - startIndex);

                if (HasNext)
                {
                    ++Index;
                }

                return true;
            }

            // parse words
            {
                var wordLength = TestForWord(Index);

                if (wordLength != -1)
                {
                    Token = _data.Substring(Index, wordLength);

                    Index += wordLength;

                    return true;
                }
            }

            // parse a regular word
            {
                var startIndex = Index;
                var endIndex = Index;

                char c;

                do
                {
                    endIndex = Index;

                    ++Index;

                    if (!HasNext)
                    {
                        break;
                    }

                    c = _data[Index];
                }
                while (!char.IsWhiteSpace(c) && TestForWord(Index) == -1);

                //Either we're out of data, or we hit whitespace or a word
                Token = _data.Substring(startIndex, (endIndex - startIndex) + 1);
            }

            return true;
        }

        /// <summary>
        /// Tests if the string starting at index is a word
        /// </summary>
        /// <param name="index"></param>
        /// <returns>If the string is a word, returns the length, otherwise returns -1</returns>
        private int TestForWord(int index)
        {
            foreach (var word in _words)
            {
                if (string.CompareOrdinal(word, 0, _data, index, word.Length) == 0)
                {
                    return word.Length;
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets all of the tokens from the given text
        /// </summary>
        /// <returns></returns>
        public List<string> GetTokens()
        {
            var list = new List<string>();

            for (; Next();)
            {
                list.Add(Token);
            }

            return list;
        }

        /// <summary>
        /// Gets all of the tokens from the given text
        /// </summary>
        /// <param name="text"></param>
        /// <param name="configureTokenizer">Optional callback to configure the tokenizer</param>
        /// <returns></returns>
        public static List<string> GetTokens(string text, Action<Tokenizer> configureTokenizer = null)
        {
            var tokenizer = new Tokenizer(text);

            configureTokenizer?.Invoke(tokenizer);

            return tokenizer.GetTokens();
        }
    }
}
