﻿using System;

namespace ManagedDoom
{
    public interface IAudio
    {
        public void SetListener(Mobj listener);
        public void Update();
        public void StartSound(Mobj mobj, Sfx sfx, SfxType type);
        public void StopSound(Mobj mobj);
        public void Reset();
    }
}
