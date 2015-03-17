using UnityEngine;
using UnityEditor;

using System.Collections;

/**
	interface to control SublimeSocket
*/
public class SublimeSocketInterface : MonoBehaviour {

	// connect
	[MenuItem ("Window/SublimeSocket/connect", false, 1)]
	static void Connect () {
		SublimeSocketClient.StartConnect();
	}
	[MenuItem ("Window/SublimeSocket/connect", true)]
	static bool IsConnectable () {
		if (SublimeSocketClient.ws != null) return SublimeSocketClient.ws.ReadyState != USSAWebSocketSharp.WsState.OPEN;
		return true;
	}

	// disconnect
	[MenuItem ("Window/SublimeSocket/disconnect", false, 2)]
	static void Close () {
		SublimeSocketClient.CloseConnection();
	}
	[MenuItem ("Window/SublimeSocket/disconnect", true)]
	static bool IsClosable () {
		if (SublimeSocketClient.ws != null) return SublimeSocketClient.ws.ReadyState == USSAWebSocketSharp.WsState.OPEN;
		return false;
	}


	// reload
	[MenuItem ("Window/SublimeSocket/reload", false, 13)]
	static void Reload () {
		SublimeSocketClient.CloseConnection();
		SublimeSocketClient.StartConnect();
	}
	[MenuItem ("Window/SublimeSocket/reload", true)]
	static bool IsReloadable () {
		if (SublimeSocketClient.ws != null) return SublimeSocketClient.ws.ReadyState == USSAWebSocketSharp.WsState.OPEN;
		return false;
	}


	// autoConnect
	[MenuItem ("Window/SublimeSocket/autoConnect-on", false, 24)]
	static void TurnAutoOn () {
		ReferenceManager.UpdateAutoConnect(PreferenceSetings.PREFERENCE_PARAM_AUTO_ON);
	}
	[MenuItem ("Window/SublimeSocket/autoConnect-on", true)]
	static bool IsTurnableAutoToOn () {
		var paramDict = ReferenceManager.PreferenceDict(Application.dataPath + SublimeSocketClient.PREFERENCE_FILE_PATH);
		return paramDict[PreferenceSetings.PREFERENCE_ITEM_AUTOCONNECT] != PreferenceSetings.PREFERENCE_PARAM_AUTO_ON;
	}

	[MenuItem ("Window/SublimeSocket/autoConnect-off", false, 25)]
	static void TurnAutoOff () {
		ReferenceManager.UpdateAutoConnect(PreferenceSetings.PREFERENCE_PARAM_AUTO_OFF);
	}
	[MenuItem ("Window/SublimeSocket/autoConnect-off", true)]
	static bool IsTurnableAutoToOff () {
		var paramDict = ReferenceManager.PreferenceDict(Application.dataPath + SublimeSocketClient.PREFERENCE_FILE_PATH);
		return paramDict[PreferenceSetings.PREFERENCE_ITEM_AUTOCONNECT] == PreferenceSetings.PREFERENCE_PARAM_AUTO_ON;
	}


	// completion
	[MenuItem ("Window/SublimeSocket/completion-on", false, 36)]
	static void TurnCompletionOn () {
		ReferenceManager.UpdateCompletion(PreferenceSetings.PREFERENCE_PARAM_COMPLETION_ON);
	}
	[MenuItem ("Window/SublimeSocket/completion-on", true)]
	static bool IsTurnableCompletionToOn () {
		var paramDict = ReferenceManager.PreferenceDict(Application.dataPath + SublimeSocketClient.PREFERENCE_FILE_PATH);
		return paramDict[PreferenceSetings.PREFERENCE_ITEM_COMPLETION] != PreferenceSetings.PREFERENCE_PARAM_AUTO_ON;
	}

	[MenuItem ("Window/SublimeSocket/completion-off", false, 37)]
	static void TurnCompletionOff () {
		ReferenceManager.UpdateCompletion(PreferenceSetings.PREFERENCE_PARAM_COMPLETION_OFF);
	}
	[MenuItem ("Window/SublimeSocket/completion-off", true)]
	static bool IsTurnableCompletionToOff () {
		var paramDict = ReferenceManager.PreferenceDict(Application.dataPath + SublimeSocketClient.PREFERENCE_FILE_PATH);
		return paramDict[PreferenceSetings.PREFERENCE_ITEM_COMPLETION] == PreferenceSetings.PREFERENCE_PARAM_AUTO_ON;
	}


	// compile by save
	[MenuItem ("Window/SublimeSocket/compileBySave-on", false, 48)]
	static void TurnCompileBySaveOn () {
		ReferenceManager.UpdateCompileBySave(PreferenceSetings.PREFERENCE_PARAM_COMPILE_BY_SAVE_ON);
	}
	[MenuItem ("Window/SublimeSocket/compileBySave-on", true)]
	static bool IsTurnableCompileBySaveToOn () {
		var paramDict = ReferenceManager.PreferenceDict(Application.dataPath + SublimeSocketClient.PREFERENCE_FILE_PATH);
		return paramDict[PreferenceSetings.PREFERENCE_ITEM_COMPILE_BY_SAVE] == PreferenceSetings.PREFERENCE_PARAM_COMPILE_BY_SAVE_OFF;
	}

	[MenuItem ("Window/SublimeSocket/compileBySave-off", false, 49)]
	static void TurnCompileBySaveOff () {
		ReferenceManager.UpdateCompileBySave(PreferenceSetings.PREFERENCE_PARAM_COMPILE_BY_SAVE_OFF);
	}
	[MenuItem ("Window/SublimeSocket/compileBySave-off", true)]
	static bool IsTurnableCompileBySaveToOff () {
		var paramDict = ReferenceManager.PreferenceDict(Application.dataPath + SublimeSocketClient.PREFERENCE_FILE_PATH);
		return paramDict[PreferenceSetings.PREFERENCE_ITEM_COMPILE_BY_SAVE] == PreferenceSetings.PREFERENCE_PARAM_COMPILE_BY_SAVE_ON;
	}



	// compile anyway
	[MenuItem ("Window/SublimeSocket/compileAnyway-on", false, 60)]
	static void TurnCompileAnywayOn () {
		ReferenceManager.UpdateCompileAnyway(PreferenceSetings.PREFERENCE_PARAM_COMPILE_ANYWAY_ON);
	}
	[MenuItem ("Window/SublimeSocket/compileAnyway-on", true)]
	static bool IsTurnableCompleAnywayToOn () {
		var paramDict = ReferenceManager.PreferenceDict(Application.dataPath + SublimeSocketClient.PREFERENCE_FILE_PATH);
		return paramDict[PreferenceSetings.PREFERENCE_ITEM_COMPILE_ANYWAY] == PreferenceSetings.PREFERENCE_PARAM_COMPILE_ANYWAY_OFF;
	}

	[MenuItem ("Window/SublimeSocket/compileAnyway-off", false, 61)]
	static void TurnCompileAnywayOff () {
		ReferenceManager.UpdateCompileAnyway(PreferenceSetings.PREFERENCE_PARAM_COMPILE_ANYWAY_OFF);
	}
	[MenuItem ("Window/SublimeSocket/compileAnyway-off", true)]
	static bool IsTurnableCompleAnywayToOff () {
		var paramDict = ReferenceManager.PreferenceDict(Application.dataPath + SublimeSocketClient.PREFERENCE_FILE_PATH);
		return paramDict[PreferenceSetings.PREFERENCE_ITEM_COMPILE_ANYWAY] == PreferenceSetings.PREFERENCE_PARAM_COMPILE_ANYWAY_ON;
	}


	// open preferences
	[MenuItem ("Window/SublimeSocket/open preferences", false, 72)]
	static void OpenPref () {
		var path = "";
		switch (Application.platform) {
			case RuntimePlatform.OSXEditor:{
				path = Application.dataPath+"/SublimeSocketAsset/Preferences.txt";
				break;
			}
			case RuntimePlatform.WindowsEditor:{
				path = Application.dataPath+"\\SublimeSocketAsset\\Preferences.txt";
				break;
			}
		}
    	UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(@path, 1);
	}
}
