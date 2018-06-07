Shader "Custom/Banded"
{
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_SecondaryTex("Secondary (RGB)", 2D) = "white" {}
		_NormalTex("Normal Map", 2D) = "bump" {}

		_Color("Color", Color) = (1, 1, 1, 1)

		_LightSteps("Light Steps", Float) = 64
	}
		SubShader{
		Tags{ "Queue" = "Geometry" }
		//Cull Off
		CGPROGRAM
		
	#pragma surface surf Banded 

	half _LightSteps;

	half4 LightingBanded(SurfaceOutput s, half3 lightDir, half atten) {
		s.Normal = normalize(s.Normal);
		half NdotL = max(0.0, dot(s.Normal, lightDir));
		half lightBandsMultiplier = _LightSteps / 256;
		half lightBandsAdditive = _LightSteps / 2;
		fixed bandedNdotL = (floor((NdotL * 256 + lightBandsAdditive) / _LightSteps)) * lightBandsMultiplier;

		half3 lightingModel = bandedNdotL * s.Albedo;
		half3 attenColor = atten * _LightColor0.rgb;
		return half4(lightingModel * attenColor, s.Alpha);
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