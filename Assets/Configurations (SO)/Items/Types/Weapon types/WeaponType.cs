using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Weapon Type", menuName = "Items/Weapon Type", order = 50)]
public class WeaponTypeSO : ScriptableObject
{
    [Header("Language strings")]
    [field: SerializeField] public LocalizedString nameString { get; private set; }
    [field: SerializeField] public LocalizedString descriptionString { get; private set; }

    [Header("Damage collider")]
    [field: SerializeField] public Vector3 damageColliderCenter { get; private set; }
    [field: SerializeField] public Vector3 damageColliderSize { get; private set; }

    [Header("Animations")]
    [field: SerializeField] public string drawAnimationName { get; private set; }
    [field: SerializeField] public string sheathAnimationName { get; private set; }
}
