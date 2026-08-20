namespace UPnL.SignalRush.World
{
    public static class JumpReachability
    {
        public static float MaxHeight(float jumpVelocity, float gravityMagnitude)
        {
            return IsValidBallisticInput(jumpVelocity, gravityMagnitude) ? jumpVelocity * jumpVelocity / (2f * gravityMagnitude) : 0f;
        }

        public static float AirTime(float jumpVelocity, float gravityMagnitude, float fallGravityMultiplier)
        {
            return IsValidBallisticInput(jumpVelocity, gravityMagnitude) && fallGravityMultiplier > 0f
                ? jumpVelocity / gravityMagnitude + (float)System.Math.Sqrt(2f * MaxHeight(jumpVelocity, gravityMagnitude) / (gravityMagnitude * fallGravityMultiplier))
                : 0f;
        }

        public static float MaxGap(float horizontalSpeed, float jumpVelocity, float gravityMagnitude, float fallGravityMultiplier)
        {
            return horizontalSpeed > 0f ? horizontalSpeed * AirTime(jumpVelocity, gravityMagnitude, fallGravityMultiplier) : 0f;
        }

        private static bool IsValidBallisticInput(float jumpVelocity, float gravityMagnitude)
        {
            return jumpVelocity > 0f && gravityMagnitude > 0f;
        }
    }
}
