# HƯỚNG DẪN TÍCH HỢP VÀ SỬ DỤNG PTITGameSDK

Tài liệu này hướng dẫn cách tích hợp PTITGameSDK vào bất kỳ dự án Unity nào. PTITGameSDK là bộ Tracking Data chuẩn hóa của Viện CDIT - PTIT, đã thiết kế sẵn toàn bộ schema theo Data Dictionary (**Log Form 3**), hỗ trợ tự động bóc tách và gửi dữ liệu lên Firebase Analytics.

---

## HƯỚNG DẪN TÍCH HỢP VÀO PROJECT MỚI (DÀNH CHO DEV GAME)

### 1.1. Cài đặt SDK vào dự án mới

1. Kéo thả file `CDIT_PTITGameSDK_v1.0.unitypackage` (vừa tạo ở trên) vào cửa sổ **Project** của dự án mới và nhấn **Import**.
2. **Cài đặt Firebase Analytics:**
   Vì SDK sử dụng lệnh bắn log của Firebase, bắt buộc phải cài đặt **Firebase Unity SDK (Analytics module)** vào dự án.
3. **Cấu hình Firebase App:**
   Tạo project trên Firebase Console, tải file `google-services.json` (Android) hoặc `GoogleService-Info.plist` (iOS) và đưa vào trong thư mục `Assets`.

---

### 1.2. Phần nào SDK đã hỗ trợ

Khi sử dụng SDK này, bạn **không cần quan tâm** đến việc nhớ tên parameter hay ép kiểu dữ liệu. SDK đã bọc sẵn:

- **Tự động đính kèm Metadata:** Các thông tin chung (timestamp, version, session_id, audience_id...) sẽ tự động gắn vào MỌI event mà không cần truyền thủ công ở mỗi nơi gọi hàm.
- **Fluent Builder API chuẩn hóa:** Mọi event đã được thiết kế sẵn (Ví dụ: `LevelTrackEvent.Create...()`, `AdTrackEvent.Create...()`) ép buộc bạn phải truyền đúng số lượng và kiểu biến theo đúng Data Dictionary.
- **Tự động Parse JSON:** Xử lý các object phức tạp (như mảng `events` hay object `result` trong level track) thành chuỗi JSON tương thích với Firebase.
- **Độc lập mã nguồn:** Chạy trên 1 Assembly độc lập (`.asmdef`), không lo bị xung đột tên class với code game của bạn.

---

### 2.3. Trách nhiệm của Dev Game (Những phần bạn cần code)

Mặc dù SDK đã bọc sẵn schema, nhưng SDK **không tự động nhận diện** được khi nào người chơi qua màn hay nạp tiền. Dưới đây là 3 việc bạn phải cấu hình bằng tay trong game:

#### Bước A. Khởi tạo Firebase

Bạn phải có 1 script chạy lúc mở game (màn hình Loading) để khởi tạo Firebase.

```csharp
using Firebase;
using Firebase.Analytics;

void Start() {
    FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
        if (task.Result == DependencyStatus.Available) {
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        } else {
            UnityEngine.Debug.LogError("Firebase Error: " + task.Result);
        }
    });
}
```

#### Bước B. Thiết lập Metadata dùng chung

Ngay sau khi user login hoặc lấy được ID, bạn phải gán các thông tin này vào SDK để nó lưu và dùng cho các event tiếp theo. Gọi hàm sau ở màn hình Splash/Loading:

```csharp
using PTITGameSDK.Core;

// 1. (Optional) Nếu sử dụng Server Log nội bộ, truyền Game ID của dự án bạn vào đây (VD: "SandSort")
// Nếu không gọi hàm này, hệ thống sẽ lấy Game ID mặc định được cấu hình trong PTITGameTracker.cs
PTITGameTracker.Instance.InitTracking("your_game_id_here");
```

> [!WARNING]
> **Lưu ý khi nâng cấp SDK:** Nếu bạn cập nhật SDK từ phiên bản cũ sang phiên bản mới hoặc dùng chung SDK cho nhiều game, hãy nhớ kiểm tra và cập nhật lại chuỗi Game ID truyền vào hàm `InitTracking` (hoặc cấu hình mặc định trong `PTITGameTracker.cs`) thành đúng tên game hiện tại để tránh việc dữ liệu log bị gửi nhầm sang bảng dữ liệu của game khác!

```csharp
// 2. Gán thông tin dùng chung cho tất cả các log
PTITGameTracker.Instance.SetGlobalMetadata(
    sessionId: "phiên_chơi_hiện_tại",
    audienceId: "id_của_user",
    campaignName: "tên_chiến_dịch",
    variant: "tên_ab_test_nếu_có"
);
```

#### Bước C. Cắm Event vào các hàm Logic của Game

Bạn phải xác định các vị trí trong game (Code gameplay/UI) để cắm các dòng bắn sự kiện.

**1. Log Quản lý phiên (Audience Tracker):**
_Gọi khi mở app lần đầu tiên:_

```csharp
AudienceInforEvent.CreateFirstOpenEvent()
    .SetLocation("Hanoi", "Vietnam", "Asia", "North")
    .SetDevice("mobile", "Samsung", "S24", "Android", "14", "Android", "vi")
    .Track();
```

**2. Log Chơi Game (Level Tracker):**
_Gọi khi bắt đầu level:_

```csharp
LevelTrackEvent.Create("start")
    .SetLevelInfo("attemp_1", "level_15", "v1")
    .SetTimeInfo(1700000000, 0, 0)
    .Track();
```

_Gọi khi kết thúc level (Win):_

```csharp
// Chuỗi JSON thông tin chi tiết trận đấu
string resultJson = "{\"score\":1500,\"coin_in\":100}";

LevelTrackEvent.Create("end")
    .SetLevelInfo("attemp_1", "level_15", "v1")
    .SetTimeInfo(1700000000, 1700000120, 120f)
    .SetActionType("win")
    .SetUsage(1, 1) // 1 = có dùng booster / xem ads
    .SetResultJson(resultJson)
    .Track();
```

**3. Log Tiền tệ (Economy):**
_Gọi khi kiếm/tiêu tiền:_

```csharp
// Nạp mua GEM (Tiền cứng)
HardCurrencyEvent.Create("buy_booster", "in", "gem", 50, "ingame", "buy_revive").Track();

// Nhận thưởng COIN (Tiền mềm)
SoftCurrencyEvent.Create("in", "coin", 100, "win_level").Track();
```

**4. Log Quảng cáo & Nạp tiền (Ad / IAP):**

```csharp
// Quảng cáo
AdTrackEvent.Create("show")
    .SetBasicInfo("IronSource", 1, "reward", "revive_unit", "AdMob", "revive_panel")
    .SetLTV(0.015f)
    .Track();

// Mua IAP thành công
IAPTrackEvent.Create("purchase_success", "shop", "gem_shop", "pack_gold_100")
    .SetDuration(15.5f)
    .Track();
```

### 2.4. Làm sao để test xem Log đã bắn thành công chưa?

Để xem log lập tức (không cần đợi 12-24h như bản production), hãy làm theo cách sau:

1. Cắm máy điện thoại Android vào máy tính.
2. Mở Terminal/CMD gõ lệnh kích hoạt Debug:
   `adb shell setprop debug.firebase.analytics.app <tên_package_game_của_bạn>`
3. Lên giao diện **Firebase Console** trên trình duyệt, chọn **Analytics -> DebugView**.
4. Mở game lên bấm test, bạn sẽ thấy các Event bắn lên trên màn hình DebugView theo thời gian thực (trễ khoảng vài giây).

### 2.5. Xử lý đẩy Log khi thoát/ẩn Game (Flush Logs)

Hệ thống gửi log nội bộ của SDK mặc định sẽ tự gom mẻ (batch) và gửi đi mỗi **5 giây**. Tuy nhiên, khi người chơi đột ngột thoát game hoặc ẩn game (pause), những log chưa kịp gửi có thể bị nghẽn lại.

Để đảm bảo không mất data, bạn cần đẩy (flush) toàn bộ log lên server ngay khi bắt được sự kiện thoát/ẩn game.

**Cách xử lý:**

- **Trường hợp 1:** Nếu trong dự án của bạn **ĐÃ CÓ SẴN** một file quản lý State của game (ví dụ: `AppStateManager`, `GameManager`...) có chứa hàm `OnApplicationPause` hoặc `OnApplicationQuit`, bạn chỉ cần gọi thêm dòng lệnh này vào trong các hàm đó:
  ```csharp
  PTITGameSDK.Core.PTITGameTracker.Instance.FlushLogs();
  ```
- **Trường hợp 2:** Nếu dự án của bạn **CHƯA CÓ** các hàm quản lý State này và bạn muốn SDK tự động lo liệu, hãy mở file `Assets/PTITGameSDK/Runtime/Core/PTITGameTracker.cs` lên, tìm đến cuối file và **Bỏ comment (Uncomment)** 2 hàm `OnApplicationPause` và `OnApplicationQuit` đã được chuẩn bị sẵn ở đó.

### 2.6. Quản lý Ngữ cảnh Phiên và Hành vi Nâng cao (Behavior Tracking)

Trong các bản nâng cấp mới, SDK hỗ trợ ghi nhận luồng hành vi liên tục của người dùng thông qua lớp `TrackingContext` và các lớp sự kiện chuyên biệt như `HomeTrackEvent`, `LuckySpinTrackEvent`, `DailyTaskTrackEvent`.

**Quản lý Context Toàn cục (`TrackingContext`):**
Lớp `PTITGameSDK.Modules.TrackingContext` giúp duy trì các biến vòng đời liên cảnh (ví dụ từ gameplay về lại home) mà không cần truy vấn `PlayerPrefs` liên tục.

- Gọi `TrackingContext.GenerateNewHomeVisit();` mỗi khi màn hình Home hiển thị để tăng chỉ số `HomeVisitIndex` và cập nhật chuỗi `HomeVisitId` (dùng để nối các hành vi diễn ra từ Home).

**Các sự kiện hành vi đặc thù:**

```csharp
// 1. Home Tracking
HomeTrackEvent.Create("action", "show")
    .SetHomeVisitInfo(TrackingContext.HomeVisitId, TrackingContext.HomeVisitIndex, TrackingContext.EntrySource)
    .Track();

// 2. Lucky Spin Tracking
LuckySpinTrackEvent.Create("action", "spin")
    .SetSpinInfo("free", 1, "free")
    .Track();

// 3. Daily Task Tracking
DailyTaskTrackEvent.Create("action", "claim")
    .SetClaimInfo("task_1", "play_level", "star", "star", 10)
    .Track();
```

---

## PHẦN 3: HƯỚNG DẪN SỬ DỤNG REMOTE CONFIG

SDK tích hợp sẵn Module Remote Config (cùng cấu trúc ScriptableObject Collection chuẩn hóa), kết hợp giữa Firebase và PlayerPrefs để lưu cache tự động, giúp game đọc dữ liệu ngay lập tức (instant) không độ trễ.

### 3.1. Tùy chỉnh tham số cấu hình (đã đi kèm SDK)

SDK đã bao gồm sẵn một file cấu hình mang tên `PTITRemoteConfigData` (nằm trong thư mục `PTITGameSDK/Resources`). File này đã chứa cứng danh sách toàn bộ các trường thông tin tiêu chuẩn dùng chung cho mọi dự án.

Khi đưa SDK sang dự án mới, bạn **không cần phải tạo lại**, mà chỉ cần:

1. Mở file cấu hình có sẵn trong thư mục `PTITGameSDK/Resources` ra.
2. Điền hoặc sửa **Default Value** cho các tham số tiêu chuẩn.
3. Nếu game của bạn có thêm thông số đặc thù riêng, chỉ cần mở file script `PTITRemoteConfigCollection.cs` và khai báo thêm các biến `RemoteConfigEntry<T>` mới là xong!

### 3.2. Fetch Data từ Server

Tại màn hình Loading (hoặc sau khi FirebaseApp đã khởi tạo xong), hãy gọi hàm sau để bắt đầu tải Config về máy:

`csharp
using PTITGameSDK.Modules;

void FetchRemoteConfig()
{
// Cài đặt các giá trị mặc định cho Firebase (nếu cần, hoặc truyền null)
PTITRemoteConfig.Instance.InitializeAndFetch(null);

    // Chờ tải xong (timeout mặc định 20 giây)
    StartCoroutine(WaitForConfig());

}

IEnumerator WaitForConfig()
{
// Đợi fetch từ Firebase
yield return PTITRemoteConfig.Instance.WaitForRemoteConfig();

    // Gán dữ liệu vào ScriptableObject của bạn
    myRemoteConfigCollection.Init();

    Debug.Log("Remote Config đã sẵn sàng sử dụng!");

    // Sử dụng thử
    int cooldown = myRemoteConfigCollection.interCoolDown.value;

}
`

**Lợi ích của cơ chế này:**
Nếu máy người chơi mất mạng, hoặc quá thời gian timeout (20s) mà Firebase chưa trả dữ liệu về, PTITRemoteConfig sẽ tự động fallback về dữ liệu **PlayerPrefs Cache** (từ lần tải thành công trước đó), hoặc fallback về **Default Value**. Game của bạn sẽ không bị kẹt lại ở màn hình Loading!

### 3.3. Từ điển Các tham số RemoteConfig Tiêu chuẩn

SDK đã định nghĩa sẵn các biến cấu hình thông dụng cho hầu hết các dự án game. Các Developer (Dev) được yêu cầu phải đối chiếu và sử dụng các biến này tại đúng các **điểm Checkpoint (chuyển cảnh, bắt đầu/kết thúc level)** trong dự án của mình để logic đồng nhất.

Dưới đây là bảng giải thích ý nghĩa các biến trong PTITRemoteConfigData.asset:

| Tên Biến           | Ý nghĩa & Cách sử dụng (Checkpoint)                                                                                                              |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| **interCoolDown**  | Thời gian chờ (giây) giữa 2 lần hiện Quảng cáo Interstitial. Check trước khi gọi hàm Show Inter Ads.                                             |
| **interAdsConfig** | Cấu hình chuyên sâu cho Inter Ads: minWinLevel (Level tối thiểu để hiện Ads khi Win), minLoseLevel (khi Lose), và minReplayLevel (khi chơi lại). |

| **
NoAdsShowLevel** | Level **bắt đầu xuất hiện icon remove quảng cáo**. |
| **miniumLevelsForBanner** | Level bắt đầu hiện Banner Ads ở đáy màn hình. |
| **
RateUsShowLevel** | Level đầu tiên sẽ kích hoạt hiển thị Popup đánh giá sao (Rate Us) sau khi chơi xong. |
| **
RateUsLevelCooldown** | Số level phải chơi qua giữa 2 lần nhắc lại Popup Rate Us (nếu người chơi ấn Later/Cancel). |
| **setLevel** | _(Đặc biệt quan trọng)_ Mã ID hoặc chỉ số bộ Level đang được kích hoạt (Adjust Level). Dùng để phân chia các nhóm người chơi vào các bộ Level Design khác nhau trong A/B Testing. |
| **challengeLevelShow** | Level bắt đầu mở khóa chế độ chơi khó / thử thách (Challenge Mode). |
| **BoosterLevelShow** | Level kích hoạt mở khóa và hướng dẫn dùng vật phẩm hỗ trợ (Booster). |
| **tutorialConfigs** | Chuỗi JSON cấu hình các bước hướng dẫn tân thủ (bật tắt nút, vị trí trỏ tay...). |
| **userAttributionConfig** | Cấu hình lộ trình Quảng cáo/IAP tùy theo hành vi người dùng (User Segmentation). Thay đổi tỷ lệ/vị trí Ads cho _Tệp User Nạp Tiền_ so với _Tệp User Thường_. |
| **bannerEnable** | Công tắc tổng (True/False) để bật/tắt toàn bộ Banner Ads. |
| **timerEnabled** | Công tắc (True/False) bật/tắt chế độ đếm ngược thời gian trong level. |
| **darkModeDefault / monochromeImage** | Các cờ cấu hình giao diện (UI) hoặc sự kiện (Event) đặc thù. |
| **ArrowType / handcraftedLevelActive** | Tùy biến logic Game đặc thù (loại khối, cơ chế...). Tùy vào game có thể dùng hoặc bỏ qua. |

> [!NOTE]
> **Lưu ý cho Dev:**
>
> 1. Nếu có bất kỳ tham số nào trong bảng trên mà bạn không rõ cách áp dụng vào game của mình, hãy báo ngay cho người phụ trách dự án để được giải thích.
> 2. Nếu dự án của bạn có thêm các cơ chế đặc thù không nằm trong bảng này (ví dụ: boss_hp, player_speed...), bạn **hoàn toàn có thể khai báo thêm** bằng cách mở file PTITRemoteConfigCollection.cs và thêm một biến public RemoteConfigEntry<T> myCustomVar;.

---

## PHẦN 4: TÍCH HỢP HỆ THỐNG QUẢNG CÁO (ADS MEDIATION)

PTITGameSDK hiện tại đã tích hợp sẵn 01 Module để quản lý Quảng Cáo (`PTITAdManager`), cho phép bạn **Chuyển đổi dễ dàng giữa AppLovin MAX và Unity LevelPlay** mà không cần sửa code game.
Đặc biệt, SDK hỗ trợ **Cài đặt Tự động (Auto-Dependency)**. Khi import SDK này vào dự án mới:

- **Unity LevelPlay** sẽ tự động được tải về dự án thông qua UPM (`manifest.json`) nhờ vào Editor Script đi kèm.
- **AppLovin MAX** đã được đóng gói cứng trực tiếp vào SDK (`ThirdParty/MaxSdk`) nên bạn không cần cài thêm.

### 4.1. Thiết lập SDK Quảng Cáo và thay Key (Rất quan trọng)

**LƯU Ý QUAN TRỌNG:** Bạn BẮT BUỘC phải thực hiện các bước sau để nhúng Ads vào game:

1. Mở Scene đầu tiên của game (ví dụ: `Loading Scene`).
2. Mở thư mục `Assets/PTITGameSDK/Prefabs/` và kéo thả Prefab có tên **PTITAdManager** vào Scene của bạn.
3. Nhấp chọn GameObject `PTITAdManager` vừa kéo vào Scene. Tại cửa sổ **Inspector**:
   - Thay đổi biến `Active Mediation` để chọn mạng quảng cáo bạn muốn dùng (`AppLovinMax` hoặc `UnityLevelPlay`).
   - Điền đầy đủ các thông số Key (LevelPlay AppKey, BannerID, InterstitialID...) hoặc AppLovin Max Keys cho dự án của bạn.

> [!WARNING]
> Nếu bạn quên không đưa Prefab này vào Scene, khi game gọi đến hàm quảng cáo sẽ có 1 log lỗi màu đỏ xuất hiện: _"[PTITAdManager] No configured PTITAdManager exists in the active scene..."_ và game có thể sẽ bị lỗi Null Reference. Hãy luôn kiểm tra kỹ cấu hình Prefab này!

### 4.2. Cách gọi Quảng cáo chuẩn

Để giữ cho logic game không bao giờ bị gián đoạn (ngay cả khi rớt mạng, lỗi tải Ads, hay Ads chưa sẵn sàng), bạn **chỉ cần gọi các hàm sau**:

**1. Gọi Quảng cáo Interstitial (Toàn màn hình)**
Hàm này sẽ tự động chạy `onComplete` kể cả khi người chơi không xem được Ads, đảm bảo game vẫn đi tiếp (chuyển scene).

```csharp
PTITGameSDK.Modules.Ads.PTITAdManager.Instance.ShowInterstitial(() => {
    // Logic sau khi tắt Ads (hoặc nếu Ads lỗi)
    Debug.Log("Tiếp tục chuyển màn...");
});
```

**2. Gọi Quảng cáo Rewarded (Nhận thưởng)**
Tương tự, hàm sẽ gọi `onReceiveReward` nếu người chơi xem xong Ads, hoặc nếu game đang ở chế độ `isDisableAds = true`.

```csharp
PTITGameSDK.Modules.Ads.PTITAdManager.Instance.ShowRewardedAd(() => {
    // Trao thưởng cho user
    Debug.Log("Tặng 3 khối block mới!");
});
```

**3. Bật/Tắt Banner & MRec**

```csharp
PTITGameSDK.Modules.Ads.PTITAdManager.Instance.ToggleBannerVisibility(true);  // Hiện Banner
PTITGameSDK.Modules.Ads.PTITAdManager.Instance.ToggleBannerVisibility(false); // Ẩn Banner

PTITGameSDK.Modules.Ads.PTITAdManager.Instance.ToggleMRecVisibility(); // Bật/Tắt MRec
```

> [!TIP]
> Tất cả doanh thu quảng cáo (Ad Revenue) từ người chơi sẽ được `PTITAdManager` tự động bắt sự kiện và gửi thẳng lên Firebase Analytics thông qua `PTITGameTracker` với tên sự kiện là `ad_impression` và các tham số về `value` (doanh thu), `currency` (tiền tệ). Dev không cần code thêm tính năng đo lường doanh thu!

---

## PHẦN 5: TÍCH HỢP HỆ THỐNG MUA HÀNG (IN-APP PURCHASING - IAP)

PTITGameSDK hiện cung cấp một Module IAP cực kỳ tối ưu tên là `PTITIAPManager`, được thiết kế để **tự động hóa hoàn toàn luồng thanh toán và Tracking doanh thu**.

Đặc biệt, nếu dự án của bạn chưa cài `Unity Purchasing`, hệ thống **Auto-Installer** của SDK sẽ tự động tải thư viện này thông qua UPM (`manifest.json`) khi bạn Import SDK.

### 5.1. Khởi tạo danh sách Gói Nạp (Products)

Khác với Quảng cáo (khởi tạo tự động), IAP bắt buộc bạn phải truyền danh sách các ID Gói nạp (Product IDs) vào trước khi sử dụng. Bạn có thể gọi đoạn code này ở bất kỳ đâu khi game bắt đầu (ví dụ: `Awake` của GameManager, hoặc lúc Load Scene Menu):

```csharp
using PTITGameSDK.Modules.IAP;

void Start()
{
    string[] consumableIds = new string[] {
        "com.mygame.coin_pack_1",
        "com.mygame.coin_pack_2"
    };

    string[] nonConsumableIds = new string[] {
        "com.mygame.remove_ads",
        "com.mygame.unlock_premium"
    };

    // Gọi hàm Khởi tạo IAP
    PTITIAPManager.Instance.Initialize(consumableIds, nonConsumableIds);
}
```

### 5.2. Thực hiện giao dịch mua (Buy Product)

Để gọi bảng thanh toán của Google Play/App Store lên, bạn chỉ cần gọi 1 hàm duy nhất kèm theo Callback (Action) để nhận kết quả. Điều này giúp luồng code game không bị đóng băng khi đợi mua hàng:

```csharp
using PTITGameSDK.Modules.IAP;

public void OnClickBuyRemoveAds()
{
    PTITIAPManager.Instance.BuyProduct("com.mygame.remove_ads", (success, errorMessage) =>
    {
        if (success)
        {
            Debug.Log("Mua Remove Ads thành công!");
            // Mở khóa tính năng
            PlayerPrefs.SetInt("IsRemoveAds", 1);
        }
        else
        {
            Debug.LogWarning("Mua thất bại hoặc bị hủy: " + errorMessage);
        }
    });
}
```

> [!TIP]
> **Tự động đo lường doanh thu IAP (Revenue Tracking):**
>
> Khi một giao dịch được mua **THÀNH CÔNG**, `PTITIAPManager` sẽ tự động trích xuất Giá tiền (ví dụ: `1.99`) và Loại tiền tệ (ví dụ: `USD`) để đóng gói thành sự kiện `in_app_purchase` và bắn thẳng lên **Firebase Analytics** cùng với **Server Nội bộ** thông qua `PTITGameTracker`.
>
> Nó cũng tự động bắn chuỗi sự kiện phân tích hành vi `iap_track` (`click_button_iap`, `purchase_success`, `purchase_error`). Dev làm game **KHÔNG CẦN** viết thêm bất kỳ dòng code Tracking nào khác!
