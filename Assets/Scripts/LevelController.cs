using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Monta a fase atual em codigo (cenario, paredes, personagens, moedas, totens, lojinha, HUD, mapa)
// e controla o fluxo: apresentacao da mamae, tempo, dicas, lojinha, vitoria, derrota, pausa e proxima fase.
public class LevelController : MonoBehaviour
{
    enum State { Intro, Playing, Shop, Won, Lost, Paused }

    // Cada cenario tem 2752x1536 px com 100 px por unidade (27.52 x 15.36). Nas fases 4 e 5 eles sao empilhados.
    float WorldW, WorldH;
    const float Cell = 0.25f;          // grade usada para saber onde da para andar
    const float Clearance = 0.22f;     // folga em volta das paredes para uma celula contar como chao
    const float FeetOffset = 0.2f;     // os pes do personagem ficam abaixo do centro do sprite
    const float TalkDistance = 1.1f;

    static readonly Color Cream = new Color(1f, 0.93f, 0.78f);
    static readonly Color Gold = new Color(1f, 0.85f, 0.3f);
    static readonly Color Pink = new Color(1f, 0.35f, 0.7f);
    static readonly Color ShopGreen = new Color(0.35f, 0.9f, 0.45f);

    State state = State.Intro;
    LevelDef def;
    Difficulty dif;
    GameMode mode;
    int levelIndex, endedFrame = -1, startCoins;
    float introShownAt;

    float timeLeft, elapsed, momMoveAt, nextTick, bannerUntil, youMarkerUntil;
    int hits, coinsTaken, mistakes;

    Camera cam;
    float camSize;
    Transform player, mom, shop;
    PlayerController pc;
    Sprite[] momSprites;
    readonly List<Transform> lookalikes = new List<Transform>();
    readonly List<Transform> coins = new List<Transform>();
    readonly List<Totem> totems = new List<Totem>();
    GameObject prompt, youMarker;
    Transform darkness;       // Extra Hard: escuridao em volta da crianca
    float darkScale = 1f, wrongMomUntil;

    // itens da lojinha (valem so na fase em que foram comprados)
    int shields;
    bool boughtShield, boughtShoes, boughtGlasses, boughtClock, boughtHint;

    // grade de chao andavel
    int gw, gh;
    bool[,] walk;
    int[,] dist;
    readonly List<Vector2Int> reachable = new List<Vector2Int>();

    // HUD
    Text timerText, leftText, rightText, bannerText, centerText, hintText;
    Image bannerPanel, overlay;
    RectTransform minimap, miniPlayer, miniShop;
    readonly List<RectTransform> miniTotems = new List<RectTransform>();
    GameObject introRoot, bigMapRoot, shopRoot;
    RectTransform bigMap, bigPlayer;
    Text shopText;
    int shopSel;

    class Totem { public Transform t; public SpriteRenderer sr; public bool used; }

    class ShopItem { public string name, desc; public int price; public System.Func<bool> owned; public System.Action buy; }
    List<ShopItem> shopItems;

    // ---------------------------------------------------------------- montagem

    void Start()
    {
        Time.timeScale = 1f;
        UIKit.EnsureEventSystem();
        levelIndex = Mathf.Clamp(GameSettings.Level, 0, Levels.Count - 1);
        mode = GameSettings.Mode;
        def = Levels.Get(levelIndex);
        dif = Difficulty.For(levelIndex, mode, GameSettings.Dif);
        WorldW = def.WorldW;
        WorldH = def.WorldH;
        timeLeft = dif.Seconds;
        startCoins = GameSettings.Coins;

        momSprites = DressMom(def.MomShirt, def.MomSkirt);
        BuildWorld();
        BuildHud();
        BuildShopItems();
        AudioManager.StopMusic();
        AudioManager.Music("musica_fase");
        ShowIntro();
    }

    void BuildWorld()
    {
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        camSize = dif.CameraSize;
        cam.orthographicSize = camSize;
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.AddComponent<AudioListener>();

        // um ou mais cenarios, de cima para baixo
        for (int k = 0; k < def.Parts.Length; k++)
        {
            var bg = new GameObject("Cenario" + k).AddComponent<SpriteRenderer>();
            bg.sprite = Resources.Load<Sprite>(def.Parts[k]);
            bg.sortingOrder = -10;
            bg.transform.position = new Vector3(0f, WorldH / 2f - LevelDef.PartH * (k + 0.5f), 0f);
            // escurece um pouco o cenario para os personagens vivos se destacarem; nas fases 4 e 5 muda a luz
            bg.color = def.Tint;
        }

        var walls = new GameObject("Paredes");
        for (int i = 0; i < def.Walls.GetLength(0); i++)
        {
            var a = new Vector2(def.Walls[i, 0], def.Walls[i, 1]);
            var b = new Vector2(def.Walls[i, 2], def.Walls[i, 3]);
            var go = new GameObject("Wall");
            go.transform.SetParent(walls.transform, false);
            go.transform.position = (a + b) / 2f;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
        }
        Physics2D.SyncTransforms();
        BuildGrid();

        // Crianca: sai de um dos pontos da fase
        var sp = def.ChildSpawns[Random.Range(0, def.ChildSpawns.Length)];
        Vector2Int start = NearestFree(Cellof(sp));
        FloodFrom(start);
        var p = MakeCharacter("Crianca", "Crianca", FeetToPos(CellCenter(start)), 10);
        var rb = p.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var circle = p.AddComponent<CircleCollider2D>();
        circle.radius = 0.2f;
        circle.offset = new Vector2(0, -FeetOffset);
        pc = p.AddComponent<PlayerController>();
        Outline.Add(p, Gold, 0.045f);
        var ring = new GameObject("Anel").AddComponent<SpriteRenderer>();
        ring.sprite = GenSprites.Ring;
        ring.transform.SetParent(p.transform, false);
        ring.transform.localPosition = new Vector3(0, -0.38f, 0);
        ring.sortingOrder = 3;
        player = p.transform;
        youMarker = MakeWorldText(p.transform, "VOCE", new Vector3(0, 0.75f, 0), Gold);

        // Mamae: lugar sorteado, longe da crianca, com a roupa desta fase
        var momGo = MakeCharacter("Mamae", "Mae", FeetToPos(PickMomSpot()), 8);
        if (momSprites != null) momGo.GetComponent<DirectionalSprite>().Use(momSprites);
        mom = momGo.transform;
        Outline.Add(momGo, Color.white);
        momGo.AddComponent<IdleLookAround>();
        momMoveAt = dif.MomMovesEvery > 0 ? dif.MomMovesEvery : float.MaxValue;

        // Mulheres parecidas com a mamae (roupa parecida, mas nunca igual)
        for (int i = 0; i < dif.Lookalikes; i++)
        {
            var c = RandomFarCell(3f, 2.5f, lookalikes);
            var go = MakeCharacter("Parecida", "Mae", FeetToPos(CellCenter(c)), 7);
            DressLookalike(go, i);
            Outline.Add(go, Color.white);
            AddWalker(go, RandomRoute(c), 0.9f + Random.value * 0.4f, false);
            go.GetComponent<Collider2D>().isTrigger = true; // atravessavel: nao trava a passagem
            lookalikes.Add(go.transform);
        }

        // Segurancas fazendo ronda pelos corredores
        for (int i = 0; i < dif.Guards; i++)
        {
            var c = RandomFarCell(4f, 1.5f);
            var go = MakeCharacter("Seguranca", "Policial", FeetToPos(CellCenter(c)), 6);
            Outline.Add(go, new Color(0.55f, 0.75f, 1f));
            AddWalker(go, RandomRoute(c), dif.GuardSpeed * (0.85f + Random.value * 0.3f), true);
        }

        // Lojinha: perto do comeco, para dar tempo de juntar moedas e voltar
        var sc = RandomCellBetween(3.5f, 8f);
        var shopGo = new GameObject("Lojinha");
        shopGo.transform.position = CellCenter(sc) + Vector2.down * 0.1f;
        shopGo.transform.localScale = Vector3.one * 1.3f;
        var ssr = shopGo.AddComponent<SpriteRenderer>();
        ssr.sprite = GenSprites.Shop;
        ssr.sortingOrder = 5;
        MakeWorldText(shopGo.transform, "LOJINHA", new Vector3(0, 1.45f, 0), ShopGreen, 0.8f);
        var sbox = shopGo.AddComponent<BoxCollider2D>(); // a barraca e solida, ninguem atravessa
        sbox.size = new Vector2(0.95f, 0.5f);
        sbox.offset = new Vector2(0, 0.28f);
        shop = shopGo.transform;
        Physics2D.SyncTransforms();
        for (int dx = -3; dx <= 3; dx++)
            for (int dy = -1; dy <= 4; dy++)
            {
                int x = sc.x + dx, y = sc.y + dy;
                if (x >= 0 && y >= 0 && x < gw && y < gh) walk[x, y] = false;
            }
        FloodFrom(start);

        // Moedas e totens de dica
        var avoid = new List<Transform> { shop };
        for (int i = 0; i < dif.Coins; i++)
        {
            var c = RandomFarCell(2f, 1.2f, coins);
            var go = new GameObject("Moeda");
            go.transform.position = CellCenter(c);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GenSprites.Coin;
            sr.sortingOrder = 2;
            go.AddComponent<Spin>();
            coins.Add(go.transform);
        }
        for (int i = 0; i < dif.Totems; i++)
        {
            var spread = new List<Transform>(avoid);
            foreach (var tt in totems) spread.Add(tt.t);
            var c = RandomFarCell(3f, 5f, spread);
            var go = new GameObject("Totem");
            go.transform.position = CellCenter(c) + Vector2.down * 0.1f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GenSprites.TotemOn;
            sr.sortingOrder = 5;
            totems.Add(new Totem { t = go.transform, sr = sr });
        }

        // Placas com o nome de cada lugar, para as dicas fazerem sentido (no escuro do Extra Hard nao ha placas)
        if (!dif.Dark)
        {
            var signs = new GameObject("Placas");
            for (int i = 0; i < def.PlaceNames.Length; i++)
            {
                var s = MakeWorldText(signs.transform, ShortName(def.PlaceNames[i]), Vector3.zero, Cream, 0.75f);
                s.transform.position = def.PlacePos[i];
            }
        }

        if (dif.Dark)
        {
            var dk = new GameObject("Escuridao").AddComponent<SpriteRenderer>();
            dk.sprite = DarkSprite();
            dk.sortingOrder = 25; // por cima de tudo do cenario, abaixo dos textos
            darkness = dk.transform;
            darkness.position = player.position;
        }

        prompt = MakeWorldText(null, "APERTE E", Vector3.zero, Gold);
        prompt.SetActive(false);

        cam.transform.position = new Vector3(player.position.x, player.position.y, -10);
        ClampCamera();
    }

    GameObject MakeCharacter(string name, string folder, Vector2 pos, int order)
    {
        var go = new GameObject(name) { layer = 2 }; // Ignore Raycast: so as paredes contam nas consultas de fisica
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = order;
        go.AddComponent<DirectionalSprite>().Load(folder);
        go.transform.localScale = Vector3.one * 1.1f;
        return go;
    }

    void AddWalker(GameObject go, List<Vector2> route, float speed, bool guard)
    {
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var c = go.AddComponent<CircleCollider2D>();
        c.radius = 0.22f;
        c.offset = new Vector2(0, -FeetOffset);
        var w = go.AddComponent<Walker>();
        w.route = route;
        w.speed = speed;
        w.level = guard ? this : null;
    }

    GameObject MakeWorldText(Transform parent, string text, Vector3 local, Color color, float scale = 1f)
    {
        var root = new GameObject("Texto");
        if (parent != null) root.transform.SetParent(parent, false);
        root.transform.localPosition = local;
        root.transform.localScale = Vector3.one * scale / (parent != null ? parent.lossyScale.x : 1f);
        var go = new GameObject("Letras");
        go.transform.SetParent(root.transform, false);
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = UIKit.PixelFont;
        go.GetComponent<MeshRenderer>().material = tm.font.material;
        tm.characterSize = 0.03f;
        tm.fontSize = 48;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        go.GetComponent<MeshRenderer>().sortingOrder = 31;
        var back = new GameObject("Fundo").AddComponent<SpriteRenderer>();
        back.transform.SetParent(root.transform, false);
        back.sprite = GenSprites.White;
        back.color = new Color(0, 0, 0, 0.75f);
        back.transform.localScale = new Vector3(0.15f * text.Length + 0.2f, 0.28f, 1f);
        back.sortingOrder = 30;
        return root;
    }

    // ---------------------------------------------------------------- roupas

    static Sprite[] DressMom(float shirt, float skirt)
    {
        if (shirt < 0f && skirt < 0f) return null; // roupa original do desenho
        try { return Recolor.Mom(shirt, skirt); }
        catch (System.Exception e) { Debug.LogWarning("Recolor falhou: " + e.Message); return null; }
    }

    static readonly float[] Hues = { 0f, 25f, 50f, 90f, 125f, 180f, 210f, 250f, 280f, 320f, 345f };

    static float HueGap(float a, float b) { float d = Mathf.Abs(Mathf.Repeat(a - b, 360f)); return Mathf.Min(d, 360f - d); }

    static List<float> FarHues(float from)
    {
        var l = new List<float>();
        foreach (var h in Hues) if (HueGap(h, from) >= 40f) l.Add(h);
        return l;
    }

    void DressLookalike(GameObject go, int i)
    {
        // fases 1 e 2: blusa e saia diferentes; 3 e 4: mesma saia da mamae; 5: metade com a mesma blusa e saia diferente
        float momShirt = def.MomShirt < 0f ? 345f : def.MomShirt;
        float momSkirt = def.MomSkirt < 0f ? 225f : def.MomSkirt;
        bool sameShirt = levelIndex >= 4 && i % 2 == 1;
        bool sameSkirt = !sameShirt && levelIndex >= 2;
        var shirts = FarHues(momShirt);
        var skirts = FarHues(momSkirt);
        float shirt = sameShirt ? def.MomShirt : shirts[(i * 5 + levelIndex) % shirts.Count];
        float skirt = sameSkirt ? def.MomSkirt : skirts[(i * 3 + 1 + levelIndex) % skirts.Count];
        var sprites = DressMom(shirt, skirt);
        if (sprites != null) go.GetComponent<DirectionalSprite>().Use(sprites);
        else go.GetComponent<SpriteRenderer>().color = Color.HSVToRGB(Mathf.Max(shirt, 0f) / 360f, 0.35f, 1f);
    }

    // ---------------------------------------------------------------- grade de chao

    Vector2Int Cellof(Vector2 feet) => new Vector2Int(
        Mathf.Clamp(Mathf.FloorToInt((feet.x + WorldW / 2f) / Cell), 0, gw - 1),
        Mathf.Clamp(Mathf.FloorToInt((feet.y + WorldH / 2f) / Cell), 0, gh - 1));

    Vector2 CellCenter(Vector2Int c) => new Vector2(-WorldW / 2f + (c.x + 0.5f) * Cell, -WorldH / 2f + (c.y + 0.5f) * Cell);
    static Vector2 FeetToPos(Vector2 feet) => feet + Vector2.up * FeetOffset;
    static Vector2 Feet(Transform t) => (Vector2)t.position - Vector2.up * FeetOffset;

    void BuildGrid()
    {
        gw = Mathf.CeilToInt(WorldW / Cell);
        gh = Mathf.CeilToInt(WorldH / Cell);
        walk = new bool[gw, gh];
        for (int x = 0; x < gw; x++)
            for (int y = 0; y < gh; y++)
                walk[x, y] = Physics2D.OverlapCircle(CellCenter(new Vector2Int(x, y)), Clearance) == null;
    }

    Vector2Int NearestFree(Vector2Int c)
    {
        for (int r = 0; r < 30; r++)
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    int x = c.x + dx, y = c.y + dy;
                    if (x >= 0 && y >= 0 && x < gw && y < gh && walk[x, y]) return new Vector2Int(x, y);
                }
        return c;
    }

    void FloodFrom(Vector2Int s)
    {
        dist = new int[gw, gh];
        for (int x = 0; x < gw; x++) for (int y = 0; y < gh; y++) dist[x, y] = -1;
        var q = new Queue<Vector2Int>();
        dist[s.x, s.y] = 0;
        q.Enqueue(s);
        reachable.Clear();
        Vector2Int[] n = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            reachable.Add(c);
            foreach (var d in n)
            {
                var m = c + d;
                if (m.x < 0 || m.y < 0 || m.x >= gw || m.y >= gh || !walk[m.x, m.y] || dist[m.x, m.y] >= 0) continue;
                dist[m.x, m.y] = dist[c.x, c.y] + 1;
                q.Enqueue(m);
            }
        }
    }

    bool Reachable(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < gw && c.y < gh && dist[c.x, c.y] >= 0;

    Vector2 PickMomSpot()
    {
        // candidatos da fase que da para alcancar, do mais longe para o mais perto (distancia andando)
        var cands = new List<Vector2Int>();
        foreach (var s in def.MomSpots)
        {
            var c = NearestFree(Cellof(s));
            if (Reachable(c) && (mom == null || Vector2.Distance(CellCenter(c), Feet(mom)) > 4f)) cands.Add(c);
        }
        if (cands.Count < 3)
            for (int i = 0; i < 40; i++) cands.Add(reachable[Random.Range(0, reachable.Count)]);
        cands.Sort((a, b) => dist[b.x, b.y].CompareTo(dist[a.x, a.y]));
        // fase 1: sorteia entre a metade mais longe; nas outras, entre as mais longe
        float frac = new[] { 0.5f, 0.45f, 0.4f, 0.4f, 0.35f }[Mathf.Min(levelIndex, 4)];
        int n = Mathf.Max(1, Mathf.RoundToInt(cands.Count * frac));
        return CellCenter(cands[Random.Range(0, n)]);
    }

    Vector2Int RandomFarCell(float minFromPlayer, float minSpacing, List<Transform> others = null)
    {
        Vector2 pf = Feet(player);
        for (int i = 0; i < 200; i++)
        {
            var c = reachable[Random.Range(0, reachable.Count)];
            var p = CellCenter(c);
            if (Vector2.Distance(p, pf) < minFromPlayer) continue;
            bool ok = true;
            if (others != null) foreach (var o in others) if (Vector2.Distance(o.position, p) < minSpacing) { ok = false; break; }
            if (ok) return c;
        }
        return reachable[Random.Range(0, reachable.Count)];
    }

    Vector2Int RandomCellBetween(float min, float max)
    {
        Vector2 pf = Feet(player);
        for (int i = 0; i < 400; i++)
        {
            var c = reachable[Random.Range(0, reachable.Count)];
            float d = Vector2.Distance(CellCenter(c), pf);
            // precisa de espaco em volta para a barraca nao ficar em cima de uma parede
            if (d >= min && d <= max && Free(c, 2, 7)) return c;
        }
        return RandomFarCell(min, 0f);
    }

    // celulas livres em volta (r para os lados e para baixo, up para cima, onde fica o desenho da barraca)
    bool Free(Vector2Int c, int r, int up)
    {
        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= up; dy++)
            {
                int x = c.x + dx, y = c.y + dy;
                if (x < 0 || y < 0 || x >= gw || y >= gh || !walk[x, y]) return false;
            }
        return true;
    }

    List<Vector2> RandomRoute(Vector2Int start)
    {
        // ronda de ida e volta entre pontos ligados em linha reta, sem atravessar paredes
        var pts = new List<Vector2> { CellCenter(start) };
        int want = Random.Range(2, 5);
        for (int i = 0; i < 80 && pts.Count < want; i++)
        {
            var last = pts[pts.Count - 1];
            var p = CellCenter(reachable[Random.Range(0, reachable.Count)]);
            float d = Vector2.Distance(last, p);
            if (d < 2f || d > 9f) continue;
            if (Physics2D.CircleCast(last, 0.24f, (p - last).normalized, d)) continue;
            pts.Add(p);
        }
        if (pts.Count == 1) pts.Add(pts[0]);
        for (int i = 0; i < pts.Count; i++) pts[i] = FeetToPos(pts[i]);
        return pts;
    }

    // ---------------------------------------------------------------- HUD

    void BuildHud()
    {
        var canvas = UIKit.MakeCanvas("HUD", 10);
        var t = canvas.transform;
        UIKit.Panel(t, new Color(0, 0, 0, 0.6f), new Vector2(0.5f, 1f), new Vector2(0, -26), new Vector2(2400, 52));
        timerText = UIKit.Label(t, "", 24, Cream, new Vector2(0.5f, 1f), new Vector2(0, -26), new Vector2(300, 48));
        leftText = UIKit.Label(t, "", 11, Cream, new Vector2(0f, 1f), new Vector2(250, -26), new Vector2(480, 48), TextAnchor.MiddleLeft);
        rightText = UIKit.Label(t, "", 11, Cream, new Vector2(1f, 1f), new Vector2(-250, -26), new Vector2(480, 48), TextAnchor.MiddleRight);

        // dica atual, logo abaixo da barra
        hintText = UIKit.Label(t, "", 11, Gold, new Vector2(0f, 1f), new Vector2(500, -66), new Vector2(980, 24), TextAnchor.MiddleLeft);

        bannerPanel = UIKit.Panel(t, new Color(0, 0, 0, 0.8f), new Vector2(0.5f, 1f), new Vector2(-100, -110), new Vector2(860, 64));
        bannerText = UIKit.Label(bannerPanel.transform, "", 14, Gold, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840, 60));
        bannerPanel.gameObject.SetActive(false);

        // minimapa: o cenario em miniatura, com a crianca, os totens e a lojinha (a mamae NAO aparece)
        var ms = Fit(256f, 220f);
        var frame = UIKit.Panel(t, new Color(0.35f, 0.2f, 0.1f, 0.95f), new Vector2(1f, 1f),
            new Vector2(-8f - (ms.x + 12f) / 2f, -57f - (ms.y + 12f) / 2f), ms + new Vector2(12, 12));
        minimap = UIKit.Rect(frame.transform, "Minimapa", new Vector2(0.5f, 0.5f), Vector2.zero, ms);
        MapImage(minimap);
        foreach (var tt in totems) miniTotems.Add(Dot(minimap, new Color(0.2f, 0.7f, 1f), 8));
        miniShop = Dot(minimap, ShopGreen, 9);
        miniPlayer = Dot(minimap, Gold, 10);
        UIKit.Label(t, "TAB: MAPA GRANDE", 9, Cream, new Vector2(1f, 1f), new Vector2(-8f - (ms.x + 12f) / 2f, -57f - ms.y - 12f - 12f), new Vector2(268, 20));

        // retrato da mamae no canto, para nunca esquecer quem procurar
        var card = UIKit.Panel(t, new Color(0, 0, 0, 0.65f), new Vector2(0f, 0f), new Vector2(120, 70), new Vector2(230, 130));
        UIKit.Picture(card.transform, momSprites != null ? momSprites[6] : Resources.Load<Sprite>("Art/Mae/south"),
            new Vector2(0f, 0.5f), new Vector2(45, 0), new Vector2(80, 110)).preserveAspect = true;
        UIKit.Label(card.transform, "MAMAE:\n" + def.Outfit.Replace(" E ", "\nE "), 9, Pink, new Vector2(1f, 0.5f), new Vector2(-68, 0), new Vector2(130, 110));

        // mapa grande (TAB): mostra os nomes dos lugares, mas nao a mamae
        bigMapRoot = UIKit.Panel(t, new Color(0, 0, 0, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2400, 1400)).gameObject;
        var bs = def.Parts.Length > 1 ? Fit(990f, 590f) : Fit(990f, 552f);
        var bigFrame = UIKit.Panel(bigMapRoot.transform, new Color(0.35f, 0.2f, 0.1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, 14), bs + new Vector2(14, 14));
        bigMap = UIKit.Rect(bigFrame.transform, "Mapa", new Vector2(0.5f, 0.5f), Vector2.zero, bs);
        MapImage(bigMap);
        int fs = def.Parts.Length > 1 ? 7 : 9;
        for (int i = 0; i < def.PlaceNames.Length && !dif.Dark; i++)
        {
            string nm = ShortName(def.PlaceNames[i]);
            var w = def.PlacePos[i];
            var bg = UIKit.Panel(bigMap, new Color(0, 0, 0, 0.7f), new Vector2(0.5f, 0.5f), MapPos(bigMap, w), new Vector2(nm.Length * fs + 10, fs * 2));
            UIKit.Label(bg.transform, nm, fs, Cream, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(nm.Length * fs + 10, fs * 2));
        }
        if (def.Parts.Length > 1)
            for (int k = 0; k < def.Parts.Length; k++)
                UIKit.Label(bigMap, def.PartNames[k], 12, Gold, new Vector2(0f, 1f), new Vector2(-70, -bs.y * (k + 0.5f) / def.Parts.Length), new Vector2(130, 24), TextAnchor.MiddleRight);
        foreach (var tt in totems) PlaceOn(bigMap, Dot(bigMap, new Color(0.2f, 0.7f, 1f), 12), tt.t.position);
        PlaceOn(bigMap, Dot(bigMap, ShopGreen, 14), shop.position);
        bigPlayer = Dot(bigMap, Gold, 16);
        UIKit.Label(bigMapRoot.transform, "AMARELO: VOCE    AZUL: TOTEM DE DICA    VERDE: LOJINHA        TAB: FECHAR", 10, Cream,
            new Vector2(0.5f, 0.5f), new Vector2(0, 14 - bs.y / 2f - 24f), new Vector2(1200, 24));
        bigMapRoot.SetActive(false);

        overlay = UIKit.Panel(t, new Color(0, 0, 0, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2400, 1400));
        centerText = UIKit.Label(overlay.transform, "", 20, Cream, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1150, 560));
        overlay.gameObject.SetActive(false);

        // lojinha
        shopRoot = UIKit.Panel(t, new Color(0.08f, 0.05f, 0.03f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 540)).gameObject;
        UIKit.Label(shopRoot.transform, "LOJINHA", 22, ShopGreen, new Vector2(0.5f, 1f), new Vector2(0, -35), new Vector2(800, 40));
        shopText = UIKit.Label(shopRoot.transform, "", 11, Cream, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(800, 360), TextAnchor.UpperLeft);
        UIKit.Label(shopRoot.transform, "SETAS: ESCOLHER    ENTER: COMPRAR    ESC: SAIR", 10, Gold, new Vector2(0.5f, 0f), new Vector2(0, 28), new Vector2(800, 24));
        shopRoot.SetActive(false);
    }

    // tamanho do mapa na tela mantendo a proporcao do mundo (fases 4 e 5 sao mais altas)
    Vector2 Fit(float maxW, float maxH)
    {
        float a = WorldW / WorldH;
        return maxW / a <= maxH ? new Vector2(maxW, maxW / a) : new Vector2(maxH * a, maxH);
    }

    void MapImage(RectTransform r)
    {
        int n = def.Parts.Length;
        for (int k = 0; k < n; k++)
        {
            var part = UIKit.Rect(r, "Parte" + k, new Vector2(0.5f, 0.5f),
                new Vector2(0f, r.sizeDelta.y * (0.5f - (k + 0.5f) / n)), new Vector2(r.sizeDelta.x, r.sizeDelta.y / n));
            var raw = part.gameObject.AddComponent<RawImage>();
            var sprite = Resources.Load<Sprite>(def.Parts[k]);
            if (sprite != null) raw.texture = sprite.texture;
            // Extra Hard: o mapa fica todo escuro, so aparecem os pontos
            raw.color = dif.Dark ? new Color(0.03f, 0.03f, 0.05f) : new Color(def.Tint.r + 0.1f, def.Tint.g + 0.1f, def.Tint.b + 0.1f);
            raw.raycastTarget = false;
        }
    }

    // "PADARIA DO SHOPPING" vira "PADARIA" nas placas e no mapa grande (la ja da para ver em que parte fica)
    static string ShortName(string s)
    {
        foreach (var suf in new[] { " DO MERCADO", " DO SHOPPING", " DO PARQUE" })
            if (s.EndsWith(suf)) return s.Substring(0, s.Length - suf.Length);
        return s;
    }

    static Sprite darkSprite;
    static Sprite DarkSprite()
    {
        // quadrado preto enorme com um buraco redondo e suave no meio
        if (darkSprite != null) return darkSprite;
        const int n = 256;
        const float units = 40f;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        float r = Difficulty.DarkRadius * n / units;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(r * 0.55f, r, d));
                px[y * n + x] = new Color32(0, 0, 0, (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        darkSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / units);
        return darkSprite;
    }

    RectTransform Dot(RectTransform parent, Color c, float size)
    {
        var r = UIKit.Rect(parent, "Ponto", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
        var i = r.gameObject.AddComponent<Image>();
        i.color = c;
        i.raycastTarget = false;
        return r;
    }

    Vector2 MapPos(RectTransform map, Vector2 world) =>
        new Vector2(world.x / WorldW * map.sizeDelta.x, world.y / WorldH * map.sizeDelta.y);

    void PlaceOn(RectTransform map, RectTransform dot, Vector2 world) => dot.anchoredPosition = MapPos(map, world);

    void ShowIntro()
    {
        state = State.Intro;
        Time.timeScale = 0f;
        introShownAt = Time.unscaledTime;
        var canvas = UIKit.MakeCanvas("Apresentacao", 20);
        introRoot = canvas.gameObject;
        var t = canvas.transform;
        UIKit.Panel(t, new Color(0.05f, 0.03f, 0.02f, 0.96f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2400, 1400));
        UIKit.Label(t, def.Title, 24, new Color(0.96f, 0.64f, 0.35f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(1200, 50));
        UIKit.Label(t, GameSettings.ModeName(mode) + "  -  " + GameSettings.DifName(dif.Dif), 11, Gold, new Vector2(0.5f, 1f), new Vector2(0, -82), new Vector2(1200, 22));
        UIKit.Label(t, def.Story, 12, Cream, new Vector2(0.5f, 1f), new Vector2(0, -122), new Vector2(1240, 60));

        UIKit.Label(t, "ESTA E A SUA MAMAE:", 15, Pink, new Vector2(0.5f, 0.5f), new Vector2(-280, 150), new Vector2(500, 30));
        UIKit.Picture(t, momSprites != null ? momSprites[6] : Resources.Load<Sprite>("Art/Mae/south"),
            new Vector2(0.5f, 0.5f), new Vector2(-280, 40), new Vector2(180, 180)).preserveAspect = true;
        UIKit.Label(t, def.Outfit, 11, Cream, new Vector2(0.5f, 0.5f), new Vector2(-280, -62), new Vector2(500, 30));

        UIKit.Label(t, "VOCE E ESTA CRIANCA:", 15, Gold, new Vector2(0.5f, 0.5f), new Vector2(280, 150), new Vector2(500, 30));
        UIKit.Picture(t, Resources.Load<Sprite>("Art/Crianca/south"), new Vector2(0.5f, 0.5f), new Vector2(280, 40), new Vector2(180, 180)).preserveAspect = true;
        UIKit.Label(t, "COM CONTORNO AMARELO", 11, Cream, new Vector2(0.5f, 0.5f), new Vector2(280, -62), new Vector2(500, 30));

        var tips = new List<string>();
        if (dif.Lookalikes > 0) tips.Add("CUIDADO: " + dif.Lookalikes + " MULHERES PARECIDAS COM A MAMAE! OLHE A ROUPA." +
            (mode == GameMode.Livre ? "" : " ERRAR TIRA " + Difficulty.WrongMomPenalty + "s"));
        if (dif.Dark) tips.Add("EXTRA HARD: ESTA TUDO ESCURO, VOCE SO ENXERGA EM VOLTA DE VOCE");
        if (def.Parts.Length > 1) tips.Add("MAPA GRANDE: " + string.Join(" + ", def.PartNames) + ", LIGADOS POR CORREDORES");
        tips.Add(dif.Guards + " SEGURANCAS: ESBARRAR DEIXA VOCE LENTO POR 1s");
        tips.Add("TOTEM AZUL (i): APERTE E PARA GANHAR UMA DICA DE ONDE A MAMAE ESTA");
        tips.Add("MOEDAS: GASTE NA LOJINHA ($) COM ESCUDO, TENIS, OCULOS" + (mode == GameMode.Livre ? "" : ", RELOGIO") + " E DICA");
        if (dif.MomMovesEvery > 0) tips.Add("A MAMAE MUDA DE LUGAR A CADA " + dif.MomMovesEvery + "s!");
        if (mode != GameMode.Livre) tips.Add("TEMPO: " + dif.Seconds + "s");
        UIKit.Label(t, string.Join("\n", tips), 11, Cream, new Vector2(0.5f, 0f), new Vector2(0, 190), new Vector2(1220, 170));
        UIKit.Label(t, "ENTER: COMECAR    WASD/SETAS: ANDAR    E: FALAR    TAB: MAPA    ESC: MENU", 11, Gold, new Vector2(0.5f, 0f), new Vector2(0, 55), new Vector2(1240, 40));
    }

    void StartPlaying()
    {
        Destroy(introRoot);
        state = State.Playing;
        Time.timeScale = 1f;
        youMarkerUntil = Time.time + 4f;
        AudioManager.Sfx("iniciar");
    }

    void ShowBanner(string s, float seconds)
    {
        bannerText.text = s;
        bannerPanel.gameObject.SetActive(true);
        bannerUntil = Time.unscaledTime + seconds;
    }

    // ---------------------------------------------------------------- regras

    public void Hit()
    {
        if (state != State.Playing) return;
        if (shields > 0)
        {
            shields--;
            AudioManager.Sfx("confirmar");
            ShowBanner("O ESCUDO TE PROTEGEU! (" + shields + " RESTANDO)", 1.4f);
            return;
        }
        hits++;
        pc.Slow(Difficulty.HitSlowSeconds, Difficulty.HitSlowFactor);
        AudioManager.Sfx("esbarrao");
        ShowBanner("ESBARRAO! VOCE FICOU LENTO", 1.2f);
    }

    string HintText()
    {
        // lugares com nome, do mais perto da mamae para o mais longe
        var order = new List<int>();
        for (int i = 0; i < def.PlaceNames.Length; i++) order.Add(i);
        Vector2 m = mom.position;
        order.Sort((a, b) => Vector2.Distance(def.PlacePos[a], m).CompareTo(Vector2.Distance(def.PlacePos[b], m)));
        switch (dif.Dif)
        {
            case Dificuldade.Facil:
                return "A MAMAE ESTA PERTO DE: " + def.PlaceNames[order[0]];
            case Dificuldade.Normal:
            {
                // o lugar certo e mais um lugar longe dali, em ordem sorteada
                int other = order[Random.Range(Mathf.Min(4, order.Count - 1), order.Count)];
                bool swap = Random.value < 0.5f;
                string a = def.PlaceNames[swap ? other : order[0]], b = def.PlaceNames[swap ? order[0] : other];
                return "A MAMAE ESTA PERTO DE " + a + " OU DE " + b;
            }
            default:
                return "A MAMAE ESTA " + RegionText(m);
        }
    }

    // dificil e extra hard: so a regiao do mapa (e em qual cenario, nas fases 4 e 5)
    string RegionText(Vector2 p)
    {
        int k = Mathf.Clamp(Mathf.FloorToInt((WorldH / 2f - p.y) / LevelDef.PartH), 0, def.Parts.Length - 1);
        float cy = WorldH / 2f - LevelDef.PartH * (k + 0.5f);
        string side = p.x < -WorldW / 6f ? "NO LADO ESQUERDO" : p.x > WorldW / 6f ? "NO LADO DIREITO" : "NO MEIO";
        string vert = p.y > cy ? "EM CIMA" : "EMBAIXO";
        string part = def.Parts.Length > 1 ? "NO " + def.PartNames[k] + ", " : "";
        return part + side + ", MAIS PARA " + (vert == "EM CIMA" ? "CIMA" : "BAIXO");
    }

    void GiveHint()
    {
        string h = HintText();
        hintText.text = "DICA: " + h;
        AudioManager.Sfx("pista");
        ShowBanner(h, 3.5f);
    }

    void Interact()
    {
        Vector2 pf = Feet(player);
        if (Vector2.Distance(Feet(mom), pf) < TalkDistance) { FoundMom(); return; }
        if (Vector2.Distance(shop.position, pf) < TalkDistance + 0.2f) { OpenShop(); return; }
        foreach (var l in lookalikes)
            if (Vector2.Distance(Feet(l), pf) < TalkDistance)
            {
                // falar com a mamae errada custa 3 segundos (uma vez por pessoa a cada 1,5s, para nao tirar varias vezes seguidas)
                if (Time.time < wrongMomUntil) return;
                wrongMomUntil = Time.time + 1.5f;
                mistakes++;
                AudioManager.Sfx("voltar");
                if (mode != GameMode.Livre)
                {
                    timeLeft -= Difficulty.WrongMomPenalty;
                    ShowBanner("ESSA NAO E A MAMAE! -" + Difficulty.WrongMomPenalty + "s\nA SUA TEM " + def.Outfit, 2.2f);
                }
                else ShowBanner("ESSA NAO E A MAMAE! A SUA TEM " + def.Outfit, 2f);
                return;
            }
        foreach (var tt in totems)
            if (!tt.used && Vector2.Distance(tt.t.position, pf) < TalkDistance)
            {
                tt.used = true;
                tt.sr.sprite = GenSprites.TotemOff;
                GiveHint();
                return;
            }
    }

    // ---------------------------------------------------------------- lojinha

    void BuildShopItems()
    {
        shopItems = new List<ShopItem>
        {
            new ShopItem { name = "ESCUDO", desc = "ESBARRAR NOS 2 PROXIMOS SEGURANCAS NAO ATRAPALHA", price = 3,
                owned = () => boughtShield, buy = () => { boughtShield = true; shields += 2; } },
            new ShopItem { name = "TENIS", desc = "ANDA 30% MAIS RAPIDO", price = 4,
                owned = () => boughtShoes, buy = () => { boughtShoes = true; pc.speedBonus = 1.3f; } },
            new ShopItem { name = "OCULOS", desc = "ENXERGA MAIS LONGE (A CAMERA ABRE)", price = 3,
                owned = () => boughtGlasses, buy = () => { boughtGlasses = true; camSize += 1.1f; darkScale = 1.5f; } },
        };
        if (mode != GameMode.Livre)
            shopItems.Add(new ShopItem { name = "RELOGIO", desc = "+20 SEGUNDOS", price = 3,
                owned = () => boughtClock, buy = () => { boughtClock = true; timeLeft += 20f; } });
        shopItems.Add(new ShopItem { name = "DICA", desc = "DIZ PERTO DE QUE LUGAR A MAMAE ESTA", price = 2,
            owned = () => boughtHint, buy = () => { boughtHint = true; GiveHint(); } });
    }

    void OpenShop()
    {
        state = State.Shop;
        Time.timeScale = 0f;
        shopSel = 0;
        prompt.SetActive(false);
        shopRoot.SetActive(true);
        AudioManager.Sfx("confirmar");
        RefreshShop("");
    }

    void CloseShop()
    {
        shopRoot.SetActive(false);
        state = State.Playing;
        Time.timeScale = 1f;
        AudioManager.Sfx("voltar");
    }

    void RefreshShop(string msg)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("SUAS MOEDAS: " + GameSettings.Coins + "\n\n");
        for (int i = 0; i < shopItems.Count; i++)
        {
            var it = shopItems[i];
            string mark = i == shopSel ? "> " : "  ";
            string price = it.owned() ? "COMPRADO" : it.price + " MOEDAS";
            sb.Append(mark + it.name.PadRight(9) + price + "\n     " + it.desc + "\n\n");
        }
        if (msg != "") sb.Append("\n" + msg);
        shopText.text = sb.ToString();
    }

    void ShopInput()
    {
        if (Inp.Pressed(K.Pause) || Inp.Pressed(K.Map)) { CloseShop(); return; }
        if (Inp.Pressed(K.Up)) { shopSel = (shopSel + shopItems.Count - 1) % shopItems.Count; AudioManager.Sfx("navegar"); RefreshShop(""); }
        if (Inp.Pressed(K.Down)) { shopSel = (shopSel + 1) % shopItems.Count; AudioManager.Sfx("navegar"); RefreshShop(""); }
        if (Inp.Pressed(K.Confirm))
        {
            var it = shopItems[shopSel];
            if (it.owned()) { RefreshShop("VOCE JA COMPROU ESSE ITEM NESTA FASE."); return; }
            if (GameSettings.Coins < it.price) { AudioManager.Sfx("voltar"); RefreshShop("FALTAM " + (it.price - GameSettings.Coins) + " MOEDAS."); return; }
            GameSettings.Coins -= it.price;
            it.buy();
            AudioManager.Sfx("moeda");
            RefreshShop("COMPROU: " + it.name + "!");
        }
    }

    // ---------------------------------------------------------------- fim de fase

    int PhaseScore()
    {
        int s = 1000 + levelIndex * 250 - hits * 100 - mistakes * 100;
        if (mode != GameMode.Livre) s += Mathf.RoundToInt(timeLeft * 10f);
        return Mathf.Max(0, s);
    }

    void FoundMom()
    {
        if (state != State.Playing) return;
        state = State.Won;
        int score = PhaseScore();
        GameSettings.TotalScore += score;
        AudioManager.Sfx("vitoria");
        string stats = "TEMPO: " + elapsed.ToString("0.0") + "s   MOEDAS PEGAS: " + coinsTaken + "/" + dif.Coins +
                       "\nESBARROES: " + hits + "   ENGANOS: " + mistakes + "\nPONTOS DA FASE: " + score;
        if (levelIndex < Levels.Count - 1)
        {
            End("VOCE ACHOU A MAMAE!\n\n" + stats + "\nTOTAL: " + GameSettings.TotalScore + "   MOEDAS GUARDADAS: " + GameSettings.Coins,
                "ENTER: PROXIMA FASE   M: MENU");
        }
        else
        {
            bool rec = GameSettings.SaveRecord(mode, GameSettings.TotalScore);
            End("PARABENS! VOCE ACHOU A MAMAE\nNAS " + Levels.Count + " FASES!\n\n" + stats + "\nTOTAL: " + GameSettings.TotalScore +
                (rec ? "\nNOVO RECORDE!" : "\nRECORDE: " + GameSettings.Record(mode)), "R: JOGAR DE NOVO   M: MENU");
        }
    }

    void TimeUp()
    {
        timeLeft = 0f;
        state = State.Lost;
        AudioManager.Sfx("derrota");
        if (mode == GameMode.ContraOTempo)
        {
            bool rec = GameSettings.SaveRecord(mode, GameSettings.TotalScore);
            End("O TEMPO ACABOU!\nVOCE CHEGOU ATE A FASE " + (levelIndex + 1) + "\nTOTAL: " + GameSettings.TotalScore +
                (rec ? "\nNOVO RECORDE!" : "\nRECORDE: " + GameSettings.Record(mode)), "R: COMECAR DE NOVO   M: MENU");
        }
        else End("O TEMPO ACABOU!\nA MAMAE ESTA ESPERANDO...", "R: TENTAR DE NOVO   M: MENU");
    }

    void End(string text, string keys)
    {
        centerText.text = text + "\n\n" + keys;
        overlay.gameObject.SetActive(true);
        bannerPanel.gameObject.SetActive(false);
        bigMapRoot.SetActive(false);
        prompt.SetActive(false);
        endedFrame = Time.frameCount;
        Time.timeScale = 0f;
    }

    void MoveMom()
    {
        FloodFrom(NearestFree(Cellof(Feet(player))));
        mom.position = FeetToPos(PickMomSpot());
        momMoveAt = elapsed + dif.MomMovesEvery;
        hintText.text = "";
        ShowBanner("A MAMAE MUDOU DE LUGAR! A DICA ANTIGA NAO VALE MAIS", 2.5f);
    }

    // ---------------------------------------------------------------- loop

    void Update()
    {
        if (state == State.Intro)
        {
            // espera um instante para a tecla que abriu a fase nao pular a apresentacao
            if (Time.unscaledTime - introShownAt < 0.6f) return;
            if (Inp.Pressed(K.Enter) || Inp.Pressed(K.Confirm)) StartPlaying();
            else if (Inp.Pressed(K.Pause)) { Time.timeScale = 1f; SceneManager.LoadScene("Menu"); }
            return;
        }

        if (state == State.Shop) { ShopInput(); UpdateHud(); return; }

        if ((state == State.Playing || state == State.Paused) && Inp.Pressed(K.Pause))
        {
            bool pause = state == State.Playing;
            state = pause ? State.Paused : State.Playing;
            Time.timeScale = pause ? 0f : 1f;
            overlay.gameObject.SetActive(pause);
            if (pause) { bannerPanel.gameObject.SetActive(false); bigMapRoot.SetActive(false); }
            centerText.text = pause ? "PAUSADO\n\nESC: CONTINUAR\nR: REINICIAR FASE   M: MENU" : "";
            AudioManager.Sfx(pause ? "voltar" : "confirmar");
        }

        if (state == State.Playing && Inp.Pressed(K.Map)) bigMapRoot.SetActive(!bigMapRoot.activeSelf);

        if (state == State.Playing) Tick();

#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
        // atalhos so no editor, para testar: F2 pula para a proxima fase, F3 leva a crianca ate perto da mamae, F4 da 10 moedas
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && state == State.Playing)
        {
            if (kb.f3Key.wasPressedThisFrame) player.position = mom.position + Vector3.left * 0.7f;
            if (kb.f4Key.wasPressedThisFrame) GameSettings.Coins += 10;
            if (kb.f2Key.wasPressedThisFrame && levelIndex < Levels.Count - 1) { NextLevel(); return; }
        }
#endif

        UpdateHud();

        // a tecla que terminou a fase (E) nao pode pular a tela de resultado no mesmo quadro
        if (state == State.Playing || Time.frameCount == endedFrame) return;
        if (state == State.Won && levelIndex < Levels.Count - 1 && Inp.Pressed(K.Enter)) NextLevel();
        else if (Inp.Pressed(K.Restart)) Restart();
        else if (Inp.Pressed(K.Menu)) { Time.timeScale = 1f; SceneManager.LoadScene("Menu"); }
    }

    void Tick()
    {
        elapsed += Time.deltaTime;
        if (mode != GameMode.Livre)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 10f && timeLeft > 0f && Time.time >= nextTick) { AudioManager.Sfx("tique"); nextTick = Time.time + 1f; }
            if (timeLeft <= 0f) { TimeUp(); return; }
        }
        if (elapsed >= momMoveAt) MoveMom();

        Vector2 pf = Feet(player);
        for (int i = coins.Count - 1; i >= 0; i--)
            if (Vector2.Distance(coins[i].position, pf) < 0.45f)
            {
                Destroy(coins[i].gameObject);
                coins.RemoveAt(i);
                coinsTaken++;
                GameSettings.Coins++;
                AudioManager.Sfx("moeda");
            }

        // "APERTE E" em cima de quem esta perto
        Transform near = null;
        float best = TalkDistance;
        float lift = 0.8f;
        if (Vector2.Distance(Feet(mom), pf) < best) { best = Vector2.Distance(Feet(mom), pf); near = mom; }
        foreach (var l in lookalikes)
        {
            float d = Vector2.Distance(Feet(l), pf);
            if (d < best) { best = d; near = l; lift = 0.8f; }
        }
        foreach (var tt in totems)
        {
            if (tt.used) continue;
            float d = Vector2.Distance(tt.t.position, pf);
            if (d < best) { best = d; near = tt.t; lift = 1.15f; }
        }
        float ds = Vector2.Distance(shop.position, pf);
        if (ds < TalkDistance + 0.2f && near != mom) { near = shop; lift = 2.1f; }
        prompt.SetActive(near != null);
        if (near != null) prompt.transform.position = (Vector2)near.position + Vector2.up * lift;

        if (Inp.Pressed(K.Interact)) Interact();
    }

    void LateUpdate()
    {
        if (player == null) return;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, camSize, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
        if (darkness != null)
        {
            darkness.position = new Vector3(player.position.x, player.position.y - FeetOffset * 0.5f, 0f);
            darkness.localScale = Vector3.one * Mathf.Lerp(darkness.localScale.x, darkScale, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
        }
        var target = new Vector3(player.position.x, player.position.y, -10);
        cam.transform.position = Vector3.Lerp(cam.transform.position, target, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        ClampCamera();
    }

    void ClampCamera()
    {
        float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
        // a barra do HUD cobre o topo da tela: deixa a camera subir um pouco a mais
        float hud = halfH * 2f * (52f / 720f);
        var p = cam.transform.position;
        p.x = halfW * 2f >= WorldW ? 0f : Mathf.Clamp(p.x, -WorldW / 2f + halfW, WorldW / 2f - halfW);
        p.y = halfH * 2f >= WorldH ? 0f : Mathf.Clamp(p.y, -WorldH / 2f + halfH, WorldH / 2f - halfH + hud);
        cam.transform.position = p;
    }

    void UpdateHud()
    {
        timerText.text = mode == GameMode.Livre ? Fmt(elapsed) : Fmt(timeLeft);
        timerText.color = mode != GameMode.Livre && timeLeft < 15f ? new Color(1f, 0.4f, 0.35f) : Cream;
        leftText.text = "FASE " + (levelIndex + 1) + "/" + Levels.Count + "  " + def.Name + "\nMOEDAS " + GameSettings.Coins + "  PONTOS " + GameSettings.TotalScore;
        var items = new List<string>();
        if (shields > 0) items.Add("ESCUDO x" + shields);
        if (boughtShoes) items.Add("TENIS");
        if (boughtGlasses) items.Add("OCULOS");
        rightText.text = GameSettings.ModeName(mode) + " - " + GameSettings.DifName(dif.Dif) + "   ESBARROES " + hits + "\n" + (items.Count > 0 ? string.Join(" ", items) + "   " : "") + "ESC: PAUSA";
        if (bannerPanel.gameObject.activeSelf && Time.unscaledTime > bannerUntil) bannerPanel.gameObject.SetActive(false);
        if (youMarker != null) youMarker.SetActive(state == State.Playing && Time.time < youMarkerUntil);

        PlaceOn(minimap, miniPlayer, player.position);
        PlaceOn(minimap, miniShop, shop.position);
        for (int i = 0; i < totems.Count; i++)
        {
            PlaceOn(minimap, miniTotems[i], totems[i].t.position);
            miniTotems[i].gameObject.SetActive(!totems[i].used);
        }
        if (bigMapRoot.activeSelf) PlaceOn(bigMap, bigPlayer, player.position);
    }

    void NextLevel()
    {
        GameSettings.Level = levelIndex + 1;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void Restart()
    {
        // No Contra o Tempo, perder recomeca a corrida toda; nos outros modos repete a fase
        bool fullRestart = (state == State.Won && levelIndex == Levels.Count - 1) || (state == State.Lost && mode == GameMode.ContraOTempo);
        if (fullRestart) GameSettings.NewRun();
        else GameSettings.Coins = startCoins; // repetir a fase devolve as moedas que voce tinha ao entrar nela
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    static string Fmt(float t) { int s = Mathf.CeilToInt(Mathf.Max(0f, t)); return (s / 60).ToString("00") + ":" + (s % 60).ToString("00"); }
}

public class PlayerController : MonoBehaviour
{
    public float speed = 4.5f;
    public float speedBonus = 1f;
    Rigidbody2D rb;
    DirectionalSprite ds;
    float slowUntil, slowFactor = 1f;

    void Awake() { rb = GetComponent<Rigidbody2D>(); ds = GetComponent<DirectionalSprite>(); }

    public void Slow(float seconds, float factor) { slowUntil = Time.time + seconds; slowFactor = factor; }

    void FixedUpdate()
    {
        var v = Inp.Move();
        float k = speedBonus * (Time.time < slowUntil ? slowFactor : 1f);
        rb.linearVelocity = v * speed * k;
        if (v.sqrMagnitude > 0.01f) ds.Face(v);
    }
}

// Anda de ponto em ponto (ida e volta). Se for seguranca, esbarrar na crianca a deixa lenta.
public class Walker : MonoBehaviour
{
    public List<Vector2> route;
    public float speed = 1.5f;
    public LevelController level;
    Rigidbody2D rb;
    DirectionalSprite ds;
    int idx = 1, dir = 1;
    float waitUntil, lastHit = -10f;

    void Awake() { rb = GetComponent<Rigidbody2D>(); ds = GetComponent<DirectionalSprite>(); }

    void FixedUpdate()
    {
        if (route == null || route.Count < 2 || Time.time < waitUntil) return;
        Vector2 pos = rb.position, target = route[idx];
        if (Vector2.Distance(pos, target) < 0.08f)
        {
            if (idx + dir >= route.Count || idx + dir < 0) dir = -dir;
            idx += dir;
            waitUntil = Time.time + Random.Range(0.2f, 1.2f);
            return;
        }
        ds.Face((target - pos).normalized);
        rb.MovePosition(Vector2.MoveTowards(pos, target, speed * Time.fixedDeltaTime));
    }

    void OnCollisionEnter2D(Collision2D c) => Bump(c);
    void OnCollisionStay2D(Collision2D c) => Bump(c);

    void Bump(Collision2D c)
    {
        if (c.collider.GetComponent<PlayerController>() == null) return;
        // para um instante para nao empurrar a crianca contra a parede
        waitUntil = Time.time + 1.2f;
        if (level != null && Time.time - lastHit > 2f) { lastHit = Time.time; level.Hit(); }
    }
}

// A mamae fica parada olhando em volta.
public class IdleLookAround : MonoBehaviour
{
    static readonly Vector2[] Dirs = { Vector2.down, Vector2.left, Vector2.right, new Vector2(-1, -1), new Vector2(1, -1), Vector2.up };
    DirectionalSprite ds;
    float next;
    void Awake() { ds = GetComponent<DirectionalSprite>(); }
    void Update()
    {
        if (Time.time < next) return;
        next = Time.time + Random.Range(0.8f, 2f);
        ds.Face(Dirs[Random.Range(0, Dirs.Length)]);
    }
}

// Moeda girando.
public class Spin : MonoBehaviour
{
    float phase;
    void Awake() { phase = Random.value * 6f; }
    void Update() { transform.localScale = new Vector3(Mathf.Abs(Mathf.Cos(Time.time * 3f + phase)) * 0.8f + 0.2f, 1f, 1f); }
}
