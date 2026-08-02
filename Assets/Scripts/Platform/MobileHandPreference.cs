namespace ChaosArena.Platform
{
    public enum MobileHand
    {
        Left,
        Right
    }

    /// <summary>
    /// Small storage seam keeps the control preference testable without using
    /// PlayerPrefs in EditMode tests.
    /// </summary>
    public interface IMobileHandPreferenceStore
    {
        string GetString(string key, string defaultValue);
        void SetString(string key, string value);
    }

    public static class MobileHandPreference
    {
        public const string PlayerPrefsKey = "ChaosArena.Mobile.Hand";

        public static MobileHand Load(IMobileHandPreferenceStore store)
        {
            if (store == null)
                return MobileHand.Right;

            return store.GetString(PlayerPrefsKey, "right") == "left"
                ? MobileHand.Left
                : MobileHand.Right;
        }

        public static MobileHand Toggle(IMobileHandPreferenceStore store)
        {
            var next = Load(store) == MobileHand.Right ? MobileHand.Left : MobileHand.Right;
            store?.SetString(PlayerPrefsKey, next == MobileHand.Left ? "left" : "right");
            return next;
        }
    }
}
