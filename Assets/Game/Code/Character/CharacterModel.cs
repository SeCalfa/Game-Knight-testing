using UnityEngine;

namespace Game.Code.Character
{
    public class CharacterModel : MonoBehaviour
    {
        [SerializeField] private CharacterMovement characterMovement;

        private void Hit()
        {
            characterMovement.Hit();
        }

        private void Attack1End()
        {
            characterMovement.AttackEnd(AttackState.Attack1);
        }
        
        private void Attack2End()
        {
            characterMovement.AttackEnd(AttackState.Attack2);
        }
        
        private void Attack3End()
        {
            characterMovement.AttackEnd(AttackState.Attack3);
        }
    }
}
