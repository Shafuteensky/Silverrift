using System.Collections;
using UnityEngine;

public static class GameObjectTools
{

    // ============= Объект ==============

    // Полностью скрыть объект визуально и физически, как если бы он был уничтожен (GameObject.Destroy)
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

    // Медленно сделать объект невидимым и уничтожить
    public static IEnumerator FadeOutAndDestroy(GameObject gameObjectToHide)
    {
        yield return null;
        // ТУТ ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ИСЧЕЗНОВЕНИЯ
        GameObject.Destroy(gameObjectToHide);
    }
    // ============= Иерархия ==============

    // Найти и вернуть первого ребенка с тэгом
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

    // Поднять всех детей выше по иерархии (на уровень родителя)
    public static void MoveChildrenUp(GameObject parent)
    {
        Transform parentTransform = parent.transform;

        if (parentTransform.parent == null)
        {
            Debug.LogWarning($"{parent.name} has no parent to move children under.");
            return;
        }

        Transform grandParent = parentTransform.parent; // Получаем родителя текущего объекта

        int childOrderIndex = 0;
        while (parentTransform.childCount > 0)
        {
            Transform child = parentTransform.GetChild(0); // Берём первого ребёнка
            child.SetParent(grandParent); // Переносим под родителя текущего объекта
            child.SetSiblingIndex(childOrderIndex);
            childOrderIndex += 1;
        }
    }

    // Уничтожить всех детей
    public static void DestroyAllChildren(Transform gameObject)
    {
        foreach (Transform child in gameObject)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    // ============= Позиция ==============

    // Приземлить модель 
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

    // Получить позицию с коррекцией по Y 
    public static Vector3 GetAlignedPosition(GameObject objectToAlign, float radius = 0.25f, int maxRayLength = 100)
    {
        Vector3 correctedByYPosition;

        RaycastHit hit;
        Physics.SphereCast(objectToAlign.transform.position, radius, Vector3.down, out hit, maxRayLength);
        correctedByYPosition = hit.point;

        return correctedByYPosition;
    }

    // ============= Размер ==============

    // Увеличить и уменьшить размер
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

    // Увеличить и уменьшить размер
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
    /// Запускает тряску объекта.
    /// </summary>
    /// <param name="monoBehaviour">Объект, от которого будет запущена корутина.</param>
    /// <param name="targetTransform">Трансформ объекта, который нужно трясти.</param>
    /// <param name="duration">Длительность тряски.</param>
    /// <param name="intensity">Сила тряски.</param>
    /// <param name="frequency">Частота тряски.</param>
    public static void StartShake(MonoBehaviour monoBehaviour, Transform targetTransform, float duration, float intensity, float frequency = 1f)
    {
        stopShake = false;
        monoBehaviour.StartCoroutine(ShakeCoroutine(targetTransform, duration, intensity, frequency));
    }

    /// <summary>
    /// Останавливает текущую тряску.
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

            // Рассчитываем плавное уменьшение интенсивности
            float smoothIntensity = Mathf.Lerp(intensity, 0, elapsed / duration);

            // Рассчитываем смещения с учётом частоты
            float offsetX = Mathf.PerlinNoise(elapsed * frequency, 0f) * 2f - 1f; // от -1 до 1
            float offsetY = Mathf.PerlinNoise(0f, elapsed * frequency) * 2f - 1f; // от -1 до 1
            float offsetZ = Mathf.PerlinNoise(elapsed * frequency, elapsed * frequency) * 2f - 1f; // от -1 до 1

            targetTransform.localPosition = originalPosition + new Vector3(offsetX, offsetY, offsetZ) * smoothIntensity;

            elapsed += Time.deltaTime;
            yield return null;
        }

        targetTransform.localPosition = originalPosition;
    }
}

