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

using Serilog;
using SharpLife.CommandSystem;
using SharpLife.CommandSystem.Commands;
using SharpLife.CommandSystem.Commands.VariableFilters;
using SharpLife.Game.Server.Entities;
using SharpLife.Game.Server.Entities.Animation;
using SharpLife.Game.Server.Entities.EntityList;
using SharpLife.Game.Shared.Entities;
using SharpLife.Game.Shared.Models;
using SharpLife.Game.Shared.Models.BSP;
using SharpLife.Game.Shared.Models.MDL;
using SharpLife.Game.Shared.Physics;
using SharpLife.Models.BSP.FileFormat;
using SharpLife.Models.MDL.FileFormat;
using SharpLife.Utility;
using SharpLife.Utility.Mathematics;
using System;
using System.Numerics;

namespace SharpLife.Game.Server.Physics
{
    /// <summary>
    /// Manages the physics state
    /// </summary>
    public sealed class GamePhysics
    {
        private readonly ILogger _logger;

        private readonly ITime _engineTime;

        private readonly SnapshotTime _gameTime;

        private readonly ServerEntities _entities;

        private readonly ServerEntityList _entityList;

        private readonly BSPModel _worldModel;

        /// <summary>
        /// Binary tree that divides the world into sections for fast lookups
        /// </summary>
        private readonly AreaNode[] _areaNodes = new AreaNode[PhysicsConstants.MaxAreaNodes];

        private int _areaNodeCount;

        private AreaNode HeadAreaNode => _areaNodes[0];

        private readonly StudioCache _studioCache = new StudioCache();

        //TODO: create
        private readonly IVariable _sv_clienttrace;

        private GroupOperation _groupOp;

        public uint GroupMask { get; set; }

        private bool _touchLinkSemaphore;

        //TODO: get rid of the global flags state and pass it into trace functions
        public TraceFlags TraceFlags { get; set; }

        private readonly ClipNode[] box_clipnodes = new ClipNode[PhysicsConstants.MaxBoxSides];

        private readonly Models.BSP.FileFormat.Plane[] box_planes = new Models.BSP.FileFormat.Plane[PhysicsConstants.MaxBoxSides];

        private readonly Hull[] box_hull;

        private static readonly byte[] _studioHullControllers = new byte[MDLConstants.MaxControllers]
        {
            127,
            127,
            127,
            127,
            127,
            127,
            127,
            127
        };

        private readonly byte[] _studioHullBlenders = new byte[MDLConstants.MaxBlenders]
        {
            0,
            0
        };

        public GamePhysics(ILogger logger,
            ITime engineTime, SnapshotTime gameTime,
            ServerEntities entities, ServerEntityList entityList,
            BSPModel worldModel,
            ICommandContext commandContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _engineTime = engineTime ?? throw new ArgumentNullException(nameof(engineTime));
            _gameTime = gameTime ?? throw new ArgumentNullException(nameof(gameTime));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _entityList = entityList ?? throw new ArgumentNullException(nameof(entityList));
            _worldModel = worldModel ?? throw new ArgumentNullException(nameof(worldModel));

            //TODO: need to reset this on map spawn for singleplayer
            //TODO: mark as server cvar
            _sv_clienttrace = commandContext.RegisterVariable(
                new VariableInfo("sv_clienttrace")
                .WithHelpInfo("Scale multiplier for trace lines ran against studio models")
                .WithValue(1)
                .WithNumberFilter());

            InitBoxHull();

            box_hull = new Hull[1]
            {
                new Hull(0, PhysicsConstants.MaxBoxSides, Vector3.Zero, Vector3.Zero, box_clipnodes, new Memory<Models.BSP.FileFormat.Plane>(box_planes))
            };

            CreateAreaNode(0, ref _worldModel.SubModel.Mins, ref _worldModel.SubModel.Maxs);
        }

        public bool TestGroupOperation(uint lhsMask, uint rhsMask)
        {
            switch (_groupOp)
            {
                case GroupOperation.And: return (lhsMask & rhsMask) != 0;
                case GroupOperation.Nand: return (lhsMask & rhsMask) == 0;

                default: throw new InvalidOperationException("Unknown group operation type");
            }
        }

        private void InitBoxHull()
        {
            for (var i = 0; i < box_clipnodes.Length; ++i)
            {
                box_clipnodes[i] = new ClipNode
                {
                    PlaneIndex = i
                };

                var baseIndex = (i % 2) == 0 ? 0 : 1;

                box_clipnodes[i].Children[baseIndex] = (int)Contents.Empty;
                box_clipnodes[i].Children[1 - baseIndex] = i + 1;
            }

            for (var i = 0; i < box_planes.Length; ++i)
            {
                box_planes[i] = new Models.BSP.FileFormat.Plane
                {
                    Type = (PlaneType)(i / 2)
                };

                box_planes[i].Normal.Index(i / 2, 1);
            }
        }

        private AreaNode CreateAreaNode(int depth, ref Vector3 mins, ref Vector3 maxs)
        {
            var node = _areaNodes[_areaNodeCount++] = new AreaNode();

            if (depth == 4)
            {
                node.Axis = -1;
                node.Children[1] = null;
                node.Children[0] = null;
            }
            else
            {
                node.Axis = (maxs.X - mins.X <= maxs.Y - mins.Y) ? 1 : 0;
                node.Distance = (maxs.Index(node.Axis) + mins.Index(node.Axis)) * 0.5f;

                var mins1 = mins;
                var mins2 = mins;
                var maxs1 = maxs;
                var maxs2 = maxs;

                mins2.Index(node.Axis, node.Distance);
                maxs1.Index(node.Axis, node.Distance);

                node.Children[0] = CreateAreaNode(depth + 1, ref mins2, ref maxs2);
                node.Children[1] = CreateAreaNode(depth + 1, ref mins1, ref maxs1);
            }

            return node;
        }

        private void FindTouchedLeafs(BaseEntity ent, BaseNode node, ref int topnode)
        {
            if (node.Contents == Contents.Solid)
            {
                return;
            }

            if (node.Contents < Contents.Node)
            {
                //TODO: use a more efficient way to get the index
                ent.PhysicsState.AddLeafNumber((short)_worldModel.BSPFile.Leaves.IndexOf((Leaf)node));
                return;
            }

            var currentNode = (Node)node;

            var result = PhysicsUtils.BoxOnPlaneSide(ref ent._absMin, ref ent._absMax, currentNode.Plane);

            if (result == BoxOnPlaneSideResult.CrossesPlane && topnode == -1)
            {
                //TODO: use a more efficient way to get the index
                topnode = _worldModel.BSPFile.Nodes.IndexOf(currentNode);
            }

            if ((result & BoxOnPlaneSideResult.InFront) != 0)
            {
                FindTouchedLeafs(ent, currentNode.Children[0], ref topnode);
            }

            if ((result & BoxOnPlaneSideResult.Behind) != 0)
            {
                FindTouchedLeafs(ent, currentNode.Children[1], ref topnode);
            }
        }

        private Hull HullForBsp(BaseEntity ent, in Vector3 mins, in Vector3 maxs, out Vector3 offset)
        {
            var model = ent.Model;

            if (model == null)
            {
                throw new InvalidOperationException($"Hit a {ent.ClassName} with no model");
            }

            if (!(model is BSPModel bspModel))
            {
                throw new InvalidOperationException($"Hit a {ent.ClassName} with wrong model type ({model.GetType().Name}:{model.Name})");
            }

            var width = maxs.X - mins.X;

            Hull result;

            if (width <= 8.0)
            {
                result = bspModel.Hulls[0];
                offset = bspModel.Hulls[0].ClipMins;
            }
            else
            {
                result = bspModel.Hulls[2];

                if (width <= 36.0)
                {
                    result = bspModel.Hulls[1];

                    if (maxs.Z - mins.Z <= 36.0)
                    {
                        result = bspModel.Hulls[3];
                    }
                }

                offset = result.ClipMins - mins;
            }

            offset += ent.Origin;

            return result;
        }

        public Contents HullPointContents(Hull hull, int num, ref Vector3 p)
        {
            int i;

            for (i = num; i >= 0;)
            {
                if (hull.FirstClipNode > i || hull.LastClipNode < i)
                {
                    throw new InvalidOperationException("HullPointContents: bad node number");
                }

                var pNode = hull.ClipNodes[i];

                var pPlane = hull.Planes.Span[pNode.PlaneIndex];

                var dot = (pPlane.Type > PlaneType.Z ? Vector3.Dot(pPlane.Normal, p) : p.Index((int)pPlane.Type)) - pPlane.Distance;

                if (dot >= 0.0)
                {
                    i = pNode.Children[0];
                }
                else
                {
                    i = pNode.Children[1];
                }
            }

            return (Contents)i;
        }

        private Contents LinkContents(AreaNode node, ref Vector3 pos)
        {
            foreach (var entity in node.Solids)
            {
                if (entity.Solid != Solid.Not)
                {
                    continue;
                }

                if (entity.PhysicsState.GroupInfo != 0 && !TestGroupOperation(entity.PhysicsState.GroupInfo, GroupMask))
                {
                    continue;
                }

                if (!(entity.Model is BSPModel bspModel))
                {
                    continue;
                }

                //TODO: refactor into bounding box intersection test
                if (entity.AbsMin.X > pos.X
                        || entity.AbsMin.Y > pos.Y
                        || entity.AbsMin.Z > pos.Z
                        || pos.X > entity.AbsMax.X
                        || pos.Y > entity.AbsMax.Y
                        || pos.Z > entity.AbsMax.Z)
                {
                    continue;
                }

                if ((int)entity.Contents < -100 || (int)entity.Contents > 100)
                {
                    _logger.Debug("Invalid contents on trigger field: {0}", entity.ClassName);
                }

                var hull = HullForBsp(entity, Vector3.Zero, Vector3.Zero, out var offset);
                var localPosition = pos - offset;

                if (HullPointContents(hull, hull.FirstClipNode, ref localPosition) != Contents.Empty)
                {
                    return entity.Contents;
                }
            }

            if (node.Axis != -1)
            {
                if (pos.Index(node.Axis) > node.Distance)
                {
                    return LinkContents(node.Children[0], ref pos);
                }

                if (pos.Index(node.Axis) < node.Distance)
                {
                    return LinkContents(node.Children[1], ref pos);
                }
            }

            return Contents.Empty;
        }

        public Contents PointContents(ref Vector3 p)
        {
            var contents = HullPointContents(_worldModel.Hulls[0], 0, ref p);

            if (contents == Contents.Solid)
            {
                return Contents.Solid;
            }

            //Convert current to regular water
            if (contents <= Contents.Current0)
            {
                contents = Contents.Water;
            }

            var result = LinkContents(HeadAreaNode, ref p);

            if (result != Contents.Empty)
            {
                return result;
            }

            return contents;
        }

        private void TouchLinks(BaseEntity ent, AreaNode node)
        {
            foreach (var touched in node.Triggers)
            {
                if (ReferenceEquals(ent, touched))
                {
                    continue;
                }

                if (touched.PhysicsState.GroupInfo != 0
                    && ent.PhysicsState.GroupInfo != 0
                    && !TestGroupOperation(touched.PhysicsState.GroupInfo, ent.PhysicsState.GroupInfo))
                {
                    continue;
                }

                if (touched.Solid != Solid.Trigger)
                {
                    continue;
                }

                //TODO: refactor into bounding box intersection test
                if (ent.AbsMin.X > touched.AbsMax.X
                        || ent.AbsMin.Y > touched.AbsMax.Y
                        || ent.AbsMin.Z > touched.AbsMax.Z
                        || touched.AbsMin.X > ent.AbsMax.X
                        || touched.AbsMin.Y > ent.AbsMax.Y
                        || touched.AbsMin.Z > ent.AbsMax.Z)
                {
                    continue;
                }

                if (touched.Model is BSPModel)
                {
                    var hull = HullForBsp(touched, ent.Mins, ent.Maxs, out var offset);

                    var localPosition = ent.Origin - offset;

                    if (HullPointContents(hull, hull.FirstClipNode, ref localPosition) != Contents.Solid)
                    {
                        continue;
                    }
                }

                _gameTime.ElapsedTime = _engineTime.ElapsedTime;
                touched.Touch(ent);
            }

            if (node.Axis != -1)
            {
                if (ent._absMax.Index(node.Axis) > node.Distance)
                {
                    TouchLinks(ent, node.Children[0]);
                }

                if (node.Distance > ent._absMin.Index(node.Axis))
                {
                    TouchLinks(ent, node.Children[1]);
                }
            }
        }

        public void UnlinkEdict(BaseEntity ent)
        {
            if (ent.PhysicsState.Area != null)
            {
                //TODO: optimize
                ent.PhysicsState.Area.Triggers.Remove(ent);
                ent.PhysicsState.Area.Solids.Remove(ent);
                ent.PhysicsState.Area = null;
            }
        }

        public void LinkEdict(BaseEntity ent, bool touchTriggers)
        {
            UnlinkEdict(ent);

            if (!ReferenceEquals(_entities.World, ent) && !ent.PendingDestruction)
            {
                ent.SetAbsBox();

                if (ent.MoveType == MoveType.Follow && _entityList.GetEntity(ent.AimEntity) != null)
                {
                    var aimEnt = _entityList.GetEntity(ent.AimEntity);

                    ent.PhysicsState.CopyNodeStateFrom(aimEnt.PhysicsState);
                }
                else
                {
                    ent.PhysicsState.ClearNodeState();

                    if (ent.Model != null)
                    {
                        int topNode = -1;

                        FindTouchedLeafs(ent, _worldModel.BSPFile.Nodes[0], ref topNode);

                        if (ent.PhysicsState.LeafCount > PhysicsConstants.MaxLeafs)
                        {
                            ent.PhysicsState.MarkLeafCountOverflowed(topNode);
                        }
                    }
                }

                if (ent.Solid != Solid.Not)
                {
                    if (ent.Solid == Solid.BSP && ent.Model == null)
                    {
                        _logger.Debug($"Inserted {ent.ClassName} with no model");
                        return;
                    }
                }
                else if (ent.Contents >= Contents.Empty)
                {
                    return;
                }

                var i = HeadAreaNode;

                while (i.Axis != -1)
                {
                    if (ent._absMin.Index(i.Axis) > i.Distance)
                    {
                        i = i.Children[0];
                    }
                    else if (ent._absMax.Index(i.Axis) < i.Distance)
                    {
                        i = i.Children[1];
                    }
                    else
                    {
                        break;
                    }
                }

                if (ent.Solid == Solid.Trigger)
                {
                    i.Triggers.Add(ent);
                }
                else
                {
                    i.Solids.Add(ent);
                }

                if (touchTriggers && !_touchLinkSemaphore)
                {
                    _touchLinkSemaphore = true;
                    TouchLinks(ent, HeadAreaNode);
                    _touchLinkSemaphore = false;
                }
            }
        }

        //TODO: part of the renderer as well
        private void StudioPlayerBlend(SequenceDescriptor pseqdesc, out int pBlend, ref float pPitch)
        {
            pBlend = (int)(pPitch * 3.0);

            if (pseqdesc.Blends[0].Start > pBlend)
            {
                pPitch -= pseqdesc.Blends[0].Start / 3.0f;
                pBlend = 0;
            }
            else
            {
                if (pBlend > pseqdesc.Blends[0].End)
                {
                    pPitch -= pseqdesc.Blends[0].End / 3.0f;
                    pBlend = 255;
                }
                else
                {
                    var blendRange = pseqdesc.Blends[0].End - pseqdesc.Blends[0].Start;

                    if (blendRange < 0.1)
                    {
                        pBlend = 127;
                    }
                    else
                    {
                        pBlend = (int)((pBlend - pseqdesc.Blends[0].Start) * 255.0 / blendRange);
                    }

                    pPitch = 0;
                }
            }
        }

        private Hull[] HullForEntity(BaseEntity ent, in Vector3 mins, in Vector3 maxs, out Vector3 offset)
        {
            if (ent.Solid == Solid.BSP)
            {
                if (ent.MoveType != MoveType.PushStep && ent.MoveType != MoveType.Push)
                {
                    throw new InvalidOperationException("Solid.BSP without MoveType.Push");
                }

                //TODO: avoid allocating here if possible
                return new[] { HullForBsp(ent, mins, maxs, out offset) };
            }

            box_planes[0].Distance = ent.Maxs.X - mins.X;
            box_planes[1].Distance = ent.Mins.X - maxs.X;
            box_planes[2].Distance = ent.Maxs.Y - mins.Y;
            box_planes[3].Distance = ent.Mins.Y - maxs.Y;
            box_planes[4].Distance = ent.Maxs.Z - mins.Z;
            box_planes[5].Distance = ent.Mins.Z - maxs.Z;

            offset = ent.Origin;

            return box_hull;
        }

        private Hull[] HullForStudioModel(BaseAnimating pEdict, StudioModel studioModel, in Vector3 mins, in Vector3 maxs, out Vector3 offset, out int pNumHulls)
        {
            var size = maxs - mins;

            bool useStudioHull;
            float sizeScale;

            if (!VectorUtils.VectorsEqual(Vector3.Zero, size) || (TraceFlags & TraceFlags.SimpleBox) != 0)
            {
                useStudioHull = false;
                sizeScale = 0.5f;
            }
            else if ((pEdict.Flags & EntityFlags.Client) == 0)
            {
                useStudioHull = true;
                sizeScale = 0.5f;
            }
            else if (_sv_clienttrace.Float == 0)
            {
                useStudioHull = false;
                sizeScale = 0.5f;
            }
            else
            {
                sizeScale = _sv_clienttrace.Float * 0.5f;
                useStudioHull = true;
                size.X = size.Y = size.Z = 1;
            }

            //TODO: rework
            var skipShield = !pEdict.HasShield && false;// (g_bIsTerrorStrike || g_bIsCStrike || g_bIsCZero);

            if (useStudioHull || (studioModel.StudioFile.Flags & MDLFlags.ComplexModelIntersection) != 0)
            {
                size *= sizeScale;
                offset = Vector3.Zero;

                if ((pEdict.Flags & EntityFlags.Client) != 0)
                {
                    var angles = pEdict.Angles;

                    StudioPlayerBlend(studioModel.StudioFile.Sequences[(int)pEdict.Sequence], out var iBlend, ref angles.X);

                    _studioHullBlenders[0] = (byte)iBlend;

                    return _studioCache.StudioHull(
                        studioModel,
                        pEdict.Frame,
                        (int)pEdict.Sequence,
                        angles,
                        pEdict.Origin,
                        size,
                        _studioHullControllers,
                        _studioHullBlenders,
                        out pNumHulls,
                        skipShield);
                }
                else
                {
                    return _studioCache.StudioHull(
                        studioModel,
                        pEdict.Frame,
                        (int)pEdict.Sequence,
                        pEdict.Angles,
                        pEdict.Origin,
                        size,
                        pEdict.Controllers,
                        pEdict.Blenders,
                        out pNumHulls,
                        skipShield);
                }
            }

            pNumHulls = 1;
            return HullForEntity(pEdict, mins, maxs, out offset);
        }

        private bool RecursiveHullCheck(Hull hull, int num, float p1f, float p2f, ref Vector3 p1, ref Vector3 p2, ref Trace trace)
        {
            if (num >= 0)
            {
                //TODO: figure out if planes check is possible
                if (num < hull.FirstClipNode || num > hull.LastClipNode /*|| hull.Planes == null*/)
                {
                    throw new InvalidOperationException("RecursiveHullCheck: bad node number");
                }

                float front, back;

                var pNode = hull.ClipNodes[num];
                var pPlane = hull.Planes.Span[pNode.PlaneIndex];

                if (pPlane.Type <= PlaneType.Z)
                {
                    front = p1.Index((int)pPlane.Type) - pPlane.Distance;
                    back = p2.Index((int)pPlane.Type) - pPlane.Distance;
                }
                else
                {
                    front = Vector3.Dot(pPlane.Normal, p1) - pPlane.Distance;
                    back = Vector3.Dot(pPlane.Normal, p2) - pPlane.Distance;
                }

                if (front >= 0.0 && back >= 0.0)
                {
                    return RecursiveHullCheck(hull, pNode.Children[0], p1f, p2f, ref p1, ref p2, ref trace);
                }

                if (front < 0.0 && back < 0.0)
                {
                    return RecursiveHullCheck(hull, pNode.Children[1], p1f, p2f, ref p1, ref p2, ref trace);
                }

                float frac;

                if (front < 0.0)
                {
                    frac = (float)((front + 0.03125) / (front - back));
                }
                else
                {
                    frac = (float)((front - 0.03125) / (front - back));
                }

                frac = Math.Clamp(frac, 0, 1);

                if (float.IsNaN(frac))
                {
                    return false;
                }

                var distanceFraction = p2f - p1f;
                var mid = p1 + ((p2 - p1) * frac);
                var midFraction = (distanceFraction * frac) + p1f;
                var side = front > 0.0 ? 1 : 0;

                if (!RecursiveHullCheck(hull, pNode.Children[side], p1f, midFraction, ref p1, ref mid, ref trace))
                {
                    return false;
                }

                if (HullPointContents(hull, pNode.Children[side ^ 1], ref mid) != Contents.Solid)
                {
                    return RecursiveHullCheck(hull, pNode.Children[side ^ 1], midFraction, p2f, ref mid, ref p2, ref trace);
                }

                if (trace.AllSolid)
                {
                    return false;
                }

                if (side != 0)
                {
                    trace.Plane.Normal = -pPlane.Normal;
                    trace.Plane.Distance = -pPlane.Distance;
                }
                else
                {
                    trace.Plane.Normal = pPlane.Normal;
                    trace.Plane.Distance = pPlane.Distance;
                }

                while (true)
                {
                    trace.Fraction = midFraction;
                    if (HullPointContents(hull, hull.FirstClipNode, ref mid) != Contents.Solid)
                    {
                        trace.EndPosition = mid;
                        return false;
                    }

                    frac -= 0.1f;

                    if (frac < 0.0)
                    {
                        break;
                    }

                    midFraction = (distanceFraction * frac) + p1f;
                    mid = p1 + ((p2 - p1) * frac);
                }
                trace.EndPosition = mid;
                _logger.Debug("backup past 0");

                return false;
            }

            var contents = (Contents)num;

            if (contents == Contents.Solid)
            {
                trace.StartSolid = true;
                return true;
            }

            trace.AllSolid = false;

            if (contents == Contents.Empty)
            {
                trace.InOpen = true;
                return true;
            }

            if (contents == Contents.Translucent)
            {
                return true;
            }

            trace.InWater = true;

            return true;
        }

        private void SingleClipMoveToEntity(BaseEntity ent, in Vector3 start, in Vector3 mins, in Vector3 maxs, in Vector3 end, out Trace trace)
        {
            trace = new Trace
            {
                Fraction = 1.0f,
                AllSolid = true,
                EndPosition = end
            };

            Hull[] pHulls;
            Vector3 offset;
            int numhulls;

            var model = ent.Model;

            if (model is StudioModel studioModel)
            {
                if (!(ent is BaseAnimating animating))
                {
                    throw new InvalidOperationException($"Entity of type {ent.ClassName} has studio model set for it, but is not a {nameof(BaseAnimating)}");
                }

                pHulls = HullForStudioModel(animating, studioModel, mins, maxs, out offset, out numhulls);
            }
            else
            {
                pHulls = HullForEntity(ent, mins, maxs, out offset);
                numhulls = 1;
            }

            var start_l = start - offset;
            var end_l = end - offset;

            Vector3 up;
            Vector3 right;
            Vector3 forward;

            var rotated = ent.Solid == Solid.BSP && (ent.Angles.X != 0 || ent.Angles.Y != 0 || ent.Angles.Z != 0);

            if (rotated)
            {
                VectorUtils.AngleToVectors(ent.Angles, out up, out right, out forward);

                start_l = new Vector3(
                    Vector3.Dot(start_l, up),
                    -Vector3.Dot(start_l, right),
                    Vector3.Dot(start_l, forward)
                );

                end_l = new Vector3(
                    Vector3.Dot(end_l, up),
                    -Vector3.Dot(end_l, right),
                    Vector3.Dot(end_l, forward)
                );
            }

            if (numhulls == 1)
            {
                RecursiveHullCheck(pHulls[0], pHulls[0].FirstClipNode, 0.0f, 1.0f, ref start_l, ref end_l, ref trace);
            }
            else
            {
                int closest = 0;

                for (int i = 0; i < numhulls; ++i)
                {
                    var tempTrace = new Trace
                    {
                        AllSolid = true,
                        Fraction = 1.0f,
                        EndPosition = end
                    };

                    RecursiveHullCheck(pHulls[i], pHulls[i].FirstClipNode, 0.0f, 1.0f, ref start_l, ref end_l, ref tempTrace);

                    if (i == 0 || tempTrace.AllSolid || tempTrace.StartSolid || trace.Fraction > tempTrace.Fraction)
                    {
                        var startSolid = trace.StartSolid;

                        trace = tempTrace;

                        trace.StartSolid = startSolid;

                        closest = i;
                    }
                }

                trace.HitGroup = _studioCache.HitgroupForStudioHull(closest);
            }

            if (trace.Fraction != 1.0)
            {
                if (rotated)
                {
                    VectorUtils.AngleToVectorsTranspose(ent.Angles, out forward, out right, out up);

                    trace.Plane.Normal = new Vector3(
                        Vector3.Dot(trace.Plane.Normal, forward),
                        Vector3.Dot(trace.Plane.Normal, right),
                        Vector3.Dot(trace.Plane.Normal, up)
                    );
                }

                trace.EndPosition = start + ((end - start) * trace.Fraction);
            }

            if (trace.Fraction < 1.0 || trace.StartSolid)
            {
                trace.Entity = ent;
            }
        }

        private void MoveBounds(ref Vector3 start, ref Vector3 mins, ref Vector3 maxs, ref Vector3 end, out Vector3 boxmins, out Vector3 boxmaxs)
        {
            boxmins = new Vector3();
            boxmaxs = new Vector3();

            for (int i = 0; i < 3; ++i)
            {
                if (end.Index(i) > start.Index(i))
                {
                    boxmins.Index(i, start.Index(i) + mins.Index(i) - 1.0f);
                    boxmaxs.Index(i, end.Index(i) + maxs.Index(i) + 1.0f);
                }
                else
                {
                    boxmins.Index(i, end.Index(i) + mins.Index(i) - 1.0f);
                    boxmaxs.Index(i, start.Index(i) + maxs.Index(i) + 1.0f);
                }
            }
        }

        private bool DoesSphereIntersect(in Vector3 vSphereCenter, float fSphereRadiusSquared, in Vector3 vLinePt, in Vector3 vLineDir)
        {
            var localStart = vLinePt - vSphereCenter;

            var dot = Vector3.Dot(localStart, vLineDir);

            return 0.000001 < ((dot + dot) * (dot + dot))
                - (vLineDir.LengthSquared()
                * 4.0
                * (localStart.LengthSquared() - fSphereRadiusSquared));
        }

        private bool CheckSphereIntersection(BaseEntity ent, in Vector3 start, in Vector3 end)
        {
            if ((ent.Flags & EntityFlags.Client) == 0)
            {
                return false;
            }

            var animating = ent as BaseAnimating;

            var pModel = ent.Model as StudioModel;

            var traceOrg = start;
            var traceDir = end - start;

            var pSequence = pModel.StudioFile.Sequences[(int)animating.Sequence];

            float fSphereRadiusSquared = 0;

            for (int i = 0; i < 3; ++i)
            {
                var value = Math.Max(Math.Abs(pSequence.BBMin.Index(i)), Math.Abs(pSequence.BBMax.Index(i)));

                fSphereRadiusSquared += value * value;
            }

            return DoesSphereIntersect(ent.Origin, fSphereRadiusSquared, traceOrg, traceDir);
        }

        private void ClipToLinks(AreaNode node, ref MoveClip clip)
        {
            foreach (var pEntity in node.Solids)
            {
                if (pEntity.PhysicsState.GroupInfo != 0 && clip.PassEntity?.PhysicsState.GroupInfo != 0
                    && !TestGroupOperation(pEntity.PhysicsState.GroupInfo, clip.PassEntity.PhysicsState.GroupInfo))
                {
                    continue;
                }

                if (pEntity.Solid == Solid.Not
                    || ReferenceEquals(clip.PassEntity, pEntity))
                {
                    continue;
                }

                if (pEntity.Solid == Solid.Trigger)
                {
                    throw new InvalidOperationException("Trigger in clipping list");
                }

                //TODO: change from return to continue to process remaining entities
                //TODO: does it make sense to pass passentity?
                if (!pEntity.ShouldCollide(clip.PassEntity))
                {
                    return;
                }

                if (pEntity.Solid == Solid.BSP)
                {
                    if ((pEntity.Flags & EntityFlags.MonsterClip) != 0 && !clip.MonsterClipBrush)
                    {
                        continue;
                    }
                }
                else if (clip.Type == TraceType.IgnoreMonsters && pEntity.MoveType != MoveType.PushStep)
                {
                    continue;
                }

                if (clip.IgnoreTransparent && pEntity.RenderMode != RenderMode.Normal && (pEntity.Flags & EntityFlags.WorldBrush) == 0)
                {
                    continue;
                }

                if (clip.BoxMins.X > pEntity.AbsMax.X
                    || clip.BoxMins.Y > pEntity.AbsMax.Y
                    || clip.BoxMins.Z < pEntity.AbsMax.Z
                    || pEntity.AbsMin.X > clip.BoxMaxs.X
                    || pEntity.AbsMin.Y > clip.BoxMaxs.Y
                    || pEntity.AbsMin.Z > clip.BoxMaxs.Z)
                {
                    continue;
                }

                if (pEntity.Solid != Solid.SlideBox && !CheckSphereIntersection(pEntity, clip.Start, clip.End))
                {
                    continue;
                }

                if (clip.PassEntity != null && clip.PassEntity.Size.X != 0 && pEntity.Size.X == 0)
                {
                    continue;
                }

                if (clip.Trace.AllSolid)
                {
                    return;
                }

                if (clip.PassEntity != null
                    && (SharedEntityUtils.HandleEquals(pEntity.Owner, clip.PassEntity)
                    || SharedEntityUtils.HandleEquals(clip.PassEntity.Owner, pEntity)))
                {
                    continue;
                }

                Trace trace;

                if ((pEntity.Flags & EntityFlags.Monster) != 0)
                {
                    SingleClipMoveToEntity(pEntity, clip.Start, clip.Mins2, clip.Maxs2, clip.End, out trace);
                }
                else
                {
                    SingleClipMoveToEntity(pEntity, clip.Start, clip.Mins, clip.Maxs, clip.End, out trace);
                }

                if (trace.AllSolid || trace.StartSolid || clip.Trace.Fraction > trace.Fraction)
                {
                    clip.Trace.Entity = pEntity;

                    if (clip.Trace.StartSolid)
                    {
                        clip.Trace = trace;
                        clip.Trace.StartSolid = true;
                    }
                    else
                    {
                        clip.Trace = trace;
                    }
                }
            }

            if (node.Axis == -1)
            {
                return;
            }

            if (clip.BoxMaxs.Index(node.Axis) > node.Distance)
            {
                ClipToLinks(node.Children[0], ref clip);
            }

            if (clip.BoxMins.Index(node.Axis) < node.Distance)
            {
                ClipToLinks(node.Children[1], ref clip);
            }
        }

        public Trace Move(ref Vector3 start, in Vector3 mins, in Vector3 maxs, in Vector3 end, TraceType type, BaseEntity passedict, bool ignoreTransparent, bool monsterClipBrush)
        {
            var clip = new MoveClip();

            SingleClipMoveToEntity(_entities.World, start, mins, maxs, end, out clip.Trace);

            var worldFraction = clip.Trace.Fraction;

            if (clip.Trace.Fraction == 0)
            {
                return clip.Trace;
            }

            clip.Trace.Fraction = 1.0f;

            clip.Start = start;
            clip.End = clip.Trace.EndPosition;

            clip.Mins = mins;
            clip.Maxs = maxs;

            clip.Type = type;
            clip.IgnoreTransparent = ignoreTransparent;

            clip.PassEntity = passedict;
            clip.MonsterClipBrush = monsterClipBrush;

            //The original version had ignoreTransparent as a high bit in type so this didn't always work
            //Since the only use of Missile is leeches and the high bit isn't set there this was fixed
            if (type == TraceType.Missile)
            {
                clip.Mins2.X = -15;
                clip.Maxs2.X = 15;
                clip.Mins2.Y = -15;
                clip.Maxs2.Y = 15;
                clip.Mins2.Z = -15;
                clip.Maxs2.Z = 15;
            }
            else
            {
                clip.Mins2 = mins;
                clip.Maxs2 = maxs;
            }

            MoveBounds(ref start, ref clip.Mins2, ref clip.Maxs2, ref clip.End, out clip.BoxMins, out clip.BoxMaxs);
            ClipToLinks(_areaNodes[0], ref clip);

            //TODO: set this here?
            //_currentTrace.Entity = clip.Trace.Entity;
            clip.Trace.Fraction *= worldFraction;

            return clip.Trace;
        }
    }
}
