using Elumenati;
using UnityEngine;

[System.Serializable]
public class PostProcessing_Stack1 : PostProcessing_Helper {
#if UNITY_POST_PROCESSING_STACK_V1
    [Header ("Only needed if using HINTED mode, automatic uses the post processing settings from the example camera.")]
    // if you are getting a compiler error for:
    // error CS0234: The type or namespace name 'PostProcessing' does not exist in the namespace 'UnityEngine.Rendering' (are you missing an assembly reference?)
    // this is because you have UNITY_POST_PROCESSING_STACK_V1 defined in the preprocessor macros but the code is missing from assets folder.
    // either remove the "UNITY_POST_PROCESSING_STACK_V1" macro from PROJECT SETTINGS->PLAYER->SCRIPTING DEFINE SYMBOLS.... 
    // or add the post processing stack v1 to the project folder.  (v1 is the old effects stack. mostly used with asset store scenes with build in effects.).
    public UnityEngine.PostProcessing.PostProcessingProfile firstPass;
    override protected void ApplyPostProcessingAutomatic (CubeFaceCamera cam, SuperCubeMapper cubeCapture) {
        if (cubeCapture.exampleCamera) {
            var examplePostProcessing = cubeCapture.exampleCamera.GetComponent<UnityEngine.PostProcessing.PostProcessingBehaviour> ();
            if (examplePostProcessing && examplePostProcessing.profile && examplePostProcessing.enabled) {
                var faceCameraPostProcessing = cam.m_camera.gameObject.AddComponent<UnityEngine.PostProcessing.PostProcessingBehaviour> ();
                faceCameraPostProcessing.profile = examplePostProcessing.profile;
            } else {
                Debug.Log ("UnityEngine.PostProcessing.PostProcessingBehaviour profile not found on example camera.  The developer can add the profile to the example camera or switch to Hinted.");
            }
        }
    }
    override protected void ApplyPostProcessingHinted (CubeFaceCamera cam, SuperCubeMapper cubeCapture) {
        if (firstPass) {
            var faceCameraPostProcessing = cam.m_camera.gameObject.AddComponent<UnityEngine.PostProcessing.PostProcessingBehaviour> ();
            faceCameraPostProcessing.profile = firstPass;
        } else {
            Debug.Log ("firstPass profile not found");
        }
    }
#else
    // these lines are displayed backwards
    [Header("is set under project settings - > player - > scripting define symbols ")]
    [Header("and ensure the preprocessor define UNITY_POST_PROCESSING_STACK_V1")]
    [Header("Add the code to the project directly")]
    [Header("To enable Post Processing V1 Stack")]
    public bool disableWarning = true;

    public void Start() {
        if (!disableWarning) {
            Debug.LogError("PostProcessing_Stack1 needs to be enabled through the editor");
            Debug.LogError("To enable Post Processing V1 Stack: Add the code to the project directly and ensure the preprocessor define UNITY_POST_PROCESSING_STACK_V1 is set under project settings - > player - > scripting define symbols ");
        }
    }

    protected override void ApplyPostProcessingHinted(CubeFaceCamera cam, SuperCubeMapper cubeCapture) {
    }

    protected override void ApplyPostProcessingAutomatic(CubeFaceCamera cam, SuperCubeMapper cubeCapture) {
    }
#endif
}
