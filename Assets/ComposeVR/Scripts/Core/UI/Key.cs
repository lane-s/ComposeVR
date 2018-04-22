using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Key : MonoBehaviour {
    public Material WhiteKeyMaterial;
    public Material BlackKeyMaterial;
    public Material OutOfRangeMaterial;

    private int note;

    public static readonly string[] NoteNames = { "C", "C#", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B" };
    public static readonly bool[] WhiteKeys = { true, false, true, false, true, true, false, true, false, true, false, true };

    public int Note {
        set {
            note = value;
            if(note < 0 || note > 127) {
                GetComponent<MeshRenderer>().material = OutOfRangeMaterial;
            }
            else {
                if(WhiteKeys[note % 12]) {
                    GetComponent<MeshRenderer>().material = WhiteKeyMaterial;
                }
                else {
                    GetComponent<MeshRenderer>().material = BlackKeyMaterial;
                }
            }

        }
        get { return note; }
    }

    public string NoteName {
        get { return NoteNames[note % 12]; }
    }
}
