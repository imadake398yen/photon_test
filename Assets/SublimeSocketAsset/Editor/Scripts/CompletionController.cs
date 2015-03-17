using UnityEngine;
using UnityEditor;

using System;
using System.Text;
using System.IO;

using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using System.Runtime.Serialization;
using System.Xml;
using System.Linq;

using USSAUniRx;

/**
	completion control
*/
public class CompletionController {
	readonly List<string> otherLibAddresses;

	// completion support.
	private TypeMapGenerateController typeMapGenCont;
	private CompletionCacheController compCacheCont;


	// completion push method.
	private Action<CompletionParamDict> PushCompletion;

	// box for latest completion information.
	private static string currentCompletionIdentity;

	private static string currentPath; 
	private static string currentBody;
	private static Point currentPoint;
	private static string currentLineStr;
	private static string currentMatchMode;


	static int beforeErrorCount;

	/**
		constructor

		load after compiling.
	*/
	public CompletionController (
		Action<string> PushLog,
		Action<string> PushStatusMessage,
		Action<CompletionParamDict> PushCompletion, 
		string basePath, 
		string assetBasePath
	) {
		this.PushCompletion = PushCompletion;
		
		otherLibAddresses = LoadProjctStaticLibAddresses(assetBasePath);
		

		// ready completion matcher
		{
			CompletionMatcher.Completed = (string completionIdentity, CompletionCollection compCollection, bool isFinal) => {
				Completed(completionIdentity, compCollection, isFinal);
			};

			CompletionMatcher.Recomplete = (string completionIdentity, string nextMatchMode) => {
				Recomplete(completionIdentity, nextMatchMode);
			};
		}

		var projectActiveLibAddress = Path.Combine(basePath, CompletionDLLInformations.PROJECT_DLL_PATH);

		/*
			setup typemap generator
		*/
		typeMapGenCont = new TypeMapGenerateController(
			(string completionIdentity, TypeMap typeMap, string body, Point point) => {
				// send ast error count to client if exist.
				if (0 < typeMap.errorCount) {
					PushStatusMessage(CompletionMessages.ASTVALIDATION_HEADER + typeMap.errorCount + CompletionMessages.ASTVALIDATION_FAIL_FOOTER + (point.row + 1));
				}

				if (typeMap.errorCount == 0 && beforeErrorCount != 0) {
					PushStatusMessage(CompletionMessages.ASTVALIDATION_HEADER + typeMap.errorCount + CompletionMessages.ASTVALIDATION_FIXED_FOOTER);
				}

				beforeErrorCount = typeMap.errorCount;

				// continue completion.
				TypeMapped(completionIdentity, typeMap, body, point);
			}, 
			projectActiveLibAddress, otherLibAddresses
		);


		/*
			setup cache controller.
		*/
		compCacheCont = new CompletionCacheController(basePath, otherLibAddresses, projectActiveLibAddress);

		// ready typeMap and cache.
		var parallel = Observable.WhenAll(
			typeMapGenCont.ReadyTypeFoundation(),
			compCacheCont.ReadyCompletionCache()
		);

		parallel.Subscribe(results => {
			// start consuming completion queue.
			Observable.EveryUpdate().Subscribe(_ => IgniteLast());

			// notify
			PushLog(CompletionMessages.MESSAGE_COMPLETION_READY);
		});
		
	}

	public void IgniteLast () {
		if (0 < completeQueue.Count) {
			var last = completeQueue.LastOrDefault();

			completeQueue.Clear();
			Complete(
				last.path,
				last.completionIdentity,
				last.body,
				last.point
			);
		}
	}

	public void ApplyFileSetting (string source) {
		var p = source.Split(new char[] {':'});
		if (p.Length != 3) return;

		int tabCount;
		if (!int.TryParse(p[1], out tabCount)) {
			return;
		}

		bool useSpace;
		if (!bool.TryParse(p[2], out useSpace)) {
			return;
		}

		// by default
		CompletionMatcher.tabReplacer = " ";

		if (useSpace) {
			var tabStr = string.Empty;
			for (int i = 0; i < tabCount; i++) {
				tabStr = tabStr + " ";
			}

			CompletionMatcher.tabReplacer = tabStr;
		}
	}

	
	public List<string> LoadProjctStaticLibAddresses (string assetBasePath) {
		var libAddresses = new List<string>();
		
		// add platform dependent dll
		switch (Application.platform) {
			case RuntimePlatform.OSXEditor:{
				libAddresses.Add(Path.Combine(EditorApplication.applicationPath, CompletionDLLInformations.MAC_UNITY_DLL_PATH));
				if (SublimeSocketAssetSettings.IS_NOT_TRIAL) libAddresses.Add(Path.Combine(EditorApplication.applicationPath, CompletionDLLInformations.MAC_CSHARP_DLL_PATH));
				break;
			}
			case RuntimePlatform.WindowsEditor:{
				var windowsDirBasePath = Directory.GetParent(Directory.GetParent(EditorApplication.applicationPath).ToString()).ToString() + SocketOSSettings.WINDOWS_ADDING_PATH_SPLITTER;
				libAddresses.Add(Path.Combine(windowsDirBasePath, CompletionDLLInformations.WINDOWS_UNITY_DLL_PATH));
				if (SublimeSocketAssetSettings.IS_NOT_TRIAL) libAddresses.Add(Path.Combine(windowsDirBasePath, CompletionDLLInformations.WINDOWS_CSHARP_DLL_PATH));
				break;
			}
		}

		// add other /project/Assets/*.dll paths
		// exclude /Editor folder.
		var otherDllsPaths = Directory.GetFiles(assetBasePath, "*.dll", SearchOption.AllDirectories).Where(path => !path.Contains(CompletionDLLInformations.SSA_EXCLUDE_PATH_EDITOR)).ToList();
		libAddresses = libAddresses.Union(otherDllsPaths).ToList();
		
		return libAddresses;
	}


	/**
		start completion from received source
	*/
	public void IgniteCompletion (string resource) {
		var splittedResource = resource.Split(new char[] {':'}, 5);
		
		if (splittedResource.Length != 5) {
			Debug.LogError(CompletionMessages.RESULTMESSAGE_ILLEGALFORMAT + ":less or much of parameters");
			return;
		}

		if (!splittedResource[2].Contains(",")) {
			Debug.LogError(CompletionMessages.RESULTMESSAGE_ILLEGALFORMAT + ":less of row and col");
			return;
		}


		var completionIdentity = splittedResource[1];
		

		var rowColStrArray = splittedResource[2].Split(',');
		

		int row;
		if (!int.TryParse(rowColStrArray[0], out row)) {
			return;
		}
		
		int col;
		if (!int.TryParse(rowColStrArray[1], out col)) {
			return;
		}


		var path = splittedResource[3];
		var bodyCandicateLines = splittedResource[4].Replace("\r", string.Empty).Split('\n');// r is for windows.
		var body = string.Join("\n", bodyCandicateLines.Select(line => DeleteAfterComment(line)).ToArray());
		
		var point = new Point(row, col);

		// Debug.LogError("row "+row +", col "+col);

		// --------------- data receiving verified! start completing. ---------------

		AddCompletion(path, completionIdentity, body, point);;

		// return control as soon as possible.
		return;
	}

	public string DeleteAfterComment (string line) {
		if (!line.Contains("//")) return line;
		return line.Substring(0, line.IndexOf("//"));
	}

	public void AddCompletion (string path, string completionIdentity, string body, Point point) {
		var newOne = new Queued(path, completionIdentity, body, point);
		completeQueue.Add(newOne);
	}

	List<Queued> completeQueue = new List<Queued>();
	class Queued {
		public string path;
		public string completionIdentity;
		public string body;
		public Point point;

		public Queued (string path, string completionIdentity, string body, Point point) {
			this.path = path;
			this.completionIdentity = completionIdentity;
			this.body = body;
			this.point = point;
		}
	}


	/**
		ignite complete
			-> return controll

			-> generate TypeMap
				-> Match
					-> send completions
	*/
	public void Complete (string path, string completionIdentity, string body, Point point) {
		// update latest
		currentCompletionIdentity = completionIdentity;

		currentPath = path;
		currentBody = body;
		currentPoint = point;
		currentLineStr = string.Empty;
		
		currentMatchMode = CompletionMatcher.DetectMatchingTrigger(ref body, point, ref currentLineStr);
		typeMapGenCont.TypeMapUpdate(completionIdentity, path, body, point);
	}

	/**
		retry with same data. 2nd another type matching.
		-> generate TypeMap
			-> Match
				-> send completions
	*/
	public void Recomplete (string completionIdentity, string nextMatchMode) {
		if (currentCompletionIdentity != completionIdentity) return;

		var path = currentPath;
		var body = currentBody;
		var point = currentPoint;

		currentMatchMode = CompletionMatcher.ModifyByMatchedTrigger(nextMatchMode, ref body, point);

		if (currentMatchMode == CompletionMatches.MATCH_KIND_NONE) return;// abort
		
		// type matching. with sync(already in other thread.)
		typeMapGenCont.ResetIdentity();// same completion identity, other data.
		typeMapGenCont.TypeMapUpdate(completionIdentity, path, body, point);
	}


	/**
		new TypeMap pushed from TypeMapGenerateController -> CompletionMatcher
	*/
	private void TypeMapped (string completionIdentity, TypeMap typeMap, string body, Point point) {
		/*
			if identity not changed (by incoming latest completion), continue to push completion.
				parameters never changed from start. also matchMode too.

			else,
				generate latest completion newly. like "It is put to here JUST NOW."

				this mechanism reduces number of generation of the TypeMap, although the Threads.
		*/
		if (currentCompletionIdentity == completionIdentity) {
			CompletionMatcher.MatchModeMatching(currentCompletionIdentity, currentMatchMode, typeMap, currentPoint, currentLineStr);
		} else {
			// another new data is set! run with latest data.
			Complete(currentPath, currentCompletionIdentity, currentBody, currentPoint);
		}
	}



	/**
		push completed data via identity gate.
	*/
	private void Completed (string completionIdentity, CompletionCollection compCollection, bool isFinal) {
		// if empty, do nothing.
		if (compCollection.count == 0) return;

		if (currentCompletionIdentity == completionIdentity) {
			if (isFinal) {
				var data = new CompletionParamDict(
					currentPath,
					completionIdentity,
					compCollection,
					true // this is "show completion window" flag for SublimeSocket.
				);
				PushCompletion(data);
			} else {
				var data = new CompletionParamDict(
					currentPath,
					completionIdentity,
					compCollection,
					false
				);
				PushCompletion(data);
			}
		}
	}




	static void Assert(bool condition, string message) {
		if (!condition) throw new Exception(message);
	}

}



/**
	the completion info dict
*/
public class CompletionParamDict {
	public readonly Dictionary<string, object> data;

	/**
		constructor for formatted data of Completion.
	*/
	public CompletionParamDict (
		string path, 
		string identity,
		CompletionCollection completionCollections,
		bool show
	) {
		data = new Dictionary<string, object>();
		// set format and parts 
		data["name"] = path;
		data["identity"] = identity;
		data["pool"] = identity;
		if (show) data["show"] = identity;
		data["formathead"] = CompletionDataFormats.FORMAT_LARGEHEAD + CompletionDataFormats.FORMAT_PARAMSTYPEFMT + "\t"+CompletionDataFormats.FORMAT_RETURNTYPE;
		data["formattail"] = CompletionDataFormats.FORMAT_SMALLHEAD + CompletionDataFormats.FORMAT_PARAMSTARGETFMT + "$0";
		data["completion"] = completionCollections;
	}
}

