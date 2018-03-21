using System;
using UnityEngine;

public class SoundModuleMenu : MonoBehaviour {

    public event EventHandler<EventArgs> ChangeInstrumentButtonClicked;
    public event EventHandler<EventArgs> LoadPresetButtonClicked;
    public event EventHandler<EventArgs> MenuClosed;

    private void Awake() {
        Display(false); 
    }

    public void OnChangeInstrumentButtonClicked() {
        if(ChangeInstrumentButtonClicked != null) {
            ChangeInstrumentButtonClicked(this, new EventArgs());
        }
    }

    public void OnLoadPresetButtonClicked() {
        if(LoadPresetButtonClicked != null) {
            LoadPresetButtonClicked(this, new EventArgs());
        }
    }

    public void OnCloseButtonClicked() {
        Display(false);
    }

    public void OnMenuClosed() {
        if(MenuClosed != null) {
            MenuClosed(this, new EventArgs());
        }
    }

    public void Display(bool display) {
        OnMenuClosed();

        transform.GetChild(0).gameObject.SetActive(display);
    }
}
