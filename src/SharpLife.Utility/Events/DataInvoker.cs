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

namespace SharpLife.Utility.Events
{
    /// <summary>
    /// Invokes a <see cref="Delegates.DataListener{TDataType}"/>
    /// </summary>
    /// <typeparam name="TDataType"></typeparam>
    internal sealed class DataInvoker<TDataType> : Invoker where TDataType : EventData
    {
        public override object Target => Listener.Target;

        public override Delegate Delegate => Listener;

        private Delegates.DataListener<TDataType> Listener { get; }

        public DataInvoker(Delegates.DataListener<TDataType> listener)
        {
            Listener = listener ?? throw new ArgumentNullException(nameof(listener));
        }

        public override void Invoke(in Event @event)
        {
            Listener(@event, @event.Data as TDataType);
        }
    }
}
