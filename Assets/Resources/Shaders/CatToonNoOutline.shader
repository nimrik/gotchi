// Same two-band toon pass as Gotchi/CatToon but without the outline hull — for pupils, eye glints, whiskers,
// blush and other small features that would be swallowed by an outline.
Shader "Gotchi/CatToonNoOutline"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _ShadeTint ("Shade Tint", Color) = (0.70, 0.62, 0.74, 1)
        _ShadeStrength ("Shade Strength", Range(0, 1)) = 0.0
        _LightDir ("Light Direction", Vector) = (-0.35, 0.75, -0.55, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+1" }
        Pass
        {
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color, _ShadeTint; float _ShadeStrength; float4 _LightDir;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 n : TEXCOORD1; };
            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.n = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
                float ndl = dot(normalize(i.n), normalize(_LightDir.xyz));
                float lit = smoothstep(-0.08, 0.10, ndl);
                fixed3 shaded = albedo.rgb * lerp(fixed3(1, 1, 1), _ShadeTint.rgb, _ShadeStrength);
                return fixed4(lerp(shaded, albedo.rgb, lit), 1);
            }
            ENDCG
        }
    }
}
