using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class DisableTooltipsOnAwake : MonoBehaviour {

    private void Start() {
        StartCoroutine(HideTooltips());
    }

    private IEnumerator HideTooltips() {
        yield return new WaitForSecondsRealtime(1.0f);
        GetComponent<VRTK_ControllerTooltips>().ToggleTips(false, VRTK_ControllerTooltips.TooltipButtons.ButtonOneTooltip);
    }
}
