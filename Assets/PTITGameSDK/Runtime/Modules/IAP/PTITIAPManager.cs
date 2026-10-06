#if PTIT_USE_IAP && UNITY_PURCHASING
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using PTITGameSDK.Core;

namespace PTITGameSDK.Modules.IAP
{
    public class PTITIAPManager : MonoBehaviour, IStoreListener
    {
        private static PTITIAPManager _instance;
        public static PTITIAPManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("PTITIAPManager");
                    _instance = go.AddComponent<PTITIAPManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private IStoreController _storeController;
        private IExtensionProvider _storeExtensionProvider;

        private Action<bool, string> _onPurchaseCallback;
        private string _currentBuyingProductId;

        public bool IsInitialized => _storeController != null && _storeExtensionProvider != null;

        /// <summary>
        /// Initialize Unity IAP with a list of products.
        /// </summary>
        public void Initialize(string[] consumableIds, string[] nonConsumableIds)
        {
            if (IsInitialized) return;

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            if (consumableIds != null)
            {
                foreach (var id in consumableIds)
                {
                    builder.AddProduct(id, ProductType.Consumable);
                }
            }

            if (nonConsumableIds != null)
            {
                foreach (var id in nonConsumableIds)
                {
                    builder.AddProduct(id, ProductType.NonConsumable);
                }
            }

            UnityPurchasing.Initialize(this, builder);
        }

        /// <summary>
        /// Trigger a purchase for a specific product ID.
        /// </summary>
        public void BuyProduct(string productId, Action<bool, string> onComplete)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PTITIAPManager] IAP is not initialized!");
                onComplete?.Invoke(false, "Not Initialized");
                return;
            }

            Product product = _storeController.products.WithID(productId);
            if (product != null && product.availableToPurchase)
            {
                _onPurchaseCallback = onComplete;
                _currentBuyingProductId = productId;
                _storeController.InitiatePurchase(product);
                
                // Track click button (optional, or rely on Game logic to track clicks)
                IAPTrackEvent.Create("click_button_iap", "shop", "store", productId).Track();
            }
            else
            {
                Debug.LogError("[PTITIAPManager] Product not found or not available for purchase: " + productId);
                onComplete?.Invoke(false, "Product not available");
                IAPTrackEvent.Create("purchase_error", "shop", "store", productId).SetError("Product not available").Track();
            }
        }

        public void RestorePurchases(Action<bool, string> onRestoreComplete)
        {
            if (!IsInitialized)
            {
                onRestoreComplete?.Invoke(false, "Not Initialized");
                return;
            }

            if (Application.platform == RuntimePlatform.IPhonePlayer || 
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                var apple = _storeExtensionProvider.GetExtension<IAppleExtensions>();
                apple.RestoreTransactions((result, error) => {
                    onRestoreComplete?.Invoke(result, error);
                });
            }
            else
            {
                onRestoreComplete?.Invoke(false, "Restore is not supported on this platform.");
            }
        }

        #region IStoreListener Implementation

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _storeExtensionProvider = extensions;
            Debug.Log("[PTITIAPManager] IAP Initialized successfully!");
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError("[PTITIAPManager] IAP Initialization Failed: " + error);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[PTITIAPManager] IAP Initialization Failed: {error}. Message: {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            var product = purchaseEvent.purchasedProduct;
            Debug.Log($"[PTITIAPManager] Purchase Successful: {product.definition.id}");

            // Track Revenue
            var parameters = new Dictionary<string, object>
            {
                { "value", (double)product.metadata.localizedPrice },
                { "currency", product.metadata.isoCurrencyCode },
                { "product_id", product.definition.id },
                { "transaction_id", product.transactionID }
            };
            GenericTrackEvent.Create("in_app_purchase", parameters).Track();
            
            // Log via Module
            IAPTrackEvent.Create("purchase_success", "shop", "store", product.definition.id).Track();

            // Callback to game
            if (_currentBuyingProductId == product.definition.id && _onPurchaseCallback != null)
            {
                _onPurchaseCallback.Invoke(true, "Success");
                _onPurchaseCallback = null;
                _currentBuyingProductId = null;
            }

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogError($"[PTITIAPManager] Purchase Failed: {product.definition.id}, Reason: {failureReason}");

            // Log via Module
            IAPTrackEvent.Create("purchase_error", "shop", "store", product.definition.id).SetError(failureReason.ToString()).Track();

            // Callback to game
            if (_currentBuyingProductId == product.definition.id && _onPurchaseCallback != null)
            {
                _onPurchaseCallback.Invoke(false, failureReason.ToString());
                _onPurchaseCallback = null;
                _currentBuyingProductId = null;
            }
        }

        #endregion
    }
}
#endif
