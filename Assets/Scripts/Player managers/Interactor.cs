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
    [SerializeField] private GameObject playerModel; // ��� �������� ��� ������ ������������ �� �������� �� ����
    [SerializeField] private Transform interactorSource; // �������� ����� ���� � ����� ������� ������

    [Header("Highlighting")]
    [field: SerializeField] public HighlightProfile detectingInteractableProfile { get; private set; }
    [field: SerializeField] public HighlightProfile lookingAtInteractableProfile { get; private set; }

    private float highlightRadius = 2f; // ������ ���������
    private float interactRange = 2f; // ��������� �������������� (�� ��������� ����) �� ���������
    private float focusRange = 10f; // ��������� �������������� (��������� ����)
    private float interactSphereRadius = 0.25f; // ������ ����� ������� ���������

    private Ray lookRay; // ��� ����������
    private RaycastHit raycastHit; // ���������� � ���� �� ������ ������
    private Collider[] nearestToRayEndColliders; // ��������� ���������� �� ����� ����
    private Collider[] nearestToPlayerColliders; // ��������� ���������� ������ ������
    private Collider closestCollider;
    private IInteractable closestInteractable;
    private float lookRayMaxRange = 1000f; // ��������� ���� ����������
    private List<IInteractable> highlightedObjects = new List<IInteractable>(); // ������� ������ ����� ��������� �� ������

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
        //if (Time.time - lastHighlightUpdateTime >= highlightUpdateInterval) // ����������� � ���������� ����
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
        interactorSource = this.transform; // ������ ���� �� ������ ������
        lookRay = new Ray(interactorSource.position, interactorSource.forward);
        Debug.DrawRay(interactorSource.position, interactorSource.forward, Color.yellow, 0, true);

        // �� ���� ���� ������ ���������
        if (!Physics.Raycast(lookRay, out raycastHit, lookRayMaxRange)) 
            return false;

        // ����� �� ���������� �� ��������� ���������
        if (Vector3.Distance(playerModel.transform.position, raycastHit.point) > interactRange) 
            return false;

        return true;
    }

    private void GetColliders()
    {
        // ����� �� ����� ���� [, 1<<7] � ��� ���������� (����� IInteractable)
        nearestToRayEndColliders = Physics.OverlapSphere(raycastHit.point, interactSphereRadius); 

        // ����� ���������� � ������ ���� ����� ����������
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
        // ��������� �����������������
        if (closestCollider != null)
            closestCollider.TryGetComponent(out closestInteractable);

        // ����� ������ ��������� � ������ ����������� ��������
        nearestToPlayerColliders = Physics.OverlapSphere(playerModel.transform.position, highlightRadius,
            LayerMask.GetMask("Items"));
    }

    // ======================== Interaction ========================

    // ���������� ��������� �������������� ������ � ��������� �������� ��� ������� ������� ������� ��������������
    public void HandleInteractionWithClosest()
    {
        // ��������� ���� � �� �����������������
        if (closestCollider == null || closestInteractable == null) 
            return;

        // ��������� ������ �� ���������� �� ��������� ���������
        if (Vector3.Distance(playerModel.transform.position, closestCollider.transform.position) > interactRange) 
            return;

        // �������������� � �����
        //closestInteractable?.Interact(); 
        // �������������� � ������ IInteractable ������� ����������
        var mObjs = closestCollider.GetComponents<MonoBehaviour>();
        IInteractable[] interfaceScripts = (from a in mObjs where a.GetType().GetInterfaces().
                                            Any(k => k == typeof(IInteractable)) select (IInteractable)a).ToArray();
        foreach (var iScript in interfaceScripts)
            iScript.Interact();
    }

    private void ShowClosestObjectInfo()
    {
        // ���������� ��� ��� �� �� �����������������
        if (closestCollider == null || closestInteractable == null)
        {
            UIManager.HideTargetInfo();
            return;
        }

        // ������� �� �� �������� �� ��������� ���������
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
        // ������ ��������� � ���������� ��������
        foreach (var obj in highlightedObjects)
        {
            if (obj == null || obj is null || obj.Equals(null))
                continue;

            obj.RemoveHighlight();
        }
        highlightedObjects.Clear();

        // ���������� ����� �������
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

// ��������� ����������������� ���������
public interface IInteractable
{
    public void Interact();
    public void Highlight(HighlightPlus.HighlightProfile profile);
    public void RemoveHighlight();
    public List<string> GetInfo();
}
