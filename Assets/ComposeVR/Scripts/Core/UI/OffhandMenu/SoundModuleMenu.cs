using System;
using System.Collections;
using UnityEngine;

public class SoundModuleMenu : MonoBehaviour {

    public event EventHandler<EventArgs> ChangeInstrumentButtonClicked;
    public event EventHandler<EventArgs> LoadPresetButtonClicked;
    public event EventHandler<EventArgs> MenuClosed;

    private void Awake() {
        Display(true);
    }

    public void OnChangeInstrumentButtonClicked() {
        if (ChangeInstrumentButtonClicked != null) {
            ChangeInstrumentButtonClicked(this, new EventArgs());
        }
    }

    public void OnLoadPresetButtonClicked() {
        StartCoroutine(PresetButtonClickedDelay());
    }

    private IEnumerator PresetButtonClickedDelay() {
        yield return new WaitForSeconds(0.15f);
        if (LoadPresetButtonClicked != null) {
            LoadPresetButtonClicked(this, new EventArgs());
        }
    }

    public void OnMenuClosed() {
        if (MenuClosed != null) {
            MenuClosed(this, new EventArgs());
        }
    }

    public void Display(bool display) {
        OnMenuClosed();
    }
}