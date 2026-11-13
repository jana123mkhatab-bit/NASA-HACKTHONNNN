using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LastSignal
{
    [RequireComponent(typeof(EventSystem))]
    public sealed class RuntimePointerInput : MonoBehaviour
    {
        EventSystem _eventSystem;
        GameObject _pressed;
        PointerEventData _data;

        void Awake()
        {
            _eventSystem = GetComponent<EventSystem>();
            _data = new PointerEventData(_eventSystem) { button = PointerEventData.InputButton.Left };
        }

        void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            _data.position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                _pressed = RaycastTarget();
                if (_pressed != null) ExecuteEvents.Execute(_pressed, _data, ExecuteEvents.pointerDownHandler);
            }

            if (!mouse.leftButton.wasReleasedThisFrame) return;

            GameObject released = RaycastTarget();
            if (_pressed != null)
            {
                ExecuteEvents.Execute(_pressed, _data, ExecuteEvents.pointerUpHandler);
                if (_pressed == released)
                    ExecuteEvents.Execute(_pressed, _data, ExecuteEvents.pointerClickHandler);
            }
            _pressed = null;
        }

        GameObject RaycastTarget()
        {
            var results = new System.Collections.Generic.List<RaycastResult>();
            _eventSystem.RaycastAll(_data, results);
            for (int i = 0; i < results.Count; i++)
            {
                var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(results[i].gameObject);
                if (target != null) return target;
            }
            return null;
        }
    }
}
