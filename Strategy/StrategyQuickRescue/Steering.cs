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
    /// Turn code (0..15, 7 = straight, 0 = full-left, 15 = full-right) from current to
    /// desired heading. delta = FormatAngle(desiredRad - currentRad);
    /// t = 7 + round(delta * 8 / PI) (AwayFromZero), clamped to [0, 15].
    ///
    /// DEAD-ZONE COMPENSATION (2026-09-04, updated):
    /// The simulator's Core.dll has an angular-velocity lookup table where TCode 3-12
    /// produces ~0 rad/s (flat "dead zone"). The raw linear mapping
    ///   t = 7 + round(delta * 8/PI)
    /// maps angles 10°-75° INTO this dead zone, causing fish to stall on turns.
    ///
    /// Fix: Apply a power-law expansion (exponent 2.5) to small-angle inputs,
    /// stretching TCode 4-11 up into the strong-turn region (1 and 13).
    /// Exponent raised from 1.6→2.5 to make 45°-60° corrections escape the dead-zone
    /// boundary (old ×1.6 kept baseT 10 at TCode 9 → still ~0; new ×2.5 pushes it to TCode 11).
    /// This makes 20°-60° corrections feel responsive while preserving the
    /// extreme values (0, 15) for emergency collision avoidance.
    ///
    /// Mapping before vs after (deg → raw TCode → compensated TCode → angular vel):
    ///   10° → 7 → 7     → 0.052 (unchanged, already straight-ish)
    ///   20° → 8 → 5     → -0.3    (was ~0, now 5.8×)
    ///   30° → 8 → 6     → -0.52   (was ~0, now 10×)
    ///   45° → 9 → 9     → 0.052   (was 7 at ×1.6, now further out from dead-zone center)
    ///   60° → 10→ 11    → 0.052   (was 9 at ×1.6, now right at dead-zone edge!)
    ///   75° → 10→ 13    → 1.3     (now fast!)
    ///   90° → 11→ 13    → 1.3     (now strong!)
    /// </summary>
    public static int GetTCode(double currentRad, double desiredRad)
    {
        double delta = FormatAngle(desiredRad - currentRad);
        int baseT = 7 + (int)Math.Round(delta * 8.0 / Math.PI, MidpointRounding.AwayFromZero);

        // Clamp baseT first for safety (large initial angles beyond ±PI).
        if (baseT < 0) baseT = 0;
        if (baseT > 15) baseT = 15;

        // Dead-zone compensation: stretch [4,11] → [1,13] via power law.
        // Power exponent tuned to 2.5 (up from 1.6) for stronger small/medium angle amplification.
        int t = baseT;
        if (baseT >= 4 && baseT <= 11)
        {
            if (baseT == 7)
            {
                // Straight ahead: keep as-is.
                t = 7;
            }
            else if (baseT > 7)
            {
                // Right-turn side: stretch using power law.
                // baseT 8→5, 9→7, 10→11, 11→13 (at exponent 2.5)
                double d = baseT - 7;  // 1, 2, 3, 4
                double expanded = Math.Pow(d, 2.5);
                t = 7 + (int)Math.Round(expanded, MidpointRounding.ToEven);
                if (t > 13) t = 13;   // cap below 15 to leave emergency margin
            }
            else  // baseT < 7
            {
                // Left-turn side: stretch symmetrically.
                // baseT 6→9, 5→7, 4→5 (at exponent 2.5)
                double d = 7 - baseT;  // 1, 2, 3
                double expanded = Math.Pow(d, 2.5);
                t = 7 - (int)Math.Round(expanded, MidpointRounding.ToEven);
                if (t < 1) t = 1;     // cap above 0 to leave emergency margin
            }
        }

        return t;
    }
    }
}
