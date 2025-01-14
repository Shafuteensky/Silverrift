using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item Spawner Configuration", menuName = "Spawner configuration/Item configuration", order = 60)]
public class ItemSpawnerConfigurationSO : ScriptableObject
{
    [SerializeField] public List<BasicItemSO> itemsSpawnList = new List<BasicItemSO>(); // Список объектов спавна
    [Tooltip("Use only Spawn Chance Factor (without rarity chance)")]
    [SerializeField] public bool overrideRarityChance = false; // Шанс спавна НЕ из редкости, а из spawnChanceFactor для всех предметов
    [SerializeField] [Range(0.01f, 20f)] public float spawnChanceFactor = 1f; // Множитель шанса спавна для всех предметов
    [Tooltip("If not – full stack spawned")]
    [SerializeField] public bool randomAmount = false;
}
