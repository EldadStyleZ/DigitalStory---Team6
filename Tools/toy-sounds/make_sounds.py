"""
Makes the toy's sound files: one short tune per song and the balloon pop, as WAV files in
Assets/_Project/Audio. Run it again after changing a sound, then return to Unity so it imports them.

The song tunes copy the old game's jingles where it had one. The flute's own notes are not files: the
Flute Voice plays them live with the old game's synthesizer, so they fade out instead of stopping dead.
"""
import math
import os
import random
import struct
import sys
import wave

RATE = 44100
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "..", "..", "Assets", "_Project", "Audio")

BASE_HERTZ = 587.33
SEMITONES = [0, 2, 5, 7, 9, 12]


def save(path, samples):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    peak = max(1e-6, max(abs(s) for s in samples))
    scale = 0.85 / peak if peak > 0.85 else 1.0
    with wave.open(path, "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s * scale)) * 32767)) for s in samples))


def tone(samples, start, seconds, start_hertz, end_hertz, volume, shape="sine"):
    """Adds one gliding tone that fades in quickly and out smoothly."""
    first = int(start * RATE)
    count = int(seconds * RATE)
    while len(samples) < first + count:
        samples.append(0.0)
    phase = 0.0
    for i in range(count):
        share = i / count
        hertz = start_hertz + (end_hertz - start_hertz) * share
        phase += 2 * math.pi * hertz / RATE
        if shape == "triangle":
            wave_value = 2 / math.pi * math.asin(math.sin(phase))
        elif shape == "square":
            wave_value = 0.6 * math.tanh(4 * math.sin(phase))
        else:
            wave_value = math.sin(phase)
        envelope = min(1.0, i / (0.01 * RATE)) * (1 - share) ** 1.5
        samples[first + i] += wave_value * envelope * volume


def noise(samples, start, seconds, volume, rng):
    first = int(start * RATE)
    count = int(seconds * RATE)
    while len(samples) < first + count:
        samples.append(0.0)
    for i in range(count):
        samples[first + i] += (rng.random() * 2 - 1) * volume * (1 - i / count) ** 3


def note_hertz(key):
    return BASE_HERTZ * 2 ** (SEMITONES[key - 1] / 12)


def song_tunes(rng):
    tunes = {}

    dance = []
    for step, key in enumerate([1, 3, 5, 6]):
        tone(dance, step * 0.09, 0.35, note_hertz(key), note_hertz(key), 0.5, "triangle")
    tunes["Dance"] = dance

    conga = []
    for step, key in enumerate([2, 1, 1, 2, 2, 1]):
        tone(conga, step * 0.16, 0.14, note_hertz(key), note_hertz(key), 0.5, "triangle")
        noise(conga, step * 0.16, 0.05, 0.15, rng)
    tunes["Conga"] = conga

    heart = []
    for beat in (0.0, 0.45):
        tone(heart, beat, 0.18, 110, 70, 0.9)
        tone(heart, beat + 0.17, 0.22, 100, 60, 0.7)
    tone(heart, 0.0, 0.9, note_hertz(4), note_hertz(4), 0.15)
    tunes["Heart"] = heart

    disco = []
    for beat in range(4):
        tone(disco, beat * 0.25, 0.2, 130, 45, 1.0)
        noise(disco, beat * 0.25 + 0.125, 0.04, 0.25, rng)
    tunes["Disco"] = disco

    balloons = []
    for step, (low, high) in enumerate([(300, 600), (420, 760), (540, 920), (660, 1080), (780, 1240)]):
        tone(balloons, step * 0.07, 0.12, low, high, 0.6)
    tunes["Balloons"] = balloons

    choir = []
    for step, semitone in enumerate([12, 16, 19, 24, 19, 16, 21, 19, 17, 16, 14, 12]):
        hertz = 1500 * 2 ** (semitone / 12)
        tone(choir, step * 0.17, 0.15, hertz * 1.04, hertz, 0.5, "triangle")
        tone(choir, step * 0.17, 0.12, hertz * 1.5, hertz * 1.48, 0.2)
    tunes["Choir"] = choir

    surf = []
    tone(surf, 0.0, 0.6, 400, 1300, 0.8)
    tunes["CrowdSurf"] = surf

    faint = []
    for start, seconds, high, low in [(0.0, 0.3, 392, 380), (0.32, 0.3, 370, 359), (0.64, 0.3, 349, 339), (0.96, 0.9, 311, 302)]:
        tone(faint, start, seconds, high, low, 0.6, "square")
    tunes["Faint"] = faint

    return tunes


def pop_sound(rng):
    """The burst of a floating rat's balloon."""
    pop = []
    noise(pop, 0.0, 0.08, 0.9, rng)
    tone(pop, 0.0, 0.1, 900, 200, 0.6)
    return pop


if __name__ == "__main__":
    rng = random.Random(7)
    tunes = song_tunes(rng)
    for name, samples in tunes.items():
        save(os.path.join(OUT, "Songs", f"{name}.wav"), samples)
    save(os.path.join(OUT, "Pop.wav"), pop_sound(rng))
    print("made", 1 + len(tunes), "sounds in", os.path.normpath(OUT))
