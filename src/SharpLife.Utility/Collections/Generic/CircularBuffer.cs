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
using System.Collections;
using System.Collections.Generic;

namespace SharpLife.Utility.Collections.Generic
{
    /// <summary>
    /// A circular buffer that stores objects of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class CircularBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _list;
        private int _nextIndex;
        private int _currentIndex = -1;

        /// <summary>
        /// Total capacity of this buffer
        /// </summary>
        public int Capacity => _list.Length;

        /// <summary>
        /// Current number of elements in this buffer
        /// </summary>
        public int Count => IsFull ? Capacity : _nextIndex;

        /// <summary>
        /// Whether there are no elements in the buffer
        /// </summary>
        public bool IsEmpty => !IsFull && _nextIndex == 0;

        /// <summary>
        /// Whether the maximum number of elements are in the buffer
        /// If true, adding an element will overwrite the oldest element
        /// <see cref="Add(T)"/>
        /// </summary>
        public bool IsFull { get; private set; }

        /// <summary>
        /// Gets the current element
        /// Equivalent to calling <see cref="Get(int)"/> with index 0
        /// Will throw if no elements are in the buffer
        /// </summary>
        public T Current => _list[_currentIndex];

        /// <summary>
        /// Gets or sets an element by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get => _list[GetRealIndex(index)];
            set => _list[GetRealIndex(index)] = value;
        }

        /// <summary>
        /// Creates a circular buffer with the given capacity
        /// </summary>
        /// <param name="capacity"></param>
        public CircularBuffer(int capacity)
        {
            _list = new T[capacity];
        }

        /// <summary>
        /// Adds an element
        /// If the buffer is full, the oldest element will be overwritten
        /// <see cref="IsFull"/>
        /// </summary>
        /// <param name="item"></param>
        /// <returns>The old value at the position, if there was one</returns>
        public T Add(T item)
        {
            var oldValue = _list[_nextIndex];

            _list[_nextIndex] = item;

            _currentIndex = _nextIndex;

            if (_nextIndex + 1 >= Capacity)
            {
                _nextIndex = 0;

                IsFull = true;
            }
            else
            {
                ++_nextIndex;
            }

            return oldValue;
        }

        /// <summary>
        /// Removes all elements and resets this buffer to its original state
        /// </summary>
        public void Clear()
        {
            Array.Fill(_list, default);
            _nextIndex = 0;
            _currentIndex = -1;
            IsFull = false;
        }

        private int GetRealIndex(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (IsFull)
            {
                return (_nextIndex + index) % Capacity;
            }

            return index;
        }

        /// <summary>
        /// Gets an enumerator that allows enumerating the buffer's elements in the order that they were added
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            T[] destination;

            if (IsFull)
            {
                destination = new T[_list.Length];

                //Copy the buffer in the order that the elements were inserted
                var firstPart = Capacity - _nextIndex;

                Array.Copy(_list, _nextIndex, destination, 0, firstPart);

                if (_nextIndex != 0)
                {
                    Array.Copy(_list, 0, destination, firstPart, Capacity - firstPart);
                }
            }
            else
            {
                destination = new T[_nextIndex];
                Array.Copy(_list, 0, destination, 0, _nextIndex);
            }

            return ((IEnumerable<T>)destination).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
