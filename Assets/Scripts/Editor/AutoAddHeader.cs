// 模板路径: /Contents/Resources/ScriptTemplates
// 把以下文件添加到头文件中
/*
** Author      : #Author#
** CreateDate  : #CreateDate#
** Description : 
*/


using System;
using System.IO;
using UnityEditor;

public class AutoAddHeader : UnityEditor.AssetModificationProcessor
{
    private static void OnWillCreateAsset(string path)
    {
        path = path.Replace(".meta", "");

        if (path.EndsWith(".cs"))
        {
            string content = File.ReadAllText(path);
            content = content.Replace("#Author#", "Yogi")
                //.Replace("#CreateDate#", "")
                .Replace("#CreateDate#", DateTime.Now.ToString("yyyy-mm-dd HH:mm:ss"));
            File.WriteAllText(path, content);
            AssetDatabase.Refresh();
        }
    }
}
