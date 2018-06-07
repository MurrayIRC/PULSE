using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppManager : Manager {
    public override void Startup()
    {
        QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = 60;
        Screen.SetResolution(400, 225, FullScreenMode.ExclusiveFullScreen);

		managerState = ManagerState.Started;
    }

	public override void Shutdown()
    {
        managerState = ManagerState.Shutdown;
    }
}
