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
    /// <para>
    /// Reusable tokenizer configuration
    /// <seealso cref="Tokenizer"/>
    /// </para>
    /// <para>Each change creates a new configuration object</para>
    /// <para>Cache the result to avoid allocations</para>
    /// </summary>
    public sealed class TokenizerConfiguration
    {
        public sealed class CommentDefinition
        {
            public readonly string StartingDelimiter;

            public readonly string EndingDelimiter;

            /// <summary>
            /// Invoked when this comment is encountered, passing the current Tokenizer, this CommentDefinition and the comment string excluding delimiters
            /// </summary>
            public readonly Action<Tokenizer, CommentDefinition, string> Callback;

            /// <summary>
            /// Creates a new comment definition
            /// </summary>
            /// <param name="startingDelimiter">The starting delimiter of the comment. Must contain valid characters</param>
            /// <param name="endingDelimiter">The ending delimiter of the comment. Must contain valid characters. If null, the comment ends after the newline</param>
            /// <param name="callback">Optional callback to invoke when this comment is encountered</param>
            public CommentDefinition(string startingDelimiter, string endingDelimiter = null, Action<Tokenizer, CommentDefinition, string> callback = null)
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

        public static readonly IEnumerable<string> NoWords = Enumerable.Empty<string>();

        public static readonly IEnumerable<CommentDefinition> NoCommentDefinitions = Enumerable.Empty<CommentDefinition>();

        public static readonly CommentDefinition CStyleComment = new CommentDefinition("//");

        public static readonly CommentDefinition CPPStyleComment = new CommentDefinition("/*", "*/");

        public static readonly CommentDefinition[] CStyleComments = new[] { CStyleComment };

        public static readonly CommentDefinition[] CPPStyleComments = new[] { CStyleComment, CPPStyleComment };

        public static readonly TokenizerConfiguration Default = new TokenizerConfiguration();

        public IEnumerable<string> Words { get; private set; } = NoWords;

        public IEnumerable<CommentDefinition> CommentDefinitions { get; private set; } = NoCommentDefinitions;

        /// <summary>
        /// If true, newlines will be left as separate tokens
        /// </summary>
        public bool LeaveNewlines { get; private set; }

        private TokenizerConfiguration Clone()
        {
            return new TokenizerConfiguration
            {
                Words = Words.ToList(),
                CommentDefinitions = CommentDefinitions.ToList(),
                LeaveNewlines = LeaveNewlines
            };
        }

        public TokenizerConfiguration WithWords(IEnumerable<string> words)
        {
            if (words == null)
            {
                throw new ArgumentNullException(nameof(words));
            }

            if (words.Any(string.IsNullOrEmpty))
            {
                throw new ArgumentException("A word must be non-null and contain at least one character", nameof(words));
            }

            var config = Clone();

            config.Words = words.ToList();

            return config;
        }

        public TokenizerConfiguration WithCommentDefinitions(IEnumerable<CommentDefinition> commentDefinitions)
        {
            if (commentDefinitions == null)
            {
                throw new ArgumentNullException(nameof(commentDefinitions));
            }

            var config = Clone();

            config.CommentDefinitions = commentDefinitions.ToList();

            return config;
        }

        public TokenizerConfiguration WithLeaveNewlines(bool leaveNewlines = true)
        {
            var config = Clone();

            config.LeaveNewlines = leaveNewlines;

            return config;
        }

        /// <summary>
        /// Configures the tokenizer to handle C style comments only
        /// </summary>
        /// <param name="tokenizer"></param>
        /// <returns></returns>
        public TokenizerConfiguration WithCStyleComments() => WithCommentDefinitions(CStyleComments);

        /// <summary>
        /// Configures the tokenizer to handle C and C++ style comments
        /// </summary>
        /// <param name="tokenizer"></param>
        /// <returns></returns>
        public TokenizerConfiguration WithCPPStyleComments() => WithCommentDefinitions(CPPStyleComments);
    }
}
