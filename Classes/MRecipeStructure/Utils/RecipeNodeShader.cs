using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using MSolvLib.MarkGeometry;
using SharpGLShader;
using System;
using System.Collections.Generic;

namespace MRecipeStructure.Classes.MRecipeStructure.Utils
{
    public class RecipeNodeShader : MGLShader
    {
        #region Section: Private Properties

        private List<IMarkGeometry> _fiducialPattern;
        private Func<string, List<IMarkGeometry>> _fetchDxfFunc;

        #endregion

        #region Section: Public Properties

        public double[] SelectionColor = MGLShader.Red;
        public double[] FiducialColor = MGLShader.Cyan;
        public double[] DefaultColor = MGLShader.White;
        public double[] TileColor = new double[] { 1.0, 0.7, 0.7, 0.5 };

        #endregion

        #region Section: Constructor

        public RecipeNodeShader(Func<string, List<IMarkGeometry>> fetchDxfFunc)
            : base()
        {
            _fetchDxfFunc = fetchDxfFunc;
        }

        #endregion

        #region Section: Adding Geometries

        public virtual void AddRecipe(MRecipe recipe)
        {
            if (recipe == null)
                return;

            foreach (var plate in recipe.Plates)
                AddRecipeNode(recipe, plate);
        }

        public virtual void AddRecipeNode(MRecipe recipe, MRecipeBaseNode recipeNode)
        {
            if (recipe == null || recipeNode == null)
                return;

            var extents = new GeometryExtents<double>()
            {
                MinX = double.MaxValue,
                MinY = double.MaxValue,
                MinZ = double.MaxValue,
                MaxX = double.MinValue,
                MaxY = double.MinValue,
                MaxZ = double.MinValue,
            };

            MRecipe.BeginGetAllLayers_Parallel(recipeNode, (layer) =>
            {
                extents = GeometryExtents<double>.Combine(
                    extents,
                    AddLayer(recipe, layer)
                );
            });

            // calculate size of fiducial relative to the node
            var fiducialSize = 0.025 * extents.Hypotenuse;

            // generate fiducial pattern
            GenerateFiducialPattern(fiducialSize);

            // get node's transform
            var baseTransform = recipeNode.TransformInfo.ToMatrix4x4();//MRecipe.GetAbsoluteTransform(recipe, recipeNode);

            // render fiducials in parent's reference frame
            foreach (var fiducial in recipeNode.Fiducials)
            {
                var transform = GeometricArithmeticModule.CombineTransformations(
                    baseTransform,
                    GeometricArithmeticModule.GetTranslationTransformationMatrix(
                        fiducial.X, fiducial.Y, fiducial.Z
                    )
                );

                foreach (var geometry in _fiducialPattern)
                {
                    var clone = (IMarkGeometry)geometry.Clone();
                    clone.Transform(transform);
                    AddDefault(clone, FiducialColor);
                }
            }
        }

        public virtual void AddLayerTiles(MRecipe recipe, MRecipeDeviceLayer layer)
        {
            // get layer's transform
            var transform = MRecipe.GetRelativeTransform(recipe, layer);

            // update layer's tile info
            if (layer.TileDescriptions.Count <= 0)
            {
                layer.GenerateTileDescriptionsFromSettings(
                    (patternFilePath)  =>
                    {
                        return GeometricArithmeticModule.CalculateExtents(
                            _fetchDxfFunc(patternFilePath)
                        );
                    }
                );
            }

            // render tiles
            for (int i = 0; i < layer.TileDescriptions.Count; i++)
            {
                var tile = (MarkGeometryRectangle)layer.TileDescriptions[i];
                tile.Transform(transform);

                AddDefault(tile, TileColor);
            }
        }

        #endregion

        #region Section: Helpers

        private GeometryExtents<double> AddLayer(MRecipe recipe, MRecipeDeviceLayer layer)
        {
            var extents = new GeometryExtents<double>()
            {
                MinX = double.MaxValue,
                MinY = double.MaxValue,
                MinZ = double.MaxValue,
                MaxX = double.MinValue,
                MaxY = double.MinValue,
                MaxZ = double.MinValue,
            };

            // calculate layer's transform
            var transform = MRecipe.GetRelativeTransform(recipe, layer);

            // fetch geometries from pattern file
            var geometries = _fetchDxfFunc(layer.PatternFilePath);

            for (int i = 0; i < geometries?.Count; i++)
            {
                var clone = (IMarkGeometry)geometries[i].Clone();
                clone.Transform(transform);

                // render geometry
                AddDefault(
                    clone,
                    DefaultColor
                );

                // update extents
                extents = GeometryExtents<double>.Combine(
                    extents,
                    clone.Extents
                );
            }

            return extents;
        }

        public void GenerateFiducialPattern(double radius = 2.5, int numOfLines = 4)
        {
            _fiducialPattern = new List<IMarkGeometry>();

            var baseLine = new MarkGeometryLine(new MarkGeometryPoint(-radius, 0), new MarkGeometryPoint(radius, 0));
            var transform = GeometricArithmeticModule.GetRotationTransformationMatrix(
                0, 0, Math.PI / numOfLines
            );

            for (int i = 0; i < numOfLines; i++)
            {
                _fiducialPattern.Add((IMarkGeometry)baseLine.Clone());
                baseLine.Transform(transform);
            }

            _fiducialPattern.Add(new MarkGeometryCircle(radius));
        } 

        #endregion
    }
}
