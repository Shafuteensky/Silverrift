using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;
using Image = UnityEngine.UI.Image;

public class DescriptionPanelHandler : MonoBehaviour
{
    [SerializeField] private GameObject typePanel;
    [SerializeField] private GameObject rarityPanel;

    [SerializeField] private GameObject modelPanel;

    [SerializeField] public GameObject nameSubstratePanel;
    [SerializeField] private GameObject namePanel;

    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private GameObject statsPanel;

    [SerializeField] private GameObject weightPanel;
    [SerializeField] private GameObject pricePanel;
    [SerializeField] private GameObject currencyIconPanel;

    ScriptablesCacher serviceLocator;

    private void Awake()
    {
        ScriptTools.RegisterScript(this);

        serviceLocator = FindFirstObjectByType<ScriptablesCacher>();
    }

    // ===========================================

    public void SetDescriptionFromItemSlot(ItemSlotUI itemSlot)
    {
        BasicItemSO item = itemSlot.containerSlot.item;
        int itemAmount = itemSlot.containerSlot.amount;

        // -------- �������� --------
        rarityPanel.GetComponent<TextMeshProUGUI>().text = item.rarity.nameString.GetLocalizedString();

        // -------- ��� � ������ --------
        string type;
        // ������������ ������, ���� UsableItemSO
        if (item is UsableItemSO usable && item.type == serviceLocator.ITWeapon) // ������������ � ������
            type = usable.weaponType.nameString.GetLocalizedString(); 
        // ������������ ���, ���� ����
        else
            type = item.type.nameString.GetLocalizedString();
        typePanel.GetComponent<TextMeshProUGUI>().text = type;

        // -------- ����������� --------
        modelPanel.GetComponent<UnityEngine.UI.Image>().sprite = item.image; // ���� �����������, ����� 3� ������

        // -------- �������� --------
        namePanel.GetComponent<TextMeshProUGUI>().text = item.nameString.GetLocalizedString();

        // -------- �������� --------
        descriptionPanel.GetComponent<TextMeshProUGUI>().text = item.descriptionString.GetLocalizedString();

        // -------- ����� --------
        TextMeshProUGUI stats = statsPanel.GetComponent<TextMeshProUGUI>();
        string stackSize = LocalizationSettings.StringDatabase.GetLocalizedString("Common strings", "STRING_MAX_STACK_SIZE");
        stats.text = stackSize + ": " + item.stackSize.ToString();
        // ��� ��������� ����� ��������� ������ ������ � ������
        if (item is UsableItemSO)
        {
            stats.text += "\n���� � ��� �����";
        }
        else if (item is ConsumableItemSO)
        {
            stats.text += "\n�����-��-�����";
        }
        else // BasicItemSO
        {

        }

        // -------- ��� --------
        string weightString = LocalizationSettings.StringDatabase.GetLocalizedString("Common strings", "STRING_WEIGHT");
        weightPanel.GetComponent<TextMeshProUGUI>().text = weightString + ": " + item.weight.ToString();

        // -------- ���� --------
        UIManager uiManager = FindFirstObjectByType<UIManager>(); // !!!
        PlayerManager playerManager = FindFirstObjectByType<PlayerManager>(); // !!!
        string priceSize = LocalizationSettings.StringDatabase.GetLocalizedString("Common strings", "STRING_PRICE");
        string tradingPriceString = "";
        string actualPriceString = item.price.ToString();

        string priceString = (item.price * itemAmount).ToString();
        if (itemAmount > 1)
            priceString += " (" + actualPriceString + ")";

        // ���� ����� � ���� ������� � ������� �� �������� �������
        if (uiManager.currentLayout == UIManager.UILayouts.Barter // !!! ����� � ��������� �������
            && item.type != serviceLocator.ITCurrency) 
        {
            // ��������� � ����������� �� ������/�������� � ��������� ��������
            float factor;
            // ��������� ������� �� ���������� �������� � ���������� ���� � ��������/������� ������� 
            if (itemSlot.relatedContainer.config.groupTag == containerTag.trader)
                factor = playerManager.tradingWith.GetTraderItemsChargePercent();
            // ��������� ������� �� ���������� ������ � ���������� ���� � ��������/������� ������� 
            else
                factor = playerManager.tradingWith.GetPlayerItemsChargePercent();

            int tradingPrice = (int)Mathf.Floor(item.price * factor);
            tradingPriceString = tradingPrice.ToString();

            priceString = (tradingPrice * itemAmount).ToString();
            if (itemAmount > 1) 
                priceString += " (" + tradingPriceString + ")";
        }
        // ���� ��������
        pricePanel.GetComponent<TextMeshProUGUI>().text = priceSize + ": " + priceString;
        // ������ �������� ������ ��������
        currencyIconPanel.GetComponent<Image>().sprite = item.tradingCurrency.image;
    }
}
