using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class PgameLoading : MonoBehaviour
{
    [SerializeField] private CanvasGroup loadingElement;
    [SerializeField] private CanvasGroup LogoScreenElement;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        loadingElement.gameObject.SetActive(false);
        LogoScreenElement.gameObject.SetActive(true);
        Load().Forget();
    }
    public async UniTask Load()
    {
        await UniTask.Delay(300);
        var tcs = new UniTaskCompletionSource();
        DOTween.To(() => LogoScreenElement.alpha, x => LogoScreenElement.alpha = x, 0, 1f).OnComplete(() =>
        {
            loadingElement.gameObject.SetActive(true);
            LogoScreenElement.gameObject.SetActive(false);
            tcs.TrySetResult();
        });
        await tcs.Task;
        
        // Wait a small amount of time for the loading UI to appear before loading the next scene
        await UniTask.Delay(500);
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1);
    }
}
