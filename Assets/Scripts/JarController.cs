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
    private const float CAMERA_OFFSET_Z = -3f;
    private const float CAMERA_OFFSET_X = -10f;
    private const float CAMERA_OFFSET_Y = 5f;
    [SerializeField] private Camera _theCamera; //Set in Inspector
    [SerializeField] private Animator _bananaManController, _robotKyleController;
    [SerializeField] private Transform _bananaTransform, _robotTransform;
    private string _currentJson;
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
            // Continuously monitors a local folder for new json files [DONE]
            // Ingests said json files and deletes after ingestion [DONE]
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
        // Camera focuses on character listed in the JSON
        // Triggers animation state changes for character listed in the JSON between the two animations you chose[DONE]

        //_bananaManController.SetTrigger();
        //_bananaManController.ResetTrigger();
        JarJsonObj myJarObj = JsonUtility.FromJson<JarJsonObj>(theJson);

        if (myJarObj == null)
        {
            Debug.LogWarning($"The JSON did not deserialize into a JarJsonObj. Please verify the formatting of JSON. The Json: {theJson}");
            return;
        }
        switch (myJarObj.character)
        {
            case "robot_kyle":
                CameraHandler(_robotTransform);
                _robotKyleController.SetTrigger(myJarObj.animation);
                break;
            case "banana_man":
                CameraHandler(_bananaTransform);
                _bananaManController.SetTrigger(myJarObj.animation);
                break;
            default:
                Debug.LogWarning($"The character name in the JarJsonObj does not match any character in the scene! Camera will not change and animations" +
                                 $" will not play. Character Name: {myJarObj.character}");
                break;
        }
    }

    private void CameraHandler(Transform cameraFocus)
    {
         var focusPosition = cameraFocus.position;
        // //var focusPosition = cameraFocus.localPosition;
        Transform camTransform = _theCamera.transform;
        // Vector3 camLocalPos = camTransform.localPosition;
        _theCamera.transform.LookAt(camTransform.position + cameraFocus.transform.rotation * -Vector3.forward,
            camTransform.rotation * Vector3.up);
        camTransform.position = new Vector3(focusPosition.x + CAMERA_OFFSET_X, focusPosition.y+ CAMERA_OFFSET_Y,
            focusPosition.z + CAMERA_OFFSET_Z);
        _theCamera.transform.LookAt(cameraFocus.localPosition);

        // camLocalPos = new Vector3(camTransform.localPosition.x, camLocalPos.y + CAMERA_OFFSET_Y,
        //     camLocalPos.z + CAMERA_OFFSET_Z);
        // camTransform.localPosition = camLocalPos;

    }

}
