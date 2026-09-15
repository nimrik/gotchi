// Flat-colour toon look for the 3D cat (built-in render pipeline): an inverted-hull outline pass plus a
// two-band lit pass with a fixed light direction, so the character looks the same in every scene.
Shader "Gotchi/CatToon"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _ShadeTint ("Shade Tint", Color) = (0.70, 0.62, 0.74, 1)
        _ShadeStrength ("Shade Strength", Range(0, 1)) = 0.25
        _LightDir ("Light Direction", Vector) = (-0.35, 0.75, -0.55, 0)
        _OutlineColor ("Outline", Color) = (0.15, 0.10, 0.10, 1)
        _OutlineWidth ("Outline Width", Float) = 0.045
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _OutlineWidth;
            fixed4 _OutlineColor;
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert(appdata_base v)
            {
                v2f o;
                float3 p = v.vertex.xyz + normalize(v.normal) * _OutlineWidth;
                o.pos = UnityObjectToClipPos(float4(p, 1));
                return o;
            }
            fixed4 frag(v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }

        Pass
        {
            Name "TOON"
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
