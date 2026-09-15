using System;
using System.Collections;
using System.IO;
using Gotchi.Creature;
using Gotchi.Data;
using Gotchi.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gotchi.Core
{
    // Dev/QA capture hook for the creature alone: `Gotchi -lab <dir>` builds its own canvas (the game boots
    // normally underneath but is hidden), renders every species, a sheet of emotions, and frame bursts of the
    // animations and simulated touches, writes PNGs into <dir> and quits. Self-starting so nothing in the
    // game bootstrap needs to know about it.
    public class CreatureLab : MonoBehaviour
    {
        private string _dir;
        private Canvas _canvas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-lab")
                {
                    var go = new GameObject("CreatureLab");
                    go.AddComponent<CreatureLab>()._dir = args[i + 1];
                    return;
                }
        }

        private void Start()
        {
            Directory.CreateDirectory(_dir);
            _canvas = UIFactory.CreateCanvas("LabCanvas");
            _canvas.sortingOrder = 100;
            StartCoroutine(Run());
        }

        private RectTransform Page(Color background)
        {
            foreach (Transform child in _canvas.transform) Destroy(child.gameObject);
            var page = UIFactory.CreatePanel("Page", _canvas.transform, background);
            UIFactory.Fill(page.rectTransform);
            return page.rectTransform;
        }

        private static RectTransform Cell(RectTransform page, float nx, float ny)
        {
            var cell = UIFactory.CreateRect("Cell", page);
            UIFactory.Place(cell, new Vector2(nx, ny), new Vector2(nx, ny), Vector2.zero, Vector2.zero);
            return cell;
        }

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(1f);

            // 1. Every species, joy.
            var page = Page(UIFactory.Hex("FFF4E8"));
            var species = (SpeciesType[])Enum.GetValues(typeof(SpeciesType));
            for (int i = 0; i < species.Length; i++)
            {
                float nx = 0.14f + (i % 4) * 0.24f, ny = 0.86f - (i / 4) * 0.23f;
                var cell = Cell(page, nx, ny);
                new PetPortraitView(cell, species[i], this, 190f).SetEmotion(EmotionType.Joy, false);
                var label = UIFactory.CreateText("Name", cell, species[i].ToString(), 22, UIFactory.Ink, TextAnchor.MiddleCenter, true);
                label.rectTransform.anchoredPosition = new Vector2(0f, -190f);
                label.rectTransform.sizeDelta = new Vector2(220f, 40f);
            }
            yield return new WaitForSeconds(2.5f);
            yield return Shot("lab-species.png");

            // 2. Emotion sheet on the cat.
            page = Page(UIFactory.Hex("FFF4E8"));
            var emotions = new[]
            {
                EmotionType.Joy, EmotionType.Excitement, EmotionType.Love, EmotionType.Pride, EmotionType.Curiosity, EmotionType.Wonder,
                EmotionType.Sorrow, EmotionType.Grief, EmotionType.Rage, EmotionType.Annoyance, EmotionType.Terror, EmotionType.Shock,
                EmotionType.Embarrassment, EmotionType.Worry, EmotionType.Contempt, EmotionType.Relief,
            };
            for (int i = 0; i < emotions.Length; i++)
            {
                float nx = 0.14f + (i % 4) * 0.24f, ny = 0.88f - (i / 4) * 0.235f;
                var cell = Cell(page, nx, ny);
                var pet = new PetPortraitView(cell, SpeciesType.Cat, this, 180f);
                pet.SetEmotion(emotions[i], false);
                var label = UIFactory.CreateText("Name", cell, emotions[i].ToString(), 22, UIFactory.Ink, TextAnchor.MiddleCenter, true);
                label.rectTransform.anchoredPosition = new Vector2(0f, -180f);
                label.rectTransform.sizeDelta = new Vector2(220f, 40f);
            }
            yield return new WaitForSeconds(3f);
            yield return Shot("lab-emotions.png");

            // 3. Frame bursts: one big creature; walking, stances, reactions, simulated touches (world units).
            page = Page(UIFactory.Hex("FFF4E8"));
            var big = Cell(page, 0.5f, 0.55f);
            var hero = new PetPortraitView(big, SpeciesType.Cat, this, 560f);
            hero.SetEmotion(EmotionType.Satisfaction, false);
            if (hero.Is3D)
            {
                yield return Run3D(hero);
                Application.Quit();
                yield break;
            }
            var body = hero.Body;
            hero.Animator.SetStance(Stance.Stand, 60f);
            yield return new WaitForSeconds(2f);
            yield return Burst("lab-idle", 6, 0.5f);
            hero.Animator.WalkTo(28f, 22f);
            yield return new WaitForSeconds(0.3f);
            yield return Burst("lab-walk", 10, 0.07f);
            yield return new WaitForSeconds(1.5f);
            hero.Animator.SetStance(Stance.Sit, 60f);
            yield return new WaitForSeconds(1.6f);
            yield return Shot("lab-sit.png");
            hero.Play(OneShot.Groom);
            yield return new WaitForSeconds(1.3f);
            yield return Shot("lab-groom.png");
            yield return new WaitForSeconds(1.5f);
            hero.Animator.SetStance(Stance.Lie, 60f);
            yield return new WaitForSeconds(1.6f);
            yield return Shot("lab-lie.png");
            hero.SetConditions(20f, 20f, 20f, 100f);
            yield return new WaitForSeconds(2f);
            yield return Shot("lab-sleep.png");
            hero.SetConditions(100f, 100f, 100f, 100f);
            hero.Animator.SetStance(Stance.Stand, 60f);
            yield return new WaitForSeconds(1.5f);
            hero.Play(OneShot.Stretch);
            yield return Burst("lab-stretch", 8, 0.25f);
            yield return new WaitForSeconds(1f);
            body.SimulatePress(new Vector2(3f, 44f));                         // pat the head
            yield return Burst("lab-poke", 8, 0.05f);
            yield return new WaitForSeconds(0.9f);
            yield return Shot("lab-hold.png");
            body.SimulateRelease();
            yield return new WaitForSeconds(1f);
            body.SimulatePress(new Vector2(0f, 20f));                         // grab the body, lift
            for (int i = 1; i <= 12; i++) { body.SimulateMoveWorld(new Vector2(body.Pos.x + i * 1.5f, 20f + i * 5f)); yield return null; yield return null; }
            yield return new WaitForSeconds(0.8f);
            yield return Shot("lab-dangle.png");
            body.SimulateRelease();
            yield return Burst("lab-drop", 10, 0.06f);
            yield return new WaitForSeconds(1.5f);
            hero.Play(OneShot.Attack, 1f);
            yield return Burst("lab-attack", 8, 0.07f);
            yield return new WaitForSeconds(1.2f);
            hero.SetEmotion(EmotionType.Joy, true);
            yield return Burst("lab-joy", 6, 0.3f);
            hero.Play(OneShot.Faint, 1f);
            yield return new WaitForSeconds(1f);
            yield return Shot("lab-faint.png");
            hero.Animator.Revive();
            yield return new WaitForSeconds(1f);
            hero.SetAccessory("hat_beanie");
            yield return new WaitForSeconds(0.8f);
            yield return Shot("lab-beanie.png");
            hero.SetAccessory("scarf_star");
            yield return new WaitForSeconds(0.3f);
            yield return Shot("lab-scarf.png");
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        // Same tour for the 3D cat: loops, walking, sleep, one-shots, simulated touches, accessories.
        private IEnumerator Run3D(PetPortraitView hero)
        {
            var cat = hero.Cat3D;
            yield return new WaitForSeconds(2f);
            yield return Burst("lab-idle", 6, 0.5f);
            cat.WalkTo(1.0f, 1.2f);
            yield return new WaitForSeconds(0.3f);
            yield return Burst("lab-walk", 10, 0.07f);
            yield return new WaitForSeconds(1.5f);
            cat.WalkTo(0f, 1.2f);
            yield return new WaitForSeconds(1.6f);
            hero.SetEmotion(EmotionType.Joy, true);
            yield return Burst("lab-joy", 6, 0.3f);
            hero.SetEmotion(EmotionType.Sorrow, true);
            yield return new WaitForSeconds(1.2f);
            yield return Shot("lab-sad.png");
            hero.SetEmotion(EmotionType.Rage, true);
            yield return new WaitForSeconds(1.2f);
            yield return Shot("lab-angry.png");
            hero.SetEmotion(EmotionType.Curiosity, true);
            yield return new WaitForSeconds(1.2f);
            yield return Shot("lab-curious.png");
            hero.SetEmotion(EmotionType.Love, true);
            yield return new WaitForSeconds(1.2f);
            yield return Shot("lab-love.png");
            hero.SetEmotion(EmotionType.Satisfaction, false);
            hero.SetConditions(20f, 20f, 20f, 100f);
            yield return new WaitForSeconds(2f);
            yield return Shot("lab-sleep.png");
            hero.SetConditions(100f, 100f, 100f, 100f);
            yield return new WaitForSeconds(1f);
            hero.Play(OneShot.Stretch);
            yield return Burst("lab-stretch", 8, 0.18f);
            yield return new WaitForSeconds(0.8f);
            cat.SimulatePress(cat.LocalFromWorld(0f, 2.0f));                  // pat the head
            yield return Burst("lab-poke", 8, 0.06f);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("lab-hold.png");
            cat.SimulateRelease();
            yield return new WaitForSeconds(0.8f);
            cat.SimulatePress(cat.LocalFromWorld(0f, 0.9f));                  // grab the body, lift
            for (int i = 1; i <= 14; i++) { cat.SimulateMove(cat.LocalFromWorld(i * 0.08f, 0.9f + i * 0.14f)); yield return null; yield return null; }
            yield return new WaitForSeconds(0.6f);
            yield return Shot("lab-dangle.png");
            cat.SimulateRelease();
            yield return Burst("lab-drop", 10, 0.07f);
            yield return new WaitForSeconds(1.2f);
            hero.Play(OneShot.Attack, 1f);
            yield return Burst("lab-attack", 8, 0.06f);
            yield return new WaitForSeconds(1f);
            hero.React(PetPart.Paws);
            yield return Burst("lab-wave", 6, 0.15f);
            yield return new WaitForSeconds(0.8f);
            hero.Play(OneShot.Faint, 1f);
            yield return new WaitForSeconds(1.2f);
            yield return Shot("lab-faint.png");
            cat.Revive();
            yield return new WaitForSeconds(1f);
            hero.SetAccessory("hat_beanie");
            yield return new WaitForSeconds(0.5f);
            yield return Shot("lab-beanie.png");
            hero.SetAccessory("scarf_star");
            yield return new WaitForSeconds(0.3f);
            yield return Shot("lab-scarf.png");
            hero.SetAccessory("crown_tiny");
            yield return new WaitForSeconds(0.3f);
            yield return Shot("lab-crown.png");
            hero.SetAccessory("");
            hero.SetConditions(100f, 20f, 100f, 20f);
            yield return new WaitForSeconds(1f);
            yield return Shot("lab-dirty.png");
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(_dir, name), tex.EncodeToPNG());
            Destroy(tex);
        }

        // Captures `frames` frames `interval` seconds apart and lays them out side by side (half size) in one PNG.
        private IEnumerator Burst(string name, int frames, float interval)
        {
            Texture2D sheet = null;
            int w = 0, h = 0;
            for (int f = 0; f < frames; f++)
            {
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (sheet == null)
                {
                    w = tex.width / 2; h = tex.height / 2;
                    sheet = new Texture2D(w * frames, h, TextureFormat.RGB24, false);
                }
                var src = tex.GetPixels32();
                var dst = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++) dst[y * w + x] = src[(y * 2) * tex.width + x * 2];
                sheet.SetPixels32(f * w, 0, w, h, dst);
                Destroy(tex);
                float t = 0f;
                while (t < interval) { t += Time.deltaTime; yield return null; }
            }
            sheet.Apply();
            File.WriteAllBytes(Path.Combine(_dir, name + ".png"), sheet.EncodeToPNG());
            Destroy(sheet);
        }
    }
}
