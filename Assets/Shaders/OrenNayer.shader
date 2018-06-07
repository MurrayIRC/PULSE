Shader "Custom/OrenNayer"
{
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_SecondaryTex("Secondary (RGB)", 2D) = "white" {}
		_NormalTex("Normal Map", 2D) = "bump" {}

		_Color("Color", Color) = (1, 1, 1, 1)

		_Roughness("Roughness", Range(0, 1)) = 0.5
	}
		SubShader{
		Tags{ "Queue" = "Geometry" }
		//Cull Off
		CGPROGRAM
		
	#pragma surface surf OrenNayer 
	half _Roughness;

	half4 LightingOrenNayer(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
		s.Normal = normalize(s.Normal);
		half roughnessSqr = _Roughness * _Roughness;
		half3 o_n_fraction = roughnessSqr / (roughnessSqr + half3(0.33, 0.13, 0.09));
		half3 oren_nayar = half3(1, 0, 0) + half3(-0.5, 0.17, 0.45) * o_n_fraction;
		half cos_ndotl = saturate(dot(s.Normal, lightDir));
		half cos_ndotv = saturate(dot(s.Normal, viewDir));
		half oren_nayar_s = saturate(dot(lightDir, viewDir)) - cos_ndotl * cos_ndotv;
		oren_nayar_s /= lerp(max(cos_ndotl, cos_ndotv), 1, step(oren_nayar_s, 0));

		half3 lightingModel = s.Albedo * cos_ndotl * (oren_nayar.x + s.Albedo * oren_nayar.y + oren_nayar.z * oren_nayar_s);
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