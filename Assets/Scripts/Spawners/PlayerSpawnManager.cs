using RootMotion;
using RootMotion.FinalIK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static RootMotion.FinalIK.Grounding;
using static RootMotion.FinalIK.VRIK;
using static UnityEditor.Experimental.GraphView.GraphView;

public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Spawn settings")]
    [SerializeField] private bool spawnInTheAir = false;
    [field: SerializeField] public List<BasicItemSO> items { get; private set; }

    [Header("Player settings")]
    [SerializeField] private GameObject playerEntityPrefab;
    [SerializeField] private PlayerRaceSO playerRaceConfig;

    [Header("Player Inventory configs")]
    [SerializeField] private ItemContainerConfigSO backpackConfig;
    [SerializeField] private ItemContainerConfigSO quickBarConfig;
    [SerializeField] private ItemContainerConfigSO headSlotConfig;
    [SerializeField] private ItemContainerConfigSO bodySlotConfig;
    [SerializeField] private ItemContainerConfigSO feetSlotConfig;
    [SerializeField] private ItemContainerConfigSO specialSlotConfig;
    [SerializeField] private ItemContainerConfigSO bagSlotConfig;
    [SerializeField] private ItemContainerConfigSO accessorySlotsConfig;
    [SerializeField] private ItemContainerConfigSO craftingFieldConfig;
    [SerializeField] private ItemContainerConfigSO craftingResultsConfig;
    [SerializeField] private ItemContainerConfigSO barterPlayerTransfersConfig;
    [SerializeField] private ItemContainerConfigSO barterTraderTransfersConfig;

    private Transform spawnPoint; // Точка спавнера в пространстве

    private GameObject cameraManager;
    private GameObject cameraPivot; // Пустой объект, вокруг которого поворачивает камера
    private GameObject playerCamera;

    private void Awake()
    {
        spawnPoint = this.gameObject.transform;
        if (!spawnInTheAir)
            spawnPoint.position = GameObjectTools.GetAlignedPosition(this.gameObject);
    }

    private ServiceLocator serviceLocator;

    void Start()
    {
        serviceLocator = ServiceLocator.Instance;

        InstantiatePlayer();

        //GameObject.Destroy(this.gameObject);
    }

    // ======================================================

    // Создать сущность игрока и его модель, пересчитать статы
    private void InstantiatePlayer()
    {
        GameObject playerEntity;
        // Создать сущность персонажа со всеми компонентами игрока (! КРОМЕ АКТУАЛЬНЫХ МОДЕЛИ И КОСТЕЙ !)
        playerEntity = PlayerCharacterTools.InstantiatePlayerEntity(spawnPoint, playerEntityPrefab, playerRaceConfig);
        //Создать модель персонажа после появления менеджера игрока
        StartCoroutine(WaitForPlayerManager());

        StartCoroutine(WaitForInventoryManager(playerEntity));
        //PlayerCharacterTools.LoadPlayer();

        // Активация UI
        serviceLocator.uiManager.enabled = true;
    }
    // Смена расы (физически/модель и статы)
    private IEnumerator WaitForPlayerManager()
    {
        var playerManager = serviceLocator.playerEntity.GetComponent<PlayerManager>();
        while (playerManager.isInitialized == false)
            yield return null;

        // Смена расы игрока с регистрацией точек сцепки
        playerManager.ChangePlayerRace(playerRaceConfig);
    }
    //
    private IEnumerator WaitForInventoryManager(GameObject playerEntity)
    {
        var inventoryManager = serviceLocator.playerEntity.GetComponent<PlayerManager>();
        while (inventoryManager.isInitialized == false)
            yield return null;

        ItemGiver.GiveItems(playerEntity, items);
    }

    #region Depricated

    // ======================= Сборка компонент сущности игрока (если без префаба) ========================

    private void AddRigidbody(GameObject gameObject)
    {
        gameObject.AddComponent<Rigidbody>();
        gameObject.GetComponent<Rigidbody>().mass = 65;
        gameObject.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
        gameObject.GetComponent<Rigidbody>().constraints =
            RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        gameObject.GetComponent<Rigidbody>().useGravity = false;
    }

    private void AddAnimator(GameObject gameObject)
    {
        if (gameObject.GetComponent<Animator>() == null)
            gameObject.AddComponent<Animator>();
        gameObject.GetComponent<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
        gameObject.GetComponent<Animator>().applyRootMotion = false;
        gameObject.GetComponent<Animator>().animatePhysics = true;
    }

    private void AddCamera(GameObject gameObject)
    {
        cameraManager = new GameObject("Camera Entity");
        cameraManager.transform.position = spawnPoint.position;
        cameraManager.transform.rotation = spawnPoint.rotation;
        cameraManager.transform.localPosition = Vector3.zero;
        cameraManager.transform.parent = gameObject.transform;

        cameraPivot = new GameObject("Camera Pivot");
        cameraPivot.transform.parent = cameraManager.transform;
        cameraPivot.transform.localPosition = new Vector3(0.45f, 1.45f, 0f);

        playerCamera = new GameObject("Main Camera");
        playerCamera.AddComponent<Camera>();
        playerCamera.GetComponent<Camera>().GetUniversalAdditionalCameraData().renderPostProcessing = true;
        playerCamera.tag = "MainCamera";
        playerCamera.transform.parent = cameraPivot.transform;
        playerCamera.transform.localPosition = new Vector3(0, 0, -1.2f);

        playerCamera.AddComponent<Interactor>();
        cameraManager.AddComponent<CameraManager>();
    }

    private void AddInventoryManager(GameObject gameObject)
    {
        gameObject.AddComponent<PlayerInventoryManager>();
        PlayerInventoryManager manager = gameObject.GetComponent<PlayerInventoryManager>();
        manager.SetConfigs(
            backpackConfig,
            quickBarConfig,
            headSlotConfig,
            bodySlotConfig,
            feetSlotConfig,
            specialSlotConfig,
            bagSlotConfig,
            accessorySlotsConfig,
            craftingFieldConfig,
            craftingResultsConfig,
            barterPlayerTransfersConfig,
            barterTraderTransfersConfig);
    }

    #endregion

}
