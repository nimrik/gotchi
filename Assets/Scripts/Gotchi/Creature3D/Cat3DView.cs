using System;
using System.Collections.Generic;
using Gotchi.Creature;
using Gotchi.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gotchi.Creature3D
{
    // The 3D cat as a UI element. A private off-screen stage (model + camera → render texture) is shown through
    // a RawImage inside the pet holder, so the rest of the UI (overlays, bubbles, layout) is untouched.
    // Drives the legacy animation clips authored in Blender (Tools/blender/build_cat.py), the face (feature
    // meshes toggled by name + eye/brow bones), touch (tap / hold / rub / pick-up-and-drop), wandering and a
    // mood-tinted contact shadow. Mirrors the CreatureBody/CreatureBrain surface PetPortraitView relies on.
    public class Cat3DView
    {
        public const string ModelPath = "Creatures/Cat3D/cat";
        private const int StageLayer = 30;
        private const float VisibleHeight = 5.6f;      // world units seen vertically by the stage camera (room for lifts/hops)
        private const float CamDistance = 10.8f, CamFov = 29.1f, CamHeight = 3.0f, LookHeight = 2.2f;
        private const float MaxLift = 1.4f;            // how high a pick-up can raise the cat before it leaves the frame
        private const float FaintOffsetX = -0.95f;     // the Fainted clip lies the cat to one side; slide back to centre
        private const float GroundOffset = 0.62f;      // ground line sits Size*0.62 below the anchor (matches CreatureBody)
        private const float HalfWidth = 1.2f;          // walkable range in world units
        private const float Aspect = 1.5f;             // render texture width / height (room to walk without clipping)

        private static readonly string[] LoopNames = { "Idle", "Happy", "Sad", "Sleep", "Alert", "Walk", "Fainted" };
        private static readonly string[] HiddenByDefault =
        {
            "MouthFrown", "MouthOpen", "MouthTongue", "BrowL", "BrowR", "BlushL", "BlushR", "HappyL", "HappyR", "ShutL", "ShutR",
            "Tear", "Sweat", "Drool", "HeartL", "HeartR", "HeartTip", "DirtL", "DirtR", "Dirt3",
        };
        private static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>
        {
            { "Fur", new Color(0.235f, 0.165f, 0.150f) }, { "White", new Color(1.0f, 0.965f, 0.915f) },
            { "EarPink", new Color(0.960f, 0.600f, 0.690f) }, { "Eye", new Color(0.965f, 0.700f, 0.190f) },
            { "Pupil", new Color(0.110f, 0.085f, 0.085f) }, { "Glint", Color.white }, { "Nose", new Color(0.985f, 0.900f, 0.880f) }, { "Whisker", new Color(0.930f, 0.880f, 0.800f) },
            { "Ink", new Color(0.150f, 0.105f, 0.105f) }, { "Tongue", new Color(0.930f, 0.480f, 0.560f) },
            { "Blush", new Color(0.970f, 0.640f, 0.700f) }, { "Tear", new Color(0.480f, 0.760f, 0.940f) },
            { "HeartRed", new Color(0.930f, 0.330f, 0.420f) }, { "Dirt", new Color(0.520f, 0.400f, 0.290f) },
            { "Beanie", new Color(0.960f, 0.620f, 0.700f) }, { "Pom", new Color(1.0f, 0.965f, 0.915f) },
            { "Scarf", new Color(0.560f, 0.820f, 0.780f) }, { "Bow", new Color(0.910f, 0.330f, 0.380f) },
            { "Crown", new Color(0.970f, 0.780f, 0.280f) },
        };
        private static readonly HashSet<string> NoOutline = new HashSet<string> { "Pupil", "Glint", "Ink", "Blush", "Tear", "Nose", "Whisker" };
        private static readonly Dictionary<string, Material> SharedMaterials = new Dictionary<string, Material>();
        private static int _stageCount;

        public readonly float Size;
        public readonly RectTransform Root;          // centred on the anchor, same footprint as the 2D creature rect
        public bool Fainted { get; private set; }
        public bool Touching => _pressed;
        public bool Grounded => _slideY <= 0.001f && !_pressed;
        public bool Sleeping, Dirty, Hungry;
        public float Facing = 1f;
        public Color Mood = Color.white;
        public event Action<PetPart> Tapped;
        public event Action<PetPart> Held;
        public event Action Petted;

        private readonly MonoBehaviour _host;
        private readonly RawImage _image;
        private readonly GameObject _stage;
        private readonly Transform _slide, _turn;
        private readonly Camera _camera;
        private readonly RenderTexture _rt;
        private readonly Animation _anim;
        private readonly Dictionary<string, AnimationState> _clips = new Dictionary<string, AnimationState>();
        private readonly Dictionary<string, GameObject> _parts = new Dictionary<string, GameObject>();
        private readonly Material _shadowMat;
        private readonly Transform _eyeL, _eyeR, _browL, _browR;
        private readonly Vector3 _eyeUpAxis, _eyeRightAxis, _browFrontAxis, _browUpAxis;
        private readonly Quaternion _browRestL, _browRestR;
        private readonly Vector3 _browRestPosL, _browRestPosR;
        private readonly List<(PetPart part, Transform bone, Vector3 offset, float radius)> _hitSpheres = new List<(PetPart, Transform, Vector3, float)>();
        private string _lastPawSide = "L";
        private readonly float _worldPerCanvasUnit;

        private FaceTarget _face = FaceTarget.Neutral;
        private Spring _eyeOpen = new Spring(1f, 300f, 0.9f), _eyeScale = new Spring(1f, 200f, 0.9f), _browTilt = new Spring(0f, 150f, 0.9f), _browRaise = new Spring(0f, 150f, 0.9f);
        private Spring _yaw = new Spring(0f, 60f, 0.9f);
        private float _blinkTimer = 2f, _blinkPhase = -1f;
        private string _loop = "Idle";
        private float _slideX, _slideY, _velY;
        private bool _pressed, _dragging, _heldFired, _rubbed;
        private float _pressTime, _pressX, _lastDx;
        private int _reversals;
        private PetPart _pressPart;
        private Vector2 _pressLocal, _lastLocal;
        private Action _onArrive;
        private float _walkTarget, _walkSpeed;
        private bool _walking;
        private float _idleTimer = 25f;
        private bool _allowWander;
        private Color _shadowColor;

        public bool AllowWander { get => _allowWander; set => _allowWander = value; }

        public Cat3DView(Transform parent, MonoBehaviour host, float size)
        {
            _host = host;
            Size = size;
            Root = UIFactory.CreateRect("Cat3D", parent);
            Root.anchorMin = Root.anchorMax = new Vector2(0.5f, 0.5f);
            Root.sizeDelta = new Vector2(size * 1.6f, size * 1.6f);

            // ---- off-screen stage
            int index = _stageCount++;
            _stage = new GameObject("CatStage" + index);
            _stage.transform.position = new Vector3(3000f + index * 60f, 500f, 0f);
            _slide = new GameObject("Slide").transform; _slide.SetParent(_stage.transform, false);
            _turn = new GameObject("Turn").transform; _turn.SetParent(_slide, false);
            var modelRoot = new GameObject("Model").transform; modelRoot.SetParent(_turn, false);

            var prefab = Resources.Load<GameObject>(ModelPath);
            if (prefab == null) throw new InvalidOperationException("[Cat3D] Missing model at Resources/" + ModelPath);
            var model = UnityEngine.Object.Instantiate(prefab, modelRoot);
            model.name = "cat";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            // Face the model down -Z of the stage (the camera sits on -Z): the eye bones sit on the front of the
            // head, so head→eyes is the model's forward direction whatever the export axes did.
            var headBone = FindDeep(model.transform, "Head");
            var eyeLBone = FindDeep(model.transform, "EyeL");
            var eyeRBone = FindDeep(model.transform, "EyeR");
            Vector3 front = headBone != null && eyeLBone != null && eyeRBone != null
                ? (eyeLBone.position + eyeRBone.position) * 0.5f - headBone.position : -Vector3.forward;
            front = Vector3.ProjectOnPlane(front, Vector3.up).normalized;
            if (front.sqrMagnitude < 0.01f) front = -Vector3.forward;
            modelRoot.localRotation = Quaternion.AngleAxis(Vector3.SignedAngle(front, Vector3.back, Vector3.up), Vector3.up);
            SetLayerRecursively(_stage, StageLayer);

            // ---- renderers, materials, feature lookup
            _shadowMat = new Material(Shader.Find("Gotchi/CatShadow"));
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                _parts[r.gameObject.name] = r.gameObject;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string matName = mats[i] != null ? mats[i].name.Replace(" (Instance)", "") : "";
                    mats[i] = r.gameObject.name == "Shadow" ? _shadowMat : MaterialFor(matName, r.gameObject.name, mats[i]);
                }
                r.sharedMaterials = mats;
            }
            foreach (string name in HiddenByDefault) Show(name, false);
            foreach (var kv in _parts) if (kv.Key.StartsWith("Acc_")) kv.Value.SetActive(false);

            // ---- bones
            _eyeL = FindDeep(model.transform, "EyeL"); _eyeR = FindDeep(model.transform, "EyeR");
            _browL = FindDeep(model.transform, "BrowL"); _browR = FindDeep(model.transform, "BrowR");
            _eyeUpAxis = LocalAxisFor(_eyeL, _stage.transform.up);
            _eyeRightAxis = LocalAxisFor(_eyeL, _stage.transform.right);
            _browFrontAxis = _browL != null ? _browL.InverseTransformDirection(-_stage.transform.forward).normalized : Vector3.forward;
            _browUpAxis = _browL != null ? _browL.InverseTransformDirection(_stage.transform.up).normalized : Vector3.up;
            if (_browL != null) { _browRestL = _browL.localRotation; _browRestPosL = _browL.localPosition; }
            if (_browR != null) { _browRestR = _browR.localRotation; _browRestPosR = _browR.localPosition; }

            // Hit spheres for touch, centred relative to each bone's rest position so they follow the animation.
            AddHit(PetPart.Head, headBone, 0.64f, 1.15f);
            AddHit(PetPart.Body, FindDeep(model.transform, "Body"), 0.34f, 0.66f);
            AddHit(PetPart.Paws, FindDeep(model.transform, "ArmL"), -0.30f, 0.36f);
            AddHit(PetPart.Paws, FindDeep(model.transform, "ArmR"), -0.30f, 0.36f);
            AddHit(PetPart.Paws, FindDeep(model.transform, "LegL"), -0.22f, 0.30f);
            AddHit(PetPart.Paws, FindDeep(model.transform, "LegR"), -0.22f, 0.30f);
            AddHit(PetPart.Tail, FindDeep(model.transform, "Tail2"), 0.05f, 0.45f);

            // ---- animation (legacy): loops on layer 0, one-shots additive on layer 1
            _anim = model.GetComponent<Animation>() ?? model.GetComponentInChildren<Animation>();
            if (_anim != null)
            {
                _anim.playAutomatically = false;
                _anim.cullingType = AnimationCullingType.AlwaysAnimate;
                foreach (AnimationState st in _anim)
                {
                    string n = st.name; int bar = n.LastIndexOf('|'); if (bar >= 0) n = n.Substring(bar + 1);
                    _clips[n] = st;
                    if (Array.IndexOf(LoopNames, n) >= 0) { st.layer = 0; st.wrapMode = WrapMode.Loop; }
                    else { st.layer = 1; st.wrapMode = WrapMode.Once; st.blendMode = AnimationBlendMode.Additive; }
                }
                if (_clips.TryGetValue("Idle", out var idle)) _anim.Play(idle.name);
            }

            // ---- camera → render texture → RawImage
            int px = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.RoundToInt(size * 2.2f)), 256, 1024);
            _rt = new RenderTexture(Mathf.Min(2048, Mathf.RoundToInt(px * Aspect)), px, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "CatRT" + index };
            var camGo = new GameObject("Camera"); camGo.transform.SetParent(_stage.transform, false);
            _camera = camGo.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.cullingMask = 1 << StageLayer;
            _camera.fieldOfView = CamFov;
            _camera.nearClipPlane = 0.5f; _camera.farClipPlane = 40f;
            _camera.targetTexture = _rt;
            _camera.allowHDR = false; _camera.allowMSAA = true; _camera.useOcclusionCulling = false;
            _camera.depth = -50f;
            camGo.transform.localPosition = new Vector3(0f, CamHeight, -CamDistance);
            camGo.transform.LookAt(_stage.transform.TransformPoint(new Vector3(0f, LookHeight, 0f)));
            camGo.layer = StageLayer;

            float imageSide = VisibleHeight * (size * 0.25f);          // 1 world unit = Size/4 canvas units
            _worldPerCanvasUnit = 1f / (size * 0.25f);
            float groundViewportY = _camera.WorldToViewportPoint(_stage.transform.position).y;
            var imageGo = new GameObject("CatImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(CatTouch), typeof(CatStageLink));
            _image = imageGo.GetComponent<RawImage>();
            _image.rectTransform.SetParent(Root, false);
            _image.rectTransform.anchorMin = _image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _image.rectTransform.pivot = new Vector2(0.5f, groundViewportY);
            _image.rectTransform.sizeDelta = new Vector2(imageSide * Aspect, imageSide);
            _image.rectTransform.anchoredPosition = new Vector2(0f, -size * GroundOffset);
            _image.texture = _rt;
            _image.raycastTarget = false;
            imageGo.GetComponent<CatTouch>().View = this;
            imageGo.GetComponent<CatStageLink>().Stage = _stage;
            _stage.AddComponent<CatStageDriver>().View = this;

            _shadowColor = new Color(0.30f, 0.25f, 0.30f, 0.45f);
            _shadowMat.color = _shadowColor;
            ApplyFace();
            Debug.Log($"[Cat3D] stage {index}: rt {_rt.width}x{_rt.height}, camera aspect {_camera.aspect:F2}, image {_image.rectTransform.sizeDelta}, ground v {groundViewportY:F3}, clips {_clips.Count}");
        }

        // ---- materials ----

        private static Material MaterialFor(string matName, string objectName, Material imported)
        {
            string key = matName;
            if (string.IsNullOrEmpty(key) || (!Palette.ContainsKey(key) && key != "HeadFur" && key != "BodyFur"))
                key = GuessMaterial(objectName, matName);
            if (SharedMaterials.TryGetValue(key, out var cached)) return cached;
            bool outline = !NoOutline.Contains(key);
            var mat = new Material(Shader.Find(outline ? "Gotchi/CatToon" : "Gotchi/CatToonNoOutline")) { name = key };
            if (key == "HeadFur" || key == "BodyFur")
            {
                mat.mainTexture = Resources.Load<Texture2D>(key == "HeadFur" ? "Creatures/Cat3D/cat_head" : "Creatures/Cat3D/cat_body");
                mat.color = Color.white;
            }
            else if (Palette.TryGetValue(key, out var c)) mat.color = c;
            else if (imported != null && imported.HasProperty("_Color")) mat.color = imported.color;
            if (outline) { mat.SetFloat("_ShadeStrength", 0.10f); mat.SetFloat("_OutlineWidth", 0.06f); mat.SetColor("_OutlineColor", new Color(0.22f, 0.11f, 0.15f)); }
            if (key == "White" || key == "Pom" || key == "Glint") mat.SetFloat("_ShadeStrength", 0.07f);
            SharedMaterials[key] = mat;
            return mat;
        }

        private static string GuessMaterial(string objectName, string matName)
        {
            if (objectName == "Head") return "HeadFur";
            if (objectName == "Body") return "BodyFur";
            if (objectName.StartsWith("EarInner")) return "EarPink";
            if (objectName.StartsWith("Eye")) return "Eye";
            if (objectName.StartsWith("Pupil")) return "Pupil";
            if (objectName.StartsWith("Glint")) return "Glint";
            if (objectName.StartsWith("Foot") || objectName == "ArmL") return "White";
            if (objectName.StartsWith("Blush")) return "Blush";
            if (objectName == "Tear" || objectName == "Sweat" || objectName == "Drool") return "Tear";
            if (objectName.StartsWith("Heart")) return "HeartRed";
            if (objectName.StartsWith("Dirt")) return "Dirt";
            if (objectName.StartsWith("Whisker")) return "Whisker";
            if (objectName.StartsWith("Mouth") || objectName.StartsWith("Brow") || objectName.StartsWith("Happy") || objectName.StartsWith("Shut")) return "Ink";
            if (objectName == "Nose") return "Nose";
            return string.IsNullOrEmpty(matName) ? "Fur" : matName;
        }

        // ---- helpers ----

        // Bones and feature meshes share names (Head, Body, EyeL…): prefer the transform that is not a renderer.
        private static Transform FindDeep(Transform root, string name)
        {
            Transform fallback = null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != name) continue;
                if (t.GetComponent<Renderer>() == null) return t;
                fallback = fallback ?? t;
            }
            return fallback;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        // Which local axis of `bone` lies along `worldDir` (at rest), as a signed unit vector.
        private static Vector3 LocalAxisFor(Transform bone, Vector3 worldDir)
        {
            if (bone == null) return Vector3.up;
            Vector3 l = bone.InverseTransformDirection(worldDir);
            int a = Mathf.Abs(l.x) >= Mathf.Abs(l.y) && Mathf.Abs(l.x) >= Mathf.Abs(l.z) ? 0 : Mathf.Abs(l.y) >= Mathf.Abs(l.z) ? 1 : 2;
            var v = Vector3.zero; v[a] = 1f;
            return v;
        }

        private void AddHit(PetPart part, Transform bone, float upOffset, float radius)
        {
            if (bone == null) return;
            Vector3 world = bone.position + _stage.transform.up * upOffset;
            _hitSpheres.Add((part, bone, bone.InverseTransformPoint(world), radius));
        }

        private void Show(string name, bool on)
        {
            if (_parts.TryGetValue(name, out var go) && go.activeSelf != on) go.SetActive(on);
        }

        // ---- public API (mirrors CreatureBody / CreatureBrain) ----

        public void SetFace(FaceTarget face) { _face = face; ApplyFace(); }

        public void SnapMood() { _shadowColor = ShadowFor(Mood); _shadowMat.color = _shadowColor; }

        private static Color ShadowFor(Color mood)
        {
            var c = Color.Lerp(new Color(0.30f, 0.25f, 0.30f), mood, 0.6f);
            c.a = 0.5f;
            return c;
        }

        public void SetLoop(LoopClip loop)
        {
            if (Fainted) return;
            CrossFadeLoop(loop.ToString());
        }

        private void CrossFadeLoop(string name)
        {
            if (_anim == null || !_clips.TryGetValue(name, out var st)) return;
            if (_loop == name && _anim.IsPlaying(st.name)) return;
            _loop = name;
            _anim.CrossFade(st.name, 0.35f, PlayMode.StopSameLayer);
        }

        public void Play(OneShot clip, float direction = 1f)
        {
            if (Fainted && clip != OneShot.Faint) return;
            switch (clip)
            {
                case OneShot.Faint: Fainted = true; CrossFadeLoop("Fainted"); return;
                case OneShot.WaveL: case OneShot.WaveR:
                    PlayClip(direction < 0f ? "WaveL" : "WaveR"); return;
                case OneShot.Attack: _yaw.Kick(direction * -60f); PlayClip("Attack"); return;
                case OneShot.Hurt: _yaw.Kick(direction * 40f); PlayClip("Hurt"); return;
                default: PlayClip(clip.ToString()); return;
            }
        }

        private void PlayClip(string name)
        {
            if (_anim == null) return;
            if (!_clips.TryGetValue(name, out var st))
            {
                // clips without their own animation fall back to a close one
                string alt = name == "Yawn" ? "Stretch" : name == "Shake" ? "Wiggle" : name == "Sniff" ? "Nod" : "Wiggle";
                if (!_clips.TryGetValue(alt, out st)) return;
            }
            if (name == "Yawn") ShowMouthOpenFor(0.8f);
            _anim.Stop(st.name);
            st.time = 0f;
            _anim.Play(st.name, PlayMode.StopSameLayer);
        }

        private float _mouthOpenUntil;
        private void ShowMouthOpenFor(float seconds) => _mouthOpenUntil = Time.time + seconds;

        // Waves the paw that was last touched.
        public void WaveLastPaw() => PlayClip("Wave" + _lastPawSide);

        public void Revive()
        {
            Fainted = false;
            CrossFadeLoop("Idle");
        }

        public void SetSleeping(bool sleeping) { Sleeping = sleeping; ApplyFace(); }

        public void WalkTo(float targetX, float speed = 1.2f, Action onArrive = null)
        {
            if (Fainted || _pressed) { onArrive?.Invoke(); return; }
            _walkTarget = Mathf.Clamp(targetX, -HalfWidth, HalfWidth);
            _walkSpeed = Mathf.Max(0.3f, speed);
            _onArrive = onArrive;
            _walking = true;
            Facing = Mathf.Sign(_walkTarget - _slideX);
            CrossFadeLoop("Walk");
        }

        public float PosX => _slideX;

        public void SetAccessory(string id)
        {
            foreach (var kv in _parts)
                if (kv.Key.StartsWith("Acc_")) kv.Value.SetActive(!string.IsNullOrEmpty(id) && kv.Key.StartsWith("Acc_" + id));
        }

        public void SetConditions(bool dirty, bool hungry)
        {
            Dirty = dirty; Hungry = hungry;
            ApplyFace();
        }

        public void EnableTouch() => _image.raycastTarget = true;

        // ---- face ----

        private void ApplyFace()
        {
            var f = _face;
            bool happyEyes = f.Squint > 0.5f && !Sleeping;
            bool shut = Sleeping || (!happyEyes && f.EyeOpen < 0.2f);
            bool eyesOpen = !happyEyes && !shut;
            foreach (string s in new[] { "L", "R" })
            {
                Show("Happy" + s, happyEyes);
                Show("Shut" + s, shut);
                Show("Eye" + s, eyesOpen); Show("Pupil" + s, eyesOpen); Show("Glint" + s, eyesOpen);
                Show("Blush" + s, f.Blush > 0.6f && !Sleeping);
                Show("Brow" + s, f.BrowShow > 0.5f && !Sleeping);
            }
            bool open = !Sleeping && (f.MouthOpen > 0.45f || Time.time < _mouthOpenUntil);
            bool frown = !open && !Sleeping && f.MouthCurve < -0.1f;
            Show("MouthOpen", open); Show("MouthTongue", open);
            Show("MouthFrown", frown);
            Show("MouthSmile", !open && !frown);
            Show("Tear", f.Tear > 0.5f && !Sleeping);
            Show("Sweat", f.Sweat > 0.5f && !Sleeping);
            Show("HeartL", f.Heart > 0.5f); Show("HeartR", f.Heart > 0.5f); Show("HeartTip", f.Heart > 0.5f);
            Show("DirtL", Dirty); Show("DirtR", Dirty); Show("Dirt3", Dirty);
            Show("Drool", Hungry && !Sleeping);
            _eyeOpen.Target = eyesOpen ? Mathf.Clamp(f.EyeOpen, 0.35f, 1f) : 1f;
            _eyeScale.Target = 1f + (f.EyeScale - 1f) * 0.6f;
            _browTilt.Target = f.BrowTilt;
            _browRaise.Target = f.BrowRaise;
        }

        // ---- per-frame ----

        internal void Tick(float dt)
        {
            if (_image == null || Root == null) return;
            bool shown = _image.isActiveAndEnabled;
            if (_camera.enabled != shown) _camera.enabled = shown;
            _eyeOpen.Step(dt); _eyeScale.Step(dt); _browTilt.Step(dt); _browRaise.Step(dt);

            // blink
            if (_blinkPhase < 0f)
            {
                _blinkTimer -= dt;
                if (_blinkTimer <= 0f) { _blinkPhase = 0f; _blinkTimer = UnityEngine.Random.Range(2.5f, 6f); }
            }
            else { _blinkPhase += dt / 0.14f; if (_blinkPhase >= 1f) _blinkPhase = -1f; }

            if (_mouthOpenUntil > 0f && Time.time >= _mouthOpenUntil) { _mouthOpenUntil = 0f; ApplyFace(); }

            // mood shadow
            var target = ShadowFor(Mood);
            _shadowColor = Color.Lerp(_shadowColor, target, 1f - Mathf.Exp(-dt * 4f));
            _shadowMat.color = _shadowColor;

            // walking
            if (_walking && !_pressed)
            {
                float step = _walkSpeed * dt;
                if (Mathf.Abs(_walkTarget - _slideX) <= step)
                {
                    _slideX = _walkTarget; _walking = false;
                    CrossFadeLoop(Sleeping ? "Sleep" : _face.Loop.ToString());
                    var cb = _onArrive; _onArrive = null; cb?.Invoke();
                }
                else _slideX += Mathf.Sign(_walkTarget - _slideX) * step;
            }

            // drop after a pick-up
            if (!_pressed && _slideY > 0f)
            {
                _velY -= 28f * dt;
                _slideY += _velY * dt;
                if (_slideY <= 0f) { _slideY = 0f; _velY = 0f; PlayClip("Hop"); }
            }

            // hold detection
            if (_pressed && !_dragging && !_heldFired && Time.time - _pressTime > 0.5f)
            {
                _heldFired = true;
                Held?.Invoke(_pressPart);
            }

            // facing
            _yaw.Target = -Facing * 32f;
            _yaw.Step(dt);
            _turn.localRotation = Quaternion.Euler(0f, _yaw.Value, 0f);
            _slide.localPosition = new Vector3(_slideX + (Fainted ? FaintOffsetX : 0f), _slideY, 0f);

            // idle wandering
            if (_allowWander && !_walking && !_pressed && !Fainted && !Sleeping && _slideY <= 0f)
            {
                _idleTimer -= dt;
                if (_idleTimer <= 0f)
                {
                    _idleTimer = UnityEngine.Random.Range(20f, 45f);
                    if (UnityEngine.Random.value < 0.6f)
                    {
                        float dest = Mathf.Abs(_slideX) > 0.4f ? 0f : (UnityEngine.Random.value < 0.5f ? -1f : 1f) * UnityEngine.Random.Range(0.7f, 1.3f);
                        WalkTo(dest, UnityEngine.Random.Range(0.9f, 1.4f), () => Facing = 1f);
                    }
                }
            }
        }

        internal void LateTick()
        {
            // eyes: scale after the animation pass so blink/expression win over the clip's rest keys
            float open = _eyeOpen.Value * (_blinkPhase >= 0f ? Mathf.Max(0.08f, Mathf.Abs(_blinkPhase * 2f - 1f)) : 1f);
            float sc = _eyeScale.Value;
            Vector3 eyeScale = Vector3.one + _eyeUpAxis * (open * sc - 1f) + _eyeRightAxis * (sc - 1f);
            if (_eyeL != null) _eyeL.localScale = eyeScale;
            if (_eyeR != null) _eyeR.localScale = eyeScale;
            // brows: tilt about the viewing axis (inner ends down for positive = angry), raise along up
            if (_browL != null)
            {
                _browL.localRotation = _browRestL * Quaternion.AngleAxis(-_browTilt.Value, _browFrontAxis);
                _browL.localPosition = _browRestPosL + _browUpAxis * (_browRaise.Value * 0.03f);
            }
            if (_browR != null)
            {
                _browR.localRotation = _browRestR * Quaternion.AngleAxis(_browTilt.Value, _browFrontAxis);
                _browR.localPosition = _browRestPosR + _browUpAxis * (_browRaise.Value * 0.03f);
            }
        }

        // ---- touch (called by CatTouch) ----

        private bool HitPart(Vector2 local, out PetPart part)
        {
            part = PetPart.Body;
            var rect = _image.rectTransform.rect;
            Vector2 uv = new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
            Ray ray = _camera.ViewportPointToRay(new Vector3(uv.x, uv.y, 0f));
            float best = float.MaxValue;
            foreach (var (p, bone, offset, radius) in _hitSpheres)
            {
                Vector3 c = bone.TransformPoint(offset);
                Vector3 oc = ray.origin - c;
                float b = Vector3.Dot(oc, ray.direction);
                float disc = b * b - (oc.sqrMagnitude - radius * radius);
                if (disc < 0f) continue;
                float t = -b - Mathf.Sqrt(disc);
                if (t < 0f) t = -b + Mathf.Sqrt(disc);
                if (t < 0f || t >= best) continue;
                best = t; part = p;
                if (p == PetPart.Paws) _lastPawSide = bone.name.EndsWith("R") ? "R" : "L";
            }
            return best < float.MaxValue;
        }

        private bool LocalPoint(PointerEventData e, out Vector2 local) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_image.rectTransform, e.position, e.pressEventCamera, out local);

        internal void OnPointerDown(PointerEventData e)
        {
            if (!LocalPoint(e, out var local) || !HitPart(local, out var part)) return;
            _pressed = true; _dragging = false; _heldFired = false; _rubbed = false; _reversals = 0; _lastDx = 0f;
            _pressTime = Time.time; _pressPart = part; _pressLocal = _lastLocal = local; _pressX = _slideX;
            _walking = false;
        }

        internal void OnDrag(PointerEventData e)
        {
            if (!_pressed || !LocalPoint(e, out var local)) return;
            Vector2 delta = local - _pressLocal;
            float dx = local.x - _lastLocal.x;
            if (Mathf.Abs(dx) > 1f)
            {
                if (_lastDx != 0f && Mathf.Sign(dx) != Mathf.Sign(_lastDx)) _reversals++;
                _lastDx = dx;
            }
            _lastLocal = local;
            // rubbing back and forth on the head/body = petting
            if (!_rubbed && _reversals >= 3 && Mathf.Abs(delta.y) < Size * 0.2f && Time.time - _pressTime < 2.5f)
            {
                _rubbed = true;
                PlayClip("Pat");
                Petted?.Invoke();
            }
            if (!_dragging && delta.magnitude > Size * 0.06f && (_pressPart == PetPart.Body || _pressPart == PetPart.Head) && delta.y > Size * 0.03f)
            {
                _dragging = true;
                CrossFadeLoop("Alert");
            }
            if (_dragging)
            {
                _slideX = Mathf.Clamp(_pressX + delta.x * _worldPerCanvasUnit, -HalfWidth, HalfWidth);
                _slideY = Mathf.Clamp(delta.y * _worldPerCanvasUnit, 0f, MaxLift);
            }
        }

        internal void OnPointerUp(PointerEventData e)
        {
            if (!_pressed) return;
            _pressed = false;
            if (_dragging)
            {
                _dragging = false;
                _velY = 0f;
                CrossFadeLoop(Sleeping ? "Sleep" : _face.Loop.ToString());
                if (_slideY <= 0f) PlayClip("Hop");
            }
            else if (!_heldFired && !_rubbed && Time.time - _pressTime < 0.35f)
                Tapped?.Invoke(_pressPart);
        }

        // ---- lab helpers (simulated input in image-local canvas units) ----

        public void SimulatePress(Vector2 local)
        {
            if (!HitPart(local, out var part)) part = PetPart.Body;
            _pressed = true; _dragging = false; _heldFired = false; _rubbed = false; _reversals = 0; _lastDx = 0f;
            _pressTime = Time.time; _pressPart = part; _pressLocal = _lastLocal = local; _pressX = _slideX; _walking = false;
        }
        public void SimulateMove(Vector2 local)
        {
            if (!_pressed) return;
            Vector2 delta = local - _pressLocal;
            _lastLocal = local;
            if (!_dragging && delta.magnitude > Size * 0.06f && delta.y > Size * 0.03f) { _dragging = true; CrossFadeLoop("Alert"); }
            if (_dragging)
            {
                _slideX = Mathf.Clamp(_pressX + delta.x * _worldPerCanvasUnit, -HalfWidth, HalfWidth);
                _slideY = Mathf.Clamp(delta.y * _worldPerCanvasUnit, 0f, MaxLift);
            }
        }
        public void SimulateRelease()
        {
            if (!_pressed) return;
            _pressed = false;
            if (_dragging) { _dragging = false; _velY = 0f; CrossFadeLoop(_face.Loop.ToString()); }
        }
        // Image-local canvas point for a world-unit position (x right, y up from the cat's ground point).
        public Vector2 LocalFromWorld(float x, float y) => new Vector2(x, y) / _worldPerCanvasUnit;

        internal void Dispose()
        {
            if (_rt != null) { _rt.Release(); UnityEngine.Object.Destroy(_rt); }
            if (_shadowMat != null) UnityEngine.Object.Destroy(_shadowMat);
        }
    }

    // Pointer events from the RawImage → the view.
    public class CatTouch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [NonSerialized] public Cat3DView View;
        public void OnPointerDown(PointerEventData eventData) => View?.OnPointerDown(eventData);
        public void OnPointerUp(PointerEventData eventData) => View?.OnPointerUp(eventData);
        public void OnDrag(PointerEventData eventData) => View?.OnDrag(eventData);
    }

    // Tears the off-screen stage down together with the UI element that shows it.
    public class CatStageLink : MonoBehaviour
    {
        public GameObject Stage;
        private void OnDestroy() { if (Stage != null) Destroy(Stage); }
    }

    // Runs the view's per-frame logic from the stage object (Update before, LateUpdate after the Animation pass).
    public class CatStageDriver : MonoBehaviour
    {
        [NonSerialized] public Cat3DView View;
        private void Update() => View?.Tick(Time.deltaTime);
        private void LateUpdate() => View?.LateTick();
        private void OnDestroy() => View?.Dispose();
    }
}
