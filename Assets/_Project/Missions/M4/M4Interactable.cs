using System;
using UnityEngine;

namespace LastSignal.M4
{
    /// <summary>Something the astronaut can use with E when close enough (registered with SurfaceWalker).</summary>
    public class M4Interactable : MonoBehaviour
    {
        public string prompt = "Use";
        public float radius = 4f;
        public Action onUse;
        public bool available = true;
    }
}
