using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoad : MonoBehaviour
{
    public void LoadScene(int index) { 
        SceneManager.LoadScene(index);
    }
}
