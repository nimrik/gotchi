using System;
using Gotchi.Data;

namespace Gotchi.Economy
{
    public class CurrencyWallet
    {
        private readonly PetSaveData _data;

        public event Action<CurrencyType, int> OnBalanceChanged;

        public CurrencyWallet(PetSaveData data)
        {
            _data = data;
        }

        public int Get(CurrencyType currency) =>
            currency == CurrencyType.Soft ? _data.softCurrency : _data.premiumCurrency;

        public void Add(CurrencyType currency, int amount)
        {
            if (amount <= 0) return;
            SetBalance(currency, Get(currency) + amount);
        }

        public bool TrySpend(CurrencyType currency, int amount)
        {
            if (amount < 0) return false;
            int balance = Get(currency);
            if (balance < amount) return false;
            SetBalance(currency, balance - amount);
            return true;
        }

        private void SetBalance(CurrencyType currency, int value)
        {
            if (currency == CurrencyType.Soft) _data.softCurrency = value;
            else _data.premiumCurrency = value;
            OnBalanceChanged?.Invoke(currency, value);
        }
    }
}
