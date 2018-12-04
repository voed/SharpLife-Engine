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
            if (Index + 1 < _data.Length && _data[Index] == '/' && _data[Index + 1] == '/')
            {
                var index = _data.IndexOf('\n', Index + 2);

                if (index == -1)
                {
                    Index = _data.Length;
                    return false;
                }

                //Leave the newline as the next token
                if (LeaveNewLines)
                {
                    Index = index;
                }
                else
                {
                    Index = index + 1;
                }

                return true;
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
        /// <param name="text"></param>
        /// <param name="leaveNewLines"></param>
        /// <returns></returns>
        public static List<string> GetTokens(string text, bool leaveNewLines = false)
        {
            return GetTokens(text, DefaultWords, leaveNewLines);
        }

        /// <summary>
        /// Gets all of the tokens from the given text
        /// Uses the given list of words
        /// </summary>
        /// <param name="text"></param>
        /// <param name="words"></param>
        /// <param name="leaveNewLines"></param>
        /// <returns></returns>
        public static List<string> GetTokens(string text, IEnumerable<string> words, bool leaveNewLines = false)
        {
            var list = new List<string>();

            for (var tokenizer = new Tokenizer(text) { Words = words, LeaveNewLines = leaveNewLines }; tokenizer.Next();)
            {
                list.Add(tokenizer.Token);
            }

            return list;
        }
    }
}
