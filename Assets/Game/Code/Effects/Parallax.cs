using UnityEngine;

namespace Game.Code.Effects
{
    public class Parallax : MonoBehaviour
    {
        [SerializeField]
        private float parallaxIntensity;

        private Camera cam;
        private float startPos, spriteLength;

        private void Awake()
        {
            cam = Camera.main;

            startPos = transform.localPosition.x;
            spriteLength = GetComponent<SpriteRenderer>().bounds.size.x;
        }

        private void Update()
        {
            Scroll();
        }

        private void Scroll()
        {
            var currentCamPos = cam.transform.position;
            var distance = currentCamPos.x * (1 - parallaxIntensity);

            transform.localPosition = new Vector3(startPos + distance, transform.localPosition.y, transform.localPosition.z);
        }
    }
}
