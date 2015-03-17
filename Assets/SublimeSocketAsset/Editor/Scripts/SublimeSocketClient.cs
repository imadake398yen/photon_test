using UnityEngine;
using UnityEditor;

using System;

using System.IO;
using System.Collections;
using System.Collections.Generic;

using System.Text;
using System.Linq;

using System.Reflection;

using USSAUniRx;

public class SublimeSocketClient {

	/*
		constructor
	*/
	static SublimeSocketClient () {
		// loggingpath, only for debug.
		// LogWriter.StartTransLogging(Directory.GetParent(Application.dataPath).ToString());

		// autoRefreshEnable = true;
		assetBasePath = Application.dataPath;
		ReferenceManager.assetBasePath = assetBasePath;

		unityVersionNumber = Application.unityVersion.Split('.')[0];

		// Deeply depends on Unity's [InitializeOnLoad], WebScoket will be kill before re-run. set notConnected here.
		ReferenceManager.UpdateConnectionStatus(PreferenceSetings.PREFERENCE_PARAM_STATUS_NOTCONNECTED);
	}
	
	public const string SublimeTextVersion = "3";
	public const string SublimeSocketApiVersion = "1.5.0";

	public const string ASSET_FOLDER_NAME = "/SublimeSocketAsset";
	
	// preferences
	public const string PREFERENCE_FILE_PATH = ASSET_FOLDER_NAME + "/Preferences.txt";

	// compile anyway trigger
	public const string PREFERENCE_COMPILETRIGGER_PATH = ASSET_FOLDER_NAME + "/CompileTriggerFile/trigger.cs";
 
	// filer source
	public const string FILTERSOURCE_PATH = ASSET_FOLDER_NAME + "/Filter/UnityFilterSource.txt";

	// switchApp location
	public const string SWITCHAPP_PATH = "SUBLIMESOCKET_PATH:tool/switch/SwitchApp";


	/*
		local updatable datas
	*/
	static readonly string assetBasePath;
	static string unityVersionNumber;
	public static bool UnityEditorVerified = false;
	public static bool UnityEditorIsPlaying = false;
	public static string completelyCompiled = CompilationMessages.COMPILE_WAITING;


	public static USSAWebSocketSharp.WebSocket ws;
	public static CompletionController completionCont;
	private static TransLogger transLogger;
	private static SaveActionManager saveAct;

	private static DummyMainThread dummyMainThread = null;

	// private static readonly bool autoRefreshEnable;

	/*
		called when construct
	*/
	public static void Automate() {
		if (System.IO.File.Exists(assetBasePath + PREFERENCE_FILE_PATH)) {
			// load pref as dictionary
			var paramDict = ReferenceManager.PreferenceDict(assetBasePath + PREFERENCE_FILE_PATH);

			AutoConnect(
				paramDict[PreferenceSetings.PREFERENCE_ITEM_AUTOCONNECT], 
				paramDict[PreferenceSetings.PREFERENCE_ITEM_STATUS]
			);
		}
	}

	/**
		connect automatically
	*/
	public static void AutoConnect(string autoConnectStatusStr, string connectionStatusStr) {
		if (autoConnectStatusStr == PreferenceSetings.PREFERENCE_PARAM_AUTO_OFF) return;

		// check connected or not
		if (connectionStatusStr == PreferenceSetings.PREFERENCE_PARAM_STATUS_NOTCONNECTED) {
			StartConnect();
		}
	}

	/**
		return IsConnectable or not (actually run StartConnect and CloseConnection.)
	*/
	public static bool IsConnectable () {
		StartConnect();
		
		var latestParamDict = ReferenceManager.PreferenceDict(assetBasePath + PREFERENCE_FILE_PATH);
		if (latestParamDict[PreferenceSetings.PREFERENCE_ITEM_ERROR] == PreferenceSetings.PREFERENCE_PARAM_ERROR_NONE) {
			CloseConnection();
			return true;
		}
		CloseConnection();
		return false;
	}


	/**
		set compiled time to force-check triggered for compile 
	*/
	public static void SetLatestSavedTime () {
		var path = assetBasePath + PREFERENCE_FILE_PATH;
		var paramDict = ReferenceManager.PreferenceDict(path);

		switch (paramDict[PreferenceSetings.PREFERENCE_ITEM_COMPILE_ANYWAY]) {
			case PreferenceSetings.PREFERENCE_PARAM_COMPILE_ANYWAY_ON:
				var triggetPath = assetBasePath + PREFERENCE_COMPILETRIGGER_PATH;

				var directoryPath = Directory.GetParent(triggetPath).ToString();
				if (!Directory.Exists(directoryPath)) {
					Directory.CreateDirectory(directoryPath);
				}

				// rewrite as file
				var commentedDateDescription = "//"+DateTime.Now.ToString();
				using (StreamWriter sw = new StreamWriter(triggetPath)) {
					sw.WriteLine(commentedDateDescription);
				}
				break;
			case PreferenceSetings.PREFERENCE_PARAM_COMPILE_ANYWAY_OFF:
				break;
		}
	}

	/**
		close connection.
	*/
	public static void CloseConnection () {
		Assert(ws != null, "ws is null");

		if (ws.ReadyState == USSAWebSocketSharp.WsState.OPEN) ws.Close();

		ReferenceManager.UpdateConnectionStatus(PreferenceSetings.PREFERENCE_PARAM_STATUS_NOTCONNECTED);
	}


	/**
		start connect to SublimeSocket
	*/
	public static void StartConnect () {
		var paramDict = ReferenceManager.PreferenceDict(assetBasePath + PREFERENCE_FILE_PATH);
		// check if ws is exist and not ready for open.
		if (ws != null) {
			Assert(ws.ReadyState == USSAWebSocketSharp.WsState.CLOSED, "already connecting. ws.ReadyState:"+ws.ReadyState);
		}

		
		// set status to connecting
		ReferenceManager.UpdateConnectionStatus(PreferenceSetings.PREFERENCE_PARAM_STATUS_CONNECTING);

		// if WebPlayer, get policy before connect.
		if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebPlayer || 
			EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebPlayerStreamed) {

			// try or die.
			var result = Security.PrefetchSocketPolicy(PreferenceSetings.PREFERENCE_PARAM_DEFAULT_HOST, PreferenceSetings.PREFERENCE_PARAM_DEFAULT_BYTESERVER_PORT);
			if (!result) {
				Debug.LogWarning("SSA:connection aborted. reason:cannot detect Socket Policy server for Web Player @"+PreferenceSetings.PREFERENCE_PARAM_DEFAULT_HOST + ":" + PreferenceSetings.PREFERENCE_PARAM_DEFAULT_BYTESERVER_PORT+", please setup SublimeSocket's ByteDataServer on SublimeSocket before running. see README");
				return;
			}
		}

		ReadyCompletion(paramDict);

		ReadyTransLogging(paramDict);

		ReadySaveAction();

		ConnectToTextEditor(paramDict);
	}



	static void MainThreadBehaviour () {
		MainThreadDispatcher.EditorThreadDispatcher.Instance.UpdateDispatcherHeart();
	}


	
	public static void ReadyCompletion (Dictionary<string, string> paramDict) {
		if (UnityEditorIsPlaying) return;
		if (paramDict[PreferenceSetings.PREFERENCE_ITEM_COMPLETION] == PreferenceSetings.PREFERENCE_PARAM_COMPLETION_ON) {
			var projectDLLBasePath = Directory.GetParent(assetBasePath).ToString() + "/";

			completionCont = new CompletionController(
				logMessage => {
					try {
						var messageDict = new Dictionary<string, string>(){{"message", logMessage}};
						var messageData = USSAJson.USSAJsonConvert.SerializeObject(messageDict);
						Send("showAtLog:"+messageData);
					} catch (Exception e1) {
						Debug.LogError("SSA:failed to send message e1:" + e1);
					}
				},
				statusMessage => {
					try {
						var messageDict = new Dictionary<string, string>(){{"message", statusMessage}};
						var messageData = USSAJson.USSAJsonConvert.SerializeObject(messageDict);
						Send("showStatusMessage:"+messageData);
					} catch (Exception e2) {
						Debug.LogError("SSA:failed to send message e2:" + e2);
					}
				},
				completionData => {
					try {
						var completionCandidates = USSAJson.USSAJsonConvert.SerializeObject(completionData.data);
						Send("runCompletion:"+completionCandidates);
					} catch (Exception e3) {
						Debug.LogError("SSA:failed to send completion e3:" + e3);
					}
				},
				projectDLLBasePath,
				assetBasePath
			);
		}
	}

	static void ReadyTransLogging (Dictionary<string, string> paramDict) {
		Assert(System.IO.File.Exists(paramDict[PreferenceSetings.PREFERENCE_ITEM_TARGET]), "observeTargetFilePath does not exist, target: "+paramDict[PreferenceSetings.PREFERENCE_ITEM_TARGET] + ".\nPlease delete "+ ASSET_FOLDER_NAME +"/Preferences.txt once then choose Window > SublimeSocket > connect.");
		transLogger = new TransLogger(paramDict[PreferenceSetings.PREFERENCE_ITEM_TARGET]);
		transLogger.StartTransLogging(SendLogToTextEditor);
	}

	static void ConnectToTextEditor (Dictionary<string, string> paramDict) {
		var serverPath = paramDict[PreferenceSetings.PREFERENCE_ITEM_SERVER];
		Assert(0 < serverPath.Length, "serverPath is empty, server: "+serverPath);

		ws = new USSAWebSocketSharp.WebSocket(serverPath);

		ws.OnOpen += (sender, e) => {
			// version verify
			{
				var versionVerifyDict = new Hashtable() {
						{"socketVersion", SublimeTextVersion},
						{"apiVersion", SublimeSocketApiVersion}
				};

				var verifyStr = USSAJson.USSAJsonConvert.SerializeObject(versionVerifyDict);
				Send("versionVerify:"+verifyStr);
			}

			// validated
			ReferenceManager.UpdateConnectionStatus(PreferenceSetings.PREFERENCE_PARAM_STATUS_CONNECTED);
			ReferenceManager.UpdateConnectionError(PreferenceSetings.PREFERENCE_PARAM_ERROR_NONE);
			
			
			// input filter to SublimeSocket
			{
				var filterSourceStr = string.Empty;

				// load filterSourceStr from text file
				if (System.IO.File.Exists(assetBasePath + FILTERSOURCE_PATH)) {
					
					using (StreamReader sr = File.OpenText(assetBasePath + FILTERSOURCE_PATH)) {
						filterSourceStr = string.Empty;
						
						var backet = string.Empty;
						while ((backet = sr.ReadLine()) != null) {
							// remove 4 space
							backet = backet.Replace(SocketDefinitions.FILTER_SETTING_TAB, string.Empty);

							// ignore commented line
							if (backet.StartsWith("//")) {
								
							} else {
								// remove line.
								filterSourceStr += backet.Replace("\n", string.Empty);
							}
						}
					}
				}

				//input SublimeSocket filter to Sublime Text.
				foreach (var line in filterSourceStr.Split(new [] {SublimeSocketCommunicationKeys.SUBLIMESOCKET_SIGN}, StringSplitOptions.RemoveEmptyEntries)) {
					Send(line);
				}
			}

			// SublimeSocketAsset rises. start waiting loop.
			SendWaitOrderToTextEditor(100);
		};

		ws.OnMessage += (sender, e) => {
			if (!String.IsNullOrEmpty(e.Data)) {
				if (e.Data.StartsWith(SublimeSocketCommunicationKeys.HEADER_COMPILED)) {
					if (e.Data == SublimeSocketCommunicationKeys.HEADER_COMPILED + ":" + CompilationMessages.COMPILED_FAILED) {
						completelyCompiled = CompilationMessages.COMPILED_FAILED;
					} else {
						completelyCompiled = CompilationMessages.COMPILED_SUCCEEDED;
					}
					
					return;
				}

				if (e.Data == SublimeSocketCommunicationKeys.HEADER_SAVED) {
					if (!ReferenceManager.ShouldCompileBySave()) return;

					if (UnityEditorIsPlaying) {
						SendEditorIsPlayingMessageToTextEditor();
						SendAsyncLogLoadOrderToTextEditor();
						return;
					}

					if (dummyMainThread != null && dummyMainThread.IsRunning()) {
						dummyMainThread.StopThen(Save);
						return;
					}

					// ignite saving.
					Save();

					return;
				}

				if (e.Data.StartsWith(SublimeSocketCommunicationKeys.HEADER_COMPLETION)) {
					if (completionCont == null) return;
					if (UnityEditorIsPlaying) {
						SendEditorIsPlayingMessageToTextEditor();
						return;
					}
					completionCont.IgniteCompletion(e.Data);
					return;
				}

				if (e.Data.StartsWith(SublimeSocketCommunicationKeys.HEADER_SETTING)) {
					if (completionCont == null) return;
					completionCont.ApplyFileSetting(e.Data);
					return;
				}

				if (e.Data == SublimeSocketCommunicationKeys.HEADER_WAITING) {
					
					if (completelyCompiled != CompilationMessages.COMPILE_WAITING) {
						// コンパイル失敗と成功で別れる。失敗であれば何もしない。成功であれば、スレッドをつくる、、が、よく考えたら開始前に必ずスレッド死んでるので、スレッド作る。
						dummyMainThread = new DummyMainThread(MainThreadBehaviour);
						return;
					}

					// continue waiting.
					
					var message = transLogger.ReadNextOrNull();
					SendLogToTextEditor(message);
						
					SendWaitOrderToTextEditor(100);
					return;
				}

				if (e.Data == SublimeSocketCommunicationKeys.HEADER_MANUALLOAD) {
					if (UnityEditorIsPlaying) {
						var message = transLogger.ReadNextOrNull();
						SendLogToTextEditor(message);
					}
					return;
				}

				// verification and refuse

				//verified case
				if (e.Data.StartsWith(SublimeSocketCommunicationKeys.HEADER_IDENTIFICATION_VERIFY)) {
					Debug.Log("SublimeSocket verified");
					return;
				} else if (e.Data.StartsWith(SublimeSocketCommunicationKeys.HEADER_IDENTIFICATION_VERIFY_UPDATABLE)) {
					Debug.Log("SublimeSocket verify result:"+e.Data);
					return;
				}

				//connection refused
				else if (e.Data.StartsWith(SublimeSocketCommunicationKeys.HEADER_REFUSED_SSUPDATE))  {
					Debug.LogError("SublimeSocket verify result:"+e.Data); 
					return;
				} else if (e.Data.StartsWith(SublimeSocketCommunicationKeys.HEADER_REFUSED_CLIENTUPDATE)) {
					Debug.LogError("SublimeSocket verify result:"+e.Data); 
					return;
				} else if (e.Data.StartsWith(SublimeSocketCommunicationKeys.HEADER_REFUSED_SSDIFFERENT)) {
					Debug.LogError("SublimeSocket verify result:"+e.Data); 
					return;
				}
			}
		};

		ws.OnError += (sender, e) => {
			ReferenceManager.UpdateConnectionStatus(PreferenceSetings.PREFERENCE_PARAM_STATUS_NOTCONNECTED);
			switch (e.Message) {
				case "Connection refused":

					Debug.LogWarning("SSA:error. reason:SublimeSocket returns no-signal. please check the SublimeText & SublimeSocket is running.");
					ReferenceManager.UpdateConnectionError(PreferenceSetings.PREFERENCE_PARAM_ERROR_CONREFUSED);
					break;

				case "The WebSocket frame can not be read from the network stream.":
					Debug.LogWarning("SSA:error. reason:"+e.Message);
					ReferenceManager.UpdateConnectionError(PreferenceSetings.PREFERENCE_PARAM_ERROR_CANNOTREAD);
					break;

				case "Thread was being aborted":
					Debug.LogWarning("SSA:WebSocket Thread error. reason:"+e.Message);
					ReferenceManager.UpdateConnectionError(PreferenceSetings.PREFERENCE_PARAM_ERROR_ABORTED);
					break;

				default:
					Debug.LogWarning("SSA:other error. reason:"+e.Message);
					break;
			}
			
			ws.Close();
		};


		ws.OnClose += (sender, e) => {
			ReferenceManager.UpdateConnectionStatus(PreferenceSetings.PREFERENCE_PARAM_STATUS_NOTCONNECTED);

			switch (e.Reason) {
				case "The WebSocket frame can not be read from the network stream.":
					Debug.LogWarning("SSA:connection closed. reason:"+e.Reason);
					break;
				default:
					Debug.LogWarning("SSA:connection closed with unexpected reason: "+e.Reason);
					break;
			}
		};
		
		ws.Connect();
	}

	static void ReadySaveAction () {
		saveAct = new SaveActionManager();
		saveAct.StartManaging();
	}

	static void SendLogToTextEditor (string message) {
		if (string.IsNullOrEmpty(message)) return;
		var logContents = message.Replace(SublimeSocketCommunicationKeys.SUBLIMESOCKET_CONCAT, "'-''>'").Replace("//", "'/''/'");

		foreach (string splitted in Split(logContents, sendableSize)) {
			var filterDict = new Hashtable() {
				{"name", "unity"},
				{"source", splitted}
				// ,{"debug", true}
			};
			// back \n for multiline matching.
			var filterStr = USSAJson.USSAJsonConvert.SerializeObject(filterDict).Replace("\\n", "\n");

			Send("filtering:" + filterStr);
		}
	}

	

	static void Save () {
		// update saveTrigger.cs
		SetLatestSavedTime();

		completelyCompiled = CompilationMessages.COMPILE_WAITING;

		// if (currentThreadType == SublimeSocketThreading.THREADTYPE_UNITY_EDITOR_UPDATE) {
		// 	saveAct.Save();
		// 	// need work.
		// } else {
			SaveFromDummyThread();
		// }
	}

	static void SaveFromDummyThread () {
		/*
			save unity app through focusing.
			this feature requires AutoRefresh freature of Unity Editor.
			should be "ON"

			but not yet applied.
		*/
		// if (autoRefreshEnable) {} else {}

		switch (Application.platform) {
			case RuntimePlatform.OSXEditor:{
				// ask "call SwitchApp" to SublimeSocket
				var shellPath = SWITCHAPP_PATH;

				var shellDict = new Hashtable() {
					{"main", ""},
					{"", shellPath},
					{"-f", "com.unity3d.UnityEditor" + unityVersionNumber + ".x"},
					{"-t", "com.sublimetext."+SublimeTextVersion}
					// ,{"debug", "true"}
				};

				var shellStr = USSAJson.USSAJsonConvert.SerializeObject(shellDict);

				Send("runShell:"+shellStr);
				break;
			}
			case RuntimePlatform.WindowsEditor:{
				var shellPath = SWITCHAPP_PATH.Replace("/", "\\\\");

				var shellDict = new Hashtable() {
					{"main", ""},
					{"", shellPath+".exe"},
					{"-f", "Unity"}
					// ,{"debug", "true"}
				};
				
				var shellStr = USSAJson.USSAJsonConvert.SerializeObject(shellDict);
				Send("runShell:"+shellStr);

				var shellDict2 = new Hashtable() {
					{"main", ""},
					{"", shellPath+".exe"},
					{"-f", "sublime_text"},
					{"delay", "100"}
					// ,{"debug", "true"}
				};
				
				shellStr = USSAJson.USSAJsonConvert.SerializeObject(shellDict2);
				Send("runShell:"+shellStr);
				break;
			}
		}
		
		// compilation will begin. will success or fail.
		// if compilation will fail, SSA keeps connecting. (but Unity Editor's Update loop is unstable. should reactivate if need.)
		// or succeded, compilation will start, this context will be destroyed.

		// should wait completelyCompiled sign from Unity Editor.
		SendWaitOrderToTextEditor(1000);
	}

	static void SendWaitOrderToTextEditor (int waitFrame) {
		var reWaitEvent = new Hashtable() {
			{"identity", SublimeSocketCommunicationKeys.HEADER_WAITING},
			{"ms", waitFrame.ToString()},
			{"selectors", 
				new List<object>() {
					new Hashtable() {
						{
							"monocastMessage", new Dictionary<string, string>() {
								{"target", "unitysocket"},
								{"message", SublimeSocketCommunicationKeys.HEADER_WAITING}
							}
						}
					}
				}
			},
		};
		var reWaitEventStr = USSAJson.USSAJsonConvert.SerializeObject(reWaitEvent);

		Send("afterAsync:" + reWaitEventStr);
	}


	static void SendAsyncLogLoadOrderToTextEditor () {
		var reWaitEvent = new Hashtable() {
			{"identity", SublimeSocketCommunicationKeys.HEADER_MANUALLOAD},
			{"ms", "1000"},
			{"selectors", 
				new List<object>() {
					new Hashtable() {
						{
							"monocastMessage", new Dictionary<string, string>() {
								{"target", "unitysocket"},
								{"message", SublimeSocketCommunicationKeys.HEADER_MANUALLOAD}
							}
						}
					}
				}
			},
		};
		var reWaitEventStr = USSAJson.USSAJsonConvert.SerializeObject(reWaitEvent);

		Send("afterAsync:" + reWaitEventStr);
	}


	static void SendEditorIsPlayingMessageToTextEditor () {
		var logEvent = new Hashtable() {{"message", CompletionMessages.MESSAGE_EDITOR_IS_PLAYING}};
		var logEventStr = "showAtLog:" + USSAJson.USSAJsonConvert.SerializeObject(logEvent);
		var messageEventStr = "showStatusMessage:" + USSAJson.USSAJsonConvert.SerializeObject(logEvent);

		Send(logEventStr + SublimeSocketCommunicationKeys.SUBLIMESOCKET_CONCAT + messageEventStr);
	}

	static List<string> Split(string str, int chunkSize) {
		return Enumerable.Range(0, (str.Length / chunkSize) + 1).Select(i => {
			var readableSize = str.Length - i * chunkSize;
			if (chunkSize < readableSize) {
				readableSize = chunkSize;
			}

			return str.Substring(i * chunkSize, readableSize);
		}).ToList();
	}

	static void Send (string message) {
		if (ws == null) return;
		if (ws.ReadyState != USSAWebSocketSharp.WsState.OPEN) return;
		if (!ws.Send(SublimeSocketCommunicationKeys.SUBLIMESOCKET_SIGN + message)) {
			Debug.LogError("faild to send log to SublimeSocket message:" + message);
		}
	}



	const int sendableSize = (int)(USSAWebSocketSharp.WebSocket._fragmentLen * 0.8);

	static void Assert(bool condition, string message) {
		if (!condition) throw new Exception(message);
	}
}
