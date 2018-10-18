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
using SharpLife.Engine.Shared.API.Engine.Server;
using SharpLife.Game.Server.Entities;
using SharpLife.Game.Server.Entities.EntityList;
using SharpLife.Game.Shared.Audio;
using SharpLife.Game.Shared.Entities;
using SharpLife.Game.Shared.Physics;
using SharpLife.Models.BSP.FileFormat;
using SharpLife.Utility;
using SharpLife.Utility.Mathematics;
using System;
using System.Linq;
using System.Numerics;

namespace SharpLife.Game.Server.Physics
{
    public sealed partial class GameMovement
    {
        private struct MoveCache
        {
            public BaseEntity Entity;
            public Vector3 Origin;
        }

        private readonly ILogger _logger;

        private readonly ITime _engineTime;

        private readonly SnapshotTime _gameTime;

        private readonly IServerClients _serverClients;

        private readonly Random _random;

        private readonly ServerEntities _entities;

        private readonly ServerEntityList _entityList;

        private readonly GamePhysics _physics;

        //TODO: create
        private readonly IVariable _sv_maxvelocity;

        private readonly IVariable _sv_stepsize;

        private readonly IVariable _sv_bounce;

        private readonly IVariable _sv_gravity;

        private readonly IVariable _sv_stopspeed;

        private readonly IVariable _sv_friction;

        //Tracked separately from engine frametime to allow independent updating of physics
        private double _frameTime;

        private Trace _currentTrace;

        public ref Trace CurrentTrace => ref _currentTrace;

        public int ForceRetouch { get; set; }

        private MoveCache[] _moveCache = new MoveCache[0];

        public GameMovement(ILogger logger, ITime engineTime, SnapshotTime gameTime,
            IServerClients serverClients,
            ServerEntities entities, ServerEntityList entityList,
            Random random,
            GamePhysics physics,
            ICommandContext commandContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _engineTime = engineTime ?? throw new ArgumentNullException(nameof(engineTime));
            _gameTime = gameTime ?? throw new ArgumentNullException(nameof(gameTime));
            _serverClients = serverClients ?? throw new ArgumentNullException(nameof(serverClients));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _entityList = entityList ?? throw new ArgumentNullException(nameof(entityList));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _physics = physics ?? throw new ArgumentNullException(nameof(physics));

            //TODO: add a filter to enforce positive values only
            _sv_maxvelocity = commandContext.RegisterVariable(
                new VariableInfo("sv_maxvelocity")
                .WithHelpInfo("The maximum velocity in any axis that an entity can have. Any velocity greater than this is clamped to this value")
                .WithValue(2000)
                .WithNumberFilter()
                .WithNumberSignFilter(true));

            //TODO: mark as server cvar
            _sv_stepsize = commandContext.RegisterVariable(
                new VariableInfo("sv_stepsize")
                .WithHelpInfo("Defines the maximum height that characters can still step over (e.g. stairs)")
                .WithValue(18)
                .WithNumberFilter()
                .WithNumberSignFilter(true));

            //TODO: mark as server cvar
            _sv_bounce = commandContext.RegisterVariable(
                new VariableInfo("sv_bounce")
                .WithHelpInfo("Multiplier for physics bounce effect when objects collide with other objects")
                .WithValue(1)
                .WithNumberFilter()
                .WithNumberSignFilter(true));

            //TODO: mark as server cvar
            _sv_gravity = commandContext.RegisterVariable(
                new VariableInfo("sv_gravity")
                .WithHelpInfo("The world's gravity amount, in units per second")
                .WithValue(800)
                .WithNumberFilter());

            //TODO: mark as server cvar
            _sv_stopspeed = commandContext.RegisterVariable(
                new VariableInfo("sv_stopspeed")
                .WithHelpInfo("Minimum stopping speed when on the ground")
                .WithValue(100)
                .WithNumberFilter()
                .WithNumberSignFilter(true));

            //TODO: mark as server cvar
            _sv_friction = commandContext.RegisterVariable(
                new VariableInfo("sv_friction")
                .WithHelpInfo("World friction")
                .WithValue(4)
                .WithNumberFilter()
                .WithNumberSignFilter(true));
        }

        private void SetGlobalTrace(in Trace trace)
        {
            _currentTrace = trace;

            //Convert null entities to the world
            _currentTrace.Entity = _currentTrace.Entity ?? _entities.World;
        }

        private void EnsureMoveCacheCapacity()
        {
            if (_moveCache.Length < _entityList.EntityCount)
            {
                _moveCache = new MoveCache[_entityList.EntityCount];
            }
        }

        /// <summary>
        /// Clears all references to entities in the cache
        /// </summary>
        private void ClearMoveCache()
        {
            Array.Clear(_moveCache, 0, _moveCache.Length);
        }

        private void CheckVelocity(BaseEntity ent)
        {
            var velocity = ent.Velocity;
            var origin = ent.Origin;

            for (int i = 0; i < 3; ++i)
            {
                if (float.IsNaN(velocity.Index(i)))
                {
                    _logger.Information("Got a NaN velocity on {0}", ent.ClassName);
                    velocity.Index(i, 0);
                }

                if (float.IsNaN(origin.Index(i)))
                {
                    _logger.Information("Got a NaN origin on {0}", ent.ClassName);
                    origin.Index(i, 0);
                }

                if (velocity.Index(i) > _sv_maxvelocity.Float)
                {
                    _logger.Debug("Got a velocity too high on {0}", ent.ClassName);
                    velocity.Index(i, _sv_maxvelocity.Float);
                }
                else if (-_sv_maxvelocity.Float > velocity.Index(i))
                {
                    _logger.Debug("Got a velocity too low on {0}", ent.ClassName);
                    velocity.Index(i, -_sv_maxvelocity.Float);
                }
            }

            ent.Velocity = velocity;
            ent.Origin = origin;
        }

        private byte ClipVelocity(ref Vector3 input, ref Vector3 normal, out Vector3 output, float overbounce)
        {
            output = new Vector3();

            byte result = 0;

            if (normal.Z > 0.0)
            {
                result |= 1;
            }

            if (normal.Z == 0.0)
            {
                result |= 2;
            }

            var dot = Vector3.Dot(input, normal) * overbounce;

            for (int i = 0; i < 3; ++i)
            {
                var value = input.Index(i) - (normal.Index(i) * dot);

                output.Index(i, value);

                if (value > -0.1 && value < 0.1)
                {
                    output.Index(i, 0);
                }
            }

            return result;
        }

        private BaseEntity TestEntityPosition(BaseEntity ent)
        {
            var trace = _physics.Move(ref ent.RefOrigin, ent.Mins, ent.Maxs, ent.RefOrigin, 0, ent, false, false);

            if (trace.StartSolid)
            {
                SetGlobalTrace(trace);
                return trace.Entity;
            }

            return null;
        }

        private Trace PushEntity(BaseEntity ent, in Vector3 push)
        {
            var end = ent.Origin + push;

            var type = TraceType.Missile;

            if (ent.MoveType != MoveType.FlyMissile)
            {
                type = ent.Solid <= Solid.Trigger ? TraceType.IgnoreMonsters : TraceType.None;
            }

            var trace = _physics.Move(ref ent.RefOrigin, ent.Mins, ent.Maxs, end, type, ent, false, (ent.Flags & EntityFlags.MonsterClip) != 0);

            if (trace.Fraction != 0.0)
            {
                ent.Origin = trace.EndPosition;
            }

            _physics.LinkEdict(ent, true);

            if (trace.Entity != null)
            {
                Impact(ent, trace.Entity, trace);
            }

            return trace;
        }

        private bool RunThink(BaseEntity ent)
        {
            if (!ent.PendingDestruction)
            {
                var thinkTime = ent.NextThink;

                if (0.0 < thinkTime && thinkTime < _frameTime + _engineTime.ElapsedTime)
                {
                    if (thinkTime < _engineTime.ElapsedTime)
                    {
                        thinkTime = (float)_engineTime.ElapsedTime;
                    }

                    ent.NextThink = 0;
                    _gameTime.ElapsedTime = thinkTime;
                    ent.Think();
                }
            }

            if (ent.PendingDestruction)
            {
                _entityList.DestroyEntity(ent);
            }

            return !ent.PendingDestruction;
        }

        private void Impact(BaseEntity e1, BaseEntity e2, in Trace ptrace)
        {
            _gameTime.ElapsedTime = _engineTime.ElapsedTime;

            if (((e1.Flags | e2.Flags) & EntityFlags.PendingDestruction) == 0)
            {
                if (e1.PhysicsState.GroupInfo != 0
                    && e2.PhysicsState.GroupInfo != 0
                    && !_physics.TestGroupOperation(e1.PhysicsState.GroupInfo, e2.PhysicsState.GroupInfo))
                {
                    return;
                }

                if (e1.Solid != Solid.Not)
                {
                    SetGlobalTrace(ptrace);
                    e1.Touch(e2);
                }

                if (e2.Solid != Solid.Not)
                {
                    SetGlobalTrace(ptrace);
                    e2.Touch(e1);
                }
            }
        }

        //TODO: implement (not here)
        private void SV_StartSound(int recipients, BaseEntity entity, Channel channel, string sample, float volume, float attenuation, int fFlags, int pitch)
        {
        }

        private bool CheckWater(BaseEntity ent)
        {
            ent.WaterLevel = WaterLevel.Dry;
            ent.WaterType = Contents.Empty;

            _physics.GroupMask = ent.PhysicsState.GroupInfo;

            var point = new Vector3(
                0.5f * (ent.AbsMin.X + ent.AbsMax.X),
                0.5f * (ent.AbsMin.Y + ent.AbsMax.Y),
                ent.AbsMin.Z + 1.0f
            );

            var contents = _physics.PointContents(ref point);

            if (contents <= Contents.Water)
            {
                ent.WaterType = contents;
                ent.WaterLevel = WaterLevel.Feet;

                if (ent.AbsMin.Z == ent.AbsMax.Z)
                {
                    ent.WaterLevel = WaterLevel.Head;
                }
                else
                {
                    point.Z = (ent.AbsMax.Z + ent.AbsMin.Z) * 0.5f;

                    if (_physics.PointContents(ref point) <= Contents.Water)
                    {
                        ent.WaterLevel = WaterLevel.Waist;

                        point += ent.ViewOffset;

                        if (_physics.PointContents(ref point) <= Contents.Water)
                        {
                            ent.WaterLevel = WaterLevel.Head;
                        }
                    }
                }

                if (contents <= Contents.Current0)
                {
                    ent.BaseVelocity += (int)ent.WaterLevel * 50.0f * PhysicsConstants.CurrentTable[Contents.Current0 - contents];
                }
            }

            return ent.WaterLevel > WaterLevel.Feet;
        }

        private void CheckWaterTransition(BaseEntity ent)
        {
            _physics.GroupMask = ent.PhysicsState.GroupInfo;

            var point = new Vector3(
                (ent._absMin.X + ent._absMax.X) * 0.5f,
                (ent._absMin.Y + ent._absMax.Y) * 0.5f,
                ent._absMin.Z + 1.0f
            );

            //TODO: this code and the SV_CheckWater function are very similar, and probably in the player physics code
            var contents = _physics.PointContents(ref point);

            if (ent.WaterType != Contents.Node)
            {
                if (contents <= Contents.Water)
                {
                    if (ent.WaterType != Contents.Empty)
                    {
                        SV_StartSound(0, ent, Channel.Auto, "player/pl_wade2.wav", Volume.Normal, Attenuation.Normal, 0, Pitch.Normal);
                    }

                    ent.WaterType = Contents.Empty;
                    ent.WaterLevel = WaterLevel.Dry;
                }
                else
                {
                    if (ent.WaterType == Contents.Empty)
                    {
                        SV_StartSound(0, ent, Channel.Auto, "player/pl_wade1.wav", Volume.Normal, Attenuation.Normal, 0, Pitch.Normal);

                        var velocity = ent.Velocity;
                        velocity.Z *= 0.5f;
                        ent.Velocity = velocity;
                    }

                    ent.WaterType = contents;
                    ent.WaterLevel = WaterLevel.Feet;

                    if (ent._absMin.Z == ent._absMax.Z)
                    {
                        ent.WaterLevel = WaterLevel.Head;
                    }
                    else
                    {
                        point.Z = (ent._absMax.Z + ent._absMin.Z) * 0.5f;

                        if (_physics.PointContents(ref point) <= Contents.Water)
                        {
                            ent.WaterLevel = WaterLevel.Waist;

                            point += ent.ViewOffset;

                            if (_physics.PointContents(ref point) <= Contents.Water)
                            {
                                ent.WaterLevel = WaterLevel.Head;
                            }
                        }
                    }
                }
            }
            else
            {
                ent.WaterType = contents;
                ent.WaterLevel = WaterLevel.Feet;
            }
        }

        private float RecursiveWaterLevel(in Vector3 center, float output, float input, int count)
        {
            var offset = ((output - input) * 0.5f) + input;

            if (count > 4)
            {
                return offset;
            }

            var test = center;
            test.Z += offset;

            if (_physics.PointContents(ref test) == Contents.Water)
            {
                return RecursiveWaterLevel(center, output, offset, count + 1);
            }
            else
            {
                return RecursiveWaterLevel(center, offset, input, count + 1);
            }
        }

        private float Submerged(BaseEntity ent)
        {
            var center = (ent.AbsMin + ent.AbsMax) * 0.5f;

            var bottom = ent._absMin.Z - center.Z;

            if (ent.WaterLevel != WaterLevel.Waist)
            {
                if (ent.WaterLevel != WaterLevel.Head)
                {
                    if (ent.WaterLevel != WaterLevel.Feet)
                    {
                        return 0;
                    }

                    return RecursiveWaterLevel(center, 0.0f, bottom, 0) - bottom;
                }

                _physics.GroupMask = ent.PhysicsState.GroupInfo;

                var test = new Vector3(center.X, center.Y, ent._absMax.Z);

                if (_physics.PointContents(ref test) == Contents.Water)
                {
                    return ent.Maxs.Z - ent.Mins.Z;
                }
            }

            var top = ent._absMax.Z - center.Z;
            var halfTop = top * 0.5f;

            var point = new Vector3(
                center.X,
                center.Y,
                center.Z + halfTop
            );

            float waterLevel;

            if (_physics.PointContents(ref point) == Contents.Water)
            {
                waterLevel = RecursiveWaterLevel(center, top, halfTop, 1);
            }
            else
            {
                waterLevel = RecursiveWaterLevel(center, halfTop, 0.0f, 1);
            }

            return waterLevel - bottom;
        }

        private bool PushRotate(BaseEntity pusher, float movetime)
        {
            if (pusher.AngularVelocity.X == 0 && pusher.AngularVelocity.Y == 0 && pusher.AngularVelocity.Z == 0)
            {
                pusher.LastThinkTime += movetime;
                return true;
            }

            var aVelocity = pusher.AngularVelocity * movetime;

            VectorUtils.AngleToVectors(pusher.Angles, out var forwardNow, out var rightNow, out var upNow);

            var savedAngles = pusher.Angles;

            pusher.Angles += aVelocity;

            VectorUtils.AngleToVectorsTranspose(pusher.Angles, out var forward, out var right, out var up);

            pusher.LastThinkTime += movetime;

            _physics.LinkEdict(pusher, false);

            if (pusher.Solid == Solid.Not)
            {
                return true;
            }

            EnsureMoveCacheCapacity();

            int num_moved = 0;

            //Don't check against the world
            foreach (var check in _entityList.Skip(1))
            {
                if (check.MoveType == MoveType.None
                    || check.MoveType == MoveType.Push
                    || check.MoveType == MoveType.Follow
                    || check.MoveType == MoveType.Noclip)
                {
                    continue;
                }

                if ((check.Flags & EntityFlags.OnGround) == 0 || check.GroundEntity != pusher.Handle)
                {
                    if (check.AbsMin.X >= pusher._absMax.X
                        || check.AbsMin.Y >= pusher._absMax.Y
                        || check.AbsMin.Z >= pusher._absMax.Z
                        || pusher.AbsMin.X >= check._absMax.X
                        || pusher.AbsMin.Y >= check._absMax.Y
                        || pusher.AbsMin.Z >= check._absMax.Z)
                    {
                        continue;
                    }

                    if (TestEntityPosition(check) == null)
                    {
                        continue;
                    }
                }

                if (check.MoveType != MoveType.Walk)
                {
                    check.Flags &= ~EntityFlags.OnGround;
                }

                var savedOrigin = check.Origin;

                _moveCache[num_moved].Origin = check.Origin;
                _moveCache[num_moved].Entity = check;

                ++num_moved;

                if (num_moved >= _moveCache.Length)
                {
                    throw new InvalidOperationException("Out of edicts in simulator!");
                }

                Vector3 distance;

                if (check.MoveType == MoveType.PushStep)
                {
                    distance = ((check._absMin + check._absMax) * 0.5f) - pusher.RefOrigin;
                }
                else
                {
                    distance = check.RefOrigin - pusher.RefOrigin;
                }

                pusher.Solid = Solid.Not;

                var mod = new Vector3(
                    Vector3.Dot(forwardNow, distance),
                    -Vector3.Dot(rightNow, distance),
                    Vector3.Dot(upNow, distance)
                );

                var move = new Vector3(
                    Vector3.Dot(mod, forward) - distance.X,
                    Vector3.Dot(mod, right) - distance.Y,
                    Vector3.Dot(mod, up) - distance.Z
                );

                var trace = PushEntity(check, move);

                pusher.Solid = Solid.BSP;

                if (check.MoveType != MoveType.PushStep)
                {
                    if ((check.Flags & EntityFlags.Client) != 0)
                    {
                        check.FixAngle = FixAngleMode.AddAVelocity;

                        var avel = check.AngularVelocity;
                        avel.Y += aVelocity.Y;
                        check.AngularVelocity = avel;
                    }
                    else
                    {
                        check.Angles += new Vector3(0, aVelocity.Y, 0);
                    }
                }

                if (TestEntityPosition(check) != null
                    && check.Mins.X != check.Maxs.X)
                {
                    if (check.Solid != Solid.Not
                        && check.Solid != Solid.Trigger)
                    {
                        check.Origin = savedOrigin;

                        _physics.LinkEdict(check, true);

                        pusher.Angles = savedAngles;

                        _physics.LinkEdict(pusher, false);

                        pusher.LastThinkTime -= movetime;

                        pusher.Blocked(check);

                        for (int i = 0; i < num_moved; ++i)
                        {
                            var pMoved = _moveCache[i].Entity;

                            pMoved.Origin = _moveCache[i].Origin;

                            if ((pMoved.Flags & EntityFlags.Client) != 0)
                            {
                                var avel = pMoved.AngularVelocity;
                                avel.Y = 0;
                                pMoved.AngularVelocity = avel;
                            }
                            else if (pMoved.MoveType != MoveType.PushStep)
                            {
                                var angles = pMoved.Angles;
                                angles.Y -= aVelocity.Y;
                                pMoved.Angles = angles;
                            }

                            _physics.LinkEdict(pMoved, false);
                        }

                        ClearMoveCache();

                        return false;
                    }
                    else
                    {
                        var mins = check.Mins;
                        var maxs = check.Maxs;

                        mins.X = 0;
                        maxs.X = 0;
                        mins.Y = 0;
                        maxs.Y = 0;

                        check.Mins = mins;
                        check.Maxs = maxs;
                    }
                }
            }

            ClearMoveCache();

            return true;
        }

        private void PushMove(BaseEntity pusher, float movetime)
        {
            if (pusher.Velocity.X == 0 && pusher.Velocity.Y == 0 && pusher.Velocity.Z == 0)
            {
                pusher.LastThinkTime += movetime;
                return;
            }

            var savedOrigin = pusher.Origin;

            var move = pusher.Velocity * movetime;

            var mins = pusher.AbsMin + move;
            var maxs = pusher.AbsMax + move;

            pusher.Origin = savedOrigin + move;

            pusher.LastThinkTime += movetime;

            _physics.LinkEdict(pusher, false);

            if (pusher.Solid != Solid.Not)
            {
                int num_moved = 0;

                //Don't check against the world
                foreach (var check in _entityList.Skip(1))
                {
                    if (check.MoveType == MoveType.None
                        || check.MoveType == MoveType.Push
                        || check.MoveType == MoveType.Follow
                        || check.MoveType == MoveType.Noclip)
                    {
                        continue;
                    }

                    if ((check.Flags & EntityFlags.OnGround) == 0 || check.GroundEntity != pusher.Handle)
                    {
                        if (check._absMin.X >= maxs.X
                            || check._absMin.Y >= maxs.Y
                            || check._absMin.Z >= maxs.Z
                            || mins.X >= check._absMax.X
                            || mins.Y >= check._absMax.Y
                            || mins.Z >= check._absMax.Z)
                        {
                            continue;
                        }

                        if (TestEntityPosition(check) == null)
                        {
                            continue;
                        }
                    }

                    if (check.MoveType != MoveType.Walk)
                        check.Flags &= ~EntityFlags.OnGround;

                    var savedBlockOrigin = check.Origin;

                    _moveCache[num_moved].Origin = check.Origin;
                    _moveCache[num_moved].Entity = check;

                    ++num_moved;

                    pusher.Solid = Solid.Not;

                    PushEntity(check, move);

                    pusher.Solid = Solid.BSP;

                    if (TestEntityPosition(check) != null
                        && check.Mins.X != check.Maxs.X)
                    {
                        if (check.Solid != Solid.Not
                            && check.Solid != Solid.Trigger)
                        {
                            check.Origin = savedBlockOrigin;

                            _physics.LinkEdict(check, true);

                            pusher.Origin = savedOrigin;

                            _physics.LinkEdict(pusher, false);

                            pusher.LastThinkTime -= movetime;

                            pusher.Blocked(check);

                            for (int i = 0; i < num_moved; ++i)
                            {
                                var moved = _moveCache[i].Entity;
                                moved.Origin = _moveCache[i].Origin;

                                _physics.LinkEdict(moved, false);
                            }

                            break;
                        }

                        var checkMins = check.Mins;
                        var checkMaxs = check.Maxs;

                        checkMins.X = 0;
                        checkMaxs.X = 0;
                        checkMins.Y = 0;
                        checkMaxs.Y = 0;
                        checkMaxs.Z = checkMins.Z;

                        check.Mins = checkMins;
                        check.Maxs = checkMaxs;
                    }
                }

                ClearMoveCache();
            }
        }

        private bool InternalCheckBottom(BaseEntity ent, ref Vector3 start, in Vector3 mins, in Vector3 maxs)
        {
            start.Z = mins.Z + _sv_stepsize.Float;

            start.X = (mins.X + maxs.X) * 0.5f;
            start.Y = (mins.Y + maxs.Y) * 0.5f;

            var stop = new Vector3(
                start.X,
                start.Y,
                start.Z - (2 * _sv_stepsize.Float)

            );

            var trace = _physics.Move(ref start, Vector3.Zero, Vector3.Zero, stop, TraceType.IgnoreMonsters, ent, false, (ent.Flags & EntityFlags.MonsterClip) != 0);

            if (trace.Fraction == 1.0)
            {
                return false;
            }

            var middle = trace.EndPosition.Z;

            for (int x = 0; x <= 1; ++x)
            {
                for (int y = 0; y <= 1; ++y)
                {
                    start.X = x != 0 ? maxs.X : mins.X;
                    start.Y = y != 0 ? maxs.Y : mins.Y;

                    trace = _physics.Move(ref start, Vector3.Zero, Vector3.Zero, stop, TraceType.IgnoreMonsters, ent, false, (ent.Flags & EntityFlags.MonsterClip) != 0);

                    if (trace.Fraction == 1.0 || middle - trace.EndPosition.Z > _sv_stepsize.Float)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool CheckBottom(BaseEntity ent)
        {
            var mins = ent.Origin + ent.Mins;
            var maxs = ent.Origin + ent.Maxs;

            _physics.GroupMask = ent.PhysicsState.GroupInfo;

            var start = new Vector3(
                0,
                0,
                mins.Z - 1.0f);

            for (int x = 0; x <= 1; ++x)
            {
                for (int y = 0; y <= 1; ++y)
                {
                    start.X = x != 0 ? maxs.X : mins.X;
                    start.Y = y != 0 ? maxs.Y : mins.Y;

                    if (_physics.PointContents(ref start) != Contents.Solid)
                    {
                        return InternalCheckBottom(ent, ref start, mins, maxs);
                    }
                }
            }

            return true;
        }

        private bool IsOnGround(BaseEntity ent)
        {
            var mins = ent.Origin + ent.Mins;
            var maxs = ent.Origin + ent.Maxs;

            _physics.GroupMask = ent.PhysicsState.GroupInfo;

            var point = new Vector3(
                0,
                0,
                mins.Z - 1.0f);

            for (int x = 0; x <= 1; ++x)
            {
                for (int y = 0; y <= 1; ++y)
                {
                    point.X = x != 0 ? maxs.X : mins.X;
                    point.Y = y != 0 ? maxs.Y : mins.Y;

                    if (_physics.PointContents(ref point) == Contents.Solid)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
