// Menu inicial em JavaScript puro. Mesmas funções do menu do Godot:
// modos de jogo, opções (salvas no navegador), créditos e fundo que alterna.

const BACKGROUNDS = [
  "assets/menu_bg_shopping.png",
  "assets/menu_bg_parque.png",
  "assets/menu_bg_mercado.png",
];
const BG_INTERVAL = 7000;
const MODE_NAMES = { historia: "História", "contra-o-tempo": "Contra o Tempo", livre: "Modo Livre" };
const STORAGE_KEY = "onde-esta-a-mamae:opcoes";

const $ = (sel) => document.querySelector(sel);
const stage = $("#stage");
const hint = $("#hint");
const mainButtons = $("#mainButtons");

// ---------- Opções ----------
const settings = { music: 0.8, sfx: 0.8, reduceMotion: false };
try {
  Object.assign(settings, JSON.parse(localStorage.getItem(STORAGE_KEY)) || {});
} catch { /* sem armazenamento: usa o padrão */ }

function saveSettings() {
  try { localStorage.setItem(STORAGE_KEY, JSON.stringify(settings)); } catch { /* ignora */ }
}

const applyVolumes = () => Sound.setVolumes(settings.music, settings.sfx);
applyVolumes();

function bindSlider(id, key) {
  const input = $(id);
  const paint = () => input.style.setProperty("--fill", `${input.value * 100}%`);
  input.value = settings[key];
  paint();
  input.addEventListener("input", () => {
    settings[key] = Number(input.value);
    paint();
    saveSettings();
    applyVolumes();
    if (key === "sfx") Sound.play("navegar"); // amostra do volume dos efeitos
  });
}
bindSlider("#music", "music");
bindSlider("#sfx", "sfx");

function setSwitch(el, on) { el.setAttribute("aria-checked", String(on)); }

const motionSwitch = $("#reduceMotion");
setSwitch(motionSwitch, settings.reduceMotion);
motionSwitch.addEventListener("click", () => {
  Sound.play("alternar");
  settings.reduceMotion = !settings.reduceMotion;
  setSwitch(motionSwitch, settings.reduceMotion);
  saveSettings();
});

const fullscreenSwitch = $("#fullscreen");
fullscreenSwitch.addEventListener("click", () => {
  Sound.play("alternar");
  if (document.fullscreenElement) document.exitFullscreen();
  else document.documentElement.requestFullscreen?.();
});
document.addEventListener("fullscreenchange", () => setSwitch(fullscreenSwitch, !!document.fullscreenElement));

// ---------- Escala inteira (320×180 → ×2, ×3, ×4...) ----------
function fit() {
  const scale = Math.max(1, Math.floor(Math.min(innerWidth / 320, innerHeight / 180)));
  stage.style.setProperty("--scale", scale);
}
addEventListener("resize", fit);
fit();

// ---------- Telas ----------
let current = mainButtons;
let returnFocus = mainButtons.querySelector("button");

function show(screen, focusEl) {
  silentFocus = true;
  document.querySelectorAll(".screen").forEach((s) => (s.hidden = s !== screen));
  current = screen;
  if (screen === mainButtons) {
    returnFocus.focus();
  } else {
    hint.textContent = "Esc: voltar";
    (focusEl || screen.querySelector("input, button")).focus();
  }
  silentFocus = false;
}

mainButtons.querySelectorAll("button").forEach((btn) => {
  btn.addEventListener("focus", () => (hint.textContent = btn.dataset.hint));
  btn.addEventListener("click", () => {
    returnFocus = btn;
    Sound.play(btn.dataset.mode ? "iniciar" : "confirmar");
    if (btn.dataset.mode) {
      $("#playTitle").textContent = MODE_NAMES[btn.dataset.mode];
      show($("#play"));
    } else {
      show($("#" + btn.dataset.open));
    }
  });
});
document.querySelectorAll(".back").forEach((b) => b.addEventListener("click", () => {
  Sound.play("voltar");
  show(mainButtons);
}));

// Mouse em cima = foco, igual ao Godot.
document.querySelectorAll("button, input").forEach((el) =>
  el.addEventListener("mouseenter", () => el.focus()));

// ---------- Teclado: setas/WASD para navegar, Enter/Espaço para escolher, Esc para voltar ----------
addEventListener("keydown", (e) => {
  const key = e.key;
  if (key === "Escape" && current !== mainButtons) {
    Sound.play("voltar");
    show(mainButtons);
    return;
  }
  const up = key === "ArrowUp" || key === "w" || key === "W";
  const down = key === "ArrowDown" || key === "s" || key === "S";
  if (!up && !down) return;
  e.preventDefault();
  const items = [...current.querySelectorAll("button, input")];
  const i = items.indexOf(document.activeElement);
  const next = i < 0 ? 0 : (i + (down ? 1 : -1) + items.length) % items.length;
  items[next].focus();
});

// Clicar fora não deve deixar o menu sem foco.
document.addEventListener("mousedown", (e) => {
  if (!e.target.closest("button, input")) e.preventDefault();
});

// ---------- Som ao navegar ----------
// Toca quando o foco muda por ação do jogador (não no foco automático ao trocar de tela).
let silentFocus = true;
document.addEventListener("focusin", () => {
  if (!silentFocus) Sound.play("navegar");
});
["keydown", "pointerdown"].forEach((type) =>
  addEventListener(type, () => Sound.unlock(), { capture: true }));

// ---------- Fundo alternando entre os cenários ----------
const bgA = $("#bgA");
const bgB = $("#bgB");
let bgIndex = 0;
BACKGROUNDS.forEach((src) => (new Image().src = src));

setInterval(() => {
  bgIndex = (bgIndex + 1) % BACKGROUNDS.length;
  const src = BACKGROUNDS[bgIndex];
  if (settings.reduceMotion) {
    bgA.src = src;
    return;
  }
  bgB.src = src;
  bgB.classList.add("fading");
  setTimeout(() => {
    bgA.src = src;
    bgB.classList.remove("fading");
  }, 1250);
}, BG_INTERVAL);

// ---------- Início ----------
if (!settings.reduceMotion) $("#ui").classList.add("intro");
show(mainButtons);
