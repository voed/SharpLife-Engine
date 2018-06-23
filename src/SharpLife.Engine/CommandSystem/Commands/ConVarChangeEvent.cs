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

namespace SharpLife.Engine.CommandSystem.Commands
{
    public struct ConVarChangeEvent
    {
        private readonly ConVar _variable;

        public IConVar Variable => _variable;

        public string String
        {
            get => _variable.String;
            set => _variable.SetString(value, true);
        }

        public float Float
        {
            get => _variable.Float;
            set => _variable.SetFloat(value, true);
        }

        public int Integer
        {
            get => _variable.Integer;
            set => _variable.SetInteger(value, true);
        }

        public bool Boolean
        {
            get => _variable.Boolean;
            set => _variable.SetBoolean(value, true);
        }

        public string OldString { get; }

        public float OldFloat { get; }

        public int OldInteger { get; }

        public bool OldBoolean { get; }

        /// <summary>
        /// Indicates whether the variable is different from its old value
        /// </summary>
        public bool Different => Variable.String != OldString;

        internal ConVarChangeEvent(ConVar variable, string oldString, float oldFloat, int oldInteger, bool oldBoolean)
        {
            _variable = variable ?? throw new ArgumentNullException(nameof(variable));
            OldString = oldString ?? throw new ArgumentNullException(nameof(oldString));
            OldFloat = oldFloat;
            OldInteger = oldInteger;
            OldBoolean = oldBoolean;
        }
    }
}
