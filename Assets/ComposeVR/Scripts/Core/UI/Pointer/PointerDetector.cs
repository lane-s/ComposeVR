using System;
using UnityEngine;
using VRTK;

public class PointerDetectorEventArgs : EventArgs
{
    public VRTK_BasePointerRenderer Pointer;
}

public class PointerDetector : MonoBehaviour
{
    public event EventHandler<PointerDetectorEventArgs> PointerEnter;
    public event EventHandler<PointerDetectorEventArgs> PointerExit;


    private PointerDetectorEventArgs detectorEventArgs;

    private void Awake()
    {
        detectorEventArgs = new PointerDetectorEventArgs();
    }

    public void OnPointerEnter(VRTK_BasePointerRenderer pointer)
    {
        if (PointerEnter != null)
        {
            detectorEventArgs.Pointer = pointer;
            PointerEnter(this, detectorEventArgs);
        }
    }

    public void OnPointerExit(VRTK_BasePointerRenderer pointer)
    {
        if (PointerExit != null)
        {
            detectorEventArgs.Pointer = pointer;
            PointerExit(this, detectorEventArgs);
        }
    }
}
