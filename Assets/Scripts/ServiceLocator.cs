using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class ServiceLocator : MonoBehaviour
{
    [field: Header("Player")]
    [field: SerializeField] public PlayerManager playerManager { get; private set; }
    [field: SerializeField] public InputManager inputManager { get; private set; }
    [field: SerializeField] public PlayerLocomotion playerLocomotion { get; private set; }
    [field: SerializeField] public AnimatorManager animatorManager { get; private set; }
    [field: SerializeField] public PlayerInventoryManager playerInventoryManager { get; private set; }
    [field: SerializeField] public BarterProcessor barterProcessor { get; private set; }
    [field: SerializeField] public CameraManager cameraManager { get; private set; }
    [field: SerializeField] public Interactor interactor { get; private set; }
    [field: SerializeField] public GameObject playerEntity { get; private set; }
    [field: SerializeField] public GameObject player { get; private set; }

    [field: Header("UI")]
    [field: SerializeField] public UIManager uiManager { get; private set; }
    [field: SerializeField] public ItemsDropZone itemsDropZone { get; private set; }
    [field: SerializeField] public DescriptionPanelHandler descriptionPanelHandler { get; private set; }
    [field: SerializeField] public ItemSplitterManager itemSplitterManager { get; private set; }
    [field: SerializeField] public ItemMenuHandler itemMenuHandler { get; private set; }

    public static ServiceLocator Instance { get; private set; }

    // =============================================

    private void Awake()
    {
        Instance = ScriptTools.CreateStaticScriptInstance(Instance, this);
    }

    public void RegisterScript(MonoBehaviour script)
    {
        bool registered = true;

        switch (script)
        {
            // --- Player scripts ---
            case PlayerManager PM:
                if (playerManager == null)
                    playerManager = PM;
                break;

            case InputManager IM:
                if (inputManager == null)
                    inputManager = IM;
                break;

            case PlayerLocomotion PL:
                if (playerLocomotion == null)
                    playerLocomotion = PL;
                break;

            case AnimatorManager AM:
                if (animatorManager == null)
                    animatorManager = AM;
                break;

            case PlayerInventoryManager PIM:
                if (playerInventoryManager == null)
                    playerInventoryManager = PIM;
                break;

            case BarterProcessor BP:
                if (barterProcessor == null)
                    barterProcessor = BP;
                break;

            // --- Player camera scripts ---
            case CameraManager CM:
                if (cameraManager == null)
                    cameraManager = CM;
                break;

            case Interactor I:
                if (interactor == null)
                    interactor = I;
                break;

            // --- UI scripts ---
            case UIManager UI:
                if (uiManager == null)
                    uiManager = UI;
                break;

            case ItemsDropZone IDZ:
                if (itemsDropZone == null)
                    itemsDropZone = IDZ;
                break;

            case DescriptionPanelHandler DPH:
                if (descriptionPanelHandler == null)
                    descriptionPanelHandler = DPH;
                break;

            case ItemSplitterManager ISM:
                if (itemSplitterManager == null)
                    itemSplitterManager = ISM;
                break;

            case ItemMenuHandler IMH:
                if (itemMenuHandler == null)
                    itemMenuHandler = IMH;
                break;

            //  ------------
            default:
                registered = false;
                Debug.Log("Unknown script type: " + script.GetType().Name);
                break;
        }

        if (registered)
        {
            //Debug.Log($"Service Locator registered a new script: {script.GetType().Name}");
        }
    }

    public void RegisterPlayerEntity(GameObject playerEntity)
    {
        this.playerEntity = playerEntity;
        player = playerEntity.transform.GetChild(0).gameObject;
    }
}

// ****************** ScriptTools ******************

public static class ScriptTools
{
    public static T CreateStaticScriptInstance<T>(T instance, T script) where T : MonoBehaviour
    {
        if (instance == null)
        {
            instance = script;
            Object.DontDestroyOnLoad(script.gameObject); // Если объект должен пережить смену сцен
        }
        else
        {
            Object.Destroy(script.gameObject); // Удалить дублирующийся экземпляр
        }
        return instance;
    }

    public static void RegisterScript(MonoBehaviour script)
    {
        // Если ServiceLocator уже существует, сразу регистрируем скрипт
        if (ServiceLocator.Instance != null)
            ServiceLocator.Instance.RegisterScript(script);
        // Иначе запускаем корутину для ожидания
        else
            script.StartCoroutine(WaitAndRegister(script));
    }

    public static void RegisterPlayerEntity(GameObject playerEntity)
    {
        // Если ServiceLocator уже существует, сразу регистрируем скрипт
        if (ServiceLocator.Instance != null)
            ServiceLocator.Instance.RegisterPlayerEntity(playerEntity);
    }

    // ================================

    private static IEnumerator WaitAndRegister(MonoBehaviour script)
    {
        // Ждать, пока Instance ServiceLocator не будет создан
        while (ServiceLocator.Instance == null)
            yield return null; // Ждет один кадр

        // После создания ServiceLocator регистрируем скрипт
        ServiceLocator.Instance.RegisterScript(script);
    }
}