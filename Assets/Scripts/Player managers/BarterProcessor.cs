using UnityEngine;
using UnityEngine.UI;

public class BarterProcessor : InitializableMonoBehaviour
{
    public TraderManager traderManager;
    private Container playerTransfer;
    private Container traderTransfer;

    private UIManager uiManager;
    private PlayerInventoryManager playerInventoryManager;
    private ScriptablesCacher scriptablesCacher;

    int playerTransferWorth;
    int traderTransferWorth;

    private void Awake()
    {
        ScriptTools.RegisterScript(this);
        scriptablesCacher = ScriptablesCacher.Instance;
    }

    private ServiceLocator serviceLocator;

    private void Start()
    {
        serviceLocator = ServiceLocator.Instance;
        uiManager = serviceLocator.uiManager;
        playerInventoryManager = serviceLocator.playerInventoryManager;

        if (!AreDependenciesInitialized(uiManager, playerInventoryManager))
            return;
        Initialize();

        if (!IsInitialized()) return;

        playerTransfer = playerInventoryManager.barterPlayerTransfer;
        traderTransfer = playerInventoryManager.barterTraderTransfer;

        // ������ "�������������" (������ �������) ���������� ������ (�������) ��� ������ ��������� ��������� � ����������
        playerTransfer.OnChangeContentsEvent.AddListener(RecalculatePlayerTransferWorth);
        playerTransfer.OnChangeContentsEvent.AddListener(SetBarterButtonState);
        traderTransfer.OnChangeContentsEvent.AddListener(RecalculateTraderTransferWorth);
        traderTransfer.OnChangeContentsEvent.AddListener(SetBarterButtonState);

        // ���������� ������ �������
        uiManager.barterDealButton.onClick.AddListener(BarterItems);
        uiManager.barterBalanceButton.onClick.AddListener(BalanceWealths);
    }

    // ================ ���������� ���� ������� ================

    // ����� ���������� �� �����������-����������
    private void BarterItems()
    {
        if (!IsInitialized()) return;

        // ����������� �������� �� ��������� ������ � ��������� �������� (� ������� �����, ���� �� ������� �����)
        playerTransfer.MoveAllItemsTo(serviceLocator.playerInventoryManager.interactedContainer, true);

        // ����������� �������� �� ��������� �������� � ��������� ������ (� �������� �������, ���� �� ������� �����)
        int remainingItems = traderTransfer.MoveAllItemsTo(serviceLocator.playerInventoryManager.backpack);
        if (remainingItems > 0)
            remainingItems = traderTransfer.MoveAllItemsTo(serviceLocator.playerInventoryManager.quickBar);
        if (remainingItems > 0)
            traderTransfer.DropAllItems();

        playerTransferWorth = 0;
        traderTransferWorth = 0;
    }

    // ��������� ���������� ������ ������� ���� ��������� ��������� � ���������� �����
    private void SetBarterButtonState()
    {
        if (!IsInitialized()) return;

        uiManager.barterDealButton.interactable = IsTransfersWorthsEqual();
    }

    // �������� �������� ��������� ��������� � ����������
    private void BalanceWealths()
    {
        if (!IsInitialized()) return;

        Container traderInv = playerInventoryManager.interactedContainer;
        Container playerInv = playerInventoryManager.backpack;
        Container playerQuickbarInv = playerInventoryManager.quickBar;
        BasicItemSO currency = traderManager.tradingCurrency;

        // ������ ��� ������ � ���������
        MoveAllCurrency(traderTransfer, traderInv, currency);
        MoveAllCurrency(playerTransfer, playerInv, currency);

        // ----- ����������� ��������� -----

        // ������� � ��������� ��������� ��������� ���� ����������
        int wealthDifference = Mathf.Abs(playerTransferWorth - traderTransferWorth);

        // �������� ��������� ������ ������, ��� ��������� ��������
        if (playerTransferWorth > traderTransferWorth)
        {
            // ����������� ������ �� ��������� �������� � ��������
            MoveMoneyWealth(traderInv, traderTransfer, wealthDifference, currency);
        }

        // �������� ��������� �������� ������, ��� ��������� ������
        else if (playerTransferWorth < traderTransferWorth)
        {
            // ����������� ������ �� ��������� ������ � ��������
            int remainedWealth = MoveMoneyWealth(playerInv, playerTransfer, wealthDifference, currency);
            if (remainedWealth > 0)
                MoveMoneyWealth(playerQuickbarInv, playerTransfer, remainedWealth, currency);
        }
    }

    // ����������� ���������� ������ �� ������ ��������� � ������
    private int MoveMoneyWealth(Container fromContainer, Container toContainer, int wealthValue, BasicItemSO currency)
    {
        int remainedWealthValue = wealthValue;
        int wealthValueToMove = 0;

        // �������� ������ ������
        for (int index = 0; index < fromContainer.Count; index++)
        {
            // ���������� ������� ���� � ���������� ������ �����
            if (remainedWealthValue == 0) return 0;

            ContainerSlot slot = fromContainer[index];

            // � ������ ���� ������� � ��� ������ �������� � ����������
            if (!slot.hasItem || slot.item != currency)
                continue;

            // �������� � ����������� ������ ������, ��� ���� � �����
            if (remainedWealthValue > slot.amount)
            {
                wealthValueToMove = slot.amount;
                remainedWealthValue -= slot.amount;
            }
            // ������ ��� ������� ��, ������� ���� � �����
            else
            {
                wealthValueToMove = remainedWealthValue;
                remainedWealthValue = 0;
            }

            fromContainer.MoveItemTo(toContainer, index, wealthValueToMove);
        }

        return remainedWealthValue;
    }

    // �������� ��������� ��������� (����������) ������ � ����������
    private int GetMoneyWealth(Container containers, BasicItemSO currency)
    {
        int moneyWealth = 0;

        foreach (ContainerSlot slot in containers)
        {
            if (slot.hasItem && slot.item == currency)
                moneyWealth += slot.amount;
        }

        Debug.Log(moneyWealth);
        return moneyWealth;
    }

    // ������ ��� ������ �� ����������
    private void MoveAllCurrency(Container fromContainer, Container toContainer, BasicItemSO currency)
    {
        for (int index = 0; index < fromContainer.Count; index++)
        {
            ContainerSlot slot = fromContainer[index];

            if (slot.hasItem && slot.item == currency)
                fromContainer.MoveItemTo(toContainer, index, slot.amount);
        }
    }

    // ================ ��������� �������� ��������� ��������� ================

    // ����� �� �������� ���� ��������� � ���� �����������
    private bool IsTransfersWorthsEqual()
    {
        if (!IsInitialized()) return false;

        bool isWorthsEqual = false;

        if (traderTransferWorth == playerTransferWorth // �������� �����
            && !traderTransfer.isEmpty() && !playerTransfer.isEmpty()) // ��������� �� �����
            isWorthsEqual = true;

        return isWorthsEqual;
    }

    private void RecalculatePlayerTransferWorth()
    {
        float buyPercent = traderManager.GetPlayerItemsChargePercent();
        playerTransferWorth = GetContainerItemsWorth(playerTransfer, buyPercent);
    }

    private void RecalculateTraderTransferWorth()
    {
        float sellPercent = traderManager.GetTraderItemsChargePercent();
        traderTransferWorth = GetContainerItemsWorth(traderTransfer, sellPercent);
    }

    // ������ �������� ���� ��������� � ����������
    private int GetContainerItemsWorth(Container container, float factor = 1)
    {
        int sumWorth = 0;

        foreach (ContainerSlot slot in container)
        {
            if (!slot.hasItem)
                continue;

            // ���� ������� � ������, �� �� ��������� ��������� ������/�������
            if (slot.item.type == scriptablesCacher.ITCurrency)
                sumWorth += (slot.item.price * slot.amount);
            // ����� � ��������� ��������� � ���� ��������
            else
                sumWorth += ((int)Mathf.Floor(slot.item.price * factor) * slot.amount);
        }

        return sumWorth;
    }
}
