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

namespace SharpLife.Engine.Client.Host
{
    /// <summary>
    /// The client host, responsible for all engine level client operations
    /// </summary>
    public interface IEngineClientHost
    {
        void PostInitialize();

        void Shutdown();

        void Update(float deltaSeconds);

        void Draw();
    }
}
