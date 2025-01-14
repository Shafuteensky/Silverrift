using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Container configuration", menuName = "Items/Container config", order = 55)]
public class ItemContainerConfigSO : ScriptableObject
{
    [field: SerializeField] public LocalizedString nameString { get; private set; }
    [field: SerializeField] [field: Range(1, 100)] public int numberOfSlots { get; private set; } = 5;

    // ���� ���������� � �������� � ������ � ����������� (��� ��������� ������ � ��� ��������)
    [Tooltip("If permanent � contants saved at all times (player inventory and stashes, traders); not � deleted with scene")]
    [field: SerializeField] public bool isPermanent { get; private set; } = false;

    // ������������ �� �������� ��������� � ��� �������� ���� ��������� �������
    [field: SerializeField] public bool destroyIfEmpty { get; private set; } = false;

    // False � ����� ��������� ������� ������ ���� ItemType; True � ������ ���� slotsType
    [Tooltip("Does the container hold items of a certain type. If so, Slots Type defines the type")]
    [field: SerializeField] public bool isSpecialized { get; private set; } = false;

    // ������ ���� �������� ����� ��������� � ��������� � ���������
    [Tooltip("Type of the items, allowed to be contained. Active if Is Specialized = True")]
    [field: SerializeField] public ItemTypeSO slotsType { get; private set; }

    // ��������� ���������� �������� ������ ����� ���������� ����� (���� ���� ��������� ����������)
    [Tooltip("True � reads and applies contained items effects")]
    [field: SerializeField] public containerTag groupTag { get; private set; } = containerTag.common;

    // ��������� � ��������� �� ������ ������������ � ���� ���������
    [Tooltip("True � reads and applies contained items effects")]
    [field: SerializeField] public bool isApplyingItemsEffects { get; private set; }

    // ������ ���������, ���������� ��� �������� �� ����� ����������
    [Tooltip("This items will be generated in this container, if list is not empty")]
    [field: SerializeField] public List<BasicItemSO> itemsInIt { get; private set; }
}

public enum containerTag
{
    common,
    equipment,
    trader
}
