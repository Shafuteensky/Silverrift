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

    public int bagSlotsCount { get; private set; } // ������ �������/�����
    private const int maxBagSlots = 200; // ����������� ���������� ������� ������ ���������

    [field: Header("Main Containers")]
    [field: SerializeField] public ItemContainerConfigSO backpackConfig { get; private set; }
    public Container backpack { get; private set; } // ����������� ��������� ����������� ������� ������ �������. ����������� �������� �� �������
    
    [field: SerializeField] public ItemContainerConfigSO quickBarConfig { get; private set; }
    public Container quickBar { get; private set; } // ������� ������

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

    [field: NonSerialized] public Container interactedContainer { get; private set; } // �������� �������� ��� ��������� ��������
    [NonSerialized] public int activeItemSlotIndex; // �������� ���� �������� (������� ����� ������� � � ����)
    [NonSerialized] public int lastActiveItemSlotIndex; // ��������� �������� ���� ��������
    [NonSerialized] private GameObject activeItemGameObject; // ������ ��������� �������� (� ���� � � ����)

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

        // ����� �������� ���������� ������ SaveManager.LoadPlayerEquipment();
        RecalculateBackpackSlotsCount();
        // ����� �������� ��������� ������ SaveManager.LoadPlayerInventory();

        playerManager = serviceLocator.playerManager;
        uiManager = serviceLocator.uiManager;

        if (AreDependenciesInitialized(playerManager, uiManager))
            Initialize();

        if (!IsInitialized()) return;

        LinkContainersToSlots();
    }

    // "�������" ���������� � �� ui-��������������� (��������� ������ ��� ��������� ��������� ������)
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

    //!!! ������������ ��������� �� �������� � ��� ������ ��������� ��������
    // ����������� ���������� ��������� ������ ��������� (�������/�����)
    public void RecalculateBackpackSlotsCount() 
    {
        int newBagSlotsCount = new int(); // ����� ���������� ������ � ����� ���� �������
        newBagSlotsCount = backpackConfig.numberOfSlots + GetSlotsBonus(backpackSlot) + GetSlotsBonus(headSlot) + GetSlotsBonus(bodySlot) 
            + GetSlotsBonus(feetSlot) + GetSlotsBonus(accessorySlots);
        if (newBagSlotsCount < bagSlotsCount) // ���� ������ ����� ������, ��� ����, �� ������� ������������� �������� �� �����
        {
            int startingIndexOfRemovableItems = newBagSlotsCount;
            for (int i = backpack.Count - 1; i >= startingIndexOfRemovableItems; i--)
            {
                if (backpack[i].hasItem)
                    backpack[i].item.DropUnderPlayer(backpack[i].amount, true);
            }
        }

        if (newBagSlotsCount > maxBagSlots) // ����� ���������� ������ �� ����� ��������� ��������
            newBagSlotsCount = maxBagSlots; 
        bagSlotsCount = newBagSlotsCount;

        while (backpack.Count < bagSlotsCount) // ��������� ������ ����������, ���� ������ ����� ������
        {
            ContainerSlot emptySlot; // ������ ��������� ���� � ����� ����� ����������
            emptySlot = new ContainerSlot(backpack);
            emptySlot.Empty();
            backpack.Add(emptySlot);
        }

        while (backpack.Count > bagSlotsCount) // ������� �����, ���� ���������� ����� ������
            backpack.RemoveAt(backpack.Count - 1);

        uiManager?.RepopulateBackpack();
        backpack.OnChangeContents();
    }

    private int GetSlotsBonus(Container container) // �������� ����� ���������� ������ ���������� ������� �� ������� ��������� (����������)
    {
        if (container == null) // ���� ������ �� ������
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

    //====================== ����������� =====================

    // ��������������� � ���� ����������
    public bool MoveItemInEquipment(Container fromContainer, int itemIndex, bool fromAnyContainer = false)
    {
        ContainerSlot slot = fromContainer[itemIndex];
        BasicItemSO item = slot.item;
        bool isItemMovedToEquipment = false;
        bool isItemMovedToQuickbar = false;
        if (fromContainer == backpack || fromAnyContainer)
        {
            // ���� ������� ���� ����� �� ���������� � ��������� ��� ����
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
            // ���� ��� �������� ����� ������ � ����������� � �������
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

    //====================== �������� ����� � ������� =====================

    // ������� �� �����-���� ������� (�� ����� �� ����)
    public bool IsAnyItemActive()
    {
        if (activeItemSlotIndex == -1
            || !quickBar[activeItemSlotIndex].hasItem)
            return false;
        else
            return true;
    }

    // ���� �� ������� � �������� �� �������
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

    // ��� �� ������ ��������
    public void ScrollActiveSlot(int step)
    {
        SetActiveSlotByIndex(GetScrolledSlotIndex(step));
    }
    //
    public int GetScrolledSlotIndex(int step)
    {
        int activeItemSlotIndex = uiManager.focusedQuickbarSlot;
        // ������ ����� � ������� ������ ���� � ������������ ��������� ����
        if (activeItemSlotIndex <= 0 && step < 0)
            return quickBar.Count - 1;
        // ������ ������ � ������� ��������� ���� � ������������ ������ ����
        else if (activeItemSlotIndex == quickBar.Count - 1 && step > 0)
            return 0;
        // ������� �� ������ ��� ��������� ���� � ������������ ����������/��������� ����
        else
            return activeItemSlotIndex + step;
    }

    // ���������� �������� ���� ��������
    public void SetActiveSlotByIndex(int slotIndex, bool changeOnlyIndex = false)
    {
        if (!IsInitialized()) return;
        if (slotIndex < -1 || slotIndex >= quickBar.Count) return;

        // ������ ������� �� ���� ����� ������� ������
        RemoveItemFromHand();

        if (activeItemSlotIndex != -1)
            lastActiveItemSlotIndex = activeItemSlotIndex;

        // ������ ����� �������� ����
        if (activeItemSlotIndex == -1 && slotIndex == -1)
            activeItemSlotIndex = lastActiveItemSlotIndex;
        else
            activeItemSlotIndex = slotIndex;

        // ������ ����� � ����������� ��������� �����
        uiManager.SetQuickbarCellFocused(lastActiveItemSlotIndex, false);

        // ���� changeOnlyIndex, �� �� �������� ������ UI ��������� � �� ����� � ���� �������
        if (changeOnlyIndex)
            return;

        // ���� ���������� �� ����� (�������� ���� ���� / �� ��������)
        if (activeItemSlotIndex != -1)
        {
            uiManager.SetQuickbarCellFocused(activeItemSlotIndex, true);
            // � ��� ���� ������� � ����� � ����
            ContainerSlot slot = quickBar[activeItemSlotIndex];
            if (slot.hasItem)
                TakeItemInHand(slot.item);
        }

        // ������������ ��������� ����� � UI
    }
    // ��� ������ ��� ��������� ������� ����� ���������� ��������
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

    // ����� ������� � ����
    private void TakeItemInHand(BasicItemSO item)
    {
        // ��������� � ���� ������ ������
        Transform rightHand = playerManager.characterPoints.rightHandHoldingPoint;
        GameObject takenItem = PlayerCharacterTools.AttachObjectToAttachingPoint(item.modelPrefab, rightHand);

        // ���� ������������ (������/����������) � ������� damage dealer �� ������ ������
        if (item is UsableItemSO usable)
            ItemTools.AddDamageDealer(takenItem, usable);

        activeItemGameObject = takenItem;

        // ��������� �������� ������� ��������
        // ��������� �������� ��������� ��������
    } 

    // ���/���� damage dealer ��������� ��������
    public void SetActiveItemObjectDamageDealerState(bool state)
    {
        activeItemGameObject.transform.Find("Damage dealer")?.gameObject.SetActive(state);
    }

    // ================= ����������������� ��������� =================

    public void SetInteractedContainer(Container container)
    {
        interactedContainer = container;
        interactedContainer.OnChangeContentsEvent.AddListener(serviceLocator.uiManager.RepopulateInteractedContainer);
    }
}
