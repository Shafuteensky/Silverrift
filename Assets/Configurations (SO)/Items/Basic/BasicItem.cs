using System.Collections.Generic;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "Basic Item", menuName = "Items/Basic", order = 10)]
public class BasicItemSO : ScriptableObject // Все мировые предметы начинаются отсюда
{
    [field: SerializeField] public LocalizedString nameString { get; private set; }
    [field: SerializeField] public LocalizedString descriptionString { get; private set; }

    [field: Header("Resources")]
    [field: SerializeField] public GameObject modelPrefab { get; private set; }
    [field: SerializeField] public Sprite image { get; private set; }
    [field: SerializeField] public AudioClip pickupSound { get; private set; }
    [field: SerializeField] public AudioClip dropSound { get; private set; }

    [field: Header("Stats")]
    [field: SerializeField] public ItemTypeSO type { get; private set; }
    [field: SerializeField] public ItemRaritySO rarity { get; private set; }
    [field: SerializeField] public BasicItemSO tradingCurrency { get; private set; }
    [field: SerializeField] [field: Range(0, 9999)] public int price { get; private set; } = 1;
    [field: SerializeField] [field: Range(1, 999)] public int stackSize { get; private set; } = 1;
    [field: SerializeField][field: Range(0.01f, 100)] public float weight { get; private set; } = 0.5f;

    [field: Header("Disassembly")]
    [field: SerializeField] public bool isDisassemblable { get; private set; } = false;
    [field: SerializeField] public List<BasicItemSO> disassemblyItems { get; private set; }
    [field: SerializeField] public List<int> disassemblyItemsAmount { get; private set; }

    [field: Header("Primary action animation")]
    // Анимация, проигрываемая игроком во время использования
    [field: SerializeField] public string primaryActionAnimationName { get; private set; }
    // Скорость проигрывания анимации
    [field: SerializeField] public float animationSpeed { get; private set; } = 1;

    // ==========================

    public void DropUnderPlayer(int amount, bool isPhysical = false, bool findGround = true, int maxGroundDistance = 50) // Скинуть предмет на землю
    {
        if (amount <=0)
            return;

        // Сброс под игроком
        GameObject player = GameObject.Find("Player");
        Vector3 position = Vector3.zero;
        if (player != null)
            position = player.transform.position;

        // Найти твердь под точкой сброса
        if (findGround)
        {
            RaycastHit hit;
            Vector3 yOffset = new Vector3(0, -0.5f, 0);
            Physics.Raycast(position - yOffset, Vector3.down, out hit, maxGroundDistance);
            position.y = hit.point.y;
        }

        this.CreateItemWorldObject(amount, position, isPhysical);
    }

    // Главная функция спавна модели (мирового предмета) из конфигурации
    public GameObject CreateItemWorldObject(int amount, Vector3 position, bool isPhysical = true, bool isMeshCollider = false,
        bool isRandomPosition = true, bool isRandomAndRotation = true, float offset = 0.3f)
    {
        Quaternion rotation = Quaternion.identity;
        // Создать префаб предмета с случайным поворотом
        if (isRandomPosition)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-offset, offset), position.y, Random.Range(-offset, offset));
            position += randomOffset;
        }
        if (isRandomAndRotation)
        {
            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            rotation = randomRotation;
        }
        GameObject Item = Instantiate(this.modelPrefab, position, rotation);
        // !!! Удалить все дочерние объекты: точки сцепки, damage dealer (элементы активного предмета)
        GameObjectTools.DestroyAllChildren(Item.transform);
        Item.layer = LayerMask.NameToLayer("Items");

        // Привязка свойств (конфига) предмета его мировой репрезентации
        if (Item.GetComponent<ItemManager>() == null)
            Item.AddComponent<ItemManager>().itemConfig = this;
        Item.GetComponent<ItemManager>().SetAmount(amount);

        // Скрипт обводки (ВРЕМЕННЫЙ, ПОКА НЕТ ПОДСВЕТКИ)
        //if (Item.GetComponent<Outline>() == null)
        //    Item.AddComponent<Outline>().enabled = false;

        Item.AddComponent<HighlightPlus.HighlightEffect>();

        if (isPhysical)
            Item.AddComponent<Rigidbody>().excludeLayers = LayerMask.GetMask("Player");

        if (isMeshCollider)
        {
            Item.AddComponent<MeshCollider>().convex = true;
            Item.GetComponent<MeshCollider>().excludeLayers = LayerMask.GetMask("Player");
        }
        else
            Item.AddComponent<BoxCollider>().excludeLayers = LayerMask.GetMask("Player");

        return Item;
    }
}


