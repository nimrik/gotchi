// Soft contact shadow under the 3D cat: a filled circle whose alpha fades out radially (by UV distance from
// the centre). Tinted with the mood colour by Cat3DView.
Shader "Gotchi/CatShadow"
{
    Properties
    {
        _Color ("Color", Color) = (0.3, 0.25, 0.3, 0.45)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float r = length(i.uv - 0.5) * 2.0;
                float a = smoothstep(1.0, 0.35, r);
                return fixed4(_Color.rgb, _Color.a * a);
            }
            ENDCG
        }
    }
}
