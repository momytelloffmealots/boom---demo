using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class LoadingView : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    public float fadeTime = 0.3f; // Thời gian làm mờ

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ShowLoading(float duration, Action onLoadingComplete)
    {
        gameObject.SetActive(true);
        StartCoroutine(LoadingRoutine(duration, onLoadingComplete));
    }

    private IEnumerator LoadingRoutine(float duration, Action onLoadingComplete)
    {
        // 1. Mờ ảo hiện ra (Từ Alpha 0 lên 1) che kín toàn bộ màn hình
        yield return StartCoroutine(FadeAlpha(0f, 1f, fadeTime));

        // ---------------------------------------------------------
        // 2. ✨ ĐIỂM QUAN TRỌNG NHẤT ✨
        // Ngay lúc màn hình đang bị che kín 100%, ta âm thầm gọi Controller 
        // để nó dọn dẹp Menu và bật môi trường 3D lên phía sau lưng.
        onLoadingComplete?.Invoke();
        // ---------------------------------------------------------

        // 3. Đứng im chờ nốt phần thời gian load giả lập
        float waitTime = duration - (fadeTime * 2);
        if (waitTime > 0) yield return new WaitForSeconds(waitTime);

        // 4. Mờ dần biến mất (Từ Alpha 1 về 0)
        // Lúc này khi màn hình sáng dần lên, cảnh 3D đã sẵn sàng bày ra trước mắt!
        yield return StartCoroutine(FadeAlpha(1f, 0f, fadeTime));

        // 5. Xong việc, tắt hoàn toàn LoadingView
        gameObject.SetActive(false);
    }

    private IEnumerator FadeAlpha(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
    }

    public void HideLoading()
    {
        gameObject.SetActive(false);
    }
}