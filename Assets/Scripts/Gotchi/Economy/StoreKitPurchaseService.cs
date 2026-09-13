using System;

namespace Gotchi.Economy
{
    // Production path for iOS. Apple App Store Review Guideline 3.1.1 requires digital goods and
    // currency unlocked inside the app to go through Apple's in-app purchase system (StoreKit) —
    // a third-party processor such as Stripe is not permitted for that and would be rejected.
    // Implement with Unity IAP (com.unity.purchasing): initialize with the product ids from
    // ShopCatalog, forward ProcessPurchase results into onComplete, and add receipt validation.
    public class StoreKitPurchaseService : IPurchaseService
    {
        public bool IsAvailable => false;

        public void Purchase(ShopItem item, Action<PurchaseResult> onComplete) =>
            onComplete?.Invoke(PurchaseResult.Fail("Store not configured yet."));
    }
}
