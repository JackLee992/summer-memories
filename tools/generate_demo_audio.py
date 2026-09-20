#!/usr/bin/env python3
"""Reproducible original prototype music/SFX. Python standard library only.

No samples, soundtrack extracts, remote APIs, or voice models are used.
Run from any directory: python3 tools/generate_demo_audio.py
"""
import argparse
import hashlib
import json
import math
from array import array
from pathlib import Path
import random
import sys
import uuid
import wave

ROOT = Path(__file__).resolve().parents[1]
RATE = 44100
TAU = 2 * math.pi
SEED = 20260920


def buffer(seconds):
    n = round(seconds * RATE)
    return array('f', [0]) * n, array('f', [0]) * n


def frequency(midi):
    return 440 * 2 ** ((midi - 69) / 12)


def tone(channels, start, duration, hz, gain, pan=0, attack=.012, release=.25, pluck=False):
    left, right = channels
    a = max(0, round(start * RATE))
    b = min(len(left), round((start + duration) * RATE))
    lg, rg = math.sqrt((1 - pan) / 2), math.sqrt((1 + pan) / 2)
    for i in range(a, b):
        t = (i - a) / RATE
        envelope = min(1, t / attack, max(0, (duration - t) / release))
        envelope = envelope * envelope * (3 - 2 * envelope)
        if pluck:
            envelope *= math.exp(-3 * t / duration)
        v = gain * envelope * (math.sin(TAU * hz * t) + .16 * math.sin(TAU * hz * 2 * t))
        left[i] += v * lg
        right[i] += v * rg


def ambience():
    channels = buffer(32)
    # Four original suspended voicings; one quiet motif, no borrowed tune.
    chords = [(50, 57, 64), (46, 53, 60), (43, 50, 57), (48, 55, 62)]
    for bar, chord in enumerate(chords):
        for voice, note in enumerate(chord):
            tone(channels, bar * 8, 8, frequency(note), .105,
                 pan=(voice - 1) * .5, attack=1.3, release=2)
    for start, note, pan in [(2, 74, -.3), (5, 69, .3), (10, 77, -.2),
                             (13, 76, .2), (18, 74, -.3), (21, 69, .3),
                             (26, 72, -.2), (29, 69, .2)]:
        tone(channels, start, 2.7, frequency(note), .095, pan, pluck=True)
        tone(channels, start + .24, 2.4, frequency(note), .028, -pan, pluck=True)
    rng = random.Random(SEED)
    smooth = 0
    for i in range(len(channels[0])):
        t = i / RATE
        smooth = .985 * smooth + .015 * rng.uniform(-1, 1)
        envelope = min(1, t / 2, (32 - t) / 2)
        surf = smooth * .3 * (.6 + .4 * math.sin(TAU * t / 8)) * envelope
        channels[0][i] += surf
        channels[1][i] += surf * .9
    return channels


def sweep(seconds, start_hz, end_hz, noise_gain, rising=False):
    channels = buffer(seconds)
    rng = random.Random(SEED + round(start_hz))
    phase, noise = 0, 0
    for i in range(len(channels[0])):
        t = i / RATE
        k = t / seconds
        hz = start_hz * (end_hz / start_hz) ** k
        phase += TAU * hz / RATE
        noise = .76 * noise + .24 * rng.uniform(-1, 1)
        envelope = math.sin(math.pi * k) ** 1.4
        if rising:
            envelope *= .3 + .7 * k
        v = envelope * (.24 * math.sin(phase) + noise_gain * noise)
        pan = .55 * math.sin(TAU * k)
        channels[0][i] = v * math.sqrt((1 - pan) / 2)
        channels[1][i] = v * math.sqrt((1 + pan) / 2)
    return channels


def shell_pickup():
    channels = buffer(1.4)
    for start, note, gain in [(0, 81, .24), (.13, 88, .16), (.28, 93, .07)]:
        tone(channels, start, 1.4 - start, frequency(note), gain, pluck=True)
    return channels


def ensure_meta(path, folder=False):
    meta = Path(str(path) + '.meta')
    if meta.exists():
        return
    # Stable GUIDs across regeneration; never replace existing Unity GUIDs.
    relative = path.relative_to(ROOT).as_posix()
    guid = uuid.uuid5(uuid.NAMESPACE_URL, 'summer-memories/' + relative).hex
    header = f'fileFormatVersion: 2\nguid: {guid}\n'
    if folder:
        body = 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    else:
        body = ('AudioImporter:\n  externalObjects: {}\n  serializedVersion: 7\n'
                '  defaultSettings:\n    serializedVersion: 2\n    loadType: 0\n'
                '    sampleRateSetting: 0\n    sampleRateOverride: 44100\n'
                '    compressionFormat: 1\n    quality: 1\n    conversionMode: 0\n'
                '    preloadAudioData: 1\n  platformSettingOverrides: {}\n'
                '  forceToMono: 0\n  normalize: 0\n  loadInBackground: 0\n'
                '  ambisonic: 0\n  3D: 1\n  userData: Original procedural prototype\n'
                '  assetBundleName: \n  assetBundleVariant: \n')
    meta.write_text(header + body, encoding='utf-8')


def write_wav(path, channels, target_peak):
    peak = max(max(abs(x) for x in c) for c in channels)
    gain = target_peak / peak if peak else 1
    pcm = array('h')
    energy = 0
    max_delta = 0
    previous = [0, 0]
    for pair in zip(*channels):
        for ch, sample in enumerate(pair):
            v = sample * gain
            energy += v * v
            max_delta = max(max_delta, abs(v - previous[ch]))
            previous[ch] = v
            pcm.append(round(max(-1, min(1, v)) * 32767))
    if sys.byteorder != 'little':
        pcm.byteswap()
    with wave.open(str(path), 'wb') as out:
        out.setnchannels(2)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(pcm.tobytes())
    return {'file': path.name, 'seconds': len(channels[0]) / RATE,
            'sampleRate': RATE, 'channels': 2, 'bits': 16,
            'peakDbFS': round(20 * math.log10(target_peak), 2),
            'rmsDbFS': round(20 * math.log10(math.sqrt(energy / len(pcm))), 2),
            'maxAdjacentSampleDelta': round(max_delta, 6),
            'firstSamples': list(pcm[:2]), 'lastSamples': list(pcm[-2:]),
            'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out', type=Path, default=ROOT / 'Assets/Resources/Audio/Prototype')
    args = parser.parse_args()
    out = args.out.resolve()
    out.mkdir(parents=True, exist_ok=True)
    in_assets = out.is_relative_to(ROOT / 'Assets')
    if in_assets:
        folder = out
        while folder != ROOT / 'Assets':
            ensure_meta(folder, folder=True)
            folder = folder.parent
    specs = [
        ('st_ambient_v01.wav', ambience, .25, '32s original seaside mystery sketch; fades at both ends'),
        ('st_shadow_reveal_v01.wav', lambda: sweep(1.15, 110, 55, .34), .5, 'Shadow revealed'),
        ('st_rewind_v01.wav', lambda: sweep(2.2, 90, 900, .2, True), .5, 'Time rewind'),
        ('st_hair_bind_v01.wav', lambda: sweep(.65, 580, 145, .5), .45, 'Hair action cue'),
        ('st_shell_pickup_v01.wav', shell_pickup, .42, 'Shell clue collected'),
    ]
    records = []
    for filename, make, peak, purpose in specs:
        path = out / filename
        record = write_wav(path, make(), peak)
        record['purpose'] = purpose
        record['loop'] = False
        record['status'] = 'generated_not_runtime_integrated'
        records.append(record)
        if in_assets:
            ensure_meta(path)
        print(f'{filename}: {record["seconds"]:.2f}s, peak {record["peakDbFS"]} dBFS')
    report = {'generator': 'tools/generate_demo_audio.py', 'seed': SEED,
              'source': 'Original math synthesis; no third-party samples or soundtrack',
              'version': 1, 'files': records}
    # Report belongs with build evidence, not in runtime Resources.
    report_dir = ROOT / 'Builds/audio-audit'
    report_dir.mkdir(parents=True, exist_ok=True)
    (report_dir / 'st-demo-audio.json').write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
