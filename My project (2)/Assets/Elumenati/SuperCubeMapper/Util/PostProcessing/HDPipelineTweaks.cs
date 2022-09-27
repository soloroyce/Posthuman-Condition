

namespace Elumenati {
    static public class HDPipelineTweaks {
        static public void CopyCameraFromTo(UnityEngine.Camera exampleCamera,  UnityEngine.Camera m_camera ) {
#if EXPERIMENTAL_HDPIPELINE
            var hdadfrom = exampleCamera.gameObject.GetComponent<UnityEngine.Experimental.Rendering.HDPipeline.HDAdditionalCameraData>();
            if(hdadfrom != null) {
                var hdadto = m_camera.gameObject.GetComponent<UnityEngine.Experimental.Rendering.HDPipeline.HDAdditionalCameraData>();
                if(hdadto == null) {
                    hdadto = m_camera.gameObject.AddComponent<UnityEngine.Experimental.Rendering.HDPipeline.HDAdditionalCameraData>();
                }
                hdadto.clearColorMode = hdadfrom.clearColorMode;
                hdadto.backgroundColorHDR = hdadfrom.backgroundColorHDR;
                hdadto.volumeLayerMask = hdadfrom.volumeLayerMask;
            }
#endif
        }
    }
}
