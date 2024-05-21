using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Serialization;

public class JarController : MonoBehaviour
{
    
    [System.Serializable]
    public class JarJsonObj
    {
        public string character;
        public string animation;
    }
    // Add an event for moving characters around the scene
    // Add an event for starting/stopping a recording of the camera view to write to a local mp4 clip -
    // Configure mp4 output dir within inspector
    // NOTE: Local Folder should be a public variable in Editor [DONE]
    public string jsonFilePath = "";
    [SerializeField] private Camera _theCamera;
    [SerializeField] private Animator _bananaManController, _robotKyleController;
    [SerializeField] private Transform _bananaTransform, _robotTransform;
    private Dictionary<string, string> _currentlyPlayingAnimations = new Dictionary<string, string>();
    private string _currentJson;
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
        JarJsonObj myJarObj = JsonUtility.FromJson<JarJsonObj>(theJson);

        if (myJarObj == null)
        {
            Debug.LogWarning($"The JSON did not deserialize into a JarJsonObj. Please verify the formatting of JSON. The Json: {theJson}");
            return;
        }
        switch (myJarObj.character)
        {
            case ROBOT_KYLE_STRING:
                StopCurrentlyPlayingAnimation(ROBOT_KYLE_STRING, _robotKyleController);
                CameraHandler(_robotTransform);
                _robotKyleController.SetBool(myJarObj.animation, true);
                TrackCurrentlyPlayingAnimation(ROBOT_KYLE_STRING, myJarObj.animation);
                break;
            case BANANA_MAN_STRING:
                StopCurrentlyPlayingAnimation(BANANA_MAN_STRING, _bananaManController);
                CameraHandler(_bananaTransform);
                _bananaManController.SetBool(myJarObj.animation, true);
                TrackCurrentlyPlayingAnimation(BANANA_MAN_STRING, myJarObj.animation);
                break;
            default:
                Debug.LogWarning($"The character name in the JarJsonObj does not match any character in the scene! Camera will not change and animations" +
                                 $" will not play. Character Name: {myJarObj.character}");
                break;
        }
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

}
