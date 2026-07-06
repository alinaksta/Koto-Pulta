Shader "Custom/RadialFillSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _FillAmount ("Fill Amount", Range(0, 1)) = 1
        _StartAngle ("Start Angle", Range(0, 360)) = 90
        [Toggle] _Clockwise ("Clockwise", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float2 localPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _FillAmount;
            float _StartAngle;
            float _Clockwise;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.localPos = v.vertex.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv) * _Color * i.color;

                float angle = atan2(i.localPos.y, i.localPos.x);
                float startAngle = radians(_StartAngle);
                float normalizedAngle = frac((angle - startAngle) / (2.0 * UNITY_PI) + 1.0);

                if (_Clockwise > 0.5)
                {
                    normalizedAngle = 1.0 - normalizedAngle;
                }

                clip(_FillAmount - normalizedAngle);

                return color;
            }
            ENDCG
        }
    }
}
