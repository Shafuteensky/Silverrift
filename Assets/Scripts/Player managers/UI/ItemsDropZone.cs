using UnityEngine;
using UnityEngine.EventSystems;

public class ItemsDropZone : InitializableMonoBehaviour, IDropHandler
{
    private UIManager uiManager;

    private void Awake()
    {
        ScriptTools.RegisterScript(this);
    }

    private ServiceLocator serviceLocator;

    private void Start()
    {
        serviceLocator = ServiceLocator.Instance;

        uiManager = serviceLocator.uiManager;

        if (AreDependenciesInitialized(uiManager))
            Initialize();
    }

    // ================= Drop =================

    public void OnDrop(PointerEventData eventData)
    {
        if (!IsInitialized(true)) return;

        if (eventData.button != PointerEventData.InputButton.Left) // Только ЛКМ
            return;

        if (!uiManager.isDragging)
            return;
        Container fromContainer = uiManager.originalContainer; // Контейнер, из которого начали перемещать предмет
        int fromIndex = uiManager.originalIndex;
        ContainerSlot slot = fromContainer[fromIndex];

        if (fromContainer == null || !slot.hasItem) // В слоте должен был быть предмет
            return;

        if (fromContainer.config.groupTag == containerTag.trader) // Трейдерские предметы нельзя выбрасывать
            return;

        fromContainer.RemoveAndDropItem(fromIndex, slot.amount);

        uiManager.playerInventoryManager.RecalculateBackpackSlotsCount();
        //uiManager.RepopulatePlayerInv();

        uiManager.draggedItemImagePanel.SetActive(false); // Иконка предмета – отключить
        uiManager.isDragging = false;
    }
}
