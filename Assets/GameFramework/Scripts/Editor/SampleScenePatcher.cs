using GameFramework.AI;
using GameFramework.Character;
using GameFramework.Weapon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameFramework.Editor
{
    /// <summary>
    /// 修补已有 SampleScene 的控制与相机配置（适配 Player 预制体 + PlayerCamera）。
    /// </summary>
    public static class SampleScenePatcher
    {
        private const string SampleScenePath = "Assets/GameFramework/Generated/SampleScene.unity";
        private const string PlayerPrefabPath = "Assets/GameFramework/Generated/Player.prefab";

        [MenuItem("Tools/GameFramework/Patch Sample Scene Controls")]
        public static void PatchSampleScene()
        {
            if (!System.IO.File.Exists(SampleScenePath))
            {
                EditorUtility.DisplayDialog("GameFramework", "未找到 SampleScene，请先执行 Build Minimal Playable Sample。", "OK");
                return;
            }

            var scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);

            CombatLayerSetup.EnsureProjectLayers();
            CombatLayerSetup.ApplySampleSceneLayers();

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                EditorUtility.DisplayDialog("GameFramework", "场景中未找到 Player 对象。", "OK");
                return;
            }

            Transform pivot = player.transform.Find("CameraPivot");
            if (pivot == null)
            {
                GameObject pivotGo = new GameObject("CameraPivot");
                pivotGo.transform.SetParent(player.transform, false);
                pivotGo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                pivot = pivotGo.transform;
            }

            Transform characterRoot = player.transform.Find("Root");
            if (characterRoot == null)
            {
                EditorUtility.DisplayDialog("GameFramework", "Player 下未找到 Root 节点，请检查预制体结构。", "OK");
                return;
            }

            ThirdPersonCameraController cameraController = Object.FindObjectOfType<ThirdPersonCameraController>();
            if (cameraController == null)
            {
                EditorUtility.DisplayDialog("GameFramework", "场景中未找到 PlayerCamera 上的 ThirdPersonCameraController。", "OK");
                return;
            }

            SimpleFollowCamera oldFollow = cameraController.GetComponent<SimpleFollowCamera>();
            if (oldFollow != null)
            {
                Object.DestroyImmediate(oldFollow);
            }

            cameraController.SetTarget(player.transform, pivot);

            ThirdPersonCharacterController controller = player.GetComponent<ThirdPersonCharacterController>();
            if (controller != null)
            {
                controller.SetCameraController(cameraController);
                controller.SetCharacterRoot(characterRoot);
            }

            WeaponRuntime weaponRuntime = player.GetComponent<WeaponRuntime>();
            if (weaponRuntime != null)
            {
                if (player.GetComponent<WeaponShotVisualizer>() == null)
                {
                    player.AddComponent<WeaponShotVisualizer>();
                }

                SerializedObject weaponSo = new SerializedObject(weaponRuntime);
                weaponSo.FindProperty("useCombatLayerDefaults").boolValue = true;
                weaponSo.ApplyModifiedPropertiesWithoutUndo();
            }

            ThirdPersonCameraController cameraControllerForMask = cameraController;
            if (cameraControllerForMask != null)
            {
                SerializedObject cameraSo = new SerializedObject(cameraControllerForMask);
                cameraSo.FindProperty("useCombatLayerDefaults").boolValue = true;
                cameraSo.ApplyModifiedPropertiesWithoutUndo();
            }

            PlayerCombatHud hud = Object.FindObjectOfType<PlayerCombatHud>();
            if (hud != null)
            {
                EditorUtility.SetDirty(hud);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("GameFramework", "SampleScene 已按 Player 预制体结构完成修补。", "OK");
        }
    }
}
