using MSolvLib.Classes;
using MSolvLib.MarkGeometry;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkGeometriesLib.Classes.DXFParser
{
    public class DXFLoader
    {
        public static List<IMarkGeometry> LoadDXF(string filename, params string[] layerNames)
        {
            if (layerNames == null || layerNames.Length <= 0)
            {
                layerNames = new string[] { "All" };
            }

            var geometries = new List<IMarkGeometry>();
            var entities = DXFlibCS.CustomExtractVectorsInOrder(filename);

            foreach (var e in entities)
            {
                if (layerNames.Contains(e.Layer.Name) || layerNames.Contains("All"))
                {
                    if (e is netDxf.Entities.Point point)
                    {
                        geometries.Add(
                            new MarkGeometryPoint(point)
                            { 
                                LayerName = e.Layer.Name,
                                Stroke = e.Color.ToColor()
                            }
                        );
                    }
                    else if (e is netDxf.Entities.Spline spline)
                    {
                        geometries.Add(
                            new MarkGeometrySpline(spline)
                            {
                                LayerName = e.Layer.Name,
                                Stroke = e.Color.ToColor()
                            }
                        );
                    }
                    else if (e is netDxf.Entities.LwPolyline lwPLine)
                    {
                        geometries.Add(
                            new MarkGeometryPath(lwPLine)
                            {
                                LayerName = e.Layer.Name,
                                Stroke = e.Color.ToColor()
                            }
                        );
                    }
                    else if (e is netDxf.Entities.Polyline pLine)
                    {
                        geometries.Add(
                            new MarkGeometryPath(pLine)
                            {
                                LayerName = e.Layer.Name,
                                Stroke = e.Color.ToColor()
                            }
                        );
                    }
                    else if (e is netDxf.Entities.Line line)
                    {
                        geometries.Add(
                            new MarkGeometryLine(line)
                            {
                                LayerName = e.Layer.Name,
                                Stroke = e.Color.ToColor()
                            }
                        );
                    }
                    else if (e is netDxf.Entities.Circle circle)
                    {
                        geometries.Add(
                            new MarkGeometryCircle(circle)
                            {
                                LayerName = e.Layer.Name,
                                Stroke = e.Color.ToColor()
                            }
                        );
                    }
                    else if (e is netDxf.Entities.Arc arc)
                    {
                        geometries.Add(
                            new MarkGeometryArc(arc)
                            {
                                LayerName = e.Layer.Name,
                                Stroke = e.Color.ToColor()
                            }
                        );
                    }
                }
            }

            return geometries;
        }

        public static void BeginExtractGeometries(Action<IMarkGeometry> callback, string filename, params string[] layerNames)
        {
            if (layerNames == null || layerNames.Length <= 0)
            {
                layerNames = new string[] { "All" };
            }

            var entities = DXFlibCS.CustomExtractVectorsInOrder(filename);

            foreach (var e in entities)
            {
                if (layerNames.Contains(e.Layer.Name) || layerNames.Contains("All"))
                {
                    if (e is netDxf.Entities.Point point)
                    {
                        callback(new MarkGeometryPoint(point));
                    }
                    else if (e is netDxf.Entities.Spline spline)
                    {
                        callback(
                            new MarkGeometrySpline(spline)
                        );
                    }
                    else if (e is netDxf.Entities.LwPolyline lwPLine)
                    {
                        callback(
                            new MarkGeometryPath(lwPLine)
                        );
                    }
                    else if (e is netDxf.Entities.Polyline pLine)
                    {
                        callback(
                            new MarkGeometryPath(pLine)
                        );
                    }
                    else if (e is netDxf.Entities.Line line)
                    {
                        callback(
                            new MarkGeometryLine(line)
                        );
                    }
                    else if (e is netDxf.Entities.Circle circle)
                    {
                        callback(
                            new MarkGeometryCircle(circle)
                        );
                    }
                    else if (e is netDxf.Entities.Arc arc)
                    {
                        callback(
                            new MarkGeometryArc(arc)
                        );
                    }
                }
            }
        }

        public static List<IMarkGeometry> EntityToMarkGeometries(List<EntityObject> entities)
        {
            var geometries = new List<IMarkGeometry>();

            foreach (var e in entities)
            {
                if (e is netDxf.Entities.Point point)
                {
                    geometries.Add(
                        new MarkGeometryPoint(point)
                        {
                            LayerName = e.Layer.Name,
                            Stroke = e.Color.ToColor()
                        }
                    );
                }
                else if (e is netDxf.Entities.Spline spline)
                {
                    geometries.Add(
                        new MarkGeometrySpline(spline)
                        {
                            LayerName = e.Layer.Name,
                            Stroke = e.Color.ToColor()
                        }
                    );
                }
                else if (e is netDxf.Entities.LwPolyline lwPLine)
                {
                    geometries.Add(
                        new MarkGeometryPath(lwPLine)
                        {
                            LayerName = e.Layer.Name,
                            Stroke = e.Color.ToColor()
                        }
                    );
                }
                else if (e is netDxf.Entities.Polyline pLine)
                {
                    geometries.Add(
                        new MarkGeometryPath(pLine)
                        {
                            LayerName = e.Layer.Name,
                            Stroke = e.Color.ToColor()
                        }
                    );
                }
                else if (e is netDxf.Entities.Line line)
                {
                    geometries.Add(
                        new MarkGeometryLine(line)
                        {
                            LayerName = e.Layer.Name,
                            Stroke = e.Color.ToColor()
                        }
                    );
                }
                else if (e is netDxf.Entities.Circle circle)
                {
                    geometries.Add(
                        new MarkGeometryCircle(circle)
                        {
                            LayerName = e.Layer.Name,
                            Stroke = e.Color.ToColor()
                        }
                    );
                }
                else if (e is netDxf.Entities.Arc arc)
                {
                    geometries.Add(
                        new MarkGeometryArc(arc)
                        {
                            LayerName = e.Layer.Name,
                            Stroke = e.Color.ToColor()
                        }
                    );
                }
            }

            return geometries;
        }
    }
}
