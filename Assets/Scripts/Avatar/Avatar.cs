using System.Collections;
using System.Collections.Generic;
using UnityEngine;
    
public class Avatar : MonoBehaviour {
    public AvatarController Controller { get { return controller; } }
    [SerializeField] private AvatarController controller;

    public AvatarCamera Camera { get { return camera; } }
    [SerializeField] private new AvatarCamera camera;

    private void Awake() {
    }

    private void Start() {
    }

    private void Update() {
    }
}
