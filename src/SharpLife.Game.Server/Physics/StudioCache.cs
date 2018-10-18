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

using SharpLife.CommandSystem.Commands;
using SharpLife.Game.Shared.Models.BSP;
using SharpLife.Game.Shared.Models.MDL;
using SharpLife.Game.Shared.Physics;
using SharpLife.Models.BSP.FileFormat;
using SharpLife.Models.MDL.FileFormat;
using SharpLife.Models.MDL.Rendering;
using SharpLife.Utility.Collections.Generic;
using SharpLife.Utility.Mathematics;
using System;
using System.Linq;
using System.Numerics;

namespace SharpLife.Game.Server.Physics
{
    public sealed class StudioCache
    {
        private readonly Models.BSP.FileFormat.Plane[] studio_planes = new Models.BSP.FileFormat.Plane[MDLConstants.MaxBones * PhysicsConstants.MaxBoxSides];
        private readonly Hull[] studio_hull = new Hull[MDLConstants.MaxBones];
        private readonly ClipNode[] studio_clipnodes = new ClipNode[PhysicsConstants.MaxBoxSides];
        private readonly int[] studio_hull_hitgroup = new int[MDLConstants.MaxBones];

        private readonly StudioModelBoneCalculator _boneCalculator = new StudioModelBoneCalculator();

        private readonly IVariable _cacheStudio;

        public StudioCache()
        {
            for (int i = 0; i < PhysicsConstants.MaxBoxSides; ++i)
            {
                studio_clipnodes[i] = new ClipNode
                {
                    PlaneIndex = i
                };

                //Alternate which child is open
                studio_clipnodes[i].Children[i % 2] = (int)Contents.Empty;
                studio_clipnodes[i].Children[(i + 1) % 2] = i + 1;
            }

            //The last child is marked different to indicate start solid
            studio_clipnodes[5].Children[0] = (int)Contents.Solid;

            var pPlanes = new Memory<Models.BSP.FileFormat.Plane>(studio_planes);

            for (var i = 0; i < studio_hull.Length; ++i)
            {
                studio_hull[i] = new Hull(0, PhysicsConstants.MaxBoxSides - 1, Vector3.Zero, Vector3.Zero, studio_clipnodes, pPlanes.Slice(0, PhysicsConstants.MaxBoxSides));
                pPlanes = pPlanes.Slice(PhysicsConstants.MaxBoxSides);
            }
        }

        private const int MaxStudioCacheEntries = 16;

        private sealed class StudioCacheEntry
        {
            public float Frame;
            public int Sequence;

            public Vector3 Angles;
            public Vector3 Origin;
            public Vector3 Size;

            public byte[] Controller = new byte[MDLConstants.MaxControllers];
            public byte[] Blending = new byte[MDLConstants.MaxBlenders];

            public StudioModel Model;

            public int StartHull;
            public int StartPlane;
            public int NumHulls;
        };

        private readonly CircularBuffer<StudioCacheEntry> _studioCache = new CircularBuffer<StudioCacheEntry>(MaxStudioCacheEntries);

        private int _currentHull;

        private int _currentPlane;

        private readonly Models.BSP.FileFormat.Plane[] _cachePlanes = new Models.BSP.FileFormat.Plane[MDLConstants.MaxBones * PhysicsConstants.MaxBoxSides];

        private readonly Hull[] _cacheHulls = new Hull[MDLConstants.MaxBones];

        private readonly int[] _cacheHullHitGroups = new int[MDLConstants.MaxBones];

        private void InitStudioCache()
        {
            _studioCache.Clear();
            _currentHull = 0;
            _currentPlane = 0;
        }

        private void FlushStudioCache()
        {
            InitStudioCache();
        }

        private StudioCacheEntry CheckStudioCache(StudioModel pModel, float frame, int sequence,
            in Vector3 angles, in Vector3 origin, in Vector3 size,
            byte[] controller, byte[] blending)
        {
            foreach (var entry in _studioCache)
            {
                if (entry != null
                    && ReferenceEquals(entry.Model, pModel)
                    && entry.Frame == frame
                    && entry.Sequence == sequence
                    && VectorUtils.VectorsEqual(entry.Angles, angles)
                    && VectorUtils.VectorsEqual(entry.Origin, origin)
                    && VectorUtils.VectorsEqual(entry.Size, size)
                    && entry.Controller.SequenceEqual(controller)
                    && entry.Blending.SequenceEqual(blending))
                {
                    return entry;
                }
            }

            return null;
        }

        private void AddToStudioCache(float frame, int sequence,
            in Vector3 angles, in Vector3 origin, in Vector3 size,
            byte[] controller, byte[] blending, StudioModel pModel, Hull[] pHulls, int numhulls)
        {
            if (numhulls + _currentHull >= MDLConstants.MaxBones)
            {
                FlushStudioCache();
            }

            var entry = new StudioCacheEntry
            {
                Frame = frame,
                Sequence = sequence,
                Angles = angles,
                Origin = origin,
                Size = size,
                Controller = controller.ToArray(),
                Blending = blending.ToArray(),
                Model = pModel,
                StartPlane = _currentPlane,
                StartHull = _currentHull,
                NumHulls = numhulls
            };

            _studioCache.Add(entry);

            Array.Copy(pHulls, 0, _cacheHulls, _currentHull, numhulls);
            Array.Copy(studio_planes, 0, _cachePlanes, _currentPlane, numhulls * PhysicsConstants.MaxBoxSides);
            Array.Copy(studio_hull_hitgroup, 0, _cacheHullHitGroups, _currentHull, numhulls);

            _currentHull += numhulls;
            _currentPlane += PhysicsConstants.MaxBoxSides * numhulls;
        }

        public Hull[] StudioHull(StudioModel pModel, float frame, int sequence,
            in Vector3 angles, in Vector3 origin, in Vector3 size,
            byte[] pcontroller, byte[] pblending,
            out int pNumHulls, bool bSkipShield)
        {
            if (_cacheStudio.Boolean)
            {
                var pCache = CheckStudioCache(pModel, frame, sequence, angles, origin, size, pcontroller, pblending);

                if (pCache != null)
                {
                    Array.Copy(_cacheHulls, pCache.StartHull, studio_hull, 0, pCache.NumHulls);
                    Array.Copy(_cachePlanes, pCache.StartPlane, studio_planes, 0, pCache.NumHulls * PhysicsConstants.MaxBoxSides);
                    Array.Copy(_cacheHullHitGroups, pCache.StartHull, studio_hull_hitgroup, 0, pCache.NumHulls);

                    pNumHulls = pCache.NumHulls;
                    return studio_hull;
                }
            }

            var angles2 = new Vector3(
                -angles.X,
                angles.Y,
                angles.Z
                );

            //TODO: pass correct values
            var bones = _boneCalculator.SetUpBones(pModel.StudioFile, 0, (uint)sequence, 0, frame, 10, new BoneData());

            var planes = new Span<Models.BSP.FileFormat.Plane>(studio_planes);

            for (var i = 0; i < pModel.StudioFile.Hitboxes.Count; ++i, planes = planes.Slice(PhysicsConstants.MaxBoxSides))
            {
                //TODO: this can result in garbage data if there are more groups after the shield group
                if (bSkipShield && i == 21)
                {
                    continue;
                }

                var hitbox = pModel.StudioFile.Hitboxes[i];

                studio_hull_hitgroup[i] = hitbox.Group;

                var transform = bones[hitbox.BoneIndex];

                //TODO: verify that this is correct
                for (int side = 0; side < PhysicsConstants.MaxBoxSides; ++side)
                {
                    var plane = planes[side];

                    plane.Type = PlaneType.AnyZ;

                    plane.Normal.X = transform.M11;
                    plane.Normal.Y = transform.M21;
                    plane.Normal.Z = transform.M31;

                    var boundary = (side % 2) == 0 ? hitbox.Max : hitbox.Min;

                    plane.Distance = boundary.Index(side / 2)
                        + (plane.Normal.X * transform.M14)
                        + (plane.Normal.Y * transform.M24)
                        + (plane.Normal.Z * transform.M34);

                    var distanceAdjust = Math.Abs(plane.Normal.X * size.X)
                        + Math.Abs(plane.Normal.Y * size.Y)
                        + Math.Abs(plane.Normal.Z * size.Z);

                    if ((side % 2) == 0)
                    {
                        plane.Distance += distanceAdjust;
                    }
                    else
                    {
                        plane.Distance -= distanceAdjust;
                    }
                }
            }

            pNumHulls = pModel.StudioFile.Hitboxes.Count;

            if (bSkipShield)
            {
                --pNumHulls;
            }

            if (_cacheStudio.Boolean)
            {
                AddToStudioCache(frame, sequence, angles, origin, size, pcontroller, pblending, pModel, studio_hull, pNumHulls);
            }

            return studio_hull;
        }

        public int HitgroupForStudioHull(int index)
        {
            return studio_hull_hitgroup[index];
        }
    }
}
