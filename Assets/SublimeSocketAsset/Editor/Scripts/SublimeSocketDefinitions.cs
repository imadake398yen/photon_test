/**
	definitions
*/
public static class PreferenceSetings {
	public const string PREFERENCE_ITEM_VERSION					= "version";
	public const string PREFERENCE_PARAM_VERSION				= "2.0.2";

	public const string PREFERENCE_ITEM_SERVER					= "server";
	public const string PREFERENCE_PARAM_DEFAULT_PROTOCOL		= "ws://";
	public const string PREFERENCE_PARAM_DEFAULT_HOST			= "127.0.0.1";

	public const int PREFERENCE_PARAM_DEFAULT_WSSERVER_PORT		= 8823;
	public const int PREFERENCE_PARAM_DEFAULT_BYTESERVER_PORT	= 8824;
 
	public const string PREFERENCE_ITEM_TARGET					= "target";
 
	public const string PREFERENCE_ITEM_STATUS					= "status"; 
	public const string PREFERENCE_PARAM_STATUS_NOTCONNECTED	= "notConnected";
	public const string PREFERENCE_PARAM_STATUS_CONNECTING		= "connecting";
	public const string PREFERENCE_PARAM_STATUS_CONNECTED		= "connected";
 
	public const string PREFERENCE_ITEM_ERROR					= "error";
	public const string PREFERENCE_PARAM_ERROR_NONE				= "none";
	public const string PREFERENCE_PARAM_ERROR_CONREFUSED		= "Connection refused";
	public const string PREFERENCE_PARAM_ERROR_CANNOTREAD		= "The WebSocket frame can not be read from the network stream.";
	public const string PREFERENCE_PARAM_ERROR_ABORTED			= "Thread was being aborted";
	
	public const string PREFERENCE_ITEM_AUTOCONNECT				= "autoConnect";
	public const string PREFERENCE_PARAM_AUTO_ON				= "on";
	public const string PREFERENCE_PARAM_AUTO_OFF				= "off";
		
	public const string PREFERENCE_ITEM_COMPLETION				= "completion";
	public const string PREFERENCE_PARAM_COMPLETION_ON			= "on";
	public const string PREFERENCE_PARAM_COMPLETION_OFF			= "off";
 		
	public const string PREFERENCE_ITEM_PLAYFLAG				= "playFlag";
	public const string PREFERENCE_PARAM_PLAY_ON				= "on";
	public const string PREFERENCE_PARAM_PLAY_OFF				= "off";
		
	public const string PREFERENCE_ITEM_BREAKFLAG				= "breakFlag";
	public const string PREFERENCE_PARAM_BREAK_ON				= "on";
	public const string PREFERENCE_PARAM_BREAK_OFF				= "off";

	public const string PREFERENCE_ITEM_COMPILE_BY_SAVE			= "compileBySave";
	public const string PREFERENCE_PARAM_COMPILE_BY_SAVE_ON		= "on";
	public const string PREFERENCE_PARAM_COMPILE_BY_SAVE_OFF	= "off";

	public const string PREFERENCE_ITEM_COMPILE_ANYWAY			= "compileAnyway";
	public const string PREFERENCE_PARAM_COMPILE_ANYWAY_ON		= "on";
	public const string PREFERENCE_PARAM_COMPILE_ANYWAY_OFF		= "off";
}


public static class SublimeSocketThreading {
	public const int DUMMY_MAIN_THREAD_INTERVAL		= 50;
}

public static class SublimeSocketCommunicationKeys {
	public const string SUBLIMESOCKET_SIGN			= "ss@";
	public const string SUBLIMESOCKET_CONCAT		= "->";

	public const string HEADER_COMPILED				= "compiled";
	public const string HEADER_SAVED				= "saved";
	public const string HEADER_COMPLETION			= "completion";
	public const string HEADER_SETTING				= "setting";
	public const string HEADER_WAITING				= "waiting";
	public const string HEADER_MANUALLOAD			= "manualload";

	public const string HEADER_IDENTIFICATION_VERIFY			= "VERIFIED:";
	public const string HEADER_IDENTIFICATION_VERIFY_UPDATABLE	= "VERIFIED/CLIENT_UPDATE";

	public const string HEADER_REFUSED_SSUPDATE		= "REFUSED/SUBLIMESOCKET_UPDATE";
	public const string HEADER_REFUSED_CLIENTUPDATE	= "REFUSED/CLIENT_UPDATE";
	public const string HEADER_REFUSED_SSDIFFERENT	= "REFUSED/DIFFERENT_SUBLIMESOCKET";
}

public static class CompletionSettings {
	public const int HEADHIT_MIN				= 1;
	public const int HEADHIT_LIMIT				= 1;
	public const string HEADHIT_EXCLUDE_BRACKET	= "}";

	public const string RETURN_SYSTEM_VOID		= "System.Void";
	public const string RETURN_VOID				= "Void";

	public const string ANONYMOUNS_LINE_DEFINITION = "_";
	public const string TYPE_UNKNOWN			= "?";

	public const string COMPLETION_KEYWORD		= "keyword";

	public const int COMPLETION_PUSH_LIMIT_SIZE	= 1024*20;
}

public static class CompletionMatches {
	public const string MATCH_KIND_NONE				= "";
	public const string MATCH_KIND_SPACE			= " ";
	
	public const string MATCH_KIND_DOT				= ".";
	public const string MATCH_KIND_DOT_RETRY		= ".RETRY";

	public const string MATCH_KIND_DOT_EXIT_BKT		= "}";
	public const string MATCH_KIND_DOTIGNORE		= "}.";
	public const string MATCH_KIND_NEWSPACE			= "new ";
	public const string MATCH_KIND_EQUAL			= "=";
	public const string MATCH_KIND_RETURN			= "return";

	public const string MATCH_KIND_THIS				= "this";
	public const string MATCH_KIND_THIS_RETRY		= "thisRETRY";
	
	public const string MATCH_KIND_HEAD				= "HEAD";
	public const string MATCH_KIND_ARRAY_MARK		= "[]";

}

public static class CompletionTypeStrings {
	public const string TYPE_ARRAY							= "System.Array";
	public const string TYPE_TYPE_RESOLVERESULT				= "USSA.NRefactory.Semantics.TypeResolveResult";
	public const string TYPE_NAMESPACERE_SOLVERESULT		= "USSA.NRefactory.Semantics.NamespaceResolveResult";
	public const string TYPE_ALIASNAMESPACE_RESOLVERESULT	= "USSA.NRefactory.CSharp.Resolver.AliasNamespaceResolveResult";
}

public static class CompletionDictSettings {
	// type-tree
	public const string KEY_CLASS					= "c";
	public const string KEY_CONSTRUCTOR				= "o";
	public const string KEY_METHOD					= "m";
	public const string KEY_PROPERTY				= "p";
	public const string KEY_FIELD					= "f";
	public const string KEY_DEFAULT					= "d";

	public const string COMPLETIONKEY_HEAD			= "h";
	public const string COMPLETIONKEY_RETURN		= "r";
	public const string COMPLETIONKEY_PARAMTYPES	= "t";
	public const string COMPLETIONKEY_PARAMNAMES	= "n";
}

public static class CompletionDataFormats {
	public const string FORMAT_LARGEHEAD			= "HEAD";
	public const string FORMAT_SMALLHEAD			= "head";
	public const string FORMAT_RETURNTYPE			= "return";
	public const string FORMAT_PARAMSTYPEFMT		= "paramsTypeDef";
	public const string FORMAT_PARAMSTARGETFMT		= "paramsTargetFmt";
}

public static class CompletionMessages {
	// messages
	public const string RESULTMESSAGE_ILLEGALFORMAT	= "SSA: illegal format.";
	public const string MESSAGE_COMPLETION_READY	= "SSA: completion is ready.";
	public const string MESSAGE_EDITOR_IS_PLAYING	= "SSA: Unity Editor is in play mode. completion & compilation results will show after Editor stopped .";
	public const string ASTVALIDATION_HEADER		= "SSA: ";
	public const string ASTVALIDATION_FAIL_FOOTER	= " ast validation alert(s) around line:";
	public const string ASTVALIDATION_FIXED_FOOTER	= " ast validation alert.";
}

public static class CompilationMessages {
	public const string COMPILE_WAITING				= "waiting";
	public const string COMPILED_SUCCEEDED			= "succeeded";
	public const string COMPILED_FAILED				= "failed";
}

public static class CompletionLimitations {
	// limit completion candidate by return type
	public const int RETURN_TYPE_LIMIT_NOTHING 		= 0x000000;
	public const int RETURN_TYPE_LIMIT_VOID			= 0x000001;
	public const int RETURN_TYPE_LIMIT_PRIMITIVE	= 0x000010;
	public const int RETURN_TYPE_LIMIT_OBJECT		= 0x000100;

	// limit completion candidate by attr type
	public const int RETURN_ATTR_LIMIT_NOTHING		= 0x000000;
	public const int RETURN_ATTR_LIMIT_PROPERTY		= 0x000001;
	public const int RETURN_ATTR_LIMIT_CLASS		= 0x000010;
	public const int RETURN_ATTR_LIMIT_CONSTRUCTOR	= 0x000100;
	public const int RETURN_ATTR_LIMIT_METHOD		= 0x001000;
	public const int RETURN_ATTR_LIMIT_FIELD		= 0x010000;
	public const int RETURN_ATTR_LIMIT_DEFAULT		= 0x100000;
}

public static class CompletionDLLInformations {
	public const char DELIM_GENERIC					= '`';
	public const char DELIM_OPENBRACE				= '[';
	public const string PROJECT_DLL_PATH			= "Library/ScriptAssemblies/Assembly-CSharp.dll";
	public const string SSA_EXCLUDE_PATH_EDITOR 	= "/Editor/";
	
	public const string MAC_UNITY_DLL_PATH			= "Contents/Frameworks/Managed/UnityEngine.dll";
	public const string MAC_CSHARP_DLL_PATH			= "Contents/Frameworks/Mono/lib/mono/unity/mscorlib.dll";
	
	public const string WINDOWS_UNITY_DLL_PATH		= "Editor/Data/Managed/UnityEngine.dll";
	public const string WINDOWS_CSHARP_DLL_PATH		= "Editor/Data/Mono/lib/mono/unity/mscorlib.dll";

	public const string STATIC_COMPLETION_CACHE_PATH = "Assets/SublimeSocketAsset/StaticCompletionCache.cache";
}

public static class SocketDefinitions {
	public const string MAC_LOGFILE_PATH			= "/Library/Logs/Unity/Editor.log";
	public const string WINDOWS_LOGFILE_PATH		= "\\Local\\Unity\\Editor\\Editor.log";
	public const string FILTER_SETTING_TAB			= "    ";// 4space
}

public static class SocketOSSettings {
	public const string WINDOWS_CR_CODE = "\r";
	public const string WINDOWS_ADDING_PATH_SPLITTER = "/";
}

public static class SublimeSocketAssetSettings {
	public const bool IS_NOT_TRIAL = true;
}


