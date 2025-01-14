using BehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using static LeTai.Asset.TranslucentImage.VPMatrixCache;
using static UnityEditor.Progress;
using static UnityEngine.Rendering.STP;
using Object = UnityEngine.Object;

public class ContainerManager : MonoBehaviour, IInteractable
{
    public static ServiceLocator serviceLocator { get; private set; }

    [field: SerializeField] public ItemContainerConfigSO containerConfig {  get; private set; }
    // Функция, выполняющаяся при изменении наполнения конейнера (передача ее контейнеру)
    [field: SerializeField] public ManagedUnityEvent OnChangeContentsEvent { get; private set; } 
    public Container container { get; private set; }

    public bool isOpenedOnce { get; private set; } = false;

    void OnEnable()
    {
        container = new Container(containerConfig, this.name, this); // !!! ПЕРЕНЕСТИ В INTERACT(), ЧТОБЫ НЕ СОЗДАВАТЬ КОНЕЙНЕР ПРЕДВАРИТЕЛЬНО

        // Добавляем события менеджера поверх событий конейнера
        foreach (var listener in OnChangeContentsEvent.GetListeners())
            container.OnChangeContentsEvent.AddListener(listener);

        this.gameObject.layer = LayerMask.NameToLayer("Containers");
    }

    private void Start()
    {
        if (containerConfig.itemsInIt.Count > 0)
            GenerateLootingStash(); // TEST !!!

        serviceLocator = ServiceLocator.Instance;
    }

    private void OnDestroy()
    {
        if (containerConfig.isPermanent)
        {
            // СОХРАНИТЬ
        }
        else
        {
            if (serviceLocator.uiManager.currentLayout == UIManager.UILayouts.LootingContainer)
                serviceLocator.uiManager.SwitchLayout(UIManager.UILayouts.StandardGameplayOverlay);
            container.Clear();
        }
    }

    // =============== IInteractable ===============

    public void Interact()
    {
        // Задаем контейнер, с которым взаимодействуем
        serviceLocator.playerInventoryManager.SetInteractedContainer(container);

        // Если контейнер не принадлежит торговцу
        if (this.containerConfig.groupTag != containerTag.trader)
        {
            // Игрок входит в состояния лутинга
            serviceLocator.playerManager.SetPlayerState(PlayerManager.PlayerStates.looting);

            // Открыть меню просмотра содержимого контейнера
            serviceLocator.uiManager.SetLayoutLooting();
            serviceLocator.uiManager.RepopulateInteractedContainer();
            //serviceLocator.uiManager.RepopulateLooting();
        }

        isOpenedOnce = true;
    }

    public void Highlight(HighlightPlus.HighlightProfile profile)
    {
        if (this.TryGetComponent(out HighlightPlus.HighlightEffect highlightScript))
        {
            highlightScript.profile = profile;
            highlightScript.highlighted = true;
        }
    }

    public void RemoveHighlight()
    {
        if (this.TryGetComponent(out HighlightPlus.HighlightEffect highlightScript))
        {
            highlightScript.highlighted = false;
        }
    }

    public List<string> GetInfo()
    {
        List<string> info = new List<string>();
        info.Add(containerConfig.nameString.GetLocalizedString());

        var locData = LocalizationSettings.StringDatabase;
        string containerType = locData.GetLocalizedString("Containers", "CONTAINER_ALLOWED_TYPE");
        if (containerConfig.isSpecialized)
            info.Add(containerType + ": " + containerConfig.slotsType.nameString.GetLocalizedString());

        return info;
    }

    // ================ Генерация содержимого (лута, пула торговца) ================

    private void GenerateLootingStash()
    {
        if (this.container.isEmpty())
            foreach (var item in containerConfig.itemsInIt)
                this.container.AddItem(item, item.stackSize);
    }
}










// **************************** Container ********************************

public class Container: List<ContainerSlot>
{
    public string name {  get; private set; }
    public ItemContainerConfigSO config { get; private set; } // Отсюда берутся данные для инициализации
    public ManagedUnityEvent OnChangeContentsEvent = new ManagedUnityEvent(); // Функция, выполняющаяся при изменении наполнения конейнера

    private bool isManaged = false;
    private ContainerManager relaitedManager;

    public Container(ItemContainerConfigSO newContainerConfig, string name = "Unnamed Container", 
        ContainerManager containerManager = null)
    {
        config = newContainerConfig;
        this.name = name;

        if (containerManager != null)
        {
            isManaged = true;
            relaitedManager = containerManager;
        }

        addSlots(config.numberOfSlots);
    }

    // При изменении содержимого конейнера (перемещение, добавление, удаление)
    public void OnChangeContents()
    {
        // Выполнение событий из списка, заданного в editor-е
        if (OnChangeContentsEvent.GetListenerCount() > 0)
            OnChangeContentsEvent.Invoke();

        // Освобождать из памяти (уничтожать) контейнер и его менеджер после открытия (при соблюдении условий)
        if (isManaged && config.destroyIfEmpty && relaitedManager.isOpenedOnce && this.isEmpty())
            Object.Destroy(relaitedManager);
    }

    private void addSlots(int slotsAmount)
    {
        for (int i = 0; i < slotsAmount; i++)
        {
            ContainerSlot emptySlot = new ContainerSlot(this);
            emptySlot.ID = i;
            Add(emptySlot);
        }
    }

    // ================== Добавление содержимого ==================

    // Добавление предмета в контейнер (поднятие с земли в инвентарь игрока, получение награды)
    public int AddItem(BasicItemSO item, int amount, bool dropRemainsIfNoSpace = false, 
        bool onlyCreateStacks = false) // Возвращает остаток (количество)
    {
        if (((amount <= 0) || item == null) || !IsSlotTypeCompatible(item, this))
            return 0;

        int remainingAmount = amount;

        // Добавление в существующий стак – просмотр всех слотов на наличие такого же предмета
        if (!onlyCreateStacks)
            remainingAmount = FillStacks(this, remainingAmount, item);
        // Существующие стаки переполнены или отсутствуют – заполнить новую ячейку
        remainingAmount = CreateStacks(this, remainingAmount, item); 

        if ((remainingAmount > 0) && dropRemainsIfNoSpace) // Не все предметы распределены (нет места) – скинуть остатки на землю
            item.DropUnderPlayer(remainingAmount, true);

        OnChangeContents();

        if (remainingAmount >= 1)
            return remainingAmount;
        else
            return 0;
    }

    public void AddItem(BasicItemSO item, int amount, int index)
    {
        this[index].hasItem = true;
        this[index].item = item;
        this[index].amount = amount;

        OnChangeContents();
    }

    // ================== Стаки ===================

    private int CreateStacks(Container container, int amount, BasicItemSO item) // Возвращает количество нераспределенных предметов
    {
        if (amount <= 0) // Если не все предметы распределены по существующим стакам
            return 0;

        for (int i = 0; i < container.Count; i++)
        {
            // Если в инвентаре есть незанятая ячейка, то положить предмет в нее
            if (!container[i].hasItem)
            {
                ContainerSlot newItem = new ContainerSlot(container);
                newItem.hasItem = true;
                newItem.item = item;

                // Если amount к добавлению предметов менее стака – создать неполный стак
                if (amount <= item.stackSize)
                {
                    newItem.amount = amount;
                    container[i] = newItem;
                    amount = 0;
                }
                // Более стака – создать полный стак
                else
                {
                    newItem.amount = item.stackSize;
                    container[i] = newItem;
                    amount -= item.stackSize;
                }
            }

            // Если все добавляемые предметы (их количество) распределены, то окончить просмотр ячеек
            if (amount == 0)
                break;
        }

        return amount;
    }

    private int FillStacks(Container container, int amount, BasicItemSO item)
    {
        if (amount <= 0) // Если не все предметы распределены по существующим стакам
            return 0;

        foreach (ContainerSlot slot in container)
            if ((slot.hasItem) && (slot.item == item))
                if (slot.amount < item.stackSize)
                    while ((slot.amount < item.stackSize) && (amount > 0))
                    {
                        slot.amount++;
                        amount--;
                    }

        return amount;
    }

    // ============== Перемещение содержимого ==============

    // Перемещение предмета из ячейки этого контейнера в другой контейнер
    // Перемещение через SHIFT (без указания целевой ячейки)
    public bool MoveItemTo(Container toContainer, int fromSlotIndex, int amountToMove, bool swapFirstIfHasItem = false) 
    {
        if ((this[fromSlotIndex].hasItem == false) || !IsSlotTypeCompatible(this[fromSlotIndex], toContainer))
            return false;

        bool isItemMoved = false;
        ContainerSlot fromSlot = this[fromSlotIndex];

        // Проверка всех ячеек контейнера на наличие такого-же предмета
        int remainedAmount = FillStacks(toContainer, amountToMove, fromSlot.item);
        remainedAmount = CreateStacks(toContainer, remainedAmount, fromSlot.item);
        remainedAmount = fromSlot.amount - (fromSlot.amount - remainedAmount);
        if (remainedAmount <= 0)
        {
            if ((fromSlot.amount - amountToMove) == 0)
                fromSlot.Empty();
            else
                fromSlot.amount -= amountToMove;
            isItemMoved = true;
        }
        else 
            fromSlot.amount = remainedAmount;

        // Предмет все еще не перемещен и активен флаг помещения в первую ячейку – поместить в первую ячейку (для экипировки)
        if (!isItemMoved && swapFirstIfHasItem)
        {
            this.MoveItemTo(toContainer, fromSlotIndex, remainedAmount, 0);

            isItemMoved = true;
        }
        else
            OnChangeContents();

        if (toContainer != this)
            toContainer.OnChangeContents();
        return isItemMoved;
    }

    // Основной метод перемещения (из ячейки в ячейку). Перемещение через перетаскивание 
    public bool MoveItemTo(Container toContainer, int fromSlotIndex, int amount, int toSlotIndex)
    {
        ContainerSlot fromSlot = this[fromSlotIndex];
        ContainerSlot toSlot = toContainer[toSlotIndex];

        // Типы слотов совпадают – продолжить
        if ((this[fromSlotIndex].item == null) || !IsSlotTypeCompatible(this[fromSlotIndex], toContainer)
            // Тэги одинаковые (чтобы нельзя было перемещать предметы между рюкзаком игрока и инвентарем торговца)
            || ( (this.config.groupTag == containerTag.trader || toContainer.config.groupTag == containerTag.trader)
            && this.config.groupTag != toContainer.config.groupTag)
            // Слоты существуют
            || (fromSlot == null || toSlot == null)) 
            return false;

        // Если перенос не в контейнер – скинуть предмет под ноги
        if (toContainer == null)
        {
            // ТУТ ЗАПРОС КОЛИЧЕСТВА СКИДЫВАЕМОГО ПРЕДМЕТА
            RemoveItem(fromSlotIndex, fromSlot.amount);
            fromSlot.item.DropUnderPlayer(fromSlot.amount, true);
            return false;
        }

        // Перемещение предмета в заполненный слот 
        if (toSlot.hasItem) 
        {
            // Предметы одинаковые и стак неполный
            if (toSlot.item == fromSlot.item)
            {
                // Сумма предметов не превышает максимум стака – объединить
                if ((toSlot.amount + amount) <= toSlot.item.stackSize)
                {
                    toSlot.amount += fromSlot.amount;
                    fromSlot.Empty();
                }
                // Превышает – переместить сколько вмещается, остальное оставить
                else
                {
                    int transferAmount = toSlot.item.stackSize - toSlot.amount;
                    toSlot.amount = toSlot.item.stackSize;
                    fromSlot.amount -= transferAmount;
                }
            }

            //Предмет В подходит по типу слоту ИЗ – поменять их местами
            else if (IsSlotTypeCompatible(toContainer[toSlotIndex], this)) 
                fromSlot.SwapWithAnother(toSlot);

            if (toContainer != this)
                toContainer.OnChangeContents();
            OnChangeContents();
            return true;
        }
        // Перемещение предмета в пустой слот
        else
        {
            // ТУТ ЗАПРОС КОЛИЧЕСТВА ПЕРЕМЕЩАЕМОГО ПРЕДМЕТА
            MoveItemToEmptySlot(fromSlotIndex, amount, toContainer, toSlotIndex);
            return true;
        }

    }

    // Перемещение в ячейку, принятую за пустую (не содержит предмет; hasItem = false)
    private void MoveItemToEmptySlot(int fromSlotIndex, int amount, Container toContainer, 
        int toSlotIndex)
    {
        ContainerSlot fromSlot = this[fromSlotIndex];
        ContainerSlot toSlot = toContainer[toSlotIndex];

        toSlot.AddItem(fromSlot.item, fromSlot.amount);
        RemoveItem(fromSlotIndex, amount);

        if (toContainer != this)
            toContainer.OnChangeContents();
        OnChangeContents();
    }

    // Переместить все предметы из этого контейнера в другой
    public int MoveAllItemsTo(Container toContainer, bool addNewSlotsIfNoSpace = false)
    {
        // Создать новые слоты в toContainer, если все предметы не помещаются
        if (addNewSlotsIfNoSpace)
        {
            int slotsLackAmount = this.GetOccupiedSlotsCount() - toContainer.GetEmptySlotsCount();
            if (slotsLackAmount > 0)
                toContainer.addSlots(slotsLackAmount);
        }

        int remainingItems = this.GetOccupiedSlotsCount();

        // Перемещение каждого предмета
        for (int i = 0; i < this.Count(); i++) // По всем ячейкам этого контейнера
        {
            // Обработать следующий слот если в этом нет предмета
            if (!this[i].hasItem)
                continue;

            // Есть предмет – попытаться переместить
            if (this.MoveItemTo(toContainer, i, this[i].amount))
            {
                remainingItems -= 1;
            }
        }

        return remainingItems;
    }

    // =============================

    public void SplitItem(int index, int newStackSize, bool dropRemainsIfNoSpace, Container containerToAddSplittedTo)
    {
        ContainerSlot slot;
        slot = this[index];

        if (newStackSize < 1 || newStackSize >= slot.item.stackSize)
            return;

        RemoveItem(index, newStackSize);
        containerToAddSplittedTo.AddItem(slot.item, newStackSize, dropRemainsIfNoSpace, true);

        if (containerToAddSplittedTo != this)
            containerToAddSplittedTo.OnChangeContents();

        OnChangeContents();
    }

    public void RemoveItem(int index, int amount)
    {
        this[index].RemoveItem(amount);

        OnChangeContents();
    }

    public void RemoveAndDropItem(int index, int amount, bool isPhysical = true)
    {
        this[index].item.DropUnderPlayer(amount, isPhysical);
        this[index].RemoveItem(amount);

        OnChangeContents();
    }

    public void DropAllItems(bool isPhysical = true)
    {
        foreach (ContainerSlot slot in this)
        {
            if (slot.hasItem)
            {
                slot.item.DropUnderPlayer(slot.amount, isPhysical);
                slot.Empty();
            }
        }

        OnChangeContents();
    }

    // ================= Логика =================

    // Совместимы ли предмет из слота с принимаемым типом предметов конейнера toContainer
    private bool IsSlotTypeCompatible(ContainerSlot slot, Container toContainer)
    {
        // Не специализированный (принимает любой тип) – совместим
        if (!toContainer.config.isSpecialized) 
            return true;
        // Специализирован и типы одинаковы – совместим
        else if (toContainer.config.slotsType == slot.item.type) 
            return true;
        // Специализирован и типы различны – не совместим
        else
            return false;
    }

    // Совместимы ли тип предмета с принимаемым типом предметов конейнера toContainer
    private bool IsSlotTypeCompatible(BasicItemSO item, Container toContainer)
    {
        if (!toContainer.config.isSpecialized)
            return true;
        else if (toContainer.config.slotsType == item.type)
            return true;
        else
            return false;
    }

    public bool isEmpty()
    {
        foreach (var item in this)
            if (item.hasItem)
                return false;
        return true;
    }
    public bool isFull()
    {
        foreach (var item in this)
            if (!item.hasItem)
                return true;
        return false;
    }

    private int GetEmptySlotsCount()
    {
        int emptySlotCount = 0;

        foreach (ContainerSlot slot in this)
        {
            if (slot.hasItem)
                continue;

            emptySlotCount += 1;
        }

        return emptySlotCount;
    }
    private int GetOccupiedSlotsCount()
    {
        int occupiedSlotCount = 0;

        foreach (ContainerSlot slot in this)
        {
            if (!slot.hasItem)
                continue;

            occupiedSlotCount += 1;
        }

        return occupiedSlotCount;
    }

    // ========== DEBUG ============

    public void PrintContains()
    {
        string items = this.ToString() + " contains: ";
        foreach (ContainerSlot slot in this)
        {
            if (slot.hasItem)
                items += slot.item.ToString();
            else items += "null";
            items += ", ";
        }
        Debug.Log(items);
    }
}

// ***************************** ContainerSlot **********************************

public class ContainerSlot : ICloneable
{
    public int ID = 0;
    public bool hasItem = false;
    public BasicItemSO item = null;
    public int amount = 0;
    public bool isInteractible = true;
    public readonly Container relatedContainer;

    public ContainerSlot(Container relatedContainer)
    {
        ID = 0; // В будущем поможет сортировать в своем порядке (?)
        this.Empty();
        this.relatedContainer = relatedContainer;
    }

    public object Clone()
    {
        return this;
    }

    // Запретить использование "="
    private ContainerSlot(ContainerSlot other)
    {
        throw new NotSupportedException("Assignment is not allowed!");
    }

    // =======================================

    // Добавляет определенный предмет в слот
    public void AddItem(BasicItemSO item, int amount) 
    {
        if (amount <= 0 || item == null)
            return;

        this.hasItem = true;
        this.item = item;
        this.amount = amount;
    }

    // Очищает слот если удалено все количество, отнимает часть если не все 
    public void RemoveItem(int amount) 
    {
        if (amount <= 0)
            return;

        if (amount >= item.stackSize || (this.amount-amount) == 0)
            this.Empty();
        else
            this.amount -= amount;
    }

    // Обнуляет слот до состояния пустого (без предмета)
    public void Empty() 
    {
        this.item = null;
        this.amount = 0;
        this.hasItem = false;
    }

    // Полностью меняет местами слоты
    public void SwapWithAnother(ContainerSlot slot) 
    {
        BasicItemSO transferItem = this.item;
        int transferAmount = this.amount;
        bool transferHasItem = this.hasItem;

        this.item = slot.item;
        this.amount = slot.amount;
        this.hasItem = slot.hasItem;

        slot.item = transferItem;
        slot.amount = transferAmount;
        slot.hasItem = transferHasItem;
    }
}
