using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace ComposeVR {
    public static class NoteOrbFactory {
        private static GameObject _orbPrefab;
        private static GameObject OrbPrefab {
            get {
               if(_orbPrefab == null) {
                    _orbPrefab = AssetLoader.GetCoreAssetBundle().LoadAsset<GameObject>("NoteOrb");
                }

                return _orbPrefab;
            }
        }

        private static GameObject _corePrefab;
        private static GameObject CorePrefab {
            get {
               if(_corePrefab == null) {
                    _corePrefab = AssetLoader.GetCoreAssetBundle().LoadAsset<GameObject>("NoteCore");
                }

                return _corePrefab;
            }
        }

        public static NoteOrb DefaultNoteOrb(Vector3 position, Quaternion rotation) {
            return Object.Instantiate(OrbPrefab, position, rotation).GetComponent<NoteOrb>();
        }

        public static NoteOrb DuplicateNoteOrb(NoteOrb sourceOrb) {
            //Create new orb with identical position, rotation, and scale
            NoteOrb copy = DefaultNoteOrb(sourceOrb.transform.position, sourceOrb.transform.rotation);
            copy.transform.localScale = sourceOrb.transform.localScale;

            Object.Destroy(copy.transform.Find("Cores").Find("InitialCore").gameObject);
            copy.NoteCores.Clear();
            copy.SelectedNotes.Clear();

            //Copy note cores
            List<NoteCore> sourceCores = sourceOrb.NoteCores;
            for(int i = 0; i < sourceCores.Count; i++) {
                NoteCore coreCopy = DuplicateNoteCore(sourceCores[i]);

                coreCopy.transform.SetParent(copy.transform);
                coreCopy.transform.localScale = sourceCores[i].transform.localScale;
                coreCopy.transform.localPosition = sourceCores[i].transform.localPosition;
                coreCopy.GetComponent<Scalable>().TargetScale = coreCopy.transform.localScale;

                copy.NoteCores.Add(coreCopy);
            }

            copy.SelectedNotes = new List<int>(sourceOrb.SelectedNotes);
            copy.GetComponent<VRTK_InteractableObject>().isGrabbable = true;
            return copy;
        }

        private static NoteCore DuplicateNoteCore(NoteCore sourceCore) {
            NoteCore copy = Object.Instantiate(CorePrefab, sourceCore.transform.position, sourceCore.transform.rotation).GetComponent<NoteCore>();
            copy.Color = sourceCore.Color;
            copy.Note = sourceCore.Note;
            return copy;
        }
    }
}
