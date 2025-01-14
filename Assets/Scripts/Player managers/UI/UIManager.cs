using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using LeTai.Asset.TranslucentImage;
using static RootMotion.FinalIK.RagdollUtility;
using static LeTai.Asset.TranslucentImage.VPMatrixCache;
using System.ComponentModel;
using static UnityEditor.Progress;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Linq;

public class UIManager : InitializableMonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject inventoryGridPrefab;
    [SerializeField] private GameObject containerCellPrefab;

    [Header("Overlay")]
    [SerializeField] private GameObject overlayLayout;

    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject compassPanel;
    [SerializeField] private GameObject targetDescriptionPanel;
    private TextMeshProUGUI targetDescriptionText;

    [SerializeField] private GameObject effectLitePanel;
    [SerializeField] private GameObject effectsFullPanel;

    [SerializeField] private GameObject questsPanel;
    [SerializeField] private GameObject quickbarPanel;
    [SerializeField] private GameObject dialogPanel;

    [Header("Player inventory")]
    [SerializeField] private GameObject inventoryLayout;
    [SerializeField] private GameObject inventoryButtonsPanel;

    [SerializeField] private GameObject backpackPanel;
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject craftPanel;
    [SerializeField] private GameObject blueprintsPanel;

    [Header("Inventory panels")]
    [SerializeField] private GameObject leftContainerPanel;
    [SerializeField] private GameObject playerTransferPanel;
    [SerializeField] private GameObject traderTransferPanel;

    [Header("Items")]
    [SerializeField] public GameObject itemDescriptionPanel;
    [SerializeField] public GameObject itemMenuPanel;
    [SerializeField] public GameObject itemSplitterPanel;
    [SerializeField] public GameObject draggedItemImagePanel;
    [SerializeField] public GameObject dropZonePanel;

    [Header("Layouts")]
    [SerializeField] private GameObject containerLayout;
    [SerializeField] private GameObject containerInventoryButtons;
    [SerializeField] private GameObject barterLayout;

    [Header("Player Inventory Containers")]
    [SerializeField] private GameObject headSlot;
    [SerializeField] private GameObject bodySlot;
    [SerializeField] private GameObject feetSlot;
    [SerializeField] private GameObject backpackSlot;
    [SerializeField] private GameObject specialSlot;
    [SerializeField] private GameObject accessoriesGrid;

    [Header("Barter")]
    [SerializeField] private GameObject barterBalanceButtonObject;
    [field: NonSerialized] public Button barterBalanceButton { get; private set; }
    [SerializeField] private GameObject barterDealButtonObject;
    [field: NonSerialized] public Button barterDealButton { get; private set; }

    [Header("Container grids")]
    // ������� ������� � ������������� �����
    [SerializeField] private GameObject quickbarGrid;
    [SerializeField] private GameObject craftingOptionsGrid;
    // �������������� ��� ������������� �����
    private GameObject backpackGrid;
    private GameObject containerInventoryGrid;
    private GameObject barterPlayerTransferGrid;
    private GameObject barterTraderTransferGrid;

    [Header("Item slots")]
    [SerializeField] private GameObject focusedSlotPanel;

    // --------------------------------------------

    // ��� ������� ������� �� ������ � ������
    [NonSerialized] public Container originalContainer;
    [NonSerialized] public int originalIndex;
    [NonSerialized] public bool isDragging;
    // ��� ���� ������ � ��������� � ������
    [NonSerialized] public ContainerSlot processingSlot;

    InputManager inputManager;
    CameraManager cameraManager;
    PlayerManager playerManager;
    [NonSerialized] public PlayerInventoryManager playerInventoryManager;

    int elementPadding = 10;

    private bool isMenuShown; // ������� �� ���� �������� (����� �� ��������� ��� ������������ � ���������)
    public bool isInventoryOpened { get; private set; } = false; // ���� �� ��������� ���������� UI �� �����, ����� ������������ �������
    public UILayouts currentLayout { get; private set; } // ������� ����������

    // --------------------------------------------

    private void Awake()
    {
        barterBalanceButton = barterBalanceButtonObject.GetComponent<Button>();
        barterDealButton = barterDealButtonObject.GetComponent<Button>();

        ScriptTools.RegisterScript(this);
    }

    private ServiceLocator serviceLocator;

    private void Start()
    {
        serviceLocator = ServiceLocator.Instance;

        targetDescriptionText = targetDescriptionPanel.GetComponentInChildren<TextMeshProUGUI>();

        // �������� ������� �������� ���� (������� ����) ��� ������� Translucent Image
        Color transculentPanelColor = new Color(0, 0, 0); // ���� �������
        void configureTranslucentImage(Transform panel)
        {
            if (panel.TryGetComponent(out TranslucentImage image))
            {
                image.source = Camera.main.GetComponent<TranslucentImageSource>();
                image.color = transculentPanelColor;
            }
        }
        // ��� ������� �������� UI � �� ��������� ���������, ��� �������� Translucent Image
        foreach (Transform child in this.gameObject.transform)
        {
            foreach (Transform childOfChild in child.transform)
                configureTranslucentImage(childOfChild);
            configureTranslucentImage(child);
        }

        // ���������� ������� �����������
        InstantiateInvGrid(backpackPanel, ref backpackGrid);
        InstantiateInvGrid(leftContainerPanel, ref containerInventoryGrid);
        InstantiateInvGrid(playerTransferPanel, ref barterPlayerTransferGrid);
        InstantiateInvGrid(traderTransferPanel, ref barterTraderTransferGrid);

        StartCoroutine(GetManagersAndInit());
    }

    // �������� ������������� ����� ����������� ����������
    private void InstantiateInvGrid(GameObject inventoryPanel, ref GameObject grid, bool autoHideScroller = false)
    {
        // ������������� �������
        GameObject scrollView = Instantiate(inventoryGridPrefab, inventoryPanel.transform);
        if (autoHideScroller)
            scrollView.GetComponent<ScrollRect>().verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        // ���������
        RectTransform scrollViewRect = scrollView.GetComponent<RectTransform>();
        scrollViewRect.anchorMin = new Vector2(0, 0);
        scrollViewRect.anchorMax = new Vector2(1, 1);
        scrollViewRect.offsetMax = new Vector2(0, 0);
        scrollViewRect.offsetMin = new Vector2(0, 0);

        // ��������������� �������
        GameObject viewport = scrollView.transform.GetChild(0).gameObject;
        grid = viewport.transform.GetChild(0).gameObject;
        grid.name = inventoryPanel.name + "Grid";
        // ������� �� �����
        GridLayoutGroup gridLayout = grid.GetComponent<GridLayoutGroup>();
        gridLayout.padding = new RectOffset(elementPadding, elementPadding, elementPadding, elementPadding);

        // Scrollbar
        GameObject scrollbar = scrollView.transform.GetChild(1).gameObject;
        // ������� �� �����
        RectTransform rect = scrollbar.GetComponent<RectTransform>();
        rect.offsetMax = new Vector2(-elementPadding, -elementPadding); // +pos+width, -top
        rect.offsetMin = new Vector2(-30-elementPadding, elementPadding); // -width, +all
    }

    private IEnumerator GetManagersAndInit()
    {
        while (serviceLocator.cameraManager == null || serviceLocator.playerInventoryManager == null)
            yield return null;

        cameraManager = serviceLocator.cameraManager;
        playerManager = serviceLocator.playerManager;
        playerInventoryManager = serviceLocator.playerInventoryManager;

        if (AreDependenciesInitialized(cameraManager, playerInventoryManager, playerManager))
            Initialize();

        // ����� ���������� UI
        Deactivate();
        UILayouts standard = UILayouts.StandardGameplayOverlay;
        SetLayout(standard);
        PopulateAllPlayerContainers();
    }

    //====================== ���������� =====================
    #region ����������
    // ��������� ���������� UI
    public enum UILayouts
    {
        StandardGameplayOverlay = 0,
        MoreInfoOverlay = 1,
        PlayerEquipment = 2,
        CraftMenu = 3,
        Compendium = 4,
        LootingContainer = 5,
        Barter = 6,
        Dialog = 7
    }

    // ������������ ���������� UI (� ���������� ����� ������ ������)
    public void SwitchLayout(UILayouts layout, bool facePlayer = false)
    {
        if (!IsInitialized(true)) return;
        if (isDragging) return;

        // ------- ����� �� �������� ���������/���������� ------

        // ��� ������ � ���� ������� ������� ����������� ����������
        if (currentLayout == UILayouts.Barter)
        {
            ClearPlayerBarterTransfer();
            ClearTraderBarterTransfer();
        }

        // ------- ���� � ����� ���������/��������� ------
        // ���� ��������� ������ � �� �������
        if (layout != currentLayout) 
        {
            SetLayout(layout);
            if (facePlayer)
                cameraManager.FacePlayer();
            else
                cameraManager.RestorePosition();
        }
        // ������ � �������
        else
        {
            SetLayout(UILayouts.StandardGameplayOverlay);
            cameraManager.RestorePosition();
        }

        RefreshInteractivity();
    }

    // ��������� ���������� UI
    private void SetLayout(UILayouts layout)
    {
        if (!IsInitialized(true)) return;

        if (!Enum.IsDefined(typeof(UILayouts), layout))
            return;

        currentLayout = layout;
        Deactivate();
        switch (layout)
        {
            case UILayouts.StandardGameplayOverlay:
                {
                    overlayLayout.SetActive(true);
                    statsPanel.SetActive(true);
                    compassPanel.SetActive(true);
                    effectLitePanel.SetActive(true);
                    targetDescriptionPanel.SetActive(true);

                    quickbarPanel.SetActive(true);

                    playerManager.SetPlayerState(PlayerManager.PlayerStates.moving);
                    break;
                }
            case UILayouts.MoreInfoOverlay:
                {
                    overlayLayout.SetActive(true);
                    statsPanel.SetActive(true);
                    compassPanel.SetActive(true);
                    effectsFullPanel.SetActive(true);
                    targetDescriptionPanel.SetActive(true);
                    questsPanel.SetActive(true);

                    quickbarPanel.SetActive(true);

                    playerManager.SetPlayerState(PlayerManager.PlayerStates.moving);
                    break;
                }
            case UILayouts.PlayerEquipment:
                {
                    overlayLayout.SetActive(true);
                    statsPanel.SetActive(true);
                    compassPanel.SetActive(true);
                    effectLitePanel.SetActive(true);
                    quickbarPanel.SetActive(true);

                    inventoryLayout.SetActive(true);
                    inventoryButtonsPanel.SetActive(true);
                    equipmentPanel.SetActive(true);
                    backpackPanel.SetActive(true);

                    playerManager.SetPlayerState(PlayerManager.PlayerStates.inventory);
                    break;
                }
            case UILayouts.CraftMenu:
                {
                    overlayLayout.SetActive(true);
                    quickbarPanel.SetActive(true);

                    inventoryLayout.SetActive(true);
                    inventoryButtonsPanel.SetActive(true);
                    craftPanel.SetActive(true);
                    backpackPanel.SetActive(true);

                    playerManager.SetPlayerState(PlayerManager.PlayerStates.inventory);
                    break;
                }
            case UILayouts.Compendium:
                {
                    inventoryLayout.SetActive(true);
                    inventoryButtonsPanel.SetActive(true);
                    blueprintsPanel.SetActive(true);
                    break;
                }
            case UILayouts.LootingContainer:
                {
                    overlayLayout.SetActive(true);
                    statsPanel.SetActive(true);
                    compassPanel.SetActive(true);
                    targetDescriptionPanel.SetActive(true);
                    quickbarPanel.SetActive(true);

                    inventoryLayout.SetActive(true);
                    backpackPanel.SetActive(true);

                    SetActiveWithChilds(containerLayout);

                    playerManager.SetPlayerState(PlayerManager.PlayerStates.inventory);
                    break;
                }
            case UILayouts.Barter:
                {
                    overlayLayout.SetActive(true);
                    quickbarPanel.SetActive(true);

                    inventoryLayout.SetActive(true);
                    backpackPanel.SetActive(true);

                    SetActiveWithChilds(containerLayout);
                    containerInventoryButtons.SetActive(false);
                    SetActiveWithChilds(barterLayout);

                    break;
                }
            case UILayouts.Dialog:
                {
                    overlayLayout.SetActive(true);
                    statsPanel.SetActive(true);
                    compassPanel.SetActive(true);
                    targetDescriptionPanel.SetActive(true);
                    dialogPanel.SetActive(true);

                    //playerManager.SetPlayerState(PlayerManager.PlayerStates.dialog);
                    break;
                }
            default:
                break;
        }
    }

    // ������� ��� ������ UI ��������� ������
    public void SetLayoutOverlay()
    {
        SwitchLayout(UILayouts.StandardGameplayOverlay);
    }
    public void SetLayoutEquipment()
    {
        SwitchLayout(UILayouts.PlayerEquipment, true);
    }
    public void SetLayoutCrafting()
    {
        SwitchLayout(UILayouts.CraftMenu);
    }
    public void SetLayoutCompendium()
    {
        SwitchLayout(UILayouts.Compendium);
    }
    public void SetLayoutBarter()
    {
        SwitchLayout(UILayouts.Barter);
    }
    public void SetLayoutLooting()
    {
        SwitchLayout(UILayouts.LootingContainer);
    }

    // ���������� ���� ��������� UI � �� �����
    private void Deactivate()
    {
        SetActiveWithChilds(overlayLayout, false);
        SetActiveWithChilds(inventoryLayout, false);
        SetActiveWithChilds(containerLayout, false);
        SetActiveWithChilds(barterLayout, false);
        SetActiveWithChilds(itemDescriptionPanel, false);
    }
    private void SetActiveWithChilds(GameObject parent, bool isActive = true)
    {
        foreach (Transform child in parent.transform)
            child.gameObject.SetActive(isActive);
        parent.SetActive(isActive);
    }
    #endregion

    //====================== ���������� =====================
    #region ����������
    // ����� ��� �� ��������� ����������
    public void TakeAllItems()
    {
        Container fromContainer = playerInventoryManager.interactedContainer;
        if (fromContainer.MoveAllItemsTo(playerInventoryManager.backpack) > 0)
            fromContainer.MoveAllItemsTo(playerInventoryManager.quickBar); // ���� ��� ����� � ������� � ���������� � �������
    }

    // �������� �������� (����������� �������� ������� � �������� ��������)
        // �������� ������
    public void ClearPlayerBarterTransfer()
    {
        Container fromContainer = playerInventoryManager.barterPlayerTransfer;
        if (fromContainer.MoveAllItemsTo(playerInventoryManager.backpack) > 0)
            fromContainer.MoveAllItemsTo(playerInventoryManager.quickBar); // ���� ��� ����� � ������� � ���������� � �������
    }
        // �������� ��������
    public void ClearTraderBarterTransfer()
    {
        Container fromContainer = playerInventoryManager.barterTraderTransfer;
        fromContainer.MoveAllItemsTo(playerInventoryManager.interactedContainer);
    }

    // �������� ��������������� ����� � ������ ������ ��������
    public void RefreshInteractivity()
    {
        PlayerManager playerManager = FindFirstObjectByType<PlayerManager>();
        RefreshIn(playerInventoryManager.backpack);
        RefreshIn(playerInventoryManager.quickBar);

        void RefreshIn(Container container)
        {
            foreach (ContainerSlot slot in container)
            {
                if (!slot.hasItem) continue;
                // ������ �� ������������ �����:
                slot.isInteractible =
                    // ����� �������
                    !( ( (playerManager.state == PlayerManager.PlayerStates.bartering)
                    // � ������ �������� � �������� �� ���������
                    && (slot.item.tradingCurrency != playerManager.tradingWith.tradingCurrency) )
                    // ���
                    // ����� ��������������� � �����������
                    || ( playerManager.state == PlayerManager.PlayerStates.looting
                    // � �������� ���������������
                    && playerInventoryManager.interactedContainer.config.isSpecialized
                    // � ��� �������� �� �������� � ���� �������� ���������
                    && (slot.item.type != playerInventoryManager.interactedContainer.config.slotsType) ) );
            }

            // � ������������ � ������ ��������������� �������� UI-������
            for (int i = 0; i < this.quickbarGrid.transform.childCount; i++)
            {
                GameObject uiSlot = this.quickbarGrid.transform.GetChild(i).gameObject;
                uiSlot.GetComponent<ItemSlotUI>()?.UpdateInteractivity();
            }
            for (int i = 0; i < this.backpackGrid.transform.childCount; i++)
            {
                GameObject uiSlot = this.backpackGrid.transform.GetChild(i).gameObject;
                uiSlot.GetComponent<ItemSlotUI>()?.UpdateInteractivity();
            }
        }

        //RepopulateContainersAccordingToLayout();
    }

    // ================= ���������� ����� �������� =================
    
    // ������ ��������� ����� �����������
    public void RepopulateContainer(GameObject toUIGrid, Container fromContainer)
    {
        GameObjectTools.DestroyAllChildren(toUIGrid.transform);

        for (int i  = 0; i < fromContainer.Count; i++)
        {
            GameObject cell = Instantiate(containerCellPrefab, toUIGrid.transform);
            ItemSlotUI cellUI = cell.GetComponent<ItemSlotUI>();
            cellUI.containerSlotIndex = i;
            cellUI.relatedContainer = fromContainer;
            cellUI.UpdateSlot();
        }
    }

    // �������������� ������������ ��������� (��� ������� ���������� ������ ����������)
    public void RepopulateBackpack()
    {
        RepopulateContainer(backpackGrid, playerInventoryManager.backpack);
    }
    public void RepopulateInteractedContainer()
    {
        RepopulateContainer(containerInventoryGrid, playerInventoryManager.interactedContainer);
    }

    // ��������� ���������� ���� UI-������������� �����������
    private void PopulateAllPlayerContainers()
    {
        RepopulateContainer(quickbarGrid, playerInventoryManager.quickBar);
        //SetQuickbarCellFocused(playerInventoryManager.activeItemSlotIndex, true);
        RepopulateContainer(backpackGrid, playerInventoryManager.backpack);

        RepopulateContainer(headSlot, playerInventoryManager.headSlot);
        RepopulateContainer(bodySlot, playerInventoryManager.bodySlot);
        RepopulateContainer(feetSlot, playerInventoryManager.feetSlot);
        RepopulateContainer(backpackSlot, playerInventoryManager.backpackSlot);
        RepopulateContainer(specialSlot, playerInventoryManager.specialSlot);
        RepopulateContainer(accessoriesGrid, playerInventoryManager.accessorySlots);

        RepopulateContainer(craftingOptionsGrid, playerInventoryManager.craftingResults);
        if (playerInventoryManager.interactedContainer != null)
            RepopulateContainer(containerInventoryGrid, playerInventoryManager.interactedContainer);

        RepopulateContainer(barterPlayerTransferGrid, playerInventoryManager.barterPlayerTransfer);
        RepopulateContainer(barterTraderTransferGrid, playerInventoryManager.barterTraderTransfer);
    }

    // ================= ���������� ����� ����� =================

    // �������� ��� ����� �����
    private void UpdateUISlots(GameObject gridPanel)
    {
        //for (int i = 0; i < gridPanel.transform.childCount; i++)
        //{
        //    GameObject uiSlot = gridPanel.transform.GetChild(i).gameObject;
        //    uiSlot.GetComponent<ItemSlotUI>()?.UpdateSlot();
        //}

        // ������ ��������� ������ �� ��� ���������� ���������� ������ (�.�. ���������� ���-�� ����� ���������� �� �����)
        if (gridPanel == backpackGrid)
            for (int i = 0; i < playerInventoryManager.bagSlotsCount; i++)
                gridPanel.transform.GetChild(i).GetComponent<ItemSlotUI>()?.UpdateSlot();
        // ��������� ���������� ��������� ���������
        else
            foreach (Transform uiSlot in gridPanel.transform)
                uiSlot.gameObject.GetComponent<ItemSlotUI>()?.UpdateSlot();
    }

    // ���������� ����� ������������ ��������� (��� UnityEvent.Invoke)
    public void UpdateQuickbar()
    {
        UpdateUISlots(quickbarGrid);
    }
    public void UpdateBackpack()
    {
        UpdateUISlots(backpackGrid);
    }
    public void UpdateEquipmentSlots()
    {
        UpdateUISlots(headSlot);
        UpdateUISlots(bodySlot);
        UpdateUISlots(feetSlot);
        UpdateUISlots(backpackSlot);
        UpdateUISlots(specialSlot);
        UpdateUISlots(accessoriesGrid);

        RepopulateBackpack();
    }
    public void UpdateCrafting()
    {
        UpdateUISlots(craftingOptionsGrid);
    }
    public void UpdateBarter()
    {
        UpdateUISlots(barterPlayerTransferGrid);
        UpdateUISlots(barterTraderTransferGrid);
    }

    #endregion

    //====================== ������ ���� �������� ������ ��������� =====================

    // -------------- �������� � ��������� �������� --------------

    // �������� �� ������� ������� ����� �������� (itemSlot)
    public void ShowDescription(ItemSlotUI itemSlot)
    {
        if (isMenuShown) // �� ��������� ��������, ���� ������� ����
            return;

        BasicItemSO item = itemSlot.containerSlot.item;

        // ��������� ���� ������ � ������ ��������
        SetColorByItemRarity(itemDescriptionPanel, item);
        DescriptionPanelHandler panelHandler = serviceLocator.descriptionPanelHandler;
        panelHandler.SetDescriptionFromItemSlot(itemSlot);

        // ���������
        SetActiveWithChilds(itemDescriptionPanel, true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(itemDescriptionPanel.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelHandler.nameSubstratePanel.GetComponent<RectTransform>());

        // ��������� ��������� ������ �������� ��������
        AlignPosition(itemDescriptionPanel, itemSlot.gameObject);
    }
    // ������
    public void HideDescription()
    {
        itemDescriptionPanel.SetActive(false);
    }

    // -------------- ���� ����� �������� --------------

    // �������� �� ������� ������� ����� �������� (itemSlot)
    public void ShowSlotMenu(GameObject itemSlot, Container container, int index)
    {
        // ��������� ������ ����
        ItemMenuHandler menuHandler = itemMenuPanel.GetComponent<ItemMenuHandler>();
        menuHandler.SetData(container, index, itemSlot);
        SetColorByItemRarity(itemMenuPanel, container[index].item);

        // ��������� ����
        isMenuShown = true;
        HideDescription();
        itemMenuPanel.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(itemMenuPanel.GetComponent<RectTransform>());

        // ��������� ��������� ������ ����
        AlignPosition(itemMenuPanel, itemSlot);
    }
    // ������
    public void HideSlotMenu()
    {
        isMenuShown = false;
        itemMenuPanel.SetActive(false);
    }

    // -------------- ���� ���������� ��������� (����������) --------------

    // �������� �� ������� ������� ����� �������� (itemSlot)
    public void ShowSplitterPanel(Container container, int index, GameObject itemSlot)
    {
        // ��������� ������ ����
        ItemSplitterManager menuHandler = itemSplitterPanel.GetComponent<ItemSplitterManager>();
        menuHandler.SetData(container, index);
        SetColorByItemRarity(itemSplitterPanel, container[index].item);

        // ��������� ����
        isMenuShown = true;
        itemSplitterPanel.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(itemSplitterPanel.GetComponent<RectTransform>());

        // ��������� ��������� ������ ����A
        AlignPosition(itemSplitterPanel, itemSlot);
    }
    // ������
    public void HideSplitterPanel()
    {
        isMenuShown = false;
        itemSplitterPanel.SetActive(false);
    }

    // -------------- ������� --------------

    // ��������� ������� ������ ������������ �����
    private Vector3 AlignPosition(GameObject panel, GameObject itemSlot)
    {
        Vector3 position = PanelInstruments.AlignPanel(this.gameObject, panel, itemSlot);
        panel.transform.position = position;
        return position;
    }

    // ��������� ����� ������� �������� �������� �����
    private void SetColorByItemRarity(GameObject panel, BasicItemSO item)
    {
        Image image = panel.GetComponent<Image>();
        image.color = item.rarity.color;
    }

    //====================== ����������� �� ������ =====================

    // ������������ ������ ������� ������ � �������
    public void SetFocusPanelState(bool isActive, Vector3 focusedSlotPosition = default, Vector2 focusPanelSize = default)
    {
        focusedSlotPanel.SetActive(isActive);

        if (isActive)
        {
            Transform focusPanelTransform = focusedSlotPanel.transform;
            focusPanelTransform.position = focusedSlotPosition;
            focusPanelTransform.GetComponent<RectTransform>().sizeDelta = focusPanelSize;
        }
    }

    public int focusedQuickbarSlot { get; private set; }

    // �������� ����� �� ������ ��������
    public void SetQuickbarCellFocused(int cellIndex, bool isFocused)
    {
        focusedQuickbarSlot = cellIndex;

        if (cellIndex < 0)
        {
            SetFocusPanelState(false);
            return;
        }

        // �������� ��������� �� ������� ������
        GridLayoutGroup quickbarGridLayout = quickbarGrid.GetComponent<GridLayoutGroup>();
        Transform quickbarCell = quickbarGridLayout.transform.GetChild(cellIndex);
        ItemSlotUI quickbarCellManager = quickbarCell.GetComponent<ItemSlotUI>();

        // ��������� ������
        quickbarCellManager.SetFocus(isFocused);
        Transform thisSlotTransform = quickbarCellManager.transform;
        SetFocusPanelState(isFocused, thisSlotTransform.position, thisSlotTransform.GetComponent<RectTransform>().sizeDelta);
    }
    //
    public void DefocusFocusedQuickbarSlot()
    {
        SetQuickbarCellFocused(focusedQuickbarSlot, false);
    }

    //====================== ���������� � ���� =====================

    public void ShowTargetInfo(string text)
    {
        if (!targetDescriptionPanel.activeSelf)
            targetDescriptionPanel.SetActive(true);
        targetDescriptionText.text = text;
    }
    public void HideTargetInfo()
    {
        if (targetDescriptionPanel.activeSelf)
            targetDescriptionPanel.SetActive(false);
    }

    // ====================== ������ =======================

    public void SetBarterButtonInteractivity(bool isInteractive)
    {
        barterBalanceButton.interactable = isInteractive;
    }
}
