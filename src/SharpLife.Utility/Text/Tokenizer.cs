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

namespace SharpLife.Utility.Text
{
    /// <summary>
    /// Tokenizes a string
    /// </summary>
    public sealed class Tokenizer
    {
        private readonly string _data;

        private TokenizerConfiguration _configuration;

        public TokenizerConfiguration Configuration
        {
            get => _configuration;
            set => _configuration = value ?? throw new ArgumentNullException(nameof(value));
        }

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
        /// Creates a tokenizer that can parse tokens out of the given string
        /// </summary>
        /// <param name="data"></param>
        /// <param name="configuration"></param>
        public Tokenizer(string data, TokenizerConfiguration configuration)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            //Preprocess to leave only \n as newlines
            _data = _data.NormalizeNewlines();
        }

        /// <summary>
        /// Creates a tokenizer that uses the default configuration
        /// </summary>
        /// <param name="data"></param>
        /// <param name="configuration"></param>
        public Tokenizer(string data)
            : this(data, TokenizerConfiguration.Default)
        {
        }

        private void SkipWhitespace()
        {
            while (Index < _data.Length)
            {
                if ((_configuration.LeaveNewlines && StringUtils.NewlineFormat.Unix.Equals(_data, Index)) || !char.IsWhiteSpace(_data[Index]))
                {
                    break;
                }

                ++Index;
            }
        }

        private bool SkipCommentLine()
        {
            foreach (var definition in _configuration.CommentDefinitions)
            {
                if (definition.StartingDelimiter.Equals(_data, Index))
                {
                    var endIndex = _data.IndexOf(definition.EndingDelimiter, Index + definition.StartingDelimiter.Length);

                    if (definition.Callback != null)
                    {
                        //Pass the comment contents only
                        var commentStartIndex = Index + definition.StartingDelimiter.Length;

                        var commentData = _data.Substring(commentStartIndex, endIndex - commentStartIndex);

                        definition.Callback(this, definition, commentData);
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
                    if (_configuration.LeaveNewlines && definition.EndingDelimiter.EndsWith(StringUtils.NewlineFormat.Unix))
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
                if (_configuration.LeaveNewlines && StringUtils.NewlineFormat.Unix.Equals(_data, Index))
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
            foreach (var word in _configuration.Words)
            {
                if (string.CompareOrdinal(word, 0, _data, index, word.Length) == 0)
                {
                    return word.Length;
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets the file position of the given index
        /// Both values are zero based
        /// </summary>
        /// <param name="index"></param>
        /// <param name="line"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">If the index is out of range of the data</exception>
        public void GetFilePosition(int index, out int line, out int column)
        {
            if (index < 0 || index >= _data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var currentLine = 0;
            var currentColumn = 0;

            for (var i = 0; i < index; ++i)
            {
                if (StringUtils.NewlineFormat.Unix.Equals(_data, i))
                {
                    ++currentLine;
                    currentColumn = 0;
                }
                else
                {
                    ++currentColumn;
                }
            }

            line = currentLine;
            column = currentColumn;
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
        /// <returns></returns>
        public static List<string> GetTokens(string text)
        {
            var tokenizer = new Tokenizer(text);

            return tokenizer.GetTokens();
        }

        /// <summary>
        /// Gets all of the tokens from the given text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static List<string> GetTokens(string text, TokenizerConfiguration configuration)
        {
            var tokenizer = new Tokenizer(text, configuration);

            return tokenizer.GetTokens();
        }
    }
}
