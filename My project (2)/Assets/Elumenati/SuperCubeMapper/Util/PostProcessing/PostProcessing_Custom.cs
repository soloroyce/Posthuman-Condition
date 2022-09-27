using Elumenati;
using UnityEngine;

// this script is a here for future use with custom post processing scripts.
// we developed a script for PostProcessing Stack V1 and unity Post processing stack V2. 
// this is for use with custom post processing systems...
// either ApplyPostProcessingHinted or ApplyPostProcessingAutomatic is called once for each camera depending on the settings.
[System.Serializable]
public class PostProcessing_Custom : PostProcessing_Helper {
    [Header("Use this script if you want to use your own custom post processing.")]
    public bool disableWarning = true; // set this line to be true if you are going to edit this script.

    public static bool IS_ENABLED {
        get {
            return false;
            // set this line to be true if you are going us this custom post processing script
            // set this line to be false if this is just a dummy script (default).
        }
    }

    // this is used to copy settings from an example camera to each cube face camera.
    protected override void ApplyPostProcessingAutomatic(CubeFaceCamera cam, SuperCubeMapper cubeCapture) {
        if (!disableWarning) {
            Camera my_camera = cam.m_camera;
            Camera exampleCamera = cubeCapture.exampleCamera;
            // apply any effects to my_camera
            Debug.LogWarning("PostProcessing_Custom.cs: apply any effect scripts to my_camera using cubeCapture.exampleCamera as an example");
        }
    }

    // this is used to set settings from to each cube face camera independent of the example camera.
    protected override void ApplyPostProcessingHinted(CubeFaceCamera cam, SuperCubeMapper cubeCapture) {
        if (!disableWarning) {
            Camera my_camera = cam.m_camera;
            // apply any effects to my_camera
            Debug.LogWarning("PostProcessing_Custom.cs: apply any effect scripts to my_camera.");
        }
    }

    public void Start() {
        if (!disableWarning) {
            Debug.LogWarning("Customize PostProcessing_Custom.cs if you want to add custom post processing effects for each cube map face camera.");
        }
    }
}
