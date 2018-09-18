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
using System.Runtime.Serialization;

namespace SharpLife.Utility
{
    /// <summary>
    /// Base class for all exceptions thrown when a file fails to load
    /// </summary>
    public class FileLoadFailureException : Exception
    {
        public FileLoadFailureException()
        {
        }

        protected FileLoadFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FileLoadFailureException(string message)
            : base(message)
        {
        }

        public FileLoadFailureException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
