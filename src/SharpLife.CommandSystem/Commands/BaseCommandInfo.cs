﻿/***
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

namespace SharpLife.CommandSystem.Commands
{
    /// <summary>
    /// Base class for info classes about specific command types
    /// </summary>
    /// <typeparam name="TDerived">The class deriving from this one</typeparam>
    public abstract class BaseCommandInfo<TDerived> where TDerived : BaseCommandInfo<TDerived>
    {
        public string Name { get; }

        public string HelpInfo { get; private set; } = string.Empty;

        public CommandFlags Flags { get; private set; }

        public object Tag { get; set; }

        protected BaseCommandInfo(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }

            Name = name;
        }

        public TDerived WithHelpInfo(string helpInfo)
        {
            HelpInfo = helpInfo ?? throw new ArgumentNullException(nameof(helpInfo));

            return this as TDerived;
        }

        public TDerived WithFlags(CommandFlags flags)
        {
            Flags = flags;

            return this as TDerived;
        }

        public TDerived AddFlags(CommandFlags flags)
        {
            //Don't allow modification of user flags
            Flags |= flags & CommandFlags.AllCommandFlags;

            return this as TDerived;
        }

        public TDerived RemoveFlags(CommandFlags flags)
        {
            Flags &= ~flags | (Flags & ~CommandFlags.AllCommandFlags);

            return this as TDerived;
        }

        public TDerived WithUserFlags(uint flags)
        {
            Flags = (((CommandFlags)flags) & ~CommandFlags.AllCommandFlags) | (Flags & CommandFlags.AllCommandFlags);

            return this as TDerived;
        }

        public TDerived AddUserFlags(uint flags)
        {
            //Don't allow modification of builtin flags
            Flags |= ((CommandFlags)flags) & ~CommandFlags.AllCommandFlags;

            return this as TDerived;
        }

        public TDerived RemoveUserFlags(uint flags)
        {
            Flags &= ((CommandFlags)~flags) | (Flags & CommandFlags.AllCommandFlags);

            return this as TDerived;
        }

        public TDerived WithTag(object tag)
        {
            Tag = tag;

            return this as TDerived;
        }
    }
}
