using UnityEngine;
using UnityEngine.UI;

public class MoreLivesManager : MonoBehaviour
{
    [Header("Logic Nạp Mạng")]
    public Button btnRefill;
    public int refillPrice = 900;
    public GameObject PopupShop;

    private void Awake()
    {
        if (btnRefill != null)
        {
            btnRefill.onClick.AddListener(OnClickRefill);
        }
    }

    private void OnClickRefill()
    {
        if (LivesManager.Instance != null)
        {
            if (LivesManager.Instance.GetCurrentLives() >= LivesManager.Instance.maxLives)
            {
                gameObject.SetActive(false);
                return;
            }
        }
        if (CurrencyManager.Instance != null && CurrencyManager.Instance.TrySpendCoins(refillPrice))
        {
            if (LivesManager.Instance != null)
            {
                LivesManager.Instance.RefillAllLives();
            }

            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Không đủ Vàng!");
            if (PopupShop != null) PopupShop.SetActive(true);
        }
    }
}