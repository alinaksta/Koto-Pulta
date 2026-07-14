Shader "Custom/SH_UnlitFloorWater"
{
    Properties
    {
        [MainTexture] _BaseMap ("Floor Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Floor Color", Color) = (1, 1, 1, 1)

        _Caustics1 ("Caustics Layer 1", 2D) = "black" {}
        _Caustics1Velocity ("Layer 1 Velocity (XY)", Vector) = (0.04, 0.01, 0, 0)
        _Caustics1MinOpacity ("Layer 1 Minimum Opacity", Range(0, 1)) = 0.1
        _Caustics1MaxOpacity ("Layer 1 Maximum Opacity", Range(0, 1)) = 0.5
        _Caustics1StopFrequency ("Layer 1 Stop Cycles / Second", Range(0.02, 2)) = 0.2

        _Caustics2 ("Caustics Layer 2", 2D) = "black" {}
        _Caustics2Velocity ("Layer 2 Velocity (XY)", Vector) = (-0.02, 0.035, 0, 0)
        _Caustics2MinOpacity ("Layer 2 Minimum Opacity", Range(0, 1)) = 0.1
        _Caustics2MaxOpacity ("Layer 2 Maximum Opacity", Range(0, 1)) = 0.35
        _Caustics2StopFrequency ("Layer 2 Stop Cycles / Second", Range(0.02, 2)) = 0.27
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

            Cull Back
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
                float2 baseUV : TEXCOORD0;
                float2 caustics1UV : TEXCOORD1;
                float2 caustics2UV : TEXCOORD2;
                float2 causticsOpacity : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_Caustics1);
            SAMPLER(sampler_Caustics1);
            TEXTURE2D(_Caustics2);
            SAMPLER(sampler_Caustics2);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Caustics1_ST;
                float4 _Caustics2_ST;
                half4 _BaseColor;
                float4 _Caustics1Velocity;
                float4 _Caustics2Velocity;
                half _Caustics1MinOpacity;
                half _Caustics1MaxOpacity;
                half _Caustics2MinOpacity;
                half _Caustics2MaxOpacity;
                float _Caustics1StopFrequency;
                float _Caustics2StopFrequency;
            CBUFFER_END

            float ForwardWaveTravel(float time, float cyclesPerSecond, out float normalizedSpeed)
            {
                float angularSpeed = max(cyclesPerSecond, 0.001) * 6.28318530718;
                float phase = time * angularSpeed;
                float stopCurve = 1.0 - cos(phase);
                normalizedSpeed = stopCurve * stopCurve * 0.25;

                // Its derivative is always positive and stays near zero around each stop.
                return time
                    - (4.0 * sin(phase)) / (3.0 * angularSpeed)
                    + sin(2.0 * phase) / (6.0 * angularSpeed);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.baseUV = TRANSFORM_TEX(input.uv, _BaseMap);

                float layer1Speed;
                float layer2Speed;
                float layer1Travel = ForwardWaveTravel(
                    _Time.y,
                    _Caustics1StopFrequency,
                    layer1Speed);
                float layer2Travel = ForwardWaveTravel(
                    _Time.y,
                    _Caustics2StopFrequency,
                    layer2Speed);
                output.caustics1UV = TRANSFORM_TEX(input.uv, _Caustics1)
                    + _Caustics1Velocity.xy * layer1Travel;
                output.caustics2UV = TRANSFORM_TEX(input.uv, _Caustics2)
                    + _Caustics2Velocity.xy * layer2Travel;
                output.causticsOpacity = float2(
                    lerp(_Caustics1MinOpacity, _Caustics1MaxOpacity, layer1Speed),
                    lerp(_Caustics2MinOpacity, _Caustics2MaxOpacity, layer2Speed));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 floor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV)
                    * _BaseColor;
                half4 layer1 = SAMPLE_TEXTURE2D(
                    _Caustics1,
                    sampler_Caustics1,
                    input.caustics1UV);
                half4 layer2 = SAMPLE_TEXTURE2D(
                    _Caustics2,
                    sampler_Caustics2,
                    input.caustics2UV);

                half layer1Blend = layer1.a * input.causticsOpacity.x;
                half layer2Blend = layer2.a * input.causticsOpacity.y;
                floor.rgb *= lerp(1.0h, layer1.r, layer1Blend);
                floor.rgb *= lerp(1.0h, layer2.r, layer2Blend);
                return floor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
