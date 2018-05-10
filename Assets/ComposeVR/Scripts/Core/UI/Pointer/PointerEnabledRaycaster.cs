using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

[RequireComponent(typeof(PointerDetector))]
public class PointerEnabledRaycaster : MonoBehaviour
{

    private void Awake()
    {
        GetComponent<VRTK_UIGraphicRaycaster>().enabled = false;
        GetComponent<PointerDetector>().PointerEnter += OnPointerEnter;
        GetComponent<PointerDetector>().PointerExit += OnPointerExit;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnPointerEnter(object sender, PointerDetectorEventArgs e)
    {
        GetComponent<VRTK_UIGraphicRaycaster>().enabled = true;
    }

    public void OnPointerExit(object sender, PointerDetectorEventArgs e)
    {
        GetComponent<VRTK_UIGraphicRaycaster>().enabled = false;
    }
}
