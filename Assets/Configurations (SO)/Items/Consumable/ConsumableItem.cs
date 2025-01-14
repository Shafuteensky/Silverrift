using UnityEngine;

[CreateAssetMenu(fileName = "Consumable Item", menuName = "Items/Consumable", order = 40)]
public class ConsumableItemSO : BasicItemSO
{
    [Header("Application time")]
    [field: SerializeField] public bool isEffectOverTime { get; private set; } = false;
    [field: SerializeField] [Range(1, 60)] public int effectTime { get; private set; } = 1;

    [Header("Application effect")]
    [field: SerializeField] [Range(-500, 500)] public int healAmount { get; private set; } = 25;
    [field: SerializeField] [Range(-500, 500)] public int staminaRegainAmount { get; private set; } = 0;
    [field: SerializeField] [Range(-100, 100)] public int thirstRecovery { get; private set; } = 0;
    [field: SerializeField] [Range(-100, 100)] public int hungerRecovery { get; private set; } = 0;
    [field: SerializeField] [Range(-50, 50)] public int temperatureBonus { get; private set; } = 0;
    [field: SerializeField] [Range(-0.5f, 2)] public float aberrationResistBonus { get; private set; } = 0;
    [field: SerializeField] public BonusEffectsScriptableObject bonusEffects { get; private set; }

    public void Use()
    {

    }
}
