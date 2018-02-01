using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CommandRouter))]
public class TCPClient : MonoBehaviour {

	public string HostIP;
	public int HostPort;

	private TcpClient client;

	private float maxUpdateTime = 1.0f;

	private Queue<string> commandQueue;
	private System.Object commandQueueLock = new System.Object();

	// Use this for initialization
	void Start () {
		commandQueue = new Queue<string> ();

		try{
			client = new TcpClient(AddressFamily.InterNetwork);
			IPAddress[] remoteHost = Dns.GetHostAddresses(HostIP);
			client.BeginConnect(remoteHost, HostPort, new
				AsyncCallback(ConnectCallback),client);
		}catch(Exception e){
			Debug.Log ("Can't connect");
		}

		DontDestroyOnLoad (this.gameObject);
	}

	private void ConnectCallback(IAsyncResult result)
	{                       
		try
		{
			NetworkStream networkStream = client.GetStream();
			byte[] buffer = new byte[client.ReceiveBufferSize];

			networkStream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReadCallback), buffer);
		}
		catch(Exception ex)
		{
			Debug.Log ("Connection callback failed");
		}
	}

	private void ReadCallback(IAsyncResult result){     

		NetworkStream networkStream;

		try
		{
			networkStream = client.GetStream();   
		}catch
		{
			Debug.Log ("Read exception");
			return;
		}         

		byte[] buffer = result.AsyncState as byte[];
		int bytesRead = networkStream.EndRead (result);

		if (bytesRead > 0) {
			lock (commandQueueLock) {
				commandQueue.Enqueue (Encoding.UTF8.GetString (buffer, 0, bytesRead));
			}
		}

		networkStream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReadCallback), buffer);
	}

	private void WriteCallback(IAsyncResult result){
		
		NetworkStream networkStream;

		try
		{
			networkStream = client.GetStream();   
			networkStream.EndWrite(result);
		}catch
		{
			Debug.Log ("Write exception");
			return;
		}   

	}

	// Update is called once per frame
	void Update () {
		int commandCount = 0;
		lock (commandQueueLock) {
			if (commandQueue != null) {
				commandCount = commandQueue.Count;
			}
		}

		float timeout = Time.realtimeSinceStartup + maxUpdateTime;

		while (commandCount > 0) {

			if (Time.realtimeSinceStartup > timeout) {
				break;
			}

			string command = null;
			lock (commandQueueLock) {
				command = commandQueue.Dequeue ();
			}

            if (command != null) {
                string[] commands = Regex.Split(command, @"(?<=[}])");
                foreach (string c in commands) {
                    if (c.Length > 0) {
                        GetComponent<CommandRouter>().routeCommand(c);
                    }
                }
			}

			commandCount--;
		}
	}
		
	void OnApplicationQuit(){
		try{
			client.Close ();
		}catch(Exception e){
			Debug.Log ("Can't close connection");
		}
	}

	public void send(String command){
		NetworkStream networkStream = client.GetStream ();
		byte[] commandBytes = CommandRouter.packMessage (command);
		networkStream.BeginWrite (commandBytes, 0, commandBytes.Length, new AsyncCallback (WriteCallback), commandBytes);
	}
}
