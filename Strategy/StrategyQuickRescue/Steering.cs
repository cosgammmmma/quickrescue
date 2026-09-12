namespace URWPGSim2D.Strategy
{
    using System;

    /// <summary>
    /// Heading/turn math for steering a fish along a path.
    /// Angles are radians on the X/Z plane, +X axis = 0.
    /// </summary>
    public static class Steering
    {
        /// <summary>Angle of the segment a-&gt;b in radians, in (-PI, PI].</summary>
        public static double SegmentAngle(Point2D a, Point2D b)
        {
            return Math.Atan2(b.Z - a.Z, b.X - a.X);
        }

        /// <summary>Normalizes an angle to (-PI, PI]. Loop-based, safe for huge inputs (e.g. +/-100*PI).</summary>
        public static double FormatAngle(double a)
        {
            while (a > Math.PI)
            {
                a -= 2.0 * Math.PI;
            }
            while (a <= -Math.PI)
            {
                a += 2.0 * Math.PI;
            }
            return a;
        }

        /// <summary>
        /// Turning angle threshold for straight-ahead: |delta| &lt;= 10°. Prevents micro-oscillation when aligned.
        /// </summary>
        private const double StraightBandRad = 10.0 * Math.PI / 180.0;

        /// <summary>Full-turn angle threshold: |delta| &gt; 60° → VCode 6.</summary>
        private const double FullTurnDeg = 60.0;

        /// <summary>
        /// Turn code (0..15, 7 = straight, 0 = full-left, 15 = full-right).
        ///   |delta| &lt;= 10°  → 7 (straight)
        ///   delta &gt; 0         → 15 (full-right)
        ///   delta &lt; 0         → 0  (full-left)
        /// All turning uses only extremes (0 or 15), bypassing any dead-zone issues.
        /// </summary>
        public static int GetTCode(double currentRad, double desiredRad)
        {
            double delta = FormatAngle(desiredRad - currentRad);
            if (Math.Abs(delta) <= StraightBandRad) return 7;
            return delta > 0.0 ? 15 : 0;
        }

        /// <summary>
        /// Speed code from a signed turn delta and the distance to destination (mm).
        ///   |delta| &gt; 60°  → 6  (slow while turning hard)
        ///   otherwise dist &lt;= 200  → 10
        ///   otherwise             → 14
        /// </summary>
        public static int AngleToVCode(double currentRad, double desiredRad, double distMm)
        {
            double deg = Math.Abs(FormatAngle(desiredRad - currentRad)) * 180.0 / Math.PI;
            if (deg > FullTurnDeg) return 6;
            return distMm <= 200.0 ? 10 : 14;
        }

        /// <summary>
        /// One-step navigation decision: computes both TCode and VCode.
        /// Used by FollowPath and direct approach for consistency.
        /// </summary>
        public static void ComputeNavigationDecision(double currentRad, double desiredRad,
            double distMm, out int tcode, out int vcode)
        {
            tcode = GetTCode(currentRad, desiredRad);
            vcode = AngleToVCode(currentRad, desiredRad, distMm);
        }

        /// <summary>
        /// Push-phase speed code — defaults to the same rules as navigation
        /// but exists as an extension point for later per-phase overrides.
        /// </summary>
        public static int PushPhaseVCode(double currentRad, double desiredRad, double distMm)
        {
            return AngleToVCode(currentRad, desiredRad, distMm);
        }
    }
}
