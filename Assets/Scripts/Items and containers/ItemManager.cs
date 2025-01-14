using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// �������� ���������, ������� ������� �� ����� � ���� ������ GameObject (� ������������� � ���� ���� ��������)
public class ItemManager : MonoBehaviour, IInteractable
{
    [SerializeField] public BasicItemSO itemConfig;
    [field: SerializeField] public int amount { get; private set; } = 0;

    private void OnDestroy()
    {
        RemoveHighlight();
    }

    public void AddAmount(int addAmount, bool notMoreThanStack = false)
    {
        // ����� ������-�� amount ����� �������
        amount += addAmount;
        if (notMoreThanStack)
            if (amount > itemConfig.stackSize)
                amount = itemConfig.stackSize;
    }

    public void SetAmount(int addAmount, bool notMoreThanStack = false)
    {
        amount = addAmount;
        if (notMoreThanStack)
            if (amount > itemConfig.stackSize)
                amount = itemConfig.stackSize;
    }

    public void SubstractAmount(int addAmount)
    {
        for (int i = 0; i < addAmount; i++) 
            if (amount > 0)
                amount -= 1;
    }

    // ========== IInteractible ==========

    public void Interact()
    {
        PlayerInventoryManager inventoryManager = ServiceLocator.Instance.playerInventoryManager;

        int remainingAmount = inventoryManager.backpack.AddItem(this.itemConfig, this.amount); // �������� ������� � ��������� ������� ������
        remainingAmount = inventoryManager.quickBar.AddItem(this.itemConfig, remainingAmount); // ������� ����� � �������

        if (remainingAmount > 0) // �� ��� ���������� ����������� � ���������
            this.amount = remainingAmount;
        else // ��� ���������� ����� � ���������� ������� ������ ��������
            Destroy(this.gameObject);
    }

    public void Highlight(HighlightPlus.HighlightProfile profile)
    {
        if (this.TryGetComponent(out HighlightPlus.HighlightEffect highlightScript))
        { 
            if (highlightScript.profile != profile)
                highlightScript.ProfileLoad(profile);
            highlightScript.innerGlowColor = this.itemConfig.rarity.color;
            highlightScript.outlineColor = this.itemConfig.rarity.color;
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
        info.Add(this.itemConfig.nameString.GetLocalizedString());
        info.Add(this.itemConfig.type.nameString.GetLocalizedString());
        info.Add(this.itemConfig.rarity.nameString.GetLocalizedString());
        var locData = LocalizationSettings.StringDatabase;
        string weightString = locData.GetLocalizedString("Common strings", "STRING_WEIGHT");
        info.Add(weightString + ": " + this.itemConfig.weight.ToString());
        string priceString = locData.GetLocalizedString("Common strings", "STRING_PRICE");
        info.Add(priceString + ": " + this.itemConfig.price.ToString());

        return info;
    }
}
