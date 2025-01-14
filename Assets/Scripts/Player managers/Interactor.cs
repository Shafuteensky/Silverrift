using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using HighlightPlus;
using System.Linq;

public class Interactor : MonoBehaviour
{
    private UIManager UIManager;

    [Header("Sources")]
    [SerializeField] private GameObject playerModel; // Сам персонаж для замера досягаемости до предмета от него
    [SerializeField] private Transform interactorSource; // Исходная точка луча – центр игровой камеры

    [Header("Highlighting")]
    [field: SerializeField] public HighlightProfile detectingInteractableProfile { get; private set; }
    [field: SerializeField] public HighlightProfile lookingAtInteractableProfile { get; private set; }

    private float highlightRadius = 2f; // Радиус подсветки
    private float interactRange = 2f; // Дистанция взаимодействия (не дальность луча) от персонажа
    private float focusRange = 10f; // Дистанция взаимодействия (дальность луча)
    private float interactSphereRadius = 0.25f; // Радиус сферы подъема предметов

    private Ray lookRay; // Луч итерактора
    private RaycastHit raycastHit; // Информация о луче из центра камеры
    private Collider[] nearestToRayEndColliders; // Ближайшие коллайдеры на конце луча
    private Collider[] nearestToPlayerColliders; // Ближайшие коллайдеры вокруг игрока
    private Collider closestCollider;
    private IInteractable closestInteractable;
    private float lookRayMaxRange = 1000f; // Дальность луча итерактора
    private List<IInteractable> highlightedObjects = new List<IInteractable>(); // Объекты внутри сферы подсветки от игрока

    //private float highlightUpdateInterval = 0.2f;
    private float lastHighlightUpdateTime = 0;

    private void Awake()
    {
        ScriptTools.RegisterScript(this);
    }

    private ServiceLocator serviceLocator;

    private void Start()
    {
        serviceLocator = ServiceLocator.Instance;

        UIManager = serviceLocator.uiManager;
    }

    private void Update()
    {
        //if (Time.time - lastHighlightUpdateTime >= highlightUpdateInterval) // Оптимизация – обновления реже
        {
            ClearInteractionInfo();
            if (!CastRay())
                return;

            GetColliders();
            HandleHighlight();
            ShowClosestObjectInfo();

            lastHighlightUpdateTime = Time.time;
        }
    }

    // ======================= Raycast =========================

    private bool CastRay()
    {
        interactorSource = this.transform; // Начало луча от центра камеры
        lookRay = new Ray(interactorSource.position, interactorSource.forward);
        Debug.DrawRay(interactorSource.position, interactorSource.forward, Color.yellow, 0, true);

        // На пути луча найден коллайдер
        if (!Physics.Raycast(lookRay, out raycastHit, lookRayMaxRange)) 
            return false;

        // Точка на допустимой от персонажа дистанции
        if (Vector3.Distance(playerModel.transform.position, raycastHit.point) > interactRange) 
            return false;

        return true;
    }

    private void GetColliders()
    {
        // Сфера на конце луча [, 1<<7] – все коллайдеры (любой IInteractable)
        nearestToRayEndColliders = Physics.OverlapSphere(raycastHit.point, interactSphereRadius); 

        // Поиск ближайшего к центру этой сферы коллайдера
        float closestDistance = float.MaxValue;
        foreach (var collider in nearestToRayEndColliders)
        {
            float distance = Vector3.Distance(raycastHit.point, collider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCollider = collider;
            }
        }
        // Коллайдер взаимодействуемый
        if (closestCollider != null)
            closestCollider.TryGetComponent(out closestInteractable);

        // Сфера вокруг персонажа – только поднимаемые предметы
        nearestToPlayerColliders = Physics.OverlapSphere(playerModel.transform.position, highlightRadius,
            LayerMask.GetMask("Items"));
    }

    // ======================== Interaction ========================

    // Обработчик инициации взаимодействия игрока с ближайшим объектом при нажатии игроком клавиши взаимодействия
    public void HandleInteractionWithClosest()
    {
        // Коллайдер есть и он взаимодействуемый
        if (closestCollider == null || closestInteractable == null) 
            return;

        // Ближайший объект на допустимой от персонажа дистанции
        if (Vector3.Distance(playerModel.transform.position, closestCollider.transform.position) > interactRange) 
            return;

        // Взаимодействие с одним
        //closestInteractable?.Interact(); 
        // Взаимодействие с каждым IInteractable объекта коллайдера
        var mObjs = closestCollider.GetComponents<MonoBehaviour>();
        IInteractable[] interfaceScripts = (from a in mObjs where a.GetType().GetInterfaces().
                                            Any(k => k == typeof(IInteractable)) select (IInteractable)a).ToArray();
        foreach (var iScript in interfaceScripts)
            iScript.Interact();
    }

    private void ShowClosestObjectInfo()
    {
        // Коллайдера нет или он не взаимодействуемый
        if (closestCollider == null || closestInteractable == null)
        {
            UIManager.HideTargetInfo();
            return;
        }

        // Предмет не на фокусной от персонажа дистанции
        if (Vector3.Distance(playerModel.transform.position, closestCollider.transform.position) > focusRange) 
        {
            UIManager.HideTargetInfo();
            return;
        }

        UIManager.ShowTargetInfo(string.Join("\n", closestInteractable.GetInfo()));
    }

    // ======================== Highlight ========================

    private void HandleHighlight()
    {
        HighlightAllNearest(detectingInteractableProfile);
        HighlightClosest(lookingAtInteractableProfile);
    }

    private void HighlightAllNearest(HighlightProfile highlightProfile)
    {
        // Убрать подсветку с предыдущих объектов
        foreach (var obj in highlightedObjects)
        {
            if (obj == null || obj is null || obj.Equals(null))
                continue;

            obj.RemoveHighlight();
        }
        highlightedObjects.Clear();

        // Подсветить новые объекты
        foreach (var collider in nearestToPlayerColliders)
        {
            if (collider.TryGetComponent(out IInteractable interactable))
            {
                interactable.Highlight(highlightProfile);
                highlightedObjects.Add(interactable);
            }
        }
    }

    private void HighlightClosest(HighlightProfile highlightProfile)
    {
        if (closestCollider == null)
            return;

        Highlight(closestCollider, highlightProfile);
    }

    private void Highlight(Collider collider, HighlightProfile profile)
    {
        if (collider == null)
            return;

        if (collider.TryGetComponent(out IInteractable interactable))
            interactable.Highlight(profile);
    }

    // ===================================

    private bool IsInteractable(Collider collider)
    {
        if (collider.TryGetComponent(out IInteractable interactable))
            return true;
        else
            return false;
    }

    private void ClearInteractionInfo()
    {
        raycastHit = new RaycastHit();
        closestCollider = null;
        closestInteractable = null;
        nearestToRayEndColliders = null;
        nearestToPlayerColliders = null;
    }

}

// ***********************************************

// Интерфейс взаимодействуемых предметов
public interface IInteractable
{
    public void Interact();
    public void Highlight(HighlightPlus.HighlightProfile profile);
    public void RemoveHighlight();
    public List<string> GetInfo();
}
