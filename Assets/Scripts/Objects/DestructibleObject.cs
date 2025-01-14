using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using static IDamageable;
using static UnityEditor.Progress;

public class DestructibleObject : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private int health = 35;

    [Header("Destructed version")]
    [SerializeField] private GameObject destructedObjPrefab;
    // Время жизни разрушенного предмета (его "кусочков") в секундах до начала эффекта "исчезновения"
    [SerializeField] private int debrisLifeTime = 5;

    [Header("Drop")]
    [SerializeField] private bool haveDrop = false;
    // Модификатор шанса дропа (поверх шанса от редкости и модификатора шанса от конфигурации)
    [SerializeField] [Range(0.1f, 3f)] private float dropModifier = 1f;
    [SerializeField] private ItemSpawnerConfigurationSO dropConfiguration;

    // ====================================

    public void TakeDamage(int damageValue, DamageType damageType)
    {
        // ТУТ МОДИФИКАТОРЫ ОТ DamageType
        health -= damageValue;

        if (health > 0)
        {
            // ТУТ ЗВУК ПОЛУЧЕНИЯ УРОНА
            ObjectShaker.StartShake(this, this.transform, 1f, 0.025f, 10f);
            return;
        }

        // ТУТ ЗВУК УНИЧТОЖЕНИЯ
        ObjectShaker.StopShake();
        Decease();
    }

    public void Decease()
    {
        // Спрятать объект
        GameObjectTools.HideObject(this.gameObject);

        // Создать его разрушенную версию с отдельными элементами
        GameObject destructedObject = Instantiate(destructedObjPrefab, this.transform);
        destructedObject.transform.localPosition = Vector3.zero;

        // Заспавнить дроп (лут)
        if (haveDrop)
        {
            int itemIndex = Random.Range(0, dropConfiguration.itemsSpawnList.Count);
            BasicItemSO item = dropConfiguration.itemsSpawnList[itemIndex];
            int amountToDrop = Random.Range(1, item.stackSize);

            float dropChance = 1;
            if (dropConfiguration.overrideRarityChance)
                dropChance = dropConfiguration.spawnChanceFactor;
            else
                dropChance = item.rarity.worldSpawnChance * dropModifier;
            if (Random.value < dropChance)
                item.CreateItemWorldObject(amountToDrop, this.transform.position);
        }

        // Дать физичность дочерним объектам
        foreach (Transform child in destructedObject.transform)
        {
            child.gameObject.layer = LayerMask.NameToLayer("Destructed");
            child.AddComponent<BoxCollider>();
            child.AddComponent<Rigidbody>();
        }

        // Спустя время удалить разрушенный предмет
        StartCoroutine(WaitAndDestroy(debrisLifeTime));
    }

    // Корутинa
    private IEnumerator WaitAndDestroy(float delayInSeconds)
    {
        yield return new WaitForSeconds(delayInSeconds);

        StartCoroutine(GameObjectTools.FadeOutAndDestroy(this.gameObject));
    }
}







