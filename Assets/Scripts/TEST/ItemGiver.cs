using System.Collections.Generic;
using UnityEngine;

public class ItemGiver : MonoBehaviour
{
    [field: SerializeField] public List<BasicItemSO> items { get; private set; }
    [field: SerializeField] public bool randomQuantity { get; private set; }
    [field: SerializeField] public bool maxQuantity { get; private set; }
    [field: SerializeField] public int quantity { get; private set; }
    [field: SerializeField] public bool toQuickbar { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name != "Player")
            return;
        GameObject playerEntity = other.transform.parent.gameObject;
        GiveItems(playerEntity, items, randomQuantity, maxQuantity, quantity, toQuickbar);
    }

    public static void GiveItems(GameObject playerEntity, List<BasicItemSO> items,
        bool randomQuantity = false, bool maxQuantity = true, int quantity = 1, bool toQuickbar = true)
    {
        var iventoryManager = playerEntity.GetComponent<PlayerInventoryManager>();

        if (iventoryManager == null)
            return;

        foreach (BasicItemSO item in items)
        {
            if (randomQuantity)
                quantity = Random.Range(1, item.stackSize);
            else if (maxQuantity)
                quantity = item.stackSize;

            if (toQuickbar)
            { 
                int remainingAmount = iventoryManager.quickBar.AddItem(item, quantity, true);
                if (remainingAmount > 0)
                    iventoryManager.backpack.AddItem(item, remainingAmount, true);
            }
            else
                iventoryManager.backpack.AddItem(item, quantity, true);
        }
    }
}
