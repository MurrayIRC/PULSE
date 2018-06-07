Shader "Custom/WrapLambert"
{
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_SecondaryTex("Secondary (RGB)", 2D) = "white" {}
		_NormalTex("Normal Map", 2D) = "bump" {}

		_Color("Color", Color) = (1, 1, 1, 1)

		_Hardness("Hardness", Range(.25, 1)) = 0.5

	}
		SubShader{
		Tags{ "Queue" = "Geometry" }
		//Cull Off
		CGPROGRAM
		
	#pragma surface surf WrapLambert 
	half _Hardness;

	half4 LightingWrapLambert(SurfaceOutput s, half3 lightDir, half atten) {
		s.Normal = normalize(s.Normal);

		half distAtten;
		if (_WorldSpaceLightPos0.w == 0.0)
			distAtten = 1.0;
		else
			distAtten = saturate(1.0 / length(lightDir));

		half diff = (max(0, dot(s.Normal, lightDir)) * atten + 1 - _Hardness) * _Hardness; ;

		half4 c;
		c.rgb = (s.Albedo * diff * _LightColor0) * distAtten;
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