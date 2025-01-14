using System.Drawing;
using UnityEngine;

public static class CharacterColliderTools
{
    private static Bounds GetBounds(GameObject gameObject)
    {
        Bounds bounds = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh.bounds;
        return bounds;
    }

    public static float GetObjectHeight(GameObject gameObject)
    {
        return GetBounds(gameObject).size.y;
    }

    public static float GetAdjustedBodyColliderHeight(BoxCollider feetCollider, GameObject modelObject)
    {
        float objectHeight = GetObjectHeight(modelObject);
        float feetColliderHeight = feetCollider.size.y;

        float newBodyColliderHeight = objectHeight - feetColliderHeight;
        return newBodyColliderHeight;
    }

    public static Vector3 GetAdjustedBodyColliderCenter(BoxCollider feetCollider, GameObject modelObject)
    {
        float objectHeight = GetObjectHeight(modelObject);
        float feetColliderHeight = feetCollider.size.y;

        float newBodyColliderCenter = (objectHeight + feetColliderHeight)/2;
        return new Vector3(0, newBodyColliderCenter, 0);
    }

    public static Vector3 GetAdjustedFeetColliderCenter(BoxCollider feetCollider, GameObject modelObject)
    {
        float feetColliderHeight = feetCollider.size.y;

        float newFeetColliderCenter = feetColliderHeight / 2;
        return new Vector3(0, newFeetColliderCenter, 0);
    }
}
