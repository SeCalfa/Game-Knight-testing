using UnityEngine;

namespace Game.Code.Effects
{
    public class Impact : MonoBehaviour
    {
        private Animator animator;
        
        private static bool _toggleImpact;
        
        private static readonly int Impact1 = Animator.StringToHash("Impact1");
        private static readonly int Impact2 = Animator.StringToHash("Impact2");

        private void Awake()
        {
            animator = GetComponent<Animator>();

            StartAnimation();
        }

        private void StartAnimation()
        {
            animator.SetTrigger(_toggleImpact ? Impact1 : Impact2);

            _toggleImpact = !_toggleImpact;
        }

        private void AnimationEnd()
        {
            Destroy(gameObject);
        }
    }
}
