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

## 4. Luồng hoạt động của các log

Mỗi event của SDK đi theo cùng một luồng: code game tạo event, gán dữ liệu theo schema, rồi gọi `.Track()`. SDK sẽ tự bổ sung metadata chung và gửi **song song** tới Firebase Analytics và server nội bộ (nếu server URL được cấu hình). Người dùng chỉ cần truyền tham số và đặt phương thức log sau mỗi hoạt động cần log

### 4.1. Luồng gọi của tất cả event

Dev gọi các event tại checkpoint tương ứng trong game. Mỗi lời gọi bên dưới đều kết thúc bằng `.Track()` để bắt đầu luồng gửi chung của SDK.

**1. Thông tin người chơi lần đầu mở app — `audience_infor`:**

```csharp
AudienceInforEvent.CreateFirstOpenEvent() // Tạo event "audience_infor", tự đặt action_name = "first_open"
    .SetLocation(city, country, continent, region) // Thêm location dạng JSON: thành phố, quốc gia, châu lục, khu vực (phần này đang để rỗng vì chưa có API)
    .SetDevice(category, brand, model, os, osVersion, platform, locale) // Thêm device dạng JSON: thiết bị, hệ điều hành, nền tảng, ngôn ngữ
    .Track(); // Gắn metadata chung và gửi log
```

**2. Phiên chơi — `audience_track`:**

```csharp
AudienceTrackEvent.CreateStartEvent(startTime) // Tạo event bắt đầu phiên: action_name = "start", end_time và duration = 0
    .Track(); // Gắn metadata chung và gửi log

AudienceTrackEvent.CreateEndEvent(startTime, endTime, duration) // Tạo event kết thúc phiên, gồm thời điểm bắt đầu/kết thúc và thời lượng
    .Track(); // Gắn metadata chung và gửi log
```

**3. Level — `level_track`:**

```csharp
LevelTrackEvent.Create(actionName) // Tạo event "level_track" và đặt action_name, ví dụ "start" hoặc "end"
    .SetLevelInfo(attemptId, levelId, levelVersion, levelType, mode) // Thêm lượt chơi, ID/version level, loại level và mode; 2 tham số cuối mặc định là "normal"/"classic"
    .SetTimeInfo(startTime, endTime, duration) // Thêm thời điểm bắt đầu/kết thúc và thời lượng; endTime <= 0 được ghi là 0
    .SetActionType(actionType) // Thêm kết quả/hành động level, ví dụ "win" hoặc "lose"
    .SetLoseReason(loseReason) // Thêm lý do thua; chỉ gọi khi cần
    .SetUsage(isUseBooster, isViewRewardAds) // Thêm cờ dùng booster và xem rewarded ad; 1 = có, 0 = không
    .SetEventsJson(eventsJson) // Thêm chuỗi JSON các diễn biến trong level;
    .SetResultJson(resultJson) // Thêm chuỗi JSON kết quả level;
    .Track(); // Gắn metadata chung và gửi log
```

**4. `hard_currency`:**

```csharp
HardCurrencyEvent.Create(actionType, actionName, currency, value, groupPlacement, placement) // Tạo log hard_currency: loại hành động, hành động, loại tiền, giá trị và vị trí phát sinh
    .Track(); // Gắn metadata chung và gửi log
```

**5. `soft_currency`:**

```csharp
SoftCurrencyEvent.Create(actionName, currency, value, placement) // Tạo log soft_currency: hành động, loại tiền, giá trị và vị trí phát sinh
    .Track(); // Gắn metadata chung và gửi log
```

**6. Booster sau khi thắng level — `booster_after_win_level`:**

```csharp
BoosterAfterWinEvent.Create(levelId, boosterName, quantity, receiveBoosterType) // Tạo log booster nhận sau khi thắng: level, tên, số lượng và hình thức nhận
    .Track(); // Gắn metadata chung và gửi log
```

**7. Booster trong/ngoài level — `booster_track_out_level`:**

```csharp
BoosterOutLevelEvent.Create(actionName, boosterName, quantity, placement) // Tạo log thao tác booster: hành động, tên, số lượng và vị trí phát sinh
    .Track(); // Gắn metadata chung và gửi log
```

Active game integration uses `actionName = "in"` when receiving boosters and `actionName = "out"` when consuming boosters.

**8. Quảng cáo — `ad_track`:**

```csharp
AdTrackEvent.Create(actionName) // Tạo event "ad_track"; action_name có thể là request, load, load_fail, show hoặc click
    .SetBasicInfo(adPlatform, statusInternet, adFormat, adUnitName, adSource, placement) // Thêm nền tảng ads, trạng thái mạng, định dạng, ad unit, nguồn ads và vị trí hiển thị
    .SetErrorInfo(networkErrorInfo, mediationErrorInfo) // Thêm lỗi network/mediation; chỉ gọi khi ads lỗi
    .SetLTV(value) // Thêm giá trị doanh thu quảng cáo (LTV); chỉ gọi khi có dữ liệu
    .SetCreativeInfo(adId, creativeId) // Thêm ID quảng cáo và creative; chỉ gọi khi có dữ liệu
    .Track(); // Gắn metadata chung và gửi log
```

**9. IAP — `iap_track`:**

```csharp
IAPTrackEvent.Create(actionName, placement, typeShop, productId) // Tạo event "iap_track": hành động IAP, vị trí, loại shop và ID sản phẩm
    .SetError(errorMessage) // Thêm thông báo lỗi; chỉ gọi khi mua lỗi
    .SetDuration(duration) // Thêm thời gian hoàn thành thao tác IAP; chỉ gọi khi cần
    .Track(); // Gắn metadata chung và gửi log
```

Chuỗi gọi thực tế trong SDK (ví dụ về level_track):

```text
LevelTrackEvent.Create(...)
    → SetLevelInfo / SetTimeInfo / Set... (lưu parameters)
    → BaseTrackEvent.Track()
        → thêm session_id, audience_id, timestamp, campaign_name, variant, version
        → PTITGameTracker.Instance.LogEvent("level_track", parameters)
            ├→ FirebaseTrackingProvider.LogEvent(...)
            │   → FirebaseAnalytics.LogEvent("level_track", firebaseParams)
            └→ CustomServerTrackingProvider.LogEvent(...)
                → SerializeToJson(...)
                → StartCoroutine(SendPostRequest(...))
                → UnityWebRequest POST tới server
```

`Create("end")` chỉ đặt parameter `action_name`; tên event gửi đi luôn là `level_track`. Các event khác (`ad_track`, `iap_track`, `audience_track`, economy...) cũng dùng cùng cơ chế sau khi gọi `.Track()`.

### 4.2. Nhánh gửi Firebase Analytics

`FirebaseTrackingProvider` chuyển dictionary parameters thành `Firebase.Analytics.Parameter[]`, giữ nguyên các kiểu `int`, `long`, `float`, `double` và chuyển các kiểu còn lại thành chuỗi. Cuối cùng provider gọi:

```csharp
FirebaseAnalytics.LogEvent(eventName, firebaseParams);
```

Để phù hợp giới hạn Firebase, hai parameter JSON phức tạp là `events` và `result` được gửi lên Firebase dưới giá trị `"[]"`. Dữ liệu JSON gốc của hai field này vẫn được giữ ở nhánh gửi server.

### 4.3. Nhánh gửi server nội bộ

Khi `PTITGameTracker` được tạo lần đầu, SDK đăng ký `FirebaseTrackingProvider` và khởi tạo server mặc định với game ID `SandSort`, URL `https://bigame.ezwork.vn`. Có thể đặt game ID hoặc URL server riêng bằng một lần gọi khi khởi tạo:

```csharp
PTITGameTracker.Instance.InitTracking(
    "your_game_id_here",
    "https://your-server.example/logs"
);
```

`CustomServerTrackingProvider` đóng gói payload theo dạng sau rồi gửi `POST` với header `Content-Type: application/json`:

```json
{
  "event_name": "level_track",
  "game_id": "your_game_id_here",
  "collection": "event",
  "parameters": { "...": "..." }
}
```

Request chạy bằng coroutine, timeout 10 giây nên không chặn luồng chính của game. Chỉ khởi tạo server một lần; mỗi lần gọi `InitTracking(...)` sẽ đăng ký thêm một server provider và cùng event có thể bị gửi nhiều lần.

### 4.4. Khi gửi server thất bại

Nếu serialize payload lỗi hoặc HTTP request không thành công, SDK không gửi lại event gốc. Thay vào đó, `CustomServerTrackingProvider` gửi event Firebase `server_log_error` trực tiếp qua `FirebaseAnalytics.LogEvent`, kèm `error_type`, `error_message` và `original_event`. Nhánh lỗi này không đi qua `PTITGameTracker` để tránh vòng lặp vô hạn.
