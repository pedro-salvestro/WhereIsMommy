using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Monta a fase do shopping em codigo e controla o fluxo (tempo, vitoria, derrota, pausa).
public class LevelController : MonoBehaviour
{
    enum State { Playing, Won, Lost, Paused }

    // Mapa de referencia: o cenario e desenhado em 2000x1116 (coordenadas de tela) e convertido p/ mundo.
    const float MapW = 2000f, MapH = 1116f;
    const float WorldW = 27.52f, WorldH = 15.36f;

    // Retangulos bloqueados (x0, y0, x1, y1) sobre o cenario.
    static readonly int[][] Walls =
    {
        // bordas
        new[]{0,0,2000,45}, new[]{0,1080,2000,1116}, new[]{0,0,55,310}, new[]{0,665,55,1116},
        new[]{0,495,15,670}, new[]{1985,495,2000,670}, new[]{1940,0,2000,320}, new[]{1940,665,2000,1116},
        // lojas e vazios
        new[]{0,310,300,500}, new[]{480,0,850,500}, new[]{1150,0,1560,500}, new[]{1740,320,2000,500},
        new[]{0,665,300,740}, new[]{440,665,865,735}, new[]{810,735,865,1116}, new[]{0,1040,860,1116},
        new[]{440,745,810,930}, new[]{1135,735,1190,1116}, new[]{1135,665,1570,735}, new[]{1740,665,2000,735},
        new[]{1190,1040,1940,1116}, new[]{1190,745,1565,930}, new[]{1740,740,1940,870},
        // mobiliario
        new[]{745,555,845,640}, new[]{1155,555,1262,640}, new[]{965,300,1000,390}, new[]{965,530,1035,620},
        new[]{935,695,1065,735}, new[]{850,45,1150,150}, new[]{850,150,925,230}, new[]{1075,150,1150,230},
        new[]{855,400,900,490}, new[]{1100,400,1150,490}, new[]{855,255,895,360}, new[]{1105,255,1145,360},
        new[]{865,735,920,830}, new[]{1085,735,1135,830}, new[]{865,870,915,960}, new[]{1090,870,1130,960},
        new[]{865,970,925,1085}, new[]{1105,1010,1135,1060},
    };

    State state = State.Playing;
    float timeLeft, elapsed;
    int hits;
    Text timerText, hitsText, centerText, bannerText;
    Image overlay;
    Transform player;

    static Vector2 W(float x, float y) => new Vector2((x / MapW - 0.5f) * WorldW, (0.5f - y / MapH) * WorldH);

    void Start()
    {
        Time.timeScale = 1f;
        UIKit.EnsureEventSystem();
        timeLeft = GameSettings.Mode == GameMode.ContraOTempo ? 60f : GameSettings.TimeLimit;
        BuildWorld();
        BuildHud();
        AudioManager.StopMusic();
        if (GameSettings.Mode == GameMode.Historia) ShowBanner("A CRIANCA SE PERDEU NO SHOPPING...\nENCONTRE A MAMAE!", 4f);
        else if (GameSettings.Mode == GameMode.ContraOTempo) ShowBanner("CHEGUE ATE A MAMAE O MAIS RAPIDO POSSIVEL!", 3f);
    }

    void BuildWorld()
    {
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = WorldH / 2f;
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.AddComponent<AudioListener>();

        var bg = new GameObject("Cenario").AddComponent<SpriteRenderer>();
        bg.sprite = Resources.Load<Sprite>("Art/Cenarios/cenario_principal_v1");
        bg.sortingOrder = -10;

        var walls = new GameObject("Paredes");
        foreach (var w in Walls)
        {
            var a = W(w[0], w[1]);
            var b = W(w[2], w[3]);
            var go = new GameObject("Wall");
            go.transform.SetParent(walls.transform, false);
            go.transform.position = (a + b) / 2f;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
        }

        // Crianca
        var p = MakeCharacter("Crianca", "Crianca", W(350, 800), 6);
        var rb = p.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var pc = p.AddComponent<CircleCollider2D>();
        pc.radius = 0.2f;
        pc.offset = new Vector2(0, -0.2f);
        p.AddComponent<PlayerController>();
        p.tag = "Untagged";
        player = p.transform;

        // Mamae
        var m = MakeCharacter("Mamae", "Mae", W(1830, 260), 5);
        var mc = m.AddComponent<CircleCollider2D>();
        mc.isTrigger = true;
        mc.radius = 1.0f;
        var goal = m.AddComponent<MommyGoal>();
        goal.level = this;
        var prompt = new GameObject("Prompt");
        prompt.transform.SetParent(m.transform, false);
        prompt.transform.localPosition = new Vector3(0, 0.75f, 0);
        var tm = prompt.AddComponent<TextMesh>();
        tm.text = "APERTE E";
        tm.font = UIKit.PixelFont;
        tm.GetComponent<MeshRenderer>().material = tm.font.material;
        tm.characterSize = 0.07f;
        tm.fontSize = 48;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.85f, 0.3f);
        prompt.GetComponent<MeshRenderer>().sortingOrder = 20;
        goal.prompt = prompt;
        prompt.SetActive(false);

        // Pessoas e policiais patrulhando
        Patrol("Policial", new Color(1, 1, 1), 1.6f, W(70, 590), W(690, 590));
        Patrol("Policial", new Color(0.8f, 0.9f, 1f), 1.4f, W(1000, 760), W(1000, 1000));
        Patrol("Policial", new Color(1f, 0.85f, 0.85f), 1.8f, W(1300, 590), W(1900, 590));
        Patrol("Policial", new Color(0.85f, 1f, 0.85f), 1.2f, W(370, 340), W(370, 470));
    }

    GameObject MakeCharacter(string name, string folder, Vector2 pos, int order)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = order;
        var ds = go.AddComponent<DirectionalSprite>();
        ds.Load(folder);
        // sprites 48px: PPU do importador faz o personagem ter ~0.9 unidades
        return go;
    }

    void Patrol(string folder, Color tint, float speed, Vector2 a, Vector2 b)
    {
        var go = MakeCharacter("NPC", folder, a, 4);
        go.GetComponent<SpriteRenderer>().color = tint;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var c = go.AddComponent<CircleCollider2D>();
        c.radius = 0.25f;
        c.offset = new Vector2(0, -0.15f);
        var pn = go.AddComponent<PatrolNPC>();
        pn.a = a;
        pn.b = b;
        pn.speed = speed;
        pn.level = this;
    }

    void BuildHud()
    {
        var canvas = UIKit.MakeCanvas("HUD", 10);
        var cream = new Color(1f, 0.93f, 0.78f);
        UIKit.Panel(canvas.transform, new Color(0, 0, 0, 0.6f), new Vector2(0.5f, 0f), new Vector2(0, 32), new Vector2(1280, 64));
        timerText = UIKit.Label(canvas.transform, "", 24, cream, new Vector2(0.5f, 0f), new Vector2(0, 32), new Vector2(500, 50));
        hitsText = UIKit.Label(canvas.transform, "", 14, cream, new Vector2(0f, 0f), new Vector2(230, 32), new Vector2(420, 50), TextAnchor.MiddleLeft);
        UIKit.Label(canvas.transform, "ESC: PAUSA", 14, cream, new Vector2(1f, 0f), new Vector2(-170, 32), new Vector2(300, 50), TextAnchor.MiddleRight);
        bannerText = UIKit.Label(canvas.transform, "", 20, new Color(1f, 0.85f, 0.3f), new Vector2(0.5f, 1f), new Vector2(0, -260), new Vector2(1100, 120));
        overlay = UIKit.Panel(canvas.transform, new Color(0, 0, 0, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2400, 1400));
        centerText = UIKit.Label(overlay.transform, "", 26, cream, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 500));
        overlay.gameObject.SetActive(false);
    }

    float bannerUntil;
    void ShowBanner(string s, float seconds) { bannerText.text = s; bannerUntil = Time.time + seconds; }

    public void Hit()
    {
        if (state != State.Playing) return;
        hits++;
        if (GameSettings.Mode != GameMode.Livre) timeLeft -= 5f;
        AudioManager.Sfx("voltar");
        ShowBanner(GameSettings.Mode == GameMode.Livre ? "ESBARRAO!" : "ESBARRAO! -5s", 1.2f);
    }

    public void ReachMommy()
    {
        if (state != State.Playing) return;
        state = State.Won;
        int score = 1000 + Mathf.Max(0, 500 - hits * 100);
        string extra = "";
        if (GameSettings.Mode != GameMode.Livre) score += Mathf.RoundToInt(timeLeft * 10f);
        if (GameSettings.Mode == GameMode.ContraOTempo)
        {
            float best = PlayerPrefs.GetFloat("best_time", 9999f);
            if (elapsed < best) { PlayerPrefs.SetFloat("best_time", elapsed); best = elapsed; extra = "\nNOVO RECORDE!"; }
            extra += "\nMELHOR: " + best.ToString("0.0") + "s";
        }
        AudioManager.Sfx("iniciar");
        End("VOCE ACHOU A MAMAE!\n\nTEMPO: " + elapsed.ToString("0.0") + "s\nESBARROES: " + hits + "\nPONTOS: " + score + extra);
    }

    void End(string text)
    {
        centerText.text = text + "\n\nR: JOGAR DE NOVO   M: MENU";
        overlay.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    void Update()
    {
        if (state == State.Playing || state == State.Paused)
        {
            if (Inp.Pressed(K.Pause))
            {
                bool pause = state == State.Playing;
                state = pause ? State.Paused : State.Playing;
                Time.timeScale = pause ? 0f : 1f;
                overlay.gameObject.SetActive(pause);
                centerText.text = pause ? "PAUSADO\n\nESC: CONTINUAR\nR: REINICIAR   M: MENU" : "";
                AudioManager.Sfx(pause ? "voltar" : "confirmar");
            }
        }

        if (state == State.Playing)
        {
            elapsed += Time.deltaTime;
            if (GameSettings.Mode != GameMode.Livre)
            {
                timeLeft -= Time.deltaTime;
                if (timeLeft <= 0f)
                {
                    timeLeft = 0f;
                    state = State.Lost;
                    End("O TEMPO ACABOU!\nA MAMAE ESTA ESPERANDO...");
                }
            }
        }

        timerText.text = GameSettings.Mode == GameMode.Livre ? "LIVRE  " + Fmt(elapsed) : Fmt(timeLeft);
        timerText.color = GameSettings.Mode != GameMode.Livre && timeLeft < 15f ? new Color(1f, 0.4f, 0.35f) : new Color(1f, 0.93f, 0.78f);
        hitsText.text = "ESBARROES: " + hits;
        if (bannerText.text != "" && Time.time > bannerUntil) bannerText.text = "";

        if (state != State.Playing)
        {
            if (Inp.Pressed(K.Restart)) { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
            else if (Inp.Pressed(K.Menu)) { Time.timeScale = 1f; SceneManager.LoadScene("Menu"); }
        }
    }

    static string Fmt(float t) { int s = Mathf.CeilToInt(t); return (s / 60).ToString("00") + ":" + (s % 60).ToString("00"); }
}

public class PlayerController : MonoBehaviour
{
    public float speed = 4.5f;
    Rigidbody2D rb;
    DirectionalSprite ds;

    void Awake() { rb = GetComponent<Rigidbody2D>(); ds = GetComponent<DirectionalSprite>(); }

    void FixedUpdate()
    {
        var v = Inp.Move();
        rb.linearVelocity = v * speed;
        if (v.sqrMagnitude > 0.01f) ds.Face(v);
    }
}

public class PatrolNPC : MonoBehaviour
{
    public Vector2 a, b;
    public float speed = 1.5f;
    public LevelController level;
    Rigidbody2D rb;
    DirectionalSprite ds;
    Vector2 target;
    float lastHit = -10f;

    void Awake() { rb = GetComponent<Rigidbody2D>(); ds = GetComponent<DirectionalSprite>(); }
    void Start() { target = b; }

    void FixedUpdate()
    {
        Vector2 pos = rb.position;
        if (Vector2.Distance(pos, target) < 0.1f) target = target == b ? a : b;
        Vector2 dir = (target - pos).normalized;
        ds.Face(dir);
        rb.MovePosition(pos + dir * speed * Time.fixedDeltaTime);
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.collider.GetComponent<PlayerController>() == null || Time.time - lastHit < 1.5f) return;
        lastHit = Time.time;
        level.Hit();
    }
}

public class MommyGoal : MonoBehaviour
{
    public LevelController level;
    public GameObject prompt;
    bool near;

    void OnTriggerEnter2D(Collider2D c) { if (c.GetComponent<PlayerController>() != null) { near = true; prompt.SetActive(true); } }
    void OnTriggerExit2D(Collider2D c) { if (c.GetComponent<PlayerController>() != null) { near = false; prompt.SetActive(false); } }
    void Update() { if (near && Inp.Pressed(K.Interact)) level.ReachMommy(); }
}
