using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject); // prevents duplicates
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // keeps music between scenes
    }
}
