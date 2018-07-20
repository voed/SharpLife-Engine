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
using System.Numerics;

namespace SharpLife.Renderer
{
    public class Transform
    {
        private Vector3 _position;
        private Quaternion _rotation = Quaternion.Identity;
        private Vector3 _scale = Vector3.One;

        public Vector3 Position { get => _position; set { _position = value; TransformChanged?.Invoke(); } }
        public Quaternion Rotation { get => _rotation; set { _rotation = value; TransformChanged?.Invoke(); } }
        public Vector3 Scale { get => _scale; set { _scale = value; TransformChanged?.Invoke(); } }

        public event Action TransformChanged;

        public Vector3 Forward => Vector3.Transform(-Vector3.UnitZ, _rotation);

        public Matrix4x4 GetTransformMatrix()
        {
            return Matrix4x4.CreateScale(_scale)
                * Matrix4x4.CreateFromQuaternion(_rotation)
                * Matrix4x4.CreateTranslation(Position);
        }
    }
}
