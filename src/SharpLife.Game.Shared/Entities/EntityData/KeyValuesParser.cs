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

using SharpLife.Utility;
using System;
using System.Collections.Generic;

namespace SharpLife.Game.Shared.Entities.EntityData
{
    /// <summary>
    /// Can parse keyvalues data into data structures
    /// </summary>
    public static class KeyValuesParser
    {
        /// <summary>
        /// Parses the next block in the tokenizer into a list of keyvalues
        /// </summary>
        /// <param name="tokenizer"></param>
        /// <returns>A list of keyvalues that have been parsed</returns>
        /// <exception cref="ArgumentException">If the syntax is invalid</exception>
        public static List<KeyValuePair<string, string>> Parse(Tokenizer tokenizer)
        {
            if (tokenizer == null)
            {
                throw new ArgumentNullException(nameof(tokenizer));
            }

            var list = new List<KeyValuePair<string, string>>();

            //Valid syntax is as follows:
            //{
            //  "key" "value"
            //  key "value"
            //  "key" value
            //  key value
            //}
            //
            //Empty blocks are allowed:
            //{
            //}
            //
            //Nested blocks are not supported

            if (tokenizer.Next())
            {
                if (tokenizer.Token != "{")
                {
                    throw new ArgumentException($"KeyValuesParser.Parse: found {tokenizer.Token} when expecting {{");
                }

                while (true)
                {
                    if (!tokenizer.Next())
                    {
                        throw new ArgumentException("KeyValuesParser.Parse: EOF without closing brace");
                    }

                    //Empty block, or end of keyvalues
                    if (tokenizer.Token == "}")
                    {
                        break;
                    }

                    var key = tokenizer.Token;

                    // another hack to fix keynames with trailing spaces
                    key = key.TrimEnd();

                    if (!tokenizer.Next())
                    {
                        throw new ArgumentException("KeyValuesParser.Parse: EOF without closing brace");
                    }

                    if (tokenizer.Token == "}")
                    {
                        throw new ArgumentException("KeyValuesParser.Parse: closing brace without data");
                    }

                    var value = tokenizer.Token;

                    list.Add(new KeyValuePair<string, string>(key, value));
                }
            }

            return list;
        }

        /// <summary>
        /// Parses all blocks
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static List<List<KeyValuePair<string, string>>> ParseAll(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var tokenizer = new Tokenizer(data);

            var list = new List<List<KeyValuePair<string, string>>>();

            //If it's empty we've ran out of data to parse
            for (var block = Parse(tokenizer); block.Count > 0; block = Parse(tokenizer))
            {
                list.Add(block);
            }

            return list;
        }
    }
}
