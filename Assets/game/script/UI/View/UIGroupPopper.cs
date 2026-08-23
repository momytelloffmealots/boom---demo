using UnityEngine;
using DG.Tweening;
using System.Collections.Generic; // Bắt buộc phải có để dùng List

namespace LabDiner.Shared.UI
{
    public class UIGroupPopper : MonoBehaviour
    {
        [Header("Danh sách UI cần nảy (Kéo thả vào đây)")]
        [SerializeField] private List<Transform> _elementsToPop = new List<Transform>();

        [Header("Animation Settings")]
        [SerializeField] private float _startDelay = 0f; // Thời gian chờ trước khi bắt đầu nảy cái đầu tiên
        [SerializeField] private float _delayBetween = 0.1f; // 🔥 HIỆU ỨNG MỚI: Độ trễ giữa các món đồ (tạo hiệu ứng nảy nối đuôi nhau)
        [SerializeField] private float _duration = 0.6f;
        [SerializeField] private float _overshoot = 1.7f;

        private void OnEnable()
        {
            if (_elementsToPop.Count == 0) return;

            // Duyệt qua từng UI con trong danh sách
            for (int i = 0; i < _elementsToPop.Count; i++)
            {
                Transform t = _elementsToPop[i];
                if (t == null) continue;

                // 1. Ép tất cả thu nhỏ về 0 ngay lập tức
                t.localScale = Vector3.zero;

                // 2. Tính toán thời gian trễ: Món đứng sau sẽ nảy muộn hơn món đứng trước
                float itemDelay = _startDelay + (i * _delayBetween);

                // 3. Ra lệnh nảy
                t.DOScale(Vector3.one, _duration)
                    .SetDelay(itemDelay)
                    .SetEase(Ease.OutBack, _overshoot)
                    .SetUpdate(true);
            }
        }

        private void OnDisable()
        {
            // Hủy Tween của tất cả các con khi Panel bị tắt
            foreach (Transform t in _elementsToPop)
            {
                if (t != null) t.DOKill();
            }
        }
    }
}