using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class SplashView : MonoBehaviour
{
    [Header("UI Elements")]
    public CanvasGroup imgPtit;
    public CanvasGroup imgSmashItOut;

    [Header("Cài đặt Animation")]
    public float fadeDuration = 0.5f;

    private CanvasGroup mainCanvasGroup;

    private void Awake()
    {
        mainCanvasGroup = GetComponent<CanvasGroup>();

        // 1. SỬA LỖI: imgFlow phải bật SẴN 100% ngay từ Frame đầu tiên để che kín màn hình Home
        if (imgPtit != null)
        {
            imgPtit.alpha = 1f;
            imgPtit.gameObject.SetActive(true);
        }

        // imgSmashItOut thì giấu đi
        if (imgSmashItOut != null)
        {
            imgSmashItOut.alpha = 0f;
            imgSmashItOut.gameObject.SetActive(false);
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
        if (imgSmashItOut != null)
        {
            imgSmashItOut.alpha = 0f;
            imgSmashItOut.gameObject.SetActive(true);

            // Đẩy SmashItOut lên lớp trên cùng để đảm bảo nó đè lên Flow
            imgSmashItOut.transform.SetAsLastSibling();
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            // Chỉ tăng độ rõ của SmashItOut lên, GIỮ NGUYÊN Flow ở dưới làm nền che chắn
            if (imgSmashItOut != null)
            {
                imgSmashItOut.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            }
            yield return null;
        }

        // Đảm bảo SmashItOut đã hiện rõ 100%
        if (imgSmashItOut != null) imgSmashItOut.alpha = 1f;

        // Tới lúc này, SmashItOut đã che kín mít màn hình. Ta mới âm thầm tắt Flow đi
        if (imgPtit != null)
        {
            imgPtit.alpha = 0f;
            imgPtit.gameObject.SetActive(false);
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