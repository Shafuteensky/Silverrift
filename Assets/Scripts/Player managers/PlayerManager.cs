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
        playerStats = new PlayerStats(); // !!! ПЕРЕНЕСТИ ПО РАЗРАБОТКЕ РПГ-СТАТ ИГРОКА
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

        // ??? ЗДЕСЬ ЗАГРУЗКА СОСТОЯНИЙ СТАТ ИГРОКА
    }

    // ================= Состояния ==================

    public enum PlayerStates
    {
        moving = 0,
        bartering = 1,
        looting = 2,
        inventory = 3
    }
    public PlayerStates state { get; private set; } = PlayerStates.moving;

    // Смена состояния игрока
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

    // ================= Персонаж игрока ==================

    // Изменить расу персонажа (модель и статы)
    public void ChangePlayerRace(PlayerRaceSO newRaceConfig)
    {
        playerRaceConfig = newRaceConfig;
        characterPoints = PlayerCharacterTools.ChangePCRace(player, newRaceConfig);
        UpdatePlayerStats();
    }

    // ================== Статы ===================

    // Пересчитать статы персонажа заново
    private void UpdatePlayerStats()
    {
        playerStats = new PlayerStats();

        playerStats = GetBaseStats();
        // playerStats = AddNStats();
    }

    // Получить базовые статы исходя из расовых множителей
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

    // ================== Активный предмет в руке ===================

    // Совершить основное действие – ЛКМ
    public void PerformPrimaryAction()
    {
        if (!IsInitialized())
            return;

        if (state != PlayerStates.moving)
            return;

        BasicItemSO activeQuickBarItem = playerInventoryManager.GetActiveItem();
        if (activeQuickBarItem == null)
            return;

        // Получить название анимации удара
        string animationName = activeQuickBarItem.primaryActionAnimationName;

        // Оружие и инстуремнты – провести атаку ----------------------
        if (activeQuickBarItem is UsableItemSO)
        { 
            playerInventoryManager.SetActiveItemObjectDamageDealerState(true);
            animatorManager.PlayAnimationAndThenFunction(
                "Upper torso",
                animationName,
                () => playerInventoryManager.SetActiveItemObjectDamageDealerState(false));
        }

        // Потребляемое – использовать ----------------------
        else if (activeQuickBarItem is ConsumableItemSO consumableItem)
        {
            animatorManager.PlayAnimationAndThenFunction(
                "Upper torso",
                animationName,
                () => playerInventoryManager.quickBar.RemoveItem(playerInventoryManager.activeItemSlotIndex, 1)
                );
            //() => consumableItem.Use()
        }

        // Надеваемое – надеть ----------------------
        else if (activeQuickBarItem is EquipableItemSO equipableItem)
        {
            //equipableItem.Equip();
        }

        // Простая вещь – ??? ----------------------
        else
        {

        }    
    }

    private Coroutine waitCoroutine;
    private int lastIndex = -1;
    private bool itemChanged = false;

    // Сменить активный предмет
    public void ChangeActiveItem(int newItemIndex)
    {
        if (!IsInitialized())
            return;

        string activeItemSheatheAnim = "";
        // В руках есть предмет – убрать его
        if (itemChanged == false && playerInventoryManager.IsAnyItemActive())
        {
            // Установка анимаций убирания
            BasicItemSO activeItem = playerInventoryManager.GetActiveItem();
            // Анимация убирания для активного оружия
            if (activeItem is UsableItemSO usableItem)
                activeItemSheatheAnim = usableItem.weaponType.sheathAnimationName;
            // Стандартная анимация убирания активного предмета – для всех остальных предметов
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

        // Снять фокус с активного
        serviceLocator.uiManager?.DefocusFocusedQuickbarSlot();
        // Зафокусить новый
        serviceLocator.uiManager?.SetQuickbarCellFocused(newItemIndex, true);
        // Ожидать краткое время перед взятием предмета в руки
        lastIndex = newItemIndex;
        if (waitCoroutine != null)
            StopCoroutine(waitCoroutine);
        waitCoroutine = StartCoroutine(WaitAndCheck(newItemIndex, () => TakeItemWithAnims()));

        // -------------------------------------------

        // Ожидать краткое время перед взятием предмета в руки
        IEnumerator WaitAndCheck(int index, Action action)
        {
            yield return new WaitForSeconds(0.5f);
            if (index == lastIndex)
                action?.Invoke();
        }

        // Взять предмет в руки
        void TakeItemWithAnims()
        {
            itemChanged = false;

            // Установка анимаций доставания
            BasicItemSO newActiveItem = playerInventoryManager.GetQuickbarItemByIndex(newItemIndex);
            string newActiveItemDrawAnim;
            // Анимации доставания для нового активного оружия
            if (newActiveItem is UsableItemSO newUsableItem)
                newActiveItemDrawAnim = newUsableItem.weaponType.drawAnimationName;
            // Стандартная анимация доставания нового активного предмета – для всех остальных предметов
            else
                newActiveItemDrawAnim = "DrawItem";

            // При "переключении" на текущий – деактивировать его
            if (newItemIndex == playerInventoryManager.activeItemSlotIndex)
                newItemIndex = -1;
            // Если активное спрятано (руки пусты) а последняя чейка не пуста – взять из нее
            if (newItemIndex == -1 && playerInventoryManager.activeItemSlotIndex == -1)
                newItemIndex = playerInventoryManager.lastActiveItemSlotIndex;

                print(newItemIndex);
            // Новый предмет существует – слот по индексу не пуст и не деактивация (предметы убраны из рук; -1)
            if (playerInventoryManager.IsQuickbarIndHasItem(newItemIndex))
            {
                // Взять предмет в руки с анимацией
                animatorManager.PlayAnimations(
                    "Upper torso",
                    newActiveItemDrawAnim,
                    () => playerInventoryManager.SetActiveSlotByIndex(newItemIndex),
                    "",
                    null,
                    activeItemSheatheAnim
                    );
            }
            // Новый предмет отсутствует или произошла деактивация (предметы убраны из рук)
            else
            {
                playerInventoryManager.SetActiveSlotByIndex(newItemIndex);
            }
        }
    }
}

// Точки привязки объектов на моделе персонажа игрока
public class PlayerCharacterPoints
{
    public Transform rightHandHoldingPoint;
}
