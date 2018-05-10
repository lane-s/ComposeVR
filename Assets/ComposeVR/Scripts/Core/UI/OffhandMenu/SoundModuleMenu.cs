using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ComposeVR
{
    public class SoundModuleMenu : MonoBehaviour, IDisplayable
    {

        public Text SelectionText;
        public GameObject BrowserButtonGroup;

        private SoundModuleController selectedSoundModule;
        private const string NO_SELECTION_TEXT = "No instrument selected";

        private void Awake()
        {
        }

        private void Update()
        {
            if (selectedSoundModule != null && !selectedSoundModule.IsBrowsing())
            {
                BrowserButtonGroup.SetActive(true);
            }
            else
            {
                BrowserButtonGroup.SetActive(false);
            }
        }

        public void OnModuleSelected(SelectableModule module)
        {
            if (module.GetComponent<SoundModuleObject>())
            {
                selectedSoundModule = module.GetComponent<SoundModuleObject>().Controller;
                selectedSoundModule.ModuleNameChanged += OnSelectedModuleNameChanged;
                SelectionText.text = selectedSoundModule.GetName();
            }
        }

        public void OnModuleDeselected()
        {
            if (selectedSoundModule != null)
            {
                selectedSoundModule.ModuleNameChanged -= OnSelectedModuleNameChanged;
                selectedSoundModule = null;
                SelectionText.text = NO_SELECTION_TEXT;
            }
        }

        private void OnSelectedModuleNameChanged(object sender, SoundModuleEventArgs e)
        {
            SelectionText.text = e.ModuleName;
        }

        public void OnChangeInstrumentButtonClicked()
        {
            if (selectedSoundModule != null)
            {
                selectedSoundModule.OnChangeInstrumentButtonClicked();
            }
        }

        public void OnLoadPresetButtonClicked()
        {
            StartCoroutine(PresetButtonClickedDelay());
        }

        private IEnumerator PresetButtonClickedDelay()
        {
            yield return new WaitForSeconds(0.15f);
            selectedSoundModule.OnLoadPresetButtonClicked();
        }

        public void Display()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
