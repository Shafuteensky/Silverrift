using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using static UIManager;

public class InputManager : InitializableMonoBehaviour
{
    private ServiceLocator serviceLocator;

    private PlayerManager playerManager;
    private PlayerControls playerControls;
    private PlayerLocomotion playerLocomotion;
    private PlayerInventoryManager inventoryManager;
    private AnimatorManager animatorManager;
    private UIManager uiManager;
    private CameraManager cameraManager;
    private ItemMenuHandler itemMenuHandler;
    [SerializeField] Interactor interactor;
    private GameObject player;

    #region Player movement
    [NonSerialized] public Vector2 movementInput;
    [NonSerialized] public Vector2 cameraInput;

    [NonSerialized] public float cameraInputX;
    [NonSerialized] public float cameraInputY;

    [NonSerialized] public float moveAmount;
    [NonSerialized] public float verticalInput;
    [NonSerialized] public float horizontalInput;

    [NonSerialized] public bool walkInput;
    [NonSerialized] public bool sprintInput;
    [NonSerialized] public bool jumpInput;
    #endregion

    [NonSerialized] public bool interactInput;
    [NonSerialized] public bool anyButton;
    [NonSerialized] public float mouseScrollY;

    #region UI Input
    [NonSerialized] public bool inventoryEquipmentInput;
    [NonSerialized] public bool inventoryCraftInput;
    [NonSerialized] public bool inventoryCompendiumInput;
    [NonSerialized] public bool inventoryInterruptionInput;
    #endregion

    // =========================================

    private void Awake()
    {
        ScriptTools.RegisterScript(this);
    }

    private void Start()
    {
        serviceLocator = ServiceLocator.Instance;

        playerManager = serviceLocator.playerManager;
        animatorManager = serviceLocator.animatorManager;
        playerLocomotion = serviceLocator.playerLocomotion;
        itemMenuHandler = serviceLocator.itemMenuHandler;
        cameraManager = serviceLocator.cameraManager;
        inventoryManager = serviceLocator.playerInventoryManager;

        StartCoroutine(GetUIManager());
    }

    private IEnumerator GetUIManager()
    {
        while (serviceLocator.uiManager == null)
            yield return null;

        uiManager = serviceLocator.uiManager;

        if (AreDependenciesInitialized(playerLocomotion, animatorManager, itemMenuHandler, 
            uiManager, cameraManager, inventoryManager, playerManager))
            Initialize();
    }

    private void OnEnable()
    {
        if (playerControls == null)
        {
            playerControls = new PlayerControls();
            // Общее
            playerControls.General.MouseScrollY.performed += y => mouseScrollY = y.ReadValue<float>();

            // Перемещение
            playerControls.PlayerMovement.Movement.performed += i => movementInput = i.ReadValue<Vector2>();
            playerControls.PlayerMovement.Camera.performed += i => cameraInput = i.ReadValue<Vector2>();

            playerControls.PlayerActions.Walking.performed += i => walkInput = !walkInput;
            playerControls.PlayerActions.Sprinting.performed += i => sprintInput = true;
            playerControls.PlayerActions.Sprinting.canceled += i => sprintInput = false;

            playerControls.PlayerActions.Jumping.performed += i => jumpInput = true;

            // Переключение UI
            playerControls.UI.Inventory.performed += i => inventoryEquipmentInput = !inventoryEquipmentInput;
            playerControls.UI.Craft.performed += i => inventoryCraftInput = !inventoryCraftInput;
            playerControls.UI.Compendium.performed += i => inventoryCompendiumInput = !inventoryCompendiumInput;

            playerControls.UI.Interruption.performed += i => inventoryInterruptionInput = !inventoryInterruptionInput;

            // Переключение активного слота квикбара
            playerControls.QuickbarSwitch.Action1.performed += i => playerManager.ChangeActiveItem(0);
            playerControls.QuickbarSwitch.Action2.performed += i => playerManager.ChangeActiveItem(1);
            playerControls.QuickbarSwitch.Action3.performed += i => playerManager.ChangeActiveItem(2);
            playerControls.QuickbarSwitch.Action4.performed += i => playerManager.ChangeActiveItem(3);
            playerControls.QuickbarSwitch.Action5.performed += i => playerManager.ChangeActiveItem(4);
            playerControls.QuickbarSwitch.Action6.performed += i => playerManager.ChangeActiveItem(5);
            playerControls.QuickbarSwitch.HideActive.performed += i => playerManager.ChangeActiveItem(-1);

            // Взаимодействие с миром
            playerControls.PlayerActions.Interaction.performed += i => interactInput = true;
            playerControls.PlayerActions.PrimaryAction.performed += i => playerManager.PerformPrimaryAction();


            // Любая кнопка
            playerControls.UI.AnyButton.performed += i => anyButton = true;
        }
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    private void Update()
    {
        if (!IsInitialized()) return;

        HandleAllInputs();
    }

    // ===================================

    public void HandleAllInputs()
    {
        // Общее
        HandleMouseScroll();

        // Пока все меню закрыты – управление включено
        if (uiManager.currentLayout == UILayouts.StandardGameplayOverlay) 
        {
            // Движения
            HandleMovementInput();
            HandleWalkingInput();
            HandleSprintingInput();
            HandleJumpingInput();
            //HandleActionInput

            // Действия
            HandlePlayerInteractions();
        }
        // Открыто какое-либо меню – управление отключено
        else
        {
            // Движения
            ResetMovementInput();
        }

        // UI
        HandleItemMenuInterruption();

        // Действия
        HandleInventoryInputs();
    }

    // =============== Общий ввод ===============

    // --------------- Колесико мыши ---------------

    // Вращение колесика мыши
    private void HandleMouseScroll()
    {
        // Scroll UP
        if (mouseScrollY > 0)
            playerManager.ChangeActiveItem(
                inventoryManager.GetScrolledSlotIndex(1));
        // Scroll DOWN
        else if (mouseScrollY < 0)
            playerManager.ChangeActiveItem(
                inventoryManager.GetScrolledSlotIndex(-1));
    }

    // =============== Передвижение ===============

    // Основное передвижение WASD
    private void HandleMovementInput()
    {
        horizontalInput = movementInput.x;
        verticalInput = movementInput.y;

        cameraInputX = cameraInput.x;
        cameraInputY = cameraInput.y;

        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));
        animatorManager.UpdateAnimatorValues(0, moveAmount, playerLocomotion.isInSprinting, playerLocomotion.isInWalking);

        // Динамическое перемещение точки поворота камера п овектору движения персонажа игрока
        cameraManager.MovePivotByFactor(horizontalInput, verticalInput);
    }

    // Остановка любого движения
    private void ResetMovementInput()
    {
        horizontalInput = 0;
        verticalInput = 0;

        cameraInputX = 0;
        cameraInputY = 0;

        moveAmount = 0;
        animatorManager.UpdateAnimatorValues(0, moveAmount, false, false);
    }

    // Пешком/бегом
    private void HandleWalkingInput()
    {
        if (moveAmount != 0 && (walkInput || moveAmount <= 0.5f))
        {
            playerLocomotion.isInWalking = true;
        }
        else
        {
            playerLocomotion.isInWalking = false;
        }
    }

    // Спринт
    private void HandleSprintingInput()
    {
        if (sprintInput && moveAmount > 0.5f)
        {
            playerLocomotion.isInSprinting = true;
        }
        else
        {
            playerLocomotion.isInSprinting = false;
        }
    }

    // Прыжок
    private void HandleJumpingInput()
    {
        if (jumpInput)
        {
            jumpInput = false;
            playerLocomotion.HandleJump();
        }
    }

    // =============== Инвентарь ===============

    // Открытие определенных компоновок UI
    private void HandleInventoryInputs()
    {
        if (inventoryInterruptionInput || interactInput)
        {
            if (inventoryInterruptionInput)
                inventoryInterruptionInput = false;
            if (interactInput)
                interactInput = false;
            uiManager.SwitchLayout(UILayouts.StandardGameplayOverlay);
            playerManager.SetPlayerState(PlayerManager.PlayerStates.moving);
        }
        else
        {
            if (inventoryEquipmentInput)
            {
                inventoryEquipmentInput = false;
                uiManager.SwitchLayout(UILayouts.PlayerEquipment, true);
            }
            else if (inventoryCraftInput)
            {
                inventoryCraftInput = false;
                uiManager.SwitchLayout(UILayouts.CraftMenu);
            }
            else if (inventoryCompendiumInput)
            {
                inventoryCompendiumInput = false;
                uiManager.SwitchLayout(UILayouts.Compendium);
            }
        }
    }

    // Сокрытие меню опций работы с предметом (UI)
    private void HandleItemMenuInterruption()
    {
        if (!anyButton)
            return;

        anyButton = false;

        if (itemMenuHandler.isHovering)
            return;

        if (uiManager.itemMenuPanel.activeSelf == true)
            uiManager.HideSlotMenu();
    }

    // =============== Взаимодействие ===============

    // Основное взаимодействие
    private void HandlePlayerInteractions()
    {
        if (!interactInput)
            return;

        interactInput = false;

        interactor.HandleInteractionWithClosest();
        //uiManager.RepopulateQuickbar();
    }

}
