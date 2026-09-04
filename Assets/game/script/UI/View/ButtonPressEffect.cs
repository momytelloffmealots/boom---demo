using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Cài đặt hiệu ứng")]
    public float pressScale = 0.9f;
    public float animationDuration = 0.1f;

    [Header("Kích thước chuẩn của nút")]
    [Tooltip("Ghim cứng kích thước ở đây để không bị lỗi với UIGroupPopper")]
    public Vector3 defaultScale = Vector3.one; // Mặc định luôn là (1, 1, 1)

    // ĐÃ XÓA HÀM START() ĐỂ KHÔNG BỊ NHẬN NHẦM SỐ 0 NỮA

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOKill();
        // Thu nhỏ lại so với kích thước mặc định (Dùng SetUpdate(true) để lỡ game có Pause thì nút vẫn nảy)
        transform.DOScale(defaultScale * pressScale, animationDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOKill();
        // Nhả ra thì phình to về đúng kích thước mặc định
        transform.DOScale(defaultScale, animationDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(defaultScale, animationDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    private void OnDisable()
    {
        // Khi tắt bảng chỉ cần Stop hiệu ứng ấn nút, KHÔNG chỉnh scale nữa để nhường việc cho Popper
        transform.DOKill();
    }
}