using System.Collections;
using UnityEngine;

public static class GameObjectTools
{

    // ============= ������ ==============

    // ��������� ������ ������ ��������� � ���������, ��� ���� �� �� ��� ��������� (GameObject.Destroy)
    public static void HideObject(GameObject gameObjectToHide)
    {
        MeshRenderer meshRenderer = gameObjectToHide.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = false;

        Collider collider = gameObjectToHide.GetComponent<BoxCollider>();
        if (collider == null)
            collider = gameObjectToHide.GetComponent<MeshCollider>();
        if (collider != null)
            collider.enabled = false;
    }

    // �������� ������� ������ ��������� � ����������
    public static IEnumerator FadeOutAndDestroy(GameObject gameObjectToHide)
    {
        yield return null;
        // ��� ���������� ������� ������������
        GameObject.Destroy(gameObjectToHide);
    }
    // ============= �������� ==============

    // ����� � ������� ������� ������� � �����
    public static GameObject FindChildWithTag(Transform parent, string tag)
    {
        GameObject child = null;

        foreach (Transform transform in parent)
        {
            if (transform.CompareTag(tag))
            {
                child = transform.gameObject;
                break;
            }
        }

        return child;
    }

    // ������� ���� ����� ���� �� �������� (�� ������� ��������)
    public static void MoveChildrenUp(GameObject parent)
    {
        Transform parentTransform = parent.transform;

        if (parentTransform.parent == null)
        {
            Debug.LogWarning($"{parent.name} has no parent to move children under.");
            return;
        }

        Transform grandParent = parentTransform.parent; // �������� �������� �������� �������

        int childOrderIndex = 0;
        while (parentTransform.childCount > 0)
        {
            Transform child = parentTransform.GetChild(0); // ���� ������� ������
            child.SetParent(grandParent); // ��������� ��� �������� �������� �������
            child.SetSiblingIndex(childOrderIndex);
            childOrderIndex += 1;
        }
    }

    // ���������� ���� �����
    public static void DestroyAllChildren(Transform gameObject)
    {
        foreach (Transform child in gameObject)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    // ============= ������� ==============

    // ���������� ������ 
    public static void AlignObjectToSurface(GameObject objectToAlign, Vector3 newPosition, Quaternion newRotation,
        bool correctY = true, int maxRayLength = 100)
    {
        Vector3 spawnPosition;

        if (!correctY)
            spawnPosition = newPosition;
        else
        {
            RaycastHit hit;
            Physics.Raycast(newPosition, Vector3.down, out hit, maxRayLength);
            spawnPosition = hit.point;
        }

        objectToAlign.transform.SetPositionAndRotation(spawnPosition, newRotation);
    }

    // �������� ������� � ���������� �� Y 
    public static Vector3 GetAlignedPosition(GameObject objectToAlign, float radius = 0.25f, int maxRayLength = 100)
    {
        Vector3 correctedByYPosition;

        RaycastHit hit;
        Physics.SphereCast(objectToAlign.transform.position, radius, Vector3.down, out hit, maxRayLength);
        correctedByYPosition = hit.point;

        return correctedByYPosition;
    }

    // ============= ������ ==============

    // ��������� � ��������� ������
    public static IEnumerator ScaleUpAndDown(Transform transform, Vector3 upScale, float duration)
    {
        Vector3 initialScale = transform.localScale;

        for (float time = 0; time < duration * 2; time += Time.deltaTime)
        {
            float progress = Mathf.PingPong(time, duration) / duration;
            transform.localScale = Vector3.Lerp(initialScale, upScale, progress);
            yield return null;
        }
        transform.localScale = initialScale;
    }

    // ��������� � ��������� ������
    public static IEnumerator FadeIn(Transform transform, Vector3 upScale)
    {
        float progress = 0;
        Vector3 initialScale = transform.localScale;

        while (progress <= 1)
        {
            transform.localScale = Vector3.Lerp(initialScale, upScale, progress);
            progress += Time.deltaTime;
            yield return null;
        }
    }
}

// ****************************** ObjectShaker **********************************

public static class ObjectShaker
{
    private static bool stopShake = false;

    /// <summary>
    /// ��������� ������ �������.
    /// </summary>
    /// <param name="monoBehaviour">������, �� �������� ����� �������� ��������.</param>
    /// <param name="targetTransform">��������� �������, ������� ����� ������.</param>
    /// <param name="duration">������������ ������.</param>
    /// <param name="intensity">���� ������.</param>
    /// <param name="frequency">������� ������.</param>
    public static void StartShake(MonoBehaviour monoBehaviour, Transform targetTransform, float duration, float intensity, float frequency = 1f)
    {
        stopShake = false;
        monoBehaviour.StartCoroutine(ShakeCoroutine(targetTransform, duration, intensity, frequency));
    }

    /// <summary>
    /// ������������� ������� ������.
    /// </summary>
    public static void StopShake()
    {
        stopShake = true;
    }

    private static System.Collections.IEnumerator ShakeCoroutine(Transform targetTransform, float duration, float intensity, float frequency)
    {
        Vector3 originalPosition = targetTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (stopShake)
            {
                targetTransform.localPosition = originalPosition;
                yield break;
            }

            // ������������ ������� ���������� �������������
            float smoothIntensity = Mathf.Lerp(intensity, 0, elapsed / duration);

            // ������������ �������� � ������ �������
            float offsetX = Mathf.PerlinNoise(elapsed * frequency, 0f) * 2f - 1f; // �� -1 �� 1
            float offsetY = Mathf.PerlinNoise(0f, elapsed * frequency) * 2f - 1f; // �� -1 �� 1
            float offsetZ = Mathf.PerlinNoise(elapsed * frequency, elapsed * frequency) * 2f - 1f; // �� -1 �� 1

            targetTransform.localPosition = originalPosition + new Vector3(offsetX, offsetY, offsetZ) * smoothIntensity;

            elapsed += Time.deltaTime;
            yield return null;
        }

        targetTransform.localPosition = originalPosition;
    }
}

