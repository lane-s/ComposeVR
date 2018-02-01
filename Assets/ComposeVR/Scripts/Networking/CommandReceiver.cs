using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CommandReceiver : MonoBehaviour {

	protected string id;
	private TCPClient client;
    private CommandRouter router;

	public void executeCommand(DAWMessage command){

		Type thisType = this.GetType ();
		MethodInfo theMethod = thisType.GetMethod (command.methodName);

        if (theMethod != null) {
            theMethod.Invoke(this, new object[]{command.methodParams});
        }
	}

    public string getID() {
        if (id == null) {
            id = Guid.NewGuid().ToString();
        }
        return id;
    }

    protected void Register() {
        getRouter().addReceiver(getID(), this);
    }

    protected void Register(string newId) {
        setID(newId);
        Register();
    }

    protected TCPClient getClient() {
        if(client == null) {
            client = GameObject.FindGameObjectWithTag("TCPClient").GetComponent<TCPClient>();
        }
        return client;
    }

    private CommandRouter getRouter() {
        if(router == null) {
            router = getClient().GetComponent<CommandRouter>();
        }
        return router;
    }

    private void setID(string newId) {
        if(this.id != null) {
            getRouter().removeReceiverIfPresent(this.id);
        }

        this.id = newId;
    }


}
