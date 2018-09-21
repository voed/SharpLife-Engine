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
    public class RenderQueue<TRenderable> : IEnumerable<TRenderable>
        where TRenderable : class, IRenderable
    {
        private const int DefaultCapacity = 250;

        private readonly List<RenderItemIndex> _indices = new List<RenderItemIndex>(DefaultCapacity);
        private readonly List<TRenderable> _renderables = new List<TRenderable>(DefaultCapacity);

        public int Count => _renderables.Count;

        public void Clear()
        {
            _indices.Clear();
            _renderables.Clear();
        }

        public void AddRange(List<TRenderable> Renderables, Vector3 viewPosition)
        {
            for (int i = 0; i < Renderables.Count; i++)
            {
                var Renderable = Renderables[i];
                if (Renderable != null)
                {
                    Add(Renderable, viewPosition);
                }
            }
        }

        public void AddRange(IReadOnlyList<TRenderable> Renderables, Vector3 viewPosition)
        {
            for (int i = 0; i < Renderables.Count; i++)
            {
                var Renderable = Renderables[i];
                if (Renderable != null)
                {
                    Add(Renderable, viewPosition);
                }
            }
        }

        public void AddRange(IEnumerable<TRenderable> Renderables, Vector3 viewPosition)
        {
            foreach (var item in Renderables)
            {
                if (item != null)
                {
                    Add(item, viewPosition);
                }
            }
        }

        public void Add(TRenderable item, Vector3 viewPosition)
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

        IEnumerator<TRenderable> IEnumerable<TRenderable>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<TRenderable>
        {
            private readonly List<RenderItemIndex> _indices;
            private readonly List<TRenderable> _Renderables;
            private int _nextItemIndex;
            private TRenderable _currentItem;

            public Enumerator(List<RenderItemIndex> indices, List<TRenderable> Renderables)
            {
                _indices = indices;
                _Renderables = Renderables;
                _nextItemIndex = 0;
                _currentItem = null;
            }

            public TRenderable Current => _currentItem;
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
