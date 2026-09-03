using UnityEngine;
using TMPro;

public class CoinDisplay : MonoBehaviour
{
    public TextMeshProUGUI txtCoin;
    
    // Biến để tránh việc đăng ký sự kiện 2 lần
    private bool isSubscribed = false;

    private void OnEnable()
    {
        SetupCoin();
    }

    private void Start()
    {
        // Bọc lót: Chạy lại lần nữa phòng trường hợp OnEnable chạy quá sớm lúc quản lý tiền chưa thức dậy
        SetupCoin();
    }

    private void SetupCoin()
    {
        if (!isSubscribed && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinChanged += UpdateCoinUI;
            UpdateCoinUI(CurrencyManager.Instance.GetCoins());
            isSubscribed = true;
        }
    }

    private void OnDisable()
    {
        if (isSubscribed && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinChanged -= UpdateCoinUI;
            isSubscribed = false; // Reset lại khi tắt UI
        }
    }

    private void UpdateCoinUI(int currentCoins)
    {
        if (txtCoin != null)
        {
            txtCoin.text = currentCoins.ToString();
        }
    }
}