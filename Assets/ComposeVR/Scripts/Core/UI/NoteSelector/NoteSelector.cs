using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRTK;

namespace ComposeVR {
    public class NoteSelectionEventArgs : EventArgs {
        private int selectedNote;
                
        public NoteSelectionEventArgs(int selectedNote) {
            this.selectedNote = selectedNote;
        }

        public int Note {
            get { return selectedNote; }
            set { selectedNote = value; }
        }
    }

    public sealed class NoteSelector : MonoBehaviour {

        public event EventHandler<NoteSelectionEventArgs> NoteSelected;

        [Tooltip("The total number of keys in the frame will be HalfKeys * 2 + 1")]
        public int HalfKeys = 9;

        public float KeySpacing = 0.001f;
        public float TickStrength = 20f;

        private Deque<Key> keys;
        private ObjectPool keyPool;

        private Transform selectedTransform;

        private Transform clipTop;
        private Transform clipBottom;

        private Transform selectorFrame;
        private Transform leftHandFrame;
        private Transform rightHandFrame;

        private VRTK_InteractableObject selectorHandle;
        private Text noteDisplay;

        private Key selectedKey;
        private NoteSelectionEventArgs noteArgs;
        private int lastNoteSelected = 60;
        private double selectedKeyOffset;
        private Vector3 lastHandlePos;

        private double maxKeyPositionY;
        private double selectedKeyPositionY;

        private SDK_BaseController.ControllerHand targetHand;

        public bool Request(SDK_BaseController.ControllerHand hand, int initialNote) {
            if(targetHand != SDK_BaseController.ControllerHand.None) {
                return false;
            }

            targetHand = hand;

            if(targetHand == SDK_BaseController.ControllerHand.Left) {
                selectorFrame.transform.position = leftHandFrame.position;
                selectorFrame.transform.rotation = leftHandFrame.rotation;
                noteDisplay.transform.parent.localRotation = Quaternion.Euler(0, 0, 180);
                noteDisplay.transform.localRotation = Quaternion.Euler(0, 180, 0);
            }
            else {
                selectorFrame.transform.position = rightHandFrame.position;
                selectorFrame.transform.rotation = rightHandFrame.rotation;
                noteDisplay.transform.parent.localRotation = Quaternion.Euler(0, 0, 0);
                noteDisplay.transform.localRotation = Quaternion.Euler(0, 180, 0);
            }

            Init(initialNote);
            return true;
        }

        public void Release() {
            Cleanup();
            targetHand = SDK_BaseController.ControllerHand.None;
            transform.SetParent(ComposeVRManager.Instance.transform);
        }

        // Use this for initialization
        void Awake () {
            keyPool = transform.Find("KeyPool").GetComponent<ObjectPool>();
            keys = new Deque<Key>(HalfKeys * 2 + 1);

            selectedTransform = transform.Find("SelectedTransform");
            clipTop = transform.Find("ClipTop");
            clipBottom = transform.Find("ClipBottom");

            selectorHandle = transform.Find("SelectorHandle").GetComponent<VRTK_InteractableObject>();
            selectorHandle.InteractableObjectUngrabbed += OnHandleUngrabbed;

            selectorFrame = transform.Find("NoteSelectorFrame");
            noteDisplay = selectorFrame.Find("Canvas").Find("NoteDisplay").GetComponent<Text>();

            leftHandFrame = transform.Find("LeftHandFrame");
            rightHandFrame = transform.Find("RightHandFrame");

            targetHand = SDK_BaseController.ControllerHand.None;

            noteArgs = new NoteSelectionEventArgs(60);

        }

        void Init(int selectedNote) {
            if(selectedNote < 0) {
                selectedNote = lastNoteSelected;
            }

            gameObject.SetActive(true);

            selectedKey = keyPool.GetObject(selectedTransform.position, selectedTransform.rotation).GetComponent<Key>();
            selectedKey.Note = selectedNote;
            selectedKey.transform.SetParent(transform);
            keys.PushFront(selectedKey);

            InitHalfKeyboard(1);
            InitHalfKeyboard(-1);

            selectedKeyOffset = 0;

            maxKeyPositionY = NoteToKeyPosition(127);
            selectedKeyPositionY = NoteToKeyPosition(selectedNote);
        }

        private void InitHalfKeyboard(int dir) {
            for(int i = 0; i < HalfKeys; i++) {
                Vector3 keyPos = selectedKey.transform.position + (selectedKey.transform.localScale.y + KeySpacing) * (i + 1) * transform.up * dir;
                Key nextKey = keyPool.GetObject(keyPos, selectedTransform.rotation).GetComponent<Key>();
                nextKey.Note = selectedKey.Note + i * dir + dir;
                nextKey.transform.SetParent(transform);

                if(dir > 0) {
                    keys.PushFront(nextKey);
                }
                else {
                    keys.PushBack(nextKey);
                }
            }
        }

        void Cleanup() {
            for(int i = 0; i < keys.Count; i++) {
                keys.Get(i).GetComponent<Poolable>().ReturnToPool();
            }
            keys.Clear();

            selectedKey = null;
            gameObject.SetActive(false);
        }

        private void OnHandleGrabbed(object sender, InteractableObjectEventArgs e) {
        }

        private void OnHandleUngrabbed(object sender, InteractableObjectEventArgs e) {
            //Instantly move the handle back to it's original position so that it can be grabbe again
            selectorHandle.transform.position = selectedTransform.position;
            selectorHandle.transform.rotation = selectedTransform.rotation;
        }
        
        // Update is called once per frame
        void Update () {
            UpdateNoteDisplay();
            UpdateClippingPlanes();

            if (selectorHandle.IsGrabbed()) {
                //Get movement along selector up axis since last frame
                Vector3 keyMove = GetHandleMovement();

                //Apply movement to all keys
                MoveKeys(keyMove);

                //Detect if the selected key has changed
                HandleSelectionChange();
            }
            lastHandlePos = selectorHandle.transform.position;
        }

        private void UpdateClippingPlanes() {
            if(selectedKey != null) {
                SetMaterialClippingPlane(selectedKey.BlackKeyMaterial);
                SetMaterialClippingPlane(selectedKey.WhiteKeyMaterial);
                SetMaterialClippingPlane(selectedKey.OutOfRangeMaterial);
            }
        }

        private void SetMaterialClippingPlane(Material shared) {
            shared.DisableKeyword("CLIP_ONE");
            shared.EnableKeyword("CLIP_TWO");
            shared.DisableKeyword("CLIP_THREE");

            shared.SetVector("_planePos", clipTop.position);
            shared.SetVector("_planeNorm", clipTop.forward);

            shared.SetVector("_planePos2", clipBottom.position);
            shared.SetVector("_planeNorm2", clipBottom.forward);
        }

        private void UpdateNoteDisplay() {
            if(selectedKey != null) {
                int octave = selectedKey.Note / 12;
                noteDisplay.text = selectedKey.NoteName + octave.ToString();
            }
        }

        private Vector3 GetHandleMovement() {
            //Project handle movement vector onto handle's local y axis
            Vector3 handleMove = selectorHandle.transform.position - lastHandlePos;
            double yMove = transform.InverseTransformVector(handleMove).y;

            double nextKeyPosition = selectedKeyPositionY - yMove;
            if(nextKeyPosition >= maxKeyPositionY) {
                yMove = -(maxKeyPositionY - selectedKeyPositionY);
                selectedKeyPositionY = maxKeyPositionY;
            }else if(nextKeyPosition <= 0.0) {
                yMove = selectedKeyPositionY;
                selectedKeyPositionY = 0.0;
            }
            else {
                selectedKeyPositionY -= yMove;
            }
            
            Vector3 keyMove = new Vector3(0, (float)yMove, 0);
            return keyMove;
        }

        private void MoveKeys(Vector3 move) {

            for(int i = 0; i < keys.Count; i++) {
                Key key = keys.Get(i);
                key.transform.localPosition += move;
            }
        }

        private void HandleSelectionChange() {
            Key prevSelected = selectedKey;

            if(Vector3.Distance(selectedKey.transform.position, selectedTransform.position) > selectedKey.transform.localScale.y/2 + KeySpacing) {
                if (selectedKey.transform.localPosition.y > selectedTransform.localPosition.y && selectedKey.Note > 0) {
                    Key lowestKey = keys.PeekBack();

                    selectedKeyOffset -= (selectedKey.transform.localScale.y + KeySpacing);
                    keys.PopFront().GetComponent<Poolable>().ReturnToPool();

                    Key nextKey = keyPool.GetObject(GetNextKeyPosition(lowestKey, -1), lowestKey.transform.rotation).GetComponent<Key>();
                    nextKey.transform.SetParent(transform);
                    nextKey.Note = lowestKey.Note - 1;
                    keys.PushBack(nextKey);
                }
                else if (selectedKey.transform.localPosition.y < selectedTransform.localPosition.y && selectedKey.Note < 127){
                    selectedKeyOffset += (selectedKey.transform.localScale.y + KeySpacing);
                    keys.PopBack().GetComponent<Poolable>().ReturnToPool();

                    Key highestKey = keys.PeekFront();
                    Key nextKey = keyPool.GetObject(GetNextKeyPosition(highestKey, 1), highestKey.transform.rotation).GetComponent<Key>();
                    nextKey.transform.SetParent(transform);
                    nextKey.Note = highestKey.Note + 1;
                    keys.PushFront(nextKey);
                }
                selectedKey = keys.Get(HalfKeys);
            }

            if (!selectedKey.Equals(prevSelected)) {
                PlayHapticTick();
                lastNoteSelected = selectedKey.Note;

                if(NoteSelected != null) {
                    noteArgs.Note = selectedKey.Note;
                    NoteSelected(this, noteArgs);
                }
            }

        }

        private Vector3 GetNextKeyPosition(Key key, int dir) {
            return transform.TransformPoint(key.transform.localPosition + (key.transform.localScale.y + KeySpacing) * Vector3.up * dir);
        }

        private void PlayHapticTick() {
            //Use hand opposite the target hand for haptic feedback
            SDK_BaseController.ControllerHand hapticHand = targetHand == SDK_BaseController.ControllerHand.Right ? SDK_BaseController.ControllerHand.Left : SDK_BaseController.ControllerHand.Right;

            VRTK_ControllerHaptics.TriggerHapticPulse(VRTK_ControllerReference.GetControllerReference(hapticHand), TickStrength);
        }

        public int GetSelectedNote() {
            if(selectedKey != null) {
                return selectedKey.Note;
            }
            return -1;
        }

        private double NoteToKeyPosition(int note) {
            return note * (selectedKey.transform.localScale.y + KeySpacing);
        }
    }
}
