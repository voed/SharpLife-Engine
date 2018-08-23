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

using SharpLife.Engine.API.Shared.Logging;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

namespace SharpLife.Engine.Shared.Logging
{
    public sealed class ForwardingTextWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public ILogListener Listener { get; set; }

        public override void Write(char value)
        {
            Listener?.Write(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (buffer.Length - index < count)
            {
                throw new ArgumentException("Invalid length or offset");
            }

            Contract.EndContractBlock();

            Listener?.Write(buffer, index, count);
        }
    }
}
