Shader "Custom/JourneyLambert"
{
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_SecondaryTex("Secondary (RGB)", 2D) = "white" {}
		_NormalTex("Normal Map", 2D) = "bump" {}

		_Color("Color", Color) = (1, 1, 1, 1)
	}
		SubShader{
		Tags{ "Queue" = "Geometry" }
		//Cull Off
		CGPROGRAM
		
	#pragma surface surf JourneyLambert
	half _Hardness;

	half4 LightingJourneyLambert(SurfaceOutput s, half3 lightDir, half atten) {
		s.Normal = normalize(s.Normal);
		s.Normal.y *= 0.3;
		half NdotL = saturate(4.0 * max(0.0, dot(s.Normal, lightDir)));
		half4 c;
		c.rgb = s.Albedo * NdotL * atten * _LightColor0;
		c.a = s.Alpha;
		return c;
	}

	struct Input {
		float2 uv_MainTex;
		float2 uv_SecondaryTex;
		float2 uv_NormalTex;
	};

	sampler2D _MainTex;
	sampler2D _SecondaryTex;
	sampler2D _NormalTex;

	half4 _Color;

	void surf(Input IN, inout SurfaceOutput o) {
		o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * tex2D(_SecondaryTex, IN.uv_SecondaryTex).rgb * _Color;
		o.Normal = UnpackNormal(tex2D(_NormalTex, IN.uv_NormalTex));
	}

	ENDCG
	}
	FallBack "Standard"
}