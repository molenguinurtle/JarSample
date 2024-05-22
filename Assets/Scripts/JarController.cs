using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.Serialization;

public class JarController : MonoBehaviour
{
    #region Serializable Objects for Json
    
    [Serializable]
    public class JarJsonObj
    {
        public string character;
        public string animation;
        public JarEvent jarEvent;
    }
    
    [Serializable]
    public struct JarEvent
    {
        public EventType eventType;
        public bool startRecording;
        public bool doMove;
        public bool doRotate;
        public MoveParams moveParams;
        public RotateParams rotateParams;
    }
    
    [Serializable]
    public struct MoveParams
    {
        public float movePosX;
        public float movePosY;
        public float movePosZ;
    }
    
    [Serializable]
    public struct RotateParams
    {
        public float rotX;
        public float rotY;
        public float rotZ;
    }
    
    [Serializable]
    public enum EventType
    {
        IgnoreEvent,
        MoveEvent,
        RecordEvent
    }
    #endregion

    #region Variables
    
    public string jsonFilePath = "";
    public string recordingOutputPath = "";
    [SerializeField] private Camera _theCamera;
    [SerializeField] private Animator _bananaManController, _robotKyleController;
    [SerializeField] private Transform _bananaCamPoint, _robotCamPoint, _bananaTransform, _robotTransform;
    private Dictionary<string, string> _currentlyPlayingAnimations = new Dictionary<string, string>();
    private string _currentJson;
    private RecorderController _theRecorder;
    private const string ROBOT_KYLE_STRING = "robot_kyle";
    private const string BANANA_MAN_STRING = "banana_man";
    
    #endregion

    void Start()
    {
        if (!string.IsNullOrEmpty(jsonFilePath) && _theCamera != null)
        {
            StartCoroutine(CheckForNewJson());
        }
        else
        {
            Debug.LogWarning($"Either filePath or camera isn't set. FilePath: {jsonFilePath}, _theCamera: {_theCamera}");
        }
    }

    /// <summary>
    /// Ingests any new json file found at 'jsonFilePath' every 1 second. If a new file is found, saves the text, deletes the file, and sends the text to the JsonHandler.
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckForNewJson()
    {
        while (Directory.Exists(jsonFilePath))
        {
            DirectoryInfo d = new DirectoryInfo(jsonFilePath);

            foreach (var file in d.GetFiles("*.json"))
            {
                _currentJson = File.ReadAllText(file.ToString());
                File.Delete(file.ToString());
                if (!string.IsNullOrEmpty(_currentJson))
                {
                    JsonHandler(_currentJson);
                    break;
                }
                Debug.LogWarning($"The File found is empty. FilePath: {file}");
            }
            yield return new WaitForSeconds(1f);
        }
        Debug.LogWarning($"Your filePath Directory doesn't exist! Please check your 'filePath' in the Inspector. FilePath: {jsonFilePath}");
        yield return null;
    }

    /// <summary>
    /// Takes a given json string and tries to deserialize it to a JarJsonObj. If conversion is successful, runs behaviour on scene objects based on given JarJsonObj parameters.
    /// Otherwise, it logs a warning about not being able to deserialize given json string
    /// </summary>
    /// <param name="theJson">The json string used for deserialization into a JarJsonObj</param>
    private void JsonHandler(string theJson)
    {
        JarJsonObj myJarObj;
        try
        {
             myJarObj = JsonUtility.FromJson<JarJsonObj>(theJson);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"The JSON threw an exception when trying to deserialize into a JarJsonObj. Please verify the formatting of JSON. " +
                             $"The Json: {theJson}; The Exception: {e.Message}");
            return;
        }

        if (myJarObj == null)
        {
            Debug.LogWarning($"The JSON did not deserialize into a JarJsonObj. Please verify the formatting of JSON. The Json: {theJson}");
            return;
        }
        
        switch (myJarObj.character)
        {
            case ROBOT_KYLE_STRING:
                JarJsonEventHandler(myJarObj, _robotCamPoint, _robotTransform, _robotKyleController, ROBOT_KYLE_STRING);
                break;
            case BANANA_MAN_STRING:
                JarJsonEventHandler(myJarObj, _bananaCamPoint, _bananaTransform, _bananaManController, BANANA_MAN_STRING);
                break;
            default:
                Debug.LogWarning($"The character name in the JarJsonObj does not match any character in the scene! Camera will not change and animations" +
                                 $" will not play. Character Name: {myJarObj.character}");
                break;
        }
    }



    #region Helper Methods

    /// <summary>
    /// This method takes a given JarJsonObj and runs its given behaviours(events) on scene objects
    /// </summary>
    /// <param name="theJarJsonObj">The JarJsonObj that holds the event info.</param>
    /// <param name="camPoint">The Transform the camera should use to focus on a given character.</param>
    /// <param name="charTransform">The Transform of a given character. Used for movement.</param>
    /// <param name="charAnim">The Animator component of a given character. Used for animation control.</param>
    /// <param name="charName">The name of a given character. Used for animation tracking.</param>
    private void JarJsonEventHandler(JarJsonObj theJarJsonObj, Transform camPoint, Transform charTransform,
        Animator charAnim, string charName)
    {
        var myEvent = theJarJsonObj.jarEvent;
        StopCurrentlyPlayingAnimation(charName, charAnim, theJarJsonObj.animation);
        if (myEvent.eventType == EventType.MoveEvent)
        {
            MoveCharacter(charTransform, myEvent);
        }
        CameraHandler(camPoint);
        charAnim.SetBool(theJarJsonObj.animation, true);
        TrackCurrentlyPlayingAnimation(charName, theJarJsonObj.animation);
#if UNITY_EDITOR
        if (myEvent.eventType == EventType.RecordEvent)
        {
            RecordEventHandler(theJarJsonObj.jarEvent);
        }
#endif
    }
    
    /// <summary>
    /// Starts or Stops recording based on the given JarEvent
    /// </summary>
    /// <param name="jarEvent">The object that determines if we Start or Stop recording</param>
    private void RecordEventHandler(JarEvent jarEvent)
    {
        //If recorderController is null, we need to set one up
        if (_theRecorder == null)
        {
            SetupRecorder();
        }

        if (_theRecorder == null) return;//If this happens, we have a problem.
        if (_theRecorder.IsRecording())
        {
            if (jarEvent.startRecording)
            {
                Debug.Log($"The recorder is already recording and a new jarEvent wants to Start Recording. Ignoring jarEvent. Event: {_currentJson}");
            }
            else
            {
                _theRecorder.StopRecording();
            }
        }
        else
        {
            if (jarEvent.startRecording)
            {
                _theRecorder.PrepareRecording();
                _theRecorder.StartRecording();
            }
            else
            {
                Debug.Log($"The recorder is already stopped and a new jarEvent wants to Stop Recording. Ignoring jarEvent. Event: {_currentJson}");
            }
        }
    }

    /// <summary>
    /// Sets up a RecorderController we can use to record gameplay in the Editor
    /// </summary>
    private void SetupRecorder()
    {
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        _theRecorder = new RecorderController(controllerSettings);
     
        var videoRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        videoRecorder.name = "My Jar Recorder";
        videoRecorder.Enabled = true;
        videoRecorder.EncoderSettings = new CoreEncoderSettings()
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
        };
     
        videoRecorder.ImageInputSettings = new CameraInputSettings()
        {
            Source = ImageSource.MainCamera,
            OutputWidth = 1280,
            OutputHeight = 720
        };
     
        videoRecorder.AudioInputSettings.PreserveAudio = true;
        if (Directory.Exists(recordingOutputPath))
        {
            videoRecorder.OutputFile = recordingOutputPath + "JarRecording_" + DateTime.Now.ToString("s");
        }
        
        controllerSettings.AddRecorderSettings(videoRecorder);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 30;
     
        RecorderOptions.VerboseMode = false;
    }
    
    /// <summary>
    /// Modifies the given Transform based on the parameters in a given JarEvent
    /// </summary>
    /// <param name="charTransform">The Transform to modify</param>
    /// <param name="jarEvent">The JarEvent that holds the Transform modification parameters</param>
    private void MoveCharacter(Transform charTransform, JarEvent jarEvent)
    {
        if (jarEvent.doMove)
        {
            charTransform.position = new Vector3(jarEvent.moveParams.movePosX, jarEvent.moveParams.movePosY, jarEvent.moveParams.movePosZ);
        }
        
        if (jarEvent.doRotate)
        {
            var newRotation = charTransform.rotation;
            newRotation.eulerAngles = new Vector3(jarEvent.rotateParams.rotX, jarEvent.rotateParams.rotY, jarEvent.rotateParams.rotZ);
            charTransform.rotation = newRotation;
        }
    }
    
    /// <summary>
    /// Moves the Camera to the given Transform and aligns it so its local forward Vector is the same as the given Transform.
    /// </summary>
    /// <param name="cameraFocus">The Transform we're aligning the camera with.</param>
    private void CameraHandler(Transform cameraFocus)
    {
        var movPosition = cameraFocus.position;
        Transform camTransform = _theCamera.transform;
        camTransform.position = new Vector3(movPosition.x, movPosition.y,
            movPosition.z);
        camTransform.parent = cameraFocus;
        camTransform.SetLocalPositionAndRotation(new Vector3(0,0), new Quaternion(0,0,0,0));
        camTransform.parent = null;
    }

    /// <summary>
    /// If the given character is currenly playing an animation, and the next animation we want to trigger isn't the same, this stops the current animation playing.
    /// </summary>
    /// <param name="characterName"></param>
    /// <param name="theAnimator"></param>
    /// <param name="tempNextBool"></param>
    private void StopCurrentlyPlayingAnimation(string characterName, Animator theAnimator, string tempNextBool)
    {
        if (!_currentlyPlayingAnimations.TryGetValue(characterName, out var theBool) || theBool == tempNextBool) return;
        if (!string.IsNullOrEmpty(theBool))
        {
            theAnimator.SetBool(theBool, false);
            _currentlyPlayingAnimations[characterName] = "";
        }
    }

    /// <summary>
    /// Tracks which animation is currently playing on given character in the scene via a dictionary.
    /// </summary>
    /// <param name="characterName"></param>
    /// <param name="animationName"></param>
    private void TrackCurrentlyPlayingAnimation(string characterName, string animationName)
    {
        if (!_currentlyPlayingAnimations.TryAdd(characterName, animationName))
        {
            _currentlyPlayingAnimations[characterName] = animationName;
        }
    }
    
    #endregion

}
