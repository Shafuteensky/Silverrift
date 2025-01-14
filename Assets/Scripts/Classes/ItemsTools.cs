using RootMotion.FinalIK;
using RootMotion;
using UnityEngine;
using System;
using System.ComponentModel;

// Иструменты работы с физическим обличием персонажа игрока (моделью и ее компонентами)
public static class ItemTools
{
    /// <summary>
    /// Добавить элемент DamageDealer объекту <paramref name="gameObject"/> в зависимости от
    /// конфигурации его типа <paramref name="config"/>
    /// </summary>
    /// <param name="gameObject"> sadk sks </param>
    /// <param name="config"></param>
    public static void AddDamageDealer(GameObject gameObject, UsableItemSO config)
    {
        // Объект-родитель: добавляемый в иерархию gameObject объект
        GameObject damageDealer = new GameObject("Damage dealer");
        Transform damageDealerTransform = damageDealer.transform;
        damageDealerTransform.parent = gameObject.transform;
        damageDealerTransform.localPosition = Vector3.zero;
        damageDealerTransform.localRotation = damageDealerTransform.parent.localRotation;
        damageDealer.layer = LayerMask.NameToLayer("Hitbox");

        // Коллайдер зоны урона
        BoxCollider collider = damageDealer.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = config.weaponType.damageColliderCenter;
        collider.size = config.weaponType.damageColliderSize;

        // Скрипт нанесения урона коллайдером
        DamageDealer damageDealerScript = damageDealer.AddComponent<DamageDealer>();
        damageDealerScript.itemConfig = config;

        // По-умолчанию отключить dd
        damageDealer.SetActive(false);
    }

    // Активировать/деактивировать коллайдер damage dealer-а
    public static void SetDamageDealerState(GameObject gameObject, bool state)
    {
        BoxCollider damageDealer = gameObject.GetComponent<BoxCollider>();

        if (damageDealer == null)
            return;

        damageDealer.enabled = state;
    }
}