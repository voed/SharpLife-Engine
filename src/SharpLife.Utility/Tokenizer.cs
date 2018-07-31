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

namespace SharpLife.Utility
{
    /// <summary>
    /// Tokenizes a string
    /// </summary>
    public sealed class Tokenizer
    {
        private readonly string _data;

        private readonly IReadOnlyList<char> _singleCharacters;

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

                while (index < Token.Length && Token[index] != '\n')
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
        /// Characters to treat as their own tokens
        /// </summary>
        public static readonly IReadOnlyList<char> SingleCharacters =
        new[]{
            '{',
            '}',
            '(',
            ')',
            '\'',
            ','
        };

        /// <summary>
        /// Creates a tokenizer that uses the default single characters list
        /// </summary>
        /// <param name="data"></param>
        public Tokenizer(string data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _singleCharacters = SingleCharacters;
        }

        /// <summary>
        /// Creates a tokenizer that uses the given list of single characters
        /// </summary>
        /// <param name="data"></param>
        /// <param name="singleCharacters"></param>
        public Tokenizer(string data, IReadOnlyList<char> singleCharacters)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _singleCharacters = singleCharacters ?? throw new ArgumentNullException(nameof(singleCharacters));
        }

        private void SkipWhitespace()
        {
            while (Index < _data.Length)
            {
                if (!char.IsWhiteSpace(_data[Index]))
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
                var index = _data.IndexOf('\n');

                if (index == -1)
                {
                    Index = _data.Length;
                    return false;
                }

                Index = index + 1;

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

            // parse single characters
            {
                var c = _data[Index];

                if (_singleCharacters.Contains(c))
                {
                    Token = new string(_data[Index++], 1);

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
                while (!char.IsWhiteSpace(c) && !_singleCharacters.Contains(c));

                //Either we're out of data, or we hit whitespace or a single character
                Token = _data.Substring(startIndex, (endIndex - startIndex) + 1);
            }

            return true;
        }

        /// <summary>
        /// Gets all of the tokens from the given text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static IList<string> GetTokens(string text)
        {
            var list = new List<string>();

            for (var tokenizer = new Tokenizer(text); tokenizer.Next();)
            {
                list.Add(tokenizer.Token);
            }

            return list;
        }

        /// <summary>
        /// Gets all of the tokens from the given text
        /// Uses the given list of single characters
        /// </summary>
        /// <param name="text"></param>
        /// <param name="singleCharacters"></param>
        /// <returns></returns>
        public static IList<string> GetTokens(string text, IReadOnlyList<char> singleCharacters)
        {
            var list = new List<string>();

            for (var tokenizer = new Tokenizer(text, singleCharacters); tokenizer.Next();)
            {
                list.Add(tokenizer.Token);
            }

            return list;
        }
    }
}
