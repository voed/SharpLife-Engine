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
    /// Contains information about a command variable
    /// A command variable can only have a value of one type at any given time, changing value types resets the other types
    /// </summary>
    public sealed class VariableInfo : BaseCommandInfo<VariableInfo>
    {
        //Default to empty string value
        public string StringValue { get; private set; } = string.Empty;

        public float? FloatValue { get; private set; }

        public int? IntegerValue { get; private set; }

        private List<IVariableFilter> _filters;

        public IReadOnlyList<IVariableFilter> Filters => _filters;

        private readonly List<Delegates.VariableChangeHandler> _onChangeDelegates = new List<Delegates.VariableChangeHandler>();

        public IReadOnlyList<Delegates.VariableChangeHandler> ChangeHandlers => _onChangeDelegates;

        public VariableInfo(string name)
            : base(name)
        {
        }

        public VariableInfo WithValue(string value)
        {
            StringValue = value ?? throw new ArgumentNullException(nameof(value));

            FloatValue = null;
            IntegerValue = null;

            return this;
        }

        public VariableInfo WithValue(float value)
        {
            FloatValue = value;
            StringValue = null;
            IntegerValue = null;

            return this;
        }

        public VariableInfo WithValue(int value)
        {
            IntegerValue = value;
            StringValue = null;
            FloatValue = null;

            return this;
        }

        public VariableInfo WithValue(bool value)
        {
            IntegerValue = value ? 1 : 0;
            StringValue = null;
            FloatValue = null;

            return this;
        }

        public VariableInfo WithFilter(IVariableFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            (_filters ?? (_filters = new List<IVariableFilter>())).Add(filter);

            return this;
        }

        public VariableInfo WithChangeHandler(Delegates.VariableChangeHandler changeHandler)
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
