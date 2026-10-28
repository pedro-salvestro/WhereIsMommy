# Onde Está a Mamãe? (Unity)

Versão em Unity do jogo de labirinto 2D do projeto [WhereIsMommy](https://github.com/pedro-salvestro/WhereIsMommy).

## Como abrir
1. Unity Hub → **Add** → selecione esta pasta (Unity 6, `6000.0.x`).
2. Ao abrir, as cenas são geradas sozinhas (ou use o menu **Mamae → Gerar Cenas**).
3. Abra `Assets/Scenes/Menu.unity` e aperte **Play**.

## Controles
| Ação | Tecla |
|:--|:--|
| Andar | WASD / setas |
| Interagir com a mamãe | E / Espaço |
| Pausar | Esc |
| Reiniciar / Menu (fim de jogo ou pausa) | R / M |

## Regras
- **História** (90 s), **Contra o Tempo** (60 s, guarda recorde) e **Livre** (sem cronômetro).
- Esbarrar nos policiais/pessoas custa 5 s.
- Pontos = 1000 + tempo restante × 10 + bônus de eficiência (500 − 100 por esbarrão).

## Estrutura
- `Assets/Scripts`: menu, fase, entrada e utilitários (tudo construído por código).
- `Assets/Resources`: sprites, cenário, áudio e fonte vindos do repositório original.
- `Assets/Editor/ProjectSetup.cs`: configura importação dos sprites e gera as cenas.

Obs.: o cenário principal já traz HUD desenhado na imagem (moedas, relógio, minimapa); é só decoração.
