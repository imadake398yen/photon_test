using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;

[InitializeOnLoad]
public class UnityEditorEventHandler {
 
	/**
		run with Launch / Play
	*/
	static UnityEditorEventHandler () {
		// initialize play flag. if already playing, true.
		SublimeSocketClient.UnityEditorIsPlaying = EditorApplication.isPlaying;

		// set handlert to update play flag.
		EditorApplication.playmodeStateChanged += CheckPlay;
		SublimeSocketClient.Automate();
	}

	static void CheckPlay () {
		// if changing to playmode, already true.
		SublimeSocketClient.UnityEditorIsPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
	}
}
