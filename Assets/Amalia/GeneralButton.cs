using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GeneralButton : MonoBehaviour
{
    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }


    public void GoToScreen(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
