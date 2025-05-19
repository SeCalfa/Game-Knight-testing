using UnityEngine;

namespace Game.Code.Character
{
    public class CharacterModel : MonoBehaviour
    {
        [SerializeField] private CharacterMovement characterMovement;
        
        private static readonly int End = Animator.StringToHash("AttackEnd");

        private void Hit()
        {
            characterMovement.Hit();
        }
        
        private void AttackEnd()
        {
            characterMovement.GetAnimator.SetTrigger(End);
            characterMovement.AttackEnd();
        }
    }
}
