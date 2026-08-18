using UnityEngine;
using UnityEngine.UI;

public class TabButton : MonoBehaviour
{
    [Header("Tab Info")]
    public string myTabName; // Điền tên của tab này (VD: "Home", "Shop")
    public StringEvent onTabSelectedEvent; // Kéo file OnTabSelected vào đây

    [Header("UI References")]
    public GameObject iconUnselected;
    public GameObject imgSelected;

    private Button myButton;

    private void Awake()
    {
        myButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        myButton.onClick.AddListener(BroadcastMyName);
        onTabSelectedEvent.OnEventRaised += HandleAnyTabChanged;
    }

    private void OnDisable()
    {
        myButton.onClick.RemoveListener(BroadcastMyName);
        onTabSelectedEvent.OnEventRaised -= HandleAnyTabChanged;
    }

    // Khi người chơi bấm nút này, nó sẽ dùng Event trung gian để hét lên tên của nó
    private void BroadcastMyName()
    {
        if (onTabSelectedEvent != null)
            onTabSelectedEvent.Raise(myTabName);
    }

    // Khi có BẤT KỲ nút nào hét lên, hàm này sẽ lắng nghe
    private void HandleAnyTabChanged(string activeTabName)
    {
        // Kiểm tra xem tên vừa được hét có phải là tên của mình không
        bool isMe = (activeTabName == myTabName);

        // Tự động thay đổi giao diện của chính nó
        iconUnselected.SetActive(!isMe);
        imgSelected.SetActive(isMe);
    }
}