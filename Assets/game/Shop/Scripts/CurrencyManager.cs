using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [SerializeField] private int currentCoins = 1000;

    public event Action<int> OnCoinChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int GetCoins() => currentCoins;

    public bool TrySpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            OnCoinChanged?.Invoke(currentCoins);
            Debug.Log($"Đã trừ {amount} coin. Số coin còn lại: {currentCoins}");
            return true;
        }

        Debug.Log("Không đủ coin!");
        return false;
    }

    public void AddCoins(int amount)
    {
        currentCoins += amount;
        OnCoinChanged?.Invoke(currentCoins);
    }
}