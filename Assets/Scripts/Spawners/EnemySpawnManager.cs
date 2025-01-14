using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    [SerializeField] private CommonNPCSO enemyConfiguration;
    private GameObject item;

    private Transform spawnPoint; // Точка спавнера в пространстве

    private void Awake()
    {
        spawnPoint = transform;
    }

    void Start()
    {
        item = Instantiate(enemyConfiguration.modelPrefab, spawnPoint.position, spawnPoint.rotation);
        item.AddComponent<EnemyManager>();
        item.GetComponent<EnemyManager>().Configuration = enemyConfiguration;
    }
}
