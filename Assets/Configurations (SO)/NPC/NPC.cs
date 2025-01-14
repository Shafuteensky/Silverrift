using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Common NPC", menuName = "NPC/Common")]
public class CommonNPCSO : ScriptableObject
{
    [Header("NPC Settings")]
    [field: SerializeField] public LocalizedString nameString { get; private set; }
    [field: SerializeField] public LocalizedString descriptionString { get; private set; }
    [field: SerializeField] public NPCStatsConfigSO config { get; private set; }

    [Header("Resources")]
    [field: SerializeField] public GameObject modelPrefab { get; private set; }

}
