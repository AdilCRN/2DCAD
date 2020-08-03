using System;
using System.Collections.Generic;
using System.Linq;
using MSolvLib.MarkGeometry;
using System.Text;
using System.Threading.Tasks;
using MSolvLib.Classes;
using System.Drawing;
using System.Windows.Media;
using Color = System.Drawing.Color;
using System.Threading;

namespace MarkGeometriesLib.Classes.MarkGeometries.Classes.Helpers
{
    public class MVectorToGeometryConverter
    {
        /// <summary>
        /// Converts a list of mark vectors to an array of mark geometries. Ignores 3D mark vectors
        /// </summary>
        /// <param name="markVectors"></param>
        /// <returns></returns>
        public static Task<IMarkGeometry[]> ConvertMarkVectors(List<MarkVector> markVectors, CancellationToken ct)
        {
            List<IMarkGeometry> geometries = new List<IMarkGeometry>();
            for (int i = 0; i < markVectors.Count; i++)
            {
                if (ct.IsCancellationRequested)
                    throw new TaskCanceledException();

                var vec = markVectors[i];
                switch (vec.type)
                {
                    case MarkVector.VectorType.LINE:
                        geometries.Add(new MarkGeometryLine(vec.x_start, vec.y_start, vec.x_end, vec.y_end));
                        break;
                    case MarkVector.VectorType.ARC:
                        geometries.Add(new MarkGeometryArc(vec.x_center, vec.y_center, vec.radius, vec.start_angle, vec.end_angle));
                        break;
                    case MarkVector.VectorType.CIRCLE:
                        geometries.Add(new MarkGeometryCircle(new MarkGeometryPoint(vec.x_center, vec.y_center), vec.radius));
                        break;
                    case MarkVector.VectorType.POINT:
                        geometries.Add(new MarkGeometryPoint(vec.x_center, vec.y_center));
                        break;
                }
            }
            return Task.FromResult(geometries.ToArray());
        }


        public static Task<IMarkGeometry[]> ConvertVectorGroups(List<VectorGroup> vectorGroups, CancellationToken ct)
        {
            List<IMarkGeometry> geometries = new List<IMarkGeometry>();
            for (int i = 0; i < vectorGroups.Count; i++)
            {
                if (ct.IsCancellationRequested)
                    throw new TaskCanceledException();

                var vectors = vectorGroups[i];

                for (int j = 0; j < vectors.Pattern.Count; j++)
                {
                    var vec = vectors.Pattern[j];

                    SolidColorBrush b = (SolidColorBrush)vectors.DisplayColor;
                    Color stroke = Color.FromArgb(b.Color.A, b.Color.B, b.Color.G, b.Color.R);
                    switch (vec.type)
                    {
                        case MarkVector.VectorType.LINE:
                            geometries.Add(new MarkGeometryLine(vec.x_start, vec.y_start, vec.x_end, vec.y_end) { Stroke = stroke});
                            break;
                        case MarkVector.VectorType.ARC:
                            geometries.Add(new MarkGeometryArc(vec.x_center, vec.y_center, vec.radius, vec.start_angle, vec.end_angle) { Stroke = stroke });
                            break;
                        case MarkVector.VectorType.CIRCLE:
                            geometries.Add(new MarkGeometryCircle(new MarkGeometryPoint(vec.x_center, vec.y_center), vec.radius) { Stroke = stroke });
                            break;
                        case MarkVector.VectorType.POINT:
                            geometries.Add(new MarkGeometryPoint(vec.x_center, vec.y_center) { Stroke = stroke });
                            break;
                    }
                }
            }
            return Task.FromResult(geometries.ToArray());
        }

    }
}
