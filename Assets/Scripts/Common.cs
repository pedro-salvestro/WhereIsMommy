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

    // Partida em andamento: fase atual (0 a 5), pontos acumulados e moedas na carteira
    // (as moedas passam de uma fase para a outra e sao gastas na lojinha).
    public static int Level = 0;
    public static int TotalScore = 0;
    public static int Coins = 0;

    public static void NewRun() { Level = 0; TotalScore = 0; Coins = 0; }

    public static string ModeName(GameMode m) =>
        m == GameMode.Historia ? "HISTÓRIA" : m == GameMode.ContraOTempo ? "CONTRA O TEMPO" : "LIVRE";

    public static string DifName(Dificuldade d) =>
        d == Dificuldade.Facil ? "FÁCIL" : d == Dificuldade.Normal ? "NORMAL" : d == Dificuldade.Dificil ? "DIFÍCIL" : "EXTRA HARD";

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

// Quanto cada fase e mais dificil: mais gente, mapas maiores, camera mais fechada.
// A dificuldade escolhida no menu muda o tempo, quantas mulheres parecidas com a mamae aparecem e a ordem das dicas.
public struct Difficulty
{
    public int Guards, Lookalikes, Coins, Totems, Crowd, BigCops, Kidnappers;
    public float GuardSpeed, CameraSize, Seconds, MomMovesEvery;
    public bool Kidnapper, Events;
    public Dificuldade Dif;
    public bool Dark => Dif == Dificuldade.ExtraHard;
    public const float HitSlowSeconds = 2.5f, HitSlowFactor = 0.4f;   // esbarrar num seguranca: fica lento por mais tempo
    public const float WrongMomPenalty = 3f;   // segundos perdidos ao falar com a mamae errada
    public const float DarkRadius = 2.1f;      // no Extra Hard, quanto a crianca enxerga em volta (unidades)
    public const float CaughtSeconds = 5f;     // tempo preso quando o policial grandao pega a crianca

    // FACIL: varios totens; NORMAL: so 1 totem de dica; DIFICIL e EXTRA HARD: sem totens.
    // Pista de gelo (ultima fase): sem dicas em nenhuma dificuldade.
    static int TotemsBy(int level, Dificuldade dif)
    {
        if (level >= 5) return 0;
        if (dif == Dificuldade.Facil) return new[] { 3, 3, 3, 3, 4 }[level];
        if (dif == Dificuldade.Normal) return 1;
        return 0;
    }

    public static Difficulty For(int level, GameMode mode, Dificuldade dif)
    {
        level = Mathf.Clamp(level, 0, 5);
        int di = (int)dif;
        bool easy = dif == Dificuldade.Facil;
        var d = new Difficulty
        {
            Dif = dif,
            Guards = new[] { 3, 4, 4, 5, 6, 6 }[level] + (easy ? -1 : 0),
            Lookalikes = (new[] { 2, 3, 4, 5, 6, 7 }[level] + new[] { 0, 2, 4, 4 }[di]) * 3,   // 3x mais mamaes parecidas
            Coins = new[] { 6, 7, 8, 9, 10, 0 }[level],   // ultima fase: sem moedas (nao tem loja)
            Totems = TotemsBy(level, dif),   // pista de gelo: sem dicas, o desafio e alcancar a mamae
            Crowd = new[] { 12, 16, 20, 26, 32, 40 }[level] + (easy ? -4 : 0),
            BigCops = new[] { 0, 1, 1, 1, 2, 2 }[level] + 2 + new[] { 0, 0, 1, 2 }[di],   // +2 em toda fase, mais no dificil
            Kidnapper = level >= 1,
            // mulheres de preto: 2 nas fases 2 e 3, 3 nas fases 4 a 6 (uma a menos no Facil, uma a mais no Dificil e no Extra Hard)
            Kidnappers = level < 1 ? 0 : Mathf.Max(1, (level <= 2 ? 2 : 3) + (easy ? -1 : di >= 2 ? 1 : 0)),
            Events = !easy,   // apagao (lugares fechados) e chuva (lugares abertos)
            GuardSpeed = new[] { 1.3f, 1.4f, 1.5f, 1.6f, 1.7f, 1.7f }[level],
            CameraSize = new[] { 3.4f, 3.3f, 3.2f, 3.1f, 3.0f, 3.0f }[level],
            MomMovesEvery = easy ? 0f : new[] { 0f, 0f, 0f, 60f, 60f, 0f }[level],
        };
        // tempo base (NORMAL, HISTORIA): os mapas crescem a cada fase
        float t = new[] { 70f, 75f, 85f, 95f, 110f, 120f }[level];
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
    AudioSource music, sfx, ambient;
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

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
            inst.ambient = go.AddComponent<AudioSource>();
            inst.ambient.loop = true;
            inst.ambient.volume = 0.45f;
        }
        return inst;
    }

    static AudioClip Clip(string name)
    {
        if (!clips.TryGetValue(name, out var c)) { c = Resources.Load<AudioClip>("Audio/" + name); clips[name] = c; }
        return c;
    }

    public static void Sfx(string name, float volume = 1f)
    {
        var c = Clip(name);
        if (c != null) Get().sfx.PlayOneShot(c, volume);
    }

    // som de fundo de cada lugar (gente conversando, passarinhos, agua...), em loop por baixo da musica
    public static void Ambient(string name)
    {
        var a = Get();
        var c = Clip(name);
        if (a.ambient.clip == c && a.ambient.isPlaying) return;
        a.ambient.clip = c;
        if (c != null) a.ambient.Play(); else a.ambient.Stop();
    }

    public static void StopAmbient() { Get().ambient.Stop(); }

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
// Quando o personagem anda, troca entre 4 quadros de passo (gerados a partir do desenho parado:
// uma perna sobe, o corpo sobe 1 pixel, depois a outra perna).
[RequireComponent(typeof(SpriteRenderer))]
public class DirectionalSprite : MonoBehaviour
{
    static readonly string[] Names = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
    static readonly Dictionary<Sprite, Sprite[]> walkCache = new Dictionary<Sprite, Sprite[]>();
    Sprite[] sprites;
    SpriteRenderer sr;
    int dirIdx = 6, frame;
    float animT;
    Vector3 lastPos;
    bool hasLast;

    public float stepRate = 8f;               // quadros por segundo andando
    public System.Action OnStep;              // chamado quando um pe toca o chao (som de passo)
    public bool Walking { get; private set; }

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
        dirIdx = idx % 8;
        Show();
    }

    void Show()
    {
        if (sprites == null || sr == null) return;
        var baseSprite = sprites[dirIdx];
        if (baseSprite == null) return;
        if (frame == 0 || frame == 2) { sr.sprite = baseSprite; return; }
        var f = WalkFrames(baseSprite);
        sr.sprite = f != null ? f[frame == 1 ? 0 : 1] : baseSprite;
    }

    void LateUpdate()
    {
        if (sprites == null) return;
        var p = transform.position;
        float dt = Time.deltaTime;
        if (!hasLast || dt <= 0f) { lastPos = p; hasLast = true; return; }
        float speed = ((Vector2)(p - lastPos)).magnitude / dt;
        lastPos = p;
        bool walking = speed > 0.25f && speed < 40f;   // muito rapido = foi teletransportado
        Walking = walking;
        int old = frame;
        if (walking)
        {
            animT += dt * stepRate * Mathf.Clamp(speed / 2.5f, 0.7f, 1.6f);
            frame = Mathf.FloorToInt(animT) % 4;
        }
        else { animT = 0f; frame = 0; }
        if (frame != old)
        {
            Show();
            if (walking && (frame == 1 || frame == 3)) OnStep?.Invoke();
        }
    }

    // dois quadros de passo para um desenho: [perna esquerda levantada, perna direita levantada]
    static Sprite[] WalkFrames(Sprite s)
    {
        if (walkCache.TryGetValue(s, out var done)) return done;
        Sprite[] outp = null;
        try
        {
            var tr = s.textureRect;
            int x0 = Mathf.FloorToInt(tr.x), y0 = Mathf.FloorToInt(tr.y);
            int w = Mathf.RoundToInt(tr.width), h = Mathf.RoundToInt(tr.height);
            var src = s.texture.GetPixels(x0, y0, w, h);
            int bot = h, top = -1, left = w, right = -1;
            for (int k = 0; k < src.Length; k++)
                if (src[k].a > 0.5f)
                {
                    int x = k % w, y = k / w;
                    if (y < bot) bot = y; if (y > top) top = y;
                    if (x < left) left = x; if (x > right) right = x;
                }
            if (top > bot)
            {
                int legTop = bot + Mathf.Max(3, Mathf.RoundToInt((top - bot) * 0.2f));
                float cx = (left + right) / 2f;
                outp = new Sprite[2];
                for (int side = 0; side < 2; side++)
                {
                    var px = new Color[src.Length];
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            Color c;
                            bool lifted = (side == 0) ? x < cx : x >= cx;
                            if (y > legTop) c = Get(src, w, h, x, y - 1);          // corpo sobe 1 px
                            else if (lifted) c = Get(src, w, h, x, y - 2);         // perna levantada
                            else c = src[y * w + x];                               // perna de apoio fica no chao
                            if (y == legTop + 1 && c.a < 0.5f) c = src[y * w + x]; // nao abre buraco na cintura
                            px[y * w + x] = c;
                        }
                    var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                    tex.SetPixels(px);
                    tex.Apply();
                    Vector2 piv = s.pivot - s.textureRectOffset;
                    outp[side] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(Mathf.Clamp01(piv.x / w), Mathf.Clamp01(piv.y / h)), s.pixelsPerUnit);
                }
            }
        }
        catch { outp = null; }   // desenho nao legivel: fica so o quadro parado
        walkCache[s] = outp;
        return outp;
    }

    static Color Get(Color[] px, int w, int h, int x, int y) => (x < 0 || y < 0 || x >= w || y >= h) ? Color.clear : px[y * w + x];
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

    // coracao (final da fase: a mamae abraca a crianca)
    static readonly string[] HeartRows = { ".XX.XX.", "XXXXXXX", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..." };
    static Sprite heart;
    public static Sprite Heart => heart ??= Make(9, 8, (x, y) =>
    {
        int ty = 6 - y, tx = x - 1;
        bool In(int a, int b) => b >= 0 && b < HeartRows.Length && a >= 0 && a < 7 && HeartRows[b][a] == 'X';
        if (In(tx, ty)) return (tx == 1 && ty == 1) ? new Color(1f, 0.75f, 0.8f) : new Color(0.95f, 0.2f, 0.35f);
        if (In(tx - 1, ty) || In(tx + 1, ty) || In(tx, ty - 1) || In(tx, ty + 1)) return new Color(0.35f, 0.05f, 0.1f);
        return Color.clear;
    }, 20, new Vector2(0.5f, 0.5f));

    // seta do megafone (aponta para a direita; o jogo gira)
    static Sprite arrow, fog;
    public static Sprite Arrow => arrow ??= Make(20, 13, (x, y) =>
    {
        float cy = 6f, d = Mathf.Abs(y - cy);
        bool head = x >= 10 && d <= (19 - x) * 0.65f;
        bool tail = x < 11 && d <= 1.6f;
        bool inner = (x >= 11 && d <= (18 - x) * 0.65f - 1f) || (x >= 1 && x < 12 && d <= 0.6f);
        if (inner) return new Color(1f, 0.45f, 0.75f);
        if (head || tail) return new Color(0.2f, 0.05f, 0.12f);
        return Color.clear;
    }, 16, new Vector2(0.5f, 0.5f));

    // vidro embacado: nevoa irregular com gotas
    public static Sprite Fog => fog ??= MakeFog();
    static Sprite MakeFog()
    {
        const int w = 320, h = 180;
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.025f, y * 0.025f) * 0.6f + Mathf.PerlinNoise(x * 0.09f + 30f, y * 0.09f) * 0.4f;
                px[y * w + x] = new Color(0.86f, 0.9f, 0.94f, Mathf.Lerp(0.82f, 0.98f, n));   // bem embacado
            }
        var rnd = new System.Random(7);
        for (int i = 0; i < 260; i++)
        {
            int cx = rnd.Next(w), cy = rnd.Next(h);
            float r = 1.5f + (float)rnd.NextDouble() * 4f;
            for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
                for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
                {
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (d > r) continue;
                    // gota: centro limpo (da para ver atras) e borda mais clara
                    px[y * w + x] = d > r - 1.2f ? new Color(1f, 1f, 1f, 0.95f) : new Color(0.8f, 0.88f, 0.95f, 0.45f);
                }
        }
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        t.SetPixels(px);
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
    }
}

// Troca as cores dos desenhos dos personagens em tempo de jogo:
// roupas das "mulheres parecidas", da multidao, da sequestradora e o policial grandao (cabelo curto e bigode).
public static class Recolor
{
    static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();

    // px: pixels do recorte (linha 0 = embaixo), w x h; dir: 0 leste, 2 norte, 4 oeste, 6 sul (ordem do DirectionalSprite)
    public delegate void PixelFn(Color[] px, int w, int h, int dir);

    public static Sprite[] Make(string folder, string key, PixelFn fn)
    {
        key = folder + ":" + key;
        if (cache.TryGetValue(key, out var done)) return done;
        var src = DirectionalSprite.LoadAll(folder);
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
            fn(px, w, h, i);
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

    static void Hsv(Color c, out float deg, out float sat, out float v)
    {
        Color.RGBToHSV(c, out float hu, out sat, out v);
        deg = hu * 360f;
    }

    // val < 1 escurece a peca (val baixo = quase preto, sem cor)
    static Color Shift(Color c, float hueDeg, float sat, float v, float val = 1f)
    {
        float s = val < 0.6f ? 0.12f : Mathf.Max(sat, 0.75f);   // cores vivas: fica mais facil ver a diferenca
        var n = Color.HSVToRGB(Mathf.Repeat(hueDeg, 360f) / 360f, s, Mathf.Clamp01(v * val));
        n.a = c.a;
        return n;
    }

    // cores de cabelo: 0 original (castanho), 1 loiro, 2 preto, 3 ruivo, 4 grisalho
    static readonly Vector3[] HairLook = { Vector3.zero, new Vector3(42f, 0.55f, 1.7f), new Vector3(20f, 0.15f, 0.45f), new Vector3(12f, 0.85f, 1.25f), new Vector3(30f, 0.08f, 1.6f) };

    static bool MomHair(float deg, float sat, float v) => (deg <= 25f || deg >= 345f) && sat > 0.45f && v > 0.12f && v < 0.6f;

    // shirtHue/skirtHue em graus (0-360); valor < 0 mantem a cor original daquela peca; hair: ver HairLook
    public static Sprite[] Mom(float shirtHue, float skirtHue, float shirtVal = 1f, float skirtVal = 1f, int hair = 0)
    {
        return Make("Mae", shirtHue + "/" + skirtHue + "/" + shirtVal + "/" + skirtVal + "/" + hair, (px, w, h, dir) =>
        {
            for (int k = 0; k < px.Length; k++)
            {
                var c = px[k];
                if (c.a <= 0f) continue;
                Hsv(c, out float deg, out float sat, out float v);
                if (shirtHue >= 0f && deg >= 335f && sat > 0.3f) px[k] = Shift(c, shirtHue, sat, v, shirtVal);
                else if (skirtHue >= 0f && deg >= 210f && deg <= 245f && sat > 0.15f) px[k] = Shift(c, skirtHue, sat, v, skirtVal);
                else if (hair > 0 && hair < HairLook.Length && MomHair(deg, sat, v))
                {
                    var hl = HairLook[hair];
                    var n = Color.HSVToRGB(hl.x / 360f, hl.y, Mathf.Clamp01(v * hl.z));
                    n.a = c.a;
                    px[k] = n;
                }
            }
        });
    }

    // crianca da multidao: blusa (azul claro) e calca (azul escuro) com outras cores
    public static Sprite[] Kid(float shirtHue, float pantsHue)
    {
        return Make("Crianca", shirtHue + "/" + pantsHue, (px, w, h, dir) =>
        {
            for (int k = 0; k < px.Length; k++)
            {
                var c = px[k];
                if (c.a <= 0f) continue;
                Hsv(c, out float deg, out float sat, out float v);
                if (deg < 195f || deg > 250f || sat <= 0.15f) continue;
                px[k] = v >= 0.45f ? Shift(c, shirtHue, sat, v) : Shift(c, pantsHue, sat, v, 1.15f);
            }
        });
    }

    // homem da multidao: o desenho do policial sem farda (bone e camisa coloridos), de cabelo curto
    public static Sprite[] Man(float shirtHue, float pantsHue, bool beard)
    {
        return Make("Policial", "civil/" + shirtHue + "/" + pantsHue + "/" + beard, (px, w, h, dir) =>
        {
            Bounds(px, w, h, out int bot, out int top);
            for (int k = 0; k < px.Length; k++)
            {
                var c = px[k];
                if (c.a <= 0f) continue;
                Hsv(c, out float deg, out float sat, out float v);
                if (deg < 195f || deg > 250f || sat <= 0.2f) continue;
                float ry = (k / w - bot) / (float)Mathf.Max(1, top - bot);
                px[k] = ry < 0.3f ? Shift(c, pantsHue, sat, v, 1.25f) : Shift(c, shirtHue, sat, Mathf.Min(1f, v * 1.9f));
            }
            ShortHair(px, w, h);
            if (beard) Mustache(px, w, h, dir);
        });
    }

    // sequestradora: roupa toda preta, capuz escuro no lugar do cabelo e oculos escuros
    public static Sprite[] Kidnapper()
    {
        return Make("Mae", "sequestradora", (px, w, h, dir) =>
        {
            for (int k = 0; k < px.Length; k++)
            {
                var c = px[k];
                if (c.a <= 0f) continue;
                Hsv(c, out float deg, out float sat, out float v);
                if (deg >= 335f && sat > 0.3f) px[k] = Shift(c, 0f, sat, v, 0.28f);
                else if (deg >= 210f && deg <= 245f && sat > 0.15f) px[k] = Shift(c, 0f, sat, v, 0.3f);
                else if (MomHair(deg, sat, v) || (!IsSkin(c) && (deg <= 40f || deg >= 345f) && sat > 0.45f && v > 0.12f))
                { var n = Color.HSVToRGB(0f, 0.05f, v * 0.42f); n.a = c.a; px[k] = n; }   // capuz escuro, sem reflexo laranja
            }
            Sunglasses(px, w, h, dir);
        });
    }

    static bool IsSkin(Color c)
    {
        if (c.a <= 0.5f) return false;
        Hsv(c, out float deg, out float sat, out float v);
        return deg > 7f && deg < 32f && sat > 0.2f && sat < 0.6f && v > 0.75f;
    }

    // oculos escuros: acha a linha dos olhos (pontos escuros no meio da pele do rosto) e pinta uma faixa preta
    static void Sunglasses(Color[] px, int w, int h, int dir)
    {
        if (dir == 1 || dir == 2 || dir == 3) return; // de costas nao aparece o rosto
        Bounds(px, w, h, out int bot, out int top);
        int bestRow = -1, bestN = 0;
        for (int y = bot + (top - bot) / 2; y <= top; y++)
        {
            int n = 0;
            for (int x = 1; x < w - 1; x++)
            {
                var c = px[y * w + x];
                Hsv(c, out _, out _, out float v);
                if (c.a > 0.5f && v < 0.35f && (IsSkin(px[y * w + x - 1]) || IsSkin(px[y * w + x + 1]))) n++;
            }
            if (n > bestN) { bestN = n; bestRow = y; }
        }
        if (bestRow < 0 || bestN < 2) return;
        var black = new Color(0.06f, 0.06f, 0.08f, 1f);
        for (int y = bestRow - 1; y <= bestRow; y++)
        {
            if (y < 0) continue;
            int minX = w, maxX = -1;
            for (int x = 0; x < w; x++) if (IsSkin(px[y * w + x]) || (px[y * w + x].a > 0.5f && x > 0 && x < w - 1 && IsSkin(px[y * w + x - 1]) && IsSkin(px[y * w + x + 1]))) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); }
            for (int x = minX; x <= maxX; x++) if (px[y * w + x].a > 0.5f) px[y * w + x] = black;
        }
    }

    // policial grandao: farda igual, cabelo curto e escuro, bigode
    public static Sprite[] BigCop()
    {
        return Make("Policial", "grandao", (px, w, h, dir) =>
        {
            ShortHair(px, w, h);
            Mustache(px, w, h, dir);
        });
    }

    static void Bounds(Color[] px, int w, int h, out int bot, out int top)
    {
        bot = h; top = 0;
        for (int k = 0; k < px.Length; k++)
            if (px[k].a > 0.5f) { int y = k / w; if (y < bot) bot = y; if (y > top) top = y; }
        if (bot > top) { bot = 0; top = h - 1; }
    }

    static bool IsHair(Color c)
    {
        if (c.a <= 0.5f) return false;
        Hsv(c, out float deg, out float sat, out float v);
        return (deg <= 25f || deg >= 345f) && sat > 0.45f && v < 0.5f && v > 0.12f;
    }

    static bool IsLine(Color c) { Hsv(c, out _, out _, out float v); return c.a > 0.5f && v < 0.13f; }

    // corta o cabelo comprido (tudo que e cabelo abaixo da altura das orelhas) e refaz o contorno preto
    static void ShortHair(Color[] px, int w, int h)
    {
        Bounds(px, w, h, out int bot, out int top);
        float cut = bot + (top - bot) * 0.74f;
        var removed = new bool[px.Length];
        for (int k = 0; k < px.Length; k++)
            if (k / w < cut && IsHair(px[k])) { removed[k] = true; px[k] = Color.clear; }
        bool Body(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && px[y * w + x].a > 0.5f && !IsLine(px[y * w + x]);
        // contorno que ficou solto no ar
        var drop = new List<int>();
        for (int k = 0; k < px.Length; k++)
        {
            if (!IsLine(px[k])) continue;
            int x = k % w, y = k / w;
            if (!Body(x + 1, y) && !Body(x - 1, y) && !Body(x, y + 1) && !Body(x, y - 1)) drop.Add(k);
        }
        foreach (var k in drop) px[k] = Color.clear;
        // contorno novo em volta de onde o cabelo saiu
        var line = new Color(0.1f, 0.08f, 0.08f, 1f);
        var add = new List<int>();
        for (int k = 0; k < px.Length; k++)
        {
            if (!removed[k] || px[k].a > 0.5f) continue;
            int x = k % w, y = k / w;
            if (Body(x + 1, y) || Body(x - 1, y) || Body(x, y + 1) || Body(x, y - 1)) add.Add(k);
        }
        foreach (var k in add) px[k] = line;
        // o cabelo que sobrou fica bem escuro
        for (int k = 0; k < px.Length; k++)
            if (IsHair(px[k])) { var c = px[k]; px[k] = new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, c.a); }
    }

    // bigode: so quando o rosto aparece (de frente ou de lado para baixo)
    static void Mustache(Color[] px, int w, int h, int dir)
    {
        if (dir != 5 && dir != 6 && dir != 7) return;
        Bounds(px, w, h, out int bot, out int top);
        int low = h, minX = w, maxX = -1;
        var skin = new bool[px.Length];
        for (int k = 0; k < px.Length; k++)
        {
            var c = px[k];
            if (c.a <= 0.5f) continue;
            float ry = (k / w - bot) / (float)Mathf.Max(1, top - bot);
            if (ry <= 0.42f || ry >= 0.66f) continue;
            Hsv(c, out float deg, out float sat, out float v);
            if (deg > 7f && deg < 32f && sat > 0.2f && sat < 0.55f && v > 0.75f) { skin[k] = true; if (k / w < low) low = k / w; }
        }
        if (low >= h) return;
        int row = low + 2; // linha y cresce para cima: duas acima da parte mais baixa do rosto
        if (row >= h) return;
        for (int x = 0; x < w; x++) if (skin[row * w + x]) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); }
        if (maxX - minX < 3) return;
        float cx = (minX + maxX) / 2f;
        var brown = new Color(0.24f, 0.14f, 0.1f, 1f);
        for (int x = minX; x <= maxX; x++)
            if (skin[row * w + x] && Mathf.Abs(x - cx) <= 2.5f * w / 48f) px[row * w + x] = brown;
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
