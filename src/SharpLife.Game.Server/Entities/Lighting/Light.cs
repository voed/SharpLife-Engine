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

using SharpLife.Game.Shared.Entities;
using SharpLife.Game.Shared.Entities.MetaData;

namespace SharpLife.Game.Server.Entities.Lighting
{
    /// <summary>
    /// Non-displayed light.
    /// Default light value is 300
    /// Default style is 0
    /// If targeted, it will toggle between on or off.
    /// </summary>
    [LinkEntityToClass("light")]
    //TODO: support multiple links
    //[LinkEntityToClass("light_spot")]
    [Networkable]
    public class Light : NetworkedEntity
    {
        private int _style;

        private string _pattern;

        public override bool KeyValue(string key, string value)
        {
            if (key == "style")
            {
                int.TryParse(value, out _style);
                return true;
            }
            else if (key == "pitch")
            {
                var angles = Angles;

                angles.X = KeyValueUtils.ParseFloat(value);

                Angles = angles;

                return true;
            }
            else if (key == "pattern")
            {
                _pattern = value;
                return true;
            }

            return base.KeyValue(key, value);
        }

        //TODO: implement
    }
}
