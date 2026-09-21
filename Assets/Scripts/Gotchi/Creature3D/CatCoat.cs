using System.Collections.Generic;
using UnityEngine;

namespace Gotchi.Creature3D
{
    // A fur colouring for the 3D cat. The player's cat is always "cocoa" (the painted reference); other players'
    // cats in the Battle Club and on the leaderboards wear one of the others so they read as different animals.
    // A coat only overrides palette keys of Cat3DView; everything it does not name keeps the reference colour.
    public class CatCoat
    {
        public string Id, Name;
        public Dictionary<string, Color> Colors = new Dictionary<string, Color>();

        public const string DefaultId = "cocoa";

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;

        private static CatCoat Make(string id, string name, string fur, string furMark, string stripe, string earTip, string eye) => new CatCoat
        {
            Id = id, Name = name,
            Colors = new Dictionary<string, Color>
            {
                { "Fur", Hex(fur) }, { "FurMark", Hex(furMark) }, { "Stripe", Hex(stripe) }, { "EarTip", Hex(earTip) }, { "Eye", Hex(eye) },
            },
        };

        public static readonly CatCoat[] All =
        {
            new CatCoat { Id = DefaultId, Name = "Cocoa" },
            Make("ginger", "Ginger", "DE8E4E", "CE7E40", "BC6C32", "EDBE95", "8FD36C"),
            Make("smoke",  "Smoke",  "7F8799", "727A8C", "656D7F", "AEB4C2", "FBC437"),
            Make("night",  "Night",  "38323D", "302B35", "28232C", "5C5562", "9BE06B"),
            Make("cream",  "Cream",  "E6CFAA", "DBC197", "CDB083", "F2E4CC", "6FB7F2"),
            // Coats of the Wild (CampaignSystem): strays and bosses.
            Make("tabby",  "Tabby",  "9C8468", "8A7358", "5E4A36", "BBA58A", "9BD06B"),
            Make("ash",    "Ash",    "C3C7D1", "B4B8C3", "A1A6B3", "DEE1E8", "6FB7F2"),
            Make("rust",   "Rust",   "B5532E", "A44826", "8E3B1E", "D98A68", "FBC437"),
            Make("shadow", "Shadow", "2A2530", "231F29", "1C1921", "4A4352", "F26B5E"),
        };

        public static CatCoat Find(string id)
        {
            foreach (var coat in All) if (coat.Id == id) return coat;
            return All[0];
        }

        // Coats other players can have (never the player's own).
        public static CatCoat ForIndex(int index) => All[1 + Mathf.Abs(index) % (All.Length - 1)];
    }
}
