using System;
using UnityEngine;

namespace Game.Code.Character
{
    [Serializable]
    public struct CharacterParams
    {
        [Header("Movement")]
        public float speed;
        public float runAcceleration;
        public float runDecceleration;
        public float accelInAir;
        public float deccelInAir;

        [Header("Gravity")]
        public float gravityScale;
        public float fallGravityMult;

        [Header("Jump")]
        [Range(1, 3)]
        public int amountOfJumps;
        public float jumpForce;

        [Header("Dash")]
        public float dashSpeed;
        public float dashDuration;
        public float dashResetTime;
        
        [Header("Slide")]
        public float wallSlideSpeed;
    }
}