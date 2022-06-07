/*
 * Unity Menu:  Editor/Preferences
 * Set:   Preferences.AutoRefresh = false
 */

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ShortCuts
{
    //[MenuItem("Tools/Clear Console %#x")]
    [MenuItem("Tools/Clear Console %q")]
    public static void ClearConsole()
    {
        Assembly assembly = Assembly.GetAssembly(typeof(ActiveEditorTracker));
        Type type = assembly.GetType("UnityEditorInternal.LogEntries");
        if (type == null)
        {
            type = assembly.GetType("UnityEditor.LogEntries");
        }
        MethodInfo method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }
    [MenuItem("Tools/Clear Console And Refresh %r")]
    public static void ClearConsoleAndRefresh() 
    {
        ClearConsole();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/SaveToAsset %#s")]
    public static void SaveToAsset()
    {
        Save();
    }

    /// <summary>
    /// 获取预制体资源路径。
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    public static string GetPrefabAssetPath(GameObject gameObject)
    {
#if UNITY_EDITOR
        // Project中的Prefab是Asset不是Instance
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            // 预制体资源就是自身
            return UnityEditor.AssetDatabase.GetAssetPath(gameObject);
        }

        // Scene中的Prefab Instance是Instance不是Asset
        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject))
        {
            // 获取预制体资源
            var prefabAsset = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
            return UnityEditor.AssetDatabase.GetAssetPath(prefabAsset);
        }

        // PrefabMode中的GameObject既不是Instance也不是Asset
        var prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject);
        if (prefabStage != null)
        {
            // 预制体资源：prefabAsset = prefabStage.prefabContentsRoot
            return prefabStage.prefabAssetPath;
        }
#endif

        // 不是预制体
        return null;
    }

    public static void Save()
    {
        if (Selection.activeGameObject != null)
        {
            var go = Selection.activeGameObject;

            var sorceGO = PrefabUtility.GetCorrespondingObjectFromSource(go);

            var path = AssetDatabase.GetAssetPath(sorceGO).ToLower();
            //var path = GetPrefabAssetPath(sorceGO);
            //PrefabUtility.SaveAsPrefabAssetAndConnect(go, path)
            PrefabUtility.SaveAsPrefabAsset(go, path,out var isSuccess);
            //PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction, out var isSuccess);
            if (isSuccess)
            {
                Debug.Log($"保存物体{go.gameObject}成功{path}", go);
            }
            else
            {
                Debug.Log($"保存物体{go.gameObject}失败{path}", go);
            }
        }
    }

    public static void Save2()
    {
        GameObject source = PrefabUtility.GetPrefabParent(Selection.activeGameObject) as GameObject;
        if (source == null) return;
        string prefabPath = AssetDatabase.GetAssetPath(source).ToLower();
        if (prefabPath.EndsWith(".prefab") == false) return;
        PrefabUtility.ReplacePrefab(Selection.activeGameObject, source, ReplacePrefabOptions.ConnectToPrefab | ReplacePrefabOptions.ReplaceNameBased);
    }
}
