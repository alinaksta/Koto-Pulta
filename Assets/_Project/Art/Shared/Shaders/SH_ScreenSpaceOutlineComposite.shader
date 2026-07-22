Shader "Hidden/Game/ScreenSpaceOutlineComposite"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Float) = 2
        _FringeAlphaMin ("Fringe Alpha Min", Range(0, 1)) = 0.05
        _FringeAlphaMax ("Fringe Alpha Max", Range(0, 1)) = 0.65
        _FringeBlendStrength ("Fringe Blend Strength", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_ScreenSpaceOutlineMask);
            half4 _OutlineColor;
            float _OutlineWidth;
            float _FringeAlphaMin;
            float _FringeAlphaMax;
            float _FringeBlendStrength;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float center = SAMPLE_TEXTURE2D_X(_ScreenSpaceOutlineMask, sampler_PointClamp, uv).r;
                float2 offset = _OutlineWidth / _ScreenParams.xy;

                float expanded = 0.0;
                expanded = max(expanded, SAMPLE_TEXTURE2D_X(_ScreenSpaceOutlineMask, sampler_PointClamp, uv + float2(offset.x, 0.0)).r);
                expanded = max(expanded, SAMPLE_TEXTURE2D_X(_ScreenSpaceOutlineMask, sampler_PointClamp, uv + float2(-offset.x, 0.0)).r);
                expanded = max(expanded, SAMPLE_TEXTURE2D_X(_ScreenSpaceOutlineMask, sampler_PointClamp, uv + float2(0.0, offset.y)).r);
                expanded = max(expanded, SAMPLE_TEXTURE2D_X(_ScreenSpaceOutlineMask, sampler_PointClamp, uv + float2(0.0, -offset.y)).r);
                expanded = max(expanded, SAMPLE_TEXTURE2D_X(_ScreenSpaceOutlineMask, sampler_PointClamp, uv + offset).r);
                expanded = max(expanded, SAMPLE_TEXTURE2D_X(_ScreenSpaceOutlineMask, sampler_PointClamp, uv - offset).r);
                expanded = max(expanded, SAMPLE_TEXTURE2D_X(_ScreenSpaceOutlineMask, sampler_PointClamp, uv + float2(offset.x, -offset.y)).r);
                expanded = max(expanded, SAMPLE_TEXTURE2D_X(_ScreenSpaceOutlineMask, sampler_PointClamp, uv + float2(-offset.x, offset.y)).r);

                float outsideEdge = saturate(expanded - center);
                float fringeAlphaMax = max(_FringeAlphaMax, _FringeAlphaMin + 0.0001);
                float fringe = expanded * (1.0 - smoothstep(_FringeAlphaMin, fringeAlphaMax, center));
                float outlineStrength = max(outsideEdge, fringe * _FringeBlendStrength) * _OutlineColor.a;
                sceneColor.rgb = lerp(sceneColor.rgb, _OutlineColor.rgb, saturate(outlineStrength));
                return sceneColor;
            }
            ENDHLSL
        }
    }
}
