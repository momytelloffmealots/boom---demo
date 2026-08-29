using System;
using UnityEngine;
using UnityEngine.UI;

public class EndGameView : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject continuePanel; // 🔥 Bảng Continue (Cứu trợ)

    [Header("Nút Bấm (Kéo thả nút vào đây)")]
    public Button btnTryAgain;
    public Button btnHome;
    public Button btnPlayOn;         // 🔥 Nút mua 5 đạn
    public Button btnCloseContinue;  // 🔥 Nút X của bảng Continue

    // Cổng phát thanh sự kiện
    public event Action OnTryAgainClicked;
    public event Action OnHomeClicked;
    public event Action OnPlayOnClicked;        // 🔥 Phát loa khi bấm Play On
    public event Action OnContinueCloseClicked; // 🔥 Phát loa khi từ chối cứu trợ (Bấm X)

    private void Awake()
    {
        if (btnTryAgain != null) btnTryAgain.onClick.AddListener(() => OnTryAgainClicked?.Invoke());
        if (btnHome != null) btnHome.onClick.AddListener(() => OnHomeClicked?.Invoke());
        if (btnPlayOn != null) btnPlayOn.onClick.AddListener(() => OnPlayOnClicked?.Invoke());
        if (btnCloseContinue != null) btnCloseContinue.onClick.AddListener(() => OnContinueCloseClicked?.Invoke());
    }

    public void ShowWin()
    {
        HideAll();
        if (winPanel != null) winPanel.SetActive(true);
    }

    public void ShowLose()
    {
        HideAll();
        if (losePanel != null) losePanel.SetActive(true);
    }

    public void ShowContinue()
    {
        HideAll();
        if (continuePanel != null) continuePanel.SetActive(true);
    }

    public void HideAll()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (continuePanel != null) continuePanel.SetActive(false);
    }
}