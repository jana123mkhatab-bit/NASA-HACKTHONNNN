using UnityEngine;

namespace LastSignal.Ship
{
    /// <summary>Turns the landing-leg collider on and off with the gear (M4 landing).</summary>
    [RequireComponent(typeof(ShipController))]
    public class GearColliderToggle : MonoBehaviour
    {
        public Collider legCollider;
        ShipController _ship;

        void Awake()
        {
            _ship = GetComponent<ShipController>();
            _ship.GearChanged += OnGear;
            if (legCollider != null) legCollider.enabled = _ship.GearDown;
        }

        void OnDestroy()
        {
            if (_ship != null) _ship.GearChanged -= OnGear;
        }

        void OnGear(bool down)
        {
            if (legCollider != null) legCollider.enabled = down;
        }
    }
}
