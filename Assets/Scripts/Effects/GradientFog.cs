using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Gradient Fog")]
public class GradientFog : MonoBehaviour {
	
	public enum GradientFogMode
	{
		Blend,
		Additive,
		Multiply,
		Screen,
		Overlay,
		Dodge
	}

	public enum GradientSourceType
	{
		Textures,
		Gradients,
	}
	
	public GradientFogMode fogMode;
	public bool ExcludeSkybox = false;

	[Header("Blend")]
	[Tooltip("Use a second ramp for transition")]
	[SerializeField]
	private bool useBlend;
	[Tooltip("Amount of blend between 2 gradients")]
	[Range(0f,1f)]
	public float blend = 0.0f;

	[Header("Gradients")]
	[Tooltip("Use ramp from textures or gradient fields")]
	public GradientSourceType gradientSource;

	public Gradient rampGradient;
	public Gradient rampBlendGradient;

	public Texture2D rampTexture;
	public Texture2D rampBlendTexture;

	[Header("Noise Texture")]
	[SerializeField]
	private bool useNoise;
	public Texture2D noiseTexture;
	[Space(5f)]
	[Tooltip("XY: Speed1 XY | WH: Speed2 XY")]
	public Vector4 noiseSpeed;
	[Space(5f)]
	[Tooltip("XY: Tiling1 XY | WH: Tiling2 XY")]
	public Vector4 noiseTiling = new Vector4(1,1,1,1);

	private Camera cam;
	private Texture2D mainRamp;
	private Texture2D blendRamp;
	private Shader fogShader;
	private Material fogMat;

	void Start () {
		CreateResources();
		UpdateTextures();
		SetKeywords();
	}

	void OnEnable(){
		CreateResources();
		UpdateTextures();
		SetKeywords();
	}

	void OnDisable(){
		ClearResources();
	}
	
	/// <summary>
	/// Updates the gradient for realTime editing.
	/// </summary>
	public void UpdateTextures()
	{
		SetGradient();
		SetKeywords();
		UpdateValues();
	}

	private void UpdateValues()
	{
		if(fogMat == null || fogShader == null)
		{
			CreateResources();
		}

		if(mainRamp != null)
		{
			fogMat.SetTexture("_SF_MainRamp",mainRamp);
		}

		if(useBlend && blendRamp != null)
		{
			fogMat.SetTexture("_SF_BlendRamp",blendRamp);
			fogMat.SetFloat("_SF_Blend",blend);
		}

		if(useNoise && noiseTexture != null)
		{
			fogMat.SetTexture("_SF_NoiseTex",noiseTexture);
			fogMat.SetVector("_SF_NoiseSpeed",noiseSpeed);
			fogMat.SetVector("_SF_NoiseTiling",noiseTiling);
		}
	}

	/// <summary>
	/// Sets the gradients from the inspector or from 2D textures.
	/// </summary>
	private void SetGradient() {
		if(gradientSource == GradientSourceType.Textures) {
			mainRamp = rampTexture;
			if(useBlend) {
				blendRamp = rampBlendTexture;
			}
		}
		else if(gradientSource == GradientSourceType.Gradients) {
			if(mainRamp != null){DestroyImmediate(mainRamp);}
			mainRamp = GenerateTextureFromGradient(rampGradient,256,8);
			if(useBlend) {
				if(blendRamp != null){DestroyImmediate(blendRamp);}
				blendRamp = GenerateTextureFromGradient(rampBlendGradient,256,8);
			}
		}
	}

	/// <summary>
	/// Generates a gradient and assign it to a temporary texture2D.
	/// </summary>
	/// <returns>The gradient.</returns>
	/// <param name="gradient">Gradient ramp</param>
	/// <param name="gWidth">ramp width.</param>
	/// <param name="gHeight">ramp height.</param>
	private Texture2D GenerateTextureFromGradient(Gradient gradient, int gWidth, int gHeight) {
		Texture2D gradientToTex = new Texture2D(gWidth, gHeight, TextureFormat.ARGB32, false);
		gradientToTex.wrapMode = TextureWrapMode.Clamp;
		gradientToTex.hideFlags = HideFlags.HideAndDontSave;
		Color current = Color.white;

		if(gradient != null) {
			for(int w = 0; w < gWidth; w++) {
				current = gradient.Evaluate (w / (float)gWidth);

				for(int h = 0; h < gHeight; h++) {
					gradientToTex.SetPixel (w, h, current);
				}
			}
		}

		gradientToTex.Apply();

		return gradientToTex;
	}

	/// <summary>
	/// Creates the Shader, Material and ramps for the first time.
	/// </summary>
	private void CreateResources() {
		//Set Shader
		if(fogShader == null) {
			fogShader = Shader.Find("Hidden/GradientFog");
		}
		//Set Material
		if(fogMat == null && fogShader != null) {
			fogMat = new Material(fogShader);
			fogMat.hideFlags = HideFlags.HideAndDontSave;
		}
		//Set Gradients
		if(mainRamp == null || blendRamp == null) {
			SetGradient();
		}
		//Set Camera and depth texture
		if(cam == null)
		{
			cam = GetComponent<Camera>();
			cam.depthTextureMode |= DepthTextureMode.Depth;
		}
	}

	private void ClearResources()
	{
		if(fogMat != null)
		{
			DestroyImmediate(fogMat);
		}

		DisableKeywords();
		cam.depthTextureMode = DepthTextureMode.None;
	}

	public void SetKeywords(){

		//Mode
		switch(fogMode)
		{
			case GradientFogMode.Blend:
				Shader.EnableKeyword("_FOG_BLEND");
				Shader.DisableKeyword("_FOG_ADDITIVE");
				Shader.DisableKeyword("_FOG_MULTIPLY");
				Shader.DisableKeyword("_FOG_SCREEN");
				Shader.DisableKeyword("_FOG_OVERLAY");
				Shader.DisableKeyword("_FOG_DODGE");
				break;
			case GradientFogMode.Additive:
				Shader.DisableKeyword("_FOG_BLEND");
				Shader.EnableKeyword("_FOG_ADDITIVE");
				Shader.DisableKeyword("_FOG_MULTIPLY");
				Shader.DisableKeyword("_FOG_SCREEN");
				Shader.DisableKeyword("_FOG_OVERLAY");
				Shader.DisableKeyword("_FOG_DODGE");
				break;
			case GradientFogMode.Multiply:
				Shader.DisableKeyword("_FOG_BLEND");
				Shader.DisableKeyword("_FOG_ADDITIVE");
				Shader.EnableKeyword("_FOG_MULTIPLY");
				Shader.DisableKeyword("_FOG_SCREEN");
				Shader.DisableKeyword("_FOG_OVERLAY");
				Shader.DisableKeyword("_FOG_DODGE");
				break;
			case GradientFogMode.Screen:
				Shader.DisableKeyword("_FOG_BLEND");
				Shader.DisableKeyword("_FOG_ADDITIVE");
				Shader.DisableKeyword("_FOG_MULTIPLY");
				Shader.EnableKeyword("_FOG_SCREEN");
				Shader.DisableKeyword("_FOG_OVERLAY");
				Shader.DisableKeyword("_FOG_DODGE");
				break;
			case GradientFogMode.Overlay:
				Shader.DisableKeyword("_FOG_BLEND");
				Shader.DisableKeyword("_FOG_ADDITIVE");
				Shader.DisableKeyword("_FOG_MULTIPLY");
				Shader.DisableKeyword("_FOG_SCREEN");
				Shader.EnableKeyword("_FOG_OVERLAY");
				Shader.DisableKeyword("_FOG_DODGE");
				break;
			case GradientFogMode.Dodge:
				Shader.DisableKeyword("_FOG_BLEND");
				Shader.DisableKeyword("_FOG_ADDITIVE");
				Shader.DisableKeyword("_FOG_MULTIPLY");
				Shader.DisableKeyword("_FOG_SCREEN");
				Shader.DisableKeyword("_FOG_OVERLAY");
				Shader.EnableKeyword("_FOG_DODGE");
				break;
		}

		//Blend
		if(useBlend)
		{
			Shader.EnableKeyword("_FOG_BLEND_ON");
			Shader.DisableKeyword("_FOG_BLEND_OFF");
		}else{
			Shader.EnableKeyword("_FOG_BLEND_OFF");
			Shader.DisableKeyword("_FOG_BLEND_ON");
		}

		//Noise
		if(useNoise)
		{
			Shader.EnableKeyword("_FOG_NOISE_ON");
			Shader.DisableKeyword("_FOG_NOISE_OFF");
		}else{
			Shader.EnableKeyword("_FOG_NOISE_OFF");
			Shader.DisableKeyword("_FOG_NOISE_ON");
		}
		
		if(ExcludeSkybox){
			Shader.EnableKeyword("_SKYBOX");
		}else{
			Shader.DisableKeyword("_SKYBOX");
		}
	}

	private void DisableKeywords()
	{
		Shader.DisableKeyword("_FOG_BLEND");
		Shader.DisableKeyword("_FOG_ADDITIVE");
		Shader.DisableKeyword("_FOG_MULTIPLY");
		Shader.DisableKeyword("_FOG_SCREEN");
		Shader.DisableKeyword("_FOG_BLEND_OFF");
		Shader.DisableKeyword("_FOG_BLEND_ON");
		Shader.DisableKeyword("_FOG_NOISE_OFF");
		Shader.DisableKeyword("_FOG_NOISE_ON");
	}

	private bool IsSupported()
	{
		if (!SystemInfo.supportsImageEffects)
		{
			return false;
		}

		if(!fogShader.isSupported || fogShader == null){
			return false;
		}
		return true;
	}

	[ImageEffectOpaque]
	void OnRenderImage(RenderTexture source,RenderTexture destination)
	{
		if(!IsSupported())
		{
			Graphics.Blit(source,destination);
			return;
		}

		UpdateValues();
		Graphics.Blit(source,destination,fogMat);
	}

}