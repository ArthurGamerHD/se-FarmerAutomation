using VRageMath;

namespace SE_Farm_Automation.Extensions
{
    public static class MatrixExtensions
    {
        public static BoundingBoxD ToBoundingBox(this MatrixD matrix)
        {
            var unityBox = new BoundingBoxD(-Vector3D.Half, Vector3D.Half);
            
            var min = Vector3D.PositiveInfinity;
            var max = Vector3D.NegativeInfinity;

            for (var index = 0; index < 8; index++)
            {
                var corner = unityBox.GetCorner(index);
                var transformed = Vector3D.Transform(corner, matrix);
                min = Vector3D.Min(min, transformed);
                max = Vector3D.Max(max, transformed);
            }

            return new BoundingBoxD(min, max);
        }
    }
}