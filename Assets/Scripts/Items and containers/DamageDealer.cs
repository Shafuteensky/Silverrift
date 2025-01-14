using System.Collections.Generic;
using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] public UsableItemSO itemConfig;

    public void OnTriggerEnter(Collider other)
    {
        DealDamageTo(other.gameObject);
    }

    // =================================

    // ������� ���� �������
    private void DealDamageTo(GameObject gameObject)
    {
        // ��� ����� ������� �������� �� ���� �������� ��� ������ ����� ��� IDamageable

        // ����� ������ ������ MonoBehaviour (�����-���� �������� ��������/���)
        MonoBehaviour script = gameObject.GetComponent<MonoBehaviour>();
        // ���� �� ���������� � ��������� � IDamageable � ������� ��� ����
        if (script != null && script is IDamageable damageableObject)
            damageableObject.TakeDamage(itemConfig.damage, itemConfig.damageType);
    }
}

// *************************** IDamageable *********************************

// ������, ��������� �� ��������� ����� � ������/�������
public interface IDamageable
{
    // �������� ����
    public void TakeDamage(int damageValue, DamageType damageType);

    // �������/������������
    public void Decease();

}

// *************************** DamageType *********************************

public enum DamageType
{
    sharp = 0,
    blunt = 1
}
