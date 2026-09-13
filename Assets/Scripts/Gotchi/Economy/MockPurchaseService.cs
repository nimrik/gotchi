using System;
using UnityEngine;

namespace Gotchi.Economy
{
    // MVP stand-in: every real-money purchase succeeds instantly. Never ship this.
    public class MockPurchaseService : IPurchaseService
    {
        public bool IsAvailable => true;

        public void Purchase(ShopItem item, Action<PurchaseResult> onComplete)
        {
            Debug.Log($"[MockPurchase] Simulated purchase of {item.DisplayName} ({item.StoreProductId})");
            onComplete?.Invoke(PurchaseResult.Ok());
        }
    }
}
