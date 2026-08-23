using System.Collections;
using UnityEngine;

public class StartupController : MonoBehaviour
{
    [Header("Liên kết tới View")]
    public SplashView splashView; // Controller chỉ liên lạc qua View

    [Header("Cài đặt Logic")]
    public float timeShowFlow = 1.5f;
    public float timeShowSmashFest = 2.0f;
    public StringEvent onTabSelectedEvent;

    // THÊM BIẾN NÀY: Dùng từ khóa "static" để nó ghi nhớ xuyên suốt phiên chơi game
    private static bool hasShownSplash = false;

    private void Start()
    {
        // Kiểm tra xem đã từng xem màn hình Splash lần nào chưa?
        if (hasShownSplash == true)
        {
            // NẾU ĐÃ XEM RỒI (Ví dụ: Vừa LoadScene để Quit game)
            // 1. Tắt màn hình Splash đi ngay lập tức (không đếm giờ nữa)
            if (splashView != null) splashView.HideSplash();

            // 2. Kích hoạt thẳng luồng Menu Home
            if (onTabSelectedEvent != null)
            {
                onTabSelectedEvent.Raise("Home");
            }
        }
        else
        {
            // NẾU CHƯA XEM (Mới mở App lên lần đầu tiên)
            // Đánh dấu là chuẩn bị xem, để lần sau LoadScene không bị lặp lại
            hasShownSplash = true;

            // Bắt đầu đếm ngược chiếu Logo
            StartCoroutine(PlaySplashScreenRoutine());
        }
    }

    private IEnumerator PlaySplashScreenRoutine()
    {
        // 1. Ra lệnh View bật Logo Flow
        if (splashView != null) splashView.ShowFlowLogo();
        yield return new WaitForSeconds(timeShowFlow);

        // 2. Ra lệnh View bật Logo Game
        if (splashView != null) splashView.ShowSmashFestLogo();
        yield return new WaitForSeconds(timeShowSmashFest);

        // 3. Xong việc, cất Splash đi
        if (splashView != null) splashView.HideSplash();

        // 4. Kích hoạt luồng Menu chính
        if (onTabSelectedEvent != null)
        {
            onTabSelectedEvent.Raise("Home");
        }
    }
}