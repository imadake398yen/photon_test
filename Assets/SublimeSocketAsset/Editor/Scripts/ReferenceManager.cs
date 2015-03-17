using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

public class ReferenceManager {
	public static string assetBasePath;

	/**
		return enviroment-modified path.
	*/
	public static string TargetFilePath () {
		var path = string.Empty;

		switch (Application.platform) {
			case RuntimePlatform.OSXEditor:{
				path = Path.Combine("/Users", System.Environment.UserName+SocketDefinitions.MAC_LOGFILE_PATH);
				break;
			}
			case RuntimePlatform.WindowsEditor:{
				var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				DirectoryInfo d = new DirectoryInfo(basePath);
				var result = d.Parent.FullName;
				path = result+SocketDefinitions.WINDOWS_LOGFILE_PATH;
				break;
			}
		}
		
		return path;
	}

	/**
		get paramDict from path
	*/
	public static Dictionary<string, string> PreferenceDict(string preferencePath) {
		if (!System.IO.File.Exists(preferencePath)) {
			// generate
			ResetPreference(preferencePath);
		}
		
		// read file
		StreamReader reader = new StreamReader(preferencePath);
		string jsonText = reader.ReadToEnd();
		reader.Close();
		Dictionary<string, string> paramDict = null;
		try {
			paramDict = USSAJson.USSAJsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText);
		} catch (Exception e) {
			Debug.LogWarning("SSA:JSON deserialize error " + e);
		}
		return paramDict;
	}

	/**
		re-generate preference file @ filePath with default parameters.
	*/
	public static void ResetPreference (string filePath) {
		var defaultDict = new Dictionary<string, string> () {
			{PreferenceSetings.PREFERENCE_ITEM_VERSION, PreferenceSetings.PREFERENCE_PARAM_VERSION},
			{PreferenceSetings.PREFERENCE_ITEM_SERVER, PreferenceSetings.PREFERENCE_PARAM_DEFAULT_PROTOCOL + PreferenceSetings.PREFERENCE_PARAM_DEFAULT_HOST + ":" + PreferenceSetings.PREFERENCE_PARAM_DEFAULT_WSSERVER_PORT},
			{PreferenceSetings.PREFERENCE_ITEM_TARGET, TargetFilePath()},
			{PreferenceSetings.PREFERENCE_ITEM_STATUS, PreferenceSetings.PREFERENCE_PARAM_STATUS_NOTCONNECTED},
			{PreferenceSetings.PREFERENCE_ITEM_ERROR, PreferenceSetings.PREFERENCE_PARAM_ERROR_NONE},
			{PreferenceSetings.PREFERENCE_ITEM_AUTOCONNECT, PreferenceSetings.PREFERENCE_PARAM_AUTO_ON},
			{PreferenceSetings.PREFERENCE_ITEM_COMPLETION, PreferenceSetings.PREFERENCE_PARAM_COMPLETION_ON},
			{PreferenceSetings.PREFERENCE_ITEM_PLAYFLAG, PreferenceSetings.PREFERENCE_PARAM_PLAY_OFF},
			{PreferenceSetings.PREFERENCE_ITEM_BREAKFLAG, PreferenceSetings.PREFERENCE_PARAM_BREAK_OFF},
			{PreferenceSetings.PREFERENCE_ITEM_COMPILE_BY_SAVE, PreferenceSetings.PREFERENCE_PARAM_COMPILE_BY_SAVE_ON},
			{PreferenceSetings.PREFERENCE_ITEM_COMPILE_ANYWAY, PreferenceSetings.PREFERENCE_PARAM_COMPILE_ANYWAY_ON}
		};

		var defaultJSON = USSAJson.USSAJsonConvert.SerializeObject(defaultDict);
		using (StreamWriter sw = new StreamWriter(filePath)) {
			sw.WriteLine(defaultJSON);
		}
	}

	/**
		update connection status.
	*/
	public static void UpdateConnectionStatus (string nextStatus) {
		var path = assetBasePath + SublimeSocketClient.PREFERENCE_FILE_PATH;

		var paramDict = PreferenceDict(path);
		paramDict[PreferenceSetings.PREFERENCE_ITEM_STATUS] = nextStatus;

		WriteOutPreferences(paramDict, path);
	}

	/**
		update connection error.
	*/
	public static void UpdateConnectionError (string nextError) {
		var path = assetBasePath + SublimeSocketClient.PREFERENCE_FILE_PATH;

		var paramDict = PreferenceDict(path);
		paramDict[PreferenceSetings.PREFERENCE_ITEM_ERROR] = nextError;

		WriteOutPreferences(paramDict, path);
	}

	/**
		update autoConnect.
	*/
	public static void UpdateAutoConnect (string nextFlag) {
		var path = assetBasePath + SublimeSocketClient.PREFERENCE_FILE_PATH;

		var paramDict = PreferenceDict(path);
		paramDict[PreferenceSetings.PREFERENCE_ITEM_AUTOCONNECT] = nextFlag;

		WriteOutPreferences(paramDict, path);
	}

	/**
		update completion status.
	*/  
	public static void UpdateCompletion (string nextFlag) {
		var path = assetBasePath + SublimeSocketClient.PREFERENCE_FILE_PATH;

		var paramDict = PreferenceDict(path);
		paramDict[PreferenceSetings.PREFERENCE_ITEM_COMPLETION] = nextFlag;

		WriteOutPreferences(paramDict, path);

		switch (nextFlag) {
			case PreferenceSetings.PREFERENCE_PARAM_COMPLETION_OFF: {
				SublimeSocketClient.completionCont = null;
				break;
			}
			case PreferenceSetings.PREFERENCE_PARAM_COMPLETION_ON: {
				SublimeSocketClient.ReadyCompletion(paramDict);
				break;
			}
			default: {
				break;
			}
		}
	}

	
	/**
		update play flag.
	*/
	public static void UpdatePlayFlag (string nextPlayFlag) {
		var path = assetBasePath + SublimeSocketClient.PREFERENCE_FILE_PATH;

		var paramDict = PreferenceDict(path);
		paramDict[PreferenceSetings.PREFERENCE_ITEM_PLAYFLAG] = nextPlayFlag;

		WriteOutPreferences(paramDict, path);
	}

	/**
		update break flag.
	*/
	public static void UpdateBreakFlag (string nextBreakFlag) {
		var path = assetBasePath + SublimeSocketClient.PREFERENCE_FILE_PATH;

		var paramDict = PreferenceDict(path);
		paramDict[PreferenceSetings.PREFERENCE_ITEM_BREAKFLAG] = nextBreakFlag;

		WriteOutPreferences(paramDict, path);
	}


	public static void UpdateCompileBySave (string compileBySaveFlag) {
		var path = assetBasePath + SublimeSocketClient.PREFERENCE_FILE_PATH;

		var paramDict = PreferenceDict(path);
		paramDict[PreferenceSetings.PREFERENCE_ITEM_COMPILE_BY_SAVE] = compileBySaveFlag;

		WriteOutPreferences(paramDict, path);
	}


	public static void UpdateCompileAnyway (string compileAnywayFlag) {
		var path = assetBasePath + SublimeSocketClient.PREFERENCE_FILE_PATH;

		var paramDict = PreferenceDict(path);
		paramDict[PreferenceSetings.PREFERENCE_ITEM_COMPILE_ANYWAY] = compileAnywayFlag;

		WriteOutPreferences(paramDict, path);
	}


	private static void WriteOutPreferences (Dictionary<string, string> paramDict, string path) {
		// rewrite to file
		var changed = USSAJson.USSAJsonConvert.SerializeObject(paramDict).Replace(",", ",\n\t").Replace("{", "{\n\t").Replace("}", "\n}");

		using (StreamWriter sw = new StreamWriter(path)) {
			sw.WriteLine(changed);
		}
	}

	public static bool ShouldCompileBySave () {
		var path = assetBasePath + SublimeSocketClient.PREFERENCE_FILE_PATH;
		var paramDict = PreferenceDict(path);

		switch (paramDict[PreferenceSetings.PREFERENCE_ITEM_COMPILE_BY_SAVE]) {
			case PreferenceSetings.PREFERENCE_PARAM_COMPILE_BY_SAVE_ON:
				return true;
			default:
				return false;
		}
	}	
}