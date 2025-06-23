using System;

namespace Game.Code.Character
{
    [Serializable]
    public struct CharacterTimers
    {
        public float lastOnGroundTime;
        public float timeAfterDash;
    }
}