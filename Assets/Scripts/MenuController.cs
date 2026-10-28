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

    class Row
    {
        public Func<string> text;
        public Action confirm;
        public Action<int> adjust;
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

        title = UIKit.Label(canvas.transform, "ONDE ESTA\nA MAMAE?", 54, Orange, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(1100, 200));
        hint = UIKit.Label(canvas.transform, "SETAS / WASD: NAVEGAR   ENTER: ESCOLHER   ESC: VOLTAR", 12, Cream,
            new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(1200, 40));
        rowsRoot = UIKit.Rect(canvas.transform, "Rows", new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(900, 400));

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
            new Row { text = () => "HISTORIA", confirm = () => Play(GameMode.Historia) },
            new Row { text = () => "CONTRA O TEMPO", confirm = () => Play(GameMode.ContraOTempo) },
            new Row { text = () => "LIVRE", confirm = () => Play(GameMode.Livre) },
            new Row { text = () => "VOLTAR", confirm = ShowMain });
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

    void Play(GameMode m)
    {
        GameSettings.Mode = m;
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
            var r = UIKit.Rect(rowsRoot, "Row" + i, new Vector2(0.5f, 1f), new Vector2(0, -40 - i * 80), new Vector2(800, 64));
            var img = r.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.35f);
            var hover = r.gameObject.AddComponent<RowHover>();
            hover.onEnter = () => { if (sel != idx) { sel = idx; AudioManager.Sfx("navegar"); Refresh(); } };
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
    public void OnPointerEnter(PointerEventData e) { onEnter?.Invoke(); }
    public void OnPointerClick(PointerEventData e) { onClick?.Invoke(); }
}
