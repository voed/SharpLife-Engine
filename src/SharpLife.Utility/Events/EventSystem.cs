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

namespace SharpLife.Utility.Events
{
    public class EventSystem : IEventSystem
    {
        public bool IsDispatching => _inDispatchCount > 0;

        private readonly Dictionary<string, EventMetaData> _events = new Dictionary<string, EventMetaData>();

        /// <summary>
        /// Keeps track of our nested dispatch count
        /// </summary>
        private int _inDispatchCount;

        private readonly List<Delegates.PostDispatchCallback> _postDispatchCallbacks = new List<Delegates.PostDispatchCallback>();

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }
        }

        public bool HasEvent(string name)
        {
            ValidateName(name);

            return _events.ContainsKey(name);
        }

        public void RegisterEvent(string name)
        {
            RegisterEvent<EmptyEventData>(name);
        }

        public void RegisterEvent<TDataType>(string name) where TDataType : EventData
        {
            ValidateName(name);

            if (IsDispatching)
            {
                throw new InvalidOperationException("Cannot register events while dispatching");
            }

            if (HasEvent(name))
            {
                throw new ArgumentException($"Event \"{name}\" has already been registered");
            }

            _events.Add(name, new EventMetaData(name, typeof(TDataType)));
        }

        public void UnregisterEvent(string name)
        {
            ValidateName(name);

            if (IsDispatching)
            {
                throw new InvalidOperationException("Cannot unregister events while dispatching");
            }

            _events.Remove(name);
        }

        public void AddListener(string name, Delegates.Listener listener)
        {
            AddListener<EmptyEventData>(name, listener);
        }

        private void InternalAddListener<TDataType>(string name, Invoker invoker) where TDataType : EventData
        {
            if (!_events.TryGetValue(name, out var metaData))
            {
                throw new InvalidOperationException($"Event \"{name}\" has not been registered");
            }

            var dataType = typeof(TDataType);

            if (!metaData.DataType.IsAssignableFrom(dataType))
            {
                throw new InvalidOperationException($"Event \"{name}\" has data type {metaData.DataType.FullName}\", not compatible with data type \"{dataType.FullName}\"");
            }

            metaData.Listeners.Add(invoker);
        }

        public void AddListener<TDataType>(string name, Delegates.Listener listener) where TDataType : EventData
        {
            ValidateName(name);

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (IsDispatching)
            {
                throw new InvalidOperationException("Cannot add listeners while dispatching");
            }

            InternalAddListener<TDataType>(name, new PlainInvoker(listener));
        }

        public void AddListener<TDataType>(string name, Delegates.DataListener<TDataType> listener) where TDataType : EventData
        {
            ValidateName(name);

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (IsDispatching)
            {
                throw new InvalidOperationException("Cannot add listeners while dispatching");
            }

            InternalAddListener<TDataType>(name, new DataInvoker<TDataType>(listener));
        }

        public void AddListeners(string[] names, Delegates.Listener listener)
        {
            AddListeners<EmptyEventData>(names, listener);
        }

        public void AddListeners<TDataType>(string[] names, Delegates.Listener listener) where TDataType : EventData
        {
            if (names == null)
            {
                throw new ArgumentNullException(nameof(names));
            }

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (IsDispatching)
            {
                throw new InvalidOperationException("Cannot add listeners while dispatching");
            }

            foreach (var name in names)
            {
                AddListener<TDataType>(name, listener);
            }
        }

        public void RemoveListeners(string name)
        {
            ValidateName(name);

            if (IsDispatching)
            {
                throw new InvalidOperationException("Cannot remove listeners while dispatching");
            }

            if (_events.TryGetValue(name, out var metaData))
            {
                metaData.Listeners.Clear();
            }
        }

        public void RemoveListener(Delegates.Listener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (IsDispatching)
            {
                throw new InvalidOperationException("Cannot remove listeners while dispatching");
            }

            foreach (var metaData in _events)
            {
                metaData.Value.Listeners.RemoveAll(invoker => ReferenceEquals(invoker.Target, listener));
            }
        }

        public void RemoveListener(string name, Delegates.Listener listener)
        {
            ValidateName(name);

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (IsDispatching)
            {
                throw new InvalidOperationException("Cannot remove listeners while dispatching");
            }

            if (_events.TryGetValue(name, out var metaData))
            {
                var index = metaData.Listeners.FindIndex(invoker => invoker.Delegate.Equals(listener));

                if (index != -1)
                {
                    metaData.Listeners.RemoveAt(index);
                }
            }
        }

        public void RemoveListener(object listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (IsDispatching)
            {
                throw new InvalidOperationException("Cannot remove listeners while dispatching");
            }

            foreach (var metaData in _events)
            {
                metaData.Value.Listeners.RemoveAll(delegateListener => delegateListener.Target == listener);
            }
        }

        public void RemoveAllListeners()
        {
            if (IsDispatching)
            {
                throw new InvalidOperationException("Cannot remove listeners while dispatching");
            }

            foreach (var metaData in _events)
            {
                metaData.Value.Listeners.Clear();
            }
        }

        public void DispatchEvent(string name)
        {
            DispatchEvent(name, EmptyEventData.Instance);
        }

        public void DispatchEvent<TDataType>(string name, TDataType data) where TDataType : EventData
        {
            ValidateName(name);

            if (_events.TryGetValue(name, out var metaData))
            {
                var @event = new Event(this, name, data);

                ++_inDispatchCount;

                for (var i = 0; i < metaData.Listeners.Count; ++i)
                {
                    metaData.Listeners[i].Invoke(@event);
                }

                --_inDispatchCount;

                if (_inDispatchCount == 0 && _postDispatchCallbacks.Count > 0)
                {
                    _postDispatchCallbacks.ForEach(callback => callback(this));
                    _postDispatchCallbacks.Clear();
                    //Avoid wasting memory, since this is a rarely used operation
                    _postDispatchCallbacks.Capacity = 0;
                }
            }
        }

        public void AddPostDispatchCallback(Delegates.PostDispatchCallback callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (!IsDispatching)
            {
                throw new InvalidOperationException("Can only add post dispatch callbacks while dispatching events");
            }

            _postDispatchCallbacks.Add(callback);
        }
    }
}
