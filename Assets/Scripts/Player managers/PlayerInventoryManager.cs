using JetBrains.Annotations;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using static LeTai.Asset.TranslucentImage.VPMatrixCache;
using static UIManager;

public class PlayerInventoryManager : InitializableMonoBehaviour
{
    //PlayerManager playerManager;

    public int bagSlotsCount { get; private set; } // Размер рюкзака/сумки
    private const int maxBagSlots = 200; // Максимально допустимый игровой размер инвентаря

    [field: Header("Main Containers")]
    [field: SerializeField] public ItemContainerConfigSO backpackConfig { get; private set; }
    public Container backpack { get; private set; } // Минимальный стартовый безусловный игровой размер рюкзака. Динамически меняется от бонусов
    
    [field: SerializeField] public ItemContainerConfigSO quickBarConfig { get; private set; }
    public Container quickBar { get; private set; } // Быстрый доступ

    [field: Header("Equipables Containers")]
    [field: SerializeField] public ItemContainerConfigSO headSlotConfig { get; private set; }
    public Container headSlot { get; private set; }
    [field: SerializeField] public ItemContainerConfigSO bodySlotConfig { get; private set; }
    public Container bodySlot { get; private set; }
    [field: SerializeField] public ItemContainerConfigSO feetSlotConfig { get; private set; }
    public Container feetSlot { get; private set; }
    [field: SerializeField] public ItemContainerConfigSO specialSlotConfig { get; private set; }
    public Container specialSlot { get; private set; }
    [field: SerializeField] public ItemContainerConfigSO bagSlotConfig { get; private set; }
    public Container backpackSlot { get; private set; }
    [field: SerializeField] public ItemContainerConfigSO accessorySlotsConfig { get; private set; }
    public Container accessorySlots { get; private set; }

    [field: Header("On-hand crafting Containers")]
    [field: SerializeField] public ItemContainerConfigSO craftingFieldConfig { get; private set; }
    public Container craftingField { get; private set; }
    [field: SerializeField] public ItemContainerConfigSO craftingResultsConfig { get; private set; }
    public Container craftingResults { get; private set; }

    [field: Header("Barter transfer Containers")]
    [field: SerializeField] public ItemContainerConfigSO barterPlayerTransfersConfig { get; private set; }
    public Container barterPlayerTransfer { get; private set; }
    [field: SerializeField] public ItemContainerConfigSO barterTraderTransfersConfig { get; private set; }
    public Container barterTraderTransfer { get; private set; }

    [field: NonSerialized] public Container interactedContainer { get; private set; } // Лутаемый конейнер или инвентарь торговца
    [NonSerialized] public int activeItemSlotIndex; // Активный слот квикбара (предмет слота активен – в руке)
    [NonSerialized] public int lastActiveItemSlotIndex; // Последний активный слот квикбара
    [NonSerialized] private GameObject activeItemGameObject; // Объект активного предмета (в мире – в руке)

    private ServiceLocator serviceLocator;
    private PlayerManager playerManager;
    private UIManager uiManager;

    private void Awake()
    {
        ScriptTools.RegisterScript(this);

        serviceLocator = ServiceLocator.Instance;
    }

    private void Start()
    {
        backpack = new Container(backpackConfig, "Backpack");
        quickBar = new Container(quickBarConfig, "Quickbar");

        headSlot = new Container(headSlotConfig, "Head Slot");
        bodySlot = new Container(bodySlotConfig, "Body Slot");
        feetSlot = new Container(feetSlotConfig, "Feet Slot");
        specialSlot = new Container(specialSlotConfig, "Special Slot");
        backpackSlot = new Container(bagSlotConfig, "Bag Slot");
        accessorySlots = new Container(accessorySlotsConfig, "Accessory Slots");

        craftingField = new Container(craftingFieldConfig, "Crafting Field");
        craftingResults = new Container(craftingResultsConfig, "Crafting Results");

        barterPlayerTransfer = new Container(barterPlayerTransfersConfig, "Barter Transfer (Player)");
        barterTraderTransfer = new Container(barterTraderTransfersConfig, "Barter Transfer (Trader)");

        // ЗДЕСЬ ЗАГРУЗКА ЭКИПИРОВКИ ИГРОКА SaveManager.LoadPlayerEquipment();
        RecalculateBackpackSlotsCount();
        // ЗДЕСЬ ЗАГРУЗКА ИНВЕНТАРЯ ИГРОКА SaveManager.LoadPlayerInventory();

        playerManager = serviceLocator.playerManager;
        uiManager = serviceLocator.uiManager;

        if (AreDependenciesInitialized(playerManager, uiManager))
            Initialize();

        if (!IsInitialized()) return;

        LinkContainersToSlots();
    }

    // "Связать" контейнера с их ui-репрезентациями (обновлять вторые при изменении состояний первых)
    private void LinkContainersToSlots()
    {
        backpack.OnChangeContentsEvent.AddListener(uiManager.UpdateBackpack);
        quickBar.OnChangeContentsEvent.AddListener(uiManager.UpdateQuickbar);
        quickBar.OnChangeContentsEvent.AddListener(RetakeFromSlot);
        headSlot.OnChangeContentsEvent.AddListener(uiManager.UpdateEquipmentSlots);
        bodySlot.OnChangeContentsEvent.AddListener(uiManager.UpdateEquipmentSlots);
        feetSlot.OnChangeContentsEvent.AddListener(uiManager.UpdateEquipmentSlots);
        specialSlot.OnChangeContentsEvent.AddListener(uiManager.UpdateEquipmentSlots);
        backpackSlot.OnChangeContentsEvent.AddListener(uiManager.UpdateEquipmentSlots);
        accessorySlots.OnChangeContentsEvent.AddListener(uiManager.UpdateEquipmentSlots);
        craftingField.OnChangeContentsEvent.AddListener(uiManager.UpdateCrafting);
        craftingResults.OnChangeContentsEvent.AddListener(uiManager.UpdateCrafting);
        barterPlayerTransfer.OnChangeContentsEvent.AddListener(uiManager.UpdateBarter);
        barterTraderTransfer.OnChangeContentsEvent.AddListener(uiManager.UpdateBarter);
    }

    // =================================

    //!!! Конфигурация инвентаря не меняется – она задает начальные значения
    // Пересчитать количество доступных слотов инвентаря (рюкзака/сумки)
    public void RecalculateBackpackSlotsCount() 
    {
        int newBagSlotsCount = new int(); // Новое количество слотов – сумма всех бонусов
        newBagSlotsCount = backpackConfig.numberOfSlots + GetSlotsBonus(backpackSlot) + GetSlotsBonus(headSlot) + GetSlotsBonus(bodySlot) 
            + GetSlotsBonus(feetSlot) + GetSlotsBonus(accessorySlots);
        if (newBagSlotsCount < bagSlotsCount) // Если слотов стало меньше, чем было, то скинуть невмещающиеся предметы на землю
        {
            int startingIndexOfRemovableItems = newBagSlotsCount;
            for (int i = backpack.Count - 1; i >= startingIndexOfRemovableItems; i--)
            {
                if (backpack[i].hasItem)
                    backpack[i].item.DropUnderPlayer(backpack[i].amount, true);
            }
        }

        if (newBagSlotsCount > maxBagSlots) // Новое количество слотов не может превышать максимум
            newBagSlotsCount = maxBagSlots; 
        bagSlotsCount = newBagSlotsCount;

        while (backpack.Count < bagSlotsCount) // Увеличить размер контейнера, если слотов стало больше
        {
            ContainerSlot emptySlot; // Пустой дефолтный слот с типом этого контейнера
            emptySlot = new ContainerSlot(backpack);
            emptySlot.Empty();
            backpack.Add(emptySlot);
        }

        while (backpack.Count > bagSlotsCount) // Удалить слоты, если количество стало меньше
            backpack.RemoveAt(backpack.Count - 1);

        uiManager?.RepopulateBackpack();
        backpack.OnChangeContents();
    }

    private int GetSlotsBonus(Container container) // Получить бонус количества слотов контейнера рюкзака от надетых предметов (экипировки)
    {
        if (container == null) // Если ничего не надето
            return 0;

        int sumNumberOfSlots = 0;
        foreach (ContainerSlot slot in container)
        {
            if (slot.item != null)
            {
                EquipableItemSO equipableItem = (EquipableItemSO)slot.item;
                int bonusSlots = equipableItem.inventorySlots;
                sumNumberOfSlots += bonusSlots;
            }
        }
        return sumNumberOfSlots;
    }

    public void SetConfigs(
        ItemContainerConfigSO backpackConfig,
        ItemContainerConfigSO quickBarConfig,
        ItemContainerConfigSO headSlotConfig,
        ItemContainerConfigSO bodySlotConfig,
        ItemContainerConfigSO feetSlotConfig,
        ItemContainerConfigSO specialSlotConfig,
        ItemContainerConfigSO bagSlotConfig,
        ItemContainerConfigSO accessorySlotsConfig,
        ItemContainerConfigSO craftingFieldConfig,
        ItemContainerConfigSO craftingResultsConfig,
        ItemContainerConfigSO barterPlayerTransfersConfig,
        ItemContainerConfigSO barterTraderTransfersConfig)
    {
        this.backpackConfig = backpackConfig;
        this.quickBarConfig = quickBarConfig;
        this.headSlotConfig = headSlotConfig;
        this.bodySlotConfig = bodySlotConfig;
        this.feetSlotConfig = feetSlotConfig;
        this.specialSlotConfig = specialSlotConfig;
        this.bagSlotConfig = bagSlotConfig;
        this.accessorySlotsConfig = accessorySlotsConfig;
        this.craftingFieldConfig = craftingFieldConfig;
        this.craftingResultsConfig = craftingResultsConfig;
        this.barterPlayerTransfersConfig = barterPlayerTransfersConfig;
        this.barterTraderTransfersConfig = barterTraderTransfersConfig;
    }

    //====================== Перемещение =====================

    // Автоперемещение в меню экипировки
    public bool MoveItemInEquipment(Container fromContainer, int itemIndex, bool fromAnyContainer = false)
    {
        ContainerSlot slot = fromContainer[itemIndex];
        BasicItemSO item = slot.item;
        bool isItemMovedToEquipment = false;
        bool isItemMovedToQuickbar = false;
        if (fromContainer == backpack || fromAnyContainer)
        {
            // Если предмет типа слота из экипировки – поместить его туда
            List<Container> playerContainers = new List<Container>() { headSlot, bodySlot, feetSlot, backpackSlot, specialSlot, accessorySlots };
            foreach (Container container in playerContainers) 
            {
                if (item.type == container.config.slotsType)
                {
                    fromContainer.MoveItemTo(container, itemIndex, slot.amount, true);
                    isItemMovedToEquipment = true;
                    break;
                }
            }
            // Если тип предмета любой другой – переместить в квикбар
            if (!isItemMovedToEquipment)
            {
                fromContainer.MoveItemTo(quickBar, itemIndex, slot.amount, true);
                isItemMovedToQuickbar = true;
            }
        }
        else
        {
            fromContainer.MoveItemTo(backpack, itemIndex, slot.amount);
        }
        return isItemMovedToQuickbar;
    }

    //====================== Активные слоты – квикбар =====================

    // Активен ли какой-либо предмет (не пусты ли руки)
    public bool IsAnyItemActive()
    {
        if (activeItemSlotIndex == -1
            || !quickBar[activeItemSlotIndex].hasItem)
            return false;
        else
            return true;
    }

    // Есть ли предмет в квикбаре по индексу
    public bool IsQuickbarIndHasItem(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= quickBar.Count) 
            return false;

        if (!quickBar[itemIndex].hasItem) 
            return false;
        else 
            return true;
    }

    //
    public BasicItemSO GetActiveItem()
    {
        if (activeItemSlotIndex < 0 || activeItemSlotIndex >= quickBar.Count)
            return null;

        if (quickBar[activeItemSlotIndex].hasItem)
            return quickBar[activeItemSlotIndex].item;
        else
            return null;
    }

    //
    public BasicItemSO GetQuickbarItemByIndex(int index)
    {
        if (index < 0 || index >= quickBar.Count)
            return null;

        if (quickBar[index].hasItem)
            return quickBar[index].item;
        else
            return null;
    }

    // Шаг по слотам квикбара
    public void ScrollActiveSlot(int step)
    {
        SetActiveSlotByIndex(GetScrolledSlotIndex(step));
    }
    //
    public int GetScrolledSlotIndex(int step)
    {
        int activeItemSlotIndex = uiManager.focusedQuickbarSlot;
        // Скролл назад и активен первый слот – активировать последний слот
        if (activeItemSlotIndex <= 0 && step < 0)
            return quickBar.Count - 1;
        // Скролл вперед и активен последний слот – активировать первый слот
        else if (activeItemSlotIndex == quickBar.Count - 1 && step > 0)
            return 0;
        // Активен не первый или последний слот – активировать предыдущий/следующий слот
        else
            return activeItemSlotIndex + step;
    }

    // Установить активный слот квикбара
    public void SetActiveSlotByIndex(int slotIndex, bool changeOnlyIndex = false)
    {
        if (!IsInitialized()) return;
        if (slotIndex < -1 || slotIndex >= quickBar.Count) return;

        // Убрать предмет из руки перед взятием нового
        RemoveItemFromHand();

        if (activeItemSlotIndex != -1)
            lastActiveItemSlotIndex = activeItemSlotIndex;

        // Задать новый активный слот
        if (activeItemSlotIndex == -1 && slotIndex == -1)
            activeItemSlotIndex = lastActiveItemSlotIndex;
        else
            activeItemSlotIndex = slotIndex;

        // Убрать фокус с предыдущего активного слота
        uiManager.SetQuickbarCellFocused(lastActiveItemSlotIndex, false);

        // Если changeOnlyIndex, то не изменять маркер UI активного и не брать в руки предмет
        if (changeOnlyIndex)
            return;

        // Слот переключен на новый (активный слот есть / не отключен)
        if (activeItemSlotIndex != -1)
        {
            uiManager.SetQuickbarCellFocused(activeItemSlotIndex, true);
            // В нем есть предмет – взять в руки
            ContainerSlot slot = quickBar[activeItemSlotIndex];
            if (slot.hasItem)
                TakeItemInHand(slot.item);
        }

        // ПЕРЕКЛЮЧЕНИЕ АКТИВНОГО СЛОТА В UI
    }
    // Для вызова при изменении состава слота контейнера квикбара
    public void RetakeFromSlot()
    {
        SetActiveSlotByIndex(activeItemSlotIndex);
    }

    //
    public void RemoveItemFromHand()
    {
        Transform rightHand = playerManager.characterPoints.rightHandHoldingPoint;
        GameObjectTools.DestroyAllChildren(rightHand);
        activeItemGameObject = null;
    }

    // Взять предмет в руку
    private void TakeItemInHand(BasicItemSO item)
    {
        // Поместить в руку модели игрока
        Transform rightHand = playerManager.characterPoints.rightHandHoldingPoint;
        GameObject takenItem = PlayerCharacterTools.AttachObjectToAttachingPoint(item.modelPrefab, rightHand);

        // Если используемое (оружие/инструмент) – создать damage dealer от класса оружия
        if (item is UsableItemSO usable)
            ItemTools.AddDamageDealer(takenItem, usable);

        activeItemGameObject = takenItem;

        // АКТИВАЦИЯ ЭФФЕКТОВ ВЗЯТОГО ПРЕДМЕТА
        // АКТИВАЦИЯ АНИМАЦИИ АКТИВНОГО ПРЕДМЕТА
    } 

    // Вкл/Выкл damage dealer активного предмета
    public void SetActiveItemObjectDamageDealerState(bool state)
    {
        activeItemGameObject.transform.Find("Damage dealer")?.gameObject.SetActive(state);
    }

    // ================= Взаимодействуемый контейнер =================

    public void SetInteractedContainer(Container container)
    {
        interactedContainer = container;
        interactedContainer.OnChangeContentsEvent.AddListener(serviceLocator.uiManager.RepopulateInteractedContainer);
    }
}
