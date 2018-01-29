using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrumPad : MonoBehaviour {

	private byte noteByte;

	public int midiNoteNumber{
		get{return (int)noteByte;}
		set{noteByte = (byte)value;}
	}

	private UDPClient client;
	private Color ogColor;

	private bool onCooldown = false;
	private const float cooldownTime = 0.3f;

	// Use this for initialization
	void Start () {
		client = GameObject.FindGameObjectWithTag ("UDPClient").GetComponent<UDPClient> ();
		ogColor = GetComponentInChildren<MeshRenderer> ().material.color;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnTriggerEnter(Collider other){
		MalletHead head = other.GetComponent<MalletHead> ();
		if (head) {
			int noteVelocity = head.GetMalletVelocity ();
			if (!head.enteringFromBack && !head.IsOnCooldown() && noteVelocity > 0) {
				byte velocityByte = (byte)noteVelocity;
				byte[] midiMessage = { 0x90, noteByte, velocityByte };
				client.sendBytes (midiMessage);
				head.struckPad = true;
				GetComponentInChildren<MeshRenderer> ().material.color = new Color (0, 1, 0);
			}
		}
	}

	void OnTriggerExit(Collider other){
		MalletHead head = other.GetComponent<MalletHead> ();
		if (head) {
			if (head.struckPad) {
				int noteVelocity = 110;
				byte velocityByte = (byte)noteVelocity;
				byte[] midiMessage = { 0x80, noteByte, velocityByte };
				client.sendBytes (midiMessage);
				head.struckPad = false;
				head.StartCooldown ();
			}
			GetComponentInChildren<MeshRenderer> ().material.color = ogColor;
		}
	}
		
}
