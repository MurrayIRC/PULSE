using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Managers : MonoBehaviour {
	private static Managers instance;

	#region Manager References

	[Manager] public static AppManager App { get; private set; }
	[Manager] public static InputManager Input { get; private set; }
	[Manager] public static CameraManager Camera { get; private set; }

	#endregion

	public static bool IsReady { get { return instance != null && instance.isReady; } }
	private bool isReady;
	
	private int numManagers;
	private int numManagersStarted;
	
	private List<IManager> allManagers;
	private List<List<IManager>> initOrder;

	private void Awake() {
		instance = this;
		isReady = false;
		numManagers = 0;
		numManagersStarted = 0;

		LoadProperties();
		StartCoroutine(LoadManagers());
	}

	#region Attribute Property Handling

	private void LoadProperties() {
		PropertyInfo[] props = this.GetType().GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);

		allManagers = new List<IManager>();
		initOrder = new List<List<IManager>>();
		List<IManager> finalStage = new List<IManager>();

		foreach (PropertyInfo p in props) {
			object[] attributes = p.GetCustomAttributes(typeof(ManagerAttribute), true);
			ManagerAttribute attr = attributes.Length > 0 ? attributes[0] as ManagerAttribute : null;
			if (attr != null) {
				numManagers++;
				IManager manager = AcquireComponent(p, attr);
				allManagers.Add(manager);

				p.SetValue(this, manager, null);
				if (attr.InitStage.HasValue) {
					while(initOrder.Count <= attr.InitStage.Value) {
						initOrder.Add(new List<IManager>());
					}
					initOrder[attr.InitStage.Value].Add(manager);
				} else {
					finalStage.Add(manager);
				}
			}
		}

		initOrder.Add(finalStage);

		// if you ever wanna pre init, call it here
	}

	private IManager AcquireComponent(PropertyInfo p, ManagerAttribute attr) {
		IManager manager = null;
		if (!string.IsNullOrEmpty(attr.CustomHookup)) {
			MethodInfo method = GetType().GetMethod(attr.CustomHookup, BindingFlags.Instance | BindingFlags.NonPublic);
			manager = method.Invoke(this, null) as IManager;
		}

		if (manager == null) {
			manager = gameObject.GetComponent(p.PropertyType) as IManager;
		}

		if (manager == null) {
			manager = gameObject.AddComponent(p.PropertyType) as IManager;
		}

		return manager;
	}

	#endregion

	#region Startup

	public static void ForceStartup() {
		instance.StartCoroutine(instance.LoadManagers());
	}

	private IEnumerator LoadManagers () {
		yield return null;

		for(int i = 0; i < initOrder.Count; i++) {
			yield return StartCoroutine(BatchLoadManagers(initOrder[i]));
		}
		
		isReady = true;
	}

	private IEnumerator BatchLoadManagers(List<IManager> stage) {
		yield return null;
		
		foreach(IManager manager in stage) {
			manager.Startup();
		}
		
		yield return null;
		
		List<IManager> managersToBeStarted = new List<IManager>(stage);
		while(managersToBeStarted.Count > 0) {
			for(int i = 0; i < managersToBeStarted.Count; i++) {
				if(managersToBeStarted[i].ManagerState == ManagerState.Started) {
					managersToBeStarted.RemoveAt(i--);
				}
			}

			yield return null;
		}
		
		numManagersStarted += stage.Count;
	}

	#endregion

	#region Shutdown

	public static void ForceShutdown() {
		instance.isReady = false;
		instance.numManagersStarted = 0;
		instance.StartCoroutine (instance.ShutdownManagers());
	}

	private IEnumerator ShutdownManagers() {
		// reverse through startup sequence to shutdown
		for(int i = initOrder.Count - 1; i >= 0; i--) {
			for(int j = initOrder[i].Count - 1; j >= 0; j--) {
				IManager manager = initOrder[i][j] ;
					
				// start shutdown process
				if (manager.ManagerState == ManagerState.Started)
				{
					manager.Shutdown();	
				}
				
				// wait for ready
				while (manager.ManagerState == ManagerState.Started)
				{
					yield return 0;	
				}			
			}
		}
	}

	#endregion

	public void DestroyAllManagers() {
		instance = null;
		
		// clear static references to managers
		PropertyInfo[] props = this.GetType().GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
		foreach (PropertyInfo p in props) {
			object[] attributes = p.GetCustomAttributes(typeof(ManagerAttribute), true);
			if (attributes.Length > 0)
				p.SetValue(this, null, null);
		}
		
		Destroy(this.gameObject);
	}
}
