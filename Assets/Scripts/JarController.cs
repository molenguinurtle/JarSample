using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Serialization;

public class JarController : MonoBehaviour
{
    // Add an event for moving characters around the scene
    // Add an event for starting/stopping a recording of the camera view to write to a local mp4 clip -
    // Configure mp4 output dir within inspector
    // NOTE: Local Folder should be a public variable in Editor [DONE]
    public string jsonFilePath = "";
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
            // Camera focuses on character listed in the JSON
            // Triggers animation state changes for character listed in the JSON between the two animations you chose 
            DirectoryInfo d = new DirectoryInfo(jsonFilePath);

            foreach (var file in d.GetFiles("*.json"))
            {
                _currentJson = File.ReadAllText(file.ToString());
                File.Delete(file.ToString());
                //Do something with the currentJSON (JSON Handler)
                //_bananaManController.SetTrigger();
                //_bananaManController.ResetTrigger();

            }

            yield return new WaitForSeconds(1f);
        }
        Debug.LogWarning($"Your filePath Directory doesn't exist! Please check your 'filePath' in the Inspector. FilePath: {jsonFilePath}");
        yield return null;
    }
    
}
