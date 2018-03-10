using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniOSC;
using OSCsharp.Data;

public class ComposeVROSCEventDispatcher : UniOSCEventDispatcher {


    public override void Awake() {
        base.Awake(); 
    }

    public override void OnEnable() {
        base.OnEnable();
    }

    public override void OnDisable() {
        base.OnDisable();
    }

    public void SendOSCPacket(string address, OscPacket msg) {
        _OSCeArg.Address = address;
        _OSCeArg.Packet = msg;
        _SendOSCMessage(_OSCeArg);
    }
}
