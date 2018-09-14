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

using SharpLife.FileFormats.MDL.Disk;
using SharpLife.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpLife.FileFormats.MDL
{
    public static class StudioIOUtils
    {
        internal static unsafe List<AnimationBlend> ReadAnimationBlends(
            BinaryReader reader,
            int numBones,
            int baseIndex,
            in Disk.SequenceDescriptor rawSequence,
            SequenceDescriptor sequence)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var list = new List<AnimationBlend>();

            //Each blend has an animation for each bone
            //Each animation has values for translation and rotation
            for (var blend = 0; blend < rawSequence.NumBlends; ++blend)
            {
                var animationPosition = baseIndex + rawSequence.AnimIndex + (Marshal.SizeOf<Disk.Animation>() * numBones * blend);

                reader.BaseStream.Position = animationPosition;

                //Read the raw animation data first
                var rawAnimations = new Disk.Animation[numBones];

                for (var bone = 0; bone < numBones; ++bone)
                {
                    rawAnimations[bone] = reader.ReadStructure<Disk.Animation>();

                    for (var axis = 0; axis < MDLConstants.NumAxes; ++axis)
                    {
                        rawAnimations[bone].Offset[axis] = EndianConverter.Little(rawAnimations[bone].Offset[axis]);
                    }
                }

                //Read each animation
                var animations = new List<Animation>(numBones);

                for (var bone = 0; bone < numBones; ++bone)
                {
                    ref var rawAnimation = ref rawAnimations[bone];

                    var animation = new Animation();

                    var offsets = new ushort[6];

                    for (var i = 0; i < 6; ++i)
                    {
                        offsets[i] = rawAnimation.Offset[i];
                    }

                    for (var axis = 0; axis < MDLConstants.NumAxes; ++axis)
                    {
                        //Leave the list null if there is no offset
                        if (rawAnimation.Offset[axis] != 0)
                        {
                            reader.BaseStream.Position = animationPosition + (Marshal.SizeOf<Disk.Animation>() * bone) + rawAnimation.Offset[axis];

                            var values = new List<short>(sequence.FrameCount);

                            for (var frame = 0; frame < sequence.FrameCount;)
                            {
                                var count = reader.ReadStructure<AnimationValue>();

                                int i;

                                //Add all valid values
                                for (i = 0; i < count.Num.Valid; ++i)
                                {
                                    var animValue = reader.ReadStructure<AnimationValue>();

                                    values.Add(animValue.Value);
                                }

                                //If it's a run, convert it
                                if (i < count.Num.Total)
                                {
                                    var value = values[values.Count - 1];

                                    for (; i < count.Num.Total; ++i)
                                    {
                                        values.Add(value);
                                    }
                                }

                                frame += count.Num.Total;
                            }

                            animation.Values[axis] = values;
                        }
                    }

                    animations.Add(animation);
                }

                list.Add(new AnimationBlend
                {
                    Animations = animations
                });
            }

            return list;
        }
    }
}
