using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ScriptablesCacher : MonoBehaviour
{
    [field: Header("Test items")]
    [field: SerializeField] public BasicItemSO item { get; private set; }

    [field: Header("Item types")]
    [field: SerializeField] public ItemTypeSO ITAccessory { get; private set; }
    [field: SerializeField] public ItemTypeSO ITBackpack { get; private set; }
    [field: SerializeField] public ItemTypeSO ITHeadgear { get; private set; }
    [field: SerializeField] public ItemTypeSO ITClothingTop { get; private set; }
    [field: SerializeField] public ItemTypeSO ITClothingBottom { get; private set; }
    [field: SerializeField] public ItemTypeSO ITSpecial { get; private set; }
    [field: SerializeField] public List<ItemTypeSO> ITEquipables { get; private set; }
    [field: SerializeField] public ItemTypeSO ITConsumable { get; private set; }
    [field: SerializeField] public ItemTypeSO ITCurrency { get; private set; }
    [field: SerializeField] public ItemTypeSO ITMaterial { get; private set; }
    [field: SerializeField] public ItemTypeSO ITTool { get; private set; }
    [field: SerializeField] public ItemTypeSO ITWeapon { get; private set; }

    [field: Header("Item rarities")]
    [field: SerializeField] public ItemRaritySO IRNoRarity { get; private set; }
    [field: SerializeField] public ItemRaritySO IRCommon { get; private set; }
    [field: SerializeField] public ItemRaritySO IRUncommon { get; private set; }
    [field: SerializeField] public ItemRaritySO IRRare { get; private set; }
    [field: SerializeField] public ItemRaritySO IREpic { get; private set; }
    [field: SerializeField] public ItemRaritySO IRLegendary { get; private set; }
    [field: SerializeField] public ItemRaritySO IRMystic { get; private set; }

    public static ScriptablesCacher Instance { get; private set; }

    private void Awake()
    {
        Instance = ScriptTools.CreateStaticScriptInstance(Instance, this);
    }
}
