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

namespace SharpLife.Networking.Shared.Communication.NetworkObjectLists
{
    /// <summary>
    /// Handle to an object
    /// </summary>
    public struct ObjectHandle : IEquatable<ObjectHandle>
    {
        public const ushort InvalidId = ushort.MaxValue;

        /// <summary>
        /// Maximum number of objects that can be created at any one time
        /// If you need more than this, change the following:
        /// <see cref="InvalidId"/>
        /// <see cref="_id"/>
        /// <see cref="_serialNumber"/>
        /// </summary>
        public const ushort MaxSupportedObjects = ushort.MaxValue - 1;

        //In a handle, the id is stored as id + 1 because 0 is the default value
        //This results in 0 becoming ushort.MaxValue, also used as the invalid id
        //This also results in invalid handles being serialized with fewer bits
        private readonly ushort _id;

        //Stored as a ushort so the entire handle is 32 bits large
        private readonly ushort _serialNumber;

        public int Id
        {
            //Wrap around to turn 0 into InvalidId
            get => unchecked((ushort)(_id - 1));
        }

        public int SerialNumber => _serialNumber;

        /// <summary>
        /// Tests if a handle is valid
        /// </summary>
        public bool Valid => Id != InvalidId;

        /// <summary>
        /// Creates a new handle from an id and serial number
        /// </summary>
        /// <param name="id"></param>
        /// <param name="serialNumber"></param>
        public ObjectHandle(int id, int serialNumber)
        {
            _id = (ushort)(id + 1);
            _serialNumber = (ushort)serialNumber;
        }

        public override string ToString()
        {
            return $"{Id}:{SerialNumber}";
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectHandle && Equals((ObjectHandle)obj);
        }

        public bool Equals(ObjectHandle other)
        {
            return Id == other.Id &&
                   SerialNumber == other.SerialNumber;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, SerialNumber);
        }

        public static bool operator ==(ObjectHandle handle1, ObjectHandle handle2)
        {
            return handle1.Equals(handle2);
        }

        public static bool operator !=(ObjectHandle handle1, ObjectHandle handle2)
        {
            return !(handle1 == handle2);
        }
    }
}
