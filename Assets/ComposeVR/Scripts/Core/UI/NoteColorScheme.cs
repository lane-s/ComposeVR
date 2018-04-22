using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorScheme", menuName = "ComposeVR/NoteColorScheme", order = 1)]
public class NoteColorScheme : ScriptableObject {
    public Color[] NoteColors = new Color[12];

    public Color GetNoteColor(int note) {
        int colorIndex = note % 12;
        if(colorIndex < 0) {
            colorIndex = 0;
        }

        return NoteColors[colorIndex];
    }
}
