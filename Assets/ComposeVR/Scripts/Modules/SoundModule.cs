using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundModule : CommandReceiver {

    private string trackID;

    public string instrumentName;
	public string defaultPreset;


	protected override void Awake(){
        base.Awake();

        //Send new track command to Bitwig with instrument area's unique id
        DAWCommand.createSoundModule(client, this.getID());
    }

    public void LoadDevice(string trackID){
		Debug.Log ("Sending load command to track");
        DAWCommand.requestBrowser(client, trackID, -1, 0, false, instrumentName);
	}


    public void TrackCreated(string trackID) {
        this.trackID = trackID;
        Debug.Log("Track created with id " + trackID);

        LoadDevice(trackID);
    }

}
