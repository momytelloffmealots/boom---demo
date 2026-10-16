using UnityEngine;
using UnityEngine.Pool;
using DG.Tweening;

public class CoinFlyEffect : MonoBehaviour
{
    public static CoinFlyEffect Instance;

    [Header("Cài đặt Tham chiếu")]
    public GameObject coinPrefab;
    public RectTransform targetUI;        
    public RectTransform canvasTransform; 

    [Header("Cài đặt Hiệu ứng (DOTween)")]
    public float scatterRadius = 250f;    
    public float scatterDuration = 0.25f; // Giảm từ 0.4f xuống 0.25f để bung ra nhanh hơn
    public float flyDuration = 0.55f;
    public float maxRandomDelay = 0.12f;

    private IObjectPool<GameObject> coinPool;
    
    // Bộ lọc thời gian chống rè âm thanh
    private static float lastAppearSoundTime = -1f;
    private static float lastReachSoundTime = -1f;

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

    public void SpawnCoins(int totalReward) 
    {
        int visualAmount = Mathf.Min(totalReward, 15);
        int coinPerVisual = Mathf.CeilToInt((float)totalReward / visualAmount);
        int remainingCoins = totalReward;

        // Phát âm thanh xuất hiện ngay lập tức khi gọi hàm
        if (AudioManager.Instance != null && Time.unscaledTime - lastAppearSoundTime > 0.1f)
        {
            AudioManager.Instance.PlayCoinAppear();
            lastAppearSoundTime = Time.unscaledTime;
        }

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
        
        coinRect.anchoredPosition = Vector2.zero; 
        // Cho xu hiển thị sẵn ở kích thước 0.5 thay vì bằng 0 (tàng hình)
        coinRect.localScale = Vector3.one * 0.5f;
        coinRect.rotation = Quaternion.identity;

        Sequence seq = DOTween.Sequence();
        
        seq.SetId("CoinFly"); 
        seq.SetUpdate(true); 
        
        Vector2 randomOffset = Random.insideUnitCircle * scatterRadius;
        Vector3 randomRotation = new Vector3(0, 0, Random.Range(-180f, 180f));
        float flyStaggerDelay = Random.Range(0f, maxRandomDelay);

        // 1. Bung xu ra NGAY LẬP TỨC (0 giây delay)
        seq.Append(coinRect.DOScale(Vector3.one, scatterDuration).SetEase(Ease.OutBack));
        seq.Join(coinRect.DOAnchorPos(randomOffset, scatterDuration).SetEase(Ease.OutCubic));
        seq.Join(coinRect.DORotate(randomRotation, scatterDuration).SetEase(Ease.OutCubic));
        
        // 2. Chờ lệch nhịp một chút trước khi từng đồng xu bay lên ví
        seq.AppendInterval(flyStaggerDelay);

        // 3. Bay về phía UI tiền
        seq.Append(coinRect.DOMove(targetUI.position, flyDuration).SetEase(Ease.InBack));
        seq.Join(coinRect.DOScale(Vector3.one * 0.8f, flyDuration));
        seq.Join(coinRect.DORotate(Vector3.zero, flyDuration));

        seq.OnComplete(() =>
        {
            if (AudioManager.Instance != null)
            {
                if (Time.unscaledTime - lastReachSoundTime > 0.1f)
                {
                    AudioManager.Instance.PlayCoinReach();
                    lastReachSoundTime = Time.unscaledTime;
                }
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCoins(coinsToGive); 
            }
            coinPool.Release(coinRect.gameObject);
        });
    }

    public void ForceComplete()
    {
        DOTween.Complete("CoinFly");
    }

    private void OnDestroy()
    {
        DOTween.Complete("CoinFly");
    }
}