using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrumMachine : SoundModule {

	private int currentMidiNote;

	void Awake(){
		base.Awake ();
		//contextMenu.Find ("MenuButtons").Find ("DrumPad").GetComponent<NewModuleButton> ().ModulePlaced += OnDrumPadPlaced;
		currentMidiNote = 36;
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnDrumPadPlaced(object sender, ModulePlacedEventArgs args){
		args.obj.GetComponent<DrumPad> ().midiNoteNumber = currentMidiNote;
		currentMidiNote++;
	}

}
