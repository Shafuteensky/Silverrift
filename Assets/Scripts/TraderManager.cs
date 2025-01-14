using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using static UnityEditor.Progress;

[RequireComponent(typeof(ContainerManager))]
public class TraderManager : MonoBehaviour, IInteractable
{
    [field: Header("Trader settings")]
    [field: SerializeField] public BasicItemSO tradingCurrency { get; private set; }
    [field: SerializeField] public int trustLevel { get; private set; }

    // ??? ���� ������������ ���� �� ������ �� ��������� ����������
    [field: SerializeField] public List<BasicItemSO> tradingPool { get; private set; }

    private void Start()
    {
        GenerateTradingStash();
    }

    // =============== IInteractable ===============

    public void Interact()
    {
        ServiceLocator serviceLocator = ServiceLocator.Instance;

        PlayerManager playerManager = serviceLocator.playerManager;
        UIManager UIManager = serviceLocator.uiManager;

        // ������������� (�����) ������� ������� � ���������
        serviceLocator.barterProcessor.traderManager = this;
        // ����� ������ � ��������� �������
        playerManager.tradingWith = this;
        playerManager.SetPlayerState(PlayerManager.PlayerStates.bartering);

        // ������� ���� �������
        UIManager.SetLayoutBarter();
        UIManager.RepopulateInteractedContainer();
    }

    public void Highlight(HighlightPlus.HighlightProfile profile)
    {
    }

    public void RemoveHighlight()
    {
    }

    public List<string> GetInfo()
    {
        return new List<string>{"Le Poisson", "Le Poisson", "Le Poisson" };
    }

    // ==========================================

    // ������� �������/������ ����������� ��������� ���������
    public float GetTraderItemsChargePercent()
    {
        return 2.5f;
    }

    // ������� �������/������ ���������� ��������� ���������
    public float GetPlayerItemsChargePercent()
    {
        return 0.3f;
    }

    private void GenerateTradingStash()
    {
        if (!TryGetComponent(out ContainerManager containerManager))
            return; 

        ItemContainerConfigSO containerConfig = containerManager.containerConfig;
        Container container = containerManager.container;

        if (container.isEmpty())
            foreach (var item in containerConfig.itemsInIt)
                container.AddItem(item, item.stackSize);

        int min = (int)Mathf.Floor(this.tradingCurrency.stackSize / 2);
        int max = (int)Mathf.Floor(this.tradingCurrency.stackSize * 3);
        int moneyAmout = Random.Range(min, max);
        container.AddItem(this.tradingCurrency, moneyAmout);
    }
}
