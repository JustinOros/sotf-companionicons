using System.Globalization;
using RedLoader;
using RedLoader.Utils;
using Sons.Ai.Vail;
using SonsSdk;
using SonsSdk.Attributes;
using TheForest.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace CompanionIcons;

public class CompanionIcons : SonsMod
{
    private const string Kelvin = "Kelvin";
    private const string Virginia = "Virginia";
    private const string KelvinHead = "Head";
    private const string VirginiaHead = "C1:C_headA0_SC";

    private class Marker
    {
        public string Kind;
        public Transform Head;
        public GameObject Root;
        public RectTransform Rect;
        public CanvasGroup Group;
    }

    private static string _configPath;
    private static bool _enabled = true;
    private static float _maxDistance = 150f;
    private static float _fadeStart = 100f;
    private static float _minDistance = 3f;
    private static float _nearSize = 48f;
    private static float _farSize = 20f;
    private static float _headOffset = 0.45f;

    private static Texture2D _kelvinTex;
    private static Texture2D _heartTex;
    private static Texture2D _heartOutlineTex;
    private static Canvas _canvas;
    private static readonly List<Marker> Markers = new();
    private static float _nextScan;
    private static float _nextTextureLookup;
    private static bool _warnedTextures;

    public CompanionIcons()
    {
        OnUpdateCallback = OnUpdate;
    }

    protected override void OnSdkInitialized()
    {
        _configPath = Path.Combine(LoaderEnvironment.UserDataDirectory, "CompanionIcons.txt");
        Load();
        RLog.Msg($"CompanionIcons loaded. Icons {(_enabled ? "on" : "off")}. Console: companionicons [on|off]");
    }

    private void OnUpdate()
    {
        if (!_enabled || !LocalPlayer.Transform || !EnsureTextures())
        {
            ShowCanvas(false);
            return;
        }

        EnsureCanvas();

        if (Time.unscaledTime >= _nextScan)
        {
            _nextScan = Time.unscaledTime + 1f;
            Rescan();
        }

        var cam = LocalPlayer.MainCam ? LocalPlayer.MainCam : Camera.main;
        if (!cam)
        {
            ShowCanvas(false);
            return;
        }

        ShowCanvas(true);
        foreach (var m in Markers)
            UpdateMarker(m, cam);
    }

    [DebugCommand("companionicons")]
    private static void ToggleCommand(string args)
    {
        args = (args ?? string.Empty).Trim().ToLowerInvariant();
        if (args == "on") SetEnabled(true);
        else if (args == "off") SetEnabled(false);
        else SetEnabled(!_enabled);
    }

    private static void SetEnabled(bool on)
    {
        _enabled = on;
        Save();
        SonsTools.ShowMessage($"Companion icons {(on ? "on" : "off")}");
        RLog.Msg($"Companion icons {(on ? "on" : "off")}");
    }

    private static bool EnsureTextures()
    {
        if (_kelvinTex && _heartTex && _heartOutlineTex)
            return true;

        if (Time.unscaledTime < _nextTextureLookup)
            return false;
        _nextTextureLookup = Time.unscaledTime + 3f;

        _kelvinTex = null;
        _heartTex = null;
        _heartOutlineTex = null;

        foreach (var t in Resources.FindObjectsOfTypeAll<Texture2D>())
        {
            if (!t) continue;
            switch (t.name)
            {
                case "RobbyLogo": _kelvinTex = Larger(_kelvinTex, t); break;
                case "HeartIcon": _heartTex = Larger(_heartTex, t); break;
                case "HeartIconOutline": _heartOutlineTex = Larger(_heartOutlineTex, t); break;
            }
        }

        if (_kelvinTex && _heartTex && _heartOutlineTex)
        {
            DestroyMarkers();
            RLog.Msg("CompanionIcons found RobbyLogo, HeartIcon and HeartIconOutline");
            return true;
        }

        if (!_warnedTextures)
        {
            _warnedTextures = true;
            RLog.Warning("CompanionIcons icon textures not loaded yet, retrying every 3 seconds");
        }
        return false;
    }

    private static Texture2D Larger(Texture2D current, Texture2D candidate)
    {
        if (!current) return candidate;
        return candidate.width > current.width ? candidate : current;
    }

    private static void EnsureCanvas()
    {
        if (_canvas) return;
        var go = new GameObject("CompanionIconsCanvas");
        UnityEngine.Object.DontDestroyOnLoad(go);
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = -50;
        Markers.Clear();
    }

    private static void ShowCanvas(bool on)
    {
        if (_canvas && _canvas.enabled != on)
            _canvas.enabled = on;
    }

    private static void Rescan()
    {
        var found = new List<(string Kind, Transform Head)>();
        CollectHeads(found);

        for (var i = Markers.Count - 1; i >= 0; i--)
        {
            var m = Markers[i];
            if (m.Root && m.Head && found.Any(f => f.Head.Pointer == m.Head.Pointer)) continue;
            if (m.Root) UnityEngine.Object.Destroy(m.Root);
            Markers.RemoveAt(i);
        }

        foreach (var (kind, head) in found)
        {
            if (!Markers.Any(m => m.Head.Pointer == head.Pointer))
                Markers.Add(CreateMarker(kind, head));
        }
    }

    private static void CollectHeads(List<(string Kind, Transform Head)> found)
    {
        try
        {
            var robby = ActorTools.GetRobby();
            if (robby) AddHead(found, Kelvin, robby.transform, KelvinHead);
        }
        catch (Exception e)
        {
            RLog.Warning($"GetRobby: {e.Message}");
        }

        try
        {
            var virginias = ActorTools.GetActors(VailActorTypeId.Virginia);
            if (virginias != null)
            {
                foreach (var v in virginias)
                    if (v) AddHead(found, Virginia, v.transform, VirginiaHead);
            }
        }
        catch (Exception e)
        {
            RLog.Warning($"GetActors Virginia: {e.Message}");
        }

        var needKelvin = !found.Any(f => f.Kind == Kelvin);
        var needVirginia = !found.Any(f => f.Kind == Virginia);
        if (!needKelvin && !needVirginia) return;

        var roots = new HashSet<IntPtr>();
        foreach (var a in UnityEngine.Object.FindObjectsOfType<Animator>())
        {
            if (!a) continue;
            var root = a.transform.root;
            if (!roots.Add(root.Pointer)) continue;
            var name = root.name;
            if (name.Contains("Creepy")) continue;
            if (needKelvin && name.StartsWith("Robby"))
                AddHead(found, Kelvin, root, KelvinHead);
            else if (needVirginia && name.StartsWith("Virginia"))
                AddHead(found, Virginia, root, VirginiaHead);
        }
    }

    private static void AddHead(List<(string Kind, Transform Head)> found, string kind, Transform root, string headName)
    {
        var head = FindDeep(root, headName);
        if (head && !found.Any(f => f.Head.Pointer == head.Pointer))
            found.Add((kind, head));
    }

    private static Marker CreateMarker(string kind, Transform head)
    {
        var go = new GameObject($"CompanionIcon_{kind}");
        go.transform.SetParent(_canvas.transform, false);
        var rect = go.AddComponent<RectTransform>();
        var group = go.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        if (kind == Kelvin)
        {
            CreateImage(go.transform, "Icon", _kelvinTex, Color.white);
        }
        else
        {
            CreateImage(go.transform, "Outline", _heartOutlineTex, new Color(0.1f, 0.02f, 0.04f, 1f));
            CreateImage(go.transform, "Icon", _heartTex, new Color(1f, 0.3f, 0.42f, 1f));
        }

        go.SetActive(false);
        return new Marker { Kind = kind, Head = head, Root = go, Rect = rect, Group = group };
    }

    private static void CreateImage(Transform parent, string name, Texture tex, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<RawImage>();
        img.texture = tex;
        img.color = color;
        img.raycastTarget = false;
        var r = img.rectTransform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private static void UpdateMarker(Marker m, Camera cam)
    {
        if (!m.Root) return;

        if (!m.Head || !m.Head.gameObject.activeInHierarchy)
        {
            m.Root.SetActive(false);
            return;
        }

        var world = m.Head.position + Vector3.up * _headOffset;
        var dist = Vector3.Distance(cam.transform.position, world);
        var sp = cam.WorldToScreenPoint(world);

        if (sp.z <= 0f || dist > _maxDistance)
        {
            m.Root.SetActive(false);
            return;
        }

        var size = Mathf.Lerp(_nearSize, _farSize, Mathf.InverseLerp(0f, _maxDistance, dist)) * (Screen.height / 1080f);
        var alpha = (1f - Mathf.InverseLerp(_fadeStart, _maxDistance, dist)) * Mathf.InverseLerp(_minDistance * 0.5f, _minDistance, dist);

        if (alpha <= 0.01f)
        {
            m.Root.SetActive(false);
            return;
        }

        m.Root.SetActive(true);
        m.Group.alpha = alpha;
        m.Rect.sizeDelta = new Vector2(size, size);
        m.Rect.position = new Vector3(sp.x, sp.y + size * 0.5f, 0f);
    }

    private static void DestroyMarkers()
    {
        foreach (var m in Markers)
            if (m.Root) UnityEngine.Object.Destroy(m.Root);
        Markers.Clear();
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (var i = 0; i < root.childCount; i++)
        {
            var f = FindDeep(root.GetChild(i), name);
            if (f) return f;
        }
        return null;
    }

    private static void Load()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                Save();
                return;
            }

            foreach (var line in File.ReadAllLines(_configPath))
            {
                var i = line.IndexOf('=');
                if (i <= 0) continue;
                var key = line[..i].Trim();
                var value = line[(i + 1)..].Trim();
                switch (key)
                {
                    case "Enabled": if (bool.TryParse(value, out var b)) _enabled = b; break;
                    case "MaxDistance": _maxDistance = ParseFloat(value, _maxDistance); break;
                    case "FadeStart": _fadeStart = ParseFloat(value, _fadeStart); break;
                    case "MinDistance": _minDistance = ParseFloat(value, _minDistance); break;
                    case "NearSize": _nearSize = ParseFloat(value, _nearSize); break;
                    case "FarSize": _farSize = ParseFloat(value, _farSize); break;
                    case "HeadOffset": _headOffset = ParseFloat(value, _headOffset); break;
                }
            }
        }
        catch (Exception e)
        {
            RLog.Warning($"CompanionIcons config load failed: {e.Message}");
        }
    }

    private static void Save()
    {
        try
        {
            var c = CultureInfo.InvariantCulture;
            File.WriteAllLines(_configPath, new[]
            {
                $"Enabled={_enabled}",
                $"MaxDistance={_maxDistance.ToString(c)}",
                $"FadeStart={_fadeStart.ToString(c)}",
                $"MinDistance={_minDistance.ToString(c)}",
                $"NearSize={_nearSize.ToString(c)}",
                $"FarSize={_farSize.ToString(c)}",
                $"HeadOffset={_headOffset.ToString(c)}"
            });
        }
        catch (Exception e)
        {
            RLog.Warning($"CompanionIcons config save failed: {e.Message}");
        }
    }

    private static float ParseFloat(string value, float fallback)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : fallback;
    }
}
