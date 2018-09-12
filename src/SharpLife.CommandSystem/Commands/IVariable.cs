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
using System.Collections.Generic;

namespace SharpLife.CommandSystem.Commands
{
    public interface IVariable : IBaseCommand
    {
        /// <summary>
        /// The initial value assigned to this variable
        /// </summary>
        string InitialValue { get; }

        string String { get; set; }

        float Float { get; set; }

        int Integer { get; set; }

        bool Boolean { get; set; }

        IReadOnlyList<IVariableFilter> Filters { get; }

        /// <summary>
        /// Invoked after the variable has changed
        /// Change handlers may change the variable by using the change event interface
        /// If the variable is reset to its old value, the change message is suppressed
        /// </summary>
        event Delegates.VariableChangeHandler OnChange;

        /// <summary>
        /// Resets this variable to <see cref="InitialValue"/>
        /// </summary>
        void RevertToInitialValue();

        /// <summary>
        /// Adds a filter to the variable
        /// </summary>
        /// <param name="filter"></param>
        void AddFilter(IVariableFilter filter);

        /// <summary>
        /// Gets a filter by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="System.Reflection.AmbiguousMatchException">If more than one filter of the given type exists on this variable</exception>
        T GetFilter<T>() where T : class, IVariableFilter;
    }
}
