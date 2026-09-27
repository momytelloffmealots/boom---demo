using UnityEngine;
using UnityEngine.Pool;
using DG.Tweening;

public class CoinFlyEffect : MonoBehaviour
{
    public static CoinFlyEffect Instance;

    [Header("Cài đặt Tham chiếu")]
    public GameObject coinPrefab;
    public RectTransform targetUI;        // Chuyển sang RectTransform cho chuẩn UI
    public RectTransform canvasTransform; 

    [Header("Cài đặt Hiệu ứng (DOTween)")]
    public float scatterRadius = 250f;    // Bán kính nổ tỏa ra
    public float scatterDuration = 0.4f;
    public float flyDuration = 0.6f;
    public float maxRandomDelay = 0.15f;

    private IObjectPool<GameObject> coinPool;

    private void Awake()
    {
        Instance = this;
        coinPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(coinPrefab, canvasTransform),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            defaultCapacity: 15
        );
    }

    // Đã bỏ biến tọa độ, tự động lấy điểm chính giữa Canvas làm gốc
    public void SpawnCoins(int totalReward) 
    {
        int visualAmount = Mathf.Min(totalReward, 15);
        int coinPerVisual = Mathf.CeilToInt((float)totalReward / visualAmount);
        int remainingCoins = totalReward;

        for (int i = 0; i < visualAmount; i++)
        {
            GameObject coinObj = coinPool.Get();
            int coinsToGive = (i == visualAmount - 1) ? remainingCoins : coinPerVisual;
            remainingCoins -= coinsToGive;

            AnimateCoin(coinObj.GetComponent<RectTransform>(), coinsToGive);
        }
    }

    private void AnimateCoin(RectTransform coinRect, int coinsToGive)
    {
        coinRect.DOKill();
        
        // 1. Ép xuất phát từ tọa độ (0,0) - Chính giữa Canvas
        coinRect.anchoredPosition = Vector2.zero; 
        coinRect.localScale = Vector3.zero;
        coinRect.rotation = Quaternion.identity;

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true); // 🔥 Đảm bảo hiệu ứng vẫn bay kể cả khi game bị Pause lúc Win
        
        Vector2 randomOffset = Random.insideUnitCircle * scatterRadius;
        Vector3 randomRotation = new Vector3(0, 0, Random.Range(-180f, 180f));
        float delay = Random.Range(0f, maxRandomDelay);

        seq.SetDelay(delay);

        // 2. Giai đoạn bùng nổ (Dùng DOAnchorPos để bay trong không gian UI)
        seq.Append(coinRect.DOScale(Vector3.one, scatterDuration).SetEase(Ease.OutBack));
        seq.Join(coinRect.DOAnchorPos(randomOffset, scatterDuration).SetEase(Ease.OutCubic));
        seq.Join(coinRect.DORotate(randomRotation, scatterDuration).SetEase(Ease.OutCubic));
        
        seq.AppendInterval(0.05f);

        // 3. Giai đoạn lao về đích (Dùng DOMove vì TargetUI là 1 object có vị trí tuyệt đối)
        seq.Append(coinRect.DOMove(targetUI.position, flyDuration).SetEase(Ease.InBack));
        seq.Join(coinRect.DOScale(Vector3.one * 0.8f, flyDuration));
        seq.Join(coinRect.DORotate(Vector3.zero, flyDuration));

        // 4. KẾT THÚC: Lúc chạm đích mới gọi hàm cộng tiền!
        seq.OnComplete(() =>
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCoins(coinsToGive); 
            }
            coinPool.Release(coinRect.gameObject);
        });
    }
}