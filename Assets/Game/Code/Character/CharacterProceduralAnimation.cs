using UnityEngine;

namespace Game.Code.Character
{
    [RequireComponent(typeof(CharacterMovement))]
    public class CharacterProceduralAnimation : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [Space]
        [SerializeField] private float scaleLerpSpeed;
        [SerializeField] private Vector2 jumpScale;
        [SerializeField] private Vector2 landScale;
        
        private CharacterMovement characterMovement;
        
        private Vector3 targetScale;

        private void Awake()
        {
            characterMovement = GetComponent<CharacterMovement>();

            targetScale = model.localScale;

            characterMovement.OnJump += OnJump;
            characterMovement.OnLand += OnLand;
        }

        private void Update()
        {
            if (model.localScale != targetScale)
            {
                model.localScale = Vector3.Lerp(
                    model.localScale,
                    targetScale,
                    Mathf.Clamp01(scaleLerpSpeed * Time.deltaTime));
            }
        }

        private void OnDisable()
        {
            characterMovement.OnJump -= OnJump;
            characterMovement.OnLand -= OnLand;
        }

        private void OnJump() => model.localScale = jumpScale;
        private void OnLand() => model.localScale = landScale;
    }
}