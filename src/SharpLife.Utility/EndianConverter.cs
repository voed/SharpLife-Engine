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
using System.Runtime.InteropServices;

namespace SharpLife.Utility
{
    /// <summary>
    /// Provides functions to convert the endianness of primitive types
    /// </summary>
    public static class EndianConverter
    {
        [StructLayout(LayoutKind.Explicit)]
        private unsafe struct FloatUnion
        {
            [FieldOffset(0)]
            public fixed byte bytes[4];

            [FieldOffset(0)]
            public float value;
        }

        [StructLayout(LayoutKind.Explicit)]
        private unsafe struct DoubleUnion
        {
            [FieldOffset(0)]
            public fixed byte bytes[8];

            [FieldOffset(0)]
            public double value;
        }

        public static sbyte Swap(sbyte value)
        {
            return value;
        }

        public static byte Swap(byte value)
        {
            return value;
        }

        public static short Swap(short value)
        {
            return (short)((value & 0xFF) << 8
                | (ushort)((value & 0xFF00) >> 8));
        }

        public static ushort Swap(ushort value)
        {
            return (ushort)((value & 0xFF) << 8
                | (ushort)((value & 0xFF00) >> 8));
        }

        public static int Swap(int value)
        {
            return (value & 0xFF) << 24
                | (value & 0xFF00) << 8
                | (value & 0xFF0000) >> 8
                | (int)((value & 0xFF000000) >> 24);
        }

        public static uint Swap(uint value)
        {
            return (value & 0xFF) << 24
                | (value & 0xFF00) << 8
                | (value & 0xFF0000) >> 8
                | (uint)((value & 0xFF000000) >> 24);
        }

        public static long Swap(long value)
        {
            return (value & 0xFF) << 56
                | (value & 0xFF00) << 40
                | (value & 0xFF0000) << 24
                | (value & 0xFF000000) << 8
                | (value & 0xFF00000000) >> 8
                | (value & 0xFF0000000000) >> 24
                | (value & 0xFF000000000000) >> 40
                | (long)(((ulong)value & 0xFF00000000000000) >> 56);
        }

        public static ulong Swap(ulong value)
        {
            return (value & 0xFF) << 56
                | (value & 0xFF00) << 40
                | (value & 0xFF0000) << 24
                | (value & 0xFF000000) << 8
                | (value & 0xFF00000000) >> 8
                | (value & 0xFF0000000000) >> 24
                | (value & 0xFF000000000000) >> 40
                | ((value & 0xFF00000000000000) >> 56);
        }

        public static unsafe float Swap(float value)
        {
            FloatUnion input;

            input.value = value;

            FloatUnion output;

            //Silence compiler warning
            output.value = 0;

            output.bytes[0] = input.bytes[3];
            output.bytes[1] = input.bytes[2];
            output.bytes[2] = input.bytes[1];
            output.bytes[3] = input.bytes[0];

            return output.value;
        }

        public static unsafe double Swap(double value)
        {
            DoubleUnion input;

            input.value = value;

            DoubleUnion output;

            //Silence compiler warning
            output.value = 0;

            output.bytes[0] = input.bytes[7];
            output.bytes[1] = input.bytes[6];
            output.bytes[2] = input.bytes[5];
            output.bytes[3] = input.bytes[4];
            output.bytes[4] = input.bytes[3];
            output.bytes[5] = input.bytes[2];
            output.bytes[6] = input.bytes[1];
            output.bytes[7] = input.bytes[0];

            return output.value;
        }

        public static sbyte Little(sbyte value)
        {
            return value;
        }

        public static byte Little(byte value)
        {
            return value;
        }

        public static short Little(short value)
        {
            if (BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static ushort Little(ushort value)
        {
            if (BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static int Little(int value)
        {
            if (BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static uint Little(uint value)
        {
            if (BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static long Little(long value)
        {
            if (BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static ulong Little(ulong value)
        {
            if (BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static float Little(float value)
        {
            if (BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static double Little(double value)
        {
            if (BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static sbyte Big(sbyte value)
        {
            return value;
        }

        public static byte Big(byte value)
        {
            return value;
        }

        public static short Big(short value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static ushort Big(ushort value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static int Big(int value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static uint Big(uint value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static long Big(long value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static ulong Big(ulong value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static float Big(float value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }

        public static double Big(double value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return Swap(value);
            }
        }
    }
}
