using UnityEngine;
using TMPro;

public class EnergyUIView : MonoBehaviour
{
    [Header("Txt UI")]
    public TextMeshProUGUI txtCount;
    public TextMeshProUGUI txtTime;

    private void Start()
    {
        if (LivesManager.Instance != null)
        {
            LivesManager.Instance.OnLivesUpdated += UpdateUI;
            LivesManager.Instance.ForceUpdateUI();
        }
    }

    private void OnDestroy()
    {
        if (LivesManager.Instance != null)
        {
            LivesManager.Instance.OnLivesUpdated -= UpdateUI;
        }
    }

    private void UpdateUI(int lives, string timeStr)
    {
        if (txtCount != null) txtCount.text = lives.ToString();
        if (txtTime != null) txtTime.text = timeStr;
    }
}