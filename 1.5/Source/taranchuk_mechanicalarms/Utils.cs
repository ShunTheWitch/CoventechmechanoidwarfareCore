namespace taranchuk_mechanicalarms
{
    public static class Utils
    {
        public static float AngleAdjusted(this float angle)
        {
            if (angle > 360f)
            {
                angle -= 360f;
            }
            if (angle < 0f)
            {
                angle += 360f;
            }
            return angle;
        }
    }
}
