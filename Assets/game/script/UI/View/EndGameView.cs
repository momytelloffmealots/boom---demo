using System;
using UnityEngine;
using UnityEngine.UI;

public class EndGameView : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Nút Bấm (Kéo thả nút vào đây)")]
    public Button btnTryAgain;
    public Button btnHome; // THÊM LỖ CẮM CHO NÚT X

    // Cổng phát thanh sự kiện
    public event Action OnTryAgainClicked;
    public event Action OnHomeClicked; // Sự kiện báo cho Trọng tài muốn về Home

    private void Awake()
    {
        if (btnTryAgain != null)
        {
            btnTryAgain.onClick.AddListener(() => OnTryAgainClicked?.Invoke());
        }

        if (btnHome != null)
        {
            btnHome.onClick.AddListener(() => OnHomeClicked?.Invoke());
        }
    }

    public void ShowWin()
    {
        if (winPanel != null) winPanel.SetActive(true);
        if (losePanel != null) losePanel.SetActive(false);
    }

    public void ShowLose()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(true);
    }

    public void HideAll()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }
}