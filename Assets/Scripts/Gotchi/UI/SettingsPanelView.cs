using System;
using System.Collections.Generic;
using Gotchi.Core;
using Gotchi.Data;
using Gotchi.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Gotchi.UI
{
    public class SettingsPanelView
    {
        private static readonly Dictionary<string, (CurrencyType currency, int amount)> PromoCodes = new Dictionary<string, (CurrencyType, int)>
        {
            { "COZY", (CurrencyType.Soft, 100) },
            { "SPARKLE", (CurrencyType.Premium, 20) },
        };

        private readonly GameContext _ctx;
        private readonly Action<string> _toast;
        private readonly MonoBehaviour _host;
        private readonly Text _accountText;
        private readonly Button _accountButton;
        private readonly RectTransform _signInForm;
        private readonly Button _notificationsButton;
        private readonly Button _resetButton;
        private readonly DialogBoxView _dialog;
        private readonly Text _header;
        private readonly Button _back;
        private readonly PagedScroll _pages;
        private readonly TabBarView _tabBar;
        private readonly Dictionary<string, int> _pageIndex = new Dictionary<string, int>();
        private static readonly string[] PageIds = { "Sound", "Account", "Codes", "About" };

        public readonly GameObject Root;

        // Same shape as the shop: big pixel title, ◀ PAGE ▶ pager with a counter, swipeable pages of rows, Back.
        public SettingsPanelView(Transform parent, GameContext ctx, Action<string> toast, Action onClose, Action onOpenShop, MonoBehaviour host, DialogBoxView dialog)
        {
            _ctx = ctx;
            _dialog = dialog;
            _toast = toast;
            _host = host;

            var root = UIFactory.CreateRect("SettingsRoot", parent);
            Root = root.gameObject;
            var scrim = UIFactory.CreatePanel("Scrim", root, UIFactory.Scrim);
            UIFactory.Fill(scrim.rectTransform);
            scrim.gameObject.AddComponent<Button>().onClick.AddListener(() => onClose());

            var card = UIFactory.CreateCard("Panel", root, UIFactory.PanelBlue);
            UIFactory.Fill((RectTransform)card.transform.parent, 28f, 28f, 100f, 120f);

            _header = UIFactory.CreatePixelText("Header", card.transform, "SETTINGS", 52, UIFactory.MenuInk, TextAnchor.MiddleCenter);
            UIFactory.Place(_header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -84f), new Vector2(0f, -24f));

            _tabBar = TabBarView.Arrows("Tabs", card.transform, new[]
            {
                new TabBarView.Tab("Sound & reminders", UIFactory.IconKind.Moon),
                new TabBarView.Tab("Account", UIFactory.IconKind.Heart),
                new TabBarView.Tab("Friends & codes", UIFactory.IconKind.Sparkle),
                new TabBarView.Tab("Purchases & about", UIFactory.IconKind.Bag),
            }, host);
            UIFactory.Place(_tabBar.Root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -172f), new Vector2(-24f, -100f));
            _tabBar.OnSelected += index => _pages.GoTo(index);

            _pages = PagedScroll.Create("Pages", card.transform, out RectTransform viewport);
            UIFactory.Place(viewport, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 110f), new Vector2(0f, -188f));
            _pages.OnPageChanged += index => _tabBar.Select(index, false);

            var sound = Page("Sound");
            SliderRow(sound, "Effects", GameSettings.SfxVolume, v => GameSettings.SfxVolume = v);
            SliderRow(sound, "Music", GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
            _notificationsButton = ButtonRow(sound, "Care reminders", "Gentle nudges, never at night.", GameSettings.NotificationsEnabled ? "On" : "Off", UIFactory.Mint, () =>
            {
                GameSettings.NotificationsEnabled = !GameSettings.NotificationsEnabled;
                UIFactory.SetButtonLabel(_notificationsButton, GameSettings.NotificationsEnabled ? "On" : "Off");
            });

            var account = Page("Account");
            _accountText = null;
            _accountButton = ButtonRow(account, "", "", "", UIFactory.Lavender, OnAccountButton, out _accountText);
            _signInForm = SignInForm(account);
            RefreshAccount();

            var codes = Page("Codes");
            TextRow(codes, "Your invite code", ctx.Data.ReferralCode, "Copy", UIFactory.Butter, () =>
            {
                GUIUtility.systemCopyBuffer = ctx.Data.ReferralCode;
                _toast("Invite code copied!");
            });
            InputRow(codes, "Friend's invite code", "Apply", code =>
            {
                code = code.Trim().ToUpperInvariant();
                if (code.Length != 6 || code == ctx.Data.ReferralCode) { _toast("That code doesn't look right."); return; }
                if (ctx.Data.referralRewardClaimed) { _toast("Invite bonus already claimed."); return; }
                ctx.Data.referralRewardClaimed = true;
                ctx.Wallet.Add(CurrencyType.Soft, 50);
                _toast("Thanks for joining a friend! +50 coins");
            });
            InputRow(codes, "Promo code", "Redeem", code =>
            {
                code = code.Trim().ToUpperInvariant();
                if (!PromoCodes.TryGetValue(code, out var reward)) { _toast("Unknown promo code."); return; }
                if (ctx.Data.redeemedPromoCodes.Contains(code)) { _toast("Already redeemed."); return; }
                ctx.Data.redeemedPromoCodes.Add(code);
                ctx.Wallet.Add(reward.currency, reward.amount);
                _toast($"Redeemed {code}: +{reward.amount} {(reward.currency == CurrencyType.Soft ? "coins" : "hearts")}");
            });

            var about = Page("About");
            ButtonRow(about, "Top up hearts", "Open the shop.", "Shop", UIFactory.Pink, () => { onClose(); onOpenShop(); });
            ButtonRow(about, "Restore purchases", "For a new device.", "Restore", UIFactory.Card, () => _toast("Nothing to restore in the mock store."));
            ButtonRow(about, "Privacy & terms", "Coming with store submission.", "View", UIFactory.Card, () => _toast("Privacy policy and terms are on the launch checklist."));
            _resetButton = ButtonRow(about, "Start over", "Deletes this pet and your progress.", "Reset", UIFactory.Coral, () =>
                _dialog.Ask("Erase this pet and all progress? This cannot be undone.", new[] { "Yes", "No" }, choice =>
                {
                    if (choice != 0) return;
                    ctx.SaveService.Delete();
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }));

            _back = UIFactory.CreateButton("Back", card.transform, "Back", UIFactory.Card, onClose, 30, host);
            UIFactory.Place((RectTransform)_back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 24f), new Vector2(200f, 96f));
            UIFactory.FitToLabel(_back);
            _pages.GoTo(0, false);
            _tabBar.Select(0, false);
        }

        private RectTransform Page(string id)
        {
            var page = _pages.AddPage("Page" + id);
            var column = UIFactory.CreateRect("Rows", page);
            UIFactory.Fill(column, 24f, 24f, 8f, 8f);
            UIFactory.AddVerticalLayout(column.gameObject, UIFactory.Spacing.List, new RectOffset(0, 0, 0, 0));
            _pageIndex[id] = _pageIndex.Count;
            return column;
        }

        // "Menu" (the old landing page) now means the first page.
        public void ShowPage(string id)
        {
            int index = _pageIndex.TryGetValue(id, out int i) ? i : 0;
            _pages.GoTo(index, false);
            _tabBar.Select(index, false);
        }

        private static Image Row(Transform parent, float height)
        {
            var card = UIFactory.CreateCard("Row", parent, UIFactory.Card, 0.8f);
            UIFactory.SetPreferredHeight(card.transform.parent.gameObject, height);
            return card;
        }

        private static void SliderRow(Transform parent, string title, float value, Action<float> onChange)
        {
            var row = Row(parent, 96f);
            var label = UIFactory.CreateText("Label", row.transform, title, 26, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Place(label.rectTransform, new Vector2(0f, 0f), new Vector2(0.35f, 1f), new Vector2(24f, 0f), new Vector2(0f, 0f));
            var slider = UIFactory.CreateSlider("Slider", row.transform, value, onChange);
            UIFactory.Place(slider.GetComponent<RectTransform>(), new Vector2(0.35f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -16f), new Vector2(-24f, 16f));
        }

        private Button ButtonRow(Transform parent, string title, string subtitle, string buttonLabel, Color color, Action onClick) =>
            ButtonRow(parent, title, subtitle, buttonLabel, color, onClick, out _);

        private Button ButtonRow(Transform parent, string title, string subtitle, string buttonLabel, Color color, Action onClick, out Text titleText)
        {
            var row = Row(parent, 100f);
            titleText = UIFactory.CreateText("Title", row.transform, title, 26, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Place(titleText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(24f, -2f), new Vector2(-180f, -6f));
            var sub = UIFactory.CreateText("Sub", row.transform, subtitle, 21, UIFactory.Muted, TextAnchor.MiddleLeft);
            UIFactory.Place(sub.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(24f, 8f), new Vector2(-180f, 2f));
            var button = UIFactory.CreateButton("Button", row.transform, buttonLabel, color, onClick, 24, _host);
            UIFactory.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-166f, -28f), new Vector2(-16f, 28f));
            return button;
        }

        private Button TextRow(Transform parent, string title, string value, string buttonLabel, Color color, Action onClick) =>
            ButtonRow(parent, title, value, buttonLabel, color, onClick);

        private void InputRow(Transform parent, string placeholder, string buttonLabel, Action<string> onSubmit)
        {
            var row = Row(parent, 96f);
            var field = UIFactory.CreateInputField("Input", row.transform, placeholder, InputField.ContentType.Alphanumeric);
            field.characterLimit = 12;
            UIFactory.Place(field.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(20f, -30f), new Vector2(-180f, 30f));
            var button = UIFactory.CreateButton("Button", row.transform, buttonLabel, UIFactory.Butter, () => onSubmit(field.text), 24, _host);
            UIFactory.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-166f, -28f), new Vector2(-16f, 28f));
        }

        private RectTransform SignInForm(Transform parent)
        {
            var row = Row(parent, 300f);
            var holder = (RectTransform)row.transform.parent;
            var email = UIFactory.CreateInputField("Email", row.transform, "Email", InputField.ContentType.EmailAddress);
            UIFactory.Place(email.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -98f), new Vector2(-20f, -18f));
            var password = UIFactory.CreateInputField("Password", row.transform, "Password", InputField.ContentType.Password);
            UIFactory.Place(password.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -196f), new Vector2(-20f, -116f));
            var signIn = UIFactory.CreateButton("SignIn", row.transform, "Sign in", UIFactory.Pink, () =>
            {
                _ctx.Auth.SignIn(email.text, password.text, result =>
                {
                    if (!result.Success) { _toast(result.Message); return; }
                    _ctx.Data.accountEmail = email.text.Trim();
                    _ctx.Data.sessionToken = result.SessionToken;
                    _ctx.SaveService.Save(_ctx.Data);
                    _toast("Signed in!");
                    RefreshAccount();
                });
            }, 26, _host);
            UIFactory.Place((RectTransform)signIn.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 20f), new Vector2(160f, 84f));
            holder.gameObject.SetActive(false);
            return holder;
        }

        private void OnAccountButton()
        {
            if (!string.IsNullOrEmpty(_ctx.Data.accountEmail))
            {
                _dialog.Ask("Log out? Your pet stays on this device.", new[] { "Yes", "No" }, choice =>
                {
                    if (choice != 0) return;
                    _ctx.Data.accountEmail = "";
                    _ctx.Data.sessionToken = "";
                    _ctx.SaveService.Save(_ctx.Data);
                    _toast("Logged out. Your pet stays on this device.");
                    RefreshAccount();
                });
                return;
            }
            _signInForm.gameObject.SetActive(!_signInForm.gameObject.activeSelf);
        }

        private void RefreshAccount()
        {
            bool signedIn = !string.IsNullOrEmpty(_ctx.Data.accountEmail);
            _accountText.text = signedIn ? _ctx.Data.accountEmail : "Playing as guest";
            UIFactory.SetButtonLabel(_accountButton, signedIn ? "Log out" : "Sign in");
            if (signedIn) _signInForm.gameObject.SetActive(false);
        }
    }
}
