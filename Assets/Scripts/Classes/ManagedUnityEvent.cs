using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Events;

[System.Serializable]
public class ManagedUnityEvent
{
    private readonly List<UnityAction> listeners = new List<UnityAction>();

    public void AddListener(UnityAction action)
    {
        if (!listeners.Contains(action))
            listeners.Add(action);
    }

    public void RemoveListener(UnityAction action)
    {
        if (listeners.Contains(action))
            listeners.Remove(action);
    }

    public void Invoke()
    {
        foreach (var action in listeners)
        {
            action?.Invoke();
        }
    }

    public IEnumerable<UnityAction> GetListeners()
    {
        return listeners.AsReadOnly();
    }

    public int GetListenerCount()
    {
        return listeners.Count;
    }

    //public static int GetListenerNumber(this UnityEventBase unityEvent)
    //{
    //    var field = typeof(UnityEventBase).GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
    //    var invokeCallList = field.GetValue(unityEvent);
    //    var property = invokeCallList.GetType().GetProperty("Count");
    //    return (int)property.GetValue(invokeCallList);
    //}
}
