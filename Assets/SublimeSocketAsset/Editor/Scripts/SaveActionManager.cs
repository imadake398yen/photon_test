using UnityEngine;
using UnityEditor;

using System;

using USSAUniRx;

public class SaveActionManager {
	public void StartManaging () {
		Observable.EveryUpdate().Subscribe(_ => SaveAct());
	}

	private bool save = false;

	public void SaveAct () {
		if (save) {
			EditorApplication.ExecuteMenuItem("Assets/Refresh");
			save = false;
		}
	}

	public void Save () {
		save = true;
	}
}