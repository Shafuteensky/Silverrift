using UnityEngine;

[CreateAssetMenu(fileName = "Equipable Item", menuName = "Items/Equipable", order = 30)]
public class EquipableItemSO : BasicItemSO
{
    [Header("Application time")]
    //public ????? equipSlot { get; private set; } // Голова, тело, ноги, артефакт/фонарь, кольцо, рюкзак
    //public ArmorConfig armorConfig { get; private set; } // Тип брони, % защиты, подвижность: (?) легкая, средняя, тяжелая
    [field: SerializeField] [Range(0, 30)] public int inventorySlots { get; private set; } = 0;
    [field: SerializeField] [Range(-50, 50)] public int temperatureBonus { get; private set; } = 0;
    [field: SerializeField] [Range(-0.5f, 2)] public float aberrationResistBonus { get; private set; } = 0;
    [field: SerializeField] public BonusEffectsScriptableObject bonusEffects { get; private set; } // Бонус этой части экипировки самой по себе
    [field: SerializeField] public EquipmentSetSO equipmentSet { get; private set; } // Бонус, если все части набора экипировки надеты вместе
}
