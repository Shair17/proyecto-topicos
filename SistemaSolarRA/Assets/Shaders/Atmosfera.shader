// Halo de atmósfera: se dibujan las caras traseras de una esfera algo más grande que el planeta,
// con un brillo que es máximo junto al planeta y se desvanece hacia afuera. Se suma a lo de detrás.
Shader "SistemaSolar/Atmosfera"
{
    Properties
    {
        _Color ("Color", Color) = (0.3, 0.6, 1, 1)
        _Potencia ("Potencia del borde", Range(0.5, 8)) = 2
        _Intensidad ("Intensidad", Range(0, 4)) = 1.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Potencia;
            float _Intensidad;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 vista : TEXCOORD1;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.vista = WorldSpaceViewDir(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float halo = saturate(dot(-normalize(i.normal), normalize(i.vista)));
                return _Color * pow(halo, _Potencia) * _Intensidad;
            }
            ENDCG
        }
    }
}
