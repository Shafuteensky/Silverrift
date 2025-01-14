using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UIManager;
using static UnityEditor.Progress;

public class ItemSlotUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
    IPointerClickHandler
{
    [SerializeField] private GameObject rarityPanel;
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private GameObject amountPanel;
    [SerializeField] private GameObject itemButton;

    [SerializeField] private static UIManager uiManager;
    private static PlayerInventoryManager inventoryManager;

    private Image rarityImage;
    private Image itemImage;
    private TextMeshProUGUI amountMesh;

    public Container relatedContainer;
    public int containerSlotIndex;
    public ContainerSlot containerSlot;
    private static ItemSlotUI draggingFrom;

    public bool isInteractible = true; // "Graying out" ���� ���
    public bool isFocused = false; 

    // ������� ����
    static float lastClick = 0f;
    static float interval = 0.3f;

    public ManagedUnityEvent onUpdateEvents { get; private set; } = new ManagedUnityEvent();

    private void Start()
    {
        uiManager = ServiceLocator.Instance.uiManager;
        if (isFocused)
            SetFocus(true);
    }

    private void OnEnable()
    {
        inventoryManager = ServiceLocator.Instance.playerInventoryManager;
        if (relatedContainer == inventoryManager.quickBar
            && this.containerSlotIndex == inventoryManager.activeItemSlotIndex)
            SetFocus(true);
    }

    private void OnDisable()
    {
        if (isFocused)
            SetFocus(false);
    }

    // ======= Hover ========

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractible)
            return;

        SetFocus(true);

        ContainerSlot itemSlot = relatedContainer[containerSlotIndex];
        if (itemSlot.hasItem)
        {
            BasicItemSO item = itemSlot.item;
            if (!uiManager.isDragging)
                uiManager.ShowDescription(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInteractible)
            return;

        SetFocus(false);

        uiManager.HideDescription();
    }

    // ======= Down/Up ========

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractible)
            return;

        // � ������ �������������� ����� �� ��������
        if (eventData.dragging)
            return;

        // ��� - ������� ���� ������ � ��������� � ������
        if (eventData.button == PointerEventData.InputButton.Right) 
        {
            if (!containerSlot.hasItem)
                return;

            uiManager.processingSlot = this.containerSlot;
            uiManager.ShowSlotMenu(this.gameObject, relatedContainer, containerSlotIndex);
        }

        // ���
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Shift+��� ��� ������� ���� � ���������������
            if ( Input.GetKey(KeyCode.LeftShift) || ((lastClick + interval) > Time.time) )
            {
                AutoMoveItem();
                uiManager.HideDescription();
            }

            //else // ��������� ���� (���������� ��� �� � ����� �������)
            //    Debug.Log("single click");
            lastClick = Time.time;
        }

        eventData.Reset();
    }

    // ======= Drag And Drop ========

    public void OnBeginDrag(PointerEventData eventData) // ����� ������� � ��������-����
    {
        if (!isInteractible)
            return;

        if (eventData.button != PointerEventData.InputButton.Left || // ������ ���
            this.containerSlot == null || !this.containerSlot.hasItem) // � ����� ������ ���� �������
            return;

        // ������ �������� ��� �������������� � ������������
        uiManager.draggedItemImagePanel.SetActive(true);
        uiManager.draggedItemImagePanel.GetComponent<Image>().sprite = this.itemImage.sprite;

        SetElementsVisibility(false);
        uiManager.isDragging = true;
        draggingFrom = this;
        uiManager.originalContainer = this.relatedContainer;
        uiManager.originalIndex = this.containerSlotIndex;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isInteractible)
            return;

        uiManager.draggedItemImagePanel.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData) // � ����� ������ ���� �������
    {
        if (!this.containerSlot.hasItem)
            return;

        SetElementsVisibility(true);
        uiManager.draggedItemImagePanel.SetActive(false); // ������ �������� � ���������
        uiManager.isDragging = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!isInteractible)
            return;

        if (eventData.button != PointerEventData.InputButton.Left) // ������ ���
            return;

        Container fromContainer = uiManager.originalContainer; // ���������, �� �������� ������ ���������� �������
        int fromIndex = uiManager.originalIndex;

        if (fromContainer == null || !fromContainer[fromIndex].hasItem) // � ����� ������ ��� ���� �������
            return;

        // ���� ���������� ����������� �� ��������� � ���������
        if (fromContainer.MoveItemTo(this.relatedContainer, fromIndex, fromContainer[fromIndex].amount, this.containerSlotIndex))
        { 
            // ���� ��/� ������ ����������
            if (this.relatedContainer.config.groupTag == containerTag.equipment
                || draggingFrom.relatedContainer.config.groupTag == containerTag.equipment)
                // ������������� ���������� ����� ��������� ������
                uiManager.playerInventoryManager.RecalculateBackpackSlotsCount(); 
        }

        uiManager.draggedItemImagePanel.SetActive(false); // ������ �������� � ���������
        uiManager.isDragging = false;
    }

    // =============== ������� ����� ===============

    // �������������� �������� (������� ���� ��� Shift+����)
    public void AutoMoveItem()
    {
        PlayerInventoryManager inventory = ServiceLocator.Instance.playerInventoryManager;

        switch (uiManager.currentLayout)
        {
            // ���� ���������� � ����� ����������, ���������, �������
            case UILayouts.PlayerEquipment:
                {
                    EquipItem();
                    break;
                }
            // ����� � ������ � �������, ���� ������
            case UILayouts.CraftMenu:
                {
                    if (this.relatedContainer == inventory.backpack || this.relatedContainer == inventory.quickBar)
                        MoveItemTo(inventory.craftingField);
                    else
                        MoveToPlayer();
                    break;
                }
            // �������� (������) �������� ���������� � ������ � �������, ��������� 
            case UILayouts.LootingContainer:
                {
                    if (this.relatedContainer == inventory.backpack || this.relatedContainer == inventory.quickBar)
                        MoveItemTo(inventory.interactedContainer);
                    else
                        MoveToPlayer();
                    break;
                }
            // ������ � ������ � �������, ������� ��������, ��������� ��������, �������� ������ 
            case UILayouts.Barter:
                {
                    if (this.relatedContainer == inventory.backpack || this.relatedContainer == inventory.quickBar)
                        MoveItemTo(inventory.barterPlayerTransfer);
                    else if (this.relatedContainer == inventory.barterTraderTransfer)
                        MoveItemTo(inventory.interactedContainer);
                    else if (this.relatedContainer == inventory.interactedContainer)
                        MoveItemTo(inventory.barterTraderTransfer);
                    else
                        MoveToPlayer();
                    break;
                }
        }

        void MoveToPlayer()
        {
            if (!MoveItemTo(inventory.backpack))
                MoveItemTo(inventory.quickBar);
        }
    }

    // ����������� ������� (��� ����������) �� ��������� � UI ������
    private bool MoveItemTo(Container toContainer)
    {
        return this.relatedContainer.MoveItemTo(toContainer, this.containerSlotIndex, this.containerSlot.amount);
    }

    // ����������� (����������� � ���� ����������) ������� �� ��������� � UI ������
    public void EquipItem(bool equipFromAnyContainer = false)
    {
        if (!uiManager.playerInventoryManager.MoveItemInEquipment(this.relatedContainer, this.containerSlotIndex, 
            equipFromAnyContainer))
            // ����������� ����� ���� ������� ��������� ����� ����������� � ����������
            uiManager.playerInventoryManager.RecalculateBackpackSlotsCount();
    }

    // ==============================

    public void test()
    {
        Debug.Log(this.relatedContainer);
        Debug.Log(this.containerSlotIndex);
        if (containerSlot.hasItem)
        {
            Debug.Log(containerSlot.item.name);
            Debug.Log(containerSlot.amount);
        }
    }

    // �������� ������ ������ (����������: ������� �������� � ������)
    public void UpdateSlot()
    {
        rarityImage = rarityPanel.GetComponent<Image>();
        itemImage = itemPanel.GetComponent<Image>();
        amountMesh = amountPanel.GetComponent<TextMeshProUGUI>();

        containerSlot = relatedContainer[containerSlotIndex];

        if (containerSlot.hasItem)
        {
            itemImage.sprite = containerSlot.item.image;
            rarityImage.sprite = containerSlot.item.rarity.image;
            rarityImage.color = containerSlot.item.rarity.color;
            SetElementsVisibility(true);

            if (containerSlot.amount == 1)
                amountPanel.SetActive(false); 
            else
            {
                amountPanel.SetActive(true);
                amountMesh.text = containerSlot.amount.ToString();
            }
        }
        else
        {
            SetElementsVisibility(false);
        }

        UpdateInteractivity();

        // ���������� ������� �� ������, ��������� � editor-�
        onUpdateEvents.Invoke();
    }

    // �������� ��������������� ������ �� ��������� �� ���������-�����
    public void UpdateInteractivity()
    {
        isInteractible = containerSlot.isInteractible;

        if (isInteractible)
        {
            float alpha = 1f;
            SetAlpha(ref itemImage, alpha);
            SetAlpha(ref rarityImage, alpha);
        }
        else
        {
            float fadedAlpha = 0.33f;
            SetAlpha(ref itemImage, fadedAlpha);
            SetAlpha(ref rarityImage, fadedAlpha);
        }

        // ��������� �����-���������� RGBA �����������
        static void SetAlpha(ref Image image, float alpha)
        {
            Color newColor = new Color(image.color.r, image.color.g, image.color.b, alpha);
            image.color = newColor;
        }
    }

    // ��������� ��������� ��������� ����������� �������� (���� ��� �������� � ������ � ���������� �� �����)
    private void SetElementsVisibility(bool isVisible, bool affectItemImage = true)
    {
        if (affectItemImage)
            itemImage.enabled = isVisible;

        rarityImage.enabled = isVisible;

        if (isVisible && this.containerSlot.amount > 1)
            amountPanel.SetActive(true);
        else
            amountPanel.SetActive(false);
    }

    // ���������� ����� �� ������
    public void SetFocus(bool isFocused)
    {
        this.isFocused = isFocused;
        Transform thisSlotTransform = this.transform; 

        // ����� � ���������� ������� ������
        if (isFocused && !uiManager.isDragging)
        {
            Vector3 upscaled = new Vector3(1.2f, 1.2f, 1.2f);
            thisSlotTransform.localScale = upscaled;
        }
        // ������ ������ � ����������� ��������� ������� ������
        else if (!(relatedContainer == inventoryManager.quickBar // ������ ���� ��� �� �������� ������ ��������
                && containerSlotIndex == inventoryManager.activeItemSlotIndex))
        {
            Vector3 initialScale = Vector3.one;
            thisSlotTransform.localScale = initialScale;
        }
    }
}
