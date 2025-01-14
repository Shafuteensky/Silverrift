using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using static LeTai.Asset.TranslucentImage.VPMatrixCache;
using static UnityEngine.Rendering.VolumeComponent;

public class PlayerManager : InitializableMonoBehaviour
{
    //InputManager inputManager;
    //CameraManager cameraManager;
    public Animator animator { get; private set; }
    GameObject player;

    public PlayerRaceSO playerRaceConfig { get; private set; }
    public PlayerStats playerStats { get; private set; }

    public TraderManager tradingWith;

    public PlayerCharacterPoints characterPoints = new PlayerCharacterPoints();

    private void Awake()
    {
        ScriptTools.RegisterScript(this);
    }

    private void OnEnable()
    {
        playerStats = new PlayerStats(); // !!! ��������� �� ���������� ���-���� ������
        SetPlayerState(PlayerStates.moving);
    }

    private ServiceLocator serviceLocator;
    private AnimatorManager animatorManager;
    private PlayerInventoryManager playerInventoryManager;

    private void Start()
    {
        serviceLocator = ServiceLocator.Instance;

        animatorManager = ServiceLocator.Instance.animatorManager;
        playerInventoryManager = ServiceLocator.Instance.playerInventoryManager;

        player = serviceLocator.player;
        animator = player.GetComponent<Animator>();

        if (AreDependenciesInitialized(animatorManager)
            && AreDependenciesInitialized(player))
            Initialize();

        // ??? ����� �������� ��������� ���� ������
    }

    // ================= ��������� ==================

    public enum PlayerStates
    {
        moving = 0,
        bartering = 1,
        looting = 2,
        inventory = 3
    }
    public PlayerStates state { get; private set; } = PlayerStates.moving;

    // ����� ��������� ������
    public void SetPlayerState(PlayerStates state)
    {
        void GeneralFunctions()
        {
            Cursor.lockState = CursorLockMode.None;
        }

        // -----------------------

        this.state = state;

        switch (state)
        {
            case PlayerStates.moving:
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    break;
                }
            case PlayerStates.bartering:
                {
                    GeneralFunctions();
                    break;
                }
            case PlayerStates.looting:
                {
                    GeneralFunctions();
                    break;
                }
            default:
                {
                    GeneralFunctions();
                    break;
                }
        }
    }

    // ================= �������� ������ ==================

    // �������� ���� ��������� (������ � �����)
    public void ChangePlayerRace(PlayerRaceSO newRaceConfig)
    {
        playerRaceConfig = newRaceConfig;
        characterPoints = PlayerCharacterTools.ChangePCRace(player, newRaceConfig);
        UpdatePlayerStats();
    }

    // ================== ����� ===================

    // ����������� ����� ��������� ������
    private void UpdatePlayerStats()
    {
        playerStats = new PlayerStats();

        playerStats = GetBaseStats();
        // playerStats = AddNStats();
    }

    // �������� ������� ����� ������ �� ������� ����������
    public PlayerStats GetBaseStats()
    {
        PlayerStats stats = new PlayerStats();

        if (playerRaceConfig == null) return stats;

        CalculateStat(ref stats.health, playerRaceConfig.healthFactor);
        CalculateStat(ref stats.stamina, playerRaceConfig.staminaFactor);

        CalculateStat(ref stats.walkingSpeed, playerRaceConfig.walkingSpeedFactor);
        CalculateStat(ref stats.runningSpeed, playerRaceConfig.runningSpeedFactor);
        CalculateStat(ref stats.sprintingSpeed, playerRaceConfig.sprintingSpeedFactor);
        CalculateStat(ref stats.rotationSpeed, playerRaceConfig.rotationSpeedFactor);
        CalculateStat(ref stats.jumpHeight, playerRaceConfig.jumpHeightFactor);

        CalculateStat(ref stats.aberrationResist, playerRaceConfig.aberrationResistFactor);

        return stats;
    }

    private static void CalculateStat(ref int stat, float factor)
    {
        stat = (int)Mathf.Ceil(stat * factor);
    }
    private static void CalculateStat(ref float stat, float factor)
    {
        stat = Mathf.Round((stat * factor) * 100f) / 100f;
    }

    // ================== �������� ������� � ���� ===================

    // ��������� �������� �������� � ���
    public void PerformPrimaryAction()
    {
        if (!IsInitialized())
            return;

        if (state != PlayerStates.moving)
            return;

        BasicItemSO activeQuickBarItem = playerInventoryManager.GetActiveItem();
        if (activeQuickBarItem == null)
            return;

        // �������� �������� �������� �����
        string animationName = activeQuickBarItem.primaryActionAnimationName;

        // ������ � ����������� � �������� ����� ----------------------
        if (activeQuickBarItem is UsableItemSO)
        { 
            playerInventoryManager.SetActiveItemObjectDamageDealerState(true);
            animatorManager.PlayAnimationAndThenFunction(
                "Upper torso",
                animationName,
                () => playerInventoryManager.SetActiveItemObjectDamageDealerState(false));
        }

        // ������������ � ������������ ----------------------
        else if (activeQuickBarItem is ConsumableItemSO consumableItem)
        {
            animatorManager.PlayAnimationAndThenFunction(
                "Upper torso",
                animationName,
                () => playerInventoryManager.quickBar.RemoveItem(playerInventoryManager.activeItemSlotIndex, 1)
                );
            //() => consumableItem.Use()
        }

        // ���������� � ������ ----------------------
        else if (activeQuickBarItem is EquipableItemSO equipableItem)
        {
            //equipableItem.Equip();
        }

        // ������� ���� � ??? ----------------------
        else
        {

        }    
    }

    private Coroutine waitCoroutine;
    private int lastIndex = -1;
    private bool itemChanged = false;

    // ������� �������� �������
    public void ChangeActiveItem(int newItemIndex)
    {
        if (!IsInitialized())
            return;

        string activeItemSheatheAnim = "";
        // � ����� ���� ������� � ������ ���
        if (itemChanged == false && playerInventoryManager.IsAnyItemActive())
        {
            // ��������� �������� ��������
            BasicItemSO activeItem = playerInventoryManager.GetActiveItem();
            // �������� �������� ��� ��������� ������
            if (activeItem is UsableItemSO usableItem)
                activeItemSheatheAnim = usableItem.weaponType.sheathAnimationName;
            // ����������� �������� �������� ��������� �������� � ��� ���� ��������� ���������
            else
                activeItemSheatheAnim = "SheatheItem";

            itemChanged = true;
            animatorManager.PlayAnimations(
                "Upper torso",
                activeItemSheatheAnim,
                () => playerInventoryManager.RemoveItemFromHand(),
                "",
                null
                );
        }

        // ����� ����� � ���������
        serviceLocator.uiManager?.DefocusFocusedQuickbarSlot();
        // ���������� �����
        serviceLocator.uiManager?.SetQuickbarCellFocused(newItemIndex, true);
        // ������� ������� ����� ����� ������� �������� � ����
        lastIndex = newItemIndex;
        if (waitCoroutine != null)
            StopCoroutine(waitCoroutine);
        waitCoroutine = StartCoroutine(WaitAndCheck(newItemIndex, () => TakeItemWithAnims()));

        // -------------------------------------------

        // ������� ������� ����� ����� ������� �������� � ����
        IEnumerator WaitAndCheck(int index, Action action)
        {
            yield return new WaitForSeconds(0.5f);
            if (index == lastIndex)
                action?.Invoke();
        }

        // ����� ������� � ����
        void TakeItemWithAnims()
        {
            itemChanged = false;

            // ��������� �������� ����������
            BasicItemSO newActiveItem = playerInventoryManager.GetQuickbarItemByIndex(newItemIndex);
            string newActiveItemDrawAnim;
            // �������� ���������� ��� ������ ��������� ������
            if (newActiveItem is UsableItemSO newUsableItem)
                newActiveItemDrawAnim = newUsableItem.weaponType.drawAnimationName;
            // ����������� �������� ���������� ������ ��������� �������� � ��� ���� ��������� ���������
            else
                newActiveItemDrawAnim = "DrawItem";

            // ��� "������������" �� ������� � �������������� ���
            if (newItemIndex == playerInventoryManager.activeItemSlotIndex)
                newItemIndex = -1;
            // ���� �������� �������� (���� �����) � ��������� ����� �� ����� � ����� �� ���
            if (newItemIndex == -1 && playerInventoryManager.activeItemSlotIndex == -1)
                newItemIndex = playerInventoryManager.lastActiveItemSlotIndex;

                print(newItemIndex);
            // ����� ������� ���������� � ���� �� ������� �� ���� � �� ����������� (�������� ������ �� ���; -1)
            if (playerInventoryManager.IsQuickbarIndHasItem(newItemIndex))
            {
                // ����� ������� � ���� � ���������
                animatorManager.PlayAnimations(
                    "Upper torso",
                    newActiveItemDrawAnim,
                    () => playerInventoryManager.SetActiveSlotByIndex(newItemIndex),
                    "",
                    null,
                    activeItemSheatheAnim
                    );
            }
            // ����� ������� ����������� ��� ��������� ����������� (�������� ������ �� ���)
            else
            {
                playerInventoryManager.SetActiveSlotByIndex(newItemIndex);
            }
        }
    }
}

// ����� �������� �������� �� ������ ��������� ������
public class PlayerCharacterPoints
{
    public Transform rightHandHoldingPoint;
}
