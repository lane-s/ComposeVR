using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TCPClient : MonoBehaviour {

	public string HostIP;
	public int HostPort;

	private TcpClient client;

	private float maxUpdateTime = 1.0f;

	private Queue<string> messageQueue;
	private System.Object messageQueueLock = new System.Object();

	// Use this for initialization
	void Start () {
		messageQueue = new Queue<string> ();

		try{
			client = new TcpClient(AddressFamily.InterNetwork);
			IPAddress[] remoteHost = Dns.GetHostAddresses(HostIP);
			client.BeginConnect(remoteHost, HostPort, new
				AsyncCallback(ConnectCallback),client);
		}catch(Exception e){
			Debug.Log ("Can't connect");
		}
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
			lock (messageQueueLock) {
				messageQueue.Enqueue (Encoding.UTF8.GetString (buffer, 0, bytesRead));
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
		int messageCount = 0;
		lock (messageQueueLock) {
			if (messageQueue != null) {
				messageCount = messageQueue.Count;
			}
		}

		float timeout = Time.realtimeSinceStartup + maxUpdateTime;

		while (messageCount > 0) {

			if (Time.realtimeSinceStartup > timeout) {
				break;
			}

			string data = null;
			lock (messageQueueLock) {
				data = messageQueue.Dequeue ();
			}

			if (data != null) {
				Debug.Log (data);

				if (data.Contains ("Hello Unity")) {
					NetworkStream networkStream = client.GetStream ();
					byte[] msg = Protocol.packMessage ("track/new/instrument");
					networkStream.BeginWrite (msg, 0, msg.Length, new AsyncCallback (WriteCallback), msg);
					StartCoroutine (maintainConnection());
				}
			}
			messageCount--;
		}
	}
		
	void OnApplicationQuit(){
		try{
			client.Close ();
		}catch(Exception e){
			Debug.Log ("Can't close connection");
		}
	}

	private IEnumerator maintainConnection(){
		while (true) {
			yield return new WaitForSeconds (10);
			NetworkStream networkStream = client.GetStream ();
			byte[] msg = Protocol.packMessage ("Hello Bitwig");
			networkStream.BeginWrite (msg, 0, msg.Length, new AsyncCallback (WriteCallback), msg);
		}
			
	}
}
