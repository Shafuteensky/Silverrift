using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Container configuration", menuName = "Items/Container config", order = 55)]
public class ItemContainerConfigSO : ScriptableObject
{
    [field: SerializeField] public LocalizedString nameString { get; private set; }
    [field: SerializeField] [field: Range(1, 100)] public int numberOfSlots { get; private set; } = 5;

    // ≈сли посто€нный Ц остаетс€ в пам€ти и сохран€етс€ (дл€ инвентар€ игрока и его хранилищ)
    [Tooltip("If permanent Ц contants saved at all times (player inventory and stashes, traders); not Ц deleted with scene")]
    [field: SerializeField] public bool isPermanent { get; private set; } = false;

    // ”ничтожаетс€ ли менеджер конейнера и сам конейнер если контейнер опустел
    [field: SerializeField] public bool destroyIfEmpty { get; private set; } = false;

    // False Ц слоты принимают предмет любого типа ItemType; True Ц только типа slotsType
    [Tooltip("Does the container hold items of a certain type. If so, Slots Type defines the type")]
    [field: SerializeField] public bool isSpecialized { get; private set; } = false;

    //  акого типа предметы будут разрешены к помещению в контейнер
    [Tooltip("Type of the items, allowed to be contained. Active if Is Specialized = True")]
    [field: SerializeField] public ItemTypeSO slotsType { get; private set; }

    // ѕозвол€ет перемещать предметы только между однотипным тэгом (если типы предметов совместимы)
    [Tooltip("True Ц reads and applies contained items effects")]
    [field: SerializeField] public containerTag groupTag { get; private set; } = containerTag.common;

    // —читывает и примен€ет ли эффект содержащихс€ в себе предметов
    [Tooltip("True Ц reads and applies contained items effects")]
    [field: SerializeField] public bool isApplyingItemsEffects { get; private set; }

    // —писок предметов, покупаемых или лутаемых из этого контейнера
    [Tooltip("This items will be generated in this container, if list is not empty")]
    [field: SerializeField] public List<BasicItemSO> itemsInIt { get; private set; }
}

public enum containerTag
{
    common,
    equipment,
    trader
}
