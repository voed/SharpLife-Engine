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

using SharpLife.CommandSystem.Commands.VariableFilters;
using System;
using System.Collections.Generic;

namespace SharpLife.CommandSystem.Commands
{
    /// <summary>
    /// Contains information about a console variable
    /// A convar can only have a value of one type at any given time, changing value types resets the other types
    /// </summary>
    public sealed class ConVarInfo : BaseCommandInfo<ConVarInfo>
    {
        //Default to empty string value
        public string StringValue { get; private set; } = string.Empty;

        public float? FloatValue { get; private set; }

        public int? IntegerValue { get; private set; }

        private List<IConVarFilter> _filters;

        public IReadOnlyList<IConVarFilter> Filters => _filters;

        private readonly List<Delegates.ConVarChangeHandler> _onChangeDelegates = new List<Delegates.ConVarChangeHandler>();

        public IReadOnlyList<Delegates.ConVarChangeHandler> ChangeHandlers => _onChangeDelegates;

        public ConVarInfo(string name)
            : base(name)
        {
        }

        public ConVarInfo WithValue(string value)
        {
            StringValue = value ?? throw new ArgumentNullException(nameof(value));

            FloatValue = null;
            IntegerValue = null;

            return this;
        }

        public ConVarInfo WithValue(float value)
        {
            FloatValue = value;
            StringValue = null;
            IntegerValue = null;

            return this;
        }

        public ConVarInfo WithValue(int value)
        {
            IntegerValue = value;
            StringValue = null;
            FloatValue = null;

            return this;
        }

        public ConVarInfo WithFilter(IConVarFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            (_filters ?? (_filters = new List<IConVarFilter>())).Add(filter);

            return this;
        }

        public ConVarInfo WithChangeHandler(Delegates.ConVarChangeHandler changeHandler)
        {
            if (changeHandler == null)
            {
                throw new ArgumentNullException(nameof(changeHandler));
            }

            _onChangeDelegates.Add(changeHandler);

            return this;
        }
    }
}
