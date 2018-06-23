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

namespace SharpLife.Engine.CommandSystem
{
    /// <summary>
    /// Thrown when a console command is invoked with the wrong syntax
    /// </summary>
    internal sealed class InvalidCommandSyntaxException : Exception
    {
        public InvalidCommandSyntaxException() : base()
        {
        }

        public InvalidCommandSyntaxException(string message) : base(message)
        {
        }

        public InvalidCommandSyntaxException(string message, Exception innerException) : base(message, innerException)
        {
        }

        private InvalidCommandSyntaxException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
