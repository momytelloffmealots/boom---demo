using UnityEngine;
using UnityEngine.UI;

public class PlayPanelController : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button btnClose;
    public Button btnPlayGame;

    [Header("Liên kết Views & Hệ thống")]
    public LoadingView loadingView;      // Kéo Panel_Loading_Gameplay vào đây
    public GameObject gameplayRoot;
    public GameObject canvasUI;
    public GameObject canvasInGame;

    [Header("Cài đặt Loading")]
    public float loadingTime = 2.0f;

    private void Awake()
    {
        if (btnClose != null) btnClose.onClick.AddListener(ClosePopup);
        if (btnPlayGame != null) btnPlayGame.onClick.AddListener(StartGameplay);

        if (loadingView != null) loadingView.HideLoading();
    }

    public void OpenPopup()
    {
        gameObject.SetActive(true);
    }

    public void ClosePopup()
    {
        gameObject.SetActive(false);
    }

    public void StartGameplay()
    {
        // 1. Ra lệnh cho LoadingView bật lên và đếm giờ.
        // Dặn nó: "Khi nào đếm xong thì hãy gọi hàm SetupGame() nhé!"
        if (loadingView != null)
        {
            loadingView.ShowLoading(loadingTime, SetupGame);
        }

        // 2. Bây giờ có thể yên tâm tắt bảng Play đi mà không sợ chết Coroutine
        gameObject.SetActive(false);
    }

    // Hàm này chỉ được chạy SAU KHI màn hình Loading đếm ngược xong
    private void SetupGame()
    {
        if (canvasUI != null) canvasUI.SetActive(false);
        if (gameplayRoot != null) gameplayRoot.SetActive(true);
        if (canvasInGame != null) canvasInGame.SetActive(true);

        // if (LevelManager.Instance != null) LevelManager.Instance.GenerateCurrentMap();
    }
}