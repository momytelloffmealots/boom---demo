using UnityEngine;
using DG.Tweening;

public class CameraZoomController : MonoBehaviour
{
    public static CameraZoomController Instance;

    [Header("Camera Zoom Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float normalFOV = 55f;      // FOV mặc định ban đầu
    [SerializeField] private float normalY = 2f;         // Độ cao Y mặc định ban đầu
    [SerializeField] private float zoomedFOV = 45f;      // FOV khi zoom lại gần
    [SerializeField] private float zoomedY = 1.7f;       // Độ cao Y khi zoom
    [SerializeField] private float zoomDuration = 0.4f;  // Thời gian zoom (giây)

    private void Awake()
    {
        // Khởi tạo Singleton để các file khác dễ dàng gọi tới
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (mainCamera == null) mainCamera = Camera.main;
    }

    // Hàm gọi khi xài Booster Đạn To
    public void ZoomInForBigBullet()
    {
        if (mainCamera == null) return;

        mainCamera.DOKill();
        mainCamera.transform.DOKill();
        mainCamera.DOFieldOfView(zoomedFOV, zoomDuration).SetEase(Ease.OutCubic);
        mainCamera.transform.DOMoveY(zoomedY, zoomDuration).SetEase(Ease.OutCubic);
    }

    // Hàm gọi khi bắn xong hoặc khi Reset màn chơi
    public void ResetZoomNormal()
    {
        if (mainCamera == null) return;

        mainCamera.DOKill();
        mainCamera.transform.DOKill();
        mainCamera.DOFieldOfView(normalFOV, zoomDuration).SetEase(Ease.OutBack);
        mainCamera.transform.DOMoveY(normalY, zoomDuration).SetEase(Ease.OutBack);
    }
}