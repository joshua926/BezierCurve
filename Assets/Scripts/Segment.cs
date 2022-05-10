using Unity.Mathematics;

namespace BezierCurve
{
    public readonly struct Segment
    {
        public readonly float3x4 points;

        public Segment(float3 p0, float3 p1, float3 p2, float3 p3)
        {
            points = new float3x4(p0, p1, p2, p3);
        }

        public Segment(in float3x4 points)
        {
            this.points = points;
        }

        public float3 Position(float t)
        {
            float3 a = math.lerp(points[0], points[1], t);
            float3 b = math.lerp(points[1], points[2], t);
            float3 c = math.lerp(points[2], points[3], t);
            float3 d = math.lerp(a, b, t);
            float3 e = math.lerp(b, c, t);
            return math.lerp(d, e, t);
        }

        public float3 Tangent(float t)
        {
            float3x3 a = new float3x3(
                points[1] - points[0],
                points[2] - points[1],
                points[3] - points[2]) * 3;
            float3x2 b = new float3x2(
                math.lerp(a[0], a[1], t),
                math.lerp(a[1], a[2], t));
            return math.lerp(b[0], b[1], t);
        }

        public float3 Acceleration(float t)
        {
            float3x3 a = new float3x3(
                points[1] - points[0],
                points[2] - points[1],
                points[3] - points[2]) * 3;
            float3x2 b = new float3x2(
                a[1] - a[0],
                a[2] - a[1]) * 2;
            return math.lerp(b[0], b[1], t);
        }

        public float3 Jerk()
        {
            float3x3 a = new float3x3(
                points[1] - points[0],
                points[2] - points[1],
                points[3] - points[2]) * 3;
            float3x2 b = new float3x2(
                a[1] - a[0],
                a[2] - a[1]) * 2;
            return b[1] - b[0];
        }

        public float3 ProjectRay(in Ray ray, int iterations, out float rayDistance, out float time)
        {
            (float5 times, float5 distances) = GetInitialTimesAndDistances(ray);
            int i = 0;
            do
            {
                CalculateInBetweenValues(ref distances, ref times, ray);
                ZoomIn(ref distances, ref times);
                i++;
            } while (i < iterations);
            int indexOfMinDistance = GetIndexOfMinDistance(distances);
            rayDistance = math.sqrt(distances[indexOfMinDistance]);
            time = times[indexOfMinDistance];
            return Position(time);
        }

        (float5 times, float5 distances) GetInitialTimesAndDistances(in Ray ray)
        {
            float5 times = default;
            times[0] = 0;
            times[2] = .5f;
            times[4] = 1;
            float5 distances = default;
            distances[0] = ray.Distancesq(points[0]);
            distances[2] = ray.Distancesq(Position(times[2]));
            distances[4] = ray.Distancesq(points[3]);
            return (times, distances);
        }

        void CalculateInBetweenValues(ref float5 times, ref float5 distances, in Ray ray)
        {
            times[1] = (times[0] + times[2]) * .5f;
            times[3] = (times[2] + times[4]) * .5f;
            distances[1] = ray.Distancesq(Position(times[1]));
            distances[3] = ray.Distancesq(Position(times[3]));
        }

        int GetIndexOfMinDistance(in float5 distances)
        {
            int indexOfMin = 2;
            indexOfMin = distances[0] < distances[indexOfMin] ? 0 : indexOfMin;
            indexOfMin = distances[1] < distances[indexOfMin] ? 0 : indexOfMin;
            indexOfMin = distances[3] < distances[indexOfMin] ? 0 : indexOfMin;
            indexOfMin = distances[4] < distances[indexOfMin] ? 0 : indexOfMin;
            return indexOfMin;
        }

        void ZoomIn(ref float5 times, ref float5 distances)
        {
            int centerIndex = math.clamp(GetIndexOfMinDistance(distances), 1, 3);
            distances[0] = distances[centerIndex - 1];
            distances[4] = distances[centerIndex + 1];
            distances[2] = distances[centerIndex];
            times[0] = times[centerIndex - 1];
            times[4] = times[centerIndex + 1];
            times[2] = times[2];
        }

        unsafe struct float5
        {
            fixed float f[5];
            public float this[int i]
            {
                get
                {
                    i = math.clamp(i, 0, 4);
                    return f[i];
                }
                set
                {
                    i = math.clamp(i, 0, 4);
                    f[i] = value;
                }
            }
        }
    }
}