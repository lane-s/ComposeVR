using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CommandReceiver : MonoBehaviour {

	protected string id;
	protected TCPClient client;

	// Use this for initialization
	protected virtual void Awake() {
        client = GameObject.FindGameObjectWithTag("TCPClient").GetComponent<TCPClient>();
        client.GetComponent<CommandRouter>().addReceiver(getID(), this);
    }

	protected void Start(){
	}

	public void executeCommand(DAWMessage command){
		Type thisType = this.GetType ();
		MethodInfo theMethod = thisType.GetMethod (command.methodName);
        if (theMethod != null) {
            theMethod.Invoke(this, command.methodParams);
        }
	}

	public string getID(){
        if(id == null) {
            id = Guid.NewGuid().ToString();
        }
		return id;
	}
}
