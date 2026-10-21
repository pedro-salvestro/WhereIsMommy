// Sons do menu com Web Audio: efeitos curtos e música em loop.
// Os volumes seguem os sliders de Música e Efeitos das Opções.
// Navegadores só liberam áudio depois do primeiro clique ou tecla,
// então a música começa na primeira interação do jogador.

const Sound = (() => {
  const FILES = {
    navegar: "assets/audio/navegar.wav",
    confirmar: "assets/audio/confirmar.wav",
    voltar: "assets/audio/voltar.wav",
    alternar: "assets/audio/alternar.wav",
    iniciar: "assets/audio/iniciar.wav",
    musica: "assets/audio/musica_menu.wav",
  };

  const AudioCtx = window.AudioContext || window.webkitAudioContext;
  const ctx = AudioCtx ? new AudioCtx() : null;
  const buffers = {};
  let musicGain, sfxGain, musicSource;
  let unlocked = false;

  if (ctx) {
    musicGain = ctx.createGain();
    sfxGain = ctx.createGain();
    musicGain.connect(ctx.destination);
    sfxGain.connect(ctx.destination);
    for (const [name, url] of Object.entries(FILES)) {
      fetch(url)
        .then((r) => r.arrayBuffer())
        .then((data) => ctx.decodeAudioData(data))
        .then((buf) => {
          buffers[name] = buf;
          if (name === "musica" && unlocked) startMusic();
        })
        .catch(() => { /* sem o arquivo, o menu segue sem som */ });
    }
  }

  // Curva de volume mais natural para o ouvido do que a linear.
  const curve = (v) => Math.pow(Math.max(0, Math.min(1, v)), 2);

  function setVolumes(music, sfx) {
    if (!ctx) return;
    musicGain.gain.setTargetAtTime(curve(music) * 0.6, ctx.currentTime, 0.05);
    sfxGain.gain.setTargetAtTime(curve(sfx), ctx.currentTime, 0.02);
  }

  function startMusic() {
    if (!ctx || musicSource || !buffers.musica) return;
    musicSource = ctx.createBufferSource();
    musicSource.buffer = buffers.musica;
    musicSource.loop = true;
    musicSource.connect(musicGain);
    musicSource.start();
  }

  function unlock() {
    if (!ctx || unlocked) return;
    unlocked = true;
    ctx.resume().then(startMusic);
  }

  function play(name) {
    if (!ctx || !unlocked || !buffers[name]) return;
    const src = ctx.createBufferSource();
    src.buffer = buffers[name];
    src.connect(sfxGain);
    src.start();
  }

  return { play, setVolumes, unlock };
})();
