using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using netDxf.Entities;
using System;
using System.Drawing;
using System.Numerics;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace MSolvLib.MarkGeometry
{
    public abstract class MarkGeometry : IMarkGeometry, IXmlSerializable
    {
        [XmlIgnore]
        public GeometryExtents<double> Extents { get; set; } = new GeometryExtents<double>();

        [XmlIgnore]
        private Color? _color = null;

        public Color? Fill
        {
            get { return _color; }
            set
            {
                _color = value;
            }
        }


        [XmlIgnore]
        private Color? _stroke = null;

        public Color? Stroke
        {
            get { return _stroke; }
            set
            {
                _stroke = value;
            }
        }


        [XmlIgnore]
        private float _transparency = 1f;

        public float Transparency
        {
            get { return _transparency; }
            set
            {
                _transparency = value < 0 ? 1f : (float)GeometricArithmeticModule.Constrain(value, 0, 1);
            }
        }

        public string LayerName { get; set; } = "0";

        public virtual string Name => "Base";

        public virtual double Area { get; protected set; } = 0;
        public virtual double Perimeter { get; protected set; } = 0;

        public MarkGeometry()
        {
            Stroke = null;
            Fill = null;
            Transparency = 1f;
        }

        public MarkGeometry(MarkGeometry input)
        {
            Stroke = input.Stroke == null ? null : (Color?) ColorTranslator.FromHtml(ColorTranslator.ToHtml((Color)input.Stroke));
            Fill = input.Fill == null ? null : (Color?)ColorTranslator.FromHtml(ColorTranslator.ToHtml((Color)input.Fill));
            Transparency = input.Transparency;
            Area = input.Area + 0;
            Perimeter = input.Perimeter + 0;
        }

        public abstract void Transform(Matrix4x4 transformationMatrixIn);
        public abstract void SetExtents();
        public abstract void Update();
        public abstract void Draw2D(IMarkGeometryVisualizer2D view, bool shouldShowVertex);

        public void Draw2D(IMarkGeometryVisualizer2D view)
        {
            Draw2D(view, false);
        }

        public virtual void SetFill(Color? colorIn)
        {
            Fill = colorIn;
        }

        public virtual void SetStroke(Color? colorIn)
        {
            Stroke = colorIn;
        }

        public abstract object Clone();
        public abstract EntityObject GetAsDXFEntity();
        public abstract EntityObject GetAsDXFEntity(string layerName);

        public virtual XmlSchema GetSchema()
        {
            return (null);
        }

        protected void ReadXmlBaseImpl(XmlReader reader)
        {
            reader.MoveToContent();

            try
            {
                var fill = reader.GetAttribute(nameof(Fill));
                if (!string.IsNullOrWhiteSpace(fill))
                {
                    Fill = ColorTranslator.FromHtml(fill);
                }
            }
            catch (ArgumentNullException)
            {
                Fill = null;
            }

            try
            {
                var stroke = reader.GetAttribute(nameof(Stroke));
                if (!string.IsNullOrWhiteSpace(stroke))
                {
                    Stroke = ColorTranslator.FromHtml(stroke);
                }
            }
            catch (ArgumentNullException)
            {
                Stroke = null;
            }

            Transparency = float.Parse(reader.GetAttribute(nameof(Transparency)));
            LayerName = reader.GetAttribute(nameof(LayerName));
        }

        protected void WriteXmlBaseImpl(XmlWriter writer)
        {
            if (Fill != null)
            {
                writer.WriteAttributeString(nameof(Fill), ColorTranslator.ToHtml((Color)Fill));
            }

            if (Stroke != null)
            {
                writer.WriteAttributeString(nameof(Stroke), ColorTranslator.ToHtml((Color)Stroke));
            }

            writer.WriteAttributeString(nameof(Transparency), Transparency.ToString());
            writer.WriteAttributeString(nameof(LayerName), LayerName);
        }

        public virtual void ReadXml(XmlReader reader)
        {
            ReadXmlBaseImpl(reader);

            Update();
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            WriteXmlBaseImpl(writer);
        }
    }
}
