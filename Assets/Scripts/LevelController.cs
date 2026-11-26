using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Monta a fase atual em codigo (cenario, paredes, personagens, moedas, totens, lojinha, HUD, mapa)
// e controla o fluxo: apresentacao da mamae, tempo, dicas, lojinha, apagao, chuva, vitoria, derrota, pausa e proxima fase.
public class LevelController : MonoBehaviour
{
    enum State { Intro, Playing, Shop, Won, Lost, Paused, Kidnap, Hug }

    float WorldW, WorldH;
    const float Cell = 0.25f;          // grade usada para saber onde da para andar
    const float Clearance = 0.2f;      // folga em volta das paredes (igual ao raio dos pes da crianca)
    const float FeetOffset = 0.2f;     // os pes do personagem ficam abaixo do centro do sprite
    const float TalkDistance = 1.1f;
    const int CharOrder = 8;           // todos os NPCs na mesma camada: quem esta mais embaixo aparece na frente

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
    int hits, coinsTaken, mistakes, caught;

    Camera cam;
    float camSize;
    Transform player, mom, shop, kidnapper;   // kidnapper: a mulher de preto que pegou a crianca
    readonly List<Transform> kidnappers = new List<Transform>();
    readonly List<GameObject> alerts = new List<GameObject>();
    readonly List<bool> alerted = new List<bool>();
    const float AlertDistance = 2.6f;   // perto assim, aparece o "!" vermelho em cima da mulher de preto
    float shakeUntil, shakeAmp;
    PlayerController pc;
    Sprite[] momSprites;
    readonly List<Transform> lookalikes = new List<Transform>();
    readonly List<Transform> coins = new List<Transform>();
    readonly List<Totem> totems = new List<Totem>();
    GameObject prompt, youMarker;
    Transform darkness, arrow;  // escuridao (Extra Hard e apagao) e seta do megafone
    float darkScale = 1f, wrongMomUntil;

    // eventos: apagao (lugares fechados) e chuva (lugares abertos), uma vez por fase
    float blackoutAt = float.MaxValue, blackoutUntil = -1f, rainAt = float.MaxValue, rainUntil = -1f;
    const float BlackoutSeconds = 10f, RainSeconds = 25f, BlackoutRadius = 3.2f;
    GameObject rainRoot;
    Image fog;
    RectTransform[] drops;
    float fogAlpha;

    // itens da lojinha (valem so na fase em que foram comprados)
    int shields;
    bool boughtShield, boughtShoes, boughtGlasses, boughtClock, boughtHint, boughtLamp, boughtUmbrella, boughtDisguise, boughtMegaphone;
    float disguiseUntil = -1f, arrowUntil = -1f;

    // dicas: cada totem (ou a dica comprada) da a proxima da lista, nunca a mesma
    int hintNext, hintSeed;

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

    bool HasBlackout => def.Indoor && dif.Events && !dif.Dark;
    bool HasRain => def.Outdoor && dif.Events;
    bool Disguised => Time.time < disguiseUntil;

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
        hintSeed = Random.Range(0, 1000);

        // personagens na mesma camada sao desenhados pela altura: quem esta mais embaixo fica na frente
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        GraphicsSettings.transparencySortAxis = Vector3.up;

        momSprites = DressMom(def.MomShirt, def.MomSkirt);
        BuildWorld();
        BuildHud();
        BuildRain();
        BuildShopItems();
        PlanEvents();
        AudioManager.StopMusic();
        AudioManager.Music("musica_fase");
        AudioManager.Ambient("amb_" + new[] { "shopping", "mercado", "parque", "aquatico", "diversoes", "gelo" }[levelIndex]);
        ShowIntro();
    }

    void PlanEvents()
    {
        float len = mode == GameMode.Livre ? 90f : dif.Seconds;
        if (HasBlackout) blackoutAt = Random.Range(15f, Mathf.Max(20f, len * 0.6f));
        if (HasRain)
        {
            rainAt = Random.Range(12f, Mathf.Max(18f, len * 0.5f));
            // os dois na mesma fase (parque de diversoes): nao deixa um cair em cima do outro
            if (HasBlackout && Mathf.Abs(rainAt - blackoutAt) < 30f) rainAt = blackoutAt + 30f;
        }
    }

    void BuildWorld()
    {
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        camSize = dif.CameraSize * 0.9f;   // visao 10% menor (mais perto)
        cam.orthographicSize = camSize;
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transparencySortMode = TransparencySortMode.CustomAxis;
        cam.transparencySortAxis = Vector3.up;
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.AddComponent<AudioListener>();

        // cenario (cada fase tem o seu)
        for (int k = 0; k < def.Parts.Length; k++)
        {
            var bg = new GameObject("Cenario" + k).AddComponent<SpriteRenderer>();
            bg.sprite = Resources.Load<Sprite>(def.Parts[k]);
            // cenarios montados em pixel art: sem borrar os pixels
            if (bg.sprite != null && def.Parts[k].EndsWith("_v8")) bg.sprite.texture.filterMode = FilterMode.Point;
            bg.sortingOrder = -10;
            float y = def.Parts.Length == 1 ? 0f : WorldH / 2f - LevelDef.PartH * (k + 0.5f);
            bg.transform.position = new Vector3(0f, y, 0f);
            bg.transform.localScale = Vector3.one * def.BgScale;
            // escurece um pouco o cenario para os personagens vivos se destacarem
            bg.color = def.Tint;
            if (def.Parts.Length == 1 && def.Occluders != null) BuildOccluders(bg);
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
        pc.ice = def.Ice;
        p.GetComponent<DirectionalSprite>().OnStep = () => { if (state == State.Playing) AudioManager.Sfx("passo", 0.35f); };
        Outline.Add(p, Gold, 0.045f);
        var ring = new GameObject("Anel").AddComponent<SpriteRenderer>();
        ring.sprite = GenSprites.Ring;
        ring.transform.SetParent(p.transform, false);
        ring.transform.localPosition = new Vector3(0, -0.38f, 0);
        ring.sortingOrder = 3;
        player = p.transform;
        youMarker = MakeWorldText(p.transform, "VOCÊ", new Vector3(0, 0.75f, 0), Gold);
        bool skating = def.SkateLoop != null && def.SkateLoop.Length > 2;

        // Mamae: sem contorno (so aparece "APERTE E" quando a crianca chega perto)
        var momGo = MakeCharacter("Mamae", "Mae", FeetToPos(PickMomSpot()), CharOrder);
        if (momSprites != null) momGo.GetComponent<DirectionalSprite>().Use(momSprites);
        mom = momGo.transform;
        if (skating) AddSkater(momGo, 0.75f, 2.4f);   // na pista de gelo a mamae fica patinando
        else momGo.AddComponent<IdleLookAround>();
        momMoveAt = dif.MomMovesEvery > 0 ? dif.MomMovesEvery : float.MaxValue;

        // Mulheres parecidas com a mamae (roupa bem parecida, mas nunca igual)
        for (int i = 0; i < dif.Lookalikes; i++)
        {
            var c = RandomFarCell(3f, 1.6f, lookalikes);
            var go = MakeCharacter("Parecida", "Mae", FeetToPos(CellCenter(c)), CharOrder);
            DressLookalike(go, i);
            if (skating && i % 5 < 3) AddSkater(go, Random.Range(0.45f, 1.15f), Random.Range(2.2f, 3.4f));
            else AddWalker(go, RandomRoute(c), 0.9f + Random.value * 0.4f, false, true);
            lookalikes.Add(go.transform);
        }

        // Sequestradora: corpo um pouco maior e roupa toda preta. Falar com ela e perder o jogo.
        for (int i = 0; i < dif.Kidnappers; i++)
        {
            var c = RandomFarCell(6f, 4f, kidnappers);
            var go = MakeCharacter("Sequestradora", "Mae", FeetToPos(CellCenter(c)), CharOrder, 1.3f);
            var ks = DressSafe(() => Recolor.Kidnapper());
            if (ks != null) go.GetComponent<DirectionalSprite>().Use(ks);
            else go.GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.3f, 0.3f);
            AddWalker(go, RandomRoute(c), 0.7f, false, true);
            kidnappers.Add(go.transform);
            var al = MakeWorldText(go.transform, "!", new Vector3(0, 1.05f, 0), new Color(1f, 0.2f, 0.2f), 1.6f);
            al.SetActive(false);
            alerts.Add(al);
            alerted.Add(false);
        }
        if (kidnappers.Count > 0) kidnapper = kidnappers[0];

        // Multidao estilo "Onde esta o Wally": mulheres, homens e criancas com roupas variadas
        BuildCrowd(skating);

        // Segurancas fazendo ronda pelos corredores
        for (int i = 0; i < dif.Guards; i++)
        {
            var c = RandomFarCell(4f, 1.5f);
            var go = MakeCharacter("Seguranca", "Policial", FeetToPos(CellCenter(c)), CharOrder);
            Outline.Add(go, new Color(0.55f, 0.75f, 1f));
            AddWalker(go, RandomRoute(c), dif.GuardSpeed * (0.85f + Random.value * 0.3f), false, true).level = this;   // da para atravessar, mas deixa lento
        }

        // Policial grandao: se pegar a crianca, leva para outro lugar e ela fica presa por 5 segundos
        var bigSprites = dif.BigCops > 0 ? DressSafe(() => Recolor.BigCop()) : null;
        for (int i = 0; i < dif.BigCops; i++)
        {
            var c = RandomFarCell(7f, 3f);
            var go = MakeCharacter("PolicialGrandao", "Policial", FeetToPos(CellCenter(c)), CharOrder, 1.5f);
            if (bigSprites != null) go.GetComponent<DirectionalSprite>().Use(bigSprites);
            Outline.Add(go, new Color(1f, 0.35f, 0.3f), 0.03f);
            var w = AddWalker(go, RandomRoute(c, 3, 6), dif.GuardSpeed * 0.95f, true, true);
            w.level = this;
            w.bigCop = true;
        }

        // Lojinha: lugar fixo de cada fase, encostada num canto, sem fechar nenhuma passagem
        var shopGo = new GameObject("Lojinha");
        shopGo.transform.position = def.ShopPos;
        var ssr = shopGo.AddComponent<SpriteRenderer>();
        ssr.sprite = GenSprites.Shop;
        ssr.sortingOrder = 5;
        MakeWorldText(shopGo.transform, "LOJINHA", new Vector3(0, 1.3f, 0), ShopGreen, 0.8f);
        var sbox = shopGo.AddComponent<BoxCollider2D>(); // a barraca e solida, ninguem atravessa
        sbox.size = new Vector2(0.95f, 0.5f);
        sbox.offset = new Vector2(0, 0.28f);
        shop = shopGo.transform;
        Physics2D.SyncTransforms();
        var sc = Cellof(def.ShopPos);
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
            var c = RandomFarCell(2f, 2.5f, coins);
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
                var s = MakeWorldText(signs.transform, def.PlaceNames[i], Vector3.zero, Cream, 0.75f);
                s.transform.position = def.PlacePos[i];
            }
        }

        // escuridao: sempre no Extra Hard; nas outras, so durante o apagao
        if (dif.Dark || HasBlackout)
        {
            var dk = new GameObject("Escuridao").AddComponent<SpriteRenderer>();
            dk.sprite = DarkSprite();
            dk.sortingOrder = 40; // por cima de tudo, ate das placas: no escuro nao da para ler de longe
            darkness = dk.transform;
            darkness.position = player.position;
            darkness.gameObject.SetActive(dif.Dark);
        }

        var ar = new GameObject("Seta").AddComponent<SpriteRenderer>();
        ar.sprite = GenSprites.Arrow;
        ar.sortingOrder = 32;
        arrow = ar.transform;
        arrow.gameObject.SetActive(false);

        prompt = MakeWorldText(null, "APERTE E", Vector3.zero, Gold);
        prompt.SetActive(false);

        cam.transform.position = new Vector3(player.position.x, player.position.y, -10);
        ClampCamera();
    }

    void BuildCrowd(bool skating)
    {
        float ms = def.MomShirt < 0f ? 345f : def.MomShirt, mk = def.MomSkirt < 0f ? 225f : def.MomSkirt;
        var shirts = FarHues(ms, 60f);
        var skirts = FarHues(mk, 45f);
        var others = new List<Transform>();
        for (int i = 0; i < dif.Crowd; i++)
        {
            var c = RandomFarCell(2.5f, 1.2f, others);
            int kind = i % 3; // 0 mulher, 1 homem, 2 crianca
            float scale = kind == 2 ? 0.85f : 1f;
            var go = MakeCharacter("Pessoa", kind == 0 ? "Mae" : kind == 1 ? "Policial" : "Crianca", FeetToPos(CellCenter(c)), CharOrder, scale);
            // poucas combinacoes de roupa por fase, para nao gerar desenhos demais
            int v = (i / 3) % 5;
            float h1 = Hues[(v * 3 + levelIndex + kind) % Hues.Length], h2 = Hues[(v * 5 + 2 + kind * 2) % Hues.Length];
            Sprite[] look = null;
            if (kind == 0) look = DressSafe(() => Recolor.Mom(shirts[(v * 2 + levelIndex) % shirts.Count], skirts[(v * 3 + 1) % skirts.Count]));
            else if (kind == 1) look = DressSafe(() => Recolor.Man(h1, h2, v % 2 == 0));
            else look = DressSafe(() => Recolor.Kid(h1, h2));
            if (look != null) go.GetComponent<DirectionalSprite>().Use(look);
            else go.GetComponent<SpriteRenderer>().color = Color.HSVToRGB(h1 / 360f, 0.3f, 1f);

            if (skating && Random.value < 0.45f) AddSkater(go, Random.Range(0.4f, 1.15f), Random.Range(2f, 3.6f));
            else if (Random.value < 0.3f) go.AddComponent<IdleLookAround>();   // parado, olhando vitrine
            else AddWalker(go, RandomRoute(c), 0.6f + Random.value * 0.6f, false, true);
            others.Add(go.transform);
        }
    }

    GameObject MakeCharacter(string name, string folder, Vector2 pos, int order, float scale = 1f)
    {
        var go = new GameObject(name) { layer = 2 }; // Ignore Raycast: so as paredes contam nas consultas de fisica
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = order;
        go.AddComponent<DirectionalSprite>().Load(folder);
        go.transform.localScale = Vector3.one * 1.1f * scale;
        allChars.Add(go.transform);
        return go;
    }

    Walker AddWalker(GameObject go, List<Vector2> route, float speed, bool big, bool passThrough)
    {
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var c = go.AddComponent<CircleCollider2D>();
        float ls = go.transform.localScale.x;
        c.radius = (big ? 0.3f : 0.242f) / ls;
        c.offset = new Vector2(0, -0.22f / ls);
        c.isTrigger = passThrough; // atravessavel: nao trava a passagem
        var w = go.AddComponent<Walker>();
        w.route = route;
        w.speed = speed;
        return w;
    }

    void AddSkater(GameObject go, float loopScale, float speed)
    {
        var s = go.AddComponent<Skater>();
        s.loop = def.SkateLoop;
        s.scale = loopScale;
        s.speed = speed;
        s.feet = FeetOffset;
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

    // ---------------------------------------------------------------- frente do cenario
    // Pedacos do proprio desenho (copa de arvore, telhado de barraca, arco da entrada, guarda-sol...) que ficam
    // NA FRENTE de quem passa atras deles. Assim o mapa tem profundidade: quem esta atras da arvore some atras dela.
    class Occluder { public SpriteRenderer sr; public float baseY, topY, x0, x1; }
    readonly List<Occluder> occluders = new List<Occluder>();
    readonly List<Transform> allChars = new List<Transform>();   // todo mundo que anda pelo mapa

    void BuildOccluders(SpriteRenderer bg)
    {
        var tex = bg.sprite.texture;
        float ppu = bg.sprite.pixelsPerUnit;
        float k = tex.width / 2752f;            // coordenadas da fase sao no desenho de 2752 px de largura
        float scale = def.BgScale;
        var root = new GameObject("Frente");
        for (int i = 0; i < def.Occluders.GetLength(0); i++)
        {
            float x0 = def.Occluders[i, 0] * k, y0 = def.Occluders[i, 1] * k, x1 = def.Occluders[i, 2] * k, y1 = def.Occluders[i, 3] * k;
            float b = def.Occluders[i, 4] * k;
            // pecas na borda do mapa podem passar do desenho: corta no limite da textura
            x0 = Mathf.Max(x0, 0f); y0 = Mathf.Max(y0, 0f); x1 = Mathf.Min(x1, tex.width); y1 = Mathf.Min(y1, tex.height);
            float w = x1 - x0, h = y1 - y0;
            if (w < 2f || h < 2f) continue;
            var sprite = Sprite.Create(tex, new Rect(x0, tex.height - y1, w, h), new Vector2(0.5f, -(b - y1) / h), ppu);
            var go = new GameObject("Frente" + i);
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(((x0 + x1) / 2f - tex.width / 2f) / ppu * scale, (tex.height / 2f - b) / ppu * scale, 0f);
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = def.Tint;
            sr.sortingOrder = CharOrder;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;   // ordena pela base do objeto, igual aos pes das pessoas
            occluders.Add(new Occluder
            {
                sr = sr,
                baseY = go.transform.position.y,
                topY = (tex.height / 2f - y0) / ppu * scale,
                x0 = (x0 - tex.width / 2f) / ppu * scale,
                x1 = (x1 - tex.width / 2f) / ppu * scale,
            });
        }
    }

    static bool Behind(Occluder o, Vector2 feet) =>
        feet.y > o.baseY && feet.y < o.topY && feet.x > o.x0 - 0.3f && feet.x < o.x1 + 0.3f;

    // a crianca fica sempre por cima da multidao; atras de um objeto, o objeto fica meio transparente
    void UpdateOccluders()
    {
        if (occluders.Count == 0 || player == null) return;
        Vector2 pf = Feet(player);
        foreach (var o in occluders)
        {
            bool p = Behind(o, pf);
            bool someone = false;
            if (!p)
                foreach (var t in allChars)
                {
                    if (t == null || t == player) continue;
                    Vector2 f = Feet(t);
                    if (f.x < o.x0 - 0.3f || f.x > o.x1 + 0.3f) continue;
                    if (Behind(o, f)) { someone = true; break; }
                }
            o.sr.sortingOrder = p ? 11 : CharOrder;
            var c = def.Tint;
            c.a = p ? 0.5f : someone ? 0.6f : 1f;
            o.sr.color = c;
        }
    }

    // ---------------------------------------------------------------- roupas

    static Sprite[] DressSafe(System.Func<Sprite[]> f)
    {
        try { return f(); }
        catch (System.Exception e) { Debug.LogWarning("Recolor falhou: " + e.Message); return null; }
    }

    static Sprite[] DressMom(float shirt, float skirt)
    {
        if (shirt < 0f && skirt < 0f) return null; // roupa original do desenho
        return DressSafe(() => Recolor.Mom(shirt, skirt));
    }

    static readonly float[] Hues = { 0f, 25f, 50f, 90f, 125f, 160f, 195f, 225f, 255f, 280f, 310f, 345f };

    static float HueGap(float a, float b) { float d = Mathf.Abs(Mathf.Repeat(a - b, 360f)); return Mathf.Min(d, 360f - d); }

    static List<float> FarHues(float from, float gap)
    {
        var l = new List<float>();
        foreach (var h in Hues) if (HueGap(h, from) >= gap) l.Add(h);
        return l;
    }

    // variacoes das parecidas: (graus na blusa, graus na saia, cabelo). Sempre ha pelo menos uma peca com cor
    // claramente diferente (30 graus ou mais), entao so a mamae tem aquela roupa.
    static readonly Vector3[] LookVariants =
    {
        new Vector3(35f, 0f, 0f), new Vector3(-35f, 0f, 1f), new Vector3(0f, 35f, 2f), new Vector3(0f, -35f, 3f),
        new Vector3(30f, 30f, 4f), new Vector3(-30f, -30f, 1f), new Vector3(50f, -30f, 2f), new Vector3(-50f, 30f, 0f),
        new Vector3(40f, 0f, 3f), new Vector3(0f, 45f, 1f), new Vector3(-40f, 40f, 4f), new Vector3(60f, 0f, 2f),
        new Vector3(0f, -50f, 0f), new Vector3(30f, -45f, 3f), new Vector3(-60f, 0f, 1f), new Vector3(45f, 45f, 2f),
    };

    void DressLookalike(GameObject go, int i)
    {
        // parecida com a mamae, mas nunca igual: a blusa e/ou a saia tem outra cor proxima, e muitas vezes o cabelo muda
        float ms = def.MomShirt < 0f ? 345f : def.MomShirt;
        float mk = def.MomSkirt < 0f ? 225f : def.MomSkirt;
        var lv = LookVariants[(i + hintSeed) % LookVariants.Length];
        // se a mamae usa a cor original do desenho, a parecida tambem recolore a peca "igual" com a mesma cor base
        float shirt = ms + lv.x, skirt = mk + lv.y;
        int hair = (int)lv.z;
        var sprites = DressSafe(() => Recolor.Mom(shirt, skirt, 1f, 1f, hair));
        if (sprites != null) go.GetComponent<DirectionalSprite>().Use(sprites);
        else go.GetComponent<SpriteRenderer>().color = Color.HSVToRGB(Mathf.Repeat(shirt, 360f) / 360f, 0.5f, 1f);
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
        float frac = new[] { 0.5f, 0.45f, 0.4f, 0.4f, 0.35f, 0.35f }[Mathf.Min(levelIndex, 5)];
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

    List<Vector2> RandomRoute(Vector2Int start, int minPts = 2, int maxPts = 5)
    {
        // ronda de ida e volta entre pontos ligados em linha reta, sem atravessar paredes
        var pts = new List<Vector2> { CellCenter(start) };
        int want = Random.Range(minPts, maxPts);
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

        bannerPanel = UIKit.Panel(t, new Color(0, 0, 0, 0.8f), new Vector2(0.5f, 1f), new Vector2(-100, -116), new Vector2(860, 76));
        bannerText = UIKit.Label(bannerPanel.transform, "", 14, Gold, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840, 72));
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
        UIKit.Label(card.transform, "MAMÃE:\n" + def.Outfit.Replace(" E ", "\nE "), 9, Pink, new Vector2(1f, 0.5f), new Vector2(-68, 0), new Vector2(130, 110));

        // mapa grande (TAB): mostra os nomes dos lugares (letra maior), mas nao a mamae
        bigMapRoot = UIKit.Panel(t, new Color(0, 0, 0, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2400, 1400)).gameObject;
        var bs = Fit(1060f, 580f);
        var bigFrame = UIKit.Panel(bigMapRoot.transform, new Color(0.35f, 0.2f, 0.1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, 14), bs + new Vector2(14, 14));
        bigMap = UIKit.Rect(bigFrame.transform, "Mapa", new Vector2(0.5f, 0.5f), Vector2.zero, bs);
        MapImage(bigMap);
        const int fs = 12;
        for (int i = 0; i < def.PlaceNames.Length && !dif.Dark; i++)
        {
            string nm = def.PlaceNames[i];
            var w = def.PlacePos[i];
            var bg = UIKit.Panel(bigMap, new Color(0, 0, 0, 0.78f), new Vector2(0.5f, 0.5f), MapPos(bigMap, w), new Vector2(nm.Length * fs + 12, fs * 2 + 2));
            UIKit.Label(bg.transform, nm, fs, Cream, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(nm.Length * fs + 12, fs * 2 + 2));
        }
        foreach (var tt in totems) PlaceOn(bigMap, Dot(bigMap, new Color(0.2f, 0.7f, 1f), 12), tt.t.position);
        PlaceOn(bigMap, Dot(bigMap, ShopGreen, 14), shop.position);
        bigPlayer = Dot(bigMap, Gold, 16);
        UIKit.Label(bigMapRoot.transform, "AMARELO: VOCÊ    AZUL: TOTEM DE DICA    VERDE: LOJINHA        TAB: FECHAR", 10, Cream,
            new Vector2(0.5f, 0.5f), new Vector2(0, 14 - bs.y / 2f - 24f), new Vector2(1200, 24));
        bigMapRoot.SetActive(false);

        overlay = UIKit.Panel(t, new Color(0, 0, 0, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2400, 1400));
        centerText = UIKit.Label(overlay.transform, "", 20, Cream, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1150, 560));
        overlay.gameObject.SetActive(false);

        // lojinha
        shopRoot = UIKit.Panel(t, new Color(0.08f, 0.05f, 0.03f, 0.96f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 580)).gameObject;
        UIKit.Label(shopRoot.transform, "LOJINHA", 22, ShopGreen, new Vector2(0.5f, 1f), new Vector2(0, -35), new Vector2(800, 40));
        shopText = UIKit.Label(shopRoot.transform, "", 11, Cream, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(840, 440), TextAnchor.UpperLeft);
        UIKit.Label(shopRoot.transform, "SETAS: ESCOLHER    ENTER: COMPRAR    ESC: SAIR", 10, Gold, new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(800, 24));
        shopRoot.SetActive(false);
    }

    // chuva: gotas caindo e o vidro embacado por cima do jogo (abaixo do HUD)
    void BuildRain()
    {
        if (!HasRain) return;
        var canvas = UIKit.MakeCanvas("Chuva", 5);
        rainRoot = canvas.gameObject;
        fog = UIKit.Picture(canvas.transform, GenSprites.Fog, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500, 850));
        fog.color = new Color(1f, 1f, 1f, 0f);
        drops = new RectTransform[110];
        for (int i = 0; i < drops.Length; i++)
        {
            var r = UIKit.Rect(canvas.transform, "Gota", new Vector2(0.5f, 0.5f), new Vector2(Random.Range(-700f, 700f), Random.Range(-420f, 420f)), new Vector2(2, Random.Range(14f, 26f)));
            var im = r.gameObject.AddComponent<Image>();
            im.color = new Color(0.75f, 0.85f, 1f, 0.55f);
            im.raycastTarget = false;
            r.localRotation = Quaternion.Euler(0, 0, -12f);
            drops[i] = r;
        }
        rainRoot.SetActive(false);
    }

    void UpdateRain()
    {
        if (rainRoot == null) return;
        bool raining = elapsed >= rainAt && elapsed < rainUntil;
        if (!raining && fogAlpha <= 0.01f) { if (rainRoot.activeSelf) rainRoot.SetActive(false); return; }
        rainRoot.SetActive(true);
        // o vidro vai embacando aos poucos; com guarda-chuva nao embaca
        float target = raining && !boughtUmbrella ? 1f : 0f;
        fogAlpha = Mathf.MoveTowards(fogAlpha, target, Time.deltaTime * (target > fogAlpha ? 0.3f : 0.5f));
        fog.color = new Color(1f, 1f, 1f, fogAlpha);
        foreach (var d in drops)
        {
            d.gameObject.SetActive(raining);
            var p = d.anchoredPosition + new Vector2(170f, -900f) * Time.deltaTime;
            if (p.y < -440f) p = new Vector2(Random.Range(-760f, 640f), 440f);
            d.anchoredPosition = p;
        }
    }

    // tamanho do mapa na tela mantendo a proporcao do mundo
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
        var momPic = momSprites != null ? momSprites[6] : Resources.Load<Sprite>("Art/Mae/south");

        // no modo historia, os avisos aparecem so na fase 1; depois so a roupa nova da mamae
        if (mode == GameMode.Historia && levelIndex > 0)
        {
            UIKit.Label(t, "A MAMÃE AGORA ESTÁ COM OUTRA ROUPA:", 15, Pink, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1000, 30));
            UIKit.Picture(t, momPic, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(240, 240)).preserveAspect = true;
            UIKit.Label(t, def.Outfit, 14, Cream, new Vector2(0.5f, 0.5f), new Vector2(0, -115), new Vector2(1000, 30));
            UIKit.Label(t, "ENTER: COMEÇAR", 11, Gold, new Vector2(0.5f, 0f), new Vector2(0, 55), new Vector2(1240, 40));
            return;
        }

        UIKit.Label(t, def.Story, 12, Cream, new Vector2(0.5f, 1f), new Vector2(0, -122), new Vector2(1240, 60));
        UIKit.Label(t, "ESTA É A SUA MAMÃE:", 15, Pink, new Vector2(0.5f, 0.5f), new Vector2(-280, 160), new Vector2(500, 30));
        UIKit.Picture(t, momPic, new Vector2(0.5f, 0.5f), new Vector2(-280, 60), new Vector2(170, 170)).preserveAspect = true;
        UIKit.Label(t, def.Outfit, 11, Cream, new Vector2(0.5f, 0.5f), new Vector2(-280, -36), new Vector2(500, 30));

        UIKit.Label(t, "VOCÊ É ESTA CRIANÇA:", 15, Gold, new Vector2(0.5f, 0.5f), new Vector2(280, 160), new Vector2(500, 30));
        UIKit.Picture(t, Resources.Load<Sprite>("Art/Crianca/south"), new Vector2(0.5f, 0.5f), new Vector2(280, 60), new Vector2(170, 170)).preserveAspect = true;
        UIKit.Label(t, "COM CONTORNO AMARELO", 11, Cream, new Vector2(0.5f, 0.5f), new Vector2(280, -36), new Vector2(500, 30));

        bool story = mode == GameMode.Historia;
        var tips = new List<string>();
        tips.Add("CUIDADO: MULHERES COM ROUPA PARECIDA COM A DA MAMÃE! OLHE BEM AS CORES." +
            (mode == GameMode.Livre ? "" : " ERRAR TIRA " + Difficulty.WrongMomPenalty + "s"));
        if (dif.BigCops > 0 || story) tips.Add("POLICIAL GRANDÃO DE BIGODE: SE TE PEGAR, TE LEVA PARA LONGE E VOCÊ FICA PRESO " + Difficulty.CaughtSeconds + "s");
        tips.Add("SEGURANÇAS: DÁ PARA PASSAR POR ELES, MAS ESBARRAR DEIXA VOCÊ LENTO POR " + Difficulty.HitSlowSeconds + "s");
        if (dif.Totems > 1) tips.Add("TOTEM AZUL (i): CADA UM DA UMA DICA DIFERENTE DO QUE A MAMÃE ESTAVA FAZENDO");
        else if (dif.Totems == 1) tips.Add("SÓ TEM 1 TOTEM AZUL (i) DE DICA NO MAPA: ACHE ELE!");
        else if (levelIndex == Levels.Count - 1) tips.Add("AQUI NÃO TEM DICAS: TODO MUNDO PATINA RÁPIDO, ALCANCE A MAMÃE!");
        else tips.Add("NESTA DIFICULDADE NÃO TEM TOTEM DE DICA: OLHE BEM A ROUPA DA MAMÃE!");
        if (dif.Coins > 0) tips.Add("MOEDAS SÃO POUCAS: GASTE BEM NA LOJINHA ($)");
        if (dif.Events) tips.Add("A LUZ PODE ACABAR (USE A LANTERNA) E PODE CHOVER (USE O GUARDA-CHUVA)");
        if (dif.Dark) tips.Add("EXTRA HARD: ESTÁ TUDO ESCURO, VOCÊ SÓ ENXERGA EM VOLTA DE VOCÊ");
        if (dif.MomMovesEvery > 0) tips.Add("A MAMÃE MUDA DE LUGAR A CADA " + dif.MomMovesEvery + "s!");
        if (mode != GameMode.Livre) tips.Add("TEMPO: " + dif.Seconds + "s");
        UIKit.Label(t, string.Join("\n", tips), 10, Cream, new Vector2(0.5f, 0f), new Vector2(0, 190), new Vector2(1240, 220));
        UIKit.Label(t, "ENTER: COMEÇAR    WASD/SETAS: ANDAR    E: FALAR    TAB: MAPA    ESC: MENU", 11, Gold, new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(1240, 40));
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
        if (state != State.Playing || Disguised || pc.Frozen) return;
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
        Shake(0.35f, 0.12f);
        ShowBanner("ESBARRÃO! VOCÊ FICOU LENTO", 1.2f);
    }

    // o policial grandao pegou a crianca: leva para longe e ela fica presa uns segundos
    public void Grab(Walker cop)
    {
        if (state != State.Playing || Disguised || pc.Frozen) return;
        if (shields > 0)
        {
            shields--;
            AudioManager.Sfx("confirmar");
            ShowBanner("O ESCUDO TE PROTEGEU DO POLICIAL! (" + shields + " RESTANDO)", 1.6f);
            cop.Hold(1.5f, false);
            return;
        }
        caught++;
        hits++;
        var c = RandomFarCell(9f, 3f, new List<Transform> { mom });
        Vector2 to = FeetToPos(CellCenter(c));
        pc.Teleport(to);
        pc.Freeze(Difficulty.CaughtSeconds);
        cop.Carry(to + Vector2.right * 0.75f, Difficulty.CaughtSeconds + 0.3f);
        youMarkerUntil = Time.time + Difficulty.CaughtSeconds + 2f;
        AudioManager.Sfx("esbarrao");
        Shake(0.5f, 0.2f);
        ShowBanner("O POLICIAL GRANDÃO TE LEVOU PARA OUTRO LUGAR!\nVOCÊ FICA PRESO POR " + Difficulty.CaughtSeconds + "s", Difficulty.CaughtSeconds);
    }

    // o que a mamae estava fazendo, a partir dos lugares mais perto dela
    List<string> Hints()
    {
        var order = new List<int>();
        for (int i = 0; i < def.PlaceNames.Length; i++) order.Add(i);
        Vector2 m = Feet(mom);
        order.Sort((a, b) => Vector2.Distance(def.PlacePos[a], m).CompareTo(Vector2.Distance(def.PlacePos[b], m)));
        string Act(int k) => def.PlaceActs[order[Mathf.Clamp(k, 0, order.Count - 1)]];
        int far = order.Count - 1 - hintSeed % Mathf.Max(1, order.Count / 2);
        string a = "A MAMÃE ESTAVA " + Act(0);
        string b = "PERTO DA MAMÃE TINHA GENTE " + Act(1);
        string c = "DE ONDE A MAMÃE ESTAVA DAVA PARA VER GENTE " + Act(2);
        string d = "A MAMÃE NÃO ESTAVA " + Act(far);
        switch (dif.Dif)
        {
            case Dificuldade.Facil: return new List<string> { a, b, c, d };
            case Dificuldade.Normal: return new List<string> { b, a, c, d };
            default: return new List<string> { c, d, b, a };
        }
    }

    void GiveHint()
    {
        var list = Hints();
        string h = list[hintNext % list.Count];
        hintNext++;
        hintText.text = "DICA: " + h;
        AudioManager.Sfx("pista");
        ShowBanner(h, 4f);
    }

    void Interact()
    {
        Vector2 pf = Feet(player);
        if (Vector2.Distance(Feet(mom), pf) < TalkDistance) { FoundMom(); return; }
        foreach (var k in kidnappers)
            if (Vector2.Distance(Feet(k), pf) < TalkDistance + 0.15f) { kidnapper = k; Kidnapped(); return; }
        if (Vector2.Distance(shop.position, pf) < TalkDistance + 0.2f) { OpenShop(); return; }
        foreach (var l in lookalikes)
            if (Vector2.Distance(Feet(l), pf) < TalkDistance)
            {
                // falar com a mamae errada custa 3 segundos (no maximo uma vez a cada 1,5s)
                if (Time.time < wrongMomUntil) return;
                wrongMomUntil = Time.time + 1.5f;
                mistakes++;
                AudioManager.Sfx("voltar");
                if (mode != GameMode.Livre)
                {
                    timeLeft -= Difficulty.WrongMomPenalty;
                    ShowBanner("ESSA NÃO É A MAMÃE! -" + Difficulty.WrongMomPenalty + "s\nA SUA TEM " + def.Outfit, 2.2f);
                }
                else ShowBanner("ESSA NÃO É A MAMÃE! A SUA TEM " + def.Outfit, 2f);
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
            new ShopItem { name = "ESCUDO", desc = "OS 2 PRÓXIMOS ESBARRÕES NÃO ATRAPALHAM (VALE CONTRA O POLICIAL GRANDÃO)", price = 8,
                owned = () => boughtShield, buy = () => { boughtShield = true; shields += 2; } },
            new ShopItem { name = "TÊNIS", desc = "ANDA 30% MAIS RÁPIDO", price = 10,
                owned = () => boughtShoes, buy = () => { boughtShoes = true; pc.speedBonus = 1.3f; } },
            new ShopItem { name = "ÓCULOS", desc = "ENXERGA UM POUCO MAIS LONGE (A CÂMERA ABRE)", price = 8,
                owned = () => boughtGlasses, buy = () => { boughtGlasses = true; camSize += 0.8f; } },
        };
        if (mode != GameMode.Livre)
            shopItems.Add(new ShopItem { name = "RELÓGIO", desc = "+15 SEGUNDOS", price = 10,
                owned = () => boughtClock, buy = () => { boughtClock = true; timeLeft += 15f; } });
        if (dif.Totems > 0)
            shopItems.Add(new ShopItem { name = "DICA", desc = "MAIS UMA DICA DO QUE A MAMÃE ESTAVA FAZENDO", price = 6,
                owned = () => boughtHint, buy = () => { boughtHint = true; GiveHint(); } });
        if (dif.Dark || HasBlackout)
            shopItems.Add(new ShopItem { name = "LANTERNA", desc = "ENXERGA MAIS LONGE NO ESCURO" + (HasBlackout ? " (QUANDO A LUZ ACABAR)" : ""), price = 7,
                owned = () => boughtLamp, buy = () => { boughtLamp = true; } });
        if (HasRain)
            shopItems.Add(new ShopItem { name = "GUARDA-CHUVA", desc = "QUANDO CHOVER, A TELA NÃO EMBAÇA", price = 5,
                owned = () => boughtUmbrella, buy = () => { boughtUmbrella = true; } });
        shopItems.Add(new ShopItem { name = "DISFARCE", desc = "OS POLICIAIS E SEGURANÇAS TE IGNORAM POR 15s", price = 12,
            owned = () => boughtDisguise, buy = () => { boughtDisguise = true; disguiseUntil = Time.time + 15f; } });
        shopItems.Add(new ShopItem { name = "MEGAFONE", desc = "A MAMÃE RESPONDE E UMA SETA APONTA PARA ELA POR 4s", price = 15,
            owned = () => boughtMegaphone, buy = () => { boughtMegaphone = true; arrowUntil = Time.time + 4f; } });
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
        // o megafone so "grita" quando a crianca sai da lojinha
        if (arrowUntil > Time.time) ShowBanner("MAMÃE: \"FILHO, ESTOU AQUI!\"\nSIGA A SETA!", 3f);
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
            sb.Append(mark + it.name.PadRight(14) + price + "\n");
        }
        sb.Append("\n" + shopItems[shopSel].desc + "\n");
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
            if (it.owned()) { RefreshShop("VOCÊ JÁ COMPROU ESSE ITEM NESTA FASE."); return; }
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
        state = State.Hug;
        prompt.SetActive(false);
        bigMapRoot.SetActive(false);
        foreach (var a in alerts) a.SetActive(false);
        AudioManager.Sfx("vitoria");
        StartCoroutine(HugAnim());
    }

    // a mamae vem ate a crianca, as duas se abracam, sobem coracoes e a camera chega perto
    System.Collections.IEnumerator HugAnim()
    {
        bool last = levelIndex == Levels.Count - 1;
        pc.enabled = false;
        var prb = player.GetComponent<Rigidbody2D>();
        if (prb != null) { prb.linearVelocity = Vector2.zero; prb.bodyType = RigidbodyType2D.Kinematic; }
        foreach (var b in mom.GetComponents<MonoBehaviour>())
            if (b is Skater || b is IdleLookAround || b is Walker) b.enabled = false;
        var mds = mom.GetComponent<DirectionalSprite>();
        var pds = player.GetComponent<DirectionalSprite>();
        float side = mom.position.x >= player.position.x ? 1f : -1f;
        Vector2 target = (Vector2)player.position + new Vector2(side * 0.42f, 0.02f);
        Vector2 from = mom.position;
        float walk = Mathf.Clamp(Vector2.Distance(from, target) / 2.6f, 0.25f, 1.2f);
        camSize = Mathf.Min(camSize, 1.9f);
        float t = 0f;
        while (t < walk)
        {
            t += Time.deltaTime;
            mom.position = Vector2.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / walk));
            if (mds != null) mds.Face(target - from);
            if (pds != null) pds.Face(new Vector2(side, 0f));
            yield return null;
        }
        if (mds != null) mds.Face(new Vector2(-side, 0f));
        if (pds != null) pds.Face(new Vector2(side, 0f));
        // abraco: as duas se apertam um pouquinho e os coracoes sobem
        Vector3 ms = mom.localScale, ps = player.localScale;
        Vector3 mp = mom.position;
        float dur = last ? 3.2f : 2f, nextHeart = 0f;
        var hearts = new List<Transform>();
        t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float sq = 1f + Mathf.Sin(Mathf.Min(1f, t * 2f) * Mathf.PI) * 0.06f;
            mom.localScale = new Vector3(ms.x * sq, ms.y / sq, ms.z);
            player.localScale = new Vector3(ps.x * sq, ps.y / sq, ps.z);
            mom.position = Vector3.Lerp(mp, (Vector3)target - new Vector3(side * 0.08f, 0f, 0f), Mathf.Min(1f, t * 3f));
            if (t >= nextHeart)
            {
                nextHeart = t + 0.28f;
                var h = new GameObject("Coracao").AddComponent<SpriteRenderer>();
                h.sprite = GenSprites.Heart;
                h.sortingOrder = 35;
                h.transform.position = (Vector2)(player.position + mom.position) / 2f + new Vector2(Random.Range(-0.35f, 0.35f), 0.7f);
                hearts.Add(h.transform);
            }
            foreach (var h in hearts)
            {
                if (h == null) continue;
                h.position += Vector3.up * Time.deltaTime * 0.7f;
                var sr = h.GetComponent<SpriteRenderer>();
                sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(sr.color.a - Time.deltaTime * 0.6f));
            }
            yield return null;
        }
        mom.localScale = ms; player.localScale = ps;
        foreach (var h in hearts) if (h != null) Destroy(h.gameObject);
        state = State.Won;
        ShowResult();
    }

    void ShowResult()
    {
        int score = PhaseScore();
        GameSettings.TotalScore += score;
        string stats = "TEMPO: " + elapsed.ToString("0.0") + "s   MOEDAS PEGAS: " + coinsTaken + "/" + dif.Coins +
                       "\nESBARRÕES: " + hits + "   ENGANOS: " + mistakes + "\nPONTOS DA FASE: " + score;
        if (levelIndex < Levels.Count - 1)
        {
            End("VOCÊ ACHOU A MAMÃE!\n\n" + stats + "\nTOTAL: " + GameSettings.TotalScore + "   MOEDAS GUARDADAS: " + GameSettings.Coins,
                "ENTER: PRÓXIMA FASE   M: MENU");
        }
        else
        {
            bool rec = GameSettings.SaveRecord(mode, GameSettings.TotalScore);
            ShowEnding();
            End("FIM!\nA MAMÃE TE ABRAÇOU BEM APERTADO\nE VOCÊS VOLTARAM PARA CASA JUNTINHOS.\n\n" + stats + "\nTOTAL: " + GameSettings.TotalScore +
                (rec ? "   NOVO RECORDE!" : "   RECORDE: " + GameSettings.Record(mode)), "R: JOGAR DE NOVO   M: MENU");
        }
    }

    // A sequestradora pega a crianca pela mao e as duas vao embora pela saida; a tela escurece e o jogo acaba.
    void Kidnapped()
    {
        if (state != State.Playing) return;
        state = State.Kidnap;
        prompt.SetActive(false);
        bigMapRoot.SetActive(false);
        AudioManager.Sfx("derrota");
        StartCoroutine(KidnapAnim());
    }

    static void SetPos(Transform t, Rigidbody2D rb, Vector2 p)
    {
        if (rb != null) rb.position = p;
        t.position = new Vector3(p.x, p.y, t.position.z);
    }

    System.Collections.IEnumerator KidnapAnim()
    {
        pc.enabled = false;   // a crianca para de obedecer o controle
        var w = kidnapper.GetComponent<Walker>();
        if (w != null) w.enabled = false;
        var krb = kidnapper.GetComponent<Rigidbody2D>();
        var prb = player.GetComponent<Rigidbody2D>();
        var kds = kidnapper.GetComponent<DirectionalSprite>();
        var pds = player.GetComponent<DirectionalSprite>();
        foreach (var col in player.GetComponents<Collider2D>()) col.enabled = false;
        foreach (var col in kidnapper.GetComponents<Collider2D>()) col.enabled = false;
        if (prb != null) { prb.linearVelocity = Vector2.zero; prb.bodyType = RigidbodyType2D.Kinematic; prb.interpolation = RigidbodyInterpolation2D.None; }
        if (krb != null) krb.interpolation = RigidbodyInterpolation2D.None;
        ShowBanner("A MULHER DE PRETO NÃO ERA A MAMÃE!\nELA PEGOU VOCÊ PELA MÃO...", 8f);

        // 1) ela vem para o lado da crianca
        Vector2 kp = kidnapper.position;
        Vector2 side = (Vector2)player.position + Vector2.right * 0.5f;
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            if (kds != null) kds.Face(side - kp);
            SetPos(kidnapper, krb, Vector2.Lerp(kp, side, t / 0.6f));
            yield return null;
        }
        if (pds != null) pds.Face(Vector2.right);
        if (kds != null) kds.Face(Vector2.left);
        yield return new WaitForSeconds(0.7f);

        // 2) as duas vao embora juntas, na direcao da entrada do parque, e a tela vai escurecendo
        Vector2 start = player.position;
        Vector2 gate = def.ChildSpawns[0] + Vector2.down * 3f;
        Vector2 dir = (gate - start).sqrMagnitude > 0.01f ? (gate - start).normalized : Vector2.down;
        const float speed = 2.2f, dur = 2.8f;
        var fade = UIKit.MakeCanvas("Sequestro", 25);
        var black = UIKit.Panel(fade.transform, new Color(0, 0, 0, 0), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6000, 4000));
        Vector3 ps = player.localScale, ks = kidnapper.localScale;
        t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            Vector2 cp = (Vector2)player.position + dir * speed * Time.deltaTime;
            SetPos(player, prb, cp);
            SetPos(kidnapper, krb, cp + Vector2.right * 0.45f + dir * 0.12f);
            if (pds != null) pds.Face(dir);
            if (kds != null) kds.Face(dir);
            // passinhos: sobe e desce um pouco
            float bob = Mathf.Abs(Mathf.Sin(t * 12f)) * 0.05f;
            player.localScale = new Vector3(ps.x, ps.y * (1f + bob), ps.z);
            kidnapper.localScale = new Vector3(ks.x, ks.y * (1f + Mathf.Abs(Mathf.Sin(t * 12f + 1.5f)) * 0.05f), ks.z);
            black.color = new Color(0, 0, 0, Mathf.Clamp01((t - 1.0f) / (dur - 1.0f)));
            yield return null;
        }
        player.localScale = ps; kidnapper.localScale = ks;
        Destroy(fade.gameObject);

        state = State.Lost;
        string why = "VOCÊ FOI SEQUESTRADO!\nA MULHER DE PRETO NÃO ERA A MAMÃE.\nA SUA TEM " + def.Outfit;
        if (mode == GameMode.ContraOTempo)
        {
            bool rec = GameSettings.SaveRecord(mode, GameSettings.TotalScore);
            End(why + "\n\nVOCÊ CHEGOU ATÉ A FASE " + (levelIndex + 1) + "\nTOTAL: " + GameSettings.TotalScore +
                (rec ? "\nNOVO RECORDE!" : "\nRECORDE: " + GameSettings.Record(mode)), "R: COMEÇAR DE NOVO   M: MENU");
        }
        else End(why, "R: TENTAR DE NOVO   M: MENU");
    }

    void TimeUp()
    {
        timeLeft = 0f;
        state = State.Lost;
        AudioManager.Sfx("derrota");
        if (mode == GameMode.ContraOTempo)
        {
            bool rec = GameSettings.SaveRecord(mode, GameSettings.TotalScore);
            End("O TEMPO ACABOU!\nVOCÊ CHEGOU ATÉ A FASE " + (levelIndex + 1) + "\nTOTAL: " + GameSettings.TotalScore +
                (rec ? "\nNOVO RECORDE!" : "\nRECORDE: " + GameSettings.Record(mode)), "R: COMEÇAR DE NOVO   M: MENU");
        }
        else End("O TEMPO ACABOU!\nA MAMÃE ESTÁ ESPERANDO...", "R: TENTAR DE NOVO   M: MENU");
    }

    // final da historia: a mamae e a crianca juntas, com um coracao no meio (sem creditos)
    void ShowEnding()
    {
        var t = overlay.transform;
        var pic = UIKit.Rect(t, "Final", new Vector2(0.5f, 0.5f), new Vector2(0, 215), new Vector2(400, 150));
        var momPic = momSprites != null ? momSprites[0] : Resources.Load<Sprite>("Art/Mae/east");
        var kidPic = Resources.Load<Sprite>("Art/Crianca/west");
        UIKit.Picture(pic, momPic, new Vector2(0.5f, 0.5f), new Vector2(-46, 0), new Vector2(140, 140)).preserveAspect = true;
        UIKit.Picture(pic, kidPic, new Vector2(0.5f, 0.5f), new Vector2(46, -8), new Vector2(120, 120)).preserveAspect = true;
        UIKit.Picture(pic, GenSprites.Heart, new Vector2(0.5f, 0.5f), new Vector2(0, 62), new Vector2(46, 40));
        centerText.rectTransform.anchoredPosition = new Vector2(0, -80);
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
        hintNext = 0;
        ShowBanner("A MAMÃE MUDOU DE LUGAR! A DICA ANTIGA NÃO VALE MAIS", 2.5f);
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
        // atalhos so no editor, para testar: F2 proxima fase, F3 perto da mamae, F4 +10 moedas,
        // F6 comeca o apagao, F7 comeca a chuva, F8 vai ate o policial grandao
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && state == State.Playing)
        {
            if (kb.f3Key.wasPressedThisFrame) pc.Teleport(mom.position + Vector3.left * 0.7f);
            if (kb.f4Key.wasPressedThisFrame) GameSettings.Coins += 10;
            if (kb.f6Key.wasPressedThisFrame && HasBlackout) blackoutAt = elapsed;
            if (kb.f7Key.wasPressedThisFrame && HasRain) rainAt = elapsed;
            if (kb.f8Key.wasPressedThisFrame)
                foreach (var w in FindObjectsByType<Walker>(FindObjectsSortMode.None))
                    if (w.bigCop) { pc.Teleport(w.transform.position + Vector3.left * 1.2f); break; }
            if (kb.f2Key.wasPressedThisFrame && levelIndex < Levels.Count - 1) { NextLevel(); return; }
        }
#endif

        UpdateHud();

        // a tecla que terminou a fase (E) nao pode pular a tela de resultado no mesmo quadro
        if (state == State.Playing || state == State.Kidnap || state == State.Hug || Time.frameCount == endedFrame) return;
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

        // apagao: 10 segundos de escuridao
        if (elapsed >= blackoutAt && blackoutUntil < 0f)
        {
            blackoutUntil = elapsed + BlackoutSeconds;
            darkness.gameObject.SetActive(true);
            darkness.localScale = Vector3.one * 6f;
            AudioManager.Sfx("esbarrao");
            ShowBanner("A ENERGIA CAIU!" + (boughtLamp ? " SUA LANTERNA AJUDA." : ""), 3f);
        }
        if (blackoutUntil > 0f && elapsed >= blackoutUntil && darkness.gameObject.activeSelf && !dif.Dark)
        {
            darkness.gameObject.SetActive(false);
            ShowBanner("A ENERGIA VOLTOU!", 1.5f);
        }
        // chuva
        if (elapsed >= rainAt && rainUntil < 0f)
        {
            rainUntil = elapsed + RainSeconds;
            ShowBanner("COMEÇOU A CHOVER!" + (boughtUmbrella ? " O GUARDA-CHUVA TE PROTEGE." : " A TELA VAI EMBAÇAR."), 2.5f);
        }
        UpdateRain();

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

        // "APERTE E" so aparece em cima de quem esta perto
        Transform near = null;
        float best = TalkDistance;
        float lift = 0.8f;
        if (Vector2.Distance(Feet(mom), pf) < best) { best = Vector2.Distance(Feet(mom), pf); near = mom; }
        foreach (var l in lookalikes)
        {
            float d = Vector2.Distance(Feet(l), pf);
            if (d < best) { best = d; near = l; lift = 0.8f; }
        }
        for (int i = 0; i < kidnappers.Count; i++)
        {
            var k = kidnappers[i];
            float d = Vector2.Distance(Feet(k), pf);
            if (d < best) { best = d; near = k; lift = 1.0f; }
            // "!" vermelho quando a crianca chega perto (com um som curto na primeira vez)
            bool close = d < AlertDistance;
            alerts[i].SetActive(close);
            if (close)
            {
                alerts[i].transform.localPosition = new Vector3(0, 1.05f + Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.08f, 0);
                if (!alerted[i]) { alerted[i] = true; AudioManager.Sfx("alerta", 0.8f); }
            }
            else if (d > AlertDistance + 1f) alerted[i] = false;
        }
        foreach (var tt in totems)
        {
            if (tt.used) continue;
            float d = Vector2.Distance(tt.t.position, pf);
            if (d < best) { best = d; near = tt.t; lift = 1.15f; }
        }
        float ds = Vector2.Distance(shop.position, pf);
        if (ds < TalkDistance + 0.2f && near != mom) { near = shop; lift = 2.1f; }
        prompt.SetActive(near != null && !pc.Frozen);
        if (near != null) prompt.transform.position = (Vector2)near.position + Vector2.up * lift;

        if (Inp.Pressed(K.Interact) && !pc.Frozen) Interact();
    }

    void LateUpdate()
    {
        UpdateOccluders();
        if (player == null) return;
        float k = 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, camSize, k);
        if (darkness != null && darkness.gameObject.activeSelf)
        {
            // Extra Hard: raio normal; apagao: um pouco maior. A lanterna aumenta os dois.
            float target = (dif.Dark ? 1f : BlackoutRadius / Difficulty.DarkRadius) * (boughtLamp ? 1.6f : 1f) * darkScale;
            darkness.position = new Vector3(player.position.x, player.position.y - FeetOffset * 0.5f, 0f);
            darkness.localScale = Vector3.one * Mathf.Lerp(darkness.localScale.x, target, k);
        }
        // seta do megafone: em volta da crianca, apontando para a mamae
        bool showArrow = Time.time < arrowUntil && state == State.Playing;
        arrow.gameObject.SetActive(showArrow);
        if (showArrow)
        {
            Vector2 dir = ((Vector2)mom.position - (Vector2)player.position).normalized;
            arrow.position = (Vector2)player.position + dir * 1.1f;
            arrow.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }
        var target2 = new Vector3(player.position.x, player.position.y, -10);
        cam.transform.position -= shakeOff;   // tira a tremida do quadro anterior antes de seguir a crianca
        shakeOff = Vector3.zero;
        cam.transform.position = Vector3.Lerp(cam.transform.position, target2, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        ClampCamera();
        // tremida no esbarrao e quando o policial pega
        if (Time.time < shakeUntil)
        {
            float a = shakeAmp * Mathf.Clamp01((shakeUntil - Time.time) / 0.25f);
            shakeOff = new Vector3(Random.Range(-a, a), Random.Range(-a, a), 0f);
            cam.transform.position += shakeOff;
        }
    }

    Vector3 shakeOff;

    void Shake(float seconds, float amp)
    {
        shakeUntil = Time.time + seconds;
        shakeAmp = amp;
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
        if (boughtShoes) items.Add("TÊNIS");
        if (boughtGlasses) items.Add("ÓCULOS");
        if (boughtLamp) items.Add("LANTERNA");
        if (boughtUmbrella) items.Add("GUARDA-CHUVA");
        if (Disguised) items.Add("DISFARCE " + Mathf.CeilToInt(disguiseUntil - Time.time) + "s");
        if (pc.Frozen) items.Add("PRESO " + Mathf.CeilToInt(pc.FrozenLeft) + "s");
        if (blackoutUntil > 0f && elapsed < blackoutUntil && !dif.Dark) items.Add("SEM LUZ " + Mathf.CeilToInt(blackoutUntil - elapsed) + "s");
        rightText.text = GameSettings.ModeName(mode) + " - " + GameSettings.DifName(dif.Dif) + "   ESBARRÕES " + hits + "\n" + (items.Count > 0 ? string.Join(" ", items) + "   " : "") + "ESC: PAUSA";
        if (bannerPanel.gameObject.activeSelf && Time.unscaledTime > bannerUntil) bannerPanel.gameObject.SetActive(false);
        if (youMarker != null) youMarker.SetActive(state == State.Playing && Time.time < youMarkerUntil);
        // disfarce: a crianca fica meio acinzentada
        var psr = player.GetComponent<SpriteRenderer>();
        psr.color = Disguised ? new Color(0.65f, 0.7f, 0.65f) : Color.white;

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
    public float[,] ice;   // retangulos de gelo: ali a crianca escorrega
    Rigidbody2D rb;
    DirectionalSprite ds;
    float slowUntil, slowFactor = 1f, frozenUntil;

    public bool Frozen => Time.time < frozenUntil;
    public float FrozenLeft => Mathf.Max(0f, frozenUntil - Time.time);

    void Awake() { rb = GetComponent<Rigidbody2D>(); ds = GetComponent<DirectionalSprite>(); }

    public void Slow(float seconds, float factor) { slowUntil = Time.time + seconds; slowFactor = factor; }
    public void Freeze(float seconds) { frozenUntil = Time.time + seconds; rb.linearVelocity = Vector2.zero; }

    public void Teleport(Vector2 pos)
    {
        rb.position = pos;
        transform.position = pos;
        rb.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    bool OnIce()
    {
        if (ice == null) return false;
        Vector2 f = rb.position + Vector2.down * 0.2f;
        for (int i = 0; i < ice.GetLength(0); i++)
            if (f.x >= ice[i, 0] && f.x <= ice[i, 2] && f.y >= ice[i, 1] && f.y <= ice[i, 3]) return true;
        return false;
    }

    void FixedUpdate()
    {
        if (Frozen) { rb.linearVelocity = Vector2.zero; return; }
        var v = Inp.Move();
        float k = speedBonus * (Time.time < slowUntil ? slowFactor : 1f);
        if (OnIce())
        {
            // no gelo: demora para acelerar, para e virar (escorrega), mas fica um pouco mais rapido
            var want = v * speed * k * 1.15f;
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, want, (v.sqrMagnitude > 0.01f ? 5f : 2.2f) * Time.fixedDeltaTime);
        }
        else rb.linearVelocity = v * speed * k;
        if (v.sqrMagnitude > 0.01f) ds.Face(v);
    }
}

// Anda de ponto em ponto (ida e volta). Se for seguranca, esbarrar na crianca a deixa lenta;
// se for o policial grandao, ele leva a crianca para longe.
public class Walker : MonoBehaviour
{
    public List<Vector2> route;
    public float speed = 1.5f;
    public LevelController level;
    public bool bigCop;
    Rigidbody2D rb;
    DirectionalSprite ds;
    int idx = 1, dir = 1;
    float waitUntil, lastHit = -10f;
    bool goHome;

    void Awake() { rb = GetComponent<Rigidbody2D>(); ds = GetComponent<DirectionalSprite>(); }

    // para um pouco; se back, depois volta para a ronda
    public void Hold(float seconds, bool back)
    {
        waitUntil = Time.time + seconds;
        lastHit = Time.time + seconds;
        goHome = back;
    }

    // leva a crianca: aparece ao lado dela, espera e depois volta para a ronda
    public void Carry(Vector2 to, float seconds)
    {
        rb.position = to;
        transform.position = to;
        ds.Face(Vector2.left);
        Hold(seconds, true);
    }

    void FixedUpdate()
    {
        if (route == null || route.Count < 2 || Time.time < waitUntil) return;
        if (goHome)
        {
            goHome = false;
            rb.position = route[idx];
            transform.position = route[idx];
            return;
        }
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

    void OnCollisionEnter2D(Collision2D c) => Bump(c.collider);
    void OnCollisionStay2D(Collision2D c) => Bump(c.collider);
    // segurancas e policiais sao atravessaveis (gatilho): a crianca passa, mas leva o efeito
    void OnTriggerEnter2D(Collider2D c) => Bump(c);
    void OnTriggerStay2D(Collider2D c) => Bump(c);

    void Bump(Collider2D c)
    {
        if (c.GetComponent<PlayerController>() == null) return;
        if (Time.time < waitUntil) return;
        // para um instante quando encosta na crianca
        waitUntil = Time.time + 0.6f;
        if (level == null || Time.time - lastHit <= Difficulty.HitSlowSeconds + 0.5f) return;
        lastHit = Time.time;
        if (bigCop) level.Grab(this);
        else level.Hit();
    }
}

// Patinador: da voltas na pista de gelo, rapido (a mamae tambem, na fase da pista).
public class Skater : MonoBehaviour
{
    public Vector2[] loop;
    public float speed = 2.5f, scale = 1f, feet = 0.2f;
    DirectionalSprite ds;
    Vector2 center;
    float dist, total;
    float[] seg;

    void Start()
    {
        ds = GetComponent<DirectionalSprite>();
        foreach (var p in loop) center += p;
        center /= loop.Length;
        seg = new float[loop.Length];
        for (int i = 0; i < loop.Length; i++) { seg[i] = Vector2.Distance(P(i), P(i + 1)); total += seg[i]; }
        dist = Random.value * total;
        Place(0f);
    }

    Vector2 P(int i) => center + (loop[i % loop.Length] - center) * scale;

    void Place(float dt)
    {
        dist = Mathf.Repeat(dist + speed * dt, total);
        float d = dist;
        for (int i = 0; i < seg.Length; i++)
        {
            if (d <= seg[i] || i == seg.Length - 1)
            {
                Vector2 a = P(i), b = P(i + 1);
                Vector2 pos = Vector2.Lerp(a, b, seg[i] > 0f ? Mathf.Clamp01(d / seg[i]) : 0f);
                transform.position = pos + Vector2.up * feet;
                if (dt > 0f) ds.Face((b - a).normalized);
                return;
            }
            d -= seg[i];
        }
    }

    void Update() { Place(Time.deltaTime); }
}

// Fica parada olhando em volta (a mamae e quem esta parado na multidao).
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
