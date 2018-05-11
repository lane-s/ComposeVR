using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class GlobalEvent : ScriptableObject {

    private List<GlobalEventListener> listeners = new List<GlobalEventListener>();

    public void Raise()
    {
        for(int i = listeners.Count - 1; i >= 0; i--)
        {
            listeners[i].OnEventRaised();
        }
    }

    public void RegisterListener(GlobalEventListener listener)
    {
        listeners.Add(listener);
    }

    public void UnregisterListener(GlobalEventListener listener)
    {
        listeners.Remove(listener);
    }
}
