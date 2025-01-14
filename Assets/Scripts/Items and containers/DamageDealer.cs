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

    // Нанести урон объекту
    private void DealDamageTo(GameObject gameObject)
    {
        // ТУТ МОЖНО СДЕЛАТЬ ИТЕРАЦИЮ ПО ВСЕМ СКРИПТАМ ДЛЯ ПОИСКА СРЕДИ НИХ IDamageable

        // Найти первый скрипт MonoBehaviour (какой-либо менеджер предмета/НПС)
        MonoBehaviour script = gameObject.GetComponent<MonoBehaviour>();
        // Если он существует и подключен к IDamageable – нанести ему урон
        if (script != null && script is IDamageable damageableObject)
            damageableObject.TakeDamage(itemConfig.damage, itemConfig.damageType);
    }
}

// *************************** IDamageable *********************************

// Объект, способный на получение урона и смерть/поломку
public interface IDamageable
{
    // Получить урон
    public void TakeDamage(int damageValue, DamageType damageType);

    // Умереть/уничтожиться
    public void Decease();

}

// *************************** DamageType *********************************

public enum DamageType
{
    sharp = 0,
    blunt = 1
}
