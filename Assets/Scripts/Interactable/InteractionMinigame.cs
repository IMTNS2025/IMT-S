using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InteractionMinigame : MonoBehaviour, IInteractable
{
    [SerializeField] private int minigameSceneIndexToLoad;
    private bool _isTransitioning;

    public void Interact()
    {
        if (_isTransitioning) return;
        StartCoroutine(SwitchSceneKeepState());
    }

    private void SetSceneRootActive(Scene scene, bool active)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            roots[i].SetActive(active);
        }
    }

    private IEnumerator SwitchSceneKeepState()
    {
        var currentScene = SceneManager.GetActiveScene();
        if (currentScene.buildIndex == minigameSceneIndexToLoad)
        {
            Debug.LogWarning("Target scene is already active. You are trying to load the same scene");
            yield break;
        }

        _isTransitioning = true;

        var targetScene = SceneManager.GetSceneByBuildIndex(minigameSceneIndexToLoad);
        if (!targetScene.IsValid() || !targetScene.isLoaded)
        {
            var loadOp = SceneManager.LoadSceneAsync(minigameSceneIndexToLoad, LoadSceneMode.Additive);
            if (loadOp == null)
            {
                _isTransitioning = false;
                yield break;
            }

            while (!loadOp.isDone)
                yield return null;

            targetScene = SceneManager.GetSceneByBuildIndex(minigameSceneIndexToLoad);
            if (!targetScene.IsValid() || !targetScene.isLoaded)
            {
                _isTransitioning = false;
                yield break;
            }
        }

        SceneManager.SetActiveScene(targetScene);
        SetSceneRootActive(targetScene, true);

        SetSceneRootActive(currentScene, false);

        _isTransitioning = false;
    }
}
