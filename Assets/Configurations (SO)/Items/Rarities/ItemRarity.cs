using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Item Rarity", menuName = "Items/Item Rarity", order = 51)]
public class ItemRaritySO : ScriptableObject
{
    [field: SerializeField] public LocalizedString nameString { get; private set; }
    [field: SerializeField] public Sprite image { get; private set; }
    [field: SerializeField] public Color color { get; private set; }

    [Header("Spawn chances")]
    [field: SerializeField] [field: Range(0f, 1f)] public float worldSpawnChance { get; private set; } // Шанс спавна в мире (через спавнер)
    [field: SerializeField] [field: Range(0f, 1f)] public float stashSpawnChance { get; private set; } // Шанс спавна в контейнере
}
