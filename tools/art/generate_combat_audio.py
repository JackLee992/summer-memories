#!/usr/bin/env python3
"""Reproducible original hit/jump/dodge prototype audio; no external samples."""
import math, random, struct, wave
from pathlib import Path
out=Path(__file__).resolve().parents[2]/'Assets/Resources/Audio/Prototype'
random.seed(20260920)
for name,duration in [('hit',.16),('jump',.22),('dodge',.20)]:
    rate=44100;data=bytearray()
    for i in range(int(rate*duration)):
        t=i/rate;envelope=(1-t/duration)**2
        if name=='hit':sample=(random.uniform(-1,1)*.7+math.sin(2*math.pi*(135*t-200*t*t))*.3)*envelope
        elif name=='jump':sample=(math.sin(2*math.pi*(260*t+700*t*t))*.6+random.uniform(-1,1)*.15)*envelope
        else:sample=random.uniform(-1,1)*envelope*math.sin(math.pi*t/duration)
        n=int(sample*16000);data.extend(struct.pack('<hh',n,n))
    with wave.open(str(out/('st_'+name+'_v02.wav')),'wb') as stream:
        stream.setnchannels(2);stream.setsampwidth(2);stream.setframerate(rate);stream.writeframes(data)
