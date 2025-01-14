using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Player race", menuName = "Player/Race")]
public class PlayerRaceSO : ScriptableObject
{
    [field: SerializeField] public LocalizedString nameString { get; private set; }
    [field: SerializeField] public LocalizedString descriptionString { get; private set; }

    [Header("Resources")]
    [field: SerializeField] public GameObject rigAndBones { get; private set; }

    [Header("Stats")] // Коэффециенты статов персонажа класса Player
    [field: SerializeField] [field: Range(0.1f, 2f)] public float healthFactor { get; private set; } = 1;
    [field: SerializeField] [field: Range(0.1f, 2f)] public float staminaFactor { get; private set; } = 1;

    [field: SerializeField][field: Range(0.1f, 2f)] public float walkingSpeedFactor { get; private set; } = 1;
    [field: SerializeField][field: Range(0.1f, 2f)] public float runningSpeedFactor { get; private set; } = 1;
    [field: SerializeField][field: Range(0.1f, 2f)] public float sprintingSpeedFactor { get; private set; } = 1;
    [field: SerializeField][field: Range(0.1f, 2f)] public float rotationSpeedFactor { get; private set; } = 1;
    [field: SerializeField][field: Range(0.1f, 2f)] public float jumpHeightFactor { get; private set; } = 1;

    [field: SerializeField] [field: Range(0.1f, 2f)] public float aberrationResistFactor { get; private set; } = 1;
}
