using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class SplashView : MonoBehaviour
{
    [Header("UI Elements")]
    public CanvasGroup imgFlow;
    public CanvasGroup imgSmashFest;

    [Header("Cài đặt Animation")]
    public float fadeDuration = 0.5f;

    private CanvasGroup mainCanvasGroup;

    private void Awake()
    {
        mainCanvasGroup = GetComponent<CanvasGroup>();

        // 1. SỬA LỖI: imgFlow phải bật SẴN 100% ngay từ Frame đầu tiên để che kín màn hình Home
        if (imgFlow != null)
        {
            imgFlow.alpha = 1f;
            imgFlow.gameObject.SetActive(true);
        }

        // imgSmashFest thì giấu đi
        if (imgSmashFest != null)
        {
            imgSmashFest.alpha = 0f;
            imgSmashFest.gameObject.SetActive(false);
        }
    }

    public void ShowFlowLogo()
    {
        // Vì Flow đã hiện sẵn từ Awake, ta không cần làm mờ từ 0 lên nữa để tránh lộ nền
    }

    public void ShowSmashFestLogo()
    {
        // Kích hoạt hiệu ứng đè ảnh mượt mà
        StartCoroutine(TransitionToSmashFest());
    }

    public void HideSplash()
    {
        // Làm mờ toàn bộ tấm rèm Splash để lộ ra Menu Home phía sau
        StartCoroutine(FadeOutAndHide(mainCanvasGroup, fadeDuration));
    }

    // --- LOGIC CHUYỂN CẢNH MỚI ---
    private IEnumerator TransitionToSmashFest()
    {
        if (imgSmashFest != null)
        {
            imgSmashFest.alpha = 0f;
            imgSmashFest.gameObject.SetActive(true);

            // Đẩy SmashFest lên lớp trên cùng để đảm bảo nó đè lên Flow
            imgSmashFest.transform.SetAsLastSibling();
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            // Chỉ tăng độ rõ của SmashFest lên, GIỮ NGUYÊN Flow ở dưới làm nền che chắn
            if (imgSmashFest != null)
            {
                imgSmashFest.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            }
            yield return null;
        }

        // Đảm bảo SmashFest đã hiện rõ 100%
        if (imgSmashFest != null) imgSmashFest.alpha = 1f;

        // Tới lúc này, SmashFest đã che kín mít màn hình. Ta mới âm thầm tắt Flow đi
        if (imgFlow != null)
        {
            imgFlow.alpha = 0f;
            imgFlow.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeOutAndHide(CanvasGroup target, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            target.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        target.alpha = 0f;
        gameObject.SetActive(false);
    }
}