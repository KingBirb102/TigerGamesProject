using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class WinIdea : MonoBehaviour
{
    public float distToWin = 1f;
    public string winSceneName = "WinScene";
    public Transform player;

    void Start()
    {
        player = GameObject.Find("Player").transform;
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, player.position) <= distToWin)
        {
            print("asdfasdfasdfafsdafsd");
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(winSceneName);
            }
        }
    }
}