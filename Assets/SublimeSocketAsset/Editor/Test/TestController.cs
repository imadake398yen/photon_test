using System.Collections.Generic;
using System.IO;
using UnityEngine;

/**
	The test suite for completion with Sublime Text. not for Atom.
	These test target files are out of this Asset.
*/
public class TestController {
	static int testIndex = 0;
	static string answer_HEAD;
	static string answer_head;
	static string answer_ret;
	static string answer_paramTypesStr;
	static string answer_paramTargetsStr;

	public static void Test (CompletionController compCont, int fireTestIdentity, string generatedCompletionIdentity, string testSourcePath) {
		Debug.Log("test start:" + fireTestIdentity);
		if (testResultCompCollection.ContainsKey(generatedCompletionIdentity)) {
			if (0 < testResultCompCollection.Count) {
				
				var succeeded = false;
				var contents = testResultCompCollection[generatedCompletionIdentity];
				foreach (var contentList in contents) {

					foreach (var dict in contentList) {
						var HEAD			= dict[CompletionDataFormats.FORMAT_LARGEHEAD];
						var head			= dict[CompletionDataFormats.FORMAT_SMALLHEAD];
						var ret				= dict[CompletionDataFormats.FORMAT_RETURNTYPE];
						var paramTypesStr	= dict[CompletionDataFormats.FORMAT_PARAMSTYPEFMT];
						var paramTargetsStr	= dict[CompletionDataFormats.FORMAT_PARAMSTARGETFMT];

						// Debug.LogError("HEAD "+HEAD);
						if (HEAD != answer_HEAD) continue;
						
						// Debug.LogError("head "+head);
						if (head != answer_head) continue;
						
						// Debug.LogError("ret "+ret);
						if (ret != answer_ret) {
							Debug.LogError("answer_ret "+answer_ret);
							continue;
						}
						
						// Debug.LogError("paramTypesStr "+paramTypesStr);
						if (paramTypesStr != answer_paramTypesStr) {
							Debug.LogError("answer_paramTypesStr "+answer_paramTypesStr + "/vs "+paramTypesStr);
							continue;
						}

						// Debug.LogError("paramTargetsStr "+paramTargetsStr);
						if (paramTargetsStr != answer_paramTargetsStr) {
							Debug.LogError("answer_paramTargetsStr "+answer_paramTargetsStr + "/vs "+paramTargetsStr);
							continue;
						}

						succeeded = true;
						break;
					}
				}
				
				if (succeeded) Debug.LogError("Test generatedCompletionIdentity PASSED! :"+generatedCompletionIdentity + " /result:" + testResultCompCollection.Count);
				else Debug.LogWarning("Test generatedCompletionIdentity FAILED by parameter! :"+generatedCompletionIdentity);
			}
		} else {
			Debug.LogWarning("Test generatedCompletionIdentity FAILED by no result! :"+generatedCompletionIdentity);
		}

		testResultCompCollection.Clear();
		
		Debug.LogError("start next:" + testIndex);

		switch (testIndex) {

			case 0:{
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/NameSpace.cs";
				Debug.Log("0 path:" + path);
				var completionIdentity = testIndex+" resolve using with alias:NameSpace.cs";
				var body = File.ReadAllText(path);
				Debug.Log("loaded");
				var point = new Point(11, 15);

				answer_HEAD = "UnityEngine.ScaleMode";
				answer_head = "ScaleMode";
				answer_ret = "ScaleMode";
				answer_paramTypesStr = "";
				answer_paramTargetsStr = "";
				Debug.Log("loaded2");
				compCont.Complete(path, completionIdentity, body, point);
				Debug.Log("loaded3");
				break;
			}


			case 1:{
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/NameSpace2.cs";
				var completionIdentity = testIndex+" resolve using with alias 2:NameSpace2.cs";
				var body = File.ReadAllText(path);
				var point = new Point(11, 15);

				answer_HEAD = "UnityEngine.ScaleMode";
				answer_head = "ScaleMode";
				answer_ret = "ScaleMode";
				answer_paramTypesStr = "";
				answer_paramTargetsStr = "";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 2:{
				// . attacked
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample2.cs";
				var completionIdentity = testIndex+" resolve local dot completion:CompletionSample2.cs";
				var body = File.ReadAllText(path);
				var point = new Point(15, 15);

				answer_HEAD = "IsNullOrEmpty";
				answer_head = "IsNullOrEmpty";
				answer_ret = "Boolean";
				answer_paramTypesStr = "(String)";
				answer_paramTargetsStr = "(${1:value})";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 3:{
				// completion approached
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample3.cs";
				var completionIdentity = testIndex+" resolve local deep dot completion:CompletionSample3.cs";
				var body = File.ReadAllText(path);
				var point = new Point(15, 22);

				answer_HEAD = "CompareTo";
				answer_head = "CompareTo";
				answer_ret = "Int32";
				answer_paramTypesStr = "(Int32)";
				answer_paramTargetsStr = "(${1:value})";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 4:{
				// single using collection then get inherited.
				// head hit for "tra" to "tansform"
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample4.cs";
				var completionIdentity = testIndex+" resolve current class's local property:CompletionSample4.cs";
				var body = File.ReadAllText(path);
				var point = new Point(14, 3);

				answer_HEAD = "transform";
				answer_head = "transform";
				answer_ret = "Transform";
				answer_paramTypesStr = "";
				answer_paramTargetsStr = "";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			// new series
			case 5:{
				/* 文中のNew */
				
				// resolve var + "new " +[SOMETHING]
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_2.cs";
				var completionIdentity = testIndex+" resolve new + space inside sentence:CompletionSample5_2.cs";
				var body = File.ReadAllText(path);
				var point = new Point(16, 15);

				answer_HEAD = "Shader";
				answer_head = "Shader";
				answer_ret = "Shader";
				answer_paramTypesStr = "()";
				answer_paramTargetsStr = "()";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 6:{
				/* 文頭のNew */
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_3.cs";
				var completionIdentity = testIndex+" resolve new + space with line head:CompletionSample5_3.cs";
				var body = File.ReadAllText(path);
				var point = new Point(16, 7);

				answer_HEAD = "Skybox";
				answer_head = "Skybox";
				answer_ret = "Skybox";
				answer_paramTypesStr = "()";
				answer_paramTargetsStr = "()";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 7:{
				// Equal Match
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_4.cs";
				var completionIdentity = testIndex+" complete equal + head:CompletionSample5_4.cs";
				var body = File.ReadAllText(path);
				var point = new Point(16, 9);

				answer_HEAD = "rigidbody";
				answer_head = "rigidbody";
				answer_ret = "Rigidbody";
				answer_paramTypesStr = "";
				answer_paramTargetsStr = "";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 8:{
				// Return Match
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_5.cs";
				var completionIdentity = testIndex+" complete return + head:CompletionSample5_5.cs";
				var body = File.ReadAllText(path);
				var point = new Point(16, 10);

				answer_HEAD = "transform";
				answer_head = "transform";
				answer_ret = "Transform";
				answer_paramTypesStr = "";
				answer_paramTargetsStr = "";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 9:{
				// Alias Dot Match

				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_7.cs";
				var completionIdentity = testIndex+" resolve alias dot:CompletionSample5_7.cs";
				var body = File.ReadAllText(path);
				var point = new Point(8, 4);

				answer_HEAD = "System.Collections.Generic.Dictionary";
				answer_head = "Dictionary";
				answer_ret = "Dictionary`2[TKey,TValue]";
				answer_paramTypesStr = "<TKey,TValue>";
				answer_paramTargetsStr = "<${1:TKey}, ${2:TValue}>";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 10:{
				// this Dot Match
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_8.cs";
				var completionIdentity = testIndex+" resolve this dot:CompletionSample5_8.cs";
				var body = File.ReadAllText(path);
				var point = new Point(5, 7);

				answer_HEAD = "tag";
				answer_head = "tag";
				answer_ret = "String";
				answer_paramTypesStr = "";
				answer_paramTargetsStr = "";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}
			

			case 11:{
				// (something + else.)
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample6.cs";
				var completionIdentity = testIndex+" complete inner brace, local dot:CompletionSample6.cs";
				var body = File.ReadAllText(path);
				var point = new Point(11, 39);

				answer_HEAD = "StartsWith";
				answer_head = "StartsWith";
				answer_ret = "Boolean";
				answer_paramTypesStr = "(String,StringComparison)";
				answer_paramTargetsStr = "(${1:value}, ${2:comparisonType})";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}
			

			case 12:{
				// new + List constructor contains Type Define + Parameter.
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample9.cs";
				var completionIdentity = testIndex+" complete List with new:CompletionSample9.cs";
				var body = File.ReadAllText(path);
				var point = new Point(5, 15);

				answer_HEAD = "List";
				answer_head = "List";
				answer_ret = "List`1[T]";
				answer_paramTypesStr = "<T>(Int32)";
				answer_paramTargetsStr = "<${1:T}>(${2:capacity})";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 13:{
				// new + Dictionary constructor contains Type Define + Parameter.
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample9_1.cs";
				var completionIdentity = testIndex+" complete equal + new + head:CompletionSample9_1.cs";
				var body = File.ReadAllText(path);
				var point = new Point(5, 15);

				answer_HEAD = "Dictionary";//Dictionary<TKey, TValue>(capacity, comparer)
				answer_head = "Dictionary";
				answer_ret = "Dictionary`2[TKey,TValue]";
				answer_paramTypesStr = "<TKey,TValue>(Int32,IEqualityComparer`1[TKey])";
				answer_paramTargetsStr = "<${1:TKey}, ${2:TValue}>(${3:capacity}, ${4:comparer})";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 14:{
				// arrayList
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_10.cs";
				var completionIdentity = testIndex+" complete Array method:CompletionSample5_10.cs";
				var body = File.ReadAllText(path);
				var point = new Point(6, 12);

				answer_HEAD = "Find";
				answer_head = "Find";
				answer_ret = "T";
				answer_paramTypesStr = "(T[],Predicate`1[T])";
				answer_paramTargetsStr = "(${1:array}, ${2:match})";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 15:{
				// generic parameterized method inside "()"
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_11.cs";
				var completionIdentity = testIndex+" complete inside if statement:CompletionSample5_11.cs";
				var body = File.ReadAllText(path);
				var point = new Point(6, 11);

				answer_HEAD = "Count";
				answer_head = "Count";
				answer_ret = "Int32";
				answer_paramTypesStr = "";
				answer_paramTargetsStr = "";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 16:{
				// parameter completion inside "()"
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_13.cs";
				var completionIdentity = testIndex+" complete inside parameter of method:CompletionSample5_13.cs";
				var body = File.ReadAllText(path);
				var point = new Point(6, 19);

				answer_HEAD = "up";
				answer_head = "up";
				answer_ret = "Vector3";
				answer_paramTypesStr = "";
				answer_paramTargetsStr = "";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 17:{
				// parameter completion in the end of line
				
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_14.cs";
				var completionIdentity = testIndex+" complete the middle line local dot:CompletionSample5_14.cs";
				var body = File.ReadAllText(path);
				var point = new Point(15, 59);

				answer_HEAD = "GetType";
				answer_head = "GetType";
				answer_ret = "Type";
				answer_paramTypesStr = "()";
				answer_paramTargetsStr = "()";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 18:{
				// Generics with using order at last.
				
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_15.cs";
				var completionIdentity = testIndex+" complete equal new List:CompletionSample5_15.cs";
				var body = File.ReadAllText(path);
				var point = new Point(8, 15);

				answer_HEAD = "List";
				answer_head = "List";
				answer_ret = "List`1[T]";
				answer_paramTypesStr = "<T>(Int32)";
				answer_paramTargetsStr = "<${1:T}>(${2:capacity})";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 19:{
				// Debug.Log in () part1
				
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_16.cs";
				var completionIdentity = testIndex+" complete the top line in method:CompletionSample5_16.cs";
				var body = File.ReadAllText(path);
				var point = new Point(12, 8);

				answer_HEAD = "Log";
				answer_head = "Log";
				answer_ret = "Void";
				answer_paramTypesStr = "(Object)";
				answer_paramTargetsStr = "(${1:message})";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 20:{
				// Debug.Log in () part2
				
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_16.cs";
				var completionIdentity = testIndex+" complete inside method:CompletionSample5_16.cs";
				var body = File.ReadAllText(path);
				var point = new Point(16, 9);

				answer_HEAD = "Log";
				answer_head = "Log";
				answer_ret = "Void";
				answer_paramTypesStr = "(Object,Object)";
				answer_paramTargetsStr = "(${1:message}, ${2:context})";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}
			
			case 21:{
				// categoly method

				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_19.cs";
				var completionIdentity = testIndex+" complete categoly method:CompletionSample5_19.cs";
				var body = File.ReadAllText(path);
				var point = new Point(9, 6);

				answer_HEAD = "TimeAssert";
				answer_head = "TimeAssert";
				answer_ret = "Void";
				answer_paramTypesStr = "(String,Int32)";
				answer_paramTargetsStr = "(${1:reason}, ${2:additionalSec})";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 22:{
				// bool categoly method without define.

				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_19.cs";
				var completionIdentity = testIndex+" complete categoly method without defined bool:CompletionSample5_19.cs";
				var body = File.ReadAllText(path);
				var point = new Point(21, 8);

				answer_HEAD = "Assert";
				answer_head = "Assert";
				answer_ret = "Void";
				answer_paramTypesStr = "(String)";
				answer_paramTargetsStr = "(${1:reason})";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}

			case 23:{
				// bool categoly method with () and without define.

				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_19.cs";
				var completionIdentity = testIndex+" complete categoly method without defined bool with ():CompletionSample5_19.cs";
				var body = File.ReadAllText(path);
				var point = new Point(25, 10);

				answer_HEAD = "Assert";
				answer_head = "Assert";
				answer_ret = "Void";
				answer_paramTypesStr = "(String)";
				answer_paramTargetsStr = "(${1:reason})";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}

			case 24:{
				// string categoly method with () and without define.

				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_19.cs";
				var completionIdentity = testIndex+" complete categoly method without defined string with ():CompletionSample5_19.cs";
				var body = File.ReadAllText(path);
				var point = new Point(29, 11);

				answer_HEAD = "TimeAssert";
				answer_head = "TimeAssert";
				answer_ret = "Void";
				answer_paramTypesStr = "(String,Int32)";
				answer_paramTargetsStr = "(${1:reason}, ${2:additionalSec})";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}

			// 文中の文頭の補完、入れちゃいたい。")", "{", "," の後であれば、単語として補完かけるやつ。字数限定のところとは別に、かなあ、、面倒。
			// あと四則演算のも全部入るわ。*-+/%&|とスペースだけど、うーーーん、、、文字かどうか、で判定すれば良いか。
			// ・spaceは取り除く　記号の後　であれば、とか。
			// まだやらない。

			case 25:{// parameter completion in the mid of block, will retry.
				/*
					retry
				*/
					
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_14.cs";
				var completionIdentity = testIndex+" complete local param in head of line:CompletionSample5_14.cs";
				var body = File.ReadAllText(path);
				var point = new Point(11, 5);

				answer_HEAD = "Count";
				answer_head = "Count";
				answer_ret = "Int32";
				answer_paramTypesStr = "";
				answer_paramTargetsStr = "";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}

			
			case 26:{// this Dot Match with retry
				/*
					retry
				*/
				
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_8.cs";
				var completionIdentity = testIndex+" complete inside foreach:CompletionSample5_8.cs";
				var body = File.ReadAllText(path);
				var point = new Point(11, 8);

				answer_HEAD = "tag";
				answer_head = "tag";
				answer_ret = "String";
				answer_paramTypesStr = "";
				answer_paramTargetsStr = "";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			case 27:{// dot match retry. in deep, abnormal phrase.
				/*
					retry
				*/
				
				var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_17.cs";
				var completionIdentity = testIndex+" complete static methof inside deep switch:CompletionSample5_17.cs";
				var body = File.ReadAllText(path);
				var point = new Point(26, 14);

				answer_HEAD = "Log";
				answer_head = "Log";
				answer_ret = "Void";
				answer_paramTypesStr = "(Object,Object)";
				answer_paramTargetsStr = "(${1:message}, ${2:context})";

				compCont.Complete(path, completionIdentity, body, point);
				break;
			}


			// using <- will not support.

			// new itself <- will not support.

			// this itself <- will not support.

			// base <- will not support.

			// case 0:{
			// 	// enum

			// 	var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_18.cs";
			// 	var completionIdentity = testIndex+" complete enum:CompletionSample5_18.cs";
			// 	var body = File.ReadAllText(path);
			// 	var point = new Point(11, 4);

			// 	answer_HEAD = "Log";
			// 	answer_head = "Log";
			// 	answer_ret = "Void";
			// 	answer_paramTypesStr = "(Object,Object)";
			// 	answer_paramTargetsStr = "(${1:message}, ${2:context})";

			// 	compCont.Complete(path, completionIdentity, body, point);
			// 	break;
			// }

			// case x:{// caution unknown completion
			// 	var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_12.cs";
			// 	var completionIdentity = path;
			// 	var body = File.ReadAllText(completionIdentity);
			// 	var point = new Point(6, 11);

			// 	answer_HEAD = "List";
			// 	answer_head = "List";
			// 	answer_ret = "List`1[T]";
			// 	answer_paramTypesStr = "<T>(Int32)";
			// 	answer_paramTargetsStr = "<${1:T}>(${2:capacity})";

			// 	Complete(path, completionIdentity, body, point);
			// 	break;
			// }

			// case 12:{// base Dot Match
			// 	var path = testSourcePath + "TestAssets/SublimeSocketAsset/Tests/CompletionSample5_9.cs";
			// 	var completionIdentity = path;
			// 	var body = File.ReadAllText(completionIdentity);
			// 	var point = new Point(5, 7);

			default:
				// looped. finally call from loop.
				break;
		}

		testIndex++;
	}


	static Dictionary<string, List<CompletionCollection>> testResultCompCollection = new Dictionary<string, List<CompletionCollection>>();

	public static void AddCompletionTestResult (string testCompletionIdentityCandidate, CompletionCollection compCollection) {
		if (testResultCompCollection.Keys.Count == 0) {
			testResultCompCollection[testCompletionIdentityCandidate] = new List<CompletionCollection>();
		}
		testResultCompCollection[testCompletionIdentityCandidate].Add(compCollection);

	}
}
