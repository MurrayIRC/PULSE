using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Manager : MonoBehaviour, IManager {
    protected ManagerState managerState = ManagerState.Uninitialized;
	public ManagerState ManagerState { get { return managerState; } }

	public abstract void Startup();
	public abstract void Shutdown();
}

public enum ManagerState {
	Uninitialized,
	Started,
	Shutdown
}

public interface IManager {
	ManagerState ManagerState { get; }
	void Startup();
	void Shutdown();
}

[AttributeUsage(AttributeTargets.Property)]
public class ManagerAttribute : Attribute {
	public int? InitStage = null;
	public int InitStageArg { 
		get { throw new NotImplementedException(); }
		set { InitStage = value; } 
	}

	public string CustomHookup = null;
}