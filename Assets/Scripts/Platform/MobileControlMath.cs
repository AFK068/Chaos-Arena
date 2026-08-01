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

        public static bool IsTouchRuntime(bool isMobilePlatform, bool isHandheld, bool hasTouchscreen) =>
            isMobilePlatform || isHandheld || hasTouchscreen;

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
}
