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

        // Расчет "дозволенности" (кнопка бартера) проведения сделки (бартера) при равной стоимости предметов в трансферах
        playerTransfer.OnChangeContentsEvent.AddListener(RecalculatePlayerTransferWorth);
        playerTransfer.OnChangeContentsEvent.AddListener(SetBarterButtonState);
        traderTransfer.OnChangeContentsEvent.AddListener(RecalculateTraderTransferWorth);
        traderTransfer.OnChangeContentsEvent.AddListener(SetBarterButtonState);

        // Функционал кнопок бартера
        uiManager.barterDealButton.onClick.AddListener(BarterItems);
        uiManager.barterBalanceButton.onClick.AddListener(BalanceWealths);
    }

    // ================ Функционал меню бартера ================

    // Обмен предметами из контейнеров-трансферов
    private void BarterItems()
    {
        if (!IsInitialized()) return;

        // Переместить предметы из трансфера игрока в инвентарь торговца (и создать слоты, если не хватает места)
        playerTransfer.MoveAllItemsTo(serviceLocator.playerInventoryManager.interactedContainer, true);

        // Переместить предметы из трансфера торговца в инвентарь игрока (и дропнуть остатки, если не хватает места)
        int remainingItems = traderTransfer.MoveAllItemsTo(serviceLocator.playerInventoryManager.backpack);
        if (remainingItems > 0)
            remainingItems = traderTransfer.MoveAllItemsTo(serviceLocator.playerInventoryManager.quickBar);
        if (remainingItems > 0)
            traderTransfer.DropAllItems();

        playerTransferWorth = 0;
        traderTransferWorth = 0;
    }

    // Разрешить проведение сделки бартера если стоимости предметов в трансферах равны
    private void SetBarterButtonState()
    {
        if (!IsInitialized()) return;

        uiManager.barterDealButton.interactable = IsTransfersWorthsEqual();
    }

    // Уравнять валютами стоимости предметов в трансферах
    private void BalanceWealths()
    {
        if (!IsInitialized()) return;

        Container traderInv = playerInventoryManager.interactedContainer;
        Container playerInv = playerInventoryManager.backpack;
        Container playerQuickbarInv = playerInventoryManager.quickBar;
        BasicItemSO currency = traderManager.tradingCurrency;

        // Убрать все деньги в инвентари
        MoveAllCurrency(traderTransfer, traderInv, currency);
        MoveAllCurrency(playerTransfer, playerInv, currency);

        // ----- Выравниваем стоимости -----

        // Разница в суммарных ценностях предметов двух трансферов
        int wealthDifference = Mathf.Abs(playerTransferWorth - traderTransferWorth);

        // Ценность трансфера игрока больше, чем трансфера торговца
        if (playerTransferWorth > traderTransferWorth)
        {
            // Переместить деньги из инвентаря торговца в трансфер
            MoveMoneyWealth(traderInv, traderTransfer, wealthDifference, currency);
        }

        // Ценность трансфера торговца больше, чем трансфера игрока
        else if (playerTransferWorth < traderTransferWorth)
        {
            // Переместить деньги из инвентаря игрока в трансфер
            int remainedWealth = MoveMoneyWealth(playerInv, playerTransfer, wealthDifference, currency);
            if (remainedWealth > 0)
                MoveMoneyWealth(playerQuickbarInv, playerTransfer, remainedWealth, currency);
        }
    }

    // Переместить количество валюты из одного конейнера в другой
    private int MoveMoneyWealth(Container fromContainer, Container toContainer, int wealthValue, BasicItemSO currency)
    {
        int remainedWealthValue = wealthValue;
        int wealthValueToMove = 0;

        // Проверка каждой ячейки
        for (int index = 0; index < fromContainer.Count; index++)
        {
            // Перемещено сколько надо – прекратить осмотр ячеек
            if (remainedWealthValue == 0) return 0;

            ContainerSlot slot = fromContainer[index];

            // В ячейке есть предмет и это валюта торговца – продолжить
            if (!slot.hasItem || slot.item != currency)
                continue;

            // Осталось к перемещению валюты больше, чем есть в слоте
            if (remainedWealthValue > slot.amount)
            {
                wealthValueToMove = slot.amount;
                remainedWealthValue -= slot.amount;
            }
            // Меньше или столько же, сколько есть в слоте
            else
            {
                wealthValueToMove = remainedWealthValue;
                remainedWealthValue = 0;
            }

            fromContainer.MoveItemTo(toContainer, index, wealthValueToMove);
        }

        return remainedWealthValue;
    }

    // Получить суммарную стоимость (количество) валюты в контейнере
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

    // Убрать всю валюту из контейнера
    private void MoveAllCurrency(Container fromContainer, Container toContainer, BasicItemSO currency)
    {
        for (int index = 0; index < fromContainer.Count; index++)
        {
            ContainerSlot slot = fromContainer[index];

            if (slot.hasItem && slot.item == currency)
                fromContainer.MoveItemTo(toContainer, index, slot.amount);
        }
    }

    // ================ Суммарная ценность предметов конейнера ================

    // Равны ли ценности всех предметов в двух контейнерах
    private bool IsTransfersWorthsEqual()
    {
        if (!IsInitialized()) return false;

        bool isWorthsEqual = false;

        if (traderTransferWorth == playerTransferWorth // Ценности равны
            && !traderTransfer.isEmpty() && !playerTransfer.isEmpty()) // Трансферы не пусты
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

    // Расчет ценности всех предметов в контейнере
    private int GetContainerItemsWorth(Container container, float factor = 1)
    {
        int sumWorth = 0;

        foreach (ContainerSlot slot in container)
        {
            if (!slot.hasItem)
                continue;

            // Если предмет – валюта, то не учитывать множитель скидки/наценки
            if (slot.item.type == scriptablesCacher.ITCurrency)
                sumWorth += (slot.item.price * slot.amount);
            // Иначе – применить множитель к цене предмета
            else
                sumWorth += ((int)Mathf.Floor(slot.item.price * factor) * slot.amount);
        }

        return sumWorth;
    }
}
