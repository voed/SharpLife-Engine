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

namespace SharpLife.Renderer.BSP
{
    /// <summary>
    /// Contains the light styles used by named lights
    /// </summary>
    public class LightStyles
    {
        private const int NormalLightValue = 264;
        private const int UnstyledLightValue = 256;
        public const int InvalidLightValue = -1;

        private struct LightStyle
        {
            public string StylePattern;
            public int Value;
        }

        private readonly LightStyle[] _lightStyles = new LightStyle[BSPConstants.MaxLightStyles];

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
            for (var i = 0; i < _lightStyles.Length; ++i)
            {
                _lightStyles[i].StylePattern = string.Empty;
                _lightStyles[i].Value = NormalLightValue;
            }
        }

        public int GetStyleValue(int style)
        {
            if (style < 0 || style >= _lightStyles.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(style));
            }

            return _lightStyles[style].Value;
        }

        public void SetStyle(int style, string value)
        {
            if (style < 0 || style >= _lightStyles.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(style));
            }

            //TODO: unlimited number of controlled lights

            _lightStyles[style].StylePattern = value ?? throw new ArgumentNullException(nameof(value));
        }

        public void AnimateLights(IEngineTime engineTime)
        {
            if (engineTime == null)
            {
                throw new ArgumentNullException(nameof(engineTime));
            }

            var offset = (int)(10.0 * engineTime.ElapsedTime);

            for (var i = 0; i < _lightStyles.Length; ++i)
            {
                ref var style = ref _lightStyles[i];

                //Styles can be empty if it hasn't been set yet
                var value = style.StylePattern.Length > 0 ? 22 * (style.StylePattern[offset % style.StylePattern.Length] - 'a') : UnstyledLightValue;

                style.Value = Math.Min(LightScale * value / 256, byte.MaxValue);
            }
        }
    }
}
