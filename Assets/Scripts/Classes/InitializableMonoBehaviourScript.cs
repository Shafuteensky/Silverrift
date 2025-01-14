using Unity.VisualScripting;
using UnityEngine;

public abstract class InitializableMonoBehaviour : MonoBehaviour
{
    public bool isInitialized { get; private set; } = false; 

    protected bool IsInitialized(bool mute = true)
    {
        if (!isInitialized)
        {
            if (!mute)
                Debug.LogWarning($"{GetType().Name}: Attempted to call a method before initialization.");
            return false;
        }
        return true;
    }

    protected void Initialize()
    {
        isInitialized = true;
    }

    protected bool AreDependenciesInitialized(params MonoBehaviour[] dependencies)
    {
        bool dependenciesInitialized = true;

        foreach (MonoBehaviour dependency in dependencies)
            if (dependency == null)
            {
                Debug.LogWarning($"{GetType().Name}: {dependency.GetType().Name} not initialized.");
                dependenciesInitialized = false;
            }

        return dependenciesInitialized;
    }

    protected bool AreDependenciesInitialized(params GameObject[] dependencies)
    {
        bool dependenciesInitialized = true;

        foreach (GameObject dependency in dependencies)
            if (dependency == null)
            {
                Debug.LogWarning($"{GetType().Name}: {dependency.GetType().Name} not initialized.");
                dependenciesInitialized = false;
            }

        return dependenciesInitialized;
    }
}