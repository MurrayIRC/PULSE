using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : Manager {
	[SerializeField] private AvatarCamera avatarCamera;

	public override void Startup()
    {
        managerState = ManagerState.Started;
    }

    public override void Shutdown()
    {
		managerState = ManagerState.Shutdown;
    }

    public Camera GetCurrentCamera() {
        return avatarCamera.Camera;
    }
}
