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
    
    [System.Serializable]
    public class JarJsonObj
    {
        public string character;
        public string animation;
        public JarEvent jarEvent;
    }
    
    [System.Serializable]
    public struct JarEvent
    {
        public EventType eventType;
        public bool startRecording;
        public float movePosX;
        public float movePosY;
        public float movePosZ;
    }
    
    [System.Serializable]
    public enum EventType
    {
        IgnoreEvent,
        MoveEvent,
        RecordEvent
    }

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
        // Add an event for moving characters around the scene [DONE]
        // Add an event for starting/stopping a recording of the camera view to write to a local mp4 clip [DONE]
        // Configure mp4 output dir within inspector [DONE]
        
        //C'est la vie. Todo: We have a lot of repeated logic below. See if we can't consolidate these calls. Either use conditional (? :) or something else. Probably...
        //  Todo 2: ...easier to maintain in the long run as well. Maybe group all the separate method calls under one big method call that just passes in the proper
        //   Todo 3: ...params for all the things being called. (i.e., JarJsonEventHandler(transform blah, transform blahCam, string blahStr, etc.)
        switch (myJarObj.character)
        {
            case ROBOT_KYLE_STRING:
                StopCurrentlyPlayingAnimation(ROBOT_KYLE_STRING, _robotKyleController);
                if (myJarObj.jarEvent.eventType == EventType.MoveEvent)
                {
                    MoveCharacter(_robotTransform, myJarObj.jarEvent);
                }
                CameraHandler(_robotCamPoint);
                _robotKyleController.SetBool(myJarObj.animation, true);
                TrackCurrentlyPlayingAnimation(ROBOT_KYLE_STRING, myJarObj.animation);
                if (myJarObj.jarEvent.eventType == EventType.RecordEvent)
                {
                    RecordEventHandler(myJarObj.jarEvent);
                }
                break;
            case BANANA_MAN_STRING:
                StopCurrentlyPlayingAnimation(BANANA_MAN_STRING, _bananaManController);
                if (myJarObj.jarEvent.eventType == EventType.MoveEvent)
                {
                    MoveCharacter(_bananaTransform, myJarObj.jarEvent);
                }
                CameraHandler(_bananaCamPoint);
                _bananaManController.SetBool(myJarObj.animation, true);
                TrackCurrentlyPlayingAnimation(BANANA_MAN_STRING, myJarObj.animation);
                if (myJarObj.jarEvent.eventType == EventType.RecordEvent)
                {
                    RecordEventHandler(myJarObj.jarEvent);
                }
                break;
            default:
                Debug.LogWarning($"The character name in the JarJsonObj does not match any character in the scene! Camera will not change and animations" +
                                 $" will not play. Character Name: {myJarObj.character}");
                break;
        }
    }



    #region Helper Methods

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
                Debug.Log($"The recorder is already recording and a new jarEvent wants to Start Recording. Ignoring jarEvent. Event: {jarEvent.ToString()}");
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
                Debug.Log($"The recorder is already stopped and a new jarEvent wants to Stop Recording. Ignoring jarEvent. Event: {jarEvent.ToString()}");
            }
        }
    }

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
        videoRecorder.OutputFile = recordingOutputPath + "JarRecording_" + DateTime.Now.ToString("s");
        
        controllerSettings.AddRecorderSettings(videoRecorder);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 30;
     
        RecorderOptions.VerboseMode = false;
    }
    private void MoveCharacter(Transform charTransform, JarEvent moveParams)
    {
        //C'est la vie. Maybe add Rotation to this as well. Just add 3 more fields to JarEvent
        charTransform.position = new Vector3(moveParams.movePosX, moveParams.movePosY, moveParams.movePosZ);
    }
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

    private void StopCurrentlyPlayingAnimation(string characterName, Animator theAnimator)
    {
        if (!_currentlyPlayingAnimations.TryGetValue(characterName, out var theTrigger)) return;
        if (!string.IsNullOrEmpty(theTrigger))
        {
            theAnimator.SetBool(theTrigger, false);
            _currentlyPlayingAnimations[characterName] = "";
        }
    }

    private void TrackCurrentlyPlayingAnimation(string characterName, string animationName)
    {
        if (!_currentlyPlayingAnimations.TryAdd(characterName, animationName))
        {
            _currentlyPlayingAnimations[characterName] = animationName;
        }
    }
    
    #endregion

}
