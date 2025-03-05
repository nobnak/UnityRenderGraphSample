Shader "Unlit/Marker" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            struct appdata {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
            };

            StructuredBuffer<float3> _Positions;
            uint _PositionsCount;

            float4 _Color;
            
            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
            //UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            v2f vert (appdata v) {
                v2f o;
                
                float3 pos_lc = v.vertex;

                pos_lc += _Positions[v.instanceID];
                o.vertex = mul(UNITY_MATRIX_VP, float4(pos_lc, 1));

                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = _Color;
                return col;
            }
            ENDCG
        }
    }
}
