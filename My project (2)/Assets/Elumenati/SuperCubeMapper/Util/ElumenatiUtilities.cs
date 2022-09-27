using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Elumenati {
    #region SettingsBase

    [Serializable]
    public abstract class SettingsFile {
        protected abstract string filename { get; }
        protected List<SettingsFileLineBase> items = new List<SettingsFileLineBase>();

        private void ResetListErrors() {
            foreach(SettingsFileLineBase item in items) {
                item.wasRead = false;
            }
        }

        private void ReadLine(string line) {
            if(line.Trim().StartsWith("#")) {
                return;
            }

            char[] seperators = {'=', '#'};
            string[] kvp = line.Split(seperators);
            if(kvp.Length >= 2) {
                kvp[0] = kvp[0].ToLower().Trim();
                kvp[1] = kvp[1].Trim();
                foreach(SettingsFileLineBase item in items) {
                    try {
                        if(item.isComment) {
                            continue;
                        }

                        if(item.ReadLine(kvp[0], kvp[1])) {
                            return;
                        }
                    } catch {
                    }
                }
            }
        }

        [NonSerialized]
        public bool beenRead = false;

        public virtual void LoadSettings() {
            // will only read on the first round so you can call it multiple times
            // this will save a new settings file if it is missing.

            if(beenRead) {
                return;
            }

            items.Clear();
            AddItems();
            bool needsSave = false;
            ResetListErrors();

            CultureExtensions.PushCulture();
            try {
                if(System.IO.File.Exists(filename)) {
                    string[] lines = System.IO.File.ReadAllLines(filename);
                    foreach(string line in lines) {
                        try {
                            ReadLine(line);
                        } catch {
                            needsSave = true;
                        }
                    }

                    foreach(SettingsFileLineBase item in items) {
                        if(!item.wasRead) {
                            needsSave = true;
                            break;
                        }
                    }
                } else {
                    needsSave = true;
                }
            } catch(Exception e) {
                Debug.LogException(e);
            }

            CultureExtensions.PopCulture();

            if(needsSave) {
                SaveSettings();
            }

            beenRead = true;
            OnPostLoad();
        }

        public virtual void OnPostLoad() {
            foreach(SettingsFileLineBase item in items) {
                if(!item.shouldWeSaveFn()) {
                    // for whatever reason that should not be saved and that means we should
                    // reset this setting back to default
                    item.ResetValueToDefault();
                }
            }
        }

        public virtual void OnPreSave() {
            // put all of the shouldWeSaveFn here
            // for example 
            // cubemapresolution.shouldWeSaveFn = ()=>isCubemapEnabled;
            // the idea here is that we want to comment out any line from the settings that isn't useful.
        }

        public void SaveSettings() {
            try {
                CultureExtensions.PushCulture();
                try {
                    OnPreSave();
                } catch {
                }

                try {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    foreach(SettingsFileLineBase item in items) {
                        if(item.shouldWeSaveFn()) {
                            sb.AppendLine(item.fullLine);
                        }
                    }

                    System.IO.File.WriteAllText(filename, sb.ToString());
                } catch {
                    Debug.LogError("COULD NOT RECOVER FROM SETTINGS FILE ERROR");
                }
            } catch {
                CultureExtensions.PopCulture();
            }
        }

        protected virtual void AddItems() {
            FieldInfo[] fieldInfos = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach(FieldInfo fieldInfo in fieldInfos) {
                //Debug.Log ("item.GetType () " + fieldInfo.FieldType.ToString ());
                if (fieldInfo.Name == "items") {
                    continue;
                } else if (fieldInfo.FieldType.IsSubclassOf(typeof(SettingsFileLineBase))) {
                    SettingsFileLineBase value = (SettingsFileLineBase) fieldInfo.GetValue(this);
                    if(value != null) {
                        items.Add(value);
                    }
                } else if(fieldInfo.FieldType.IsSubclassOf(typeof(Array))) {
                    Array value = (Array) fieldInfo.GetValue(this);
                    if(value != null) {
                        foreach(object v in value) {
                            if(v.GetType().IsSubclassOf(typeof(SettingsFileLineBase))) {
                                SettingsFileLineBase vvv = (SettingsFileLineBase) v;
                                if(vvv != null) {
                                    items.Add(vvv);
                                }
                            }
                        }
                    }
                } else if(fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() ==
                          typeof(List<>)) {
                    Type itemType = fieldInfo.FieldType.GetGenericArguments()[0]; // use this...
                    if(itemType.IsSubclassOf(typeof(SettingsFileLineBase))) {
                        IEnumerable listGeneric = (IEnumerable) fieldInfo.GetValue(this);

                        if(listGeneric != null) {
                            foreach(object vvv in listGeneric) {
                                if(vvv.GetType().IsSubclassOf(typeof(SettingsFileLineBase))) {
                                    if(vvv != null) {
                                        items.Add((SettingsFileLineBase) vvv);
                                    }
                                }
                            }
                        }
                    } else {
                        // Debug.Log (fieldInfo.Name + " : " + fieldInfo.FieldType.ToString ());
                    }
                }
            }
        }
    }

    [Serializable]
    public abstract class SettingsFileLineBase {
        [NonSerialized]
        public string name;

        private string _comment;

        public string comment {
            get {
                if(_comment.Contains("=") || _comment.Contains("#")) {
                    return _comment.Replace("#", "\r\n#").Replace("=", "@");
                } else {
                    return _comment;
                }
            }
            set { _comment = value; }
        }

        public abstract bool ReadLine(string _name, string valueStr);

        [NonSerialized]
        protected bool _wasRead = false;

        public virtual bool wasRead {
            get { return _wasRead; }
            set { _wasRead = value; }
        }

        public void BeforeRead() {
            wasRead = false;
        }

        public abstract string valueSerialized { get; }
        public abstract string fullLine { get; }

        public virtual bool isComment {
            get { return false; }
        }

        public Func<bool> shouldWeSaveFn = () => { return true; };
        public abstract void ResetValueToDefault();
    }

    public abstract class SettingsFileLineB<T> : SettingsFileLineBase {
        public T valueRaw;
        public T defaultValueRaw;

        public SettingsFileLineB(string _name, T def, string _comment) {
            name = _name.ToLower().Trim();
            valueRaw = def;
            defaultValueRaw = def;
            comment = _comment;
        }

        public override bool ReadLine(string _name, string valueStr) {
            try {
                if(_name == name) {
                    valueRaw = Parse(valueStr);
                    wasRead = true;
                    return true;
                }
            } catch {
                throw new Exception("ERROR READ LINE FAILED " + _name + " = " + valueStr);
            }

            return false;
        }

        protected abstract T Parse(string s);

        public override string valueSerialized {
            get { return valueRaw.ToString(); }
        }

        public override string fullLine {
            get {
                string name_ = name;
                string val_ = valueSerialized.Replace("#", "@");
                while(name_.Length < 25) {
                    name_ += " ";
                }

                while(val_.Length < 25) {
                    val_ += " ";
                }

                if(!shouldWeSaveFn()) {
                    return "# " + name_ + " = " + val_ + " # " + comment;
                } else {
                    return name_ + " = " + val_ + " # " + comment;
                }
            }
        }

        public override void ResetValueToDefault() {
            valueRaw = defaultValueRaw;
        }
    }

    public class SettingsComment : SettingsFileLineB<string> {
        public SettingsComment(string _comment, bool _paddingAbove = true) : base("", "", _comment) {
            paddingAbove = _paddingAbove;
        }

        private bool paddingAbove = true;

        protected override string Parse(string s) {
            return null;
        }

        public override string fullLine {
            get { return (paddingAbove ? "\n# " : "# ") + comment.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine + "# "); }
        }

        public override string valueSerialized {
            get { return null; }
        }

        public override bool isComment {
            get { return true; }
        }

        public override bool wasRead {
            get { return true; }
        }
    }

    [Serializable]
    public class SettingsString : SettingsFileLineB<string> {
        public SettingsString(string _name, string def, string _comment) : base(_name, def, _comment) {
        }

        protected override string Parse(string s) {
            return s;
        }

        public static implicit operator string(SettingsString v) {
            return v.valueRaw;
        }
    }

    [Serializable]
    public class SettingsEnum<T> : SettingsFileLineB<T> where T : struct, IConvertible {
        public SettingsEnum(string _name, T def, string _comment) : base(_name, def, _comment) {
        }

        protected override T Parse(string s) {
            T cal;
            if (!Enum.TryParse<T>(s, out cal)) {
                cal = default;
                throw new Exception("value1 is not valid member of enumeration MyEnum");
            }

            return cal;
        }

        public static implicit operator T(SettingsEnum<T> v) {
            return v.valueRaw;
        }
    }

    [Serializable]
    public class SettingsInt : SettingsFileLineB<int> {
        public SettingsInt(string _name, int def, string _comment) : base(_name, def, _comment) {
        }

        protected override int Parse(string s) {
            return int.Parse(s);
        }

        public static implicit operator int(SettingsInt v) {
            return v.valueRaw;
        }
    }

    [Serializable]
    public class SettingsBool : SettingsFileLineB<bool> {
        public SettingsBool(string _name, bool def, string _comment) : base(_name, def, _comment) {
        }

        protected override bool Parse(string s) {
            if(s == "true" || s == "1" || s == "yes") {
                return true;
            } else if(s == "false" || s == "0" || s == "no") {
                return false;
            } else {
                throw new Exception("ERROR " + s + " not parsed as bool true or false");
            }
        }

        public static implicit operator bool(SettingsBool v) {
            return v.valueRaw;
        }

        public override string valueSerialized {
            get { return this ? "true" : "false"; }
        }
    }

    [Serializable]
    public class SettingsFloat : SettingsFileLineB<float> {
        public SettingsFloat(string _name, float def, string _comment) : base(_name, def, _comment) {
        }

        protected override float Parse(string s) {
            return float.Parse(s);
        }

        public static implicit operator float(SettingsFloat v) {
            return v.valueRaw;
        }

        public float percentTo01 {
            get { return valueRaw * .01f; }
            set { valueRaw = value * 100; }
        }
    }

    [Serializable]
    public class SettingsVector3 : SettingsFileLineB<Vector3> {
        public SettingsVector3(string _name, Vector3 def, string _comment) : base(_name, def, _comment) {
        }

        protected override Vector3 Parse(string s) {
            string[] split = s.Trim().Split(',');
            if(split.Length == 3) {
                return new Vector3(
                    float.Parse(split[0].Trim()),
                    float.Parse(split[1].Trim()),
                    float.Parse(split[2].Trim())
                );
            } else {
                throw new Exception("Could not parse " + s + " as vector3 should be in the format 1.2, 2.3, 4.5");
            }
        }

        public static implicit operator Vector3(SettingsVector3 v) {
            return v.valueRaw;
        }

        public override string valueSerialized {
            get { return valueRaw.x.ToString("R") + ", " + valueRaw.y.ToString("R") + ", " + valueRaw.z.ToString("R"); }
        }
    }

    #endregion

    #region PostProcessing

    public abstract class PostProcessing_Helper {
        protected abstract void ApplyPostProcessingAutomatic(CubeFaceCamera cam, SuperCubeMapper cubeCapture);
        protected abstract void ApplyPostProcessingHinted(CubeFaceCamera cam, SuperCubeMapper cubeCapture);

        public void ApplyPostProcessing(CubeFaceCamera cam, SuperCubeMapper cubeCapture) {
            if(cubeCapture.settingsFromFile.postprocesssing_enabled) {
                switch(cubeCapture.settings.postProcessType) {
                    case Settings.PostProcessType.AUTOMATIC:
                        ApplyPostProcessingAutomatic(cam, cubeCapture);
                        break;
                    case Settings.PostProcessType.HINTED:
                        ApplyPostProcessingHinted(cam, cubeCapture);
                        break;
                    case Settings.PostProcessType.MANUAL:
                        break;
                    default:
                        Debug.Log(cubeCapture.settings.postProcessType + " mode not handled");
                        break;
                }
            }
        }

        public static void CheckForErrors() {
#if UNITY_POST_PROCESSING_STACK_V1
#if UNITY_POST_PROCESSING_STACK_V2
            Debug.LogWarning ("You shouldn't enable both UNITY_POST_PROCESSING_STACK_V1 and UNITY_POST_PROCESSING_STACK_V2. Remove one of the two preprocessor defines.  We reccommend using UNITY_POST_PROCESSING_STACK_V2, but if you want to use UNITY_POST_PROCESSING_STACK_V1 BUT UNITY_POST_PROCESSING_STACK_V2 macro keeps coming back, it is likely because the macro is automatically added by the package.  Remove PROCESSING_STACK_V2 using the package manager if you want to use V1. or just comment out these lines if you want to use them both.");
#endif
#endif

#if UNITY_POST_PROCESSING_STACK_V1
            if (PostProcessing_Custom.IS_ENABLED) {
                Debug.LogWarning ("You shouldn't enable both UNITY_POST_PROCESSING_STACK_V1 and PostProcessing_Custom. Remove either the custom effect or UNITY_POST_PROCESSING_STACK_V2  preprocessor define.  or just comment out these lines if you want to use them both.");
            }
#endif
#if UNITY_POST_PROCESSING_STACK_V2
            if (PostProcessing_Custom.IS_ENABLED) {
                Debug.LogWarning("You shouldn't enable both UNITY_POST_PROCESSING_STACK_V2 and PostProcessing_Custom. Remove either the custom effect or UNITY_POST_PROCESSING_STACK_V2  preprocessor define.  if you want to use UNITY_POST_PROCESSING_CUSTOM BUT UNITY_POST_PROCESSING_STACK_V2 macro keeps coming back, it is likely because the macro is automatically added by the package.  Remove PROCESSING_STACK_V2 using the package manager if you want to use a custom stack.. or just comment out these lines if you want to use them both.");
            }
#endif
        }
    }

    #endregion

    #region TextureSharing

    public static class SpoutExtensions {
        public static GameObject AddKlackSpoutSender(string textureName, Transform transformParent, RenderTexture texturetosend, HideFlags hideflags, Action<string> onError) {
            return AddTextureSender(textureName,
                transformParent,
                texturetosend,
                hideflags,
                onError,
                "Klak.Spout.SpoutSender, Klak.Spout",
                "Klak.Spout.SpoutSender",
                "Spout is missing or error adding it.  Download from https://github.com/keijiro/KlakSpout",
                "sourceTexture",
                "Klak.Spout.Runtime"
            );
        }

        public static GameObject AddKlackSyphon(string textureName, Transform transformParent, RenderTexture texturetosend, HideFlags hideflags, Action<string> onError) {
            return AddTextureSender(textureName,
                transformParent,
                texturetosend,
                hideflags,
                onError,
                "Klak.Syphon.SyphonServer, Klak.Syphon",
                "Klak.Syphon.SyphonServer",
                "Syphon is missing or error adding it.  Download from https://github.com/keijiro/KlakSyphon",
                "sourceTexture",
                "Klak.Syphon.Runtime"
            );
        }

        public static GameObject AddKlackNDI(string textureName, Transform transformParent, RenderTexture texturetosend, HideFlags hideflags, Action<string> onError) {
            return AddTextureSender(textureName,
                transformParent,
                texturetosend,
                hideflags,
                onError,
                "Klak.Ndi.NdiSender, Klak.Ndi",
                "Klak.Ndi.NdiSender",
                "NDI is missing or error adding it.  Download from https://github.com/keijiro/KlakNDI",
                "sourceTexture",
                "Klak.Ndi.Runtime"
            );
        }

        private static GameObject AddTextureSender(
            string textureName,
            Transform transformParent,
            RenderTexture textureToSend,
            HideFlags hideFlags,
            Action<string> onError,
            string typeName,
            string typeName2,
            string errorMessage,
            string renderTextureVaribleName,
            string assemblyName
        ) {
            try {
                Type textureSenderType = Type.GetType(typeName); //= "Klak.Syphon.SyphonServer, Klak.Syphon"
                if(textureSenderType == null) {
                    textureSenderType = Type.GetType(typeName2); //= "Klak.Syphon.SyphonServer"
                }
                if(textureSenderType == null) {
                    textureSenderType = Type.GetType(typeName + ", " + assemblyName);
                }
                if(textureSenderType == null) {
                    textureSenderType = Type.GetType(typeName2 + ", " + assemblyName);
                }
               
                if(textureSenderType != null) {
                    GameObject senderGameObject = new GameObject(textureName);
                    senderGameObject.transform.parent = transformParent;
                    Component senderComponent = senderGameObject.AddComponent(textureSenderType); //
                    PropertyInfo sourceTextureFieldInfo = textureSenderType.GetProperty(renderTextureVaribleName); //= "sourceTexture"
                    sourceTextureFieldInfo.SetValue(senderComponent, textureToSend);
                    senderGameObject.hideFlags = hideFlags;
                    return senderGameObject;
                }

                onError(errorMessage); //= "Syphon is missing or error adding it.  Download from https://github.com/keijiro/KlakSyphon"
            } catch(Exception e) {
                Debug.LogError(errorMessage + " : " + e.Message);
                onError(errorMessage + " : " + e.Message);
            }

            return null;
        }

        public static bool IsSyphonPlatform() {
            return Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor;
        }

        public static bool IsSpoutPlatform() {
            return Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;
        }

        public static bool IsNDIPlatform() {
            return Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor;
        }
    }

    #endregion

    public static class Vector3Extensions {
        public static Vector3 PitchYawRoll_to_Euler(this Vector3 v) {
            // pitch and roll are inverted compared to unity's axis.
            return new Vector3(-v.x, v.y, -v.z);
        }
    }

    public static class TransformExtensions {
        public static void SnapTo(this Transform t, Transform parent) {
            t.parent = parent;
            t.localPosition = Vector3.zero;
            t.localEulerAngles = Vector3.zero;
            t.localScale = Vector3.one;
        }
    }

    public static class CultureExtensions {
        private static Stack<System.Globalization.CultureInfo> cultureStack = new Stack<System.Globalization.CultureInfo>();

        public static void PushCulture(string type = "en") {
            cultureStack.Push(System.Threading.Thread.CurrentThread.CurrentCulture);
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(type);
        }

        public static void PopCulture() {
            if(cultureStack.Count > 0) {
                System.Threading.Thread.CurrentThread.CurrentCulture = cultureStack.Pop();
            }
        }
    }

    public enum Stereo3DMode {
        MONO = 0,
        STEREO_EYE_SEPARATED
    }

    public enum STEREO_EYE {
        BOTH = 0,
        LEFT = 1,
        RIGHT = 2
    }

    public enum TEXTUREFORMAT {
        CUBEMAP = 0,
        EQUIRECTANGULAR = 1,
        FISHEYE = 2,
        OMNITY = 3
    }

    public enum POSTPROCESSINGTWEAKS {
        AUTOMATIC,
        WARN,
        DISABLED_QUIET
    }
}
