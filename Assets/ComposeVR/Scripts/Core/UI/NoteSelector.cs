using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRTK;

namespace ComposeVR {
    public class NoteSelector : MonoBehaviour {

        [Tooltip("The total number of keys in the frame will be HalfKeys * 2 + 1")]
        public int HalfKeys = 9;

        public float KeySpacing = 0.001f;

        private Deque<Key> keys;
        private ObjectPool keyPool;

        private Transform selectedTransform;
        private Transform clipTop;
        private Transform clipBottom;
        private VRTK_InteractableObject selectorHandle;
        private Text noteDisplay;

        private Key selectedKey;
        private float selectedKeyOffset;
        private Vector3 lastHandlePos;

        // Use this for initialization
        void Awake () {
            keyPool = transform.Find("KeyPool").GetComponent<ObjectPool>();
            keys = new Deque<Key>(HalfKeys * 2 + 1);

            selectedTransform = transform.Find("SelectedTransform");
            clipTop = transform.Find("ClipTop");
            clipBottom = transform.Find("ClipBottom");

            selectorHandle = transform.Find("SelectorHandle").GetComponent<VRTK_InteractableObject>();
            selectorHandle.InteractableObjectUngrabbed += OnHandleUngrabbed;

            noteDisplay = transform.Find("Canvas").Find("NoteDisplay").GetComponent<Text>();

            Init(36);
        }

        void Init(int selectedNote) {
            selectedKey = keyPool.GetObject(selectedTransform.position, selectedTransform.rotation).GetComponent<Key>();
            selectedKey.Note = selectedNote;
            keys.PushFront(selectedKey);

            InitHalfKeyboard(1);
            InitHalfKeyboard(-1);

            selectedKeyOffset = 0;
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
                keys.PopBack();
            }
            selectedKey = null;
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

                //Lock movement if we are out of MIDI note range
                Vector3 localMove = transform.InverseTransformVector(keyMove);
                if(localMove.y > 0 && selectedKey.Note == 0 && selectedKey.transform.localPosition.y >= selectedTransform.localPosition.y) {
                    return;
                }else if(localMove.y < 0 && selectedKey.Note == 127 && selectedTransform.transform.localPosition.y <= selectedTransform.localPosition.y) {
                    return;
                }

                //Apply movement to all keys
                MoveKeys(keyMove);

                //Detect if the selected key has changed
                HandleSelectionChange(keyMove);
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
            Vector3 projectionOnUpAxis = Utility.ProjectVector(handleMove, transform.up);

            return projectionOnUpAxis;
        }

        private void MoveKeys(Vector3 move) {
            Vector3 limitAdjustment = Vector3.zero;

            for(int i = 0; i < keys.Count; i++) {
                Key key = keys.Get(i);
                key.transform.position += move;
                
                //Movement limits
                if(key.Note == 0 && key.transform.localPosition.y > selectedTransform.localPosition.y) {
                    limitAdjustment = new Vector3(key.transform.localPosition.x, selectedTransform.localPosition.y, key.transform.localPosition.z) - key.transform.localPosition;
                }else if(key.Note == 127 && key.transform.localPosition.y < selectedTransform.localPosition.y) {
                    limitAdjustment = new Vector3(key.transform.localPosition.x, selectedTransform.localPosition.y, key.transform.localPosition.z) - key.transform.localPosition;
                }
            }

            if(limitAdjustment != Vector3.zero) {
                MoveKeys(limitAdjustment);
            }
        }

        private void HandleSelectionChange(Vector3 move) {
            Vector3 localMove = transform.InverseTransformVector(move);

            if(Vector3.Distance(selectedKey.transform.position, selectedTransform.position) > selectedKey.transform.localScale.y/2 + KeySpacing) {
                if(localMove.y > 0) {
                    selectedKeyOffset -= (selectedKey.transform.localScale.y + KeySpacing);
                    keys.PopFront().GetComponent<Poolable>().ReturnToPool();
                        
                    Key lowestKey = keys.PeekBack();
                    Key nextKey = keyPool.GetObject(GetNextKeyPosition(lowestKey, -1), lowestKey.transform.rotation).GetComponent<Key>();
                    nextKey.transform.SetParent(transform);
                    nextKey.Note = lowestKey.Note - 1;
                    keys.PushBack(nextKey);
                }
                else {
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

        }

        private Vector3 GetNextKeyPosition(Key key, int dir) {
            return key.transform.position + (key.transform.localScale.y + KeySpacing) * transform.up * dir;
        }

    }
}
