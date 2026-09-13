using System;

namespace Gotchi.Economy
{
    public struct PurchaseResult
    {
        public bool Success;
        public string Message;

        public static PurchaseResult Ok() => new PurchaseResult { Success = true, Message = "OK" };
        public static PurchaseResult Fail(string message) => new PurchaseResult { Success = false, Message = message };
    }

    public interface IPurchaseService
    {
        bool IsAvailable { get; }
        void Purchase(ShopItem item, Action<PurchaseResult> onComplete);
    }
}
