using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class PointerEnabledRaycaster : MonoBehaviour {

    private void Awake() {
        GetComponent<VRTK_UIGraphicRaycaster>().enabled = false;
    }

    // Update is called once per frame
    void Update () {
		
	}

    public void PointerEnter() {
        GetComponent<VRTK_UIGraphicRaycaster>().enabled = true;
    }

    public void PointerExit() {
        GetComponent<VRTK_UIGraphicRaycaster>().enabled = false;
    }
}
