using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GlobalEventListener : MonoBehaviour {

    public GlobalEvent Event;
    public UnityEvent Response;

    public void OnEnable()
    {
        Event.RegisterListener(this);     
    }

    public void OnDisable()
    {
        Event.UnregisterListener(this);
    }

    public void OnEventRaised()
    {
        if(Response != null)
        {
            Response.Invoke();
        }
    }
}
