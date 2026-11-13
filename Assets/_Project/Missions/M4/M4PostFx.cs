using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastSignal.M4
{
    /// <summary>
    /// Scene-local post-processing on top of the project's Global Volume (priority 10, only overrides what it sets):
    /// a warm, slightly desaturated surface grade, a vignette, light chromatic aberration,
    /// and a Bokeh depth of field that is switched on only for cinematic shots.
    /// </summary>
    public class M4PostFx : MonoBehaviour
    {
        Volume _volume;
        VolumeProfile _profile;
        DepthOfField _dof;
        Vignette _vig;
        ColorAdjustments _grade;

        public static M4PostFx Create(Transform parent)
        {
            var go = new GameObject("M4 Post FX");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<M4PostFx>();
            fx._profile = ScriptableObject.CreateInstance<VolumeProfile>();
            fx._profile.name = "M4 Runtime Profile";

            fx._grade = fx._profile.Add<ColorAdjustments>(true);
            fx._grade.postExposure.Override(0.1f);
            fx._grade.contrast.Override(12f);
            fx._grade.saturation.Override(-8f);
            fx._grade.colorFilter.Override(new Color(1f, 0.96f, 0.91f));

            fx._vig = fx._profile.Add<Vignette>(true);
            fx._vig.intensity.Override(0.24f);
            fx._vig.smoothness.Override(0.5f);

            var ca = fx._profile.Add<ChromaticAberration>(true);
            ca.intensity.Override(0.05f);

            fx._dof = fx._profile.Add<DepthOfField>(true);
            fx._dof.mode.Override(DepthOfFieldMode.Bokeh);
            fx._dof.focusDistance.Override(6f);
            fx._dof.focalLength.Override(85f);
            fx._dof.aperture.Override(4f);
            fx._dof.active = false;

            fx._volume = go.AddComponent<Volume>();
            fx._volume.isGlobal = true;
            fx._volume.priority = 10f;
            fx._volume.sharedProfile = fx._profile;
            fx._volume.enabled = false;
            return fx;
        }

        public void SetSurface(bool on) { if (_volume != null) _volume.enabled = on; }

        public void Cinematic(bool on, float focusDistance = 6f)
        {
            if (_dof == null) return;
            _dof.active = on;
            _dof.focusDistance.value = focusDistance;
            _vig.intensity.value = on ? 0.36f : 0.24f;
        }

        public void Focus(float distance) { if (_dof != null) _dof.focusDistance.value = distance; }

        /// <summary>Final sequence: warmer, a touch brighter, for the sunset.</summary>
        public void Dusk(float k)
        {
            if (_grade == null) return;
            _grade.colorFilter.value = Color.Lerp(new Color(1f, 0.96f, 0.91f), new Color(1f, 0.86f, 0.74f), k);
            _grade.postExposure.value = Mathf.Lerp(0.1f, 0.3f, k);
        }

        void OnDestroy() { if (_profile != null) Destroy(_profile); }
    }
}
