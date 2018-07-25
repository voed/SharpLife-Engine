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
    /// The event system allows named events to be dispatched to listeners that want to know about them
    /// Events can contain data, represented as classes inheriting from a base event data class
    /// Listeners cannot be added or removed while an event dispatch is ongoing, they will be queued up and processed after the dispatch
    /// </summary>
    public interface IEventSystem
    {
        /// <summary>
        /// Indicates whether the event system is currently dispatching events
        /// </summary>
        bool IsDispatching { get; }

        /// <summary>
        /// Returns whether the given event exists
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool HasEvent(string name);

        /// <summary>
        /// Registers an event name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        void RegisterEvent(string name);

        /// <summary>
        /// Registers an event name
        /// </summary>
        /// <typeparam name="TDataType">Event data type</typeparam>
        /// <param name="name"></param>
        void RegisterEvent<TDataType>(string name) where TDataType : EventData;

        /// <summary>
        /// Registers an event name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        void RegisterEvent(string name, Type dataType);

        /// <summary>
        /// Unregisters an event
        /// Also removes all listeners for the event
        /// </summary>
        /// <param name="name"></param>
        void UnregisterEvent(string name);

        /// <summary>
        /// Adds a listener for a specific event
        /// </summary>
        /// <param name="name"></param>
        /// <param name="listener"></param>
        void AddListener(string name, Delegates.Listener listener);

        /// <summary>
        /// Adds a listener for a specific event
        /// </summary>
        /// <typeparam name="TDataType">Event data type</typeparam>
        /// <param name="name"></param>
        /// <param name="listener"></param>
        void AddListener<TDataType>(string name, Delegates.Listener listener) where TDataType : EventData;

        /// <summary>
        /// Adds a listener for a specific event
        /// The event name is inferred from the type
        /// </summary>
        /// <typeparam name="TDataType">Event data type</typeparam>
        /// <param name="listener"></param>
        void AddListener<TDataType>(Delegates.Listener listener) where TDataType : EventData;

        /// <summary>
        /// Adds a listener for a specific event, taking the data as a separate argument
        /// </summary>
        /// <typeparam name="TDataType"></typeparam>
        /// <param name="name"></param>
        /// <param name="listener"></param>
        void AddListener<TDataType>(string name, Delegates.DataListener<TDataType> listener) where TDataType : EventData;

        /// <summary>
        /// Adds a listener for a specific event, taking the data as a separate argument
        /// The event name is inferred from the type
        /// </summary>
        /// <typeparam name="TDataType"></typeparam>
        /// <param name="listener"></param>
        void AddListener<TDataType>(Delegates.DataListener<TDataType> listener) where TDataType : EventData;

        /// <summary>
        /// Adds a listener to multiple events
        /// <seealso cref="AddListener(string, Delegates.Listener)"/>
        /// </summary>
        /// <param name="names">List of names</param>
        /// <param name="listener"></param>
        void AddListeners(string[] names, Delegates.Listener listener);

        /// <summary>
        /// Adds a listener to multiple events
        /// <seealso cref="AddListener(string, Delegates.Listener)"/>
        /// </summary>
        /// <typeparam name="TDataType">Event data type</typeparam>
        /// <param name="names">List of names</param>
        /// <param name="listener"></param>
        void AddListeners<TDataType>(string[] names, Delegates.Listener listener) where TDataType : EventData;

        /// <summary>
        /// Removes all listeners of a specific event
        /// </summary>
        /// <param name="name"></param>
        void RemoveListeners(string name);

        /// <summary>
        /// Removes a listener by delegate
        /// </summary>
        /// <param name="listener"></param>
        void RemoveListener(Delegates.Listener listener);

        /// <summary>
        /// Removes a listener from a specific event
        /// </summary>
        /// <param name="name"></param>
        /// <param name="listener"></param>
        void RemoveListener(string name, Delegates.Listener listener);

        /// <summary>
        /// Removes the given listener from all events that is it listening to
        /// </summary>
        /// <param name="listener"></param>
        void RemoveListener(object listener);

        /// <summary>
        /// Removes all listeners
        /// </summary>
        void RemoveAllListeners();

        /// <summary>
        /// Dispatches an event to all listeners of that event
        /// An instance of <see cref="EmptyEventData"/> is provided as data
        /// </summary>
        /// <param name="name"></param>
        void DispatchEvent(string name);

        /// <summary>
        /// Dispatches an event to all listeners of that event
        /// </summary>
        /// <typeparam name="TDataType">Event data type</typeparam>
        /// <param name="name"></param>
        /// <param name="data">Data to provide to listeners</param>
        /// <exception cref="ArgumentNullException">If name or data are null</exception>
        void DispatchEvent<TDataType>(string name, TDataType data) where TDataType : EventData;

        /// <summary>
        /// Dispatches an event to all listeners of that event
        /// The event name is inferred from the data type
        /// </summary>
        /// <typeparam name="TDataType">Event data type</typeparam>
        /// <param name="data">Data to provide to listeners</param>
        /// <exception cref="ArgumentNullException">If name or data are null</exception>
        void DispatchEvent<TDataType>(TDataType data) where TDataType : EventData;

        /// <summary>
        /// Adds a post dispatch callback
        /// Use this when adding or removing listeners or events while in an event dispatch
        /// </summary>
        /// <param name="callback"></param>
        /// <exception cref="System.InvalidOperationException">If a callback is added while not in an event dispatch</exception>
        void AddPostDispatchCallback(Delegates.PostDispatchCallback callback);
    }
}
