namespace ChaosArena.Platform
{
    /// <summary>
    /// Platform-neutral calculations used by the mobile controls. Keeping them
    /// independent from Unity UI makes the edge cases cheap to test.
    /// </summary>
    public static class MobileControlMath
    {
        public static MobileStickVector NormalizeStick(float x, float y, float deadZone = 0.12f)
        {
            var magnitudeSquared = x * x + y * y;
            if (magnitudeSquared <= deadZone * deadZone)
                return MobileStickVector.Zero;

            if (magnitudeSquared <= 1f)
                return new MobileStickVector(x, y);

            var inverseMagnitude = 1f / System.MathF.Sqrt(magnitudeSquared);
            return new MobileStickVector(x * inverseMagnitude, y * inverseMagnitude);
        }

        public static MobileSafeArea ToAnchors(float safeX, float safeY, float safeWidth, float safeHeight,
            float screenWidth, float screenHeight)
        {
            if (screenWidth <= 0f || screenHeight <= 0f)
                return MobileSafeArea.FullScreen;

            return new MobileSafeArea(
                Clamp01(safeX / screenWidth),
                Clamp01(safeY / screenHeight),
                Clamp01((safeX + safeWidth) / screenWidth),
                Clamp01((safeY + safeHeight) / screenHeight));
        }

        /// <summary>
        /// A touchscreen alone does not make a desktop browser a mobile runtime.
        /// In WebGL the Yandex SDK device type is the authoritative signal.
        /// </summary>
        public static bool IsTouchRuntime(bool isMobilePlatform, bool isHandheld, string sdkDeviceType) =>
            isMobilePlatform || isHandheld || IsTouchDeviceType(sdkDeviceType);

        public static bool IsTouchDeviceType(string sdkDeviceType) =>
            string.Equals(sdkDeviceType, "mobile", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(sdkDeviceType, "tablet", System.StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// CanvasScaler with Match Width Or Height = 0.5 scales controls by the
        /// geometric mean of the width and height ratios.
        /// </summary>
        public static float GetBalancedCanvasScale(float screenWidth, float screenHeight,
            float referenceWidth = 1920f, float referenceHeight = 1080f)
        {
            if (screenWidth <= 0f || screenHeight <= 0f || referenceWidth <= 0f || referenceHeight <= 0f)
                return 0f;

            return System.MathF.Sqrt((screenWidth / referenceWidth) * (screenHeight / referenceHeight));
        }

        /// <summary>
        /// Keeps an already acquired target until another candidate is materially
        /// closer. This avoids rapid target swaps when enemies are adjacent.
        /// </summary>
        public static bool ShouldSwitchAutoAimTarget(float currentDistanceSquared, float candidateDistanceSquared,
            float switchDistanceRatio = 0.85f)
        {
            if (currentDistanceSquared < 0f)
                return true;
            if (candidateDistanceSquared < 0f)
                return false;

            var ratio = Clamp01(switchDistanceRatio);
            return candidateDistanceSquared < currentDistanceSquared * ratio * ratio;
        }

        /// <summary>
        /// Mobile dash accepts the live stick direction, or a very recent last
        /// non-zero direction after a player releases the stick to tap Dash.
        /// </summary>
        public static MobileStickVector ResolveDashDirection(float currentX, float currentY,
            float lastX, float lastY, float lastNonZeroTime, float now, float maxAge = 0.45f)
        {
            var current = NormalizeStick(currentX, currentY);
            if (current.X != 0f || current.Y != 0f)
                return current;

            if (maxAge < 0f || now - lastNonZeroTime > maxAge)
                return MobileStickVector.Zero;

            return NormalizeStick(lastX, lastY);
        }

        public static MobileHandPlacement GetHandPlacement(MobileHand hand)
        {
            return hand == MobileHand.Left
                ? new MobileHandPlacement(0f, 1f)
                : new MobileHandPlacement(1f, -1f);
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }

    public readonly struct MobileStickVector
    {
        public static readonly MobileStickVector Zero = new(0f, 0f);

        public MobileStickVector(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }
        public float Y { get; }
    }

    public readonly struct MobileSafeArea
    {
        public static readonly MobileSafeArea FullScreen = new(0f, 0f, 1f, 1f);

        public MobileSafeArea(float minX, float minY, float maxX, float maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public float MinX { get; }
        public float MinY { get; }
        public float MaxX { get; }
        public float MaxY { get; }
    }

    public readonly struct MobileHandPlacement
    {
        public MobileHandPlacement(float anchorX, float horizontalSign)
        {
            AnchorX = anchorX;
            HorizontalSign = horizontalSign;
        }

        public float AnchorX { get; }
        public float HorizontalSign { get; }
    }
}
