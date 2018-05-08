using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR {

    public static class AssetLoader {
        private static AssetBundle coreAssetBundle;
        private static Dictionary<string, Object> coreAssets;

        public static AssetBundle LoadCoreAssetBundle() {
            if(coreAssetBundle != null) {
                return coreAssetBundle;
            }

            coreAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "core"));
            if(coreAssetBundle == null) {
                Debug.LogError("Can't load core assets");
            }


            return coreAssetBundle;
        }

    }
}
