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
    // ����� ����� ������������ �������� (��� "��������") � �������� �� ������ ������� "������������"
    [SerializeField] private int debrisLifeTime = 5;

    [Header("Drop")]
    [SerializeField] private bool haveDrop = false;
    // ����������� ����� ����� (������ ����� �� �������� � ������������ ����� �� ������������)
    [SerializeField] [Range(0.1f, 3f)] private float dropModifier = 1f;
    [SerializeField] private ItemSpawnerConfigurationSO dropConfiguration;

    // ====================================

    public void TakeDamage(int damageValue, DamageType damageType)
    {
        // ��� ������������ �� DamageType
        health -= damageValue;

        if (health > 0)
        {
            // ��� ���� ��������� �����
            ObjectShaker.StartShake(this, this.transform, 1f, 0.025f, 10f);
            return;
        }

        // ��� ���� �����������
        ObjectShaker.StopShake();
        Decease();
    }

    public void Decease()
    {
        // �������� ������
        GameObjectTools.HideObject(this.gameObject);

        // ������� ��� ����������� ������ � ���������� ����������
        GameObject destructedObject = Instantiate(destructedObjPrefab, this.transform);
        destructedObject.transform.localPosition = Vector3.zero;

        // ���������� ���� (���)
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

        // ���� ���������� �������� ��������
        foreach (Transform child in destructedObject.transform)
        {
            child.gameObject.layer = LayerMask.NameToLayer("Destructed");
            child.AddComponent<BoxCollider>();
            child.AddComponent<Rigidbody>();
        }

        // ������ ����� ������� ����������� �������
        StartCoroutine(WaitAndDestroy(debrisLifeTime));
    }

    // �������a
    private IEnumerator WaitAndDestroy(float delayInSeconds)
    {
        yield return new WaitForSeconds(delayInSeconds);

        StartCoroutine(GameObjectTools.FadeOutAndDestroy(this.gameObject));
    }
}







