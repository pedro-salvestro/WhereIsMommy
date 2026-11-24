using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Constroi o menu inicial em codigo (fundo, titulo, opcoes) e trata teclado/mouse.
public class MenuController : MonoBehaviour
{
    public const string LevelScene = "Fase_Shopping";
    static readonly Color Cream = new Color(1f, 0.93f, 0.78f);
    static readonly Color Orange = new Color(0.96f, 0.64f, 0.35f);
    const string DefaultHint = "SETAS / WASD: NAVEGAR   ENTER: ESCOLHER   ESC: VOLTAR";

    class Row
    {
        public Func<string> text;
        public Action confirm;
        public Action<int> adjust;
        public string desc;
        public Text label;
    }

    readonly List<Row> rows = new List<Row>();
    Transform rowsRoot;
    Text title, hint;
    int sel;
    Action onBack;

    void Start()
    {
        Time.timeScale = 1f;
        UIKit.EnsureEventSystem();
        var canvas = UIKit.MakeCanvas("MenuCanvas");
        string[] bgs = { "menu_bg_shopping", "menu_bg_mercado", "menu_bg_parque" };
        var bg = UIKit.Picture(canvas.transform, Resources.Load<Sprite>("Art/Menu/" + bgs[UnityEngine.Random.Range(0, bgs.Length)]),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1280, 720));
        bg.rectTransform.anchorMin = Vector2.zero;
        bg.rectTransform.anchorMax = Vector2.one;
        bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;
        UIKit.Panel(canvas.transform, new Color(0, 0, 0, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2400, 1400));

        title = UIKit.Label(canvas.transform, "ONDE ESTA\nA MAMAE?", 54, Orange, new Vector2(0.5f, 1f), new Vector2(0, -125), new Vector2(1100, 200));
        hint = UIKit.Label(canvas.transform, DefaultHint, 12, Cream,
            new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(1200, 60));
        rowsRoot = UIKit.Rect(canvas.transform, "Rows", new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(900, 400));

        AudioManager.Music("musica_menu");
        ShowMain();
    }

    void ShowMain()
    {
        onBack = null;
        Build(
            new Row { text = () => "JOGAR", confirm = ShowModes },
            new Row { text = () => "OPCOES", confirm = ShowOptions },
            new Row { text = () => "SAIR", confirm = Quit });
    }

    void ShowModes()
    {
        onBack = ShowMain;
        Build(
            new Row { text = () => "HISTORIA", confirm = () => ShowDifficulty(GameMode.Historia),
                desc = "5 FASES, CADA UMA MAIS DIFICIL E COM MAPA MAIOR. PERDEU? TENTA A FASE DE NOVO." },
            new Row { text = () => "CONTRA O TEMPO", confirm = () => ShowDifficulty(GameMode.ContraOTempo),
                desc = "MENOS TEMPO NAS 5 FASES. SE O TEMPO ACABAR, RECOMECA DA FASE 1." },
            new Row { text = () => "LIVRE", confirm = () => ShowDifficulty(GameMode.Livre),
                desc = "SEM CRONOMETRO: EXPLORE AS 5 FASES COM CALMA." },
            new Row { text = () => "VOLTAR", confirm = ShowMain });
    }

    void ShowDifficulty(GameMode m)
    {
        onBack = ShowModes;
        string clock = m == GameMode.Livre ? "" : "MAIS TEMPO, ";
        Build(
            new Row { text = () => "FACIL", confirm = () => Play(m, Dificuldade.Facil),
                desc = clock + "POUCAS MULHERES PARECIDAS E A DICA DIZ O LUGAR CERTO." + RecordText(m, Dificuldade.Facil) },
            new Row { text = () => "NORMAL", confirm = () => Play(m, Dificuldade.Normal),
                desc = "MAIS MULHERES PARECIDAS. A DICA DA DOIS LUGARES: SO UM E O CERTO." + RecordText(m, Dificuldade.Normal) },
            new Row { text = () => "DIFICIL", confirm = () => Play(m, Dificuldade.Dificil),
                desc = (m == GameMode.Livre ? "" : "MENOS TEMPO, ") + "MUITAS MAMAES PARECIDAS. A DICA SO DIZ A REGIAO DO MAPA." + RecordText(m, Dificuldade.Dificil) },
            new Row { text = () => "EXTRA HARD (NO ESCURO)", confirm = () => Play(m, Dificuldade.ExtraHard),
                desc = "COMO O DIFICIL, MAS TUDO FICA ESCURO: VOCE SO ENXERGA POUCOS PASSOS EM VOLTA." + RecordText(m, Dificuldade.ExtraHard) },
            new Row { text = () => "VOLTAR", confirm = ShowModes });
        sel = 1; // NORMAL ja vem selecionado
        Refresh();
    }

    void ShowOptions()
    {
        onBack = ShowMain;
        Build(
            new Row
            {
                text = () => "VOLUME  < " + Mathf.RoundToInt(GameSettings.Volume * 100) + "% >",
                adjust = d => { GameSettings.Volume = Mathf.Clamp01(GameSettings.Volume + d * 0.1f); AudioManager.Refresh(); },
                confirm = () => { }
            },
            new Row
            {
                text = () => "MUSICA  " + (GameSettings.MusicOn ? "LIGADA" : "DESLIGADA"),
                adjust = d => ToggleMusic(),
                confirm = ToggleMusic
            },
            new Row { text = () => "VOLTAR", confirm = ShowMain });
    }

    void ToggleMusic()
    {
        GameSettings.MusicOn = !GameSettings.MusicOn;
        AudioManager.Refresh();
        AudioManager.Sfx("alternar");
    }

    static string RecordText(GameMode m, Dificuldade d)
    {
        if (m == GameMode.Livre) return "";
        int r = PlayerPrefs.GetInt(GameSettings.RecordKey(m, d), 0);
        return r > 0 ? "\nRECORDE: " + r + " PONTOS" : "";
    }

    void Play(GameMode m, Dificuldade d)
    {
        GameSettings.Mode = m;
        GameSettings.Dif = d;
        GameSettings.NewRun();
        AudioManager.Sfx("iniciar");
        AudioManager.StopMusic();
        SceneManager.LoadScene(LevelScene);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Build(params Row[] list)
    {
        foreach (Transform c in rowsRoot) Destroy(c.gameObject);
        rows.Clear();
        rows.AddRange(list);
        sel = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            int idx = i;
            var row = rows[i];
            float step = rows.Count > 4 ? 66f : 80f; // 5 linhas: mais juntas para nao cobrir o texto de ajuda
            var r = UIKit.Rect(rowsRoot, "Row" + i, new Vector2(0.5f, 1f), new Vector2(0, -36 - i * step), new Vector2(800, step - 8f));
            var img = r.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.35f);
            var hover = r.gameObject.AddComponent<RowHover>();
            hover.onEnter = () => { if (RowHover.MouseMoved() && sel != idx) { sel = idx; AudioManager.Sfx("navegar"); Refresh(); } };
            hover.onClick = () => { sel = idx; Confirm(); };
            row.label = UIKit.Label(r, "", 24, Cream, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 60));
        }
        Refresh();
    }

    void Refresh()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            bool on = i == sel;
            rows[i].label.text = (on ? "> " : "  ") + rows[i].text();
            rows[i].label.color = on ? Orange : Cream;
        }
        if (rows.Count > 0) hint.text = rows[sel].desc ?? DefaultHint;
    }

    void Confirm()
    {
        if (rows.Count == 0) return;
        AudioManager.Sfx("confirmar");
        rows[sel].confirm?.Invoke();
        if (rows.Count > 0 && rows[0].label != null) Refresh();
    }

    void Update()
    {
        if (rows.Count == 0) return;
        if (Inp.Pressed(K.Up)) { sel = (sel + rows.Count - 1) % rows.Count; AudioManager.Sfx("navegar"); Refresh(); }
        else if (Inp.Pressed(K.Down)) { sel = (sel + 1) % rows.Count; AudioManager.Sfx("navegar"); Refresh(); }
        else if (Inp.Pressed(K.Left) && rows[sel].adjust != null) { rows[sel].adjust(-1); Refresh(); }
        else if (Inp.Pressed(K.Right) && rows[sel].adjust != null) { rows[sel].adjust(1); Refresh(); }
        else if (Inp.Pressed(K.Confirm)) Confirm();
        else if (Inp.Pressed(K.Pause) && onBack != null) { AudioManager.Sfx("voltar"); onBack(); }
    }
}

public class RowHover : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public Action onEnter, onClick;
    static Vector2 lastMouse = new Vector2(-1, -1);

    // So deixa o mouse mudar a selecao se ele realmente se mexeu (cursor parado nao rouba a selecao do teclado).
    public static bool MouseMoved()
    {
#if ENABLE_INPUT_SYSTEM
        var m = UnityEngine.InputSystem.Mouse.current;
        if (m == null) return false;
        Vector2 p = m.position.ReadValue();
#else
        Vector2 p = Input.mousePosition;
#endif
        bool moved = lastMouse.x >= 0 && (p - lastMouse).sqrMagnitude > 4f;
        lastMouse = p;
        return moved;
    }

    public void OnPointerEnter(PointerEventData e) { onEnter?.Invoke(); }
    public void OnPointerClick(PointerEventData e) { onClick?.Invoke(); }
}
