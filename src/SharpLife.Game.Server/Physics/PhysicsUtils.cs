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

using SharpLife.Models.BSP.FileFormat;
using SharpLife.Utility.Mathematics;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpLife.Game.Server.Physics
{
    /// <summary>
    /// Stateless helper functions for physics
    /// </summary>
    public static class PhysicsUtils
    {
        private static BoxOnPlaneSideResult InternalBoxOnPlaneSide(in Vector3 emins, in Vector3 emaxs, Models.BSP.FileFormat.Plane p)
        {
            float distanceSquared1;
            float distanceSquared2;

            switch (p.SignBits)
            {
                case PlaneSignBit.None:
                    distanceSquared1 = Vector3.Dot(p.Normal, emaxs);
                    distanceSquared2 = Vector3.Dot(p.Normal, emins);
                    break;
                case PlaneSignBit.X:
                    distanceSquared1 = p.Normal.X * emins.X + p.Normal.Y * emaxs.Y + p.Normal.Z * emaxs.Z;
                    distanceSquared2 = p.Normal.X * emaxs.X + p.Normal.Y * emins.Y + p.Normal.Z * emins.Z;
                    break;
                case PlaneSignBit.Y:
                    distanceSquared1 = p.Normal.X * emins.X + p.Normal.Y * emins.Y + p.Normal.Z * emaxs.Z;
                    distanceSquared2 = p.Normal.X * emins.X + p.Normal.Y * emaxs.Y + p.Normal.Z * emins.Z;
                    break;
                case PlaneSignBit.X | PlaneSignBit.Y:
                    distanceSquared1 = p.Normal.X * emins.X + p.Normal.Y * emins.Y + p.Normal.Z * emaxs.Z;
                    distanceSquared2 = p.Normal.X * emaxs.X + p.Normal.Y * emaxs.Y + p.Normal.Z * emins.Z;
                    break;
                case PlaneSignBit.Z:
                    distanceSquared1 = p.Normal.X * emaxs.X + p.Normal.Y * emaxs.Y + p.Normal.Z * emins.Z;
                    distanceSquared2 = p.Normal.X * emins.X + p.Normal.Y * emins.Y + p.Normal.Z * emaxs.Z;
                    break;
                case PlaneSignBit.X | PlaneSignBit.Z:
                    distanceSquared1 = p.Normal.X * emins.X + p.Normal.Y * emaxs.Y + p.Normal.Z * emins.Z;
                    distanceSquared2 = p.Normal.X * emaxs.X + p.Normal.Y * emins.Y + p.Normal.Z * emaxs.Z;
                    break;
                case PlaneSignBit.Y | PlaneSignBit.Z:
                    distanceSquared1 = p.Normal.X * emaxs.X + p.Normal.Y * emins.Y + p.Normal.Z * emins.Z;
                    distanceSquared2 = p.Normal.X * emins.X + p.Normal.Y * emaxs.Y + p.Normal.Z * emaxs.Z;
                    break;
                case PlaneSignBit.X | PlaneSignBit.Y | PlaneSignBit.Z:
                    distanceSquared1 = Vector3.Dot(p.Normal, emins);
                    distanceSquared2 = Vector3.Dot(p.Normal, emaxs);
                    break;

                default:
                    throw new InvalidOperationException("BoxOnPlaneSide:  Bad signbits");
            }

            var result = BoxOnPlaneSideResult.None;

            if (distanceSquared1 >= p.Distance)
            {
                result |= BoxOnPlaneSideResult.InFront;
            }

            if (p.Distance > distanceSquared2)
            {
                result |= BoxOnPlaneSideResult.Behind;
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BoxOnPlaneSideResult BoxOnPlaneSide(ref Vector3 emins, ref Vector3 emaxs, Models.BSP.FileFormat.Plane p)
        {
            if (p.Type < PlaneType.AnyX)
            {
                if (p.Distance <= emins.Index((int)p.Type))
                {
                    return BoxOnPlaneSideResult.InFront;
                }

                if (p.Distance >= emaxs.Index((int)p.Type))
                {
                    return BoxOnPlaneSideResult.Behind;
                }

                return BoxOnPlaneSideResult.CrossesPlane;
            }
            else
            {
                return InternalBoxOnPlaneSide(emins, emaxs, p);
            }
        }
    }
}
