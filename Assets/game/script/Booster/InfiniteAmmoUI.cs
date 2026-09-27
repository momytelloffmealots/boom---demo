using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class InfiniteAmmoUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject infinitePanel;
    public TextMeshProUGUI txtTitle1;
    public TextMeshProUGUI txtTitle2;
    public Slider fillBar;

    private Coroutine countdownCoroutine;
    private float totalDuration;

    private void OnEnable()
    {
        SimpleCannon.OnInfiniteAmmoArmed += ShowArmedUI;
        SimpleCannon.OnInfiniteAmmoCanceled += HideUI;
        SimpleCannon.OnInfiniteAmmoStarted += StartCountdown;
        SimpleCannon.OnInfiniteAmmoEnded += HideUI;
    }

    private void OnDisable()
    {
        SimpleCannon.OnInfiniteAmmoArmed -= ShowArmedUI;
        SimpleCannon.OnInfiniteAmmoCanceled -= HideUI;
        SimpleCannon.OnInfiniteAmmoStarted -= StartCountdown;
        SimpleCannon.OnInfiniteAmmoEnded -= HideUI;
    }

    private void Start()
    {
        if (infinitePanel != null) infinitePanel.SetActive(false);
    }

    // 🔥 MỚI: Hiện Slider đầy 100%, đứng im chờ bắn
    private void ShowArmedUI(float duration)
    {
        totalDuration = duration;
        if (infinitePanel != null) infinitePanel.SetActive(true);

        if (fillBar != null)
        {
            fillBar.maxValue = 1f;
            fillBar.value = 1f;
        }

        if (txtTitle2 != null)
        {
            txtTitle2.text = $"Unlimited ball-handling time: {Mathf.CeilToInt(duration)}s";
        }

        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
    }

    // Bắt đầu chạy Slider khi đạn nổ ra khỏi nòng
    private void StartCountdown(float duration)
    {
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

        while (timer > 0)
        {
            timer -= Time.deltaTime;

            if (fillBar != null)
            {
                fillBar.value = timer / totalDuration;
            }

            if (txtTitle2 != null)
            {
                int secondsLeft = Mathf.CeilToInt(timer);
                txtTitle2.text = $"Unlimited ball-handling time: {secondsLeft}s";
            }

            yield return null;
        }
    }
}