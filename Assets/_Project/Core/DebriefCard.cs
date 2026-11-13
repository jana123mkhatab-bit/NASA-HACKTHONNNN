using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>
    /// A Debrief card shown after each mission: the real concept and the real NASA source behind it.
    /// Cards live in Assets/_Project/Core/Resources/Debrief/ and are named by mission id (M1, M2, M3, M4, Final, DesignMode).
    /// Person 6 owns the final wording and verifies every source.
    /// </summary>
    [CreateAssetMenu(menuName = "Last Signal/Debrief Card", fileName = "M1")]
    public class DebriefCard : ScriptableObject
    {
        public string missionId;
        public string title;
        [TextArea(2, 6)] public string concept;
        [TextArea(1, 4)] public string nasaSource;
        [TextArea(2, 8)] public string body;

        /// <summary>Loads the card for a mission, or builds a clearly marked placeholder if none exists.</summary>
        public static DebriefCard For(string missionId)
        {
            DebriefCard card = Resources.Load<DebriefCard>("Debrief/" + missionId);
            if (card != null) return card;

            card = CreateInstance<DebriefCard>();
            card.missionId = missionId;
            card.title = "Debrief: " + MissionIds.Title(missionId);
            card.concept = "TODO: write the concept this mission teaches.";
            card.nasaSource = "TODO: name the NASA source for this mission.";
            card.body = "No Debrief card asset found for " + missionId + ". Run the menu Last Signal > 1 to create the default cards.";
            card.hideFlags = HideFlags.DontSave;
            return card;
        }
    }
}
