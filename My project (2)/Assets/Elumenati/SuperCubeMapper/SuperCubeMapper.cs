using System.Collections.Generic;
using UnityEngine;

// ////////////////////////
// SuperCubeMapper
// www.Elumenati.com
// Written by Dr. Clement Shimizu Elumenati
// See the readme file and the SuperCubeMapper.ini 
// settings file that gets generated at runtime
// ////////////////////////

// Use the following preprocessor flags at your own risk
// (should be enabled globally under Player Settings->Configuration->Scripting Define Symbols)
// RENDERING_VOLUME                <- needed to disable vignetting on certain modes (must be enabled manually)
// EXPERIMENTAL_HDPIPELINE         <- needed to copy skybox settings on (must be enabled manually)
// UNITY_POST_PROCESSING_STACK_V1  <- needed to enable post processing effects if using the v1 stack (must be enabled manually)
// UNITY_POST_PROCESSING_STACK_V2  <- needed to enable post processing effects if using the v2 stack (this is usually automatically enabled)


namespace Elumenati {
    #region CubeMapper

    public class SuperCubeMapper : MonoBehaviour {
        [System.NonSerialized]
        public Camera exampleCamera = null;

        [Header("Project Settings set via inspector")]
        public Settings settings = new Settings();

        [Header("Runtime Settings (changed in SuperCubeMapper.ini)")]
        [SerializeField]
        private SettingsFromFile _settingsFromFile = new SettingsFromFile();

        public SettingsFromFile settingsFromFile {
            get {
                if(!_settingsFromFile.beenRead) {
                    _settingsFromFile = new SettingsFromFile();
                    _settingsFromFile.LoadSettings();
                }
                return _settingsFromFile;
            }
        }


        private bool isStereo {
            get {
                switch(settingsFromFile.stereo_mode.valueRaw) {
                    case Stereo3DMode.MONO:
                        return false;
                    case Stereo3DMode.STEREO_EYE_SEPARATED:
                        return true;
                    default:
                        Debug.LogWarning("UNKNOWN STEREO MODE" + settingsFromFile.stereo_mode.valueRaw);
                        return false;
                }
            }
        }

        private void Awake() {
            PostProcessing_Helper.CheckForErrors();

            if(!settingsFromFile.superCubeMapperEnabled) {
                Debug.Log("SettingsFromFile.superCubeMapperEnabled is false");
                enabled = false;
                return;
            }

            if(settingsFromFile.kill_vignette) {
                PostProcessEffectDisabler.GlobalDisableVignette();
            }
            exampleCamera = GetComponent<Camera>();

            if(!isStereo) {
                leftEye.CreateCubemap(this, transform);
            } else {
                GameObject cubemapRig = new GameObject("Cubemap Rig");
                cubemapRig.transform.SnapTo(transform);
                leftEye.CreateCubemap(this, cubemapRig.transform, STEREO_EYE.LEFT);
                rightEye.CreateCubemap(this, cubemapRig.transform, STEREO_EYE.RIGHT);
            }

            if(!isStereo) {
                leftEye.UpdateBlitMaterial(this);
            } else {
                leftEye.UpdateBlitMaterial(this);
                rightEye.UpdateBlitMaterial(this);
            }

            if(exampleCamera) {
                exampleCamera.enabled = settingsFromFile.primaryCameraEnabled;
            }

            if(
                exampleCamera != null && !exampleCamera.enabled &&
                settingsFromFile.dummycameraEnabled) {
                GameObject dummyCameraGo = new GameObject("DummyCamera");
                dummyCameraGo.transform.SnapTo(transform);
                dummyCamera = dummyCameraGo.AddComponent<Camera>();
                dummyCamera.clearFlags = CameraClearFlags.Color;
                dummyCamera.cullingMask = 0;
                dummyCamera.eventMask = 0;
                dummyCamera.depth = -1000;
                dummyCamera.backgroundColor = Color.black;
            }

            if(settingsFromFile.outputResult_enabled.valueRaw) {
                SuperCubeMapperBlit.Init(this);
            }

            if(settingsFromFile.generateRenderTexture) {
                int width, height;
                switch(settingsFromFile.texture_format.valueRaw) {
                    case TEXTUREFORMAT.EQUIRECTANGULAR:
                        height = settingsFromFile.cubeface_resolution * 2;
                        width = settingsFromFile.cubeface_resolution * 3;
                        if(settingsFromFile.stereo_mode.valueRaw == Stereo3DMode.STEREO_EYE_SEPARATED) {
                            height *= 2;
                        } else if(settingsFromFile.stereo_mode.valueRaw == Stereo3DMode.MONO) {
                            
                        } else {
                            Debug.LogWarning("UNKNOWN MODE");
                        }

                        break;
                    case TEXTUREFORMAT.CUBEMAP:
                        height = settingsFromFile.cubeface_resolution * 2;
                        width = settingsFromFile.cubeface_resolution * 3;
                        if(settingsFromFile.stereo_mode.valueRaw == Stereo3DMode.STEREO_EYE_SEPARATED) {
                            width *= 2;
                        } else if(settingsFromFile.stereo_mode.valueRaw == Stereo3DMode.MONO) {
                            
                        } else {
                            Debug.LogWarning("UNKNOWN MODE");
                        }

                        break;
                    case TEXTUREFORMAT.FISHEYE:
                        height = width = settingsFromFile.cubeface_resolution * 2;
                        if(settingsFromFile.stereo_mode.valueRaw == Stereo3DMode.STEREO_EYE_SEPARATED) {
                            height *= 2;
                        } else if(settingsFromFile.stereo_mode.valueRaw == Stereo3DMode.MONO) {
                            
                        } else {
                            Debug.LogWarning("UNKNOWN MODE");
                        }

                        break;
                    case TEXTUREFORMAT.OMNITY:
                        height = width = settingsFromFile.cubeface_resolution * 2;
                        if(settingsFromFile.stereo_mode.valueRaw == Stereo3DMode.STEREO_EYE_SEPARATED) {
                            height *= 2;
                        } else if(settingsFromFile.stereo_mode.valueRaw == Stereo3DMode.MONO) {
                            
                        } else {
                            Debug.LogWarning("UNKNOWN MODE");
                        }

                        break;
                    default:
                        height = settingsFromFile.cubeface_resolution * 2;
                        width = settingsFromFile.cubeface_resolution * 3;
                        if(settingsFromFile.stereo_mode.valueRaw == Stereo3DMode.STEREO_EYE_SEPARATED) {
                            height *= 2;
                        } else if(settingsFromFile.stereo_mode.valueRaw == Stereo3DMode.MONO) {
                            
                        } else {
                            Debug.LogWarning("UNKNOWN MODE");
                        }
                        Debug.LogWarning("Unknown mode");
                        break;
                }
                output = new RenderTexture(width, height, 24, RenderTextureFormat.Default);

                if(settingsFromFile.useSpout && SpoutExtensions.IsSpoutPlatform()) {
                    spoutSenderGameObject = SpoutExtensions.AddKlackSpoutSender(settingsFromFile.textureSharingName, transform, output, settings.showCamerasForDebugging ? HideFlags.DontSave : HideFlags.HideAndDontSave, errorMessage => { Debug.LogError(errorMessage); });
                }

                if(settingsFromFile.useNDI && SpoutExtensions.IsNDIPlatform()) {
                    ndiSenderGameObject = SpoutExtensions.AddKlackNDI(settingsFromFile.textureSharingName, transform, output, settings.showCamerasForDebugging ? HideFlags.DontSave : HideFlags.HideAndDontSave, errorMessage => { Debug.LogError(errorMessage); });
                }

                if(settingsFromFile.useSyphon && SpoutExtensions.IsSyphonPlatform()) {
                    syphonSenderGameObject = SpoutExtensions.AddKlackSyphon(settingsFromFile.textureSharingName, transform, output, settings.showCamerasForDebugging ? HideFlags.DontSave : HideFlags.HideAndDontSave, errorMessage => { Debug.LogError(errorMessage); });
                }
            }
            
        }

        [System.NonSerialized]
        public GameObject spoutSenderGameObject;

        [System.NonSerialized]
        public GameObject ndiSenderGameObject;

        [System.NonSerialized]
        public GameObject syphonSenderGameObject;

        private void OnDestroy() {
            Destroy(spoutSenderGameObject);
            Destroy(ndiSenderGameObject);
            Destroy(syphonSenderGameObject);
            if(output) {
                output.Release();
                Destroy(output);
                output = null;
            }

            if(leftEye != null) {
                leftEye.DoDestroy();
            }

            if(rightEye != null) {
                rightEye.DoDestroy();
            }
        }

        [System.NonSerialized]
        public static System.Action<SuperCubeMapper, RenderTexture> onNewRenderTexture = (a, b) => { };

        public class CaptureRig {
            private readonly List<Matrix4x4> arrayMatrix = new List<Matrix4x4>();
            public Material blitMaterial;
            private STEREO_EYE eye = STEREO_EYE.BOTH;

            public void UpdateBlitMaterial(SuperCubeMapper superCubeMapper) {
                if(superCubeMapper.settingsFromFile.cubemap_overdraw_enabled || superCubeMapper.settingsFromFile.texture_format != TEXTUREFORMAT.CUBEMAP) {
                    blitMaterial.SetMatrixArray(camMatrixArrayPropertyID, arrayMatrix);
                    blitMaterial.SetFloat(fovOver90MinusOnePropertyID, superCubeMapper.borderbFovOver90MinusOne);
                }

                if(superCubeMapper.isStereo) {
                    bool sidebyside = true;
                    switch(superCubeMapper.settingsFromFile.texture_format.valueRaw) {
                        case TEXTUREFORMAT.CUBEMAP:
                            sidebyside = true;
                            break;
                        case TEXTUREFORMAT.EQUIRECTANGULAR:
                            sidebyside = false;
                            break;
                        case TEXTUREFORMAT.FISHEYE:
                            sidebyside = false;
                            break;
                        case TEXTUREFORMAT.OMNITY:
                            sidebyside = false;
                            break;
                        default:
                            Debug.LogWarning("PATH NOT DEFINED");
                            break;
                    }

                    // ROTATION FOR EQUICUBE SET USING THE STEREO FLAG && EQUICUBE90/EQUICUBE
                    switch(eye) {
                        case STEREO_EYE.BOTH:
                            Debug.LogWarning("this setting isn't handled");
                            break;
                        case STEREO_EYE.LEFT:
                            blitMaterial.SetVector(offsetScalePropertyID, sidebyside ? new Vector4(-.5f, 0, .5f, -1) : new Vector4(0, -.5f, 1, .5f));
                            break;
                        case STEREO_EYE.RIGHT:
                            blitMaterial.SetVector(offsetScalePropertyID, sidebyside ? new Vector4(.5f, 0, .5f, -1) : new Vector4(0, .5f, 1, .5f));
                            break;
                        default:
                            Debug.LogWarning("UNKNOWN MODE");
                            break;
                    }
                }
            }

            public void DoDestroy() {
                cubemapCameras.Clear();
                Destroy(blitMaterial);
                blitMaterial = null;
            }

            [System.NonSerialized]
            public List<CubeFaceCamera> cubemapCameras = new List<CubeFaceCamera>();

            private static void ApplyPostProcessing(CubeFaceCamera cam, SuperCubeMapper superCubeMapper) {
                superCubeMapper.settings.postProcessingStackV1_options.ApplyPostProcessing(cam, superCubeMapper);
                superCubeMapper.settings.postProcessingStackV2_options.ApplyPostProcessing(cam, superCubeMapper);
                superCubeMapper.settings.postProcessingStackCustom_options.ApplyPostProcessing(cam, superCubeMapper);
            }

            public void CreateCubemap(SuperCubeMapper superCubeMapper, Transform parent, STEREO_EYE _eye = STEREO_EYE.BOTH) {
                eye = _eye;
                Shader cubeFlattenShader = Shader.Find("Hidden/Elumenati/SuperCubeMapper");
                if(cubeFlattenShader == null) {
                    Debug.LogError("shader Hidden/Elumenati/SuperCubeMapper missing.  Make sure SuperCubeMapper.shader exist in a resources folder AND that the shader is named Hidden/Elumenati/SuperCubeMapper.  If the shader works in the editor and not in the build.  Make sure its not being stripped during compile time.");
                }

                if(!blitMaterial) {
                    blitMaterial = new Material(cubeFlattenShader);
                }

                switch(superCubeMapper.settingsFromFile.texture_format.valueRaw) {
                    case TEXTUREFORMAT.EQUIRECTANGULAR:
                        if(superCubeMapper.settingsFromFile.cubemap_overdraw_enabled) {
                            blitMaterial.EnableKeyword("EQUIRECTANGULAR");
                        } else {
                            blitMaterial.EnableKeyword("EQUIRECTANGULAR90");
                        }

                        break;
                    case TEXTUREFORMAT.FISHEYE:
                        if(superCubeMapper.settingsFromFile.cubemap_overdraw_enabled) {
                            blitMaterial.EnableKeyword("FISHEYE");
                        } else {
                            blitMaterial.EnableKeyword("FISHEYE90");
                        }

                        blitMaterial.EnableKeyword("FULLDOME");
                        break;


                    case TEXTUREFORMAT.OMNITY:
                        if(superCubeMapper.settingsFromFile.cubemap_overdraw_enabled) {
                            blitMaterial.EnableKeyword("OMNITY");
                        } else {
                            blitMaterial.EnableKeyword("OMNITY90");
                        }

                        if(superCubeMapper.settingsFromFile.fisheyecrop_enabled) {
                            blitMaterial.EnableKeyword("CROP");
                            blitMaterial.SetFloat("_fisheye_offset", superCubeMapper.settingsFromFile.fisheye_offset);
                        } else {
                            blitMaterial.EnableKeyword("FULLDOME");
                        }

                        Matrix4x4 projectorRotation = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(superCubeMapper.settingsFromFile.projector_rotation.valueRaw.PitchYawRoll_to_Euler()), Vector3.one);
                        blitMaterial.SetMatrix("_projector_rotation", projectorRotation);
                        blitMaterial.SetVector("_domexyz", superCubeMapper.settingsFromFile.dome_offset.valueRaw);
                        blitMaterial.SetVector("_projectorxyz", superCubeMapper.settingsFromFile.projector_offset.valueRaw);
                        break;
                    case TEXTUREFORMAT.CUBEMAP:
                        if(superCubeMapper.settingsFromFile.cubemap_overdraw_enabled) {
                            blitMaterial.EnableKeyword("EQUICUBE");
                        } else {
                            blitMaterial.EnableKeyword("EQUICUBE90");
                        }

                        break;
                    default:
                        break;
                }

                if(superCubeMapper.isStereo) {
                    blitMaterial.EnableKeyword("STEREO");
                } else {
                    blitMaterial.EnableKeyword("MONO");
                }

                cubemapCameras.Clear();
                Vector3[] angles = {
                    new Vector3(0, -90, 0),
                    new Vector3(0, 0, 0),
                    new Vector3(0, 90, 0),
                    new Vector3(90, 0, -90),
                    new Vector3(0, 180, 90),
                    new Vector3(-90, -90, 0)
                };

                GameObject node = new GameObject(superCubeMapper.isStereo ? "Cubemap " + eye : "Cubemap");
                node.transform.SnapTo(parent);

                switch(eye) {
                    case STEREO_EYE.BOTH:
                        node.transform.localPosition = superCubeMapper.settingsFromFile.cubemap_offset.valueRaw;
                        node.transform.localEulerAngles = superCubeMapper.settingsFromFile.cubemap_rotation.valueRaw.PitchYawRoll_to_Euler(); // this needs to be last
                        break;
                    case STEREO_EYE.LEFT:
                        node.transform.localPosition = superCubeMapper.settingsFromFile.cubemap_offset.valueRaw + superCubeMapper.settingsFromFile.stereo_left_eye_offset;
                        node.transform.localEulerAngles = superCubeMapper.settingsFromFile.stereo_left_eye_rotation.valueRaw.PitchYawRoll_to_Euler();
                        // use quaternions to do this as local rotation
                        //node.transform.localEulerAngles += superCubeMapper.settingsFromFile.cubemap_rotation.valueRaw.PitchYawRoll_to_Euler ();
                        break;
                    case STEREO_EYE.RIGHT:
                        node.transform.localPosition = superCubeMapper.settingsFromFile.cubemap_offset.valueRaw + superCubeMapper.settingsFromFile.stereo_right_eye_offset;
                        node.transform.localEulerAngles = superCubeMapper.settingsFromFile.stereo_right_eye_rotation.valueRaw.PitchYawRoll_to_Euler();
                        // use quaternions to do this as local rotation
                        //node.transform.localEulerAngles += superCubeMapper.settingsFromFile.cubemap_rotation.valueRaw.PitchYawRoll_to_Euler ();
                        break;
                    default:
                        break;
                }

                for(int i = 0; i < angles.Length; i++) {
                    CubeFaceCamera cam = new CubeFaceCamera("cam_" + i, node.transform, superCubeMapper.settingsFromFile.cubeface_resolution, angles[i], superCubeMapper.exampleCamera, superCubeMapper.fov, eye, superCubeMapper.settingsFromFile.mipmapBias, superCubeMapper.settingsFromFile.texturefiltermode.valueRaw);
                    cubemapCameras.Add(cam);
                    blitMaterial.SetTexture("_MainTex" + i, cam.renderTexture);
                    ApplyPostProcessing(cam, superCubeMapper);
                    cam.m_camera.gameObject.hideFlags = superCubeMapper.settings.showCamerasForDebugging ? HideFlags.DontSave : HideFlags.HideAndDontSave;
                    cam.m_camera.gameObject.SetActive(superCubeMapper.settingsFromFile.cubefaces_enabled[i].valueRaw);
                    arrayMatrix.Add(cam.perspectiveProjectionMappingMatrix);
                }
            }
        }

        [System.NonSerialized]
        public CaptureRig leftEye = new CaptureRig();

        [System.NonSerialized]
        public CaptureRig rightEye = new CaptureRig();

        [System.NonSerialized]
        public Camera dummyCamera;

        public RenderTexture output;
        private static readonly int camMatrixArrayPropertyID = Shader.PropertyToID("_CamMatrixArray");
        private static readonly int fovOver90MinusOnePropertyID = Shader.PropertyToID("_borderb_fov_over_90_minus_one");
        private static readonly int offsetScalePropertyID = Shader.PropertyToID("_OffsetScale");

        public void Draw(bool drawToScreen) {
            switch(settingsFromFile.stereo_mode.valueRaw) {
                case Stereo3DMode.MONO:
                    leftEye.UpdateBlitMaterial(this);
                    if(drawToScreen) {
                        Graphics.Blit(null, leftEye.blitMaterial);
                    } else {
                        Graphics.Blit(null, output, leftEye.blitMaterial);
                    }
                    break;
                case Stereo3DMode.STEREO_EYE_SEPARATED:
                    if(drawToScreen) {
                        leftEye.UpdateBlitMaterial(this);
                        Graphics.Blit(null,  leftEye.blitMaterial);
                        rightEye.UpdateBlitMaterial(this);
                        Graphics.Blit(null,  rightEye.blitMaterial);
                    } else {
                        leftEye.UpdateBlitMaterial(this);
                        Graphics.Blit(null, output, leftEye.blitMaterial);
                        rightEye.UpdateBlitMaterial(this);
                        Graphics.Blit(null, output, rightEye.blitMaterial);
                    }

                    break;
                default:
                    Debug.LogError("Error unknown mode, " + settingsFromFile.stereo_mode);
                    break;
            }
        }
        
        private void Update() {
            if(settingsFromFile.generateRenderTexture) {
                Draw(false);
            }
        }

        private float fov {
            get {
                if(settingsFromFile.cubemap_overdraw_enabled) {
                    return settingsFromFile.cubemap_overdraw_fov.valueRaw;
                } else {
                    return 90;
                }
            }
        }

        private float borderbFovOver90MinusOne {
            get { return fov / 90f - 1; }
        }
    }

    [System.Serializable]
    public class CubeFaceCamera {
        public string name;
        public Camera m_camera;
        public RenderTexture renderTexture;

        ~CubeFaceCamera() {
            if(renderTexture) {
                renderTexture.Release();
                renderTexture = null;
            }

            if(m_camera) {
                m_camera.targetTexture = null;
                Object.Destroy(m_camera.gameObject);
            }
        }

        public CubeFaceCamera(string _name, Transform parent, int size, Vector3 angle, Camera exampleCamera, float m_fov, STEREO_EYE eye, float mipMapBias, SettingsFromFile.TextureFilterMode tfmode) {
            name = _name;
            GameObject go = new GameObject("Camera " + name + " " + angle);
            m_camera = go.AddComponent<Camera>();

            if(exampleCamera) {
                m_camera.CopyFrom(exampleCamera);
                HDPipelineTweaks.CopyCameraFromTo(exampleCamera, m_camera);
            }

            renderTexture = new RenderTexture(size, size, 24, RenderTextureFormat.Default);

            switch(tfmode){
                case SettingsFromFile.TextureFilterMode.POINT:
                    renderTexture.filterMode = FilterMode.Point;
                break;
                case SettingsFromFile.TextureFilterMode.BILINEAR:
                    renderTexture.filterMode = FilterMode.Bilinear;
                break;
                case SettingsFromFile.TextureFilterMode.TRILINEAR:
                    renderTexture.filterMode = FilterMode.Trilinear;
                    renderTexture.useMipMap = true;
                break;
                case SettingsFromFile.TextureFilterMode.DEFAULT:
                default:
                break;
            }
            renderTexture.mipMapBias = mipMapBias;

            m_camera.targetTexture = renderTexture;

            go.transform.SnapTo(parent);
            go.transform.localEulerAngles = angle;

            m_camera.usePhysicalProperties = false;
            m_camera.fieldOfView = m_fov;
            m_camera.projectionMatrix = Matrix4x4.Perspective(m_fov, 1, m_camera.nearClipPlane, m_camera.farClipPlane);
            
            //      if(eye != STEREO_EYE.BOTH) {
            //                Debug.Log("DO SOMETHING WITH " + eye);
            //      }
        }

        // See http://www.clementshimizu.com/omnimap-projective-perspective-mapping-api-for-non-planar-immersive-display-surfaces/
        public Matrix4x4 perspectiveProjectionMappingMatrix {
            get {
                Matrix4x4 M = m_camera.transform.parent.transform.localToWorldMatrix;
                Matrix4x4 V = m_camera.worldToCameraMatrix;
                Matrix4x4 p = m_camera.projectionMatrix;
                return Matrix4x4.TRS(new Vector3(.5f, .5f, .5f), Quaternion.identity, Vector3.one) * Matrix4x4.Scale(new Vector3(.5f, .5f, .5f)) * p * V * M;
            }
        }
    }

    #endregion

    #region Settings

    // settings changed via the editor
    [System.Serializable]
    public class Settings {
        public bool showCamerasForDebugging = true;

        public enum PostProcessType {
            MANUAL = 0, // ths scripts wont be used... you will customize the cameras on your own.
            AUTOMATIC = 1, // the settings are copied from this camera's post processing  this is suggested
            HINTED = 2 // the settings are set from the postProcessingStackXXX_options variables.  use this if you want to have a different post processing for the cube map.
            // we suggest that you use only one of the three systems.
        }


        [Header("You can disable post processing at run time using the ini file.")]
        public PostProcessType postProcessType = PostProcessType.AUTOMATIC;

        [Header("For unity post processing stack v2 to work you must add the system with the package manager, and ensure that the preprocessor macro UNITY_POST_PROCESSING_STACK_V2 is set.")]
        public PostProcessing_Stack2 postProcessingStackV2_options = new PostProcessing_Stack2();

        [Header("For unity post processing stack v1 to work you will need to set the preprocessor into the project and ensure the macro UNITY_POST_PROCESSING_STACK_V1 is set.")]
        public PostProcessing_Stack1 postProcessingStackV1_options = new PostProcessing_Stack1();

        [Header("Modify PostProcessing_Custom.cs if you are using a post processing system other than unity's post process stack V1 or V2.")]
        public PostProcessing_Custom postProcessingStackCustom_options = new PostProcessing_Custom();
    }

    // settings changed via the ini file
    [System.Serializable]
    public class SettingsFromFile : SettingsFile {
        private SettingsComment preamble1 = new SettingsComment(@"SuperCubeMapper

A one stop solution for generating and broadcasting real-time cubemaps or equirectangular textures for Unity3d.
This allows a SuperCubeMapper enabled app to work with immersive VR projection mapped displays like domes from the www.Elumenati.com
This plugin passing the work of projection mapping, fisheye projection optics, and edge blending to apps like Elumenati WorldViewer.

This plugin has many features:
* texture sharing enables sharing 360 views between the game application and a secondary application like Elumenati WorldViewer.
* supports post processing while avoiding stitching artifacts 
* generates stereo cubemaps or stereo equirectangular (unsupported bonus feature)
* supports 360 equirectangular and Google's equicube format
* configurable at run-time

If you delete this file it will be regenerated with defaults.
* 2048 per face cubemap in an Equicube format
* post processing enabled.
* broadcast with Spout (pc), Syphon (mac), or Newtek NDI if it is installed

How to improve performance:
Because the defaults of this plugin cause the scene to be rendered 7 times, it behooves you to disable unneeded cameras if your performance is unacceptable.

* disable the primary camera and use the first display for GUI only by setting primarycamera_enabled = false.
* disable post processing if you do not use by setting postprocesssing_enabled = false
* disable cubemap overdraw if you post processing effects do not need it (bloom needs it, color grading does not need it) by setting cubemap_overdraw_enabled = false
* often you do not need a full 6 faced cubemap to fully cover a dome display
* the yaw pitch and roll of the cubemap can be changed to orient cubemap faces to cover a dome display's active with less
  cubemap faces than you would expect.
* using these tricks in combination will get you the most bang for your buck!

About this settings file:
This file regenerates itself resets any option to its to default if it is not active in the config or if the plugin is updated.
If you delete or comment out a line from this config, it will reset that one line to the default.
Unneeded settings are hidden util the feature that requires the setting is enabled and the application is re-run.
Some text editors like notepad.exe will not reflect the newly unlocked settings until the file is reopened.
Please back up this file if you have carefully tweaked settings that you do not want to lose and make sure the settings are correct
after updating the plugin.

Contact us:
SuperCubeMapper was built by Dr. Clement Shimizu and The Elumenati 
Follow us online at:
Web : www.Elumenati.com
Twitter : @elumenati
Instagram : @the_elumenati
Instagram : @drclementshimizu
", false);

        private SettingsComment OPTIONS = new SettingsComment("OPTIONS:");
        public SettingsBool superCubeMapperEnabled = new SettingsBool("supercubemapper_enabled", true, "Set false to totally disable this plugin.");
        public SettingsBool primaryCameraEnabled = new SettingsBool("primarycamera_enabled", true, "Set true to leave the example camera enabled.");
        public SettingsBool dummycameraEnabled = new SettingsBool("dummycamera_enabled", true, "If you disable the main camera, this dummy camera will clear the screen to prepare the screen for Unity UI or GUI.");

        private SettingsComment POSTPROCESSING = new SettingsComment("POST PROCESSING:");
        public SettingsBool postprocesssing_enabled = new SettingsBool("postprocesssing_enabled", true, "Copy post process effects to cubemap cameras.  Requires the post process stack V1 or V2 to be installed and set with the preprocessor macros.");
        public SettingsEnum<POSTPROCESSINGTWEAKS> postprocesssing_tweaks = new SettingsEnum<POSTPROCESSINGTWEAKS>("postprocesssing_tweaks", POSTPROCESSINGTWEAKS.AUTOMATIC, "valid options: " + POSTPROCESSINGTWEAKS.AUTOMATIC + " or " + POSTPROCESSINGTWEAKS.WARN + " or " + POSTPROCESSINGTWEAKS.DISABLED_QUIET);
        public SettingsBool kill_vignette = new SettingsBool("kill_vignette_enabled", true, "This should be set to true.  This disabled the vignette post processing effect that will cause a outlines.");
        private SettingsComment TEXTURE_SHARING = new SettingsComment("Texture Sharing: Enable this to send the cubemap from unity to a host application.  We suggest you only have one enabled.");
        public SettingsString textureSharingName = new SettingsString("texture_sharing_name", "supercubemapper", "Name to use for texture sharing, if it is enabled");
        public SettingsBool useSpout = new SettingsBool("spout_enabled", true, "send cubemap with spout texture sharing (PC only).  Requires Klack Spout to be installed.");
        public SettingsBool useSyphon = new SettingsBool("syphon_enabled", true, "send cubemap with syphon texture sharing (Mac only).  Requires Klack Syphon to be installed.");
        public SettingsBool useNDI = new SettingsBool("ndi_enabled", false, "send cubemap with NDI texture sharing.  Requires Klack NDI to be installed.");

        private SettingsComment CUBEMAP_FACES = new SettingsComment("CUBEMAP SETTINGS:");
        public SettingsEnum<TEXTUREFORMAT> texture_format = new SettingsEnum<TEXTUREFORMAT>("texture_format", TEXTUREFORMAT.EQUIRECTANGULAR, "valid options: " + TEXTUREFORMAT.CUBEMAP + ", " + TEXTUREFORMAT.EQUIRECTANGULAR + ", or " + TEXTUREFORMAT.FISHEYE);
        public SettingsInt cubeface_resolution = new SettingsInt("cubeface_resolution", 2048, "Resolution of each Cubemap face.  If overdraw is enabled, the textures may get resized in the process.");
        private SettingsComment CUBEMAP_OVERDRAW = new SettingsComment("CUBEMAP OVERDRAW: Enable this to reduce seam artifacts with some post processing effects at the expense of performance.  We suggest enabling overdraw only if it helps.  For example, if you disable post processing we suggest you disable overdraw too.  Many post processing effects do not need overdraw.");
        public SettingsBool cubemap_overdraw_enabled = new SettingsBool("cubemap_overdraw_enabled", true, "Over-draw the cubemap.");

        public SettingsFloat cubemap_overdraw_fov = new SettingsFloat("cubemap_overdraw_fov", 100, "Cubemap face fov for overdraw if enabled.  If set to 100, there will be 10 degrees of blend zone.");
   
        public SettingsBool forceRenderTexture = new SettingsBool("forcerendertexture_enabled", false, "Output result to render texture.  This should normally be set to false (which will generate the render texture only if it is needed.  If you want access to the render texture within unity and you are not using texture sharing then this will ensure that the render texture is generated.");

        private SettingsComment CUBEMAP_R = new SettingsComment("You can disable unneeded cubemap faces for performance");

        public List<SettingsBool> cubefaces_enabled =
            new List<SettingsBool> {
                new SettingsBool("cubeface_enabled_0", true, "Enable the cubemap's left face"),
                new SettingsBool("cubeface_enabled_1", true, "Enable the cubemap's forward face"),
                new SettingsBool("cubeface_enabled_2", true, "Enable the cubemap's right face"),
                new SettingsBool("cubeface_enabled_3", true, "Enable the cubemap's down face"),
                new SettingsBool("cubeface_enabled_4", true, "Enable the cubemap's behind face"),
                new SettingsBool("cubeface_enabled_5", true, "Enable the cubemap's up face")
            };

        private SettingsComment CUBEMAP_ORIENTATION = new SettingsComment("If you need to rotate the capture rig rotation");
        public SettingsVector3 cubemap_rotation = new SettingsVector3("cubemap_rotation", Vector3.zero, "Rotate the capture cubemap rigs's pitch up, yaw right, and roll clockwise in degrees.  This is standard pitch, yaw, and roll (not unity's left hand rule).  If set to 0, 0, 0 and exporting a cubemap, then the upper center cubemap face is forward and the lower center cubemap face is backwards...");
        public SettingsVector3 cubemap_offset = new SettingsVector3("cubemap_offset", Vector3.zero, "offset the capture cubemap rigs.  1 unitless units are usually 1 meter in human scale applications.");

        private SettingsComment DomeOutput = new SettingsComment("DomeOutput");

        public SettingsBool outputResult_enabled = new SettingsBool("outputresult_enabled", false, "Output result to display.  This should normally be set to false if you are using spout to display to dome via worldviewer.");
        public SettingsInt outputDisplayIndex = new SettingsInt("outputdisplay_index", 2, "Output result to primary display 1, secondary display 2, etc");

        public SettingsBool fisheyecrop_enabled = new SettingsBool("fisheyecrop_enabled", false, "set to false for fulldome output.  set to true for cropped display like the portal or theater");
        public SettingsFloat fisheye_offset = new SettingsFloat("fisheye_offset", 1, "set to -1 for theater or slammed portal, 1 for pano crop");
        public SettingsVector3 projector_rotation = new SettingsVector3("projector_rotation", Vector3.zero, "Rotate the projector rigs's pitch up, yaw right, and roll clockwise in degrees.  This is standard pitch, yaw, and roll (not unity's left hand rule). If in doubt leave this 0, 0, 0");
        public SettingsVector3 projector_offset = new SettingsVector3("projector_offset", Vector3.zero, "offset the capture projector rigs.  1 unitless units are usually 1 meter in human scale applications. If in doubt leave this 0, 0, 0");
        public SettingsVector3 dome_offset = new SettingsVector3("dome_offset", Vector3.zero, "offset the dome.  1 unitless units are usually 1 meter in human scale applications. If in doubt leave this 0, 0, 0");

        protected override string filename {
            get { return "SuperCubeMapper.ini"; }
        }

        public bool streamEnabled {
            get {
                if(useSpout && SpoutExtensions.IsSpoutPlatform()) {
                    return true;
                }
                if(useNDI && SpoutExtensions.IsNDIPlatform()) {
                    return true;
                }
                if(useSyphon && SpoutExtensions.IsSyphonPlatform()) {
                    return true;
                }
                return false;
            }
        }

        public bool generateRenderTexture {
            get { return streamEnabled || forceRenderTexture || !outputResult_enabled; }
        }

        public override void OnPreSave() {
            cubemap_overdraw_fov.shouldWeSaveFn = () => cubemap_overdraw_enabled;
            postprocesssing_tweaks.shouldWeSaveFn = () => postprocesssing_enabled;
            useSyphon.shouldWeSaveFn = SpoutExtensions.IsSyphonPlatform;
            useSpout.shouldWeSaveFn = SpoutExtensions.IsSpoutPlatform;
            useNDI.shouldWeSaveFn = SpoutExtensions.IsNDIPlatform;
            // forceRenderTexture.shouldWeSaveFn = () => !streamEnabled && !outputResult_enabled;
            projector_rotation.shouldWeSaveFn = dome_offset.shouldWeSaveFn = projector_offset.shouldWeSaveFn = fisheyecrop_enabled.shouldWeSaveFn = () => texture_format == TEXTUREFORMAT.OMNITY;
            fisheye_offset.shouldWeSaveFn = () => fisheyecrop_enabled && texture_format == TEXTUREFORMAT.OMNITY;
            stereo_left_eye_offset.shouldWeSaveFn = IsStereo;
            stereo_right_eye_offset.shouldWeSaveFn = IsStereo;
            stereo_left_eye_rotation.shouldWeSaveFn = IsStereo;
            stereo_right_eye_rotation.shouldWeSaveFn = IsStereo;
            dummycameraEnabled.shouldWeSaveFn = () => !primaryCameraEnabled && !outputResult_enabled;
            outputDisplayIndex.shouldWeSaveFn = () => outputResult_enabled;
        }


        private bool IsStereo() {
            return stereo_mode != Stereo3DMode.MONO;
        }

        #endregion

        #region STEREO

        private SettingsComment STEREOSCOPIC = new SettingsComment("These settings are for exclusively for stereoscopic displays.  Most domes are not stereoscopic so stereo_mode should set to "+Stereo3DMode.MONO+".  This is an unofficial unsupported bonus feature.  There are better ways to do 360 stereo. (For example, the eyes will be flipped on the rear of the dome if you are using in a 360 system.) ");
        public SettingsEnum<Stereo3DMode> stereo_mode = new SettingsEnum<Stereo3DMode>("stereo_mode", Stereo3DMode.MONO, "DEFAULT IS "+Stereo3DMode.MONO+", but on active and passive stereo systems you can use one of the stereo modes to experiment with stereoscopic rendering.  The "+Stereo3DMode.STEREO_EYE_SEPARATED+" is the simplest stereo mode and uses an offset for each view.  Contact Elumenati for more advanced options.");
        public SettingsVector3 stereo_left_eye_offset = new SettingsVector3("stereo_left_eye_offset", new Vector3(-.5f, 0f, 0f), "This unitless value is highly subjective.  Most virtual worlds use 1 unit as 1 meter so an eye offset of (-.1,0,0) would be a total of 10 cm from the center to the left eye in the negative X direction.  The default is .5 meter offset to accentuate the effect.  This is much bigger than it should be so make sure to turn it down.");
        public SettingsVector3 stereo_right_eye_offset = new SettingsVector3("stereo_right_eye_offset", new Vector3(.5f, 0f, 0f), "(.1,0,0) would be a total of 10 cm from the center to the right eye in the positive X direction.");
        public SettingsVector3 stereo_left_eye_rotation = new SettingsVector3("stereo_left_eye_rotation", new Vector3(0, 0f, 0f), "Rotation of left eye as pitch up, yaw right, and roll clockwise in degrees.    This is standard aviation pitch, yaw, and roll (not unity's left hand rule)");
        public SettingsVector3 stereo_right_eye_rotation = new SettingsVector3("stereo_right_eye_rotation ", new Vector3(0, 0f, 0f), "Rotation of right eye as pitch up, yaw right, and roll clockwise in degrees.   This is standard aviation pitch, yaw, and roll (not unity's left hand rule)");

        #endregion

        #region AdvancedQuality
         private SettingsComment AdvancedQuality = new SettingsComment("Advanced Settings for quality.  Defaults are suggested.  Sometimes minor quality improvements can be achived, but use this only if you are in front of the final display output becasue a smaller preview window often produces an inconsistent result compared to a larger display.");
         public SettingsFloat mipmapBias = new SettingsFloat("mipmapbias", 0, "Most people should keep this at 0.  negative values are sharpen, postive values are blur.  Use the smallest number to achive the goal.");

        public enum TextureFilterMode{
            DEFAULT,
            POINT,
            BILINEAR,
            TRILINEAR,
        }
         public SettingsEnum<TextureFilterMode> texturefiltermode = new SettingsEnum<TextureFilterMode>("texturefiltermode", TextureFilterMode.DEFAULT, 
         "Texture Filtering.  "+TextureFilterMode.DEFAULT+" is "+TextureFilterMode.BILINEAR+" filtering.  Enabling "+TextureFilterMode.TRILINEAR+" will automatically enable mip mapping.  "+TextureFilterMode.POINT + " is also an option.");
      

        #endregion


    }
}

public class SuperCubeMapperBlit : MonoBehaviour {
    public Elumenati.SuperCubeMapper superCubeMapper;
    public Camera targetCamera;

    private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        superCubeMapper.Draw(true);
    }

    public static void Init(Elumenati.SuperCubeMapper _superCubeMapper) {
        SuperCubeMapperBlit blit;
        if(_superCubeMapper.dummyCamera != null) {
            blit = _superCubeMapper.dummyCamera.gameObject.AddComponent<SuperCubeMapperBlit>();
            blit.targetCamera = _superCubeMapper.dummyCamera;
        } else {
            GameObject c2 = new GameObject();
            blit = c2.AddComponent<SuperCubeMapperBlit>();
            blit.targetCamera = c2.gameObject.AddComponent<Camera>();
            c2.transform.parent = _superCubeMapper.transform;
            c2.transform.localPosition = Vector3.zero;
            c2.transform.localEulerAngles = Vector3.zero;
            c2.transform.localScale = Vector3.one;
        }

        blit.superCubeMapper = _superCubeMapper;
        blit.targetCamera.cullingMask = 0;
        blit.targetCamera.targetDisplay = Mathf.Clamp(_superCubeMapper.settingsFromFile.outputDisplayIndex.valueRaw - 1, 0, 10);
        blit.targetCamera.allowHDR = false;
        blit.targetCamera.renderingPath = RenderingPath.Forward;
    }
}
