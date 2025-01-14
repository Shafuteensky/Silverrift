using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemMenuHandler : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UIManager playerUIManager;
    [SerializeField] private GameObject equipButton;
    [SerializeField] private GameObject moveButton;
    [SerializeField] private GameObject useButton;
    [SerializeField] private GameObject splitButton;
    [SerializeField] private GameObject dropButton;

    // ������ � ������ ����������, � ������� ������� ��� ����
    private Container container;
    private int index;
    private ContainerSlot slot;
    private ItemSlotUI slotUI;

    ScriptablesCacher serviceLocator;

    public bool isHovering = false; // ��������� �� ���� � �������� ������ ����

    private void Awake()
    {
        ScriptTools.RegisterScript(this);

        serviceLocator = FindFirstObjectByType<ScriptablesCacher>();
    }

    // ==========================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

    // ==========================================

    public void SetData(Container container, int index, GameObject slotUI)
    {
        this.container = container;
        this.index = index;
        slot = container[index];
        this.slotUI = slotUI.GetComponent<ItemSlotUI>();

        isHovering = false;
        ConfigureButtons();
    }

    private void ConfigureButtons()
    {
        DisableButtons();

        // ��������� ������ ������������� � ����� ��� ��������� � ��������� ��������
        if (container.config.groupTag != containerTag.trader)
        {
            if (serviceLocator.ITEquipables.Contains(slot.item.type) && 
                !serviceLocator.ITEquipables.Contains(slotUI.relatedContainer.config.slotsType))
                equipButton.SetActive(true);

            if (slot.item.type == serviceLocator.ITConsumable)
                useButton.SetActive(true);

            dropButton.SetActive(true);
        }

        splitButton.SetActive(container[index].amount > 1);
        splitButton.GetComponent<Button>().interactable = container.isFull();
        moveButton.SetActive(true);
    }

    private void DisableButtons()
    {
        equipButton.SetActive(false);
        useButton.SetActive(false);
        dropButton.SetActive(false);
        splitButton.SetActive(false);
        moveButton.SetActive(false);
    }

    public void HideMenu()
    {
        isHovering = false;
        playerUIManager.HideSlotMenu();
        //playerUIManager.RepopulateContainersAccordingToLayout();
    }

    // =============  ������ ==============

    public void DropItem()
    {
        container.RemoveAndDropItem(index, slot.amount);

        HideMenu();
    }

    public void EquipItem()
    {
        slotUI.EquipItem(true);

        HideMenu();
    }

    public void UseItem()
    {
        if (slot.item is ConsumableItemSO consumable) // ???????????
            consumable.Use();

        HideMenu();
    }

    public void SplitItem()
    {
        playerUIManager.ShowSplitterPanel(container, index, slotUI.gameObject);

        HideMenu();
    }

    public void MoveItem()
    {
        slotUI.AutoMoveItem();

        HideMenu();
    }
}
