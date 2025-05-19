using UnityEngine;
using UnityEngine.Events;

namespace Game.Code.Effects
{
    public class CameraShake : MonoBehaviour
    {
        public UnityEvent onStartShake;

        // Current shake amount
        private float trauma;

        private float Trauma
        {
            get => trauma;
            set => trauma = Mathf.Clamp01(value);
        }

        private float power = 16;
        private float movementAmount = 0.8f;
        private float rotationAmount = 17f;
        private float traumaDepthMag = 0.6f;
        private float traumaDecay = 1.3f;
        private float timeCounter = 0;
        
        public static CameraShake Instance;

        private float GetFloat(float seed) { return (Mathf.PerlinNoise(seed, timeCounter) - 0.5f) * 2f; }
        private Vector3 GetVec3() { return new Vector3(GetFloat(1), GetFloat(10), GetFloat(100) * traumaDepthMag); }
        
        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (Trauma > 0)
            {
                timeCounter += Time.deltaTime * Mathf.Pow(Trauma, 0.3f) * power;

                var newPos = GetVec3() * movementAmount * Trauma;
                transform.localPosition = newPos;

                transform.localRotation = Quaternion.Euler(newPos * rotationAmount);

                Trauma -= Time.deltaTime * traumaDecay * (Trauma + 0.3f);
            }
            else
            {
                var newPos = Vector3.Lerp(transform.localPosition, Vector3.zero, Time.deltaTime);
                transform.localPosition = newPos;
                transform.localRotation = Quaternion.Euler(newPos * rotationAmount);
            }
        }

        public void Shake(float amount, float power, float movementAmount, float rotationAmount)
        {
            Trauma = amount;
            this.power = power;
            this.movementAmount = movementAmount;
            this.rotationAmount = rotationAmount;

            onStartShake?.Invoke();
        }
    }
}
