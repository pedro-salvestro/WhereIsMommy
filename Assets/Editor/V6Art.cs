#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// Gera os cenarios das fases 4 a 6 a partir das imagens do Luiz em src/assets/cenarios:
// apaga o HUD desenhado na imagem (moedas, cronometro, minimapa), dobra o tamanho (pixel art, sem borrar)
// e salva em Assets/Resources/Art/Cenarios/cenario_<nome>_v6.png. Menu: Mamae -> Gerar Cenarios Novos.
public static class V6Art
{
    struct Op
    {
        public string key; public bool mirror; public int x0, y0, x1, y1, a, b;
        public Op(string k, bool m, int X0, int Y0, int X1, int Y1, int A, int B) { key = k; mirror = m; x0 = X0; y0 = Y0; x1 = X1; y1 = Y1; a = A; b = B; }
    }

    static readonly string[,] Files =
    {
        { "aquatico", "Gemini_Generated_Image_9qvzo89qvzo89qvz.jpeg" },
        { "diversoes", "Gemini_Generated_Image_9qvzo89qvzo89qvz (1).jpeg" },
        { "gelo", "Gemini_Generated_Image_9qvzo89qvzo89qvz (2).jpeg" },
    };

    // ('s': copia o retangulo deslocado de (a,b); mirror: espelha em torno da coluna a). Coordenadas com y para baixo.
    static readonly Op[] Ops =
    {
            new Op("aquatico", false, 0, 0, 128, 62, 130, 0),
            new Op("aquatico", false, 655, 0, 722, 64, 0, 66),
            new Op("aquatico", false, 614, 0, 655, 63, 187, 26),
            new Op("aquatico", false, 722, 0, 764, 63, 116, 26),
            new Op("aquatico", false, 611, 63, 627, 92, 28, 0),
            new Op("aquatico", false, 627, 63, 642, 92, 13, 0),
            new Op("aquatico", false, 733, 63, 758, 92, -103, 0),
            new Op("aquatico", false, 1208, 0, 1320, 126, -112, 0),
            new Op("aquatico", false, 1320, 0, 1376, 126, 0, 126),
            new Op("diversoes", false, 0, 0, 128, 62, 130, 0),
            new Op("diversoes", false, 655, 0, 722, 62, 0, 290),
            new Op("diversoes", false, 614, 0, 655, 63, 187, 26),
            new Op("diversoes", false, 722, 0, 764, 63, 116, 26),
            new Op("diversoes", false, 611, 63, 627, 92, 28, 0),
            new Op("diversoes", false, 627, 63, 642, 92, 13, 0),
            new Op("diversoes", false, 733, 63, 758, 92, -103, 0),
            new Op("diversoes", true, 1208, 0, 1282, 126, 1151, 0),
            new Op("diversoes", false, 1282, 0, 1376, 126, 0, 126),
            new Op("gelo", false, 0, 0, 128, 62, 140, 0),
            new Op("gelo", false, 614, 0, 764, 64, 200, 0),
            new Op("gelo", true, 1208, 0, 1330, 126, 693, 0),
            new Op("gelo", false, 1330, 0, 1376, 126, 0, 126),
    };

    [MenuItem("Mamae/Gerar Cenarios Novos")]
    public static void Build()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "src", "assets", "cenarios"));
        for (int f = 0; f < Files.GetLength(0); f++)
        {
            string key = Files[f, 0];
            string src = Path.Combine(root, Files[f, 1]);
            if (!File.Exists(src)) { Debug.LogError("V6Art: nao achei " + src); continue; }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(src));
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32();
            foreach (var op in Ops)
            {
                if (op.key != key) continue;
                var copy = (Color32[])px.Clone();
                for (int y = op.y0; y < op.y1; y++)
                    for (int x = op.x0; x < op.x1; x++)
                    {
                        int sx = op.mirror ? 2 * op.a - x : x + op.a;
                        int sy = op.mirror ? y : y + op.b;
                        px[(h - 1 - y) * w + x] = copy[(h - 1 - sy) * w + sx];
                    }
            }
            var big = new Texture2D(w * 2, h * 2, TextureFormat.RGBA32, false);
            var bp = new Color32[w * h * 4];
            for (int y = 0; y < h * 2; y++)
                for (int x = 0; x < w * 2; x++)
                    bp[y * w * 2 + x] = px[(y / 2) * w + x / 2];
            big.SetPixels32(bp);
            big.Apply();
            string dst = "Assets/Resources/Art/Cenarios/cenario_" + key + "_v6.png";
            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", dst), big.EncodeToPNG());
            // o cenario antigo desenhado em codigo (v5) nao e mais usado
            AssetDatabase.DeleteAsset("Assets/Resources/Art/Cenarios/cenario_" + key + ".jpg");
            Debug.Log("V6Art: gerado " + dst + " (" + w * 2 + "x" + h * 2 + ")");
        }
        AssetDatabase.Refresh();
    }
}
#endif
