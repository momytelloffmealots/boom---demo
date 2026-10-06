# QUICK START - PTITGameSDK (Bản Rút Gọn)

Đây là checklist các bước **bắt buộc** Developer phải làm khi tích hợp PTITGameSDK vào game. Đọc kĩ và tick đủ các mục dưới đây.

## 1. Cài Đặt Cơ Bản

- [ ] Import `CDIT_PTITGameSDK_v1.0.unitypackage` vào Unity.
- [ ] Import **Firebase Analytics**.
- [ ] Bỏ file `google-services.json` (Android) / `GoogleService-Info.plist` (iOS) vào thư mục `Assets`.

## 2. Khởi Tạo Firebase & Tracker

- [ ] **Viết script khởi tạo Firebase** lúc mở game (gọi `FirebaseApp.CheckAndFixDependenciesAsync()`).
- [ ] **Khởi tạo Game ID & Global Metadata** ngay sau khi Firebase load xong hoặc lúc login:

```csharp
using PTITGameSDK.Core;

// Khởi tạo Game ID
PTITGameTracker.Instance.InitTracking("TEN_GAME_CUA_BAN");
```

> [!IMPORTANT]
> **LUÔN LUÔN NHỚ ĐỔI TÊN GAME Ở HÀM TRÊN!** Nếu copy SDK sang game mới mà quên đổi, toàn bộ Log của bạn sẽ bị bắn nhầm sang data của game cũ, nâng cấp SDK cũng phải đổi lại tên.

```csharp
// Truyền thông tin người dùng
PTITGameTracker.Instance.SetGlobalMetadata(
    sessionId: "ID_Phiên_chơi",
    audienceId: "ID_Người_chơi",
    campaignName: "Tên_Chiến_dịch",
    variant: "Tên_AB_Test"
);
```

## 3. Cấu Hình Quảng Cáo (PTITAdManager)

- [ ] Mở Scene đầu tiên của game (Scene Loading).
- [ ] Kéo Prefab `PTITAdManager` từ `Assets/PTITGameSDK/Prefabs/` vào Scene.
- [ ] Click vào Prefab, chọn `Active Mediation` (AppLovinMax hoặc UnityLevelPlay).
- [ ] **ĐIỀN ĐỦ CÁC KEY QUẢNG CÁO** (App Key, Banner ID, Interstitial ID, Rewarded ID) vào Inspector.
  > [!WARNING]
  > Nếu quên kéo Prefab này vào Scene Loading, game sẽ văng lỗi Null Reference khi gọi quảng cáo.

## 4. Tích Hợp IAP (Mua Hàng)

- [ ] Gọi hàm Initialize ở đầu game và truyền vào danh sách Product ID của bạn:

```csharp
PTITIAPManager.Instance.Initialize(
    new string[] { "com.game.coin_pack_1" }, // Consumables
    new string[] { "com.game.remove_ads" }   // Non-consumables
);
```

## 5. Remote Config (Tùy Chỉnh Online)

- [ ] Mở file `PTITRemoteConfigData` trong thư mục `PTITGameSDK/Resources` và điền **giá trị mặc định** cho toàn bộ các thông số.
- [ ] Ở màn hình Loading, gọi hàm Fetch:

```csharp
PTITRemoteConfig.Instance.InitializeAndFetch(null);
// Nhớ viết coroutine chờ PTITRemoteConfig.Instance.WaitForRemoteConfig()
```

## 6. Gọi Sự Kiện Trong Game (Tracking)

Gắn các dòng code sau vào logic game (bắt buộc phải có hậu tố `.Track()` ở cuối).

- **Khi load trang chủ (Home):**
  ```csharp
  TrackingContext.GenerateNewHomeVisit();
  HomeTrackEvent.Create("action", "show").SetHomeVisitInfo(TrackingContext.HomeVisitId, TrackingContext.HomeVisitIndex, TrackingContext.EntrySource).Track();
  ```
- **Khi bắt đầu level / Chơi lại:**
  ```csharp
  LevelTrackEvent.Create("start").SetLevelInfo("attemp_1", "level_1", "v1").SetTimeInfo(timestamp, 0, 0).Track();
  ```
- **Khi kết thúc level (Thắng/Thua):**
  ```csharp
  LevelTrackEvent.Create("end").SetLevelInfo("attemp_1", "level_1", "v1").SetActionType("win").SetResultJson("{\"score\":100}").Track();
  ```
- **Lưu ý chống kẹt log:** Ở hàm `OnApplicationPause` hoặc `OnApplicationQuit` của GameManager, gọi `PTITGameTracker.Instance.FlushLogs();` để xả toàn bộ log trước khi user thoát app. Mở `PTITGameTracker.cs` (cuối file) nếu muốn bỏ comment đoạn này tự động.
