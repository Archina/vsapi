﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common
{
    /// <summary>
    /// A shape element built from JSON data within the model.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ShapeElement
    {
        /// <summary>
        /// The name of the ShapeElement
        /// </summary>
        [JsonProperty]
        public string Name;


        [JsonProperty]
        public double[] From;
        [JsonProperty]
        public double[] To;

        /// <summary>
        /// Whether or not the shape element is shaded.
        /// </summary>
        [JsonProperty]
        public bool Shade = true;

        [JsonProperty]
        public bool GradientShade = false;

        /// <summary>
        /// The faces of the shape element by name.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, ShapeElementFace> Faces;

        /// <summary>
        /// The origin point for rotation.
        /// </summary>
        [JsonProperty]
        public double[] RotationOrigin;

        /// <summary>
        /// The forward vertical rotation of the shape element.
        /// </summary>
        [JsonProperty]
        public double RotationX;

        /// <summary>
        /// The forward vertical rotation of the shape element.
        /// </summary>
        [JsonProperty]
        public double RotationY;

        /// <summary>
        /// The left/right tilt of the shape element
        /// </summary>
        [JsonProperty]
        public double RotationZ;

        /// <summary>
        /// How far away are the left/right sides of the shape from the center
        /// </summary>
        [JsonProperty]
        public double ScaleX = 1;

        /// <summary>
        /// How far away are the top/bottom sides of the shape from the center
        /// </summary>
        [JsonProperty]
        public double ScaleY = 1;

        /// <summary>
        /// How far away are the front/back sides of the shape from the center.
        /// </summary>
        [JsonProperty]
        public double ScaleZ = 1;

        [JsonProperty]
        public string ClimateColorMap = null;
        [JsonProperty]
        public string SeasonColorMap = null;
        [JsonProperty]
        public short RenderPass = -1;
        [JsonProperty]
        public short ZOffset = 0;
        [JsonProperty]
        public bool FoliageWindWave;
        [JsonProperty]
        public bool WaterWave;
        [JsonProperty]
        public bool Reflective;

        /// <summary>
        /// The child shapes of this shape element
        /// </summary>
        [JsonProperty]
        public ShapeElement[] Children;

        /// <summary>
        /// The attachment points for this shape.
        /// </summary>
        [JsonProperty]
        public AttachmentPoint[] AttachmentPoints;

        /// <summary>
        /// The "remote" parent for this element
        /// </summary>
        [JsonProperty]
        public string StepParentName;

        /// <summary>
        /// The parent element reference for this shape.
        /// </summary>
        public ShapeElement ParentElement;

        /// <summary>
        /// The id of the joint attached to the parent element.
        /// </summary>
        public int JointId;


        public float[] inverseModelTransform;

        /// <summary>
        /// Walks the element tree and collects all parents, starting with the root element
        /// </summary>
        /// <returns></returns>
        public List<ShapeElement> GetParentPath()
        {
            List<ShapeElement> path = new List<ShapeElement>();
            ShapeElement parentElem = this.ParentElement;
            while (parentElem != null)
            {
                path.Add(parentElem);
                parentElem = parentElem.ParentElement;
            }
            path.Reverse();
            return path;
        }

        public void CacheInverseTransformMatrix()
        {
            if (inverseModelTransform == null)
            {
                inverseModelTransform = GetInverseModelMatrix();
            }
        }

        /// <summary>
        /// Returns the full inverse model matrix (includes all parent transforms)
        /// </summary>
        /// <returns></returns>
        public float[] GetInverseModelMatrix()
        {
            List<ShapeElement> elems = GetParentPath();

            float[] modelTransform = Mat4f.Create();

            for (int i = 0; i < elems.Count; i++)
            {
                ShapeElement elem = elems[i];
                float[] localTransform = elem.GetLocalTransformMatrix();
                Mat4f.Mul(modelTransform, modelTransform, localTransform);
            }

            Mat4f.Mul(modelTransform, modelTransform, GetLocalTransformMatrix());

            float[] inverseTransformMatrix = Mat4f.Invert(Mat4f.Create(), modelTransform);

            return inverseTransformMatrix;
        }


        internal void SetJointId(int jointId)
        {
            this.JointId = jointId;

            if (Children == null) return;
            for (int i = 0; i < Children.Length; i++)
            {
                Children[i].SetJointId(jointId);
            }
        }

        internal void ResolveRefernces()
        {
            if (Children != null)
            {
                for (int i = 0; i < Children.Length; i++)
                {
                    Children[i].ParentElement = this;
                    Children[i].ResolveRefernces();
                }
            }

            for (int i = 0; AttachmentPoints != null && i < AttachmentPoints.Length; i++)
            {
                AttachmentPoints[i].ParentElement = this;
            }
        }

        static ElementPose noTransform = new ElementPose();

        public float[] GetLocalTransformMatrix(float[] output = null, ElementPose tf = null)
        {
            if (tf == null) tf = noTransform;

            ShapeElement elem = this;
            if (output == null) output = Mat4f.Create();

            float[] origin = new float[] { 0f, 0f, 0f };

            if (elem.RotationOrigin != null)
            {
                origin[0] = (float)elem.RotationOrigin[0] / 16;
                origin[1] = (float)elem.RotationOrigin[1] / 16;
                origin[2] = (float)elem.RotationOrigin[2] / 16;
            }

            Mat4f.Translate(output, output, origin);

            if (elem.RotationX + tf.degX != 0)
            {
                Mat4f.RotateX(output, output, (float)(elem.RotationX + tf.degX) * GameMath.DEG2RAD);
            }
            if (elem.RotationY + tf.degY != 0)
            {
                Mat4f.RotateY(output, output, (float)(elem.RotationY + tf.degY) * GameMath.DEG2RAD);
            }
            if (elem.RotationZ + tf.degZ != 0)
            {
                Mat4f.RotateZ(output, output, (float)(elem.RotationZ + tf.degZ) * GameMath.DEG2RAD);
            }

            Mat4f.Scale(output, output, new float[] { (float)elem.ScaleX * tf.scaleX, (float)elem.ScaleY * tf.scaleY, (float)elem.ScaleZ * tf.scaleZ });

            Mat4f.Translate(output, output, new float[] { -origin[0], -origin[1], -origin[2] });

            Mat4f.Translate(output, output, new float[] {
                (float)elem.From[0] / 16 + tf.translateX,
                (float)elem.From[1] / 16 + tf.translateY,
                (float)elem.From[2] / 16 + tf.translateZ
            });



            return output;
        }

        public ShapeElement Clone()
        {
            ShapeElement elem = new ShapeElement()
            {
                AttachmentPoints = (AttachmentPoint[])AttachmentPoints?.Clone(),
                Faces = new Dictionary<string, ShapeElementFace>(Faces),
                From = (double[])From?.Clone(),
                To = (double[])To?.Clone(),
                inverseModelTransform = (float[])inverseModelTransform?.Clone(),
                JointId = JointId,
                RenderPass = RenderPass,
                RotationX = RotationX,
                RotationY = RotationY,
                RotationZ = RotationZ,
                Reflective = Reflective,
                RotationOrigin = (double[])RotationOrigin?.Clone(),
                SeasonColorMap = SeasonColorMap,
                ClimateColorMap = ClimateColorMap,
                StepParentName = StepParentName,
                Shade = Shade,
                ZOffset = ZOffset,
                GradientShade = GradientShade,
                ScaleX = ScaleX,
                ScaleY = ScaleY,
                ScaleZ = ScaleZ,
                Name = Name
            };

            if (Children != null)
            {
                elem.Children = new ShapeElement[Children.Length];
                for (int i = 0; i < Children.Length; i++)
                {
                    elem.Children[i] = Children[i].Clone();
                    elem.Children[i].ParentElement = elem;
                }
            }

            return elem;

        }

        public void SetJointIdRecursive(int jointId)
        {
            this.JointId = jointId;
            if (Children != null)
            {
                for (int i = 0; i < Children.Length; i++)
                {
                    Children[i].SetJointIdRecursive(jointId);
                }
            }

            CacheInverseTransformMatrix();
        }

        public void WalkRecursive(API.Common.Action<ShapeElement> onElem)
        {
            onElem(this);
            if (Children != null)
            {
                for (int i = 0; i < Children.Length; i++)
                {
                    Children[i].WalkRecursive(onElem);
                }
            }
        }
    }
}