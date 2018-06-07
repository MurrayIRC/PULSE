using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Image Effects/PixelBoy")]
public class LowRezzer : MonoBehaviour
{
    [SerializeField] private int PixelSize = 160;

	private RenderTexture buffer;
    private int height;

    private void Awake() {
        if (!SystemInfo.supportsImageEffects)
        {
            this.enabled = false;
            return;
        }
    }

    void Update() {
        float ratio = ((float)Camera.main.pixelHeight / (float)Camera.main.pixelWidth);
        height = Mathf.RoundToInt(PixelSize * ratio);
    }
	
    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        source.filterMode = FilterMode.Point;
        buffer = RenderTexture.GetTemporary(PixelSize, height, -1);
        buffer.filterMode = FilterMode.Point;
        Graphics.Blit(source, buffer);
        Graphics.Blit(buffer, destination);
        RenderTexture.ReleaseTemporary(buffer);
    }
}