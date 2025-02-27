﻿//This is from Mark Morley's tutorial on frustum culling.
//http://www.crownandcutlass.com/features/technicaldetails/frustum.html
//"This page and its contents are Copyright 2000 by Mark Morley
//Unless otherwise noted, you may use any and all code examples provided herein in any way you want.
//All other content, including but not limited to text and images, may not be reproduced without consent.
//This file was last edited on Wednesday, 24-Jan-2001 13:24:38 PST"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client
{
    public enum EnumFrustumCullMode
    {
        NoCull = 0,
        CullNormal = 1,
        CullInstant = 2,
        CullInstantShadowPassNear = 3,
        CullInstantShadowPassFar = 4
    }


    public class FrustumCulling
    {
        public int ViewDistanceSq;
        internal BlockPos playerPos;
        public float lodBiasSq;
        public double shadowRangeX;
        public double shadowRangeZ;

        double frustum00;
        double frustum01;
        double frustum02;
        double frustum03;

        double frustum10;
        double frustum11;
        double frustum12;
        double frustum13;

        double frustum20;
        double frustum21;
        double frustum22;
        double frustum23;

        double frustum30;
        double frustum31;
        double frustum32;
        double frustum33;

        double frustum40;
        double frustum41;
        double frustum42;
        double frustum43;

        double frustum50;
        double frustum51;
        double frustum52;
        double frustum53;

        public bool SphereInFrustum(double x, double y, double z, double radius)
        {
            double d = 0;

            d = frustum00 * x + frustum01 * y + frustum02 * z + frustum03;
            if (d <= -radius)
                return false;
            d = frustum10 * x + frustum11 * y + frustum12 * z + frustum13;
            if (d <= -radius)
                return false;
            d = frustum20 * x + frustum21 * y + frustum22 * z + frustum23;
            if (d <= -radius)
                return false;
            d = frustum30 * x + frustum31 * y + frustum32 * z + frustum33;
            if (d <= -radius)
                return false;
            d = frustum40 * x + frustum41 * y + frustum42 * z + frustum43;
            if (d <= -radius)
                return false;
            d = frustum50 * x + frustum51 * y + frustum52 * z + frustum53;
            if (d <= -radius)
                return false;

            return true;
        }


        public bool SphereInFrustum(Sphere sphere)
        {
            double d;

            d = frustum00 * sphere.x + frustum01 * sphere.y + frustum02 * sphere.z + frustum03;
            if (d <= -sphere.radius)
                return false;
            d = frustum10 * sphere.x + frustum11 * sphere.y + frustum12 * sphere.z + frustum13;
            if (d <= -sphere.radius)
                return false;
            d = frustum20 * sphere.x + frustum21 * sphere.y + frustum22 * sphere.z + frustum23;
            if (d <= -sphere.radius)
                return false;
            d = frustum30 * sphere.x + frustum31 * sphere.y + frustum32 * sphere.z + frustum33;
            if (d <= -sphere.radius)
                return false;
            d = frustum40 * sphere.x + frustum41 * sphere.y + frustum42 * sphere.z + frustum43;
            if (d <= -sphere.radius)
                return false;
            d = frustum50 * sphere.x + frustum51 * sphere.y + frustum52 * sphere.z + frustum53;
            if (d <= -sphere.radius)
                return false;

            return true;
        }


        public bool SphereInFrustumShadowPass(Sphere sphere)
        {
            double d;

            d = frustum00 * sphere.x + frustum01 * sphere.y + frustum02 * sphere.z + frustum03;
            if (d <= -sphere.radius)
                return false;
            d = frustum10 * sphere.x + frustum11 * sphere.y + frustum12 * sphere.z + frustum13;
            if (d <= -sphere.radius)
                return false;
            d = frustum20 * sphere.x + frustum21 * sphere.y + frustum22 * sphere.z + frustum23;
            if (d <= -sphere.radius)
                return false;
            d = frustum30 * sphere.x + frustum31 * sphere.y + frustum32 * sphere.z + frustum33;
            if (d <= -sphere.radius)
                return false;
            d = frustum40 * sphere.x + frustum41 * sphere.y + frustum42 * sphere.z + frustum43;
            if (d <= -sphere.radius)
                return false;
            d = frustum50 * sphere.x + frustum51 * sphere.y + frustum52 * sphere.z + frustum53;
            if (d <= -sphere.radius)
                return false;

            double distx = Math.Abs(playerPos.X - sphere.x);
            double distz = Math.Abs(playerPos.Z - sphere.z);

            return (distx < shadowRangeX && distz < shadowRangeZ);// && lodLevel == 1) || (distx < shadowRangeX * lodBias + 24 && distz < shadowRangeZ * lodBias + 24);
        }


        public bool SphereInFrustumAndRange(Sphere sphere, bool nowVisible, int lodLevel = 0)
        {
            double d;

            d = frustum00 * sphere.x + frustum01 * sphere.y + frustum02 * sphere.z + frustum03;
            if (d <= -sphere.radius)
                return false;
            d = frustum10 * sphere.x + frustum11 * sphere.y + frustum12 * sphere.z + frustum13;
            if (d <= -sphere.radius)
                return false;
            d = frustum20 * sphere.x + frustum21 * sphere.y + frustum22 * sphere.z + frustum23;
            if (d <= -sphere.radius)
                return false;
            d = frustum30 * sphere.x + frustum31 * sphere.y + frustum32 * sphere.z + frustum33;
            if (d <= -sphere.radius)
                return false;
            d = frustum40 * sphere.x + frustum41 * sphere.y + frustum42 * sphere.z + frustum43;
            if (d <= -sphere.radius)
                return false;
            d = frustum50 * sphere.x + frustum51 * sphere.y + frustum52 * sphere.z + frustum53;
            if (d <= -sphere.radius)
                return false;

            // Lod level 1: all stuff
            // Lod level 0: only high detail stuff

            
            double distance = playerPos.HorDistanceSqTo(sphere.x, sphere.z);
            

            return (distance < ViewDistanceSq && lodLevel == 1) || (lodBiasSq > 0 && distance < ViewDistanceSq * lodBiasSq + 32*32);
        }


        double[] tmpMat = new double[16];

        public void CalcFrustumEquations(BlockPos playerPos, double[] projectionMatrix, double[] cameraMatrix)
        {
            double[] matFrustum = Mat4d.Create();

            Mat4d.Multiply(matFrustum, projectionMatrix, cameraMatrix);

            for (int i = 0; i < 16; i++) tmpMat[i] = matFrustum[i];

            CalcFrustumEquations(playerPos, tmpMat);
        }

        /// <summary>
        /// Calculating the frustum planes.
        /// </summary>
        /// <remarks>
        /// From the current OpenGL modelview and projection matrices,
        /// calculate the frustum plane equations (Ax+By+Cz+D=0, n=(A,B,C))
        /// The equations can then be used to see on which side points are.
        /// </remarks>
        public void CalcFrustumEquations(BlockPos playerPos, double[] frustumMatrix)
        {
            this.playerPos = playerPos;
            double t;

            unchecked
            {
                double[] clip1 = frustumMatrix;

                // Extract the numbers for the RIGHT plane
                frustum00 = clip1[3] - clip1[0];
                frustum01 = clip1[7] - clip1[4];
                frustum02 = clip1[11] - clip1[8];
                frustum03 = clip1[15] - clip1[12];

                // Normalize the result
                t = Math.Sqrt(frustum00 * frustum00 + frustum01 * frustum01 + frustum02 * frustum02);
                frustum00 /= t;
                frustum01 /= t;
                frustum02 /= t;
                frustum03 /= t;

                // Extract the numbers for the LEFT plane
                frustum10 = clip1[3] + clip1[0];
                frustum11 = clip1[7] + clip1[4];
                frustum12 = clip1[11] + clip1[8];
                frustum13 = clip1[15] + clip1[12];

                // Normalize the result
                t = Math.Sqrt(frustum10 * frustum10 + frustum11 * frustum11 + frustum12 * frustum12);
                frustum10 /= t;
                frustum11 /= t;
                frustum12 /= t;
                frustum13 /= t;

                // Extract the BOTTOM plane
                frustum20 = clip1[3] + clip1[1];
                frustum21 = clip1[7] + clip1[5];
                frustum22 = clip1[11] + clip1[9];
                frustum23 = clip1[15] + clip1[13];

                // Normalize the result
                t = Math.Sqrt(frustum20 * frustum20 + frustum21 * frustum21 + frustum22 * frustum22);
                frustum20 /= t;
                frustum21 /= t;
                frustum22 /= t;
                frustum23 /= t;

                // Extract the TOP plane
                frustum30 = clip1[3] - clip1[1];
                frustum31 = clip1[7] - clip1[5];
                frustum32 = clip1[11] - clip1[9];
                frustum33 = clip1[15] - clip1[13];

                // Normalize the result
                t = Math.Sqrt(frustum30 * frustum30 + frustum31 * frustum31 + frustum32 * frustum32);
                frustum30 /= t;
                frustum31 /= t;
                frustum32 /= t;
                frustum33 /= t;

                // Extract the FAR plane
                frustum40 = clip1[3] - clip1[2];
                frustum41 = clip1[7] - clip1[6];
                frustum42 = clip1[11] - clip1[10];
                frustum43 = clip1[15] - clip1[14];

                // Normalize the result
                t = Math.Sqrt(frustum40 * frustum40 + frustum41 * frustum41 + frustum42 * frustum42);
                frustum40 /= t;
                frustum41 /= t;
                frustum42 /= t;
                frustum43 /= t;

                // Extract the NEAR plane
                frustum50 = clip1[3] + clip1[2];
                frustum51 = clip1[7] + clip1[6];
                frustum52 = clip1[11] + clip1[10];
                frustum53 = clip1[15] + clip1[14];

                // Normalize the result
                t = Math.Sqrt(frustum50 * frustum50 + frustum51 * frustum51 + frustum52 * frustum52);
                frustum50 /= t;
                frustum51 /= t;
                frustum52 /= t;
                frustum53 /= t;
            }
        }
    }
}
