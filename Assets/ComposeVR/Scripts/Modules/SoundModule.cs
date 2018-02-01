using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundModule : CommandReceiver {

    public DeviceBrowser browser;

    private int scrollPos;
    private int resultsPerPage;
    private int totalResults;

	void Awake(){
        Register();

        //Command DAW to create a new sound module
        DAWCommand.createSoundModule(getClient(), getID());

        browser = GameObject.FindGameObjectWithTag("DeviceBrowser").GetComponent<DeviceBrowser>();
    }

    /// <summary>
    /// Load a device into this module by name
    /// </summary>
    /// <param name="trackID"></param>
    private void LoadDevice(string instrumentName){
		Debug.Log ("Sending load command to track");
	}

    /// <summary>
    /// Event triggered after the DAW has successfully created a new sound module
    /// </summary>
    /// 
    public void SoundModuleCreated(string[] args) {

        Debug.Log("Track created with id " + getID());

        //Open BrowserMenu for sound module
        browser.transform.position = transform.position + Vector3.up *1.05f;

        Quaternion lookAtPlayer = Quaternion.LookRotation(browser.transform.position - GameObject.FindGameObjectWithTag("Headset").transform.position);
        browser.transform.rotation = Quaternion.Euler(browser.transform.rotation.eulerAngles.x, lookAtPlayer.eulerAngles.y, browser.transform.rotation.eulerAngles.z);


        browser.openBrowser(getID(), "Instrument");
    }

}
