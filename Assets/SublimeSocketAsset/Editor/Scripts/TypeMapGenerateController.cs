using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using USSA.NRefactory;
using USSA.NRefactory.CSharp;
using USSA.NRefactory.CSharp.Resolver;
using USSA.NRefactory.Semantics;
using USSA.NRefactory.TypeSystem;
using USSA.NRefactory.TypeSystem.Implementation;

using USSAUniRx;

/**
	Corllect complition datas from hints.
*/
class TypeMapGenerateController {

	public enum MatcherState {
		GENERATOR_INITIALIZING,
		GENERATOR_READY,
		GENERATOR_GENERATING
	};

	MatcherState mapGeneratorState;

	private string generatedTypeMapIdentity;


	// action for push.
	private Action<string, TypeMap, string, Point> TypeMapped;

	// type map for get type from position.
	public TypeMap cachedTypeMap;

	
	private List<IUnresolvedAssembly> standardAssemlbeLibs;

	// completion resource for NRefactory
	private CSharpProjectContent cSharpProject;
	private CSharpParser parser = new CSharpParser();



	readonly List<string>allLibAddresses;

	public void ResetIdentity () {
		generatedTypeMapIdentity = string.Empty;
	}

	public TypeMapGenerateController (
		Action<string, TypeMap, string, Point> TypeMapped,
		string mainLibAddress,
		List<string>libAddresses
	) {
		this.TypeMapped = TypeMapped;
		
		allLibAddresses = new List<string>(libAddresses);
		if (SublimeSocketAssetSettings.IS_NOT_TRIAL) allLibAddresses.Add(mainLibAddress);
		
		// init map
		cachedTypeMap = new TypeMap();

		// set state
		mapGeneratorState = MatcherState.GENERATOR_INITIALIZING;
	}

	public IObservable<string> ReadyTypeFoundation() {
		return Observable.FromCoroutine<string>(observer => ReadyTypeFoundationObs(observer));
	}

	public IEnumerator ReadyTypeFoundationObs (IObserver<string> observer) {
		SyncReadyTypeFoundation();
		observer.OnNext("ReadyTypeFoundationObs over");
		observer.OnCompleted();
		yield break;
	}


	public void SyncReadyTypeFoundation () {
		InitTypeFoundation(this.allLibAddresses);
		mapGeneratorState = MatcherState.GENERATOR_READY;
	}


	/**
		return typeMap for completion.
		parameters are for closure of generate TypeMap block.
	*/
	public TypeMap TypeMapUpdate (string identity, string path, string body, Point point) {
		// already generated
		if (generatedTypeMapIdentity == identity) {
			// return current cached typeMap.
			return cachedTypeMap;
		}

		// generate new map or generating old body.
		switch (mapGeneratorState) {
			case MatcherState.GENERATOR_INITIALIZING:
			case MatcherState.GENERATOR_GENERATING:
			default:
				// Do not create new TypeMap. only return cached one.
				return cachedTypeMap;

			case MatcherState.GENERATOR_READY: {
				// generate new typeMap.
				MainThreadDispatcher.Post(() => {
					try {
						mapGeneratorState = MatcherState.GENERATOR_GENERATING;

						// update type map.
						cachedTypeMap = GenerateCompletionMap(path, body, point);

						// update identity.
						generatedTypeMapIdentity = identity;

						// change state for next or already stacked completion.
						mapGeneratorState = MatcherState.GENERATOR_READY;
						
						// push completionController. then return completion data or create new (latest) completion.
						TypeMapped(identity, cachedTypeMap, body, point);
					} catch (Exception e) {
						Debug.LogWarning("SSA:TypeMapUpdate error "+e);
					}
				});
				return cachedTypeMap;
			}
		}

	}

	/**
		generate TypeMap in other thread.
	*/
	private TypeMap GenerateCompletionMap (string path, string code, Point point) {
		// generate new TypeMap.
		var currentMap = new TypeMap();
		
		try {
			var rootAst = parser.Parse(code, path);
			
			var unresolvedFile = rootAst.ToTypeSystem();
			
			// update project for partial completion.
			cSharpProject = (CSharpProjectContent)cSharpProject.AddOrUpdateFiles(unresolvedFile);
			ICompilation compilation = cSharpProject.CreateCompilation();

			CSharpAstResolver resolver = new CSharpAstResolver(compilation, rootAst, unresolvedFile);
			
			currentMap.errorCount = rootAst.Errors.Count;
			TypeTraverse(resolver, rootAst.Children, ref currentMap);
		}catch (Exception e) {
			Debug.LogWarning("SSA:GenerateCompletionMap error:"+e);
		}

		return currentMap;
	}

	/**
		collect types from node.
	*/
	void TypeTraverse (CSharpAstResolver resolver, IEnumerable<AstNode> source, ref TypeMap map) {
		foreach (var node in source) {
			
			// type resolve
			try {
				var result = resolver.Resolve(node);
				
				if (result.IsError) {
					// Point p = new Point(node.Region.EndLine, node.Region.EndColumn);
					// Debug.LogError("error kindStr "+result.GetType() + " p2.row "+p2.row + " p2.col" + p2.col);
					// map[p] = new CompletionInfo("error", "error", node.Region.BeginLine, node.Region.BeginColumn, node.Region.EndLine, node.Region.EndColumn, result.ToString());
					// LogWriter.Log("result is error:" + node.Region);
				}
				else {
					var kindStr = "";
					try {
						kindStr = result.GetType().ToString();
					} catch (Exception e) {
						Debug.LogWarning("SSA:TypeTraverse type ToString error:"+e);
					}

					if (result.GetType() == typeof(NamespaceResolveResult)) {
						Point p = new Point(node.Region.EndLine, node.Region.EndColumn);
						var typeStr = ((NamespaceResolveResult)result).NamespaceName;
						map[p] = new CompletionInfo(typeStr, kindStr, node.Region.BeginLine, node.Region.BeginColumn, node.Region.EndLine, node.Region.EndColumn, result.ToString());
					}
					else if (result.GetType() == typeof(AliasNamespaceResolveResult)) {
						Point p = new Point(node.Region.EndLine, node.Region.EndColumn);
						var typeStr = ((AliasNamespaceResolveResult)result).NamespaceName;
						map[p] = new CompletionInfo(typeStr, kindStr, node.Region.BeginLine, node.Region.BeginColumn, node.Region.EndLine, node.Region.EndColumn, result.ToString());
					}
					else if (result.GetType() == typeof(CSharpInvocationResolveResult)) {
						Point p = new Point(node.Region.EndLine, node.Region.EndColumn);
						var member = ((CSharpInvocationResolveResult)result).Member;
						var typeStr = member.ReturnType.FullName;
						
						map[p] = new CompletionInfo(typeStr, kindStr, node.Region.BeginLine, node.Region.BeginColumn, node.Region.EndLine, node.Region.EndColumn, result.ToString());
					}
					else if (result.GetType() == typeof(LocalResolveResult)) {
						Point p = new Point(node.Region.EndLine, node.Region.EndColumn);
						var typeStr = ((LocalResolveResult)result).Variable.Type.ToString();
						map[p] = new CompletionInfo(typeStr, kindStr, node.Region.BeginLine, node.Region.BeginColumn, node.Region.EndLine, node.Region.EndColumn, result.ToString());
					}
					else if (result.GetType() ==  typeof(OperatorResolveResult)) {
						Point p = new Point(node.Region.EndLine, node.Region.EndColumn);
						var typeStr = ((OperatorResolveResult)result).Type.ToString();
						
						map[p] = new CompletionInfo(typeStr, kindStr, node.Region.BeginLine, node.Region.BeginColumn, node.Region.EndLine, node.Region.EndColumn, result.ToString());
					}
					else if (result.GetType() ==  typeof(ThisResolveResult)) {
						Point p = new Point(node.Region.EndLine, node.Region.EndColumn);
						var typeStr = ((ThisResolveResult)result).Type.ToString();
						map[p] = new CompletionInfo(typeStr, kindStr, node.Region.BeginLine, node.Region.BeginColumn, node.Region.EndLine, node.Region.EndColumn, result.ToString());
					}
					else if (result.GetType() ==  typeof(TypeResolveResult)) {
						Point p = new Point(node.Region.EndLine, node.Region.EndColumn);
						var typeStr = ((TypeResolveResult)result).Type.ToString();
						map[p] = new CompletionInfo(typeStr, kindStr, node.Region.BeginLine, node.Region.BeginColumn, node.Region.EndLine, node.Region.EndColumn, result.ToString());
					}
					else if (result.GetType() == typeof(ConstantResolveResult)) {
						Point p = new Point(node.Region.EndLine, node.Region.EndColumn);
						var typeStr = ((ConstantResolveResult)result).Type.ToString();
						map[p] = new CompletionInfo(typeStr, kindStr, node.Region.BeginLine, node.Region.BeginColumn, node.Region.EndLine, node.Region.EndColumn, result.ToString());
					}
					else if (result.GetType() ==  typeof(MemberResolveResult)) {
						Point p = new Point(node.Region.EndLine, node.Region.EndColumn);
						var member = ((MemberResolveResult)result).Member;
						var typeStr = member.ReturnType.FullName;
						
						map[p] = new CompletionInfo(typeStr, kindStr, node.Region.BeginLine, node.Region.BeginColumn, node.Region.EndLine, node.Region.EndColumn, result.ToString());
					}
					else if (result.GetType() == typeof(ResolveResult)) {
						Point p = new Point(node.Region.EndLine, node.Region.EndColumn);

						// typeinfo always System.Void with writing line.
						// define line definition as mark. depends on kinds.
						var typeStr = CompletionSettings.ANONYMOUNS_LINE_DEFINITION;

						map[p] = new CompletionInfo(typeStr, kindStr, node.Region.BeginLine, node.Region.BeginColumn, node.Region.EndLine, node.Region.EndColumn, result.ToString());
					}

					// else {
						// if (result.GetType() != typeof(MethodGroupResolveResult) ){

							// ArrayAccessResolveResult
							// ForEachResolveResult
							// ResolveVisitor
							// ConstantResolveResult

							// Point p2 = new Point(node.Region.EndLine, node.Region.EndColumn);
							// Debug.LogError("kindStr "+result.GetType() + " p2.row "+p2.row + " p2.col" + p2.col);
						
						// }
					// }
				}
			} catch (Exception e) {
				Debug.LogWarning("SSA:TypeTraverse error:" + e);
			}

			if (node.Children != null) {
				TypeTraverse(resolver, node.Children, ref map);
			}
		}
	}
	


	/**
		initialize Type Detection System.
	*/
	private void InitTypeFoundation (List<string> libAddresses) {
		cSharpProject = new CSharpProjectContent();
		standardAssemlbeLibs = new List<IUnresolvedAssembly>();

		AssemblyLoader loader = AssemblyLoader.Create();
		foreach (var path in libAddresses) {
			try {
				var assembly = loader.LoadAssemblyFile(path);
				standardAssemlbeLibs.Add(assembly);
			} catch (Exception e) {
				Debug.LogWarning("SSA failed loading the assembly from "+path + ", this assembly was skipped. error:" + e);
			}
		}

		cSharpProject = (CSharpProjectContent)cSharpProject.AddAssemblyReferences(standardAssemlbeLibs.ToArray());
		cSharpProject.CreateCompilation();
	}
	
	static public void Assert(bool condition, string message) {
		if (!condition) throw new Exception(message);
	}

}


class TypeMap : Dictionary<Point, CompletionInfo> {
	public int errorCount;
}

public class Point {
	public int row, col;
	public Point (int row, int col) {
		this.row = row;
		this.col = col;
	}
}

public class CompletionInfo {
	public Point start;
	public Point end;
	public string type;
	public string kind;
	public string hint;

	public CompletionInfo (string type, string kind, int startRow, int startCol, int endRow, int endCol, string hint) {
		this.type = type;
		this.kind = kind;
		this.start = new Point(startRow, startCol);
		this.end = new Point(endRow, endCol);
		this.hint = hint;
	}
}




