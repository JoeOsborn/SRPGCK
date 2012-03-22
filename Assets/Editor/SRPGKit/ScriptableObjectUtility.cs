using UnityEngine;
using UnityEditor;
using System.IO;

public static class ScriptableObjectUtility
{
  /// <summary>
  //  This makes it easy to create, name and place unique new ScriptableObject asset files.
  /// </summary>
  public static T CreateAsset<T>(
		string tentativeName=null,
		string givenPath=null,
		bool select=true
	) where T : ScriptableObject {
    T asset = ScriptableObject.CreateInstance<T>();

    string path = givenPath ??
			AssetDatabase.GetAssetPath(Selection.activeObject);
    if (path == "")
    {
      path = "Assets";
    }
    else if (Path.GetExtension(path) != "")
    {
      path = path.Replace(Path.GetFileName(path), "");
    }

		SRPGCKEditor.EnsurePath(path);
		
    string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(
			path + "/" + (tentativeName ?? ("New " + typeof(T).ToString())) + ".asset"
		);
		Debug.Log("given "+givenPath+" actual "+path+" tent "+tentativeName+" path "+assetPathAndName);

    AssetDatabase.CreateAsset(asset, assetPathAndName);

    AssetDatabase.SaveAssets();
		if(select) {
	    EditorUtility.FocusProjectWindow();
	    Selection.activeObject = asset;
		}
		return asset;
  }
}
