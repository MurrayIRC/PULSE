using System.Collections;
using System.Collections.Generic;
using UnityEngine;
    
public class Avatar : MonoBehaviour {
    public AvatarMover Movement { get { return movement; } }
    [SerializeField] private AvatarMover movement;

    public AvatarCamera Camera { get { return camera; } }
    [SerializeField] private new AvatarCamera camera;

    private void Awake() {
    }

    private void Start() {
    }

    private void Update() {
    }
}
