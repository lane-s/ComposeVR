using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UDPClient : MonoBehaviour {

	public string hostIP;
	public int sendPort;
	public int receivePort;

	private Socket sender;
	private IPEndPoint send_end_point;

	private UdpClient listener;

	// Use this for initialization
	void Start () {
		sender = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		IPAddress send_address = IPAddress.Parse (hostIP);
		send_end_point = new IPEndPoint (send_address, sendPort);

		messageQueue = new Queue<byte[]> ();

		listener = new UdpClient (receivePort);
		listener.BeginReceive(new AsyncCallback(DataReceived), null);
			
	}

	private void DataReceived(IAsyncResult ar){
		IPEndPoint ip = new IPEndPoint (IPAddress.Any, receivePort);
		byte[] data;
		try{
			data = listener.EndReceive(ar, ref ip);
			listener.BeginReceive(new AsyncCallback(DataReceived),null);
			messageQueue.Enqueue(data);

		}catch(Exception receiveException){
			Debug.Log ("Dgram receive error: "+receiveException.Message);
		}
	}

	private Queue<byte[]> messageQueue;

	public void send(string message){
		byte[] encoded_message = Encoding.UTF8.GetBytes (message);
		try{
			sender.SendTo(encoded_message, send_end_point);
		}
		catch(Exception sendException){
			Debug.Log ("Dgram send error: "+sendException.Message);
		}
	}

	public void sendBytes(byte[] bytes){
		try{
			sender.SendTo(bytes, send_end_point);
		}
		catch(Exception sendException){
			Debug.Log ("Dgram send error: "+sendException.Message);
		}
	}

	// Update is called once per frame
	void Update () {
		while (messageQueue.Count > 0) {
			byte[] data = messageQueue.Dequeue ();
			string message = Encoding.UTF8.GetString (data);
			Debug.Log ("Data received");
			Debug.Log (message);
		}
	}
}
