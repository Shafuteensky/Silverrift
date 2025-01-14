using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Usable Item", menuName = "Items/Usable", order = 20)]
public class UsableItemSO : BasicItemSO
{
    [field: Header("================ Usable Item ==================================")]

    [field: Header("Stats")]
    [field: SerializeField] public WeaponTypeSO weaponType { get; private set; }

    [field: Header("Usable item settings")]
    [field: SerializeField] [Range(0, 1000)] public int damage { get; private set; } = 10;
    [field: SerializeField][Range(0, 1000)] public DamageType damageType { get; private set; } = 0;
    // Досягаемость ближнего боя или дальность полета снаряда дальнего боя
    [field: SerializeField] [Range(0f, 100f)] public float range { get; private set; } = 1; 
    [field: SerializeField] [Range(0, 100)] public int staminaConsumption { get; private set; } = 5;
    [field: SerializeField] [Range(0f, 10f)] public float timeBetweenAtacks { get; private set; } = 0.7f;

    [field: Header("Ranged settings")]
    public bool isRanged { get; private set; } = false;
    [field: SerializeField] [Range(1f, 200f)] public float projectileVelocity { get; private set; } = 100;
    [field: SerializeField] [Range(20, 80)] public int projectileWeight { get; private set; } = 40; // Вес в граммах
    [field: SerializeField] public BasicItemSO ammoItem { get; private set; }

    [field: Header("Throw settings")]
    [field: SerializeField] public bool isThrowable { get; private set; } = false;
    [field: SerializeField] public bool isDisappearsOnThrow { get; private set; } = false;
    [field: SerializeField] public bool isReturnsAfterThrow { get; private set; } = false;
    [field: SerializeField] [Range(1, 10)] public int returnsAfterTime { get; private set; } = 5;
}
