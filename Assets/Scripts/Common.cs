using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public enum GameMode { Historia, ContraOTempo, Livre }

public static class GameSettings
{
    public static GameMode Mode = GameMode.Historia;
    public static float Volume = 0.8f;
    public static bool MusicOn = true;
    public const float TimeLimit = 90f;
}

// Sons carregados de Resources/Audio. Persiste entre cenas.
public class AudioManager : MonoBehaviour
{
    static AudioManager inst;
    AudioSource music, sfx;

    public static AudioManager Get()
    {
        if (inst == null)
        {
            var go = new GameObject("Audio");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<AudioManager>();
            inst.music = go.AddComponent<AudioSource>();
            inst.music.loop = true;
            inst.sfx = go.AddComponent<AudioSource>();
        }
        return inst;
    }

    public static void Sfx(string name)
    {
        var c = Resources.Load<AudioClip>("Audio/" + name);
        if (c != null) Get().sfx.PlayOneShot(c);
    }

    public static void Music(string name)
    {
        var a = Get();
        var c = Resources.Load<AudioClip>("Audio/" + name);
        a.music.clip = c;
        Refresh();
        if (c != null && GameSettings.MusicOn && !a.music.isPlaying) a.music.Play();
    }

    public static void StopMusic() { Get().music.Stop(); }

    public static void Refresh()
    {
        var a = Get();
        AudioListener.volume = GameSettings.Volume;
        if (!GameSettings.MusicOn) a.music.Stop();
        else if (a.music.clip != null && !a.music.isPlaying) a.music.Play();
    }
}

// Escolhe entre os 8 sprites (north, north-east, ...) pelo vetor de direcao.
[RequireComponent(typeof(SpriteRenderer))]
public class DirectionalSprite : MonoBehaviour
{
    static readonly string[] Names = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
    Sprite[] sprites;
    SpriteRenderer sr;

    public void Load(string folder)
    {
        sr = GetComponent<SpriteRenderer>();
        sprites = new Sprite[8];
        for (int i = 0; i < 8; i++) sprites[i] = Resources.Load<Sprite>("Art/" + folder + "/" + Names[i]);
        Face(Vector2.down);
    }

    public void Face(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f || sprites == null) return;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        int idx = Mathf.RoundToInt(ang / 45f);
        if (idx < 0) idx += 8;
        sr.sprite = sprites[idx % 8];
    }
}

public static class UIKit
{
    public static Font PixelFont => Resources.Load<Font>("Fonts/PressStart2P-Regular");

    public static Canvas MakeCanvas(string name, int order = 0)
    {
        var go = new GameObject(name);
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        var s = go.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1280, 720);
        s.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
    }

    public static Text Label(Transform parent, string text, int size, Color col, Vector2 anchor, Vector2 pos, Vector2 box, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var r = Rect(parent, "Text", anchor, pos, box);
        var t = r.gameObject.AddComponent<Text>();
        t.font = PixelFont;
        t.text = text;
        t.fontSize = size;
        t.color = col;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    public static Image Panel(Transform parent, Color col, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var r = Rect(parent, "Panel", anchor, pos, size);
        var i = r.gameObject.AddComponent<Image>();
        i.color = col;
        return i;
    }

    public static Image Picture(Transform parent, Sprite s, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var i = Panel(parent, Color.white, anchor, pos, size);
        i.sprite = s;
        i.raycastTarget = false;
        return i;
    }
}
