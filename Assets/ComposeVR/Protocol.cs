using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;

public class Protocol : MonoBehaviour {

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
}
