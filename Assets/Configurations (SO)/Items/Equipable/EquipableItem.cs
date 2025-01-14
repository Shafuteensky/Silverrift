using UnityEngine;

[CreateAssetMenu(fileName = "Equipable Item", menuName = "Items/Equipable", order = 30)]
public class EquipableItemSO : BasicItemSO
{
    [Header("Application time")]
    //public ????? equipSlot { get; private set; } // ������, ����, ����, ��������/������, ������, ������
    //public ArmorConfig armorConfig { get; private set; } // ��� �����, % ������, �����������: (?) ������, �������, �������
    [field: SerializeField] [Range(0, 30)] public int inventorySlots { get; private set; } = 0;
    [field: SerializeField] [Range(-50, 50)] public int temperatureBonus { get; private set; } = 0;
    [field: SerializeField] [Range(-0.5f, 2)] public float aberrationResistBonus { get; private set; } = 0;
    [field: SerializeField] public BonusEffectsScriptableObject bonusEffects { get; private set; } // ����� ���� ����� ���������� ����� �� ����
    [field: SerializeField] public EquipmentSetSO equipmentSet { get; private set; } // �����, ���� ��� ����� ������ ���������� ������ ������
}
