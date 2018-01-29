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

	// Use this for initialization
	void Awake () {
		receiverDictionary = new Dictionary<string, CommandReceiver> ();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void routeCommand(string commandString){
		DAWMessage command = JsonConvert.DeserializeObject<DAWMessage>(commandString);

		if (receiverDictionary.ContainsKey (command.receiverID)) {
			CommandReceiver receiver = receiverDictionary [command.receiverID];
			receiver.executeCommand (command);
		}
	}

	public void addReceiver(string receiverID, CommandReceiver receiver){
		receiverDictionary.Add (receiverID, receiver);
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
