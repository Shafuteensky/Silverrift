using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Equipable Item Set", menuName = "Items/Equipable Item Set", order = 52)]
public class EquipmentSetSO : ScriptableObject
{
    [field: SerializeField] public LocalizedString nameString { get; private set; }
    [field: SerializeField] public BonusEffectsScriptableObject bonusEffects { get; private set; }
}
