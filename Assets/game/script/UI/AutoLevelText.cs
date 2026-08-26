using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class AutoLevelText : MonoBehaviour
{
    [Tooltip("Chữ hiển thị phía trước con số. Mặc định là 'LEVEL '")]
    public string prefix = "LEVEL ";

    private TextMeshProUGUI myText;

    private void Awake()
    {
        myText = GetComponent<TextMeshProUGUI>();
    }

    // Mỗi khi object chứa chữ này được bật lên (SetActive = true), nó sẽ tự nhảy số
    private void OnEnable()
    {
        int currentLevel = PlayerPrefs.GetInt("CURRENT_LEVEL_INDEX", 1);
        myText.text = prefix + currentLevel;
    }
}