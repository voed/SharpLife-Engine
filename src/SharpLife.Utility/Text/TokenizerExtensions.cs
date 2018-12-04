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
    public static class TokenizerExtensions
    {
        public static readonly Tokenizer.CommentDefinition[] CStyleComments = new[] { Tokenizer.CStyleComment };

        public static readonly Tokenizer.CommentDefinition[] CPPStyleComments = new[] { Tokenizer.CStyleComment, Tokenizer.CPPStyleComment };

        /// <summary>
        /// Configures the tokenizer to handle C style comments only
        /// </summary>
        /// <param name="tokenizer"></param>
        /// <returns></returns>
        public static Tokenizer WithCStyleComments(this Tokenizer tokenizer)
        {
            if (tokenizer == null)
            {
                throw new ArgumentNullException(nameof(tokenizer));
            }

            tokenizer.CommentDefinitions = CStyleComments;

            return tokenizer;
        }

        /// <summary>
        /// Configures the tokenizer to handle C and C++ style comments
        /// </summary>
        /// <param name="tokenizer"></param>
        /// <returns></returns>
        public static Tokenizer WithCPPStyleComments(this Tokenizer tokenizer)
        {
            if (tokenizer == null)
            {
                throw new ArgumentNullException(nameof(tokenizer));
            }

            tokenizer.CommentDefinitions = CPPStyleComments;

            return tokenizer;
        }

        /// <summary>
        /// Configures the tokenizer to use the given list of words
        /// </summary>
        /// <param name="tokenizer"></param>
        /// <returns></returns>
        public static Tokenizer WithWords(this Tokenizer tokenizer, IEnumerable<string> words)
        {
            if (tokenizer == null)
            {
                throw new ArgumentNullException(nameof(tokenizer));
            }

            tokenizer.Words = words;

            return tokenizer;
        }

        /// <summary>
        /// Configures the tokenizer to leave newlines as separate tokens
        /// </summary>
        /// <param name="tokenizer"></param>
        /// <returns></returns>
        public static Tokenizer LeaveNewlines(this Tokenizer tokenizer, bool leaveNewlines = true)
        {
            if (tokenizer == null)
            {
                throw new ArgumentNullException(nameof(tokenizer));
            }

            tokenizer.LeaveNewLines = leaveNewlines;

            return tokenizer;
        }
    }
}
