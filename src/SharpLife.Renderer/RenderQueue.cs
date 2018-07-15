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

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace SharpLife.Renderer
{
    public class RenderQueue : IEnumerable<Renderable>
    {
        private const int DefaultCapacity = 250;

        private readonly List<RenderItemIndex> _indices = new List<RenderItemIndex>(DefaultCapacity);
        private readonly List<Renderable> _renderables = new List<Renderable>(DefaultCapacity);

        public int Count => _renderables.Count;

        public void Clear()
        {
            _indices.Clear();
            _renderables.Clear();
        }

        public void AddRange(List<Renderable> Renderables, Vector3 viewPosition)
        {
            for (int i = 0; i < Renderables.Count; i++)
            {
                Renderable Renderable = Renderables[i];
                if (Renderable != null)
                {
                    Add(Renderable, viewPosition);
                }
            }
        }

        public void AddRange(IReadOnlyList<Renderable> Renderables, Vector3 viewPosition)
        {
            for (int i = 0; i < Renderables.Count; i++)
            {
                Renderable Renderable = Renderables[i];
                if (Renderable != null)
                {
                    Add(Renderable, viewPosition);
                }
            }
        }

        public void AddRange(IEnumerable<Renderable> Renderables, Vector3 viewPosition)
        {
            foreach (Renderable item in Renderables)
            {
                if (item != null)
                {
                    Add(item, viewPosition);
                }
            }
        }

        public void Add(Renderable item, Vector3 viewPosition)
        {
            int index = _renderables.Count;
            _indices.Add(new RenderItemIndex(item.GetRenderOrderKey(viewPosition), index));
            _renderables.Add(item);
            Debug.Assert(_renderables.IndexOf(item) == index);
        }

        public void Sort()
        {
            _indices.Sort();
        }

        public void Sort(Comparer<RenderOrderKey> keyComparer)
        {
            _indices.Sort(
                (RenderItemIndex first, RenderItemIndex second)
                    => keyComparer.Compare(first.Key, second.Key));
        }

        public void Sort(Comparer<RenderItemIndex> comparer)
        {
            _indices.Sort(comparer);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_indices, _renderables);
        }

        IEnumerator<Renderable> IEnumerable<Renderable>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<Renderable>
        {
            private readonly List<RenderItemIndex> _indices;
            private readonly List<Renderable> _Renderables;
            private int _nextItemIndex;
            private Renderable _currentItem;

            public Enumerator(List<RenderItemIndex> indices, List<Renderable> Renderables)
            {
                _indices = indices;
                _Renderables = Renderables;
                _nextItemIndex = 0;
                _currentItem = null;
            }

            public Renderable Current => _currentItem;
            object IEnumerator.Current => _currentItem;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_nextItemIndex >= _indices.Count)
                {
                    _currentItem = null;
                    return false;
                }
                else
                {
                    var currentIndex = _indices[_nextItemIndex];
                    _currentItem = _Renderables[currentIndex.ItemIndex];
                    _nextItemIndex += 1;
                    return true;
                }
            }

            public void Reset()
            {
                _nextItemIndex = 0;
                _currentItem = null;
            }
        }
    }
}
