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

using SharpLife.Engine.Shared.Utility;
using SharpLife.FileFormats.BSP;
using System;
using System.Collections.Generic;

namespace SharpLife.Renderer.BSP
{
    /// <summary>
    /// Contains the light styles used by named lights
    /// </summary>
    public class LightStyles
    {
        private const int NormalLightValue = 264;
        private const int UnstyledLightValue = 256;

        private const int MaxControlledStyles = 64;

        private readonly List<string> _lightStyles = new List<string>(MaxControlledStyles);

        private readonly int[] _lightStyleValues = new int[BSPConstants.MaxLightStyles];

        //TODO: make configurable
        private static readonly int LightScale = (int)((Math.Pow(2.0f, 1.0f / 2.5f) * 256.0f) + 0.5f);

        public LightStyles()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes all styles to their default values
        /// </summary>
        public void Initialize()
        {
            _lightStyles.Clear();
            for (var i = 0; i < MaxControlledStyles; ++i)
            {
                _lightStyles.Add(string.Empty);
            }

            Array.Fill(_lightStyleValues, NormalLightValue);
        }

        public int GetStyleValue(int style)
        {
            if (style < 0 || style >= BSPConstants.MaxLightStyles)
            {
                throw new ArgumentOutOfRangeException(nameof(style));
            }

            return _lightStyleValues[style];
        }

        public void SetStyle(int style, string value)
        {
            if (style < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(style));
            }

            if (style >= MaxControlledStyles)
            {
                throw new ArgumentOutOfRangeException(nameof(style));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            //TODO: unlimited number of controlled lights
            //if (_lightStyles.Count <= style)
            //{
            //    _lightStyles.Capacity = style + 1;
            //
            //    while (_lightStyles.Count <= style)
            //    {
            //        _lightStyles.Add(string.Empty);
            //    }
            //}

            _lightStyles[style] = value;
        }

        public void AnimateLights(IEngineTime engineTime)
        {
            if (engineTime == null)
            {
                throw new ArgumentNullException(nameof(engineTime));
            }

            var offset = (int)(10.0 * engineTime.ElapsedTime);

            for (var i = 0; i < _lightStyles.Count; ++i)
            {
                var style = _lightStyles[i];

                //Styles can be empty if it hasn't been set yet
                var value = style.Length > 0 ? 22 * (style[offset % style.Length] - 'a') : UnstyledLightValue;

                _lightStyleValues[i] = Math.Min(LightScale * value / 256, byte.MaxValue);
            }
        }
    }
}
