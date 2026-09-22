using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class InfiniteAmmoUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject infinitePanel;       
    public TextMeshProUGUI txtTitle1; // Tiêu đề trên (Ví dụ: Infinite Ball)
    public TextMeshProUGUI txtTitle2; // Tiêu đề dưới để đếm giây
    public Slider fillBar;            // Đã fix chuẩn kiểu Slider

    private Coroutine countdownCoroutine;

    private void OnEnable()
    {
        SimpleCannon.OnInfiniteAmmoStarted += ShowUI;
        SimpleCannon.OnInfiniteAmmoEnded += HideUI;
    }

    private void OnDisable()
    {
        SimpleCannon.OnInfiniteAmmoStarted -= ShowUI;
        SimpleCannon.OnInfiniteAmmoEnded -= HideUI;
    }

    private void Start()
    {
        if (infinitePanel != null) infinitePanel.SetActive(false);
    }

    private void ShowUI(float duration)
    {
        if (infinitePanel != null) infinitePanel.SetActive(true);

        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        countdownCoroutine = StartCoroutine(CountdownRoutine(duration));
    }

    private void HideUI()
    {
        if (infinitePanel != null) infinitePanel.SetActive(false);
        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
    }

    private IEnumerator CountdownRoutine(float duration)
    {
        float timer = duration;

        if (fillBar != null) fillBar.maxValue = 1f;

        while (timer > 0)
        {
            timer -= Time.deltaTime;

            // 1. Cập nhật thanh Slider trượt mượt mà
            if (fillBar != null)
            {
                fillBar.value = timer / duration;
            }

            // 2. Cập nhật dòng Text mô tả phía dưới
            if (txtTitle2 != null)
            {
                int secondsLeft = Mathf.CeilToInt(timer);
                // Giữ nguyên câu tiếng Anh của bạn và nối thêm số giây
                txtTitle2.text = $"Unlimited ball-handling time: {secondsLeft}s"; 
            }

            yield return null; 
        }
    }
}