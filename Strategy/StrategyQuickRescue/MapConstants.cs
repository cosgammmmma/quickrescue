namespace URWPGSim2D.Strategy
{
    using System;

    /// <summary>2D point on the play field (X/Z plane, millimeters).</summary>
    public struct Point2D
    {
        public double X;
        public double Z;

        public Point2D(double x, double z)
        {
            this.X = x;
            this.Z = z;
        }

        public override string ToString()
        {
            return "(" + this.X + ", " + this.Z + ")";
        }
    }

    /// <summary>Axis-aligned bounding box of a rectangular obstacle (DirectionRad == 0).</summary>
    public struct ObstacleAabb
    {
        public double MinX;
        public double MaxX;
        public double MinZ;
        public double MaxZ;

        public ObstacleAabb(double minX, double maxX, double minZ, double maxZ)
        {
            this.MinX = minX;
            this.MaxX = maxX;
            this.MinZ = minZ;
            this.MaxZ = maxZ;
        }

        /// <summary>True when (x, z) lies inside the box expanded by inflationMm on all four sides.</summary>
        public bool Contains(double x, double z, double inflationMm)
        {
            return x >= this.MinX - inflationMm && x <= this.MaxX + inflationMm
                && z >= this.MinZ - inflationMm && z <= this.MaxZ + inflationMm;
        }
    }

    /// <summary>
    /// QuickRescue mission map constants and geometry primitives.
    /// Field bounds and the 5 obstacle AABBs are fixed by the mission definition.
    /// </summary>
    public static class MapConstants
    {
        /// <summary>Field left edge, mm.</summary>
        public const double LeftMm = -2250.0;

        /// <summary>Field right edge, mm.</summary>
        public const double RightMm = 2250.0;

        /// <summary>Field top edge, mm.</summary>
        public const double TopMm = -1500.0;

        /// <summary>Field bottom edge, mm.</summary>
        public const double BottomMm = 1500.0;

        /// <summary>The 5 static obstacle AABBs (DirectionRad == 0, axis-aligned).</summary>
        public static readonly ObstacleAabb[] Obstacles = new ObstacleAabb[]
        {
            new ObstacleAabb(-50.0, 50.0, -550.0, 550.0),       // obs1 center pillar
            new ObstacleAabb(500.0, 600.0, 400.0, 1500.0),      // obs2 right bottom
            new ObstacleAabb(-600.0, -500.0, 400.0, 1500.0),    // obs3 left bottom
            new ObstacleAabb(500.0, 600.0, -1500.0, -400.0),    // obs4 right top
            new ObstacleAabb(-600.0, -500.0, -1500.0, -400.0),  // obs5 left top
        };

        /// <summary>2D Euclidean distance over X/Z only.</summary>
        public static double Distance(Point2D p1, Point2D p2)
        {
            double dx = p2.X - p1.X;
            double dz = p2.Z - p1.Z;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// Speed code from remaining distance per the user spec: (int)(distMm / 5.0),
        /// clamped to [0, 14] (plan: "use the explicit dist/5 formula"; acceptance asserts
        /// GetVCode(70) == 14 and GetVCode(10) == 2). This is a pure map function and is
        /// intentionally NOT tuned for simulator dynamics - the Todo 6 empirical approach
        /// law lives in PathExecutor.ApproachVCode, which the executor calls when
        /// approaching a waypoint or push point.
        /// </summary>
        public static int GetVCode(double distMm)
        {
            int v = (int)(distMm / 5.0);
            if (v < 0)
            {
                v = 0;
            }
            if (v > 14)
            {
                v = 14;
            }
            return v;
        }

        /// <summary>
        /// True when (x, z) is inside the field shrunk by inflationMm and NOT inside any
        /// obstacle AABB expanded by inflationMm on all four sides.
        /// </summary>
        public static bool IsPointValid(double x, double z, double inflationMm)
        {
            if (x < LeftMm + inflationMm || x > RightMm - inflationMm
                || z < TopMm + inflationMm || z > BottomMm - inflationMm)
            {
                return false;
            }
            for (int i = 0; i < Obstacles.Length; i++)
            {
                if (Obstacles[i].Contains(x, z, inflationMm))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
