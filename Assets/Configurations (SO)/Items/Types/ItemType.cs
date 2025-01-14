using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Item Type", menuName = "Items/Item Type", order = 50)]
public class ItemTypeSO : ScriptableObject
{
    [Header("Language strings")]
    [field: SerializeField] public LocalizedString nameString { get; private set; }
    [field: SerializeField] public LocalizedString descriptionString { get; private set; }
}
