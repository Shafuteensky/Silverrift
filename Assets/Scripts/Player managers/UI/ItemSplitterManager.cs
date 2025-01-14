using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSplitterManager : MonoBehaviour
{
    [SerializeField] private GameObject slider;
    [SerializeField] private GameObject nameTextPanel;
    [SerializeField] private GameObject splitAmountTextPanel;

    private Slider sliderSettings;
    private UIManager uiManager;

    private Container container;
    private int index;
    private ContainerSlot slot;

    private void Awake()
    {
        ScriptTools.RegisterScript(this);

        sliderSettings = slider.GetComponent<Slider>();
    }

    private ServiceLocator serviceLocator;

    private void Start()
    {
        serviceLocator = ServiceLocator.Instance;

        uiManager = serviceLocator.uiManager;
    }

    // ==========================================

    public void SetData(Container container, int index)
    {
        this.container = container;
        this.index = index;
        slot = container[index];

        sliderSettings.minValue = 1;
        sliderSettings.value = sliderSettings.minValue;
        sliderSettings.maxValue = slot.amount-1;

        nameTextPanel.GetComponent<TextMeshProUGUI>().text = slot.item.nameString.GetLocalizedString();
        UpdateSliderValueText();
    }

    public void UpdateSliderValueText()
    {
        splitAmountTextPanel.GetComponent<TextMeshProUGUI>().text = (slot.amount- sliderSettings.value) + "/" + 
            sliderSettings.value;
    }

    // ================ Кнопки ==================

    public void CancelButtonClick()
    {
        uiManager.HideSplitterPanel();
    }

    public void SplitButtonClick()
    {
        bool dropRemainsIfNoSpace = true;
        if (container.config.groupTag == containerTag.trader)
            dropRemainsIfNoSpace = false;

        container.SplitItem(index, (int)sliderSettings.value, dropRemainsIfNoSpace, container);

        uiManager.HideSplitterPanel();
    }
}
