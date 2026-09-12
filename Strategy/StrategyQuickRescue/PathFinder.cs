namespace URWPGSim2D.Strategy
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Path planning around the field obstacles.
    /// 8-connected A* shortest path over a uniform grid with body-size obstacle inflation,
    /// followed by collinear waypoint compression.
    /// </summary>
    public static class PathFinder
    {
        /// <summary>Grid cell size in millimeters.</summary>
        private const double GridStepMm = 40.0;

        /// <summary>Epsilon for the collinearity cross-product test.</summary>
        private const double CollinearEpsilon = 1e-9;

        // Eight neighbour offsets: 4 cardinal + 4 diagonal.
        // Diagonals cannot cut through blocked corners (checked at call site).
        private static readonly int[] DiI  = { -1, 1, 0, 0, -1, -1, 1, 1 };
        private static readonly int[] DiJ  = {  0, 0,-1, 1,-1,  1, 1,-1};
        private static readonly double[] DijCost = { 1.0, 1.0, 1.0, 1.0, 1.4142135623730951, 1.4142135623730951, 1.4142135623730951, 1.4142135623730951 };

        /// <summary>Node for the binary-min-heap priority queue: cell index + estimated f-cost.</summary>
        private struct HeapNode
        {
            public int Cell;
            public double F;
            public HeapNode(int cell, double f) { Cell = cell; F = f; }
        }

        // Grid convention (deterministic): cell (i, j) covers
        // X [LeftMm + i*step, LeftMm + (i+1)*step) and Z [TopMm + j*step, TopMm + (j+1)*step);
        // its representative point is the cell center
        // (LeftMm + (i + 0.5)*step, TopMm + (j + 0.5)*step).
        // Cell count per axis is floor(fieldSize / step), so a slim strip on the
        // right/bottom field edge has no cells; points there snap to the nearest cell.
        private static readonly int GridNx = (int)((MapConstants.RightMm - MapConstants.LeftMm) / GridStepMm);
        private static readonly int GridNz = (int)((MapConstants.BottomMm - MapConstants.TopMm) / GridStepMm);

        /// <summary>
        /// Finds a waypoint path from start to target avoiding obstacles inflated by inflationMm.
        /// Returns false (empty list) when start/target is invalid or the target is unreachable.
        /// </summary>
        public static bool FindPath(Point2D start, Point2D target, double inflationMm, out List<Point2D> waypoints)
        {
            int rawCellCount;
            return FindPath(start, target, inflationMm, out waypoints, out rawCellCount);
        }

        /// <summary>
        /// Test/diagnostics hook: identical to the 4-argument overload, additionally reporting
        /// rawCellCount = number of grid cells in the uncompressed A* path (0 on failure).
        /// Lets probes verify that compression actually shrinks the path.
        /// </summary>
        public static bool FindPath(Point2D start, Point2D target, double inflationMm, out List<Point2D> waypoints, out int rawCellCount)
        {
            waypoints = new List<Point2D>();
            rawCellCount = 0;

            if (!MapConstants.IsPointValid(start.X, start.Z, inflationMm)
                || !MapConstants.IsPointValid(target.X, target.Z, inflationMm))
            {
                return false;
            }

            bool[] walkable = BuildWalkabilityGrid(inflationMm);
            int startCell = SnapToWalkableCell(start, walkable);
            int targetCell = SnapToWalkableCell(target, walkable);
            if (startCell < 0 || targetCell < 0)
            {
                return false;
            }

            int[] parent = AstarSearch(walkable, startCell, targetCell);
            if (parent == null)
            {
                return false;
            }

            List<int> cells = ReconstructCellPath(parent, startCell, targetCell);
            rawCellCount = cells.Count;

            List<Point2D> raw = new List<Point2D>();
            raw.Add(start);
            for (int k = 0; k < cells.Count; k++)
            {
                raw.Add(CellCenter(cells[k]));
            }
            raw.Add(target);

            waypoints = CompressCollinear(raw);
            return true;
        }

        /// <summary>Cell walkable iff its center is valid under the given inflation.</summary>
        private static bool[] BuildWalkabilityGrid(double inflationMm)
        {
            bool[] walkable = new bool[GridNx * GridNz];
            for (int j = 0; j < GridNz; j++)
            {
                for (int i = 0; i < GridNx; i++)
                {
                    double cx = MapConstants.LeftMm + (i + 0.5) * GridStepMm;
                    double cz = MapConstants.TopMm + (j + 0.5) * GridStepMm;
                    walkable[j * GridNx + i] = MapConstants.IsPointValid(cx, cz, inflationMm);
                }
            }
            return walkable;
        }

        /// <summary>
        /// Nearest cell to p (per-axis rounding, clamped to the grid); if that cell is not
        /// walkable, scans expanding Chebyshev rings in fixed (dz, dx) order and takes the
        /// first walkable cell. Returns -1 when no walkable cell exists at all.
        /// </summary>
        private static int SnapToWalkableCell(Point2D p, bool[] walkable)
        {
            int i = Clamp((int)Math.Round((p.X - MapConstants.LeftMm) / GridStepMm - 0.5), 0, GridNx - 1);
            int j = Clamp((int)Math.Round((p.Z - MapConstants.TopMm) / GridStepMm - 0.5), 0, GridNz - 1);
            if (walkable[j * GridNx + i])
            {
                return j * GridNx + i;
            }
            for (int r = 1; r < GridNx + GridNz; r++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dz)) != r)
                        {
                            continue;
                        }
                        int ni = i + dx;
                        int nj = j + dz;
                        if (ni < 0 || ni >= GridNx || nj < 0 || nj >= GridNz)
                        {
                            continue;
                        }
                        if (walkable[nj * GridNx + ni])
                        {
                            return nj * GridNx + ni;
                        }
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// 8-connected A* with octile-distance heuristic.
        /// Cardinal moves cost 1.0; diagonal moves cost sqrt(2). Diagonal steps cannot cut blocked corners.
        /// Returns the parent array (parent[startCell] == startCell) or null when unreachable.
        /// </summary>
        private static int[] AstarSearch(bool[] walkable, int startCell, int targetCell)
        {
            int total = walkable.Length;
            double[] gScore = new double[total];
            for (int k = 0; k < total; k++)
            {
                gScore[k] = double.MaxValue;
            }
            int[] parent = new int[total];
            for (int k = 0; k < total; k++)
            {
                parent[k] = -1;
            }
            bool[] closed = new bool[total];

            gScore[startCell] = 0.0;
            parent[startCell] = startCell;

            // Binary min-heap of HeapNode keyed by f-cost.
            var heap = new List<HeapNode>();
            heap.Add(new HeapNode(startCell, OctileDistance(startCell, targetCell)));

            while (heap.Count > 0)
            {
                // Extract min.
                SwapRef(heap, 0, heap.Count - 1);
                HeapNode curNode = heap[heap.Count - 1];
                heap.RemoveAt(heap.Count - 1);

                // FIX: Re-heapify the root element after extraction
                HeapDown(heap, 0); 

                int cur = curNode.Cell;

                if (closed[cur])
                {
                    continue;
                }
                closed[cur] = true;

                if (cur == targetCell)
                {
                    return parent;
                }

                int ci = cur % GridNx;
                int cj = cur / GridNx;

                for (int d = 0; d < 8; d++)
                {
                    int ni = ci + DiI[d];
                    int nj = cj + DiJ[d];
                    if (ni < 0 || ni >= GridNx || nj < 0 || nj >= GridNz)
                    {
                        continue;
                    }
                    int next = nj * GridNx + ni;
                    if (!walkable[next] || closed[next])
                    {
                        continue;
                    }

                    // No diagonal cutting: both cardinal neighbors must be walkable too.
                    if (d >= 4 && (!walkable[nj * GridNx + ci] || !walkable[cj * GridNx + ni]))
                    {
                        continue;
                    }

                    double tentativeG = gScore[cur] + DijCost[d];
                    if (tentativeG >= gScore[next])
                    {
                        continue;
                    }

                    gScore[next] = tentativeG;
                    parent[next] = cur;

                    double h = OctileDistance(next, targetCell);
                    double f = tentativeG + h;
                    int insertIdx = heap.Count;
                    heap.Add(new HeapNode(next, f));

                    // Bubble up to maintain heap invariant.
                    int idx = insertIdx;
                    while (idx > 0)
                    {
                        int parentIdx = (idx - 1) / 2;
                        if (heap[parentIdx].F <= heap[idx].F)
                        {
                            break;
                        }
                        SwapRef(heap, parentIdx, idx);
                        idx = parentIdx;
                    }
                }
            }
            return null; // Target not reachable.
        }

        /// <summary>Swap two elements in a List{&lt;HeapNode&gt;}.</summary>
        private static void SwapRef(List<HeapNode> list, int a, int b)
        {
            HeapNode tmp = list[a];
            list[a] = list[b];
            list[b] = tmp;
        }

        /// <summary>Bubble up a newly added element at `idx`.</summary>
        private static void HeapUp(List<HeapNode> heap, int idx)
        {
            while (idx > 0)
            {
                int parentIdx = (idx - 1) / 2;
                if (heap[parentIdx].F <= heap[idx].F) break;
                SwapRef(heap, parentIdx, idx);
                idx = parentIdx;
            }
        }

        /// <summary>Bubble down the root element after extraction to maintain heap invariant.</summary>
        private static void HeapDown(List<HeapNode> heap, int idx)
        {
            int size = heap.Count;
            while (true)
            {
                int leftChild = 2 * idx + 1;
                if (leftChild >= size) break;

                int rightChild = leftChild + 1;
                int bestChild = leftChild;
                if (rightChild < size && heap[rightChild].F < heap[leftChild].F)
                {
                    bestChild = rightChild;
                }

                if (heap[idx].F <= heap[bestChild].F) break;

                SwapRef(heap, idx, bestChild);
                idx = bestChild;
            }
        }

        /// <summary>Octile distance between two grid cells (admissible, consistent heuristic for 8-connected grids).</summary>
        private static double OctileDistance(int cellA, int cellB)
        {
            int aI = cellA % GridNx;
            int aJ = cellA / GridNx;
            int bI = cellB % GridNx;
            int bJ = cellB / GridNx;
            int dx = Math.Abs(aI - bI);
            int dy = Math.Abs(aJ - bJ);
            return (dx - dy) > 0 ? dx + 0.4142135623730951 * dy : dy + 0.4142135623730951 * dx;
        }

        /// <summary>Parent-chain walk from targetCell back to startCell, reversed to start-&gt;target order.</summary>
        private static List<int> ReconstructCellPath(int[] parent, int startCell, int targetCell)
        {
            List<int> cells = new List<int>();
            int cur = targetCell;
            while (cur != startCell)
            {
                cells.Add(cur);
                cur = parent[cur];
            }
            cells.Add(startCell);
            cells.Reverse();
            return cells;
        }

        /// <summary>Representative center point of a grid cell (see convention at the top).</summary>
        private static Point2D CellCenter(int cell)
        {
            int i = cell % GridNx;
            int j = cell / GridNx;
            return new Point2D(
                MapConstants.LeftMm + (i + 0.5) * GridStepMm,
                MapConstants.TopMm + (j + 0.5) * GridStepMm);
        }

        /// <summary>
        /// Drops middle points whose incoming/outgoing segments are collinear
        /// (2D cross product magnitude &lt;= epsilon, tested against the last kept point).
        /// Always keeps the first and the last point.
        /// </summary>
        private static List<Point2D> CompressCollinear(List<Point2D> raw)
        {
            List<Point2D> result = new List<Point2D>();
            if (raw.Count == 0)
            {
                return result;
            }
            result.Add(raw[0]);
            for (int k = 1; k < raw.Count - 1; k++)
            {
                Point2D a = result[result.Count - 1];
                Point2D b = raw[k];
                Point2D c = raw[k + 1];
                double cross = (b.X - a.X) * (c.Z - b.Z) - (b.Z - a.Z) * (c.X - b.X);
                if (Math.Abs(cross) > CollinearEpsilon)
                {
                    result.Add(b);
                }
            }
            if (raw.Count > 1)
            {
                result.Add(raw[raw.Count - 1]);
            }
            return result;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }
            if (value > max)
            {
                return max;
            }
            return value;
        }
    }
}
