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

using SharpLife.Models.MDL.FileFormat;
using SharpLife.Utility.Mathematics;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpLife.Models.MDL.Rendering
{
    /// <summary>
    /// Responsible for calculating the bone positions for a given model and state
    /// </summary>
    public sealed class StudioModelBoneCalculator
    {
        private StudioFile _currentModel;

        private readonly float[] _boneAdjust = new float[MDLConstants.MaxControllers];

        private readonly float[] _controllerNormalizers = new float[MDLConstants.MaxControllers]
        {
            255.0f,
            255.0f,
            255.0f,
            64.0f, //Mouth
            255.0f,
            255.0f,
            255.0f,
            255.0f
        };

        private readonly Vector3[] _pos1 = new Vector3[MDLConstants.MaxBones];
        private readonly Vector3[] _pos2 = new Vector3[MDLConstants.MaxBones];
        private readonly Vector3[] _pos3 = new Vector3[MDLConstants.MaxBones];
        private readonly Vector3[] _pos4 = new Vector3[MDLConstants.MaxBones];

        private readonly Quaternion[] _q1 = new Quaternion[MDLConstants.MaxBones];
        private readonly Quaternion[] _q2 = new Quaternion[MDLConstants.MaxBones];
        private readonly Quaternion[] _q3 = new Quaternion[MDLConstants.MaxBones];
        private readonly Quaternion[] _q4 = new Quaternion[MDLConstants.MaxBones];

        private readonly Matrix4x4[] _bones = new Matrix4x4[MDLConstants.MaxBones];

        private static bool ShouldCompensateForLoop(int controllerIndex)
        {
            return controllerIndex != MDLConstants.MouthControllerIndex;
        }

        public unsafe Matrix4x4[] SetUpBones(StudioFile studioFile, uint sequenceIndex, float frame, in BoneData boneData)
        {
            _currentModel = studioFile ?? throw new ArgumentNullException(nameof(studioFile));

            if (sequenceIndex >= _currentModel.Sequences.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(sequenceIndex));
            }

            var sequence = _currentModel.Sequences[(int)sequenceIndex];

            var animations = sequence.AnimationBlends;

            CalcRotations(boneData, _pos1, _q1, sequence, animations[0], frame);

            if (sequence.AnimationBlends.Count > 1)
            {
                CalcRotations(boneData, _pos2, _q2, sequence, animations[1], frame);
                var s = boneData.GetBlender(0) / 255.0f;

                SlerpBones(_q1, _pos1, _q2, _pos2, s);

                if (sequence.AnimationBlends.Count == 4)
                {
                    CalcRotations(boneData, _pos3, _q3, sequence, animations[2], frame);

                    CalcRotations(boneData, _pos4, _q4, sequence, animations[3], frame);

                    s = boneData.GetBlender(0) / 255.0f;
                    SlerpBones(_q3, _pos3, _q4, _pos4, s);

                    s = boneData.GetBlender(1) / 255.0f;
                    SlerpBones(_q1, _pos1, _q3, _pos3, s);
                }
            }

            var pbones = _currentModel.Bones;

            for (var i = 0; i < pbones.Count; ++i)
            {
                var bonematrix = Matrix4x4.CreateFromQuaternion(_q1[i]);

                bonematrix.Translation = _pos1[i];

                if (pbones[i].Parent == MDLConstants.NoBoneParent)
                {
                    _bones[i] = bonematrix;
                }
                else
                {
                    //TODO: verify that this is correct
                    _bones[i] = bonematrix * _bones[pbones[i].Parent];
                }
            }

            _currentModel = null;

            return _bones;
        }

        private unsafe void CalcRotations(in BoneData boneData,
            Vector3[] pos, Quaternion[] q,
            SequenceDescriptor sequence, AnimationBlend animationBlend,
            float f)
        {
            var frame = (int)f;
            var s = f - frame;

            // add in programatic controllers
            CalcBoneAdjust(boneData);

            var pbone = _currentModel.Bones;

            for (var i = 0; i < pbone.Count; ++i)
            {
                q[i] = CalcBoneQuaternion(frame, s, pbone[i], animationBlend.Animations[i]);
                pos[i] = CalcBonePosition(frame, s, pbone[i], animationBlend.Animations[i]);
            }

            var pPos = (float*)Unsafe.AsPointer(ref pos[sequence.MotionBone]);

            if ((sequence.MotionType & MotionTypes.X) != 0)
            {
                pPos[0] = 0.0f;
            }

            if ((sequence.MotionType & MotionTypes.Y) != 0)
            {
                pPos[1] = 0.0f;
            }

            if ((sequence.MotionType & MotionTypes.Z) != 0)
            {
                pPos[2] = 0.0f;
            }
        }

        /// <summary>
        /// Calculate the adjustment settings for each controller
        /// TODO: this is called multiple times per bone setup, but doesn't rely on data that changes between calls during setup
        /// Move call to reduce cost?
        /// </summary>
        /// <param name="boneData"></param>
        private void CalcBoneAdjust(in BoneData boneData)
        {
            for (var j = 0; j < _currentModel.BoneControllers.Count; ++j)
            {
                var controller = _currentModel.BoneControllers[j];

                float value;

                // check for 360% wrapping
                //TODO: this code does not match the game's code
                if (ShouldCompensateForLoop(controller.Index) && (controller.Type & MotionTypes.RLoop) != 0)
                {
                    value = (boneData.GetController(controller.Index) * (360.0f / 256.0f)) + controller.Start;
                }
                else
                {
                    value = boneData.GetController(controller.Index) / _controllerNormalizers[controller.Index];

                    value = Math.Clamp(value, 0.0f, 1.0f);

                    value = ((1.0f - value) * controller.Start) + (value * controller.End);
                }

                switch (controller.Type & MotionTypes.Types)
                {
                    case MotionTypes.XR:
                    case MotionTypes.YR:
                    case MotionTypes.ZR:
                        _boneAdjust[j] = MathUtils.ToRadians(value);
                        break;

                    case MotionTypes.X:
                    case MotionTypes.Y:
                    case MotionTypes.Z:
                        _boneAdjust[j] = value;
                        break;
                }
            }
        }

        private unsafe Vector3 CalcBonePosition(int frame, float s, Bone bone, Animation animation)
        {
            var pos = new Vector3();

            var pPos = (float*)Unsafe.AsPointer(ref pos);

            for (var i = 0; i < 3; ++i)
            {
                pPos[i] = bone.Values[i]; // default;

                if (animation.Values[i] != null)
                {
                    var values = animation.Values[i];

                    //If there's another frame, interpolate
                    if (values.Count > frame + 1)
                    {
                        pPos[i] += ((values[frame] * (1.0f - s)) + (s * values[frame + 1])) * bone.Scales[i];
                    }
                    else
                    {
                        pPos[i] += values[frame] * bone.Scales[i];
                    }
                }

                if (bone.BoneControllers[i] != MDLConstants.NoBoneController)
                {
                    pPos[i] += _boneAdjust[bone.BoneControllers[i]];
                }
            }

            return pos;
        }

        /// <summary>
        /// Calculate the rotation of the given bone
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="s"></param>
        /// <param name="bone"></param>
        /// <param name="animation"></param>
        /// <returns></returns>
        private unsafe Quaternion CalcBoneQuaternion(int frame, float s, Bone bone, Animation animation)
        {
            var angle1 = new Vector3();
            var angle2 = new Vector3();

            var pAngle1 = (float*)Unsafe.AsPointer(ref angle1);
            var pAngle2 = (float*)Unsafe.AsPointer(ref angle2);

            for (var i = 0; i < 3; ++i)
            {
                var axis = i + 3;

                if (animation.Values[axis] == null)
                {
                    pAngle2[i] = pAngle1[i] = bone.Values[axis]; // default;
                }
                else
                {
                    var values = animation.Values[axis];

                    pAngle1[i] = values[frame];

                    if (values.Count > frame + 1)
                    {
                        pAngle2[i] = values[frame + 1];
                    }
                    else
                    {
                        pAngle2[i] = pAngle1[i];
                    }

                    pAngle1[i] = bone.Values[axis] + (pAngle1[i] * bone.Scales[axis]);
                    pAngle2[i] = bone.Values[axis] + (pAngle2[i] * bone.Scales[axis]);
                }

                if (bone.BoneControllers[axis] != MDLConstants.NoBoneController)
                {
                    pAngle1[i] += _boneAdjust[bone.BoneControllers[axis]];
                    pAngle2[i] += _boneAdjust[bone.BoneControllers[axis]];
                }
            }

            if (!VectorUtils.VectorsEqual(angle1, angle2))
            {
                var q1 = QuaternionUtils.AngleToQuaternion(angle1);
                var q2 = QuaternionUtils.AngleToQuaternion(angle2);
                return Quaternion.Slerp(q1, q2, s);
            }
            else
            {
                return QuaternionUtils.AngleToQuaternion(angle1);
            }
        }

        /// <summary>
        /// Interpolate bones between frame positions using spherical linear interpolation
        /// </summary>
        /// <param name="q1"></param>
        /// <param name="pos1"></param>
        /// <param name="q2"></param>
        /// <param name="pos2"></param>
        /// <param name="s"></param>
        private unsafe void SlerpBones(Quaternion[] q1, Vector3[] pos1, Quaternion[] q2, Vector3[] pos2, float s)
        {
            s = Math.Clamp(s, 0.0f, 1.0f);

            var s1 = 1.0f - s;

            for (var i = 0; i < _currentModel.Bones.Count; ++i)
            {
                var q3 = Quaternion.Slerp(q1[i], q2[i], s);

                var pQ1 = (float*)Unsafe.AsPointer(ref q1[i]);
                var pQ3 = (float*)Unsafe.AsPointer(ref q3);

                pQ1[0] = pQ3[0];
                pQ1[1] = pQ3[1];
                pQ1[2] = pQ3[2];
                pQ1[3] = pQ3[3];

                var pPos1 = (float*)Unsafe.AsPointer(ref pos1[i]);
                var pPos2 = (float*)Unsafe.AsPointer(ref pos2[i]);

                pPos1[0] = (pPos1[0] * s1) + (pPos2[0] * s);
                pPos1[1] = (pPos1[1] * s1) + (pPos2[1] * s);
                pPos1[2] = (pPos1[2] * s1) + (pPos2[2] * s);
            }
        }
    }
}
