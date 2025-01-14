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

    public bool isInteractible = true; // "Graying out" если нет
    public bool isFocused = false; 

    // ƒвойной клик
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

        // ¬ режими перетаскивани€ клики не работают
        if (eventData.dragging)
            return;

        // ѕ ћ - открыть меню работы с предметом в €чейке
        if (eventData.button == PointerEventData.InputButton.Right) 
        {
            if (!containerSlot.hasItem)
                return;

            uiManager.processingSlot = this.containerSlot;
            uiManager.ShowSlotMenu(this.gameObject, relatedContainer, containerSlotIndex);
        }

        // Ћ ћ
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Shift+Ћ ћ или двойной клик Ц автоперемещение
            if ( Input.GetKey(KeyCode.LeftShift) || ((lastClick + interval) > Time.time) )
            {
                AutoMoveItem();
                uiManager.HideDescription();
            }

            //else // ќдиночный клик (происходит так же и перед двойным)
            //    Debug.Log("single click");
            lastClick = Time.time;
        }

        eventData.Reset();
    }

    // ======= Drag And Drop ========

    public void OnBeginDrag(PointerEventData eventData) // ¬з€ть предмет в трансфер-слот
    {
        if (!isInteractible)
            return;

        if (eventData.button != PointerEventData.InputButton.Left || // “олько Ћ ћ
            this.containerSlot == null || !this.containerSlot.hasItem) // ¬ слоте должен быть предмет
            return;

        // »конка предмета при перетаскивании Ц активировать
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

    public void OnEndDrag(PointerEventData eventData) // ¬ слоте должен быть предмет
    {
        if (!this.containerSlot.hasItem)
            return;

        SetElementsVisibility(true);
        uiManager.draggedItemImagePanel.SetActive(false); // »конка предмета Ц отключить
        uiManager.isDragging = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!isInteractible)
            return;

        if (eventData.button != PointerEventData.InputButton.Left) // “олько Ћ ћ
            return;

        Container fromContainer = uiManager.originalContainer; //  онтейнер, из которого начали перемещать предмет
        int fromIndex = uiManager.originalIndex;

        if (fromContainer == null || !fromContainer[fromIndex].hasItem) // ¬ слоте должен был быть предмет
            return;

        // ≈сли получилось переместить из  онейнера в  онтейнер
        if (fromContainer.MoveItemTo(this.relatedContainer, fromIndex, fromContainer[fromIndex].amount, this.containerSlotIndex))
        { 
            // драг из/в €чейку экипировки
            if (this.relatedContainer.config.groupTag == containerTag.equipment
                || draggingFrom.relatedContainer.config.groupTag == containerTag.equipment)
                // ѕерерасчитать количество €чеек инвентар€ игрока
                uiManager.playerInventoryManager.RecalculateBackpackSlotsCount(); 
        }

        uiManager.draggedItemImagePanel.SetActive(false); // »конка предмета Ц отключить
        uiManager.isDragging = false;
    }

    // =============== ѕредмет слота ===============

    // јвтопермещение предмета (двойной клик или Shift+клик)
    public void AutoMoveItem()
    {
        PlayerInventoryManager inventory = ServiceLocator.Instance.playerInventoryManager;

        switch (uiManager.currentLayout)
        {
            // ћеню экипировки Ц слоты экипировки, инвентарь, квикбар
            case UILayouts.PlayerEquipment:
                {
                    EquipItem();
                    break;
                }
            //  рафт Ц рюкзак и квикбар, поле крафта
            case UILayouts.CraftMenu:
                {
                    if (this.relatedContainer == inventory.backpack || this.relatedContainer == inventory.quickBar)
                        MoveItemTo(inventory.craftingField);
                    else
                        MoveToPlayer();
                    break;
                }
            // ќткрытие (лутинг) мирового контейнера Ц рюкзак и квикбар, контейнер 
            case UILayouts.LootingContainer:
                {
                    if (this.relatedContainer == inventory.backpack || this.relatedContainer == inventory.quickBar)
                        MoveItemTo(inventory.interactedContainer);
                    else
                        MoveToPlayer();
                    break;
                }
            // Ѕартер Ц рюкзак и квикбар, трасфер торговца, инвентарь торговца, трансфер игрока 
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

    // ѕереместить предмет (все количество) из св€занной с UI €чейки
    private bool MoveItemTo(Container toContainer)
    {
        return this.relatedContainer.MoveItemTo(toContainer, this.containerSlotIndex, this.containerSlot.amount);
    }

    // Ёкипировать (переместить в слот экипировки) предмет из св€занной с UI €чейки
    public void EquipItem(bool equipFromAnyContainer = false)
    {
        if (!uiManager.playerInventoryManager.MoveItemInEquipment(this.relatedContainer, this.containerSlotIndex, 
            equipFromAnyContainer))
            // ѕересчитать слоты если предмет перемещен между экипировкой и инвентарем
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

    // ќбновить данные €чейки (визуальные: спрайты предмета в €чейке)
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

        // ¬ыполнение событий из списка, заданного в editor-е
        onUpdateEvents.Invoke();
    }

    // ќбновить инетрактивность €чейки по состо€нию ее контейнер-слота
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

        // »зменение альфа-компоненты RGBA изображени€
        static void SetAlpha(ref Image image, float alpha)
        {
            Color newColor = new Color(image.color.r, image.color.g, image.color.b, alpha);
            image.color = newColor;
        }
    }

    // »зменение видимости компонент отображени€ предмета (если нет предмета в €чейке Ц отображать не нужно)
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

    // ¬изуальный фокус на €чейке
    public void SetFocus(bool isFocused)
    {
        this.isFocused = isFocused;
        Transform thisSlotTransform = this.transform; 

        // ‘окус Ц увеличение размера €чейки
        if (isFocused && !uiManager.isDragging)
        {
            Vector3 upscaled = new Vector3(1.2f, 1.2f, 1.2f);
            thisSlotTransform.localScale = upscaled;
        }
        // —н€тие фокуса Ц возвращение исходного размера €чейки
        else if (!(relatedContainer == inventoryManager.quickBar // “олько если это не активна€ €чейка квикбара
                && containerSlotIndex == inventoryManager.activeItemSlotIndex))
        {
            Vector3 initialScale = Vector3.one;
            thisSlotTransform.localScale = initialScale;
        }
    }
}
