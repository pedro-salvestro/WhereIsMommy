using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public enum GameMode { Historia, ContraOTempo, Livre }

// Dificuldade escolhida no menu. ExtraHard: igual ao dificil, mas tudo fica escuro e a crianca so enxerga em volta dela.
public enum Dificuldade { Facil, Normal, Dificil, ExtraHard }

public static class GameSettings
{
    public static GameMode Mode = GameMode.Historia;
    public static Dificuldade Dif = Dificuldade.Normal;
    // Volume e musica ficam salvos entre uma sessao e outra (PlayerPrefs).
    public static float Volume
    {
        get => PlayerPrefs.GetFloat("volume", 0.8f);
        set { PlayerPrefs.SetFloat("volume", value); PlayerPrefs.Save(); }
    }
    public static bool MusicOn
    {
        get => PlayerPrefs.GetInt("musica", 1) == 1;
        set { PlayerPrefs.SetInt("musica", value ? 1 : 0); PlayerPrefs.Save(); }
    }
    public const float TimeLimit = 90f;

    // Partida em andamento: fase atual (0 a 4), pontos acumulados e moedas na carteira
    // (as moedas passam de uma fase para a outra e sao gastas na lojinha).
    public static int Level = 0;
    public static int TotalScore = 0;
    public static int Coins = 0;

    public static void NewRun() { Level = 0; TotalScore = 0; Coins = 0; }

    public static string ModeName(GameMode m) =>
        m == GameMode.Historia ? "HISTORIA" : m == GameMode.ContraOTempo ? "CONTRA O TEMPO" : "LIVRE";

    public static string DifName(Dificuldade d) =>
        d == Dificuldade.Facil ? "FACIL" : d == Dificuldade.Normal ? "NORMAL" : d == Dificuldade.Dificil ? "DIFICIL" : "EXTRA HARD";

    // um recorde para cada modo e dificuldade
    public static string RecordKey(GameMode m, Dificuldade d) => "recorde_" + m + "_" + d;
    public static int Record(GameMode m) => PlayerPrefs.GetInt(RecordKey(m, Dif), 0);
    public static bool SaveRecord(GameMode m, int score)
    {
        if (score <= Record(m)) return false;
        PlayerPrefs.SetInt(RecordKey(m, Dif), score);
        PlayerPrefs.Save();
        return true;
    }
}

// Quanto cada fase e mais dificil: mais NPCs, mapas maiores (fases 4 e 5), camera mais fechada.
// A dificuldade escolhida no menu muda o tempo, quantas mulheres parecidas com a mamae aparecem e o quanto a dica ajuda.
public struct Difficulty
{
    public int Guards, Lookalikes, Coins, Totems;
    public float GuardSpeed, CameraSize, Seconds, MomMovesEvery;
    public Dificuldade Dif;
    public bool Dark => Dif == Dificuldade.ExtraHard;
    public const float HitSlowSeconds = 1f, HitSlowFactor = 0.4f;
    public const float WrongMomPenalty = 3f;   // segundos perdidos ao falar com a mamae errada
    public const float DarkRadius = 2.1f;      // no Extra Hard, quanto a crianca enxerga em volta (unidades)

    public static Difficulty For(int level, GameMode mode, Dificuldade dif)
    {
        level = Mathf.Clamp(level, 0, 4);
        int di = (int)dif;
        var d = new Difficulty
        {
            Dif = dif,
            Guards = new[] { 3, 4, 5, 8, 10 }[level] + (dif == Dificuldade.Facil ? -1 : 0),
            Lookalikes = new[] { 2, 3, 4, 6, 8 }[level] + new[] { 0, 2, 4, 4 }[di],
            Coins = new[] { 10, 12, 14, 20, 26 }[level],
            Totems = new[] { 3, 3, 2, 3, 4 }[level],
            GuardSpeed = new[] { 1.3f, 1.4f, 1.5f, 1.6f, 1.7f }[level],
            CameraSize = new[] { 4.4f, 4.2f, 4.0f, 3.9f, 3.8f }[level],
            MomMovesEvery = dif == Dificuldade.Facil ? 0f : new[] { 0f, 0f, 0f, 60f, 60f }[level],
        };
        // tempo base (NORMAL, HISTORIA); as fases 4 e 5 tem mapas 2x e 3x maiores
        float t = new[] { 70f, 75f, 80f, 100f, 125f }[level];
        t *= new[] { 1.3f, 1f, 0.8f, 1f }[di];
        if (mode == GameMode.ContraOTempo) t *= 0.85f;
        d.Seconds = Mathf.Round(t / 5f) * 5f;
        return d;
    }
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

    public void Use(Sprite[] list)
    {
        sr = GetComponent<SpriteRenderer>();
        sprites = list;
        Face(Vector2.down);
    }

    public static Sprite[] LoadAll(string folder)
    {
        var list = new Sprite[8];
        for (int i = 0; i < 8; i++) list[i] = Resources.Load<Sprite>("Art/" + folder + "/" + Names[i]);
        return list;
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

// Sprites simples desenhados por codigo (nao precisam de arquivo de arte).
public static class GenSprites
{
    static Sprite coin, totem, totemUsed, ring, white, shop;

    static Sprite Make(int w, int h, System.Func<int, int, Color> f, float ppu, Vector2 pivot)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = f(x, y);
        t.SetPixels(px);
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, w, h), pivot, ppu);
    }

    public static Sprite Coin => coin ??= Make(12, 12, (x, y) =>
    {
        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(6, 6));
        if (d > 5.6f) return Color.clear;
        if (d > 4.4f) return new Color(0.75f, 0.45f, 0.05f);
        if (x >= 5 && x <= 6 && y >= 3 && y <= 8) return new Color(1f, 0.95f, 0.6f);
        return new Color(1f, 0.8f, 0.15f);
    }, 26, new Vector2(0.5f, 0.5f));

    static readonly string[] Info =
    {
        "..XX..", "..XX..", "......", ".XXX..", "..XX..", "..XX..", "..XX..", ".XXXX.",
    };

    static Sprite Totem(Color body)
    {
        return Make(14, 24, (x, y) =>
        {
            int ty = 23 - y;
            if (ty >= 20) return (x >= 5 && x <= 8) ? new Color(0.25f, 0.25f, 0.3f) : Color.clear;
            if (x == 0 || x == 13 || ty == 0 || ty == 19) return new Color(0.1f, 0.1f, 0.15f);
            int ix = x - 4, iy = ty - 5;
            if (ix >= 0 && ix < 6 && iy >= 0 && iy < 8 && Info[iy][ix] == 'X') return Color.white;
            return body;
        }, 26, new Vector2(0.5f, 0.05f));
    }

    public static Sprite TotemOn => totem ??= Totem(new Color(0.15f, 0.55f, 0.95f));
    public static Sprite TotemOff => totemUsed ??= Totem(new Color(0.4f, 0.4f, 0.45f));

    public static Sprite Ring => ring ??= Make(32, 16, (x, y) =>
    {
        float dx = (x + 0.5f - 16) / 15f, dy = (y + 0.5f - 8) / 7f;
        float r = dx * dx + dy * dy;
        return r <= 1f && r >= 0.55f ? new Color(1f, 0.9f, 0.2f, 0.9f) : r < 0.55f ? new Color(1f, 0.9f, 0.2f, 0.25f) : Color.clear;
    }, 40, new Vector2(0.5f, 0.5f));

    // Lojinha: barraca com toldo listrado e um cifrao.
    static readonly string[] Cifrao = { ".XXX.", "X.X..", ".XXX.", "..X.X", ".XXX." };
    public static Sprite Shop => shop ??= Make(22, 22, (x, y) =>
    {
        int ty = 21 - y;
        var dark = new Color(0.25f, 0.15f, 0.08f);
        if (ty <= 6) { if (ty == 0 || x == 0 || x == 21) return dark; return ((x / 3) % 2 == 0) ? new Color(0.95f, 0.3f, 0.3f) : new Color(1f, 0.95f, 0.85f); }
        if (ty <= 13) { if (x == 2 || x == 19) return dark; if (x < 2 || x > 19) return Color.clear;
            int cx = x - 8, cy = ty - 8; if (cx >= 0 && cx < 5 && cy >= 0 && cy < 5 && Cifrao[cy][cx] == 'X') return new Color(1f, 0.8f, 0.1f);
            return new Color(0.22f, 0.13f, 0.07f); }
        if (x == 0 || x == 21 || ty == 14 || ty == 21) return dark;
        return new Color(0.55f, 0.33f, 0.17f);
    }, 20, new Vector2(0.5f, 0.05f));

    public static Sprite White => white ??= Make(4, 4, (x, y) => Color.white, 4, new Vector2(0.5f, 0.5f));
}

// Troca a cor da blusa (e da saia) da mamae para criar as "mulheres parecidas".
public static class Recolor
{
    static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();

    // shirtHue/skirtHue em graus (0-360); valor < 0 mantem a cor original daquela peca
    public static Sprite[] Mom(float shirtHue, float skirtHue)
    {
        string key = shirtHue + "/" + skirtHue;
        if (cache.TryGetValue(key, out var done)) return done;
        var src = DirectionalSprite.LoadAll("Mae");
        var outp = new Sprite[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            var s = src[i];
            if (s == null) return null;
            var tr = s.textureRect;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(tr.x), 0, s.texture.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(tr.y), 0, s.texture.height - 1);
            int w = Mathf.Clamp(Mathf.RoundToInt(tr.width), 1, s.texture.width - x0);
            int h = Mathf.Clamp(Mathf.RoundToInt(tr.height), 1, s.texture.height - y0);
            Color[] px;
            try { px = s.texture.GetPixels(x0, y0, w, h); }
            catch { return null; } // imagem ainda nao legivel: quem chamou usa so uma cor por cima
            for (int k = 0; k < px.Length; k++)
            {
                var c = px[k];
                if (c.a <= 0f) continue;
                Color.RGBToHSV(c, out float hu, out float sat, out float v);
                float deg = hu * 360f;
                if (shirtHue >= 0f && deg >= 335f && sat > 0.3f) px[k] = Shift(c, shirtHue, sat, v);
                else if (skirtHue >= 0f && deg >= 210f && deg <= 245f && sat > 0.15f) px[k] = Shift(c, skirtHue, sat, v);
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(px);
            tex.Apply();
            // pivo em pixels, relativo ao recorte (o pivo do sprite e relativo ao retangulo original)
            Vector2 piv = s.pivot - (s.textureRectOffset);
            var pn = new Vector2(Mathf.Clamp01(piv.x / w), Mathf.Clamp01(piv.y / h));
            try { outp[i] = Sprite.Create(tex, new Rect(0, 0, w, h), pn, s.pixelsPerUnit); }
            catch { return null; }
        }
        cache[key] = outp;
        return outp;
    }

    static Color Shift(Color c, float hueDeg, float sat, float v)
    {
        var n = Color.HSVToRGB(Mathf.Repeat(hueDeg, 360f) / 360f, Mathf.Max(sat, 0.45f), v);
        n.a = c.a;
        return n;
    }
}

// Contorno colorido em volta de um personagem (4 copias em silhueta atras dele),
// para diferenciar os personagens vivos das pessoas desenhadas no cenario.
public class Outline : MonoBehaviour
{
    SpriteRenderer src;
    SpriteRenderer[] copies;
    static Material mat;

    public static Outline Add(GameObject go, Color c, float size = 0.035f)
    {
        var o = go.AddComponent<Outline>();
        o.src = go.GetComponent<SpriteRenderer>();
        if (mat == null) mat = new Material(Shader.Find("GUI/Text Shader"));
        Vector2[] dirs = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
        o.copies = new SpriteRenderer[dirs.Length];
        for (int i = 0; i < dirs.Length; i++)
        {
            var c2 = new GameObject("Contorno");
            c2.transform.SetParent(go.transform, false);
            c2.transform.localPosition = dirs[i] * size;
            var sr = c2.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = mat;
            sr.color = c;
            sr.sortingOrder = o.src.sortingOrder - 1;
            o.copies[i] = sr;
        }
        return o;
    }

    public void SetColor(Color c) { foreach (var s in copies) s.color = c; }

    void LateUpdate()
    {
        foreach (var s in copies) { s.sprite = src.sprite; s.flipX = src.flipX; }
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
        t.lineSpacing = 1.5f; // a fonte pixel fica apertada com o espacamento padrao
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
