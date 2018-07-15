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

using ImGuiNET;
using SDL2;
using SharpLife.Input;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace SharpLife.Renderer
{
    public class Camera : IUpdateable
    {
        private float _fov = 1f;
        private float _near = 1f;
        private float _far = 1000f;

        private Matrix4x4 _viewMatrix;
        private Matrix4x4 _projectionMatrix;

        private Vector3 _position = new Vector3(0, 3, 0);
        private Vector3 _lookDirection = new Vector3(0, -.3f, -1f);
        private float _moveSpeed = 10.0f;

        private float _yaw;
        private float _pitch;

        private Vector2 _previousMousePos;
        private IInputSystem _inputSystem;
        private GraphicsDevice _gd;
        private bool _useReverseDepth;
        private float _windowWidth;
        private float _windowHeight;

        public event Action<Matrix4x4> ProjectionChanged;
        public event Action<Matrix4x4> ViewChanged;

        public Camera(IInputSystem inputSystem, GraphicsDevice gd, float width, float height)
        {
            _inputSystem = inputSystem;
            _gd = gd;
            _useReverseDepth = gd.IsDepthRangeZeroToOne;
            _windowWidth = width;
            _windowHeight = height;
            UpdatePerspectiveMatrix();
            UpdateViewMatrix();
        }

        public void UpdateBackend(GraphicsDevice gd)
        {
            _gd = gd;
            _useReverseDepth = gd.IsDepthRangeZeroToOne;
            UpdatePerspectiveMatrix();
        }

        public Matrix4x4 ViewMatrix => _viewMatrix;
        public Matrix4x4 ProjectionMatrix => _projectionMatrix;

        public Vector3 Position { get => _position; set { _position = value; UpdateViewMatrix(); } }
        public Vector3 LookDirection => _lookDirection;

        public float FarDistance => _far;

        public float FieldOfView => _fov;
        public float NearDistance => _near;

        public float AspectRatio => _windowWidth / _windowHeight;

        public float Yaw { get => _yaw; set { _yaw = value; UpdateViewMatrix(); } }
        public float Pitch { get => _pitch; set { _pitch = value; UpdateViewMatrix(); } }

        public void Update(float deltaSeconds)
        {
            float sprintFactor = _inputSystem.Snapshot.IsKeyDown(SDL.SDL_Keycode.SDLK_LCTRL)
                ? 0.1f
                : _inputSystem.Snapshot.IsKeyDown(SDL.SDL_Keycode.SDLK_LSHIFT)
                    ? 2.5f
                    : 1f;
            Vector3 motionDir = Vector3.Zero;
            if (_inputSystem.Snapshot.IsKeyDown(SDL.SDL_Keycode.SDLK_a))
            {
                motionDir += -Vector3.UnitX;
            }
            if (_inputSystem.Snapshot.IsKeyDown(SDL.SDL_Keycode.SDLK_d))
            {
                motionDir += Vector3.UnitX;
            }
            if (_inputSystem.Snapshot.IsKeyDown(SDL.SDL_Keycode.SDLK_w))
            {
                motionDir += -Vector3.UnitZ;
            }
            if (_inputSystem.Snapshot.IsKeyDown(SDL.SDL_Keycode.SDLK_s))
            {
                motionDir += Vector3.UnitZ;
            }
            if (_inputSystem.Snapshot.IsKeyDown(SDL.SDL_Keycode.SDLK_q))
            {
                motionDir += -Vector3.UnitY;
            }
            if (_inputSystem.Snapshot.IsKeyDown(SDL.SDL_Keycode.SDLK_e))
            {
                motionDir += Vector3.UnitY;
            }

            if (motionDir != Vector3.Zero)
            {
                Quaternion lookRotation = Quaternion.CreateFromYawPitchRoll(Yaw, Pitch, 0f);
                motionDir = Vector3.Transform(motionDir, lookRotation);
                _position += motionDir * _moveSpeed * sprintFactor * deltaSeconds;
                UpdateViewMatrix();
            }

            Vector2 mouseDelta = _inputSystem.Snapshot.MousePosition - _previousMousePos;
            _previousMousePos = _inputSystem.Snapshot.MousePosition;

            if (!ImGui.IsAnyWindowHovered() && (_inputSystem.Snapshot.IsMouseDown(MouseButton.Left) || _inputSystem.Snapshot.IsMouseDown(MouseButton.Right)))
            {
                Yaw += -mouseDelta.X * 0.01f;
                Pitch += -mouseDelta.Y * 0.01f;
                Pitch = Math.Clamp(Pitch, -1.55f, 1.55f);

                UpdateViewMatrix();
            }
        }

        public void WindowResized(float width, float height)
        {
            _windowWidth = width;
            _windowHeight = height;
            UpdatePerspectiveMatrix();
        }

        private void UpdatePerspectiveMatrix()
        {
            _projectionMatrix = Util.CreatePerspective(
                _gd,
                _useReverseDepth,
                _fov,
                _windowWidth / _windowHeight,
                _near,
                _far);
            ProjectionChanged?.Invoke(_projectionMatrix);
        }

        private void UpdateViewMatrix()
        {
            Quaternion lookRotation = Quaternion.CreateFromYawPitchRoll(Yaw, Pitch, 0f);
            Vector3 lookDir = Vector3.Transform(-Vector3.UnitZ, lookRotation);
            _lookDirection = lookDir;
            _viewMatrix = Matrix4x4.CreateLookAt(_position, _position + _lookDirection, Vector3.UnitY);
            ViewChanged?.Invoke(_viewMatrix);
        }

        public CameraInfo GetCameraInfo() => new CameraInfo
        {
            CameraPosition_WorldSpace = _position,
            CameraLookDirection = _lookDirection
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraInfo
    {
        public Vector3 CameraPosition_WorldSpace;
        private float _padding1;
        public Vector3 CameraLookDirection;
        private float _padding2;
    }
}
