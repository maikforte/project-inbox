using UnityEngine;
using UnityEngine.UI;

namespace InboxZero.UI
{
    /// Tracks active/inactive sprite state for a sidebar nav button.
    /// Attach to each Nav_* GameObject and assign both sprites in the Inspector.
    /// Call SetSelected(true/false) from FloorMapScreen whenever the active tab changes.
    [RequireComponent(typeof(Image))]
    public class NavTabButton : MonoBehaviour
    {
        [Header("Sprites")]
        public Sprite activeSprite;
        public Sprite inactiveSprite;

        Image _image;

        void Awake()
        {
            _image = GetComponent<Image>();
        }

        public void SetSelected(bool selected)
        {
            if (_image == null) _image = GetComponent<Image>();
            _image.sprite = selected ? activeSprite : inactiveSprite;
        }
    }
}
