using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Progress;

public class ItemSpawnerManager : MonoBehaviour
{
    private GameObject itemObject;
    private BasicItemSO itemToSpawn;
    private Vector3 spawnPoint; // Точка спавнера в пространстве

    [SerializeField] private ItemSpawnerConfigurationSO config;

    [Tooltip("If not, ray will find the ground")]
    [SerializeField] private bool spawnInTheAir = false;

    private void Awake()
    {
        spawnPoint = transform.position;
    }

    void Start()
    {
        SpawnItem();
        Destroy(gameObject);
    }

    private void SpawnItem()
    {
        Dictionary<BasicItemSO, float> chancesList = new Dictionary<BasicItemSO, float>(); // Предмет | Шанс
        foreach (BasicItemSO item in config.itemsSpawnList)
            chancesList.Add(item, item.rarity.worldSpawnChance);
        var sortedByChance = chancesList.OrderBy(pair => pair.Value); // Сортировка по увеличению шанса

        foreach (KeyValuePair<BasicItemSO, float> pair in sortedByChance)
        {
            float spawnChance = 0;

            if (config.overrideRarityChance)
                spawnChance = config.spawnChanceFactor;
            else
                spawnChance = pair.Value * config.spawnChanceFactor;

            var rand = Random.value;
            if (spawnChance >= rand)
            {
                itemToSpawn = pair.Key;
                break;
            }
        }

        if (itemToSpawn != null)
        {
            int amount = new int();
            if (config.randomAmount)
                amount = Random.Range(1, itemToSpawn.stackSize);
            else
                amount = itemToSpawn.stackSize;

            Vector3 spawnPosition = new Vector3();
            if (spawnInTheAir)
                spawnPosition = spawnPoint;
            else
            {
                RaycastHit hit;
                Physics.Raycast(spawnPoint, Vector3.down, out hit, 100);
                spawnPosition = hit.point;
            }

            itemToSpawn.CreateItemWorldObject(amount, spawnPosition, true, false, false);
        }
    }
}
