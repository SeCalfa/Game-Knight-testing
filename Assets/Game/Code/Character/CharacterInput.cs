using UnityEngine;

namespace Game.Code.Character
{
    public class CharacterInput
    {
        public InputData Update()
        {
            // Calculating inputs
            var horizontal = Input.GetAxis("Horizontal");
            var jump = Input.GetKeyDown(KeyCode.Space);
            var dash = Input.GetKeyDown(KeyCode.LeftShift);

            // Return data
            return new InputData
            {
                Horizontal = horizontal,
                Jump = jump,
                Dash = dash
            };
        }
    }

    public struct InputData
    {
        public float Horizontal;
        public bool Jump;
        public bool Dash;
    }
}
