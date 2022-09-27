using Elumenati;
using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

[System.Serializable]
public class PostProcessing_Stack2 : PostProcessing_Helper {
#if UNITY_POST_PROCESSING_STACK_V2
    // if you are getting a compiler error for:
    // error CS0234: The type or namespace name 'PostProcessing' does not exist in the namespace 'UnityEngine.Rendering' (are you missing an assembly reference?)
    // this is because you have UNITY_POST_PROCESSING_STACK_V2 defined in the preprocessor macros but the package is missing from package manager.
    // either remove the "UNITY_POST_PROCESSING_STACK_V2" macro from PROJECT SETTINGS->PLAYER->SCRIPTING DEFINE SYMBOLS.... 
    // or add the post processing stack v2 with the package manager.
    
    
    [Header("in the folder /Packages/Post Processing/PostProcessing/")]
    [Header("This should be connected to PostProcessResources.asset")]
    public PostProcessResources ppResources;

    [Header("Only needed if using HINTED mode, automatic uses the post processing settings from the example camera.")]
    public LayerMask volumeLayer = ~0;

    public PostProcessLayer.Antialiasing antialiasingMode = PostProcessLayer.Antialiasing.None;
    public bool stopNaNPropagation = true;
    private bool loggedOnce = false;

   static private void UpgradePostProcessLayerIfNeeded(PostProcessLayer faceCameraPostProcessing, bool upgrade, bool warn) {
        // HELLO IF YOU SEE A COMPILER ERROR HERE...
        // JUST COMMENT THESE LINES OUT:
        // PLEASE REPORT BACK WHAT UNITY VERSION YOU ARE USING AND WHAT VERSION OF THE POST PROCESSING V2 Stack
        if (faceCameraPostProcessing.fastApproximateAntialiasing != null && faceCameraPostProcessing.antialiasingMode == PostProcessLayer.Antialiasing.FastApproximateAntialiasing &&
            faceCameraPostProcessing.fastApproximateAntialiasing.keepAlpha == false
        ) {
            if (warn) {
                Debug.Log("WE SUGGEST YOU SET  camera->postprocessinglayer->antialiasing->KeepAlpha to be true");
            }

            if (upgrade) {
                faceCameraPostProcessing.fastApproximateAntialiasing.keepAlpha = true;
            }
        }
    }

    protected override void ApplyPostProcessingAutomatic(CubeFaceCamera cam, SuperCubeMapper cubeCapture) {
        if (cubeCapture.exampleCamera) {
            PostProcessLayer faceCameraPostProcessing = cam.m_camera.gameObject.AddComponent<PostProcessLayer>();
            PostProcessLayer examplePostProcessing = cubeCapture.exampleCamera.GetComponent<PostProcessLayer>();
            if (examplePostProcessing && examplePostProcessing.enabled) {
                faceCameraPostProcessing.volumeLayer = examplePostProcessing.volumeLayer;

                faceCameraPostProcessing.stopNaNPropagation = examplePostProcessing.stopNaNPropagation;

                // HELLO IF YOU SEE A COMPILER ERROR HERE...
                // JUST COMMENT THESE LINES OUT:
                // PLEASE REPORT BACK WHAT UNITY VERSION YOU ARE USING AND WHAT VERSION OF THE POST PROCESSING V2 Stack
                faceCameraPostProcessing.antialiasingMode = examplePostProcessing.antialiasingMode;
                faceCameraPostProcessing.fastApproximateAntialiasing = examplePostProcessing.fastApproximateAntialiasing; // WARNING... if you start changing stuff on this you should clone that rather than link it.
                switch (cubeCapture.settingsFromFile.postprocesssing_tweaks.valueRaw) {
                    case POSTPROCESSINGTWEAKS.AUTOMATIC:
                        UpgradePostProcessLayerIfNeeded(faceCameraPostProcessing, true, false);
                        break;
                    case POSTPROCESSINGTWEAKS.DISABLED_QUIET:
                        break;
                    case POSTPROCESSINGTWEAKS.WARN:
                        UpgradePostProcessLayerIfNeeded(faceCameraPostProcessing, false, true);
                        break;
                    default:
                        Debug.Log("UNKNOWN MODE");
                        break;
                }

                //faceCameraPostProcessing.finalBlitToCameraTarget =  examplePostProcessing.finalBlitToCameraTarget;

                faceCameraPostProcessing.Init(ppResources);
                if (examplePostProcessing.volumeTrigger == cubeCapture.exampleCamera.transform) {
                    faceCameraPostProcessing.volumeTrigger = cam.m_camera.transform;
                }
            } else {
                if (!loggedOnce) {
                    loggedOnce = true;
                    if (examplePostProcessing && !examplePostProcessing.enabled) {
                        Debug.Log("UnityEngine.PostProcessing.PostProcessingBehaviour on example camera is disabled... skipping.");
                    } else {
                        Debug.Log("UnityEngine.PostProcessing.PostProcessingBehaviour profile not found on example camera.  The developer can add the profile to the example camera or switch to Hinted.");
                    }
                }
            }
        }
    }

    protected override void ApplyPostProcessingHinted(CubeFaceCamera cam, SuperCubeMapper cubeCapture) {
        PostProcessLayer faceCameraPostProcessing = cam.m_camera.gameObject.AddComponent<PostProcessLayer>();
        faceCameraPostProcessing.volumeLayer = volumeLayer;
        faceCameraPostProcessing.antialiasingMode = antialiasingMode;
        faceCameraPostProcessing.stopNaNPropagation = stopNaNPropagation;
        faceCameraPostProcessing.volumeTrigger = cam.m_camera.transform;
    }

#else
	[Header ("is set under project settings - > player - > scripting define symbols ")]
	[Header ("and ensure the preprocessor define UNITY_POST_PROCESSING_STACK_V2")]
	[Header ("Add it with the package manager and")]
	[Header ("To enable Post Processing V2 Stack")]
	public bool disableWarning = false;
	public void Start () {
		if (!disableWarning) {
			Debug.LogError ("UNITY_POST_PROCESSING_STACK_V2 preprocessor macro is missing:  ensure the code is there and the macro is defined.");
			Debug.LogError ("To enable Post Processing V2 Stack: Add the post processing stack via the package manager.  then ensure that  UNITY_POST_PROCESSING_STACK_V2 preprocessor macro is defined under project settings - > player - > scripting define symbols ");
		}
	}
	override protected void ApplyPostProcessingHinted (CubeFaceCamera cam, SuperCubeMapper cubeCapture) { }
	override protected void ApplyPostProcessingAutomatic (CubeFaceCamera cam, SuperCubeMapper cubeCapture) { }
#endif
}

/*  
// todo automate the connection of the resources...
public class EffectHelperEditor : EditorWindow {

	private class XAllPostprocessor : AssetPostprocessor {

		private static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) { }
	}

	[MenuItem ("Elumenati/Install OmnityPostProcessingStack")]
	private static void InstallEffectHelper () {
		var o = GameObject.FindObjectOfType<CubeCapture> ();
		if (o && o.gameObject.GetComponent<PostProcessing_Stack2> () == null) {
			PostProcessing_Stack2 opp = o.gameObject.AddComponent<PostProcessing_Stack2> ();

			string[] guids1 = AssetDatabase.FindAssets ("PostProcess", null);
			foreach (var g in guids1) {
				Debug.Log (g);
			}
			Debug.Log (guids1.Length);

			opp.ppResources = (PostProcessResources) AssetDatabase.LoadAssetAtPath ("Packages/Post Processing/PostProcessing/PostProcessResources", typeof (PostProcessResources));
		} else {
			PostProcessing_Stack2 opp = o.gameObject.GetComponent<PostProcessing_Stack2> ();
			Debug.Log (AssetDatabase.GetAssetPath (opp.ppResources));
			string[] guids1 = AssetDatabase.FindAssets ("PostProcess", null);
			foreach (var g in guids1) {
				Debug.Log (g);
			}
			Debug.Log (guids1.Length);
			opp.ppResources = (PostProcessResources) AssetDatabase.LoadAssetAtPath ("Packages/Post Processing/PostProcessing/PostProcessResources", typeof (PostProcessResources));

		}
	}
}*/
