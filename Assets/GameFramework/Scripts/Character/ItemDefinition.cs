using UnityEngine;

namespace GameFramework.Character
{
    /// <summary>
    /// 道具定义：当前提供最常见的“瞬时回血”基础能力。
    /// 后续可扩展成“护甲包/弹药包/临时增益”等道具。
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Character/Item Definition", fileName = "ItemDefinition")]
    public class ItemDefinition : ScriptableObject
    {
        public string itemId = "item_medkit_01";
        public string displayName = "Medkit";
        public float healAmount = 40f;
    }
}
