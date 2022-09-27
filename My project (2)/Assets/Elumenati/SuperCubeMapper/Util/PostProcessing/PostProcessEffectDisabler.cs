using UnityEngine;

namespace Elumenati {
    public static class PostProcessEffectDisabler {
        public static void GlobalDisableVignette() {
#if RENDERING_VOLUME
            // if this is causing problems... comment out the line  #define Rendering_Volume at the top
            foreach(var volume in GameObject.FindObjectsOfType<UnityEngine.Rendering.Volume>()) {
                for(int i = 0; i < volume.profile.components.Count; i++) {
                    if(volume.profile.components[i].name.ToLower().Contains("vignette")) {
                        volume.profile.components[i].active = false;
                    }
                }
            }
#endif
        }
    }
}
