using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.IO.Compression;

using USSAUniRx;

/*
	generate project cache lib & static cache lib.
*/
class CompletionCacheController {

	private string assetPath;
	private List<string> staticLibAddresses;

	// action for push
	private Action <string, string> Completed;

	// core type tree of completion.
	TreeDict completionLibrary;

	// extension method library.
	Dictionary<string, LeafDict> extensionMethodPoolDict = new Dictionary<string, LeafDict>();


	public CompletionCacheController (string assetPath, List<string> staticLibAddresses, string activeDllPath) {
		this.assetPath = assetPath;
		this.staticLibAddresses = staticLibAddresses;

		// generate project library.
		var firstLoadDll = new List<string>();
		if (SublimeSocketAssetSettings.IS_NOT_TRIAL) firstLoadDll.Add(activeDllPath);
		completionLibrary = GenerateTypeTree(firstLoadDll, ref extensionMethodPoolDict);
	}

	public IObservable<string> ReadyCompletionCache () {
		return Observable.FromCoroutine<string>(ReadyCompletionCacheObs);
	}

	public IEnumerator ReadyCompletionCacheObs (IObserver<string> observer) {
		SyncReadyCompletionCache();
		
		observer.OnNext("ReadyCompletionCacheObs over");
		observer.OnCompleted();
		yield break;
	}


	public void SyncReadyCompletionCache () {
		var cachePath = assetPath + CompletionDLLInformations.STATIC_COMPLETION_CACHE_PATH;

		if (System.IO.File.Exists(cachePath)) {
			ReadLibraryCache(ref completionLibrary, cachePath, staticLibAddresses, ref extensionMethodPoolDict);
		} else {
			GenerateLibraryCacheTo(ref completionLibrary, cachePath, staticLibAddresses, ref extensionMethodPoolDict);
		}

		// merge extension method to library.
		MergeExtensionMethods(ref completionLibrary, extensionMethodPoolDict);
		CompletionMatcher.UpdateCompletionLibrary(completionLibrary);
		
	}

	/**
		merge extension method definition to completion library.
	*/
	private void MergeExtensionMethods (ref TreeDict completionLibrary, Dictionary<string, LeafDict> extensionMethodDict) {
		foreach (var extendedTypeName in extensionMethodDict.Keys) {
			if (completionLibrary.ContainsKey(extendedTypeName)) {
				var methodDict = completionLibrary[extendedTypeName][CompletionDictSettings.KEY_METHOD];
				
				foreach (var key in extensionMethodDict[extendedTypeName].Keys) {
					methodDict[key] = extensionMethodDict[extendedTypeName][key];
				}
			}
		}
	}


	void ReadLibraryCache (ref TreeDict completionLibrary, string cachePath, List<string> staticLibAddresses, ref Dictionary<string, LeafDict> extensionMethodPoolDict) {
		var source = string.Empty;
		var count = staticLibAddresses.Count;

		bool changed;

		using (StreamReader sr = new StreamReader(cachePath)) {
			var firstLineCount = int.Parse(sr.ReadLine());
			if (count == firstLineCount) changed = false;
			else changed = true;

			source = sr.ReadToEnd();
		}

		if (changed) {
			GenerateLibraryCacheTo(ref completionLibrary, cachePath, staticLibAddresses, ref extensionMethodPoolDict);
			return;
		}

		var unzipped = "";
		switch (Application.platform) {
			case RuntimePlatform.OSXEditor:{
				// zipcase 
				unzipped = Decompress(source);
				break;
			}
			case RuntimePlatform.WindowsEditor:{
				// cannnot decompress. System.DllNotFoundException: MonoPosixHelper
				unzipped = source;
				break;
			}
		}

		try {
			var staticTreeDict = USSAJson.USSAJsonConvert.DeserializeObject<TreeDict>(unzipped);

			// merge (looks duty, but faster than LINQ.)
			foreach (var key in staticTreeDict.Keys) {
				completionLibrary[key] = staticTreeDict[key];
			}
		} catch (Exception e) {
			Debug.LogWarning("SSA failed to read completion libraries. please re-compile unity project. error:"+e);
			// System.ExecutionEngineException: SIGILL
			// do nothing
		}
	}



	void GenerateLibraryCacheTo (ref TreeDict completionLibrary, string cachePath, List<string> staticLibAddresses, ref Dictionary<string, LeafDict> extensionMethodPoolDict) {
		// record count for regenerate.
		var count = staticLibAddresses.Count;
		
		// generate type tree for static cache.
		var staticTreeDict = GenerateTypeTree(staticLibAddresses, ref extensionMethodPoolDict);

		
		// serialize
		var jsonData = USSAJson.USSAJsonConvert.SerializeObject(staticTreeDict);

		var compressed = string.Empty;
		switch (Application.platform) {
			case RuntimePlatform.OSXEditor:{
				// zipcase 
				compressed = Compress(jsonData);
				break;
			}
			case RuntimePlatform.WindowsEditor:{
				// cannnot compress. System.DllNotFoundException: MonoPosixHelper
				compressed = jsonData;
				break;
			}
		}


		using (StreamWriter sw = new StreamWriter(cachePath)) {
			sw.WriteLine(count);
			sw.WriteLine(compressed);
		}

		// merge static library to completion library. (looks duty, but faster than LINQ.)
		foreach (var key in staticTreeDict.Keys) {
			completionLibrary[key] = staticTreeDict[key];
		}
	}

	/**
		return type completion tree.
	*/
	TreeDict GenerateTypeTree(List<string> dllPathList, ref Dictionary<string, LeafDict> extensionMethodPoolDict) {

		var typeTree = new TreeDict();

		foreach (var path in dllPathList) {
			Assembly assembly = null;

			try {
				assembly = Assembly.LoadFile(path);
				var types = assembly.GetTypes();

				foreach (Type type in types) {
					const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | 
						 BindingFlags.Instance | BindingFlags.Static;

					
					var typeStr = type.ToString();//not suite .Name() for geting name.

					if (typeStr.Contains("+")) {
						// ignore. 
						continue;
					}

					/*
						extract class info
					*/
					var classDict = new LeafDict();

						// ignore

					var classParamDict = new Dictionary<string, string>();
					{	
						// includes generic or not. if includes, class definition contains <T>.
						if (typeStr.Contains(CompletionDLLInformations.DELIM_GENERIC)) {
							// remove "`", "number", "<" & ">" .
							var splitted = typeStr.Split(new char [] {CompletionDLLInformations.DELIM_GENERIC});
							classParamDict[CompletionDictSettings.COMPLETIONKEY_HEAD] = splitted[0];
							classParamDict[CompletionDictSettings.COMPLETIONKEY_RETURN] = typeStr;
							classParamDict[CompletionDictSettings.COMPLETIONKEY_PARAMTYPES] = "<" + GenerateTypesStr(splitted[1]) + ">";
							classParamDict[CompletionDictSettings.COMPLETIONKEY_PARAMNAMES] = "<" + GeneratetSublimeCompletionParameterFormatStr(GenerateTypesStr(splitted[1])) + ">";
						} else {
							classParamDict[CompletionDictSettings.COMPLETIONKEY_HEAD] = typeStr;
							classParamDict[CompletionDictSettings.COMPLETIONKEY_RETURN] = typeStr;
							classParamDict[CompletionDictSettings.COMPLETIONKEY_PARAMTYPES] = string.Empty;
							classParamDict[CompletionDictSettings.COMPLETIONKEY_PARAMNAMES] = GeneratetSublimeCompletionParameterFormatStr(string.Empty);
						}
						
					}
					classDict[typeStr] = classParamDict;
				
					/*
						extract class-constructor info
					*/
					var constructorDict = new LeafDict();
					var constructors = type.GetConstructors();
					foreach (var constructor in constructors) {
						var parametes = constructor.GetParameters();

						var paramTypeList = parametes.Select(paramsDef => paramsDef.ParameterType.ToString().Split('.').Last()).ToList();
						var paramNameList = parametes.Select(paramsDef => paramsDef.Name).ToList();

						var paramDict = new Dictionary<string, string>();

						// if is generic, remove generic description from completion info.
						if (typeStr.Contains(CompletionDLLInformations.DELIM_GENERIC)) {
							var head = string.Empty;
							var paramTypes = string.Empty;
							var paramNames = string.Empty;
							
							int indexOffsetNext = GenerateGenericInformations(typeStr, ref head, ref paramTypes, ref paramNames) + 1;
							
							paramDict[CompletionDictSettings.COMPLETIONKEY_HEAD] = head;
							paramDict[CompletionDictSettings.COMPLETIONKEY_RETURN] = type.ToString();
							
							paramDict[CompletionDictSettings.COMPLETIONKEY_PARAMTYPES] = "<" + paramTypes + ">" + "(" + GenerateParamsStr(paramTypeList) + ")";
							paramDict[CompletionDictSettings.COMPLETIONKEY_PARAMNAMES] = "<" + paramNames + ">" + "(" + GeneratetSublimeCompletionParameterFormatStr(GenerateParamsStr(paramNameList), indexOffsetNext) + ")";
						} else {
							paramDict[CompletionDictSettings.COMPLETIONKEY_HEAD] = type.ToString().Split('.').Last();
							paramDict[CompletionDictSettings.COMPLETIONKEY_RETURN] = type.ToString();
							paramDict[CompletionDictSettings.COMPLETIONKEY_PARAMTYPES] = "(" + GenerateParamsStr(paramTypeList) + ")";
							paramDict[CompletionDictSettings.COMPLETIONKEY_PARAMNAMES] = "(" + GeneratetSublimeCompletionParameterFormatStr(GenerateParamsStr(paramNameList)) + ")";
						}
						constructorDict[typeStr+".ctor:"+GenerateParamsStr(paramTypeList)] = paramDict;	
					}

					/* 
						extract method info
					*/
					var methodDict = new LeafDict();
					var methods = type.GetMethods(flags);


					foreach (var method in methods) {
						var parametes = method.GetParameters();

						// returnType and headName and param
						var headName = method.Name;
						
						

						// if this method is extension of other method, pool this.
						var isExtensionMethod = method.IsDefined(typeof(ExtensionAttribute), false);
						if (isExtensionMethod) {
							var extensionRootType = parametes[0].ParameterType.ToString();
							
							if (!extensionMethodPoolDict.ContainsKey(extensionRootType)) {
								extensionMethodPoolDict[extensionRootType] = new LeafDict();
							}
							var paramTypeListForExtensionMethod = parametes.Select(paramsDef => paramsDef.ParameterType.ToString().Split('.').Last()).ToList();
							paramTypeListForExtensionMethod = paramTypeListForExtensionMethod.GetRange(1, paramTypeListForExtensionMethod.Count-1);

							var paramNameListForExtensionMethod = parametes.Select(paramsDef => paramsDef.Name).ToList();
							paramNameListForExtensionMethod = paramNameListForExtensionMethod.GetRange(1, paramNameListForExtensionMethod.Count-1);

							var paramDictForExtensionMethod = new Dictionary<string, string>();
							paramDictForExtensionMethod[CompletionDictSettings.COMPLETIONKEY_HEAD] = headName;
							paramDictForExtensionMethod[CompletionDictSettings.COMPLETIONKEY_RETURN] = method.ReturnType.ToString();
							
							paramDictForExtensionMethod[CompletionDictSettings.COMPLETIONKEY_PARAMTYPES] = "(" + GenerateParamsStr(paramTypeListForExtensionMethod) + ")";
							paramDictForExtensionMethod[CompletionDictSettings.COMPLETIONKEY_PARAMNAMES] = "(" + GeneratetSublimeCompletionParameterFormatStr(GenerateParamsStr(paramNameListForExtensionMethod)) + ")";
							
							extensionMethodPoolDict[extensionRootType][headName+GenerateParamsStr(paramTypeListForExtensionMethod)] = paramDictForExtensionMethod;
							continue;
						}

						var paramTypeList = parametes.Select(paramsDef => paramsDef.ParameterType.ToString().Split('.').Last()).ToList();
						var paramNameList = parametes.Select(paramsDef => paramsDef.Name).ToList();

						var paramDict = new Dictionary<string, string>();
						paramDict[CompletionDictSettings.COMPLETIONKEY_HEAD] = headName;
						paramDict[CompletionDictSettings.COMPLETIONKEY_RETURN] = method.ReturnType.ToString();
						
						paramDict[CompletionDictSettings.COMPLETIONKEY_PARAMTYPES] = "(" + GenerateParamsStr(paramTypeList) + ")";
						paramDict[CompletionDictSettings.COMPLETIONKEY_PARAMNAMES] = "(" + GeneratetSublimeCompletionParameterFormatStr(GenerateParamsStr(paramNameList)) + ")";
						

						// method name contains the name of parameters type list. every overload method will be identify.
						methodDict[headName+GenerateParamsStr(paramTypeList)] = paramDict;
					}

					/*
						extract property info
					*/
					var propDict = new LeafDict();
					var properties = type.GetProperties(flags);
					foreach (var prop in properties) {
						var propStr = prop.ToString();
						var return_propName = propStr.Split(new char [] {' '}, 2);

						var returnType = return_propName[0];
						var propName = return_propName[1];

						var paramDict = new Dictionary<string, string>();
						paramDict[CompletionDictSettings.COMPLETIONKEY_HEAD] = propName;
						paramDict[CompletionDictSettings.COMPLETIONKEY_RETURN] = returnType;
						paramDict[CompletionDictSettings.COMPLETIONKEY_PARAMTYPES] = string.Empty;
						paramDict[CompletionDictSettings.COMPLETIONKEY_PARAMNAMES] = string.Empty;

						propDict[propName] = paramDict;
					}
					

					var fieldDict = new LeafDict();
					var fields = type.GetFields(flags);
					foreach (var field in fields) {
						var fieldStr = field.ToString();
						var return_fieldName = fieldStr.Split(new char [] {' '}, 2);

						var returnType = return_fieldName[0];
						var fieldName = return_fieldName[1];

						var paramDict = new Dictionary<string, string>();
						paramDict[CompletionDictSettings.COMPLETIONKEY_HEAD] = fieldName;
						paramDict[CompletionDictSettings.COMPLETIONKEY_RETURN] = returnType;
						paramDict[CompletionDictSettings.COMPLETIONKEY_PARAMTYPES] = string.Empty;
						paramDict[CompletionDictSettings.COMPLETIONKEY_PARAMNAMES] = string.Empty;

						fieldDict[fieldName] = paramDict;
					}

					var branch = new BranchDict();
					
					branch[CompletionDictSettings.KEY_CLASS] = classDict;
					branch[CompletionDictSettings.KEY_CONSTRUCTOR] = constructorDict;
					branch[CompletionDictSettings.KEY_METHOD] = methodDict;
					branch[CompletionDictSettings.KEY_PROPERTY] = propDict;
					branch[CompletionDictSettings.KEY_FIELD] = fieldDict;


					if (typeStr.Contains(CompletionDLLInformations.DELIM_GENERIC)) {
						var splitted = typeStr.Split(new char [] {CompletionDLLInformations.DELIM_GENERIC});
						var modifiedTypeStr = splitted[0] + splitted[1].Split(CompletionDLLInformations.DELIM_OPENBRACE)[0];
						
						typeTree[modifiedTypeStr] = branch;//methodDict;
					} else {
						typeTree[typeStr] = branch;//methodDict;
					}
				}

			} catch (Exception e) {
				Debug.LogWarning("SSA: failed to get information for completion from the dll:" + path + " because:"+e);
				continue;
			}
		}

		return typeTree;
	}


	private int GenerateGenericInformations (string source, ref string headRef, ref string paramTypesRef, ref string paramNamesRef) {
		var baseSplitted = source.Split(new char [] {CompletionDLLInformations.DELIM_GENERIC});

		// set headRef
		headRef = baseSplitted[0];

		var paramTypesBase = baseSplitted[1].Split(CompletionDLLInformations.DELIM_OPENBRACE)[1];
		var paramTypesStr = paramTypesBase.Substring(0, paramTypesBase.Length - 1);
		
		// set paramTypesRef
		paramTypesRef = paramTypesStr;


		// generate Generic paramTargets
		var listed = paramTypesStr.Split(',');

		var formattedGenericCollectionStr = "${" + 1 + ":"+listed[0]+"}";
		for (int i = 1; i < listed.Length; i++) {
			formattedGenericCollectionStr += ", ${"+(i+1)+":"+listed[i]+"}";
		}

		// set paramTargetsRef
		paramNamesRef =  formattedGenericCollectionStr;

		// return parameter length of Generic.
		return listed.Length;
	}

	private string GeneratetSublimeCompletionParameterFormatStr (string commadStringArray, int index = 1) {
		if (commadStringArray.Length == 0) return string.Empty;
		var listed = commadStringArray.Split(',');

		var formattedCollectionStr = "${" + index + ":"+listed[0]+"}";
		for (int i = 1; i < listed.Length; i++) {
			formattedCollectionStr += ", ${"+(index + i)+":"+listed[i]+"}";
		}

		return formattedCollectionStr;
	}

	

	/**
		Generate param strings from paramList
		e.g.
			source,filename,mode
	*/
	string GenerateParamsStr (List<string> paramList) {
		var paramStr = string.Empty;

		if (0 < paramList.Count) {
			paramStr += paramList[0];
			for (int i = 1; i < paramList.Count; i++) {
				paramStr += ","+paramList[i];
			}
		}

		return paramStr;
	}

	/**
		Generate type strings from typelist.
		e.g. 
			from 
				2[T1, T2]

			to
				T1,T2

	*/
	private string GenerateTypesStr (string paramListSource) {
		var typeListStr = string.Empty;
		
		paramListSource = paramListSource.Substring(1, paramListSource.Length-1);
		
		paramListSource = paramListSource.Replace("[", string.Empty);
		paramListSource = paramListSource.Replace("]", string.Empty);

		var typeList = paramListSource.Split(new char [] {','});
		if (0 < typeList.Length) {
			typeListStr += typeList[0];
			for (int i = 1; i < typeList.Length; i++) {
				typeListStr += ","+typeList[i];
			}
		}

		return typeListStr;
	}



	// zip
	public static string Compress(string s) {
		var bytes = Encoding.Unicode.GetBytes(s);
		using (var msi = new MemoryStream(bytes))
		using (var mso = new MemoryStream()) {
			using (var gs = new GZipStream(mso, CompressionMode.Compress)) {
				CopyStream(msi, gs);
			}
			return Convert.ToBase64String(mso.ToArray());
		}
	}

	public static string Decompress(string s) {
		var bytes = Convert.FromBase64String(s);
		using (var msi = new MemoryStream(bytes))
		using (var mso = new MemoryStream()) {
			using (var gs = new GZipStream(msi, CompressionMode.Decompress)) {
				CopyStream(gs, mso);
			}
			return Encoding.Unicode.GetString(mso.ToArray());
		}
	}

	public static void CopyStream(Stream input, Stream output) {
		byte[] buffer = new byte[32768];
		int read;
		while ((read = input.Read(buffer, 0, buffer.Length)) > 0) {
			output.Write (buffer, 0, read);
		}
	}

}



/**
	the branch dictionary of method/property/class
*/
class LeafDict : Dictionary<string, Dictionary<string, string>> {}

class BranchDict : Dictionary<string, LeafDict> {}

class TreeDict : Dictionary<string, BranchDict> {}



