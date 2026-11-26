using UnityEngine;

// Dados das 6 fases, ja em coordenadas do mundo (unidades da Unity, centro do mapa em 0,0).
// Cada fase e um mapa unico, cada vez maior. Paredes sao retangulos (x0, y0, x1, y1).
// PlaceActs: o que a mamae estaria fazendo perto de cada lugar (usado nas dicas).
// Este arquivo e gerado pelo script gen5.py.
public class LevelDef
{
    public string Name, Title, Story, Outfit;
    public string[] Parts, PartNames;     // fundo (Resources/Art/Cenarios)
    public float BgScale = 1f;            // os mapas novos foram desenhados com 50 px por unidade
    public float WorldW, WorldH;
    public float[,] Walls;
    public float[,] Occluders;            // pedacos do desenho na frente das pessoas: x0,y0,x1,y1,base (px do desenho de 2752)
    public float[,] Ice;                  // retangulos de gelo (a crianca escorrega)
    public Vector2[] SkateLoop;           // volta que os patinadores fazem na pista
    public Vector2[] ChildSpawns, MomSpots, PlacePos;
    public Vector2 ShopPos;               // lojinha: lugar fixo e aberto, que nao fecha nenhuma passagem
    public string[] PlaceNames, PlaceActs;
    public Color Tint;
    public bool Indoor, Outdoor;          // Indoor: pode faltar luz; Outdoor: pode chover
    public float MomShirt, MomSkirt;      // matiz em graus; -1 = cor original do desenho
    public const float PartH = 15.36f;
}

public static class Levels
{
    public const int Count = 6;

    public static LevelDef Get(int i)
    {
        switch (Mathf.Clamp(i, 0, Count - 1))
        {
            case 1: return L2();
            case 2: return L3();
            case 3: return L4();
            case 4: return L5();
            case 5: return L6();
        }
        return L1();
    }

    static LevelDef L1() => new LevelDef
    {
        Name = "SHOPPING", Title = "FASE 1 DE 6: O SHOPPING",
        Parts = new[] { "Art/Cenarios/cenario_principal_v1_limpo" },
        PartNames = new[] { "SHOPPING" }, BgScale = 1f,
        Story = "A CRIANCA SE DISTRAIU NUMA VITRINE\nE A MAMAE SUMIU NO MEIO DO SHOPPING!",
        Outfit = "BLUSA VINHO E SAIA AZUL", MomShirt = -1f, MomSkirt = -1f,
        Tint = new Color(0.8f, 0.8f, 0.8f), Indoor = true, Outdoor = false,
        WorldW = 27.52f, WorldH = 15.36f,
        Walls = new float[,]
        {
            {-13.76f,6.8f,13.76f,7.68f}, {-13.76f,3.21f,-12.84f,7.68f}, {-12.84f,5.78f,-7.32f,6.8f}, {-12.38f,4.5f,-10.56f,6.38f},
            {-12.84f,5.04f,-12.38f,6.18f}, {-10.54f,5.21f,-10.08f,6.8f}, {-10.04f,5.04f,-9.66f,6.18f}, {-9.61f,5.02f,-9.2f,5.82f},
            {-9.12f,5.12f,-7.82f,5.68f}, {-7.81f,5.02f,-7.34f,6.18f}, {-12.84f,3.38f,-12.38f,4.08f}, {-13.76f,0.86f,-9.62f,3.38f},
            {-10.47f,0.5f,-9.96f,1.37f}, {-9.62f,0.82f,-8.49f,1.5f}, {-7.72f,0.88f,-7.36f,1.5f}, {-7.32f,4.5f,-6.9f,7.01f},
            {-6.9f,5.68f,-4.56f,6.8f}, {-6.86f,4.67f,-5.06f,6.18f}, {-7.32f,0.86f,-4.56f,3.58f}, {-6.8f,0.05f,-6.59f,0.33f},
            {-4.96f,5.5f,-2.73f,6.8f}, {-4.57f,4.54f,-2.78f,5.28f}, {-3.22f,3.62f,-2.78f,4.28f}, {-4.96f,2.68f,-2.08f,3.62f},
            {-2.73f,2.68f,-2.08f,7.68f}, {-2.08f,5.37f,2.06f,6.8f}, {-2.04f,4.54f,-1.43f,5.54f}, {-1.36f,4.56f,-0.95f,5.19f},
            {0.94f,4.56f,1.32f,5.19f}, {1.39f,4.54f,2.02f,5.54f}, {-2.01f,2.71f,-1.47f,4.19f}, {1.45f,2.71f,2.02f,4.19f},
            {-0.51f,2.31f,0.06f,3.54f}, {-2.04f,0.92f,-1.38f,2.18f}, {1.39f,0.92f,2.02f,2.18f}, {-4.96f,0.88f,-2.08f,2.71f},
            {2.06f,2.68f,7.79f,7.68f}, {2.06f,0.88f,7.79f,2.71f}, {7.79f,5.26f,13.07f,7.68f}, {7.83f,4.91f,8.31f,5.95f},
            {8.31f,5.08f,8.62f,5.52f}, {8.78f,4.74f,10.47f,5.43f}, {10.56f,4.56f,10.95f,5.17f}, {10.99f,4.78f,12.81f,5.43f},
            {12.42f,4.13f,12.81f,4.65f}, {7.87f,3.61f,9.61f,4.48f}, {13.07f,-7.68f,13.76f,7.68f}, {10.12f,2.7f,13.07f,3.4f},
            {10.12f,0.88f,13.76f,2.7f}, {6.6f,-0.74f,6.81f,-0.44f}, {7.83f,0.8f,8.96f,1.54f}, {10.12f,0.5f,10.51f,1.06f},
            {-12.17f,-1.39f,-11.5f,-0.77f}, {-13.76f,-2.48f,-9.41f,-1.22f}, {-12.86f,-3.36f,-9.66f,-2.48f}, {-9.66f,-3.48f,-9.41f,-1.22f},
            {-13.76f,-7.68f,-12.84f,-2.48f}, {-12.84f,-4.65f,-12.42f,-3.36f}, {-12.86f,-5.07f,-11f,-4.22f}, {-12.84f,-6.45f,-12.38f,-5.7f},
            {-10.75f,-6.37f,-10f,-5.66f}, {-8.23f,-6.45f,-7.56f,-5.82f}, {-8.64f,-4.11f,-7.99f,-3.65f}, {-8.03f,-2.48f,-6.86f,-1.22f},
            {-6.61f,-1.85f,-6.19f,-1.18f}, {-5.9f,-1.85f,-4.64f,-1.39f}, {-6.86f,-2.48f,-4.56f,-1.87f}, {-8.03f,-4.15f,-7.32f,-2.48f},
            {-7.32f,-3.32f,-4.56f,-2.48f}, {-7.28f,-4.15f,-6.48f,-3.27f}, {-7.32f,-5.07f,-5.48f,-4.4f}, {-5.48f,-5.07f,-5.06f,-3.9f},
            {-5.02f,-4.15f,-4.56f,-3.27f}, {-13.76f,-7.68f,-4.96f,-6.45f}, {-3.65f,-1f,-2.08f,-0.22f}, {-0.51f,-0.85f,0.45f,0.43f},
            {2.09f,-1f,3.64f,-0.22f}, {-0.9f,-2.47f,-0.07f,-1.9f}, {-4.96f,-2.34f,-1.6f,-1.68f}, {-2.73f,-6.96f,-1.86f,-1.68f},
            {-4.96f,-3.25f,-2.73f,-2.34f}, {-4.96f,-4.17f,-4.18f,-3.25f}, {-3.65f,-4.17f,-2.78f,-3.25f}, {-4.96f,-5.04f,-2.78f,-4.39f},
            {-3.22f,-6.27f,-2.78f,-5.04f}, {-4.96f,-7.68f,-1.86f,-6.44f}, {-1.8f,-3.69f,-1.12f,-2.47f}, {-1.73f,-5.52f,-1.21f,-4.21f},
            {-1.8f,-6.88f,-1.12f,-5.65f}, {1.13f,-3.69f,1.8f,-2.47f}, {1.19f,-5.52f,1.74f,-4.21f}, {1.25f,-6.88f,1.74f,-6.22f},
            {1.8f,-2.34f,4.64f,-1.68f}, {1.8f,-6.96f,2.76f,-1.68f}, {2.76f,-3.3f,4.64f,-2.34f}, {2.76f,-4.22f,4.12f,-3.25f},
            {2.76f,-5.04f,4.64f,-4.39f}, {4.16f,-4.61f,4.64f,-3.78f}, {4.16f,-6.01f,4.51f,-5.43f}, {2.76f,-6.48f,3.2f,-5.7f},
            {-1.99f,-7.68f,-0.47f,-6.96f}, {0.49f,-7.68f,2.02f,-6.96f}, {-0.47f,-7.68f,0.49f,-7.37f}, {2.02f,-7.68f,4.64f,-6.44f},
            {4.63f,-1.84f,5.88f,-1.41f}, {6.16f,-1.84f,6.62f,-1.19f}, {4.24f,-2.58f,7.79f,-1.84f}, {6.66f,-1.84f,7.79f,-1.28f},
            {4.24f,-3.36f,7.62f,-2.58f}, {7.62f,-4.18f,7.79f,-2.58f}, {4.24f,-5.04f,5.06f,-3.36f}, {5.54f,-4.39f,6.84f,-3.7f},
            {6.92f,-4.44f,7.31f,-3.87f}, {4.24f,-6f,5.45f,-5.39f}, {7.87f,-4.26f,8.52f,-3.61f}, {9.22f,-4.26f,10.04f,-3.79f},
            {8.32f,-6.47f,9f,-5.82f}, {10.57f,-6.45f,10.97f,-5.69f}, {11.03f,-6.47f,12.33f,-5.95f}, {12.37f,-6.47f,12.81f,-5.69f},
            {12.37f,-5.54f,12.81f,-4.52f}, {12.42f,-4.44f,12.81f,-3.36f}, {9.86f,-2.58f,13.07f,-1.23f}, {10.12f,-4.18f,12.85f,-2.58f},
            {9.86f,-4.22f,10.26f,-1.23f}, {4.24f,-7.68f,13.76f,-6.47f}
        },
        ChildSpawns = new[] { new Vector2(-8.944f, -3.328f), new Vector2(-9.632f, -0.301f), new Vector2(-1.376f, -0.576f), new Vector2(8.944f, -0.301f) },
        MomSpots = new[] { new Vector2(11.365f, 4.305f), new Vector2(-11.385f, 4.055f), new Vector2(0.115f, 4.305f), new Vector2(-11.885f, -5.445f), new Vector2(-5.135f, -5.945f), new Vector2(5.615f, -6.195f), new Vector2(11.615f, -5.445f), new Vector2(8.865f, 2.055f), new Vector2(12.365f, -0.195f), new Vector2(-8.635f, -6.195f), new Vector2(-10.135f, 3.805f), new Vector2(-5.135f, 3.805f), new Vector2(8.115f, 3.305f), new Vector2(10.365f, 3.805f), new Vector2(-11.635f, 0.555f), new Vector2(-4.635f, 0.555f), new Vector2(4.865f, 0.555f), new Vector2(11.615f, 0.555f), new Vector2(-2.885f, 0.055f), new Vector2(2.865f, 0.055f), new Vector2(0.115f, -1.195f), new Vector2(0.115f, -5.195f) },
        ShopPos = new Vector2(9.84f, -6.42f),
        PlaceNames = new[] { "LOJA DE DOCES", "PADARIA", "ENTRADA", "LIVRARIA", "LANCHONETE", "LOJA DE ROUPAS", "LOJA DE PRESENTES", "CAFE", "LOJA DE ARTE", "QUIOSQUE DE SUCOS", "PIPOCA", "PRACA CENTRAL", "FARMACIA", "RESTAURANTE", "CORREDOR SUL", "SORVETERIA", "BANCO" },
        PlaceActs = new[] { "COMPRANDO DOCES", "COMPRANDO PAO", "OLHANDO A PORTA DE VIDRO DA ENTRADA", "OLHANDO OS LIVROS DA VITRINE", "COMENDO UM SANDUICHE", "EXPERIMENTANDO ROUPAS", "ESCOLHENDO UM PRESENTE", "TOMANDO CAFE", "OLHANDO QUADROS", "TOMANDO UM SUCO DE LARANJA", "COMPRANDO PIPOCA", "SENTADA PERTO DAS PLANTAS DA PRACA", "COMPRANDO REMEDIO", "ALMOCANDO", "SENTADA NUM BANCO DO CORREDOR", "TOMANDO SORVETE", "SACANDO DINHEIRO" },
        PlacePos = new[] { new Vector2(-10.182f, 4.24f), new Vector2(-4.816f, 4.24f), new Vector2(0f, 5.272f), new Vector2(5.229f, 4.24f), new Vector2(10.32f, 3.965f), new Vector2(-11.696f, 1.626f), new Vector2(-4.678f, 1.626f), new Vector2(4.816f, 1.626f), new Vector2(11.696f, 1.626f), new Vector2(-2.89f, 0.594f), new Vector2(2.89f, 0.594f), new Vector2(0f, -0.438f), new Vector2(-11.283f, -4.291f), new Vector2(-5.229f, -5.392f), new Vector2(0f, -4.704f), new Vector2(5.229f, -5.392f), new Vector2(11.558f, -3.053f) },
    };

    static LevelDef L2() => new LevelDef
    {
        Name = "MERCADO", Title = "FASE 2 DE 6: O MERCADO",
        Parts = new[] { "Art/Cenarios/cenario_mercado_v2_limpo" },
        PartNames = new[] { "MERCADO" }, BgScale = 1f,
        Story = "NO MERCADO TEM MUITA GENTE COM CARRINHO.\nACHE A MAMAE ANTES QUE ELA VA PRO CAIXA!",
        Outfit = "BLUSA VERDE E SAIA MARROM", MomShirt = 125f, MomSkirt = 30f,
        Tint = new Color(0.8f, 0.8f, 0.8f), Indoor = true, Outdoor = false,
        WorldW = 27.52f, WorldH = 15.36f,
        Walls = new float[,]
        {
            {-13.76f,6.8f,13.76f,7.68f}, {-13.76f,3.21f,-12.84f,7.68f}, {-12.84f,5.78f,-7.32f,6.8f}, {-12.38f,4.5f,-10.56f,6.38f},
            {-12.84f,5.04f,-12.38f,6.18f}, {-10.54f,5.21f,-10.08f,6.8f}, {-10.04f,5.04f,-9.66f,6.18f}, {-9.61f,5.02f,-9.2f,5.82f},
            {-9.12f,5.12f,-7.82f,5.68f}, {-7.81f,5.02f,-7.34f,6.18f}, {-12.84f,3.38f,-12.38f,4.08f}, {-12.3f,3.41f,-11.04f,3.88f},
            {-13.76f,0.86f,-9.62f,3.38f}, {-7.32f,4.54f,-6.9f,7.01f}, {-6.9f,5.68f,-4.56f,7.01f}, {-6.82f,5.02f,-5.61f,5.68f},
            {-5.56f,5.04f,-4.77f,5.42f}, {-7.32f,-0.12f,-6.89f,3.58f}, {-6.84f,-0.89f,-6.44f,3.13f}, {-5.44f,-1.06f,-4.6f,3.3f},
            {-9.58f,0.86f,-8.49f,1.5f}, {-7.72f,0.94f,-7.36f,1.5f}, {-10.47f,0.5f,-9.96f,1.37f}, {-4.79f,4.63f,-4.18f,5.68f},
            {-4.13f,5.15f,0.45f,6.8f}, {-4.13f,4.1f,-3.65f,6.8f}, {-3.65f,4.1f,-2.3f,5.15f}, {-2.3f,4.1f,-0.47f,5.11f},
            {-0.47f,4.1f,0.45f,4.45f}, {0f,4.58f,0.43f,5.85f}, {0.45f,4.1f,0.9f,6.46f}, {0.9f,5.15f,4.64f,6.8f},
            {0.93f,4.1f,2.72f,5.11f}, {2.76f,4.1f,3.64f,5.11f}, {3.68f,4.1f,4.64f,4.8f}, {-3.65f,-1.07f,-2.78f,3.32f},
            {-1.82f,-1.07f,-0.47f,3.43f}, {0.49f,-1.07f,1.8f,3.43f}, {2.76f,-1.07f,3.64f,3.32f}, {4.64f,5.15f,6.62f,6.8f},
            {4.64f,4.09f,6.4f,4.82f}, {5.97f,3.66f,6.4f,4.39f}, {6.62f,3.96f,7.79f,7.68f}, {6.45f,-0.12f,6.88f,3.96f},
            {4.63f,-1.07f,5.45f,3.05f}, {7.79f,5.56f,13.04f,7.68f}, {8.52f,4.78f,12.12f,5.56f}, {7.83f,4.91f,8.31f,5.95f},
            {12.33f,4.91f,12.81f,5.6f}, {7.79f,1.8f,8.35f,4.91f}, {12.29f,1.93f,12.85f,4.91f}, {12.85f,-7.68f,13.76f,7.68f},
            {9.22f,2.68f,11.47f,4f}, {7.83f,0.8f,8.96f,1.49f}, {10.11f,0.76f,12.85f,2.14f}, {-13.76f,-0.26f,-11.04f,0.68f},
            {-13.68f,-1.39f,-10.58f,-0.81f}, {-7.95f,-1.39f,-7.4f,-0.1f}, {-13.76f,-2.48f,-9.41f,-1.22f}, {-12.86f,-3.36f,-9.66f,-2.48f},
            {-12.86f,-4.65f,-12.42f,-3.36f}, {-12.86f,-5.07f,-11f,-4.22f}, {-13.76f,-7.68f,-12.84f,-2.48f}, {-12.38f,-6.45f,-11f,-5.87f},
            {-9.66f,-4.4f,-9.41f,-1.22f}, {-13.76f,-7.68f,-4.96f,-6.45f}, {-8.03f,-4.36f,-7.32f,-1.22f}, {-8.64f,-4.11f,-7.99f,-3.65f},
            {-7.76f,-2.48f,-4.96f,-1.72f}, {-7.32f,-3.32f,-4.96f,-2.48f}, {-7.32f,-4.15f,-6.48f,-3.27f}, {-7.32f,-5.07f,-5.48f,-4.4f},
            {-5.48f,-5.07f,-5.06f,-3.9f}, {-5.02f,-4.15f,-4.56f,-3.27f}, {-1.56f,-1.42f,-0.9f,-0.81f}, {0.89f,-1.42f,1.54f,-0.81f},
            {-4.96f,-2.34f,-1.6f,-1.72f}, {-4.96f,-3.25f,-2.73f,-2.34f}, {-2.73f,-4.65f,-1.6f,-1.99f}, {-4.96f,-4.13f,-4.18f,-3.25f},
            {-3.65f,-4.13f,-2.78f,-3.25f}, {-4.96f,-5.04f,-2.78f,-4.43f}, {-3.22f,-6.22f,-2.78f,-5.04f}, {-1.78f,-3.87f,-0.99f,-2.29f},
            {-1.73f,-4.61f,-1.23f,-4.13f}, {0.93f,-3.73f,1.67f,-2.42f}, {1.25f,-4.56f,1.74f,-3.49f}, {1.63f,-2.6f,4.64f,-1.68f},
            {1.63f,-4.87f,2.72f,-1.68f}, {2.72f,-4.17f,4.64f,-2.6f}, {3.68f,-6.26f,4.6f,-4.95f}, {-2.73f,-7.68f,-1.86f,-5.57f},
            {-1.82f,-6.88f,-1.12f,-5.65f}, {-4.96f,-7.68f,-2.73f,-6.48f}, {-1.86f,-7.68f,-0.6f,-6.96f}, {0.49f,-7.68f,1.85f,-6.96f},
            {-0.6f,-7.68f,0.49f,-7.37f}, {1.25f,-6.88f,1.74f,-6.22f}, {1.85f,-7.68f,2.72f,-5.57f}, {2.72f,-7.68f,4.64f,-6.48f},
            {6.88f,0.28f,7.79f,3.96f}, {6.88f,-2.32f,7.79f,0.28f}, {7.87f,-1.41f,8.39f,-0.07f}, {6.4f,-1.02f,6.88f,0.28f},
            {6.16f,-1.84f,6.62f,-1.19f}, {4.24f,-2.62f,7.79f,-1.84f}, {4.24f,-4.18f,7.79f,-2.58f}, {7.66f,-4.2f,8.02f,-2.32f},
            {10.12f,-0.55f,11.42f,0.28f}, {12.37f,-1.37f,12.81f,0.28f}, {9.86f,-2.58f,13.04f,-1.23f}, {10.12f,-4.18f,12.89f,-2.58f},
            {9.86f,-4.22f,10.26f,-1.23f}, {9.22f,-4.26f,10.04f,-3.79f}, {12.37f,-4.61f,12.81f,-4f}, {5.52f,-6.47f,6.39f,-4.9f},
            {7.35f,-6.47f,8.22f,-4.9f}, {9.19f,-6.47f,10.04f,-4.9f}, {11.01f,-6.47f,11.9f,-4.9f}, {4.24f,-7.68f,13.76f,-6.48f}
        },
        ChildSpawns = new[] { new Vector2(0.04f, -6.32f), new Vector2(-8.56f, 1.68f), new Vector2(6.24f, 3.68f) },
        MomSpots = new[] { new Vector2(-1.385f, 3.805f), new Vector2(9.365f, 1.055f), new Vector2(11.615f, -0.695f), new Vector2(8.615f, -5.445f), new Vector2(12.365f, -5.445f), new Vector2(-5.135f, -5.695f), new Vector2(-9.135f, -5.445f), new Vector2(-10.885f, -0.445f), new Vector2(-9.885f, 4.055f), new Vector2(-4.135f, 1.055f), new Vector2(4.115f, 1.055f), new Vector2(-0.135f, -4.695f), new Vector2(-8.635f, -0.695f), new Vector2(2.865f, 3.805f), new Vector2(9.865f, 2.305f), new Vector2(-0.135f, 0.055f), new Vector2(-11.385f, -3.945f), new Vector2(5.115f, -4.445f), new Vector2(11.365f, -4.445f), new Vector2(7.115f, -6.195f), new Vector2(0.115f, -6.945f) },
        ShopPos = new Vector2(-4.26f, 4.03f),
        PlaceNames = new[] { "LOJA DE DOCES", "PADARIA", "FRIOS", "HORTIFRUTI", "CORREDORES", "LOJA DE ROUPAS", "MESINHAS", "FARMACIA", "RESTAURANTE", "ESTOQUE", "LANCHONETE", "BANCO", "CAIXAS", "SAIDA" },
        PlaceActs = new[] { "ESCOLHENDO BALAS", "ESCOLHENDO PAES", "PEDINDO QUEIJO E PRESUNTO", "ESCOLHENDO FRUTAS", "PROCURANDO ALGO NAS PRATELEIRAS", "OLHANDO A VITRINE DE ROUPAS", "SENTADA NUMA MESINHA", "COMPRANDO REMEDIO", "ALMOCANDO", "PERTO DAS CAIXAS DE PAPELAO", "TOMANDO UM CAFEZINHO", "NA FILA DO BANCO", "PAGANDO AS COMPRAS", "OLHANDO A PLACA DE SAIDA" },
        PlacePos = new[] { new Vector2(-10.16f, 4.88f), new Vector2(-1.76f, 4.68f), new Vector2(2.74f, 4.68f), new Vector2(10.24f, 3.08f), new Vector2(-0.26f, 0.68f), new Vector2(-11.76f, 2.08f), new Vector2(-12.16f, -1.12f), new Vector2(-11.26f, -3.82f), new Vector2(-4.96f, -4.22f), new Vector2(-1.36f, -3.12f), new Vector2(5.04f, -3.32f), new Vector2(11.44f, -3.52f), new Vector2(7.74f, -5.62f), new Vector2(0.04f, -6.72f) },
    };

    static LevelDef L3() => new LevelDef
    {
        Name = "PARQUE", Title = "FASE 3 DE 6: O PARQUE",
        Parts = new[] { "Art/Cenarios/cenario_parque_v1_limpo" },
        PartNames = new[] { "PARQUE" }, BgScale = 1f,
        Story = "DOMINGO NO PARQUE! A MAMAE FOI COMPRAR\nPIPOCA E SE PERDEU NO MEIO DO POVO.",
        Outfit = "BLUSA AMARELA E SAIA AZUL", MomShirt = 50f, MomSkirt = 225f,
        Tint = new Color(0.82f, 0.82f, 0.82f), Indoor = false, Outdoor = true,
        WorldW = 27.52f, WorldH = 15.36f,
        Walls = new float[,]
        {
            {-13.76f,6.88f,13.76f,7.68f}, {-13.76f,-7.68f,-13.32f,7.68f}, {13.32f,-7.68f,13.76f,7.68f}, {-13.76f,-7.68f,13.76f,-6.56f},
            {-13.76f,4.28f,-12.56f,6.88f}, {-13.76f,-1.72f,-12.86f,4.28f}, {-13.76f,-6.56f,-12.96f,-1.72f}, {12.84f,3.88f,13.76f,6.88f},
            {12.94f,1.28f,13.76f,3.88f}, {12.84f,-6.56f,13.76f,-1.12f}, {-13.76f,4.92f,-11.86f,5.48f}, {-11f,4.98f,-9.72f,6.44f},
            {-9.2f,5.56f,-7.8f,6.32f}, {-6.4f,5.56f,-5.04f,6.32f}, {-9.66f,5.58f,-9.16f,6.24f}, {-6.92f,5.58f,-6.4f,6.24f},
            {-7.26f,5.58f,-6.96f,6.88f}, {-4.16f,4.52f,-3.76f,6.88f}, {3.74f,4.52f,4.08f,6.88f}, {-2.66f,4.98f,-1.36f,6.24f},
            {1.34f,4.98f,2.68f,6.24f}, {5.04f,4.78f,6.74f,6.44f}, {7.54f,4.88f,8.74f,6.44f}, {10.04f,4.88f,11.54f,6.52f},
            {8.64f,4.98f,9.14f,5.88f}, {-12.96f,3.24f,-6.6f,3.78f}, {-6.92f,-0.68f,-6.52f,3.78f}, {-12.52f,1.24f,-9.96f,3.58f},
            {-9.2f,1.72f,-7.72f,3.18f}, {-12.4f,-0.76f,-10.6f,1.24f}, {-9.44f,-0.72f,-7.76f,0.78f}, {-4.96f,0.88f,-1.04f,1.44f},
            {2.24f,1.32f,5.04f,2.84f}, {0.96f,0.88f,7.68f,1.44f}, {5f,0.92f,6.48f,2.44f}, {4.48f,2.68f,6.28f,3.32f},
            {6.8f,1.84f,7.8f,2.88f}, {6.54f,0.98f,6.84f,3.24f}, {9.24f,0.88f,13.76f,1.44f}, {10.44f,3.58f,11.48f,4.38f},
            {10.44f,2.24f,11.48f,2.98f}, {11.44f,3.18f,12f,3.78f}, {10.14f,0.48f,10.54f,1.08f}, {2f,-1.16f,3.68f,0.52f},
            {-0.46f,-0.82f,0.04f,-0.22f}, {-0.08f,-0.72f,0.54f,0.38f}, {1.34f,0.48f,1.84f,1.18f}, {6.48f,-0.72f,6.84f,-0.02f},
            {-12.96f,-2.4f,-9.32f,-1.88f}, {-7.32f,-2.4f,-1.04f,-1.88f}, {1.04f,-2.4f,7.68f,-1.88f}, {-11.88f,-1.88f,-10.56f,-1.44f},
            {-5.92f,-1.88f,-4.6f,-1.44f}, {4.6f,-1.88f,5.96f,-1.44f}, {6.16f,-1.76f,6.6f,-1.24f}, {-12.96f,-3.22f,-9.6f,-2.42f},
            {-12.88f,-6.42f,-9.66f,-2.92f}, {-9.66f,-6.52f,-9.32f,-3.82f}, {-3.66f,-3.02f,-2.8f,-1.92f}, {-6.86f,-3.52f,-2.16f,-2.32f},
            {-6.4f,-4.56f,-5.6f,-3.12f}, {-5.04f,-4.64f,-3.2f,-3.32f}, {-7.4f,-4.62f,-6.86f,-4.02f}, {-2.2f,-4.94f,-1.92f,-2.32f},
            {-1.8f,-3.52f,-1.12f,-2.32f}, {-1.8f,-5.22f,-1.16f,-4.22f}, {-1.8f,-6.88f,-1.12f,-6.24f}, {1.2f,-3.62f,1.8f,-2.32f},
            {1.2f,-5.56f,1.76f,-4.16f}, {1.2f,-6.88f,1.8f,-6.12f}, {2.04f,-5.12f,2.84f,-2.12f}, {2.24f,-2.82f,7.4f,-2.12f},
            {7.48f,-5.12f,8.08f,-1.88f}, {3.2f,-4.52f,4.2f,-3.22f}, {5.84f,-4.52f,6.96f,-3.22f}, {4.14f,-6.08f,5.54f,-5.22f},
            {2.64f,-3.68f,3.14f,-3.12f}, {5.44f,-4.32f,5.94f,-3.52f}, {6.44f,-3.22f,6.94f,-2.72f}, {10f,-6.76f,13f,-2.42f},
            {9.2f,-4.28f,10.08f,-3.72f}, {9.74f,-6.32f,10.04f,-5.52f}, {9.8f,-2.42f,13.76f,-1.12f}, {10.04f,-1.32f,10.84f,-0.62f},
            {-3.72f,-1f,-2f,-0.22f}, {-0.76f,2.56f,0.76f,4.78f}, {-1.1f,2.98f,-0.76f,4.38f}, {0.76f,2.98f,1.12f,4.38f},
            {-5.12f,1.38f,-2.41f,2.84f}, {-2.2f,-6.56f,-1.92f,-5.84f}, {-13.76f,6.63f,-0.66f,7.68f}, {0.64f,6.63f,13.76f,7.68f}
        },
        ChildSpawns = new[] { new Vector2(0.04f, -6.32f), new Vector2(0.04f, -1.32f), new Vector2(-8.56f, -3.02f) },
        MomSpots = new[] { new Vector2(-11.635f, 4.305f), new Vector2(-5.635f, 2.305f), new Vector2(8.365f, 2.805f), new Vector2(12.365f, 2.805f), new Vector2(-5.135f, -5.445f), new Vector2(4.615f, -3.445f), new Vector2(9.365f, -4.695f), new Vector2(5.865f, -0.445f), new Vector2(-2.635f, 3.555f), new Vector2(11.115f, -0.445f), new Vector2(-9.135f, -1.195f), new Vector2(-0.135f, 1.555f), new Vector2(-10.385f, 0.805f), new Vector2(-8.885f, -5.445f), new Vector2(-10.385f, 4.555f), new Vector2(5.865f, 4.555f), new Vector2(8.115f, 4.555f), new Vector2(11.115f, 1.805f), new Vector2(-2.885f, 0.055f), new Vector2(1.615f, 0.055f), new Vector2(-3.885f, 0.555f), new Vector2(-5.385f, -3.945f), new Vector2(5.115f, -4.945f), new Vector2(9.615f, -2.945f) },
        ShopPos = new Vector2(-7.56f, -4.32f),
        PlaceNames = new[] { "FONTE", "PARQUINHO", "CASINHA", "PIPOCA", "LANCHES", "SORVETES", "MESAS", "QUIOSQUE", "BARRACA VERDE", "JARDIM DE FLORES", "CARRINHO DE SORVETE", "PIQUENIQUE", "BANHEIROS" },
        PlaceActs = new[] { "JOGANDO UMA MOEDA NA FONTE", "OLHANDO AS CRIANCAS NO PARQUINHO", "DESCANSANDO PERTO DA CASINHA", "COMPRANDO PIPOCA", "COMENDO UM LANCHE", "TOMANDO SORVETE", "SENTADA NUMA MESA", "COMPRANDO AGUA NO QUIOSQUE", "OLHANDO A BARRACA VERDE", "CHEIRANDO AS FLORES", "PEDINDO UM PICOLE", "FAZENDO PIQUENIQUE", "NA FILA DO BANHEIRO" },
        PlacePos = new[] { new Vector2(-0.01f, 2.08f), new Vector2(-10.76f, 1.48f), new Vector2(-11.26f, -4.82f), new Vector2(-10.31f, 5.68f), new Vector2(5.84f, 5.68f), new Vector2(8.14f, 5.68f), new Vector2(11.04f, 3.08f), new Vector2(-2.86f, 0.68f), new Vector2(2.84f, 0.58f), new Vector2(-3.76f, 2.08f), new Vector2(-5.96f, -3.32f), new Vector2(5.04f, -4.52f), new Vector2(11.54f, -2.32f) },
        Occluders = new float[,] { {0f,0f,1310f,135f,135f}, {1440f,0f,2752f,135f,135f}, {0f,1380f,1310f,1536f,1536f}, {1440f,1380f,2752f,1536f,1536f} },
    };

    static LevelDef L4() => new LevelDef
    {
        Name = "PARQUE AQUATICO", Title = "FASE 4 DE 6: O PARQUE AQUATICO",
        Parts = new[] { "Art/Cenarios/cenario_aquatico_v8" },
        PartNames = new[] { "PARQUE AQUATICO" }, BgScale = 1.0f,
        Story = "CALOR! A FAMILIA FOI AO PARQUE AQUATICO,\nMAS A MAMAE SUMIU ENTRE AS PISCINAS.",
        Outfit = "BLUSA LARANJA E SAIA ROXA", MomShirt = 25f, MomSkirt = 280f,
        Tint = new Color(0.85f, 0.85f, 0.85f), Indoor = false, Outdoor = true,
        WorldW = 33f, WorldH = 18.42f,
        Walls = new float[,]
        {
            {-16.5f,8.05f,16.5f,9.21f}, {-16.5f,-9.21f,-15.7f,9.21f}, {15.7f,-9.21f,16.5f,9.21f}, {-16.5f,-9.21f,-1.14f,-8.41f},
            {1.34f,-9.21f,16.5f,-8.41f}, {-1.84f,-9.21f,-0.8f,-8.41f}, {1f,-9.21f,2.04f,-8.41f}, {-5f,2.41f,-3.76f,3.41f},
            {-13.7f,0.65f,-12.54f,1.57f}, {-11.5f,0.65f,-10.34f,1.57f}, {-9.3f,0.65f,-8.14f,1.57f}, {-2.02f,0.61f,-1.62f,0.83f},
            {7.7f,7.01f,9.78f,7.81f}, {11.5f,6.01f,14.42f,7.01f}, {10.7f,4.05f,11.86f,4.97f}, {12.6f,4.05f,13.76f,4.97f},
            {10.7f,2.15f,11.86f,3.07f}, {12.6f,2.15f,13.76f,3.07f}, {2.36f,6.07f,2.68f,6.37f}, {10.76f,0.17f,11.08f,0.47f},
            {-13.9f,0.69f,-12.38f,1.37f}, {-5.3f,0.65f,-3.74f,1.41f}, {2.3f,0.65f,3.76f,1.41f}, {5.5f,0.65f,6.68f,1.41f},
            {12.5f,-2.43f,13.94f,-1.67f}, {-0.62f,-0.73f,0.82f,0.27f}, {-10.5f,-1.53f,-9.22f,-0.99f}, {-2.7f,-1.53f,-1.42f,-0.99f},
            {3.7f,-1.53f,4.98f,-0.99f}, {8.5f,-1.53f,9.78f,-0.99f}, {-7.22f,0.65f,-6.9f,0.93f}, {11.38f,0.65f,11.7f,0.93f},
            {-7.1f,-6.15f,-3.84f,-1.99f}, {-12.14f,-5.39f,-11.5f,-4.59f}, {-11.3f,-4.81f,-10.28f,-3.19f}, {-2.94f,-7.73f,-2.62f,-7.43f},
            {-14.34f,-7.93f,-14.02f,-7.63f}, {8.7f,-4.83f,9.9f,-4.39f}, {12.66f,-4.59f,13.3f,-3.79f}, {2.3f,-7.75f,3.46f,-6.83f},
            {4.7f,-7.75f,5.86f,-6.83f}, {-2.5f,-3.41f,-1.48f,-1.79f}, {9.5f,-7.75f,10.66f,-6.83f}, {11.7f,-7.75f,12.86f,-6.83f},
            {8.18f,-7.49f,8.58f,-7.27f}, {-15.46f,0.61f,-15.14f,0.93f}, {-15.46f,-1.09f,-15.14f,-0.77f}, {-1.46f,0.81f,-1.14f,1.13f},
            {1.14f,0.81f,1.46f,1.13f}, {-1.46f,-1.09f,-1.14f,-0.77f}, {1.14f,-1.09f,1.46f,-0.77f}, {-5.46f,-6.99f,-5.14f,-6.67f},
            {-2.26f,-3.39f,-1.94f,-3.07f}, {-11.86f,-6.99f,-11.54f,-6.67f}, {14.94f,-1.09f,15.26f,-0.77f}, {14.94f,0.61f,15.26f,0.93f},
            {-13.62f,2.17f,-7.38f,6.65f}, {-13.78f,2.17f,-13.62f,6.49f}, {-7.38f,2.17f,-7.22f,6.49f}, {-13.94f,2.33f,-13.78f,6.33f},
            {-7.22f,2.33f,-7.06f,6.33f}, {7.18f,2.17f,10.06f,6.33f}, {3.02f,2.01f,7.18f,5.85f}, {2.86f,2.17f,3.02f,5.69f},
            {2.7f,2.33f,2.86f,5.53f}, {7.18f,2.01f,9.9f,2.17f}, {-13.62f,-2.47f,-10.9f,-1.51f}, {-14.74f,-2.47f,-13.62f,-1.67f},
            {-10.9f,-2.47f,-10.1f,-1.67f}, {-14.9f,-2.47f,-14.74f,-1.83f}, {-10.1f,-2.63f,-9.46f,-1.83f}, {-9.46f,-2.95f,-9.14f,-1.99f},
            {-9.14f,-3.27f,-8.82f,-2.15f}, {-8.82f,-5.35f,-8.66f,-2.31f}, {-14.58f,-2.63f,-14.42f,-2.47f}, {-10.74f,-2.63f,-10.1f,-2.47f},
            {-8.66f,-5.19f,-8.34f,-2.47f}, {-9.94f,-2.79f,-9.46f,-2.63f}, {-9.62f,-2.95f,-9.46f,-2.79f}, {-8.34f,-4.87f,-8.18f,-2.79f},
            {3.66f,-5.99f,7.34f,-2.79f}, {-9.3f,-3.11f,-9.14f,-2.95f}, {-8.18f,-4.71f,-8.02f,-2.95f}, {3.5f,-5.83f,3.66f,-2.95f},
            {7.34f,-5.83f,7.5f,-2.95f}, {-8.98f,-3.43f,-8.82f,-3.27f}, {-8.02f,-4.39f,-7.86f,-3.27f}, {-8.98f,-5.51f,-8.82f,-4.23f},
            {-9.3f,-5.67f,-8.98f,-4.55f}, {-9.46f,-5.83f,-9.3f,-4.71f}, {-9.78f,-5.83f,-9.46f,-4.87f}, {-10.42f,-5.99f,-9.78f,-5.03f},
            {-14.74f,-5.99f,-12.18f,-5.19f}, {-11.7f,-5.99f,-10.42f,-5.19f}, {-8.66f,-5.35f,-8.5f,-5.19f}, {-14.9f,-5.99f,-14.74f,-5.35f},
            {-12.18f,-6.15f,-11.7f,-5.35f}, {-14.26f,-6.15f,-12.18f,-5.99f}, {-11.7f,-6.15f,-10.58f,-5.99f}
        },
        ChildSpawns = new[] { new Vector2(0.1f, -8.49f), new Vector2(0.1f, -7.99f) },
        MomSpots = new[] { new Vector2(-12.125f, 1.085f), new Vector2(-5.375f, 1.835f), new Vector2(-2.375f, 3.085f), new Vector2(3.375f, 6.835f), new Vector2(3.375f, -1.915f), new Vector2(9.375f, -1.915f), new Vector2(-2.625f, -4.915f), new Vector2(-13.625f, -7.165f), new Vector2(-9.875f, -0.665f), new Vector2(-4.125f, -1.165f), new Vector2(7.625f, -1.165f), new Vector2(2.125f, -4.665f), new Vector2(-7.125f, 0.335f), new Vector2(8.375f, 0.335f), new Vector2(-3.375f, 1.335f), new Vector2(-10.125f, 1.085f), new Vector2(-13.125f, 0.335f), new Vector2(0.125f, -1.665f), new Vector2(-7.625f, -5.165f), new Vector2(-4.375f, 0.335f), new Vector2(-5.625f, -7.165f), new Vector2(6.125f, 0.335f), new Vector2(3.125f, 0.335f), new Vector2(12.125f, 0.835f), new Vector2(6.625f, 6.085f), new Vector2(5.125f, 0.835f), new Vector2(6.125f, -7.165f), new Vector2(8.875f, -5.915f), new Vector2(0.125f, -7.415f) },
        ShopPos = new Vector2(-3.7f, -1.29f),
        PlaceNames = new[] { "TOBOGA AMARELO", "PISCINA FUNDA", "CARRINHO DE LANCHES", "FONTE", "RIO LENTO", "SORVETERIA", "JARDIM DA ILHA", "BANHEIROS", "QUIOSQUE DE SORVETE", "BARRACA DE SUCOS", "VESTIARIOS", "GUARDA-SOIS", "TOBOGAS DUPLOS", "PISCINA DE ONDAS", "PISCINA INFANTIL", "AGUA DE COCO", "PALMEIRAS", "ENTRADA" },
        PlaceActs = new[] { "ESPERANDO NA FILA DO TOBOGA", "OLHANDO AS CRIANCAS NADANDO", "COMPRANDO UM LANCHE", "JOGANDO UMA MOEDA NA FONTE", "BOIANDO NO RIO LENTO", "TOMANDO SORVETE", "OLHANDO AS FLORES DO JARDIM", "NA FILA DO BANHEIRO", "ESCOLHENDO UM PICOLE", "TOMANDO UM SUCO", "TROCANDO DE ROUPA NO VESTIARIO", "TOMANDO SOL NO GUARDA-SOL", "OLHANDO OS TOBOGAS DUPLOS", "PULANDO ONDAS NA PISCINA", "CUIDANDO DOS PEQUENOS NA PISCINA INFANTIL", "TOMANDO AGUA DE COCO", "DESCANSANDO NA SOMBRA DAS PALMEIRAS", "OLHANDO A CATRACA DA ENTRADA" },
        PlacePos = new[] { new Vector2(-3.7f, 1.81f), new Vector2(-10.5f, 1.61f), new Vector2(-13.14f, 1.01f), new Vector2(0.1f, -1.19f), new Vector2(-7.5f, -4.59f), new Vector2(-4.5f, 1.01f), new Vector2(-11.9f, -4.59f), new Vector2(-5.5f, -6.59f), new Vector2(6.1f, 0.91f), new Vector2(3f, 0.91f), new Vector2(12.9f, 5.61f), new Vector2(11.9f, 1.41f), new Vector2(6.5f, 6.61f), new Vector2(5.5f, 1.51f), new Vector2(5.5f, -6.59f), new Vector2(13.2f, -2.79f), new Vector2(8.9f, -5.39f), new Vector2(0.1f, -6.79f) },
        Occluders = new float[,] { {-31.6897f,1434.38f,55.04f,1571.14f,1571.14f}, {35.0255f,1437.71f,121.755f,1574.48f,1574.48f}, {105.076f,1427.7f,191.806f,1564.47f,1564.47f}, {161.784f,1432.71f,248.514f,1569.47f,1569.47f}, {238.507f,1427.7f,325.236f,1564.47f,1564.47f}, {293.547f,1432.71f,380.276f,1569.47f,1569.47f}, {371.937f,1432.71f,458.667f,1569.47f,1569.47f}, {436.984f,1434.38f,523.714f,1571.14f,1571.14f}, {493.692f,1437.71f,580.422f,1574.48f,1574.48f}, {568.747f,1429.37f,655.476f,1566.14f,1566.14f}, {628.79f,1437.71f,715.52f,1574.48f,1574.48f}, {693.838f,1431.04f,780.567f,1567.81f,1567.81f}, {767.224f,1429.37f,853.954f,1566.14f,1566.14f}, {832.272f,1436.04f,919.001f,1572.81f,1572.81f}, {903.99f,1437.71f,990.72f,1574.48f,1574.48f}, {962.366f,1429.37f,1049.1f,1566.14f,1566.14f}, {1039.09f,1436.04f,1125.82f,1572.81f,1572.81f}, {1107.47f,1432.71f,1194.2f,1569.47f,1569.47f}, {1164.18f,1436.04f,1250.91f,1572.81f,1572.81f}, {1507.76f,1432.71f,1594.49f,1569.47f,1569.47f}, {1571.14f,1434.38f,1657.87f,1571.14f,1571.14f}, {1637.86f,1431.04f,1724.59f,1567.81f,1567.81f}, {1697.9f,1427.7f,1784.63f,1564.47f,1564.47f}, {1764.62f,1429.37f,1851.35f,1566.14f,1566.14f}, {1833f,1431.04f,1919.73f,1567.81f,1567.81f}, {1894.71f,1437.71f,1981.44f,1574.48f,1574.48f}, {1964.76f,1432.71f,2051.49f,1569.47f,1569.47f}, {2034.81f,1426.04f,2121.54f,1562.8f,1562.8f}, {2098.19f,1436.04f,2184.92f,1572.81f,1572.81f}, {2174.91f,1434.38f,2261.64f,1571.14f,1571.14f}, {2236.63f,1429.37f,2323.36f,1566.14f,1566.14f}, {2308.34f,1426.04f,2395.07f,1562.8f,1562.8f}, {2373.39f,1439.38f,2460.12f,1576.15f,1576.15f}, {2438.44f,1436.04f,2525.17f,1572.81f,1572.81f}, {2505.15f,1436.04f,2591.88f,1572.81f,1572.81f}, {2563.53f,1437.71f,2650.26f,1574.48f,1574.48f}, {2638.58f,1426.04f,2725.31f,1562.8f,1562.8f}, {2700.3f,1427.7f,2787.03f,1564.47f,1564.47f}, {-40.0291f,55.04f,46.7006f,191.806f,191.806f}, {2698.63f,45.0327f,2785.36f,181.799f,181.799f}, {-38.3612f,120.087f,48.3685f,256.853f,256.853f}, {2696.96f,120.087f,2783.69f,256.853f,256.853f}, {-35.0255f,200.145f,51.7042f,336.912f,336.912f}, {2703.63f,198.478f,2790.36f,335.244f,335.244f}, {-38.3612f,273.532f,48.3685f,410.298f,410.298f}, {2696.96f,278.536f,2783.69f,415.302f,415.302f}, {-35.0255f,360.262f,51.7042f,497.028f,497.028f}, {2698.63f,356.926f,2785.36f,493.692f,493.692f}, {-38.3612f,435.316f,48.3685f,572.082f,572.082f}, {2701.96f,428.645f,2788.69f,565.411f,565.411f}, {-41.697f,515.375f,45.0327f,652.141f,652.141f}, {2701.96f,515.375f,2788.69f,652.141f,652.141f}, {-36.6933f,587.093f,50.0364f,723.859f,723.859f}, {2696.96f,583.758f,2783.69f,720.524f,720.524f}, {-41.697f,665.484f,45.0327f,802.25f,802.25f}, {2705.3f,663.816f,2792.03f,800.582f,800.582f}, {-36.6933f,737.202f,50.0364f,873.968f,873.968f}, {2703.63f,733.867f,2790.36f,870.633f,870.633f}, {-40.0291f,823.932f,46.7006f,960.698f,960.698f}, {2700.3f,813.925f,2787.03f,950.691f,950.691f}, {-33.3576f,900.655f,53.3721f,1037.42f,1037.42f}, {2696.96f,900.655f,2783.69f,1037.42f,1037.42f}, {-38.3612f,965.702f,48.3685f,1102.47f,1102.47f}, {2705.3f,970.705f,2792.03f,1107.47f,1107.47f}, {-35.0255f,1049.1f,51.7042f,1185.86f,1185.86f}, {2698.63f,1049.1f,2785.36f,1185.86f,1185.86f}, {-31.6897f,1122.48f,55.04f,1259.25f,1259.25f}, {2703.63f,1130.82f,2790.36f,1267.59f,1267.59f}, {-31.6897f,1207.54f,55.04f,1344.31f,1344.31f}, {2700.3f,1199.2f,2787.03f,1335.97f,1335.97f}, {-35.0255f,1275.93f,51.7042f,1412.69f,1412.69f}, {2706.97f,1275.93f,2793.7f,1412.69f,1412.69f}, {-31.6897f,1357.65f,55.04f,1494.42f,1494.42f}, {2705.3f,1352.65f,2792.03f,1489.42f,1489.42f}, {-40.0291f,1437.71f,46.7006f,1574.48f,1574.48f}, {2701.96f,1432.71f,2788.69f,1569.47f,1569.47f}, {1222.56f,1389.34f,1546.12f,1531.11f,1531.11f}, {775.564f,233.503f,1025.75f,567.079f,567.079f}, {233.503f,583.758f,330.24f,713.852f,713.852f}, {416.97f,583.758f,513.707f,713.852f,713.852f}, {600.436f,583.758f,697.173f,713.852f,713.852f}, {1200.87f,658.812f,1247.57f,717.188f,717.188f}, {2018.13f,66.7152f,2191.59f,253.518f,253.518f}, {2335.03f,133.43f,2548.52f,266.861f,266.861f}, {2268.32f,300.218f,2365.05f,430.313f,430.313f}, {2426.76f,300.218f,2523.5f,430.313f,430.313f}, {2268.32f,458.667f,2365.05f,588.761f,588.761f}, {2426.76f,458.667f,2523.5f,588.761f,588.761f}, {1542.79f,150.109f,1626.18f,261.857f,261.857f}, {2243.3f,642.133f,2326.69f,753.881f,753.881f}, {216.824f,587.093f,343.583f,710.516f,710.516f}, {934.012f,577.086f,1064.11f,713.852f,713.852f}, {1567.81f,573.75f,1689.56f,713.852f,713.852f}, {1834.67f,570.415f,1933.07f,713.852f,713.852f}, {2418.42f,840.611f,2538.51f,970.705f,970.705f}, {1314.29f,695.505f,1451.05f,823.932f,823.932f}, {500.364f,837.275f,607.108f,895.651f,895.651f}, {1150.84f,837.275f,1257.58f,895.651f,895.651f}, {1684.56f,837.275f,1791.3f,895.651f,895.651f}, {2084.85f,837.275f,2191.59f,895.651f,895.651f}, {767.224f,653.808f,807.253f,713.852f,713.852f}, {2318.35f,653.808f,2358.38f,713.852f,713.852f}, {783.903f,934.012f,1055.77f,1280.93f,1280.93f}, {333.576f,1000.73f,446.992f,1217.55f,1217.55f}, {433.648f,1017.41f,518.71f,1134.16f,1134.16f}, {1100.8f,1300.95f,1184.19f,1412.69f,1412.69f}, {150.109f,1317.62f,233.503f,1429.37f,1429.37f}, {2068.17f,1000.73f,2189.92f,1160.84f,1160.84f}, {2401.75f,934.012f,2515.16f,1150.84f,1150.84f}, {1567.81f,1284.27f,1664.54f,1414.36f,1414.36f}, {1767.95f,1284.27f,1864.69f,1414.36f,1414.36f}, {1167.52f,900.655f,1252.58f,1017.41f,1017.41f}, {2168.24f,1284.27f,2264.98f,1414.36f,1414.36f}, {2351.71f,1284.27f,2448.45f,1414.36f,1414.36f}, {2051.49f,1334.3f,2098.19f,1392.68f,1392.68f}, {56.7079f,583.758f,143.438f,720.524f,720.524f}, {56.7079f,725.527f,143.438f,862.293f,862.293f}, {1224.22f,567.079f,1310.95f,703.845f,703.845f}, {1441.05f,567.079f,1527.78f,703.845f,703.845f}, {1224.22f,725.527f,1310.95f,862.293f,862.293f}, {1441.05f,725.527f,1527.78f,862.293f,862.293f}, {890.647f,1217.55f,977.377f,1354.32f,1354.32f}, {1157.51f,917.333f,1244.24f,1054.1f,1054.1f}, {356.926f,1217.55f,443.656f,1354.32f,1354.32f}, {2591.88f,725.527f,2678.61f,862.293f,862.293f}, {2591.88f,583.758f,2678.61f,720.524f,720.524f} },
    };

    static LevelDef L5() => new LevelDef
    {
        Name = "PARQUE DE DIVERSOES", Title = "FASE 5 DE 6: O PARQUE DE DIVERSOES",
        Parts = new[] { "Art/Cenarios/cenario_diversoes_v8" },
        PartNames = new[] { "PARQUE DE DIVERSOES" }, BgScale = 1.0f,
        Story = "LUZES, BRINQUEDOS E MUITA GENTE!\nA MAMAE FOI NA FILA E NAO VOLTOU.",
        Outfit = "BLUSA ROXA E SAIA VERDE", MomShirt = 280f, MomSkirt = 140f,
        Tint = new Color(0.85f, 0.85f, 0.85f), Indoor = true, Outdoor = true,
        WorldW = 38f, WorldH = 21.2f,
        Walls = new float[,]
        {
            {-19f,9.4f,19f,10.6f}, {-19f,-10.6f,-18.2f,10.6f}, {18.2f,-10.6f,19f,10.6f}, {-19f,-10.6f,-1.26f,-9.8f},
            {1.26f,-10.6f,19f,-9.8f}, {-1.76f,-10.6f,-1.24f,-9.8f}, {1.24f,-10.6f,1.76f,-9.8f}, {-10.6f,7.86f,-7.96f,8.16f},
            {-1.4f,-0.4f,1.4f,1f}, {-4.1f,0.8f,-3.9f,1.04f}, {3.9f,0.8f,4.1f,1.04f}, {-16.8f,1.86f,-10.88f,6.3f},
            {-10.2f,1.32f,-9.96f,1.52f}, {-9.52f,1.38f,-9.2f,1.66f}, {10.4f,1.6f,14.1f,2.6f}, {15.2f,1.46f,16.34f,2.3f},
            {8.4f,1.32f,8.64f,1.52f}, {-7.8f,1.16f,-6.1f,2f}, {-5f,1.24f,-3.34f,2.04f}, {4.8f,1.24f,6.2f,2f},
            {6.88f,1.2f,7.56f,1.92f}, {-15f,-1.04f,-13.7f,-0.4f}, {-6.2f,-1.04f,-4.9f,-0.4f}, {5.8f,-1.04f,7.1f,-0.4f},
            {15.4f,-1.04f,16.7f,-0.4f}, {-11.32f,-0.92f,-11f,-0.64f}, {11.48f,-0.92f,11.8f,-0.64f}, {-16.8f,-4.2f,-13.6f,-1.8f},
            {-16.8f,-5.4f,-16.64f,-3.4f}, {-13.76f,-5.4f,-13.6f,-3.4f}, {-10.6f,-3.04f,-9.36f,-2.4f}, {-8.2f,-3.44f,-6.96f,-2.8f},
            {-9.6f,-5.24f,-8.36f,-4.6f}, {-5.84f,-2.7f,-5.36f,-2.2f}, {-3.24f,-4.7f,-2.76f,-4.2f}, {12.8f,-5.62f,15.7f,-4.8f},
            {4f,-3.16f,5.7f,-2.32f}, {6.6f,-3.08f,6.84f,-2.88f}, {3f,-6.04f,4.4f,-5.28f}, {-17.56f,0f,-17.24f,0.32f},
            {-1.96f,4.4f,-1.64f,4.72f}, {1.64f,4.4f,1.96f,4.72f}, {-1.96f,-2.4f,-1.64f,-2.08f}, {1.64f,-2.4f,1.96f,-2.08f},
            {17.64f,0f,17.96f,0.32f}, {-7.96f,4.8f,-7.64f,5.12f}, {6.84f,5.4f,7.16f,5.72f}, {2.84f,-8.6f,3.16f,-8.28f},
            {-3.16f,-8.8f,-2.84f,-8.48f}, {-9.16f,-8.4f,-8.84f,-8.08f}, {9.84f,-8.2f,10.16f,-7.88f}, {-15.4f,1.64f,-14.38f,2.44f},
            {-6.6f,4.2f,-5.58f,5f}, {4.6f,4.2f,5.62f,5f}, {6.2f,-8.4f,7.22f,-7.6f}, {-11.4f,-9f,-10.38f,-8.2f},
            {13f,-9f,14.02f,-8.2f}, {15f,5f,16.02f,5.8f}, {-5f,3.2f,-3.98f,4f}, {3.4f,3.2f,4.42f,4f},
            {1f,-3.68f,2.52f,-3f}, {-3.4f,-6.56f,-1.94f,-5.8f}, {6.12f,-6.2f,7.56f,-5.2f}, {-12.56f,-2.4f,-12.24f,-2.08f},
            {-11.96f,-4.8f,-11.64f,-4.48f}, {-6.76f,-7.6f,-6.44f,-7.28f}, {5.44f,-8.6f,5.76f,-8.28f}, {8.84f,-3.6f,9.16f,-3.28f},
            {9.44f,-1.2f,9.76f,-0.88f}, {16.04f,-2.4f,16.36f,-2.08f}, {16.84f,-7.6f,17.16f,-7.28f}, {-15.96f,-8.6f,-15.64f,-8.28f},
            {17.64f,3.8f,17.96f,4.12f}, {6.84f,3.8f,7.16f,4.12f}, {-17.8f,1.16f,-6.76f,1.48f}
        },
        ChildSpawns = new[] { new Vector2(0f, -7.9f), new Vector2(0f, -7.5f) },
        MomSpots = new[] { new Vector2(-13.125f, 6.975f), new Vector2(-3.125f, 7.225f), new Vector2(3.125f, 7.225f), new Vector2(15.125f, 6.975f), new Vector2(-16.125f, 0.725f), new Vector2(-9.125f, 0.475f), new Vector2(9.875f, 0.475f), new Vector2(16.875f, 0.725f), new Vector2(-5.125f, -1.775f), new Vector2(6.125f, -2.525f), new Vector2(9.875f, -3.525f), new Vector2(-0.125f, -4.275f), new Vector2(-11.875f, -7.525f), new Vector2(11.125f, -7.025f), new Vector2(15.875f, -8.525f), new Vector2(-5.875f, -5.525f), new Vector2(7.875f, -5.775f), new Vector2(-9.375f, 6.475f), new Vector2(-13.875f, 0.725f), new Vector2(-0.125f, -1.525f), new Vector2(11.625f, 1.225f), new Vector2(15.875f, 0.475f), new Vector2(-6.875f, -0.025f), new Vector2(-4.125f, -0.025f), new Vector2(5.375f, -0.025f), new Vector2(7.125f, -0.025f), new Vector2(8.625f, -0.025f), new Vector2(-15.125f, -7.275f), new Vector2(13.875f, -6.775f), new Vector2(4.875f, -4.275f) },
        ShopPos = new Vector2(7.6f, -1.8f),
        PlaceNames = new[] { "TREMZINHO", "BATE-BATE", "CARROSSEL", "RODA-GIGANTE", "BILHETERIA", "PIPOCA", "CACHORRO-QUENTE", "SORVETES", "SORVETE DE MORANGO", "BALOES", "FAZENDINHA", "MESAS DE PIQUENIQUE", "BARCO PIRATA", "PIPOCA DO SUL", "ENTRADA" },
        PlaceActs = new[] { "ESPERANDO O TREMZINHO", "OLHANDO OS CARRINHOS DE BATE-BATE", "ANDANDO NO CARROSSEL", "NA FILA DA RODA-GIGANTE", "COMPRANDO INGRESSOS", "COMPRANDO PIPOCA", "COMENDO UM CACHORRO-QUENTE", "TOMANDO UM SORVETE", "TOMANDO UM SORVETE DE MORANGO", "ESCOLHENDO UM BALAO", "OLHANDO OS BICHINHOS DA FAZENDINHA", "LANCHANDO NAS MESAS", "ESPERANDO O BARCO PIRATA", "COMPRANDO PIPOCA DOCE", "OLHANDO O PORTAO DE ENTRADA" },
        PlacePos = new[] { new Vector2(-9.28f, 7.1f), new Vector2(-13.84f, 1.6f), new Vector2(0f, -1f), new Vector2(11.72f, 2.2f), new Vector2(15.76f, 1f), new Vector2(-7f, 0.6f), new Vector2(-4.16f, 0.6f), new Vector2(5.5f, 0.6f), new Vector2(7.2f, 0.6f), new Vector2(8.52f, 0.6f), new Vector2(-15.2f, -6.6f), new Vector2(-9f, -3.4f), new Vector2(14f, -6.2f), new Vector2(4.8f, -3.8f), new Vector2(0f, -8.4f) },
        Occluders = new float[,] { {-27.52f,1442.63f,47.7979f,1561.4f,1561.4f}, {23.1747f,1444.08f,98.4926f,1562.85f,1562.85f}, {82.56f,1444.08f,157.878f,1562.85f,1562.85f}, {139.048f,1445.52f,214.366f,1564.29f,1564.29f}, {205.676f,1439.73f,280.994f,1558.5f,1558.5f}, {254.922f,1438.28f,330.24f,1557.05f,1557.05f}, {314.307f,1445.52f,389.625f,1564.29f,1564.29f}, {375.141f,1441.18f,450.459f,1559.95f,1559.95f}, {434.526f,1441.18f,509.844f,1559.95f,1559.95f}, {493.912f,1436.83f,569.229f,1555.6f,1555.6f}, {553.297f,1436.83f,628.615f,1555.6f,1555.6f}, {605.44f,1436.83f,680.758f,1555.6f,1555.6f}, {661.928f,1436.83f,737.246f,1555.6f,1555.6f}, {722.762f,1441.18f,798.08f,1559.95f,1559.95f}, {777.802f,1439.73f,853.12f,1558.5f,1558.5f}, {845.878f,1438.28f,921.196f,1557.05f,1557.05f}, {896.573f,1445.52f,971.891f,1564.29f,1564.29f}, {954.509f,1438.28f,1029.83f,1557.05f,1557.05f}, {1008.1f,1436.83f,1083.42f,1555.6f,1555.6f}, {1068.93f,1442.63f,1144.25f,1561.4f,1561.4f}, {1132.67f,1441.18f,1207.98f,1559.95f,1559.95f}, {1193.5f,1436.83f,1268.82f,1555.6f,1555.6f}, {1533.88f,1436.83f,1609.2f,1555.6f,1555.6f}, {1594.71f,1436.83f,1670.03f,1555.6f,1555.6f}, {1649.75f,1441.18f,1725.07f,1559.95f,1559.95f}, {1710.59f,1438.28f,1785.9f,1557.05f,1557.05f}, {1767.07f,1438.28f,1842.39f,1557.05f,1557.05f}, {1819.22f,1445.52f,1894.53f,1564.29f,1564.29f}, {1887.29f,1444.08f,1962.61f,1562.85f,1562.85f}, {1936.54f,1445.52f,2011.86f,1564.29f,1564.29f}, {2001.72f,1448.42f,2077.04f,1567.19f,1567.19f}, {2058.21f,1439.73f,2133.52f,1558.5f,1558.5f}, {2114.69f,1436.83f,2190.01f,1555.6f,1555.6f}, {2172.63f,1439.73f,2247.95f,1558.5f,1558.5f}, {2227.67f,1436.83f,2302.99f,1555.6f,1555.6f}, {2292.85f,1441.18f,2368.17f,1559.95f,1559.95f}, {2340.65f,1444.08f,2415.97f,1562.85f,1562.85f}, {2408.72f,1448.42f,2484.04f,1567.19f,1567.19f}, {2457.97f,1448.42f,2533.29f,1567.19f,1567.19f}, {2515.91f,1445.52f,2591.23f,1564.29f,1564.29f}, {2583.98f,1445.52f,2659.3f,1564.29f,1564.29f}, {2636.13f,1446.97f,2711.44f,1565.74f,1565.74f}, {2688.27f,1442.63f,2763.59f,1561.4f,1561.4f}, {-30.4168f,40.5558f,44.9011f,159.326f,159.326f}, {2705.65f,39.1074f,2780.97f,157.878f,157.878f}, {-34.7621f,108.632f,40.5558f,227.402f,227.402f}, {2712.89f,104.286f,2788.21f,223.057f,223.057f}, {-34.7621f,173.811f,40.5558f,292.581f,292.581f}, {2708.55f,181.053f,2783.87f,299.823f,299.823f}, {-30.4168f,249.128f,44.9011f,367.899f,367.899f}, {2710f,246.232f,2785.31f,365.002f,365.002f}, {-36.2105f,307.065f,39.1074f,425.836f,425.836f}, {2710f,314.307f,2785.31f,433.078f,433.078f}, {-30.4168f,376.589f,44.9011f,495.36f,495.36f}, {2710f,372.244f,2785.31f,491.015f,491.015f}, {-28.9684f,437.423f,46.3495f,556.194f,556.194f}, {2710f,446.114f,2785.31f,564.884f,564.884f}, {-30.4168f,509.844f,44.9011f,628.615f,628.615f}, {2710f,515.638f,2785.31f,634.408f,634.408f}, {-30.4168f,572.126f,44.9011f,690.897f,690.897f}, {2711.44f,582.265f,2786.76f,701.036f,701.036f}, {-33.3137f,638.754f,42.0042f,757.524f,757.524f}, {2711.44f,641.651f,2786.76f,760.421f,760.421f}, {-28.9684f,706.829f,46.3495f,825.6f,825.6f}, {2704.2f,705.381f,2779.52f,824.152f,824.152f}, {-28.9684f,780.699f,46.3495f,899.469f,899.469f}, {2708.55f,770.56f,2783.87f,889.331f,889.331f}, {-31.8653f,837.187f,43.4526f,955.958f,955.958f}, {2711.44f,837.187f,2786.76f,955.958f,955.958f}, {-33.3137f,908.16f,42.0042f,1026.93f,1026.93f}, {2708.55f,908.16f,2783.87f,1026.93f,1026.93f}, {-33.3137f,980.581f,42.0042f,1099.35f,1099.35f}, {2705.65f,970.442f,2780.97f,1089.21f,1089.21f}, {-30.4168f,1038.52f,44.9011f,1157.29f,1157.29f}, {2710f,1037.07f,2785.31f,1155.84f,1155.84f}, {-28.9684f,1112.39f,46.3495f,1231.16f,1231.16f}, {2708.55f,1105.15f,2783.87f,1223.92f,1223.92f}, {-36.2105f,1174.67f,39.1074f,1293.44f,1293.44f}, {2704.2f,1173.22f,2779.52f,1291.99f,1291.99f}, {-36.2105f,1238.4f,39.1074f,1357.17f,1357.17f}, {2704.2f,1244.19f,2779.52f,1362.96f,1362.96f}, {-30.4168f,1313.72f,44.9011f,1432.49f,1432.49f}, {2710f,1315.17f,2785.31f,1433.94f,1433.94f}, {-31.8653f,1370.21f,43.4526f,1488.98f,1488.98f}, {2711.44f,1371.65f,2786.76f,1490.43f,1490.43f}, {-33.3137f,1438.28f,42.0042f,1557.05f,1557.05f}, {2712.89f,1444.08f,2788.21f,1562.85f,1562.85f}, {1248.54f,1402.07f,1502.01f,1517.95f,1517.95f}, {608.337f,133.255f,799.528f,198.434f,198.434f}, {1260.13f,521.432f,1491.87f,796.632f,796.632f}, {1070.38f,608.337f,1102.25f,709.726f,709.726f}, {1649.75f,608.337f,1681.62f,709.726f,709.726f}, {159.326f,311.411f,588.059f,632.96f,632.96f}, {622.821f,579.368f,669.171f,672.067f,672.067f}, {680.758f,615.579f,715.52f,667.722f,667.722f}, {2085.73f,217.263f,2440.59f,641.651f,641.651f}, {2476.8f,543.158f,2559.36f,661.928f,661.928f}, {1969.85f,579.368f,2016.2f,672.067f,672.067f}, {811.116f,556.194f,934.232f,683.655f,683.655f}, {1013.89f,556.194f,1134.11f,677.861f,677.861f}, {1723.62f,573.575f,1825.01f,677.861f,677.861f}, {1868.46f,570.678f,1929.3f,680.758f,680.758f}, {289.684f,776.354f,383.832f,842.981f,842.981f}, {926.989f,776.354f,1021.14f,842.981f,842.981f}, {1796.04f,776.354f,1890.19f,842.981f,842.981f}, {2491.28f,776.354f,2585.43f,842.981f,842.981f}, {550.4f,782.147f,585.162f,834.291f,834.291f}, {2201.6f,782.147f,2236.36f,834.291f,834.291f}, {159.326f,898.021f,391.074f,1158.74f,1158.74f}, {608.337f,898.021f,698.139f,987.823f,987.823f}, {782.147f,926.989f,871.949f,1016.79f,1016.79f}, {680.758f,1057.35f,770.56f,1147.15f,1147.15f}, {941.474f,869.053f,999.411f,963.2f,963.2f}, {1129.77f,1013.89f,1187.71f,1108.04f,1108.04f}, {2259.54f,869.053f,2539.08f,1174.67f,1174.67f}, {1665.68f,869.053f,1788.8f,996.514f,996.514f}, {1839.49f,898.021f,1885.84f,990.72f,990.72f}, {1593.26f,1100.8f,1694.65f,1205.09f,1205.09f}, {78.2147f,651.789f,153.533f,770.56f,770.56f}, {1207.98f,333.137f,1283.3f,451.907f,451.907f}, {1468.7f,333.137f,1544.02f,451.907f,451.907f}, {1207.98f,825.6f,1283.3f,944.371f,944.371f}, {1468.7f,825.6f,1544.02f,944.371f,944.371f}, {2627.44f,651.789f,2702.75f,770.56f,770.56f}, {773.457f,304.168f,848.775f,422.939f,422.939f}, {1845.29f,260.716f,1920.61f,379.486f,379.486f}, {1555.6f,1274.61f,1630.92f,1393.38f,1393.38f}, {1121.08f,1289.09f,1196.4f,1407.87f,1407.87f}, {686.552f,1260.13f,761.869f,1378.9f,1378.9f}, {2062.55f,1245.64f,2137.87f,1364.41f,1364.41f}, {260.716f,547.503f,334.585f,648.893f,648.893f}, {898.021f,362.105f,971.891f,463.495f,463.495f}, {1709.14f,362.105f,1783.01f,463.495f,463.495f}, {1825.01f,1274.61f,1898.88f,1376f,1376f}, {550.4f,1318.06f,624.269f,1419.45f,1419.45f}, {2317.47f,1318.06f,2391.34f,1419.45f,1419.45f}, {2462.32f,304.168f,2536.19f,405.558f,405.558f}, {1013.89f,434.526f,1087.76f,535.916f,535.916f}, {1622.23f,434.526f,1696.1f,535.916f,535.916f}, {1448.42f,926.989f,1558.5f,1034.17f,1034.17f}, {1129.77f,1121.08f,1235.5f,1242.75f,1242.75f}, {1810.53f,1100.8f,1929.3f,1212.33f,1212.33f}, {440.32f,825.6f,515.638f,944.371f,944.371f}, {483.773f,999.411f,559.091f,1118.18f,1118.18f}, {860.362f,1202.19f,935.68f,1320.96f,1320.96f}, {1743.9f,1274.61f,1819.22f,1393.38f,1393.38f}, {1990.13f,912.505f,2065.45f,1031.28f,1031.28f}, {2033.58f,738.695f,2108.9f,857.465f,857.465f}, {2511.56f,825.6f,2586.88f,944.371f,944.371f}, {2569.5f,1202.19f,2644.82f,1320.96f,1320.96f}, {194.088f,1274.61f,269.406f,1393.38f,1393.38f}, {2627.44f,376.589f,2702.75f,495.36f,495.36f}, {1845.29f,376.589f,1920.61f,495.36f,495.36f} },
    };

    static LevelDef L6() => new LevelDef
    {
        Name = "PISTA DE GELO", Title = "FASE 6 DE 6: A PISTA DE GELO",
        Parts = new[] { "Art/Cenarios/cenario_gelo_v6" },
        PartNames = new[] { "PISTA DE GELO" }, BgScale = 1.5625f,
        Story = "A MAMAE FOI PATINAR NA PISTA DE GELO!\nTODO MUNDO DESLIZA RAPIDO: CUIDADO PARA NAO ERRAR.",
        Outfit = "BLUSA AZUL-CLARA E SAIA VERMELHA", MomShirt = 195f, MomSkirt = 0f,
        Tint = new Color(0.85f, 0.85f, 0.85f), Indoor = false, Outdoor = false,
        WorldW = 43f, WorldH = 24f,
        Walls = new float[,]
        {
            {-21.5f,9.125f,21.5f,12f}, {-21.5f,-12f,21.5f,-9.375f}, {-21.5f,-12f,-18.438f,12f}, {18.75f,-12f,21.5f,12f},
            {-18.438f,8.75f,-17.156f,9.125f}, {17.469f,8.75f,18.75f,9.125f}, {-18.438f,-9.375f,-17.156f,-9f}, {17.469f,-9.375f,18.75f,-9f},
            {-18.438f,8.375f,-17.625f,8.75f}, {17.938f,8.375f,18.75f,8.75f}, {-18.438f,-9f,-17.625f,-8.625f}, {17.938f,-9f,18.75f,-8.625f},
            {-18.438f,8f,-17.938f,8.375f}, {18.25f,8f,18.75f,8.375f}, {-18.438f,-8.625f,-17.938f,-8.25f}, {18.25f,-8.625f,18.75f,-8.25f},
            {-18.438f,7.625f,-18.188f,8f}, {18.5f,7.625f,18.75f,8f}, {-18.438f,-8.25f,-18.188f,-7.875f}, {18.5f,-8.25f,18.75f,-7.875f},
            {-18.438f,7.25f,-18.312f,7.625f}, {18.625f,7.25f,18.75f,7.625f}, {-18.438f,-7.875f,-18.312f,-7.5f}, {18.625f,-7.875f,18.75f,-7.5f},
            {-18.438f,6.875f,-18.406f,7.25f}, {18.719f,6.875f,18.75f,7.25f}, {-18.438f,-7.5f,-18.406f,-7.125f}, {18.719f,-7.5f,18.75f,-7.125f}
        },
        ChildSpawns = new[] { new Vector2(0f, -8f), new Vector2(0f, -7.375f) },
        MomSpots = new[] { new Vector2(-13.625f, 5.125f), new Vector2(-13.625f, -5.375f), new Vector2(13.625f, 5.125f), new Vector2(13.625f, -5.375f), new Vector2(0.125f, 7.375f), new Vector2(-0.125f, -0.125f), new Vector2(-5.875f, 2.625f), new Vector2(5.875f, -2.875f), new Vector2(-8.375f, -7.375f), new Vector2(8.625f, 6.625f), new Vector2(15.375f, -0.125f), new Vector2(-15.375f, -0.125f) },
        ShopPos = new Vector2(-16.5f, -8.469f),
        PlaceNames = new[] { "PISTA DE GELO" },
        PlaceActs = new[] { "PATINANDO NA PISTA DE GELO" },
        PlacePos = new[] { new Vector2(0f, 7.938f) },
        Ice = new float[,] { {-18.438f,-9.375f,18.75f,9.125f} },
        SkateLoop = new[] { new Vector2(15f, -0.125f), new Vector2(13.858f, -2.935f), new Vector2(10.607f, -5.318f), new Vector2(5.74f, -6.91f), new Vector2(0f, -7.469f), new Vector2(-5.74f, -6.91f), new Vector2(-10.607f, -5.318f), new Vector2(-13.858f, -2.935f), new Vector2(-15f, -0.125f), new Vector2(-13.858f, 2.685f), new Vector2(-10.607f, 5.068f), new Vector2(-5.74f, 6.66f), new Vector2(-0f, 7.219f), new Vector2(5.74f, 6.66f), new Vector2(10.607f, 5.068f), new Vector2(13.858f, 2.685f) },
    };
}
