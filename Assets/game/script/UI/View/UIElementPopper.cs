using UnityEngine;
using DG.Tweening;

namespace LabDiner.Shared.UI
{
    public class UIElementPopper : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _delay = 0f;
        [SerializeField] private float _duration = 0.6f; // Thời gian nảy
        [SerializeField] private float _overshoot = 1.7f; // Độ nảy "lố" (càng cao nảy càng mạnh)

        private void OnEnable()
        {

            transform.localScale = Vector3.zero;


            transform.DOScale(Vector3.one, _duration)
                .SetDelay(_delay)
                .SetEase(Ease.OutBack, _overshoot)
                .SetUpdate(true);
        }

        private void OnDisable()
        {
            transform.DOKill();
        }
    }
}