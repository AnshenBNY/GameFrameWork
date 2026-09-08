using GameFramework.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.TPS.UI
{
    /// <summary>
    /// UIRoot 自动装配器：
    /// - 确保存在 PickupHUD + GameplayHintUI
    /// - 确保存在 ScreenHUD + WeaponUIManager
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIManager))]
    public class UIRootInstaller : MonoBehaviour
    {
        [SerializeField] private bool autoCreateIfMissing = true;

        private void Awake()
        {
            if (autoCreateIfMissing)
            {
                EnsurePickupHud();
                EnsureGameplayHintHud();
                EnsureScreenHud();
            }

            RegisterPickupHudWithContext();
        }

        private void RegisterPickupHudWithContext()
        {
            Transform pickup = transform.Find("PickupHUD");
            if (pickup != null)
            {
                RuntimeContext.RegisterPickupHud(pickup);
            }
        }

        private void EnsurePickupHud()
        {
            Transform pickup = transform.Find("PickupHUD");
            if (pickup == null)
            {
                GameObject pickupGo = new GameObject("PickupHUD");
                pickupGo.transform.SetParent(transform, false);
                pickup = pickupGo.transform;
            }

            Transform label = pickup.Find("Label");
            if (label == null)
            {
                GameObject labelGo = new GameObject("Label");
                labelGo.transform.SetParent(pickup, false);
                Text text = labelGo.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.text = "Pickup";
                text.alignment = TextAnchor.MiddleCenter;
            }

            if (pickup.GetComponent<GameplayHintUI>() == null)
            {
                // PickupHUD 由 InteractiveWeapon 直接控制显示，不挂 GameplayHintUI。
            }
        }

        private void EnsureGameplayHintHud()
        {
            Transform hint = transform.Find("GameplayHintHUD");
            if (hint == null)
            {
                GameObject hintGo = new GameObject("GameplayHintHUD");
                hintGo.transform.SetParent(transform, false);
                hint = hintGo.transform;
            }

            Transform label = hint.Find("Label");
            if (label == null)
            {
                GameObject labelGo = new GameObject("Label");
                labelGo.transform.SetParent(hint, false);
                Text text = labelGo.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.text = string.Empty;
                text.alignment = TextAnchor.MiddleCenter;
            }

            if (hint.GetComponent<GameplayHintUI>() == null)
            {
                hint.gameObject.AddComponent<GameplayHintUI>();
            }
        }

        private void EnsureScreenHud()
        {
            Transform screenHud = transform.Find("ScreenHUD");
            if (screenHud == null)
            {
                GameObject screenGo = new GameObject("ScreenHUD");
                screenGo.transform.SetParent(transform, false);
                screenHud = screenGo.transform;
            }

            EnsureWeaponHudLayout(screenHud);

            if (screenHud.GetComponent<WeaponUIManager>() == null)
            {
                screenHud.gameObject.AddComponent<WeaponUIManager>();
            }
        }

        private static void EnsureWeaponHudLayout(Transform screenHud)
        {
            Transform weaponHud = screenHud.Find("WeaponHUD");
            if (weaponHud == null)
            {
                weaponHud = new GameObject("WeaponHUD").transform;
                weaponHud.SetParent(screenHud, false);
            }

            Transform weapon = weaponHud.Find("Weapon");
            if (weapon == null)
            {
                weapon = new GameObject("Weapon").transform;
                weapon.SetParent(weaponHud, false);
                Image weaponImage = weapon.gameObject.AddComponent<Image>();
                weaponImage.raycastTarget = false;
            }

            Transform data = weaponHud.Find("Data");
            if (data == null)
            {
                data = new GameObject("Data").transform;
                data.SetParent(weaponHud, false);
            }

            Transform mag = data.Find("Mag");
            if (mag == null)
            {
                mag = new GameObject("Mag").transform;
                mag.SetParent(data, false);
            }

            if (mag.childCount == 0)
            {
                for (int i = 0; i < 32; i++)
                {
                    GameObject bullet = new GameObject($"Bullet_{i}");
                    bullet.transform.SetParent(mag, false);
                    Image bulletImage = bullet.AddComponent<Image>();
                    bulletImage.raycastTarget = false;
                }
            }

            Transform label = data.Find("Label");
            if (label == null)
            {
                label = new GameObject("Label").transform;
                label.SetParent(data, false);
                Text text = label.gameObject.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.text = "0/0";
                text.alignment = TextAnchor.MiddleLeft;
            }
        }
    }
}
