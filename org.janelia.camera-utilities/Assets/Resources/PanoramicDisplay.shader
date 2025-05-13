Shader "Unlit/PanoramicDisplay"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            // TODO: Why does it not work to add `USE_MASK USE_COLOR_CORRECTION` here, and use them in
            // `if` statements where `_TexMask` and `_TexColorCorrection` are used below, with 
            // `PanoramicDisplayCamera` calling `_material.EnableKeyword("USE_MASK")` and
            // `_material.EnableKeyword("USE_COLOR_CORRECTION")` when the texture data is not null?
            // This extension would be convenient, as client code would not have to specify the texture
            // data when it is not needed, but the `EnableKeyword` calls don't seem to have any effect.

            #pragma multi_compile __ SIX_CAMERAS

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // This shader constructs a panoramic image by remapping the rendered images from the following camera.
            // Typically, the cameras are arranged so their views form a box around a given point (i.e., the cameras are
            // looking north, south, east, west, down, and optionally, up). In this typical case, the position, near and fov
            // values will be the same for all cameras, but there is an independent input for each to allow esoteric uses.

            // It would be nice to use a struct to collect the camera parmeters.  But doing so and passing either the whole struct
            // or its fields to subroutines produces errors of the form:
            // "Only instancing constant buffers can have struct variables"

            float3 _Camera0Position;
            float3 _Camera0Forward;
            float3 _Camera0Up;
            float3 _Camera0Right;
            float _Camera0Near;
            float _Camera0FovHoriz;
            float _Camera0FovVert;
            Texture2D _TexCamera0;
            SamplerState sampler_TexCamera0;

            float4 _TexCamera0_TexelSize;

            float3 _Camera1Position;
            float3 _Camera1Forward;
            float3 _Camera1Up;
            float3 _Camera1Right;
            float _Camera1Near;
            float _Camera1FovHoriz;
            float _Camera1FovVert;
            Texture2D _TexCamera1;
            SamplerState sampler_TexCamera1;

            float4 _TexCamera1_TexelSize;

            float3 _Camera2Position;
            float3 _Camera2Forward;
            float3 _Camera2Up;
            float3 _Camera2Right;
            float _Camera2Near;
            float _Camera2FovHoriz;
            float _Camera2FovVert;
            Texture2D _TexCamera2;
            SamplerState sampler_TexCamera2;

            float4 _TexCamera2_TexelSize;

            float3 _Camera3Position;
            float3 _Camera3Forward;
            float3 _Camera3Up;
            float3 _Camera3Right;
            float _Camera3Near;
            float _Camera3FovHoriz;
            float _Camera3FovVert;
            Texture2D _TexCamera3;
            SamplerState sampler_TexCamera3;

            float4 _TexCamera3_TexelSize;

            float3 _Camera4Position;
            float3 _Camera4Forward;
            float3 _Camera4Up;
            float3 _Camera4Right;
            float _Camera4Near;
            float _Camera4FovHoriz;
            float _Camera4FovVert;
            Texture2D _TexCamera4;
            SamplerState sampler_TexCamera4;

            float4 _TexCamera4_TexelSize;

            // This sixth camera typically would be looking up to make the top of the box. Not all applicaitons need that
            // top view, though, so this camera is used only if `EnableKeyword("SIX_CAMERAS")` is called on the material
            // using this shader. See `Janelia.PanoramicDisplayCamera.EnableSixthCamera()`.

            float3 _Camera5Position;
            float3 _Camera5Forward;
            float3 _Camera5Up;
            float3 _Camera5Right;
            float _Camera5Near;
            float _Camera5FovHoriz;
            float _Camera5FovVert;
            Texture2D _TexCamera5;
            SamplerState sampler_TexCamera5;

            float4 _TexCamera5_TexelSize;

            sampler2D _TexProjectorSurfaceX;
            sampler2D _TexProjectorSurfaceY;
            sampler2D _TexProjectorSurfaceZ;

            // This texture and scalar value implement a mask for each pixel of the final image.
            // The computed color for the pixel is multipled by `(1 - _MaskScale * M)` were
            // `M` is the `_TexMask` value for the pixel.  So if `_MaskScale` is 1, then a
            // `_TexMask` value of 0 leaves the color unchanged and a value of 255 turns it black.
            // If `_MaskScale` is between 0 and 1 then it scales the effect of `_TexMask` in a way
            // that is useful for brightness compensation with a projectors shining on a cylindrical
            // screen; see the comments in `Janelia.ExampleUsingPanoramicDisplayCamera`.

            sampler2D _TexMask;
            float _MaskScale;

            sampler2D _TexColorCorrection;
            float _ColorCorrectionScale;

            // A value of 1 enables a wider filter kernel (i.e., more blurring) at the bottom of the
            // source camera images.  For an observer looking at an image projected on the ground,
            // the bottoms of the source camera images have the pixels that are close to the observer,
            // and thus large and in need of extra filtering to avoid aliasing.
            int _BottomBias = 0;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 averageAdjacent(float interU, float interV, float4 texelSize, Texture2D tex, SamplerState texSampler)
            {
                float2 i = float2(interU, interV);
                if (_BottomBias == 0)
                {
                    fixed4 result = tex.Sample(texSampler, i);

                    // Simple anti-aliasing by averaging adjacent pixels.
                    float2 offset = texelSize.xy;
                    result += tex.Sample(texSampler, i + float2( offset.x,  offset.y));
                    result += tex.Sample(texSampler, i + float2(-offset.x,  offset.y));
                    result += tex.Sample(texSampler, i + float2(-offset.x, -offset.y));
                    result += tex.Sample(texSampler, i + float2( offset.x, -offset.y));
                    result /= 5;

                    return result;
                }
                else
                {
                    // Use a Gaussian filter with sample spacing that gets bigger closer to the bottom
                    // of the source images, because those pixels appear bigger and more aliased in a
                    // panorama for overhead projection onto the floor around a viewer.  Those pixels have
                    // a lower `interV`.
                    float scale = 1;
                    const float boundaryV = 0.5f;
                    if (interV < boundaryV)
                    {
                        scale += (boundaryV - interV) / boundaryV * 17;
                    }
                    float2 sampleStep = texelSize.xy * scale;

                    fixed4 col = fixed4(0, 0, 0, 0);
                    float totalWeight = 0.0;

                    int samples = 10;
                    float center = (samples - 1) / 2.0;
                    for (int x = 0; x < samples; x++)
                    {
                        for (int y = 0; y < samples; y++)
                        {
                            float2 offset = float2(x, y) - center;

                            // Gaussian weight. Would a lookup table be faster?
                            float weight = exp(-dot(offset, offset) * 0.5);

                            float2 sampleUV = i.xy + offset * sampleStep;
                            col += tex.Sample(texSampler, sampleUV) * weight;
                            totalWeight += weight;
                        }
                    }

                    return col / totalWeight;
                }
            }

            bool rayIntersects(float3 projectorSurfacePt, float3 camPos, float3 camForward, float3 camUp, float3 camRight, float camNear, float camFovHoriz, float camFovVert, out float interDist, out float interU, out float interV)
            {
                float3 rayPt = camPos;
                float3 rayDir = normalize(projectorSurfacePt - rayPt);

                float3 planePt = camPos + camForward * camNear;
                float3 planeNorm = camForward;

                float EPS = 1e-10f;
                float planeNormDotRayDir = dot(planeNorm, rayDir);

                // Assume the ray goes from the eye point out to the floor pixel, and the plane normal points
                // out from the camera view plane. Thus, a negative dot product means the ray is moving away from
                // the plane and never can hit it. A dot product close to zero (within EPS) means the ray is
                // parallel to the plane and never can hit it.
                if (planeNormDotRayDir < EPS)
                {
                    interDist = interU = interV = 0;
                    return false;
                }

                float3 rayPtToPlanePt = planePt - rayPt;
                interDist = dot(rayPtToPlanePt, planeNorm) / planeNormDotRayDir;
                float3 interPt = rayPt + interDist * rayDir;
                float3 planePtToInterPt = interPt - planePt;

                // Extend the valid region a bit, to avoid occasional cracks between the cameras.
                float extra = 0.01F * camNear;

                float distHoriz = dot(planePtToInterPt, camRight);
                float distHorizMax = tan(radians(camFovHoriz / 2)) * camNear;
                if (abs(distHoriz) > distHorizMax + extra)
                {
                    interDist = interU = interV = 0;
                    return false;
                }

                float distVert = dot(planePtToInterPt, camUp);
                float distVertMax = tan(radians(camFovVert / 2)) * camNear;
                if (abs(distVert) > distVertMax + extra)
                {
                    interDist = interU = interV = 0;
                    return false;
                }

                interU = (distHoriz + distHorizMax) / (2 * distHorizMax);
                interV = (distVert + distVertMax) / (2 * distVertMax);
                return true;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float x = tex2D(_TexProjectorSurfaceX, i.uv);
                float y = tex2D(_TexProjectorSurfaceY, i.uv);
                float z = tex2D(_TexProjectorSurfaceZ, i.uv);
                float3 projectorSurfacePt = float3(x, y, z);
                float interDist;
                float interU = 0;
                float interV = 0;
                float interDistMin = 1.0e+20F;
                fixed4 result = fixed4(0, 0, 0, 0);

                if (rayIntersects(projectorSurfacePt, _Camera0Position, _Camera0Forward, _Camera0Up, _Camera0Right, _Camera0Near, _Camera0FovHoriz, _Camera0FovVert, interDist, interU, interV))
                {
                    if (interDist <= interDistMin)
                    {
                        interDistMin = interDist;
                        result = averageAdjacent(interU, interV, _TexCamera0_TexelSize, _TexCamera0, sampler_TexCamera0);
                    }
                }
                if (rayIntersects(projectorSurfacePt, _Camera1Position, _Camera1Forward, _Camera1Up, _Camera1Right, _Camera1Near, _Camera1FovHoriz, _Camera1FovVert, interDist, interU, interV))
                {
                    if (interDist <= interDistMin)
                    {
                        interDistMin = interDist;
                        result = averageAdjacent(interU, interV, _TexCamera1_TexelSize, _TexCamera1, sampler_TexCamera1);
                    }
                }
                if (rayIntersects(projectorSurfacePt, _Camera2Position, _Camera2Forward, _Camera2Up, _Camera2Right, _Camera2Near, _Camera2FovHoriz, _Camera2FovVert, interDist, interU, interV))
                {
                    if (interDist <= interDistMin)
                    {
                        interDistMin = interDist;
                        result = averageAdjacent(interU, interV, _TexCamera2_TexelSize, _TexCamera2, sampler_TexCamera2);
                    }
                }
                if (rayIntersects(projectorSurfacePt, _Camera3Position, _Camera3Forward, _Camera3Up, _Camera3Right, _Camera3Near, _Camera3FovHoriz, _Camera3FovVert, interDist, interU, interV))
                {
                    if (interDist < interDistMin)
                    {
                        interDistMin = interDist;
                        result = averageAdjacent(interU, interV, _TexCamera3_TexelSize, _TexCamera3, sampler_TexCamera3);
                    }
                }
                if (rayIntersects(projectorSurfacePt, _Camera4Position, _Camera4Forward, _Camera4Up, _Camera4Right, _Camera4Near, _Camera4FovHoriz, _Camera4FovVert, interDist, interU, interV))
                {
                    if (interDist <= interDistMin)
                    {
                        interDistMin = interDist;
                        result = averageAdjacent(interU, interV, _TexCamera4_TexelSize, _TexCamera4, sampler_TexCamera4);
                    }
                }

                if (SIX_CAMERAS)
                {
                    if (rayIntersects(projectorSurfacePt, _Camera5Position, _Camera5Forward, _Camera5Up, _Camera5Right, _Camera5Near, _Camera5FovHoriz, _Camera5FovVert, interDist, interU, interV))
                    {
                        if (interDist <= interDistMin)
                        {
                            interDistMin = interDist;
                            result = averageAdjacent(interU, interV, _TexCamera5_TexelSize, _TexCamera5, sampler_TexCamera5);
                        }
                    }
                }

                float mask0 = tex2D(_TexMask, i.uv);
                float mask = (1 - _MaskScale * mask0);
                result *= mask;

                fixed4 colorCorrection = tex2D(_TexColorCorrection, i.uv);
                fixed4 scaledCorrection = fixed4(1, 1, 1, 1) - _ColorCorrectionScale * colorCorrection;
                result *= scaledCorrection;

                return result;
            }

            ENDCG
        }
    }
}
