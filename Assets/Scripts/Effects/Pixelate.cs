using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[RequireComponent(typeof (Camera))]
[AddComponentMenu("Image Effects/Pixelate")]
public class Pixelate : MonoBehaviour{
	[SerializeField] private Shader shader;
	private Material _material;
	
	[Range(1,20)]
	public int PixelSizeX = 1;
	private int _pixelSizeX = 1;

	[Range(1,20)]
	public int PixelSizeY = 1;
	private int _pixelSizeY = 1;

	[SerializeField] private bool lockXY = true;

	void OnRenderImage(RenderTexture source, RenderTexture destination) {
		if (_material == null) {
			_material = new Material(shader);
		}

		_material.SetInt("_PixelateX",PixelSizeX);
		_material.SetInt("_PixelateY",PixelSizeY);
		Graphics.Blit(source, destination, _material);
	}

	void OnDisable() {
		DestroyImmediate(_material);
	}

	void Update() {
		if(PixelSizeX != _pixelSizeX) {
			_pixelSizeX = PixelSizeX;
			if (lockXY) {
				_pixelSizeY = PixelSizeY = _pixelSizeX;
			}
		}
		if(PixelSizeY!=_pixelSizeY) {
			_pixelSizeY = PixelSizeY;
			if (lockXY) {
				_pixelSizeX = PixelSizeX = _pixelSizeY;
			}
		}
	}

}