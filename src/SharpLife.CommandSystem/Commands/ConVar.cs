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
using System.Globalization;
using System.Linq;

namespace SharpLife.CommandSystem.Commands
{
    internal class ConVar : BaseConsoleCommand, IConVar
    {
        public string InitialValue { get; private set; }

        private string _stringValue = string.Empty;

        private float _floatValue;

        public string String
        {
            get => _stringValue;
            set => SetString(value, false);
        }

        public float Float
        {
            get => _floatValue;
            set
            {
                SetFloat(value, false);
            }
        }

        public int Integer
        {
            get => (int)_floatValue;
            set
            {
                SetInteger(value, false);
            }
        }

        public bool Boolean
        {
            get => _floatValue != 0;
            set
            {
                SetBoolean(value, false);
            }
        }

        private List<IConVarFilter> _filters;

        public IReadOnlyList<IConVarFilter> Filters => _filters;

        public event Delegates.ConVarChangeHandler OnChange;

        public ConVar(ConCommandSystem commandSystem, string name, string value, CommandFlags flags, string helpInfo, IReadOnlyList<IConVarFilter> filters, IReadOnlyList<Delegates.ConVarChangeHandler> changeHandlers)
            : base(commandSystem, name, flags, helpInfo)
        {
            SetString(value, true);

            Construct(filters, changeHandlers);
        }

        public ConVar(ConCommandSystem commandSystem, string name, float value, CommandFlags flags, string helpInfo, IReadOnlyList<IConVarFilter> filters, IReadOnlyList<Delegates.ConVarChangeHandler> changeHandlers)
            : base(commandSystem, name, flags, helpInfo)
        {
            SetFloat(value, true);

            Construct(filters, changeHandlers);
        }

        public ConVar(ConCommandSystem commandSystem, string name, int value, CommandFlags flags, string helpInfo, IReadOnlyList<IConVarFilter> filters, IReadOnlyList<Delegates.ConVarChangeHandler> changeHandlers)
            : base(commandSystem, name, flags, helpInfo)
        {
            SetInteger(value, true);

            Construct(filters, changeHandlers);
        }

        private void Construct(IReadOnlyList<IConVarFilter> filters, IReadOnlyList<Delegates.ConVarChangeHandler> changeHandlers)
        {
            InitialValue = String;

            _filters = filters?.ToList();

            if (changeHandlers != null)
            {
                foreach (var handler in changeHandlers)
                {
                    OnChange += handler;
                }
            }
        }

        public void RevertToInitialValue()
        {
            String = InitialValue;
        }

        public void AddFilter(IConVarFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            (_filters ?? (_filters = new List<IConVarFilter>())).Add(filter);
        }

        internal void SetString(string value, bool suppressChangeMessage)
        {
            float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var floatValue);

            SetValue(value, floatValue, suppressChangeMessage: suppressChangeMessage);
        }

        internal void SetFloat(float value, bool suppressChangeMessage)
        {
            SetValue(CommandUtils.FloatToConVarString(value), value, suppressChangeMessage);
        }

        internal void SetInteger(int value, bool suppressChangeMessage)
        {
            SetValue(value.ToString(), value, suppressChangeMessage);
        }

        internal void SetBoolean(bool value, bool suppressChangeMessage)
        {
            var intValue = value ? 1 : 0;
            SetValue(intValue.ToString(), intValue, suppressChangeMessage);
        }

        private void SetValue(string stringValue, float floatValue, bool suppressChangeMessage = false)
        {
            if (_filters != null)
            {
                foreach (var filter in _filters)
                {
                    if (!filter.Filter(ref stringValue, ref floatValue))
                    {
                        return;
                    }
                }
            }

            var changeEvent = new ConVarChangeEvent(this, String, Float, Integer, Boolean);

            _stringValue = stringValue ?? throw new ArgumentNullException(nameof(stringValue));
            _floatValue = floatValue;

            OnChange?.Invoke(ref changeEvent);

            if (!suppressChangeMessage && String != changeEvent.OldString)
            {
                //If none of the change handlers reverted the change, print a change message
                _commandSystem._logger.Information($"\"{Name}\" changed to \"{String}\"");
            }
        }

        internal override void OnCommand(ICommand command)
        {
            if (command.Count == 0)
            {
                _commandSystem._logger.Information($"\"{Name}\" is \"{String}\"");
                return;
            }

            if (command.Count == 1)
            {
                SetString(command[0], false);
                return;
            }

            throw new InvalidCommandSyntaxException("Console variables can only be set with syntax \"name value\"");
        }
    }
}
