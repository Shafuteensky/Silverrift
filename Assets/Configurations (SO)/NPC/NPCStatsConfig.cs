using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Common NPC", menuName = "NPC/Common")]
public class NPCStatsConfigSO : ScriptableObject
{
    [Header("Stats")]
    [field: SerializeField] public int health { get; private set; } = 110;
    //// [SerializeField] private AttackType attackType = ;
    //[SerializeField] private int damage = 5;
    //// [SerializeField] private ArmorType armorType = ;
    //[SerializeField] private int armorPoints = 20;
    //[SerializeField] private float attackRate = 0.5f;
    //[SerializeField] private float movingSpeed = 0.5f;
    //[SerializeField] private bool isRanged = false;
    //[SerializeField] private float rangeDistance = 10;
    //[SerializeField] private float targetDistance = 10;
    //[SerializeField] private int numberOfAttackVariants = 1;

    //[Header("Parameters")]
    //[SerializeField] private int Experience = 1;
    //[SerializeField] private int dangerLevel = 1;
    //[SerializeField] private int startSpawningLevel = 1;

    //[Header("AI")]
    //[SerializeField] private float aiUpdateInterval = 0.1f;
    //[SerializeField] private int aiType = 0;
    //[SerializeField] private float stoppingDistance = 1;
    //[SerializeField] private float acceleration = 10;
    //[SerializeField] private float angularSpeed = 150;
    //[SerializeField] private float baseOffset = 1;
    //[SerializeField] private bool autoTraverseOffMeshlink = true;
    //[SerializeField] private bool autoRepath = true;
    //[SerializeField] private bool autoBraking = true;
    //[SerializeField] private ObstacleAvoidanceType obstacleAvoidanceType = ObstacleAvoidanceType.Standard;
}
