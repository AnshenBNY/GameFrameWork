using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor
{
    /// <summary>
    /// 手动重新生成 .sln / .csproj，供 Cursor / VS Code 语言服务使用。
    /// </summary>
    public static class ProjectFilesRegenerator
    {
        [MenuItem("Tools/GameFramework/Regenerate Project Files")]
        public static void RegenerateProjectFiles()
        {
            CodeEditor.CurrentEditor.SyncAll();
            Debug.Log("[GameFramework] 已重新生成 C# 工程文件（.sln / .csproj）。");
        }
    }
}
