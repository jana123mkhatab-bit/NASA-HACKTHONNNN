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
            bool pressed;
            bool released;
            Vector2 position;
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                position = mouse.position.ReadValue();
                pressed = mouse.leftButton.wasPressedThisFrame;
                released = mouse.leftButton.wasReleasedThisFrame;
            }
            else
            {
                position = Input.mousePosition;
                pressed = Input.GetMouseButtonDown(0);
                released = Input.GetMouseButtonUp(0);
            }
            _data.position = position;

            if (pressed)
            {
                _pressed = RaycastTarget();
                if (_pressed != null)
                {
                    _data.pointerPress = _pressed;
                    _data.rawPointerPress = _pressed;
                    ExecuteEvents.ExecuteHierarchy(_pressed, _data, ExecuteEvents.pointerDownHandler);
                }
            }

            if (!released) return;

            GameObject releasedTarget = RaycastTarget();
            if (_pressed != null)
            {
                ExecuteEvents.ExecuteHierarchy(_pressed, _data, ExecuteEvents.pointerUpHandler);
                if (_pressed == releasedTarget)
                    ExecuteEvents.ExecuteHierarchy(_pressed, _data, ExecuteEvents.pointerClickHandler);
            }
            _data.pointerPress = null;
            _data.rawPointerPress = null;
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
