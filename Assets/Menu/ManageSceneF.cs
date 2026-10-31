using UnityEngine;
using UnityEngine.SceneManagement;

public class ManageSceneF : MonoBehaviour
{
    public void Tranzit(int scene)
    {
        SceneManager.LoadScene(scene);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
