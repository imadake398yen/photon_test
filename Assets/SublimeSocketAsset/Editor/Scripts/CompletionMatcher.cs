using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;

using System.Text.RegularExpressions;

/*
	static function class for completion matching.
*/
class CompletionMatcher {

	// action for push completion
	public static Action<string, CompletionCollection, bool> Completed;

	// action for run re-completion
	public static Action<string, string> Recomplete;


	private static TreeDict completionLibrary = new TreeDict();

	private static readonly List<string> DEFINED_PRIMITIVES = new List<string>{
		"Boolean", "Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64", 
		"IntPtr", "UIntPtr", "Char", "Double", "Single"
	};

	public static readonly List<string> DEFINED_KEYWORDS = new List<string>{
		/*standard keywords*/ 
			"abstract", "as", "base", "bool", "break", "byte", 
			"case", "catch", "char", "checked", "class", "const", "continue", 
			"decimal", "default", "delegate", "do", "double", "else", "enum", 
			"event", "explicit", "extern", "false", "finally", "fixed", "float", 
			"for", "foreach", "goto", "if", "implicit", "in", "int", "interface", 
			"internal", "is", "lock", "long", "namespace", "new", "null", "object", 
			"operator", "out", "override", "params", "private", "protected", "public", 
			"readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", 
			"static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", 
			"uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",

		/*contextual keywords*/ 
			"add", "alias", "ascending", "async", "await", "descending", "dynamic", "from", 
			"get", "global", "group", "into", "join", "let", "orderby", "partial", 
			"remove", "select", "set", "value", "var", "where", "yield"
	};
	

	public static string tabReplacer = " ";// by default, tab(\t) will be replaced by single space. for count as 1.


	/**
		matching trigger detection.
			detect 
				"."
				" new "
				", "
				"(" 
	*/
	public static string DetectMatchingTrigger (ref string body, Point point, ref string lineStr) {
		string [] lines = body.Split('\n');
		var targetLineStr = lines[point.row];

		// if lines contains tab, replace tab to spaces for count string.
		if (targetLineStr.Contains("\t")) {
			targetLineStr = targetLineStr.Replace("\t", tabReplacer);
		}
		
		// Debug.LogError("this.tabReplacer "+this.tabReplacer+"/"+this.tabReplacer.Length);
		// Debug.LogError("targetLineStr "+ targetLineStr);
		// Debug.LogError("targetLineStr.length " + targetLineStr.Length);
		// Debug.LogError("r/c" + point.row + ", "+point.col);

		if (0 < targetLineStr.Length) {
			var targetPoint = point.col-1;
			if (targetPoint < 0) return CompletionMatches.MATCH_KIND_NONE;
			
			if (targetLineStr.Length <= targetPoint) return CompletionMatches.MATCH_KIND_NONE;

			
			var latestHitStr = targetLineStr[targetPoint].ToString();
			var toTargetPointStr = targetLineStr.Substring(0, targetPoint);
			var toTargetPointStrWithoutSpace = toTargetPointStr.Replace(CompletionMatches.MATCH_KIND_SPACE, CompletionMatches.MATCH_KIND_NONE);

			// Debug.LogError("latestHitStr "+latestHitStr);
			// Debug.LogError("toTargetPointStr "+toTargetPointStr);
			// Debug.LogError("toTargetPointStrWithoutSpace "+toTargetPointStrWithoutSpace);

			/*
				ends with "."
				decrease col counter.

				actually:
					Something.[cursor is here]Else

				input:
					Something[cursor is here]

				dot(.) is ignored. in this turn.
			*/
			if (latestHitStr.EndsWith(CompletionMatches.MATCH_KIND_DOT)) {
				
				// also not next to "}"
				if (targetLineStr.EndsWith(CompletionMatches.MATCH_KIND_DOTIGNORE)) {
					return CompletionMatches.MATCH_KIND_NONE;
				}

				var baseLine = lines[point.row];
				var modifiedTargetPoint = point.col - 1;
				var head = baseLine.Substring(0, modifiedTargetPoint);
				
				var tailCharacterNum = baseLine.Length - point.col;
				var tail = baseLine.Substring(point.col, tailCharacterNum);
				
				// exchange body. remove new "."
				lines[point.row] = head + tail;
				body = string.Join("\n", lines);

				if (toTargetPointStrWithoutSpace.EndsWith(CompletionMatches.MATCH_KIND_THIS)) return CompletionMatches.MATCH_KIND_THIS;

				return CompletionMatches.MATCH_KIND_DOT;
			}

			/*
				"new" + " " + some character
			*/
			else if (toTargetPointStr.EndsWith(CompletionMatches.MATCH_KIND_NEWSPACE)) {
				// set latestHitStr as hint
				lineStr = latestHitStr;

				return CompletionMatches.MATCH_KIND_NEWSPACE;
			}

			/*
				"=" + some character
			*/
			else if (toTargetPointStrWithoutSpace.EndsWith(CompletionMatches.MATCH_KIND_EQUAL) && latestHitStr != CompletionMatches.MATCH_KIND_SPACE) {
				// set latestHitStr as hint
				lineStr = latestHitStr;

				return CompletionMatches.MATCH_KIND_EQUAL;
			}

			/*
				"return" + " " + some character
			*/
			else if (toTargetPointStrWithoutSpace.EndsWith(CompletionMatches.MATCH_KIND_RETURN) && latestHitStr != CompletionMatches.MATCH_KIND_SPACE) {
				// set latestHitStr as hint
				lineStr = latestHitStr;
				
				return CompletionMatches.MATCH_KIND_RETURN;
			}

			else {
				lineStr = targetLineStr.TrimStart();
				var lentgh = lineStr.Length;

				// no space, no comment, suite length.
				if (CompletionSettings.HEADHIT_MIN <= lentgh && lentgh <= CompletionSettings.HEADHIT_LIMIT && latestHitStr != CompletionSettings.HEADHIT_EXCLUDE_BRACKET) {
					return CompletionMatches.MATCH_KIND_HEAD;
				}
			}
		}

		return CompletionMatches.MATCH_KIND_NONE;
	}

	public static string ModifyByMatchedTrigger (string nextMatchMode, ref string body, Point point) {
		string [] lines = body.Split('\n');
		var targetLineStr = lines[point.row];

		// if lines contains tab, replace tab to spaces for count string.
		if (targetLineStr.Contains("\t")) {
			targetLineStr = targetLineStr.Replace("\t", tabReplacer);
		}

		switch (nextMatchMode) {
			case CompletionMatches.MATCH_KIND_DOT_RETRY:
			case CompletionMatches.MATCH_KIND_THIS_RETRY:
				var baseLine = lines[point.row];
				var modifiedTargetPoint = point.col - 1;
				var head = baseLine.Substring(0, modifiedTargetPoint);

				var tailCharacterNum = baseLine.Length - point.col;
				var tail = baseLine.Substring(point.col, tailCharacterNum);
				
				// exchange body. remove new "."
				lines[point.row] = head + tail;

				for (int i = point.row + 1; i < lines.Length; i++) {
					if (lines[i].EndsWith(CompletionMatches.MATCH_KIND_DOT_EXIT_BKT)) break;
					lines[i] = string.Empty;
				}

				body = string.Join("\n", lines);
				
				return nextMatchMode;
		}

		return CompletionMatches.MATCH_KIND_NONE;
	}


	/**
		run matching with matchMode.
			run after map generated.
	*/
	public static void MatchModeMatching (string completionIdentity, string matchMode, TypeMap typeMap, Point point, string lineStr) {
		switch (matchMode) {
			case CompletionMatches.MATCH_KIND_NONE:
				// no need to continue matching. map was generated.
				// var typeInfos = GetMatchedPointTypeInfos(typeMap, point);
				
				// foreach (var typeInfo in typeInfos) {
				// 	Debug.LogError("MATCH_KIND_NONE typeInfo "+ typeInfo.type);
				// }

				break;

			case CompletionMatches.MATCH_KIND_HEAD:
				HeadMatch(completionIdentity, typeMap, lineStr, point);
				break;

			case CompletionMatches.MATCH_KIND_NEWSPACE:
				NewMatch(completionIdentity, typeMap, lineStr, point);
				break;

			case CompletionMatches.MATCH_KIND_EQUAL:
				EqualMatch(completionIdentity, typeMap, lineStr, point);
				break;

			case CompletionMatches.MATCH_KIND_RETURN:
				ReturnMatch(completionIdentity, typeMap, lineStr, point);
				break;

			case CompletionMatches.MATCH_KIND_THIS:
				ThisMatch(completionIdentity, typeMap, point, false);
				break;

			case CompletionMatches.MATCH_KIND_THIS_RETRY:
				ThisMatch(completionIdentity, typeMap, point, true);
				break;

			case CompletionMatches.MATCH_KIND_DOT:
				DotMatch(completionIdentity, typeMap, point, false);
				break;

			case CompletionMatches.MATCH_KIND_DOT_RETRY:
				DotMatch(completionIdentity, typeMap, point, true);
				break;
		
			default:
				break;
		}
	}


	/**
		Head matching by some string characters.
		
		[whole empty spaces] or [(] or [, ] + string (char 1)
		but no Linq completion triggers.
	*/
	public static void HeadMatch (string completionIdentity, TypeMap typeMap, string headStr, Point point) {
		var currentUsingTypes = GetUsingTypes(typeMap);
		var currentFileTypes = GetFileDefinedTypes(typeMap);

		var combinedList = currentUsingTypes.Concat(currentFileTypes).ToList();

		var currentNameSpaces = GetNameSpaces(typeMap);

		var limit = new CompLimitation(
			CompletionLimitations.RETURN_TYPE_LIMIT_VOID | CompletionLimitations.RETURN_TYPE_LIMIT_PRIMITIVE | CompletionLimitations.RETURN_TYPE_LIMIT_OBJECT,
			CompletionLimitations.RETURN_ATTR_LIMIT_FIELD | CompletionLimitations.RETURN_ATTR_LIMIT_PROPERTY |
			CompletionLimitations.RETURN_ATTR_LIMIT_METHOD | CompletionLimitations.RETURN_ATTR_LIMIT_CLASS,
			headStr,
			currentNameSpaces.Select(x => x + ".").ToList()// add dot for matching with candidate's type name.
		);

		SendCompletion(completionIdentity, combinedList, limit, headStr);
	}

	/**
		"new" + Something
	*/
	public static void NewMatch (string completionIdentity, TypeMap typeMap, string headStr, Point point) {
		var currentUsingTypes = GetUsingTypes(typeMap);
		var currentFileTypes = GetFileDefinedTypes(typeMap);

		var combinedList = currentUsingTypes.Concat(currentFileTypes).ToList();

		var currentNameSpaces = GetNameSpaces(typeMap);

		var limit = new CompLimitation(
			CompletionLimitations.RETURN_TYPE_LIMIT_VOID | CompletionLimitations.RETURN_TYPE_LIMIT_PRIMITIVE | CompletionLimitations.RETURN_TYPE_LIMIT_OBJECT,
			CompletionLimitations.RETURN_ATTR_LIMIT_CLASS | CompletionLimitations.RETURN_ATTR_LIMIT_CONSTRUCTOR,
			headStr,
			currentNameSpaces.Select(x => x + ".").ToList()// add dot for matching with candidate's type name.
		);

		SendCompletion(completionIdentity, combinedList, limit, headStr);
	}

	/**
		"=" + Something
	*/
	public static void EqualMatch (string completionIdentity, TypeMap typeMap, string headStr, Point point) {
		var currentUsingTypes = GetUsingTypes(typeMap);
		var currentFileTypes = GetFileDefinedTypes(typeMap);

		var combinedList = currentUsingTypes.Concat(currentFileTypes).ToList();

		var currentNameSpaces = GetNameSpaces(typeMap);

		var limit = new CompLimitation(
			CompletionLimitations.RETURN_TYPE_LIMIT_PRIMITIVE | CompletionLimitations.RETURN_TYPE_LIMIT_OBJECT,
			CompletionLimitations.RETURN_ATTR_LIMIT_CLASS | CompletionLimitations.RETURN_ATTR_LIMIT_METHOD | 
			CompletionLimitations.RETURN_ATTR_LIMIT_PROPERTY | CompletionLimitations.RETURN_ATTR_LIMIT_FIELD,
			headStr,
			currentNameSpaces.Select(x => x + ".").ToList()// add dot for matching with candidate's type name.
		);

		SendCompletion(completionIdentity, combinedList, limit, headStr);
	}

	/**
		"return" + Something
	*/
	public static void ReturnMatch (string completionIdentity, TypeMap typeMap, string headStr, Point point) {
		var currentUsingTypes = GetUsingTypes(typeMap);
		var currentFileTypes = GetFileDefinedTypes(typeMap);

		var combinedList = currentUsingTypes.Concat(currentFileTypes).ToList();

		var currentNameSpaces = GetNameSpaces(typeMap);

		var limit = new CompLimitation(
			CompletionLimitations.RETURN_TYPE_LIMIT_PRIMITIVE | CompletionLimitations.RETURN_TYPE_LIMIT_OBJECT,
			CompletionLimitations.RETURN_ATTR_LIMIT_CLASS | CompletionLimitations.RETURN_ATTR_LIMIT_METHOD | 
			CompletionLimitations.RETURN_ATTR_LIMIT_PROPERTY | CompletionLimitations.RETURN_ATTR_LIMIT_FIELD,
			headStr,
			currentNameSpaces.Select(x => x + ".").ToList()// add dot for matching with candidate's type name.
		);

		SendCompletion(completionIdentity, combinedList, limit, headStr);
	}

	/**
		"this" + "."
	*/
	public static void ThisMatch (string completionIdentity, TypeMap typeMap, Point point, bool isRecompleted) {
		
		var matchPoint = new Point(point.row, point.col - 1);// back for "."
		var typeInfos = GetMatchedPointTypeInfos(typeMap, matchPoint);

		var types = typeInfos.Select(typeInfo => typeInfo.type).ToList();

		var limit = new CompLimitation(
			CompletionLimitations.RETURN_TYPE_LIMIT_PRIMITIVE | CompletionLimitations.RETURN_TYPE_LIMIT_OBJECT | CompletionLimitations.RETURN_TYPE_LIMIT_VOID,
			CompletionLimitations.RETURN_ATTR_LIMIT_CONSTRUCTOR | CompletionLimitations.RETURN_ATTR_LIMIT_METHOD | 
			CompletionLimitations.RETURN_ATTR_LIMIT_PROPERTY | CompletionLimitations.RETURN_ATTR_LIMIT_FIELD
		);

		var sended = SendCompletion(completionIdentity, types, limit, string.Empty);

		if (!sended && !isRecompleted) {
			Recomplete(completionIdentity, CompletionMatches.MATCH_KIND_THIS_RETRY);
		}
	}


	/**
		matching by [.] and point location
	*/
	public static void DotMatch (string completionIdentity, TypeMap typeMap, Point point, bool isRecompleted) {
		// matching by type information map from point.
		var matchPoint1 = new Point(point.row, point.col - 1);// back for "."
		var typeInfos1 = GetMatchedPointTypeInfos(typeMap, matchPoint1);

		var cLimit = new CompLimitation(
			CompletionLimitations.RETURN_TYPE_LIMIT_PRIMITIVE | CompletionLimitations.RETURN_TYPE_LIMIT_OBJECT | CompletionLimitations.RETURN_TYPE_LIMIT_VOID,
			CompletionLimitations.RETURN_ATTR_LIMIT_FIELD | CompletionLimitations.RETURN_ATTR_LIMIT_PROPERTY | CompletionLimitations.RETURN_ATTR_LIMIT_METHOD | CompletionLimitations.RETURN_ATTR_LIMIT_CLASS
		);

		var types1 = typeInfos1.Select(typeInfo => typeInfo.type).ToList();

		var sended1 = SendCompletion(completionIdentity, types1, cLimit, string.Empty);
		

		// if includes using alias, will return alias's member. or not, return class's member.
		if (!sended1) {
			var aliasKinds = typeInfos1.Where(i => i.kind == CompletionTypeStrings.TYPE_ALIASNAMESPACE_RESOLVERESULT).Select(t => t.type).ToList();

			// get child-class of alias.
			var aliasedTypes = GetNameSpacesChildren(aliasKinds);
			
			var cLimitForAlias = new CompLimitation(
				CompletionLimitations.RETURN_TYPE_LIMIT_PRIMITIVE | CompletionLimitations.RETURN_TYPE_LIMIT_OBJECT | CompletionLimitations.RETURN_TYPE_LIMIT_VOID,
				CompletionLimitations.RETURN_ATTR_LIMIT_CLASS
			);

			SendCompletion(completionIdentity, aliasedTypes, cLimitForAlias, string.Empty);
		}

		// if not completed, retry with modification.
		if (!sended1 && !isRecompleted) {
			Recomplete(completionIdentity, CompletionMatches.MATCH_KIND_DOT_RETRY);
		}
	}




	private static List<CompletionInfo> GetMatchedPointTypeInfos (TypeMap typeMap, Point point) {
		
		// add + 1 for validate row & col matching info.
		var detectRow = point.row + 1;
		var detectCol = point.col + 1;

		var results = typeMap.Keys.Where(keyPoint => detectRow == keyPoint.row && detectCol == keyPoint.col).
			Select(keyPoint => {
				var type = typeMap[keyPoint].type;

				// for matching, remove generic-type information description.
				if (type.Contains(CompletionDLLInformations.DELIM_GENERIC)) {
					var splitted = type.Split(CompletionDLLInformations.DELIM_GENERIC);
					typeMap[keyPoint].type = splitted[0] + splitted[1].Split(CompletionDLLInformations.DELIM_OPENBRACE)[0];
				}

				if (type.EndsWith(CompletionMatches.MATCH_KIND_ARRAY_MARK)) {
					typeMap[keyPoint].type = CompletionTypeStrings.TYPE_ARRAY;
				}

				return typeMap[keyPoint];
			}).ToList();

		// foreach (var result in results) {
		// 	Debug.LogError("result type "+result.type);
		// }

		return results;
	}


	private static List<string> GetFileDefinedTypes (TypeMap typeMap) {
		var typeList = typeMap.Keys.
			Where(p => (typeMap[p].kind == CompletionTypeStrings.TYPE_TYPE_RESOLVERESULT)).
			Select(result => typeMap[result].type).ToList();
		
		return new HashSet<string>(typeList).ToList();
	}

	/**
		get child class list from parent class name.
	*/
	private static List<string> GetUsingTypes (TypeMap typeMap) {

		// collect point for namespace or alias-namespace.
		var namespaceList = GetNameSpaces(typeMap);

		// generate candidates for "using".
		return GetNameSpacesChildren(namespaceList);
	}

	private static List<string> GetNameSpacesChildren (List<string> namespaceList) {
		var hashSet = new HashSet<string>();
		foreach (var baseType in namespaceList) {
			var list = completionLibrary.Keys.Where(type => type.StartsWith(baseType + ".") && CharacterCountSame(baseType + ".", type, '.'));
			var keys = new HashSet<string>(list);
			hashSet.UnionWith(keys);
		}
		return hashSet.ToList();
	}

	private static List<string> GetNameSpaces (TypeMap typeMap) {
		var namespacePointList = typeMap.Keys.
			Where(p => (
				typeMap[p].kind == CompletionTypeStrings.TYPE_NAMESPACERE_SOLVERESULT | 
				typeMap[p].kind == CompletionTypeStrings.TYPE_ALIASNAMESPACE_RESOLVERESULT
			)).
			Select(result => result).ToList();

		// reduce duplication by point.
		var pointedTypeDict = new Dictionary<int, string>();
		foreach (var p in namespacePointList) {
			var row = p.row;
			if (pointedTypeDict.ContainsKey(row)) continue;
			pointedTypeDict[row] = typeMap[p].type;
		}

		return pointedTypeDict.Values.ToList();
	}

	private static bool CharacterCountSame (string a, string b, char target) {
		if (a.Split(target).Length == b.Split(target).Length) return true;
		return false;
	}


	// Send CompletionCollection
	private static bool SendCompletion (string completionIdentity, List<string> completionClassNames, CompLimitation limitation, string headStr) {
		// foreach (var className in completionClassNames) {
		// 	Debug.LogError("className "+className);
		// }

		// foreach (var key in completionLibrary.Keys) {
		// 	Debug.LogError("completionLibrary key "+key);
		// }

		var treeKeyList = completionLibrary.Keys.Intersect(completionClassNames);
		
		// Debug.LogError("treeKeyList.Count() is " + treeKeyList.Count());

		if (treeKeyList.Count() == 0) return false;


		// foreach (var key in treeKeyList) {
		// 	Debug.LogError("treeKeyList key "+ key);
		// }


		// load type limitation
		var attrLimitation = limitation.attrLimitation;
		var typeLimitation = limitation.typeLimitation;

		var headCharacterLimitation = limitation.headCharacterLimitation;
		var headCharacterInterpolationFromUsing = limitation.usingLimitation;

		List<string> usingInitializedHeadCharacterLimitations = null;
		if (headCharacterLimitation != string.Empty && headCharacterInterpolationFromUsing != null) {
			usingInitializedHeadCharacterLimitations = new List<string>();
			foreach (var usingHeader in headCharacterInterpolationFromUsing) {
				usingInitializedHeadCharacterLimitations.Add(usingHeader + headCharacterLimitation);
			}

			// add natulary starts with headCharacterLimitation.
			usingInitializedHeadCharacterLimitations.Add(headCharacterLimitation);
		}
		
		var currentTreeDicts = treeKeyList.Select(key => completionLibrary[key]).ToList();


		// generate first completion box. add candidate keywords first.
		var completionCollections = new CompletionCollection(headStr);


		foreach (var branchDict in currentTreeDicts) {
			
			foreach (var branchKey in branchDict.Keys) {
				/*
					gateway by return limitation hint

					limitation for attribute.
				*/
				switch (branchKey) {
					case CompletionDictSettings.KEY_CLASS:
						if ((attrLimitation & CompletionLimitations.RETURN_ATTR_LIMIT_CLASS) == 0) continue;
						break;
					case CompletionDictSettings.KEY_CONSTRUCTOR:
						if ((attrLimitation & CompletionLimitations.RETURN_ATTR_LIMIT_CONSTRUCTOR) == 0) continue;
						break;
					case CompletionDictSettings.KEY_METHOD:
						if ((attrLimitation & CompletionLimitations.RETURN_ATTR_LIMIT_METHOD) == 0) continue;
						break;
					case CompletionDictSettings.KEY_PROPERTY:
						if ((attrLimitation & CompletionLimitations.RETURN_ATTR_LIMIT_PROPERTY) == 0) continue;
						break;
					case CompletionDictSettings.KEY_FIELD:
						if ((attrLimitation & CompletionLimitations.RETURN_ATTR_LIMIT_FIELD) == 0) continue;
						break;
					case CompletionDictSettings.KEY_DEFAULT:
						if ((attrLimitation & CompletionLimitations.RETURN_ATTR_LIMIT_DEFAULT) == 0) continue;
						break;
				}


				// choice
				var currentHeadNames = branchDict[branchKey];

				foreach (var headName in currentHeadNames.Keys) {
					var actualHeadName = currentHeadNames[headName][CompletionDictSettings.COMPLETIONKEY_HEAD];
					
					/*
						limitation for head-character.
					*/
					if (usingInitializedHeadCharacterLimitations != null) {
						var isStartsWith = false;
						foreach (var interpolatedUsingAndHead in usingInitializedHeadCharacterLimitations) {
							if (actualHeadName.StartsWith(interpolatedUsingAndHead)) isStartsWith = true;
						}
						if (!isStartsWith) continue;

						// remove unnecessary class description 
						// from "someClassDescription.headAndBody" to "headAndBody"
						var splitted = actualHeadName.Split('.');
						actualHeadName = splitted[splitted.Length - 1];

					} else if (headCharacterLimitation != string.Empty) {
						if (!actualHeadName.StartsWith(headCharacterLimitation)) continue;
					}
					
					var returnType = currentHeadNames[headName][CompletionDictSettings.COMPLETIONKEY_RETURN];
					

					/*
						limitation for return type.
					*/

					// primitive
					if (DEFINED_PRIMITIVES.Contains(returnType)) {
						if ((typeLimitation & CompletionLimitations.RETURN_TYPE_LIMIT_PRIMITIVE) == 0) {
							continue;
						}
					} else {
						// void & not primitive
						switch (returnType) {
							case CompletionSettings.RETURN_SYSTEM_VOID:{
								if ((typeLimitation & CompletionLimitations.RETURN_TYPE_LIMIT_VOID) == 0) continue;
								break;
							}

							case CompletionSettings.RETURN_VOID:{
								if ((typeLimitation & CompletionLimitations.RETURN_TYPE_LIMIT_VOID) == 0) continue;
								break;
							}

							default:
								if ((typeLimitation & CompletionLimitations.RETURN_TYPE_LIMIT_OBJECT) == 0) continue;
								break;
						}
					}



					// 
					// all gateways passed.
					// 



					// Debug.LogError("actualHeadName "+ actualHeadName);

					// set format.
					var currentCompletionDict = GeneratetSublimeCompletionFormat(headName, returnType, actualHeadName, currentHeadNames);

					// count size
					int count = currentCompletionDict.Keys.Sum(key => currentCompletionDict[key].Length);
					if (CompletionSettings.COMPLETION_PUSH_LIMIT_SIZE < completionCollections.count + count) {
						// Debug.LogError("partial completionCollections.count "+completionCollections.count);
						Completed(completionIdentity, completionCollections, false);
						completionCollections = new CompletionCollection();
					}

					completionCollections.Add(currentCompletionDict);
					completionCollections.count = completionCollections.count + count;
		
				}
			}
		}

		Completed(completionIdentity, completionCollections, true);
		
		return true;
	}

	private static Dictionary<string, string> GeneratetSublimeCompletionFormat (string headName, string returnType, string actualHeadName, LeafDict currentHeadNames) {
		var formatSourceDict = new Dictionary<string, string>();

		formatSourceDict[CompletionDataFormats.FORMAT_LARGEHEAD] = actualHeadName;
		formatSourceDict[CompletionDataFormats.FORMAT_SMALLHEAD] = actualHeadName.Split('.').Last();
		formatSourceDict[CompletionDataFormats.FORMAT_RETURNTYPE] = returnType.Split('.').Last();

		formatSourceDict[CompletionDataFormats.FORMAT_PARAMSTYPEFMT] = currentHeadNames[headName][CompletionDictSettings.COMPLETIONKEY_PARAMTYPES];
		formatSourceDict[CompletionDataFormats.FORMAT_PARAMSTARGETFMT] = currentHeadNames[headName][CompletionDictSettings.COMPLETIONKEY_PARAMNAMES];

		return formatSourceDict;
	}


	private string GeneratetSublimeCompletionParameterFormatStr (string commadStringArray) {
		if (commadStringArray.Length == 0) return string.Empty;
		var listed = commadStringArray.Split(',');

		var formattedCollectionStr = "${" + 1 + ":"+listed[0]+"}";
		for (int i = 1; i < listed.Length; i++) {
			formattedCollectionStr += ", ${"+(i+1)+":"+listed[i]+"}";
		}

		return formattedCollectionStr;
	}

	


	/**
		receive library for completion
	*/
	public static void UpdateCompletionLibrary (TreeDict completionLibrarySource) {
		completionLibrary = completionLibrarySource;
	}

}

public class CompLimitation {
	public int typeLimitation;
	public int attrLimitation;
	public string headCharacterLimitation;
	public List<string> usingLimitation;

	public CompLimitation (int typeLimitation, int attrLimitation, string headCharacterLimitation="", List<string> usingLimitation = null) {
		this.typeLimitation = typeLimitation;
		this.attrLimitation = attrLimitation;
		this.headCharacterLimitation = headCharacterLimitation;
		this.usingLimitation = usingLimitation;
	}
}

public class CompletionCollection : List<Dictionary<string, string>> {
	public int count;
	public CompletionCollection () {
		count = 0;
	}

	public CompletionCollection (string headStr) {
		count = 0;

		if (!string.IsNullOrEmpty(headStr)) {
			var candidateKeywords = CompletionMatcher.DEFINED_KEYWORDS.Where(keyword => keyword.StartsWith(headStr))
				.ToList();

			
			foreach (var keyword in candidateKeywords) {
				var currentCompletionDict = new Dictionary<string, string>(){
					{CompletionDataFormats.FORMAT_LARGEHEAD, keyword},
					{CompletionDataFormats.FORMAT_SMALLHEAD, keyword},
					{CompletionDataFormats.FORMAT_RETURNTYPE, CompletionSettings.COMPLETION_KEYWORD},
					{CompletionDataFormats.FORMAT_PARAMSTYPEFMT, string.Empty},
					{CompletionDataFormats.FORMAT_PARAMSTARGETFMT, string.Empty}
				};
				Add(currentCompletionDict);
				count += currentCompletionDict.Keys.Sum(key => currentCompletionDict[key].Length);
			}
		}
	}
}

