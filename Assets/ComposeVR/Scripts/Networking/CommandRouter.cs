using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine;

public class DAWMessage {
	public string receiverID;
	public string methodName;
	public string[] methodParams;
}

public class CommandRouter : MonoBehaviour {

	private Dictionary<string, CommandReceiver> receiverDictionary;
    private string incompleteCommand;

	// Use this for initialization
	void Awake () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private Dictionary<string, CommandReceiver> getReceiverDictionary() {
        if(receiverDictionary == null) {
            receiverDictionary = new Dictionary<string, CommandReceiver>();
        }
        return receiverDictionary;
    }

	public void routeCommand(string commandString){

        int openBrackets = 0;
        int closedBrackets = 0;

        for(int i = 0; i < commandString.Length; i++) {
            if(commandString[i] == '{') {
                openBrackets += 1;
            }

            if (commandString[i] == '}') {
                closedBrackets += 1;
            }
        }

        if(openBrackets > closedBrackets) {
            incompleteCommand = commandString;
            return;
        }
        else if (openBrackets == 0 && closedBrackets == 0) {
            incompleteCommand += commandString;
            return;
        }
        else if(openBrackets < closedBrackets) {
            commandString = incompleteCommand + commandString;
            incompleteCommand = "";
        }

        Debug.Log(commandString);

		DAWMessage command = JsonConvert.DeserializeObject<DAWMessage>(commandString);

		if (getReceiverDictionary().ContainsKey (command.receiverID)) {
			CommandReceiver receiver = getReceiverDictionary()[command.receiverID];
			receiver.executeCommand (command);
		}
	}

	public void addReceiver(string receiverID, CommandReceiver receiver){
        getReceiverDictionary().Add (receiverID, receiver);
	}

    public void removeReceiverIfPresent(string receiverID) {
        getReceiverDictionary().Remove(receiverID);
    }

	public static byte[] packMessage(string message){
		System.Int32 messageLength = message.Length;

		byte[] header = BitConverter.GetBytes (messageLength);
		if (BitConverter.IsLittleEndian)
			Array.Reverse (header, 0, header.Length);

		byte[] messageBytes = Encoding.UTF8.GetBytes(message);

		byte[] buffer = new byte[header.Length + messageBytes.Length];
		System.Buffer.BlockCopy (header, 0, buffer, 0, header.Length);
		System.Buffer.BlockCopy (messageBytes, 0, buffer, header.Length, messageBytes.Length);

		return buffer;
	}

	public static string createCommand(string receiverID, string methodName, params string[] methodParams){
		DAWMessage c = new DAWMessage ();
		c.receiverID = receiverID;
		c.methodName = methodName;
		c.methodParams = methodParams;

		return JsonConvert.SerializeObject (c);
	}
}
