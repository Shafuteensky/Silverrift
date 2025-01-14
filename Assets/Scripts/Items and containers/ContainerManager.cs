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
    // �������, ������������� ��� ��������� ���������� ��������� (�������� �� ����������)
    [field: SerializeField] public ManagedUnityEvent OnChangeContentsEvent { get; private set; } 
    public Container container { get; private set; }

    public bool isOpenedOnce { get; private set; } = false;

    void OnEnable()
    {
        container = new Container(containerConfig, this.name, this); // !!! ��������� � INTERACT(), ����� �� ��������� �������� ��������������

        // ��������� ������� ��������� ������ ������� ���������
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
            // ���������
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
        // ������ ���������, � ������� ���������������
        serviceLocator.playerInventoryManager.SetInteractedContainer(container);

        // ���� ��������� �� ����������� ��������
        if (this.containerConfig.groupTag != containerTag.trader)
        {
            // ����� ������ � ��������� �������
            serviceLocator.playerManager.SetPlayerState(PlayerManager.PlayerStates.looting);

            // ������� ���� ��������� ����������� ����������
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

    // ================ ��������� ����������� (����, ���� ��������) ================

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
    public ItemContainerConfigSO config { get; private set; } // ������ ������� ������ ��� �������������
    public ManagedUnityEvent OnChangeContentsEvent = new ManagedUnityEvent(); // �������, ������������� ��� ��������� ���������� ���������

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

    // ��� ��������� ����������� ��������� (�����������, ����������, ��������)
    public void OnChangeContents()
    {
        // ���������� ������� �� ������, ��������� � editor-�
        if (OnChangeContentsEvent.GetListenerCount() > 0)
            OnChangeContentsEvent.Invoke();

        // ����������� �� ������ (����������) ��������� � ��� �������� ����� �������� (��� ���������� �������)
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

    // ================== ���������� ����������� ==================

    // ���������� �������� � ��������� (�������� � ����� � ��������� ������, ��������� �������)
    public int AddItem(BasicItemSO item, int amount, bool dropRemainsIfNoSpace = false, 
        bool onlyCreateStacks = false) // ���������� ������� (����������)
    {
        if (((amount <= 0) || item == null) || !IsSlotTypeCompatible(item, this))
            return 0;

        int remainingAmount = amount;

        // ���������� � ������������ ���� � �������� ���� ������ �� ������� ������ �� ��������
        if (!onlyCreateStacks)
            remainingAmount = FillStacks(this, remainingAmount, item);
        // ������������ ����� ����������� ��� ����������� � ��������� ����� ������
        remainingAmount = CreateStacks(this, remainingAmount, item); 

        if ((remainingAmount > 0) && dropRemainsIfNoSpace) // �� ��� �������� ������������ (��� �����) � ������� ������� �� �����
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

    // ================== ����� ===================

    private int CreateStacks(Container container, int amount, BasicItemSO item) // ���������� ���������� ���������������� ���������
    {
        if (amount <= 0) // ���� �� ��� �������� ������������ �� ������������ ������
            return 0;

        for (int i = 0; i < container.Count; i++)
        {
            // ���� � ��������� ���� ��������� ������, �� �������� ������� � ���
            if (!container[i].hasItem)
            {
                ContainerSlot newItem = new ContainerSlot(container);
                newItem.hasItem = true;
                newItem.item = item;

                // ���� amount � ���������� ��������� ����� ����� � ������� �������� ����
                if (amount <= item.stackSize)
                {
                    newItem.amount = amount;
                    container[i] = newItem;
                    amount = 0;
                }
                // ����� ����� � ������� ������ ����
                else
                {
                    newItem.amount = item.stackSize;
                    container[i] = newItem;
                    amount -= item.stackSize;
                }
            }

            // ���� ��� ����������� �������� (�� ����������) ������������, �� �������� �������� �����
            if (amount == 0)
                break;
        }

        return amount;
    }

    private int FillStacks(Container container, int amount, BasicItemSO item)
    {
        if (amount <= 0) // ���� �� ��� �������� ������������ �� ������������ ������
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

    // ============== ����������� ����������� ==============

    // ����������� �������� �� ������ ����� ���������� � ������ ���������
    // ����������� ����� SHIFT (��� �������� ������� ������)
    public bool MoveItemTo(Container toContainer, int fromSlotIndex, int amountToMove, bool swapFirstIfHasItem = false) 
    {
        if ((this[fromSlotIndex].hasItem == false) || !IsSlotTypeCompatible(this[fromSlotIndex], toContainer))
            return false;

        bool isItemMoved = false;
        ContainerSlot fromSlot = this[fromSlotIndex];

        // �������� ���� ����� ���������� �� ������� ������-�� ��������
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

        // ������� ��� ��� �� ��������� � ������� ���� ��������� � ������ ������ � ��������� � ������ ������ (��� ����������)
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

    // �������� ����� ����������� (�� ������ � ������). ����������� ����� �������������� 
    public bool MoveItemTo(Container toContainer, int fromSlotIndex, int amount, int toSlotIndex)
    {
        ContainerSlot fromSlot = this[fromSlotIndex];
        ContainerSlot toSlot = toContainer[toSlotIndex];

        // ���� ������ ��������� � ����������
        if ((this[fromSlotIndex].item == null) || !IsSlotTypeCompatible(this[fromSlotIndex], toContainer)
            // ���� ���������� (����� ������ ���� ���������� �������� ����� �������� ������ � ���������� ��������)
            || ( (this.config.groupTag == containerTag.trader || toContainer.config.groupTag == containerTag.trader)
            && this.config.groupTag != toContainer.config.groupTag)
            // ����� ����������
            || (fromSlot == null || toSlot == null)) 
            return false;

        // ���� ������� �� � ��������� � ������� ������� ��� ����
        if (toContainer == null)
        {
            // ��� ������ ���������� ������������ ��������
            RemoveItem(fromSlotIndex, fromSlot.amount);
            fromSlot.item.DropUnderPlayer(fromSlot.amount, true);
            return false;
        }

        // ����������� �������� � ����������� ���� 
        if (toSlot.hasItem) 
        {
            // �������� ���������� � ���� ��������
            if (toSlot.item == fromSlot.item)
            {
                // ����� ��������� �� ��������� �������� ����� � ����������
                if ((toSlot.amount + amount) <= toSlot.item.stackSize)
                {
                    toSlot.amount += fromSlot.amount;
                    fromSlot.Empty();
                }
                // ��������� � ����������� ������� ���������, ��������� ��������
                else
                {
                    int transferAmount = toSlot.item.stackSize - toSlot.amount;
                    toSlot.amount = toSlot.item.stackSize;
                    fromSlot.amount -= transferAmount;
                }
            }

            //������� � �������� �� ���� ����� �� � �������� �� �������
            else if (IsSlotTypeCompatible(toContainer[toSlotIndex], this)) 
                fromSlot.SwapWithAnother(toSlot);

            if (toContainer != this)
                toContainer.OnChangeContents();
            OnChangeContents();
            return true;
        }
        // ����������� �������� � ������ ����
        else
        {
            // ��� ������ ���������� ������������� ��������
            MoveItemToEmptySlot(fromSlotIndex, amount, toContainer, toSlotIndex);
            return true;
        }

    }

    // ����������� � ������, �������� �� ������ (�� �������� �������; hasItem = false)
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

    // ����������� ��� �������� �� ����� ���������� � ������
    public int MoveAllItemsTo(Container toContainer, bool addNewSlotsIfNoSpace = false)
    {
        // ������� ����� ����� � toContainer, ���� ��� �������� �� ����������
        if (addNewSlotsIfNoSpace)
        {
            int slotsLackAmount = this.GetOccupiedSlotsCount() - toContainer.GetEmptySlotsCount();
            if (slotsLackAmount > 0)
                toContainer.addSlots(slotsLackAmount);
        }

        int remainingItems = this.GetOccupiedSlotsCount();

        // ����������� ������� ��������
        for (int i = 0; i < this.Count(); i++) // �� ���� ������� ����� ����������
        {
            // ���������� ��������� ���� ���� � ���� ��� ��������
            if (!this[i].hasItem)
                continue;

            // ���� ������� � ���������� �����������
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

    // ================= ������ =================

    // ���������� �� ������� �� ����� � ����������� ����� ��������� ��������� toContainer
    private bool IsSlotTypeCompatible(ContainerSlot slot, Container toContainer)
    {
        // �� ������������������ (��������� ����� ���) � ���������
        if (!toContainer.config.isSpecialized) 
            return true;
        // ��������������� � ���� ��������� � ���������
        else if (toContainer.config.slotsType == slot.item.type) 
            return true;
        // ��������������� � ���� �������� � �� ���������
        else
            return false;
    }

    // ���������� �� ��� �������� � ����������� ����� ��������� ��������� toContainer
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
        ID = 0; // � ������� ������� ����������� � ����� ������� (?)
        this.Empty();
        this.relatedContainer = relatedContainer;
    }

    public object Clone()
    {
        return this;
    }

    // ��������� ������������� "="
    private ContainerSlot(ContainerSlot other)
    {
        throw new NotSupportedException("Assignment is not allowed!");
    }

    // =======================================

    // ��������� ������������ ������� � ����
    public void AddItem(BasicItemSO item, int amount) 
    {
        if (amount <= 0 || item == null)
            return;

        this.hasItem = true;
        this.item = item;
        this.amount = amount;
    }

    // ������� ���� ���� ������� ��� ����������, �������� ����� ���� �� ��� 
    public void RemoveItem(int amount) 
    {
        if (amount <= 0)
            return;

        if (amount >= item.stackSize || (this.amount-amount) == 0)
            this.Empty();
        else
            this.amount -= amount;
    }

    // �������� ���� �� ��������� ������� (��� ��������)
    public void Empty() 
    {
        this.item = null;
        this.amount = 0;
        this.hasItem = false;
    }

    // ��������� ������ ������� �����
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
