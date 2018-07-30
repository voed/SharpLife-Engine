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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLife.CommandSystem.Commands
{
    public class CommandArgs : ICommandArgs
    {
        public ICommandContext Context { get; }

        public string Name { get; }

        public int Count => _arguments.Count;

        public string this[int index] => _arguments[index];

        private readonly IList<string> _arguments;

        IList<string> ICommandArgs.Arguments => _arguments.ToList();

        public string CommandString
        {
            get
            {
                var builder = new StringBuilder();

                builder.Append(Name);

                builder = AddArguments(builder);

                return builder.ToString();
            }
        }

        public string ArgumentsString => ArgumentsAsString(0);

        private StringBuilder AddArguments(StringBuilder builder, int firstArgumentIndex = 0)
        {
            for (var index = firstArgumentIndex; index < _arguments.Count; ++index)
            {
                var arg = _arguments[index];

                builder.Append(' ');

                if (arg.Any(char.IsWhiteSpace))
                {
                    builder.Append('\"').Append(arg).Append('\"');
                }
                else
                {
                    builder.Append(arg);
                }
            }

            return builder;
        }

        public CommandArgs(ICommandContext context, string name, IList<string> arguments)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }

            Name = name;

            _arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public string ArgumentsAsString(int firstArgumentIndex)
        {
            var builder = AddArguments(new StringBuilder(), firstArgumentIndex);

            if (builder.Length <= 0)
            {
                return string.Empty;
            }

            return builder.ToString(1, builder.Length - 1);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _arguments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}


