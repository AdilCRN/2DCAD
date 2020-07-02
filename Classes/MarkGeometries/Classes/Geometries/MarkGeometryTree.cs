using MathNet.Numerics.LinearAlgebra;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MSolvLib.MarkGeometry
{
    public class MarkGeometryTree : MarkGeometry, IMarkGeometryWrapper
    {
        public override string Name => "Tree";

        [XmlIgnore]
        private Color _bgColor = Color.Transparent;

        public Color BackgroundColor
        {
            get { return _bgColor; }
            set
            {
                _bgColor = value;
            }
        }

        public override double Area => Geometry.Area;
        public override double Perimeter => Geometry.Perimeter;

        public IMarkGeometry Geometry { get; set; } = null;
        public List<IMarkGeometry> Children { get; set; } = new List<IMarkGeometry>();

        public MarkGeometryTree()
            : base()
        {
            Fill = Color.Transparent;
            BackgroundColor = Color.Transparent;

            Update();
        }

        public MarkGeometryTree(Color bgColor, Color fgColor)
            : base()
        {
            Fill = fgColor;
            BackgroundColor = bgColor;

            Update();
        }

        public MarkGeometryTree(MarkGeometryTree input)
            : base(input)
        {
            Geometry = (IMarkGeometry)input.Geometry.Clone();
            Children = input.Children.ConvertAll(x => (IMarkGeometry)x.Clone());
            BackgroundColor = ColorTranslator.FromHtml(ColorTranslator.ToHtml(input.BackgroundColor));            

            Update();
        }

        public MarkGeometryTree(IMarkGeometry geometryIn, Color bgColor, Color fgColor)
           : base()
        {
            Fill = fgColor;
            BackgroundColor = bgColor;

            Geometry = geometryIn;
            Geometry.Fill = Fill;

            Update();
        }

        public static List<IMarkGeometry> FromGeometries(List<IMarkGeometry> geometriesIn, Color bgColor, Color fgColor)
        {
            geometriesIn.Sort(CompareHandler);
            geometriesIn.Reverse();

            var geometries = new List<IMarkGeometry>();

            foreach(var vector in geometriesIn)
            {
                bool success = false;

                foreach (var item in geometries)
                {
                    if (item is MarkGeometryTree tree && tree.AddChild_NoUpdate(vector, bgColor, fgColor))
                    {
                        success = true;
                        break;
                    }
                }

                if (!success)
                {
                    if (ShapeIsRegion(vector))
                    {
                        geometries.Add(new MarkGeometryTree(vector, bgColor, fgColor));
                    }
                    else
                    {
                        geometries.Add(vector);
                    }
                }
            }

            foreach(var item in geometries)
            {
                if (item is MarkGeometryTree)
                {
                    item.Update();
                }
            }

            return geometries;
        }

        public static int CompareHandler(IMarkGeometry aIn, IMarkGeometry bIn)
        {
            if (aIn.Area > bIn.Area)
            {
                return 1;
            }
            else if (aIn.Area < bIn.Area)
            {
                return -1;
            }

            return 0;
        }

        public bool AddChild_NoUpdate(IMarkGeometry vector, Color bgColor, Color fgColor)
        {
            if (Geometry == null)
            {
                if (!ShapeIsRegion(vector))
                {
                    return false;
                }

                Geometry = vector;
                Geometry.Fill = fgColor;
                return true;
            }

            if (!GeometricArithmeticModule.IsWithin2D(vector, Geometry))
            {
                return false;
            }

            foreach (var child in Children)
            {
                if (child is MarkGeometryTree && GeometricArithmeticModule.IsWithin2D(vector, child))
                {
                    if (!(child as MarkGeometryTree).AddChild_NoUpdate(vector, fgColor, bgColor))
                    {
                        vector.Fill = bgColor;
                        Children.Add(vector);
                    }

                    return true;
                }
            }

            if (ShapeIsRegion(vector))
            {
                Children.Add(new MarkGeometryTree(vector, fgColor, bgColor));
            }
            else
            {
                vector.Fill = bgColor;
                Children.Add(vector);
            }

            return true;
        }

        public bool AddChild(IMarkGeometry geometryIn, Color bgColor, Color fgColor)
        {
            if (AddChild_NoUpdate(geometryIn, bgColor, fgColor))
            {
                Update();
                return true;
            }

            return false;
        }

        public List<IMarkGeometry> Flatten()
        {
            var geometries = new List<IMarkGeometry>();

            if (Geometry != null)
            {
                geometries.Add(Geometry);
            }

            foreach(var child in Children)
            {
                if (child is MarkGeometryTree)
                {
                    geometries.AddRange((child as MarkGeometryTree).Flatten());
                }
                else
                {
                    geometries.Add(child);
                }
            }

            return geometries;
        }

        public static bool ShapeIsRegion(IMarkGeometry shape)
        {
            return (shape is MarkGeometryPath || shape is MarkGeometryCircle || shape is MarkGeometryRectangle || shape is MarkGeometryEllipse);
        }

        public override object Clone()
        {
            return new MarkGeometryTree(this);
        }

        public override void Draw2D(IMarkGeometryVisualizer2D view, bool shouldShowVertex)
        {
            Geometry?.Draw2D(view, shouldShowVertex);

            foreach(var child in Children)
            {
                child.Draw2D(view, shouldShowVertex);
            }
        }

        public void ParallelGetAll(Action<IMarkGeometry> callback)
        {
            Parallel.ForEach(Children, (child) =>
            {
                if (child is MarkGeometryTree childTree)
                {
                    childTree.ParallelGetAll((subChild) =>
                    {
                        callback(subChild);
                    });
                }
                else
                {
                    callback(child);
                }
            });
        }

        public void MapFunc(Func<IMarkGeometry, IMarkGeometry> function)
        {
            ParallelGetAll((geometry) => {
                geometry = function(geometry);
            });
        }

        public override void SetFill(Color? colorIn)
        {
            ParallelGetAll((geometry) => {
                geometry.SetFill(colorIn);
            });

            base.SetFill(colorIn);
        }

        public override void SetStroke(Color? colorIn)
        {
            ParallelGetAll((geometry) => {
                geometry.SetStroke(colorIn);
            });

            base.SetStroke(colorIn);
        }

        public void BeginGetAll(Func<IMarkGeometry, bool> callback)
        {
            foreach(var vector in Flatten())
            {
                callback(vector);
            }
        }

        public override EntityObject GetAsDXFEntity()
        {
            throw new NotImplementedException();
        }

        public override EntityObject GetAsDXFEntity(string layerName)
        {
            throw new NotImplementedException();
        }

        public override void SetExtents()
        {
            Extents = GeometricArithmeticModule.CalculateExtents(Flatten());
        }

        public override void Transform(Matrix<double> transformationMatrixIn)
        {
            Geometry?.Transform(transformationMatrixIn);

            foreach(var child in Children)
            {
                child.Transform(transformationMatrixIn);
            }

            Update();
        }

        public override void Update()
        {
            Geometry?.Update();

            foreach(var child in Children)
            {
                child.Update();
            }

            SetExtents();
        }
    }
}
