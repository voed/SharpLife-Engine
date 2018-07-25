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
using System.Collections.Generic;
using System.Reflection;

namespace SharpLife.Utility.Events
{
    public static class EventUtils
    {
        private static readonly Dictionary<Type, string> CachedEventTypeNames = new Dictionary<Type, string>();

        public static string EventName(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!CachedEventTypeNames.TryGetValue(type, out var name))
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.FieldType == typeof(string)
                        && field.GetCustomAttribute<EventNameAttribute>() != null)
                    {
                        name = (string)field.GetRawConstantValue();
                        break;
                    }
                }

                //Fall back to using the type name
                if (name == null)
                {
                    name = type.Name;
                }

                CachedEventTypeNames.Add(type, name);
            }

            return name;
        }

        public static string EventName<TDataType>()
        {
            return EventName(typeof(TDataType));
        }

        /// <summary>
        /// Registers all event names in a simple events class
        /// </summary>
        /// <param name="eventSystem"></param>
        /// <param name="eventList"></param>
        public static void RegisterEvents(IEventSystem eventSystem, IEventList eventList)
        {
            if (eventSystem == null)
            {
                throw new ArgumentNullException(nameof(eventSystem));
            }

            if (eventList == null)
            {
                throw new ArgumentNullException(nameof(eventList));
            }

            foreach (var name in eventList.SimpleEvents)
            {
                eventSystem.RegisterEvent(name);
            }

            foreach (var eventType in eventList.EventTypes)
            {
                eventSystem.RegisterEvent(EventName(eventType), eventType);
            }
        }

        /// <summary>
        /// Unregisters all event names in a simple events class
        /// </summary>
        /// <param name="eventSystem"></param>
        /// <param name="eventList"></param>
        public static void UnregisterEvents(IEventSystem eventSystem, IEventList eventList)
        {
            if (eventSystem == null)
            {
                throw new ArgumentNullException(nameof(eventSystem));
            }

            if (eventList == null)
            {
                throw new ArgumentNullException(nameof(eventList));
            }

            foreach (var name in eventList.SimpleEvents)
            {
                eventSystem.UnregisterEvent(name);
            }

            foreach (var eventType in eventList.EventTypes)
            {
                eventSystem.UnregisterEvent(EventName(eventType));
            }
        }
    }
}
