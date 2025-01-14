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

        // -------- Редкость --------
        rarityPanel.GetComponent<TextMeshProUGUI>().text = item.rarity.nameString.GetLocalizedString();

        // -------- Тип и подтип --------
        string type;
        // Использовать подтип, если UsableItemSO
        if (item is UsableItemSO usable && item.type == serviceLocator.ITWeapon) // Используемый и оружие
            type = usable.weaponType.nameString.GetLocalizedString(); 
        // Использовать тип, если иной
        else
            type = item.type.nameString.GetLocalizedString();
        typePanel.GetComponent<TextMeshProUGUI>().text = type;

        // -------- Изображение --------
        modelPanel.GetComponent<UnityEngine.UI.Image>().sprite = item.image; // ПОКА ИЗОБРАЖЕНИЕ, ПОТОМ 3Д МОДЕЛЬ

        // -------- Название --------
        namePanel.GetComponent<TextMeshProUGUI>().text = item.nameString.GetLocalizedString();

        // -------- Описание --------
        descriptionPanel.GetComponent<TextMeshProUGUI>().text = item.descriptionString.GetLocalizedString();

        // -------- Статы --------
        TextMeshProUGUI stats = statsPanel.GetComponent<TextMeshProUGUI>();
        string stackSize = LocalizationSettings.StringDatabase.GetLocalizedString("Common strings", "STRING_MAX_STACK_SIZE");
        stats.text = stackSize + ": " + item.stackSize.ToString();
        // Для различных типов предметов разные данные о статах
        if (item is UsableItemSO)
        {
            stats.text += "\nУрон и все такое";
        }
        else if (item is ConsumableItemSO)
        {
            stats.text += "\nГолод-не-голод";
        }
        else // BasicItemSO
        {

        }

        // -------- Вес --------
        string weightString = LocalizationSettings.StringDatabase.GetLocalizedString("Common strings", "STRING_WEIGHT");
        weightPanel.GetComponent<TextMeshProUGUI>().text = weightString + ": " + item.weight.ToString();

        // -------- Цена --------
        UIManager uiManager = FindFirstObjectByType<UIManager>(); // !!!
        PlayerManager playerManager = FindFirstObjectByType<PlayerManager>(); // !!!
        string priceSize = LocalizationSettings.StringDatabase.GetLocalizedString("Common strings", "STRING_PRICE");
        string tradingPriceString = "";
        string actualPriceString = item.price.ToString();

        string priceString = (item.price * itemAmount).ToString();
        if (itemAmount > 1)
            priceString += " (" + actualPriceString + ")";

        // Если игрок в меню бартера и предмет не является валютой
        if (uiManager.currentLayout == UIManager.UILayouts.Barter // !!! ИГРОК В СОСТОЯНИИ БАРТЕРА
            && item.type != serviceLocator.ITCurrency) 
        {
            // Множитель в зависимости от скидки/надбавки к стоимости предмета
            float factor;
            // Выводится предмет из контейнера торговца – показывать цену с наценкой/скидкой покупки 
            if (itemSlot.relatedContainer.config.groupTag == containerTag.trader)
                factor = playerManager.tradingWith.GetTraderItemsChargePercent();
            // Выводится предмет из контейнера игрока – показывать цену с наценкой/скидкой продажи 
            else
                factor = playerManager.tradingWith.GetPlayerItemsChargePercent();

            int tradingPrice = (int)Mathf.Floor(item.price * factor);
            tradingPriceString = tradingPrice.ToString();

            priceString = (tradingPrice * itemAmount).ToString();
            if (itemAmount > 1) 
                priceString += " (" + tradingPriceString + ")";
        }
        // Цена предмета
        pricePanel.GetComponent<TextMeshProUGUI>().text = priceSize + ": " + priceString;
        // Иконка торговой валюты предметы
        currencyIconPanel.GetComponent<Image>().sprite = item.tradingCurrency.image;
    }
}
