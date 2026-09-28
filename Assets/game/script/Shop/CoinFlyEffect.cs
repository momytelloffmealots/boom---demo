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
    public float scatterDuration = 0.4f;
    public float flyDuration = 0.6f;
    public float maxRandomDelay = 0.15f;

    [Header("Cài đặt Âm thanh")]
    public AudioClip appearSFX; // Âm thanh lúc bung tiền (Tùy chọn)
    public AudioClip reachSFX;  // Âm thanh Ting Ting lúc bay vào ví (Tùy chọn)
    private AudioSource audioSource;

    private IObjectPool<GameObject> coinPool;

    private void Awake()
    {
        Instance = this;
        
        // Tự động thêm AudioSource để phát nhạc UI
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

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
        coinRect.localScale = Vector3.zero;
        coinRect.rotation = Quaternion.identity;

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true); 
        
        Vector2 randomOffset = Random.insideUnitCircle * scatterRadius;
        Vector3 randomRotation = new Vector3(0, 0, Random.Range(-180f, 180f));
        float delay = Random.Range(0f, maxRandomDelay);

        seq.SetDelay(delay);

        // Phát âm thanh lúc đồng xu bắt đầu bung ra
        seq.AppendCallback(() =>
        {
            if (appearSFX != null) 
            {
                // Dùng PlayOneShot để nhiều đồng xu kêu cùng lúc không bị ngắt tiếng nhau (volume 0.5f để không quá ồn)
                audioSource.PlayOneShot(appearSFX, 0.5f); 
            }
        });

        seq.Append(coinRect.DOScale(Vector3.one, scatterDuration).SetEase(Ease.OutBack));
        seq.Join(coinRect.DOAnchorPos(randomOffset, scatterDuration).SetEase(Ease.OutCubic));
        seq.Join(coinRect.DORotate(randomRotation, scatterDuration).SetEase(Ease.OutCubic));
        
        seq.AppendInterval(0.05f);

        seq.Append(coinRect.DOMove(targetUI.position, flyDuration).SetEase(Ease.InBack));
        seq.Join(coinRect.DOScale(Vector3.one * 0.8f, flyDuration));
        seq.Join(coinRect.DORotate(Vector3.zero, flyDuration));

        seq.OnComplete(() =>
        {
            // Phát âm thanh Ting Ting khi đồng xu chạm đích
            if (reachSFX != null)
            {
                audioSource.PlayOneShot(reachSFX, 0.8f);
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCoins(coinsToGive); 
            }
            coinPool.Release(coinRect.gameObject);
        });
    }
}