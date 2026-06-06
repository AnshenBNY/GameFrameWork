using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor
{
    /// <summary>
    /// 将当前项目实际使用的 TPS Bundle 脚本迁移到 GameFramework/Scripts/TPS 下。
    /// 使用 AssetDatabase.MoveAsset 迁移，保持 .meta 与 GUID，不破坏场景/预制体引用。
    /// </summary>
    public static class TpsBundleScriptOrganizer
    {
        [MenuItem("Tools/GameFramework/Organize TPS Bundle Scripts")]
        public static void Organize()
        {
            // 仅迁移当前运行模板依赖的核心脚本；演示脚本可按需自行扩展。
            Dictionary<string, string> moveMap = new Dictionary<string, string>
            {
                // Player
                { "Assets/GameFramework/TPS Bundle/Cover+Shoot/Scripts/PlayerScripts/BasicBehaviour.cs", "Assets/GameFramework/Scripts/TPS/Player/BasicBehaviour.cs" },
                { "Assets/GameFramework/TPS Bundle/Cover+Shoot/Scripts/PlayerScripts/MoveBehaviour.cs", "Assets/GameFramework/Scripts/TPS/Player/MoveBehaviour.cs" },
                { "Assets/GameFramework/TPS Bundle/Cover+Shoot/Scripts/PlayerScripts/AimBehaviour.cs", "Assets/GameFramework/Scripts/TPS/Player/AimBehaviour.cs" },
                { "Assets/GameFramework/TPS Bundle/Cover+Shoot/Scripts/PlayerScripts/ShootBehaviour.cs", "Assets/GameFramework/Scripts/TPS/Player/ShootBehaviour.cs" },
                { "Assets/GameFramework/TPS Bundle/Cover+Shoot/Scripts/PlayerScripts/CoverBehaviour.cs", "Assets/GameFramework/Scripts/TPS/Player/CoverBehaviour.cs" },
                { "Assets/GameFramework/TPS Bundle/Cover+Shoot/Scripts/PlayerScripts/Demo Scene/Footsteps.cs", "Assets/GameFramework/Scripts/TPS/Player/Footsteps.cs" },

                // Camera
                { "Assets/GameFramework/TPS Bundle/Cover+Shoot/Scripts/LevelScripts/ThirdPersonOrbitCam.cs", "Assets/GameFramework/Scripts/TPS/Camera/ThirdPersonOrbitCam.cs" },

                // Weapon / Combat Runtime
                { "Assets/GameFramework/TPS Bundle/Cover+Shoot/Scripts/LevelScripts/InteractiveWeapon.cs", "Assets/GameFramework/Scripts/TPS/Weapon/InteractiveWeapon.cs" },

                // UI
                { "Assets/GameFramework/TPS Bundle/Cover+Shoot/Scripts/LevelScripts/WeaponUIManager.cs", "Assets/GameFramework/Scripts/TPS/UI/WeaponUIManager.cs" },

                // Demo / 教学脚本
                { "Assets/GameFramework/TPS Bundle/Cover+Shoot/Scripts/LevelScripts/Demo Scene/HintManagement.cs", "Assets/GameFramework/Scripts/TPS/Demo/HintManagement.cs" },
                { "Assets/GameFramework/TPS Bundle/Cover+Shoot/Scripts/LevelScripts/Demo Scene/TimeTrialManager.cs", "Assets/GameFramework/Scripts/TPS/Demo/TimeTrialManager.cs" },

                // 通用辅助脚本
                { "Assets/GameFramework/TPS Bundle/Cover+Shoot/Scripts/LevelScripts/ParticleSystemAutoDestroy.cs", "Assets/GameFramework/Scripts/TPS/FX/ParticleSystemAutoDestroy.cs" }
            };

            int moved = 0;
            List<string> errors = new List<string>();
            foreach (KeyValuePair<string, string> kv in moveMap)
            {
                string source = kv.Key;
                string target = kv.Value;

                if (!AssetExists(source))
                {
                    continue;
                }

                EnsureParentFolders(target);
                string result = AssetDatabase.MoveAsset(source, target);
                if (string.IsNullOrEmpty(result))
                {
                    moved++;
                    continue;
                }

                errors.Add($"{source} -> {target} | {result}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (errors.Count > 0)
            {
                Debug.LogError("TPS 脚本整理完成，但存在迁移失败项：\n" + string.Join("\n", errors));
                EditorUtility.DisplayDialog("GameFramework", $"TPS 脚本整理完成：成功 {moved}，失败 {errors.Count}。\n请查看 Console。", "OK");
                return;
            }

            EditorUtility.DisplayDialog("GameFramework", $"TPS 脚本整理完成：共迁移 {moved} 个脚本。", "OK");
        }

        private static bool AssetExists(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Object>(path) != null;
        }

        private static void EnsureParentFolders(string assetPath)
        {
            string[] parts = assetPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length - 1; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
