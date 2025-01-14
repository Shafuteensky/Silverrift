using UnityEngine;

public static class RiggedModelTools
{
    // Перепивязка костей
    public static void RebindBones(ref SkinnedMeshRenderer riggedModelRenderer, Transform newBonesRoot)
    {
        if (riggedModelRenderer == null || newBonesRoot == null)
        {
            Debug.LogError("RebindBones: Invalid arguments.");
            return;
        }

        // Находим новые кости по имени
        Transform[] oldBones = riggedModelRenderer.bones;
        Transform[] newBones = new Transform[oldBones.Length];

        for (int i = 0; i < oldBones.Length; i++)
        {
            if (oldBones[i] != null)
            {
                string boneName = oldBones[i].name;
                newBones[i] = FindBoneRecursively(newBonesRoot, boneName);

                if (newBones[i] == null)
                    Debug.LogWarning($"Bone '{boneName}' not found in the new hierarchy!");
            }
            else
            {
                newBones[i] = null;
            }
        }

        riggedModelRenderer.bones = newBones;

        // Обновляем Root Bone, если требуется
        if (riggedModelRenderer.rootBone != null)
        {
            riggedModelRenderer.rootBone = newBonesRoot.Find(riggedModelRenderer.rootBone.name);
        }
    }

    /// <summary>
    /// Recursively searches for a bone by name in the hierarchy starting from the root.
    /// </summary>
    private static Transform FindBoneRecursively(Transform root, string boneName)
    {
        if (root.name == boneName)
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindBoneRecursively(child, boneName);
            if (found != null)
                return found;
        }

        return null; // Bone not found
    }
}
