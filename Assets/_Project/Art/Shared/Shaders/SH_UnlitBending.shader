Shader "Custom/SH_UnlitBending"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)

        [Enum(X toward Y, 0, X toward Z, 1, Y toward X, 2, Y toward Z, 3, Z toward X, 4, Z toward Y, 5)]
        _BendPlane ("Local Bend Plane", Float) = 1
        _BendRadius ("Bend Radius (Object Units)", Float) = 5
        _BendAmount ("Bend Amount", Range(-1, 1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _BendPlane;
                float _BendRadius;
                float _BendAmount;
                float _Cull;
            CBUFFER_END

            void GetBendAxes(out float3 tangentAxis, out float3 bendDirection)
            {
                if (_BendPlane < 0.5)
                {
                    tangentAxis = float3(1.0, 0.0, 0.0);
                    bendDirection = float3(0.0, 1.0, 0.0);
                }
                else if (_BendPlane < 1.5)
                {
                    tangentAxis = float3(1.0, 0.0, 0.0);
                    bendDirection = float3(0.0, 0.0, 1.0);
                }
                else if (_BendPlane < 2.5)
                {
                    tangentAxis = float3(0.0, 1.0, 0.0);
                    bendDirection = float3(1.0, 0.0, 0.0);
                }
                else if (_BendPlane < 3.5)
                {
                    tangentAxis = float3(0.0, 1.0, 0.0);
                    bendDirection = float3(0.0, 0.0, 1.0);
                }
                else if (_BendPlane < 4.5)
                {
                    tangentAxis = float3(0.0, 0.0, 1.0);
                    bendDirection = float3(1.0, 0.0, 0.0);
                }
                else
                {
                    tangentAxis = float3(0.0, 0.0, 1.0);
                    bendDirection = float3(0.0, 1.0, 0.0);
                }
            }

            float3 BendPositionOS(float3 positionOS)
            {
                float radius = max(abs(_BendRadius), 0.0001);
                float curvature = _BendAmount / radius;

                if (abs(curvature) < 0.00001)
                    return positionOS;

                float3 tangentAxis;
                float3 bendDirection;
                GetBendAxes(tangentAxis, bendDirection);

                float tangentDistance = dot(positionOS, tangentAxis);
                float depth = dot(positionOS, bendDirection);
                float3 unchangedAxes = positionOS
                    - tangentAxis * tangentDistance
                    - bendDirection * depth;

                float angle = tangentDistance * curvature;
                float sinAngle;
                float cosAngle;
                sincos(angle, sinAngle, cosAngle);

                // Keep the pivot fixed and rotate each cross-section along a circular arc.
                float curvedTangent = sinAngle / curvature - depth * sinAngle;
                float curvedDepth = (1.0 - cosAngle) / curvature + depth * cosAngle;

                return unchangedAxes
                    + tangentAxis * curvedTangent
                    + bendDirection * curvedDepth;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 bentPositionOS = BendPositionOS(input.positionOS.xyz);
                output.positionCS = TransformObjectToHClip(bentPositionOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
