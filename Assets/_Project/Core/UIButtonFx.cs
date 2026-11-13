using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Button feel: a small scale response and a sound on hover and press. Added by UIFactory.Button.</summary>
    public class UIButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public bool silent;
        Selectable _selectable;
        float _target = 1f, _current = 1f;
        bool _over;

        void Awake() { _selectable = GetComponent<Selectable>(); }

        bool Live => _selectable == null || _selectable.interactable;

        public void OnPointerEnter(PointerEventData e)
        {
            _over = true;
            if (!Live) return;
            _target = 1.03f;
            if (!silent) Sfx.Play(Sfx.Id.Hover);
        }

        public void OnPointerExit(PointerEventData e) { _over = false; _target = 1f; }

        public void OnPointerDown(PointerEventData e)
        {
            if (!Live) { if (!silent) Sfx.Play(Sfx.Id.Deny); return; }
            _target = 0.96f;
            if (!silent) Sfx.Play(Sfx.Id.Click);
        }

        public void OnPointerUp(PointerEventData e) { _target = _over && Live ? 1.03f : 1f; }

        void OnDisable() { _current = _target = 1f; transform.localScale = Vector3.one; _over = false; }

        void Update()
        {
            if (Mathf.Approximately(_current, _target)) return;
            _current = Mathf.MoveTowards(_current, _target, Time.unscaledDeltaTime * 0.9f);
            transform.localScale = new Vector3(_current, _current, 1f);
        }
    }
}
