Shader "Hidden/MultiplePassShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			sampler2D _PrevResult;
			float _CurrentSample;

            fixed4 frag (v2f i) : SV_Target
            {
				float4 curResult = float4(1.0,1.0,1.0,1.0);//tex2D(_MainTex, i.uv);
				float4 prevResult = tex2D(_PrevResult, i.uv);

				float4 result = (prevResult * (_CurrentSample) + curResult) / (_CurrentSample + 1.0);


				return prevResult;
            }
            ENDCG
        }
    }
}
