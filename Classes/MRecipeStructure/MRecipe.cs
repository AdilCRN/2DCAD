using System;
using MSolvLib;
using System.IO;
using System.Numerics;
using MSolvLib.MarkGeometry;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MSolvLib.Classes.MarkGeometries.Classes.Helpers;
using MarkGeometriesLib.Classes.Generics;

namespace MRecipeStructure.Classes.MRecipeStructure
{
    [Serializable]
    public class MRecipe : MLinkedNode
    {
        #region Section: File Saving Meta

        [XmlIgnore]
        public static string DefaultFileExtension = "rcp";

        [XmlIgnore]
        public static string FileFilter = "MSOLV Recipe files (*.rcp)|*.rcp";

        /**
         *  This variable is automatically updated when its contents are loaded from a file.
         *  see @LoadFromFile.
         *  
         *  This allows changes to be saved (to it's original file) without the
         *  need to ask a user for a file path.
         *  
         *  Example:
         *      var status = DonorPlateSettings.LoadFromFile(@"C:\MSOLV\Recipes\default.xml");
         *      status.Save(config.DefaultFilePath);
         */
        [XmlIgnore]
        public string DefaultFilePath = ""; 

        #endregion

        #region Section: Data Binding

        private TransformInfo _transform;

        public TransformInfo TransformInfo
        {
            get { return _transform; }
            set
            {
                _transform = value;
                NotifyPropertyChanged();
            }
        }


        private ObservableCollection<MRecipePlate> _plates;

        public ObservableCollection<MRecipePlate> Plates
        {
            get { return _plates; }
            set
            {
                _plates = value;
                NotifyPropertyChanged();
            }
        } 

        #endregion

        #region Section: Constructors

        /// <summary>
        ///     The copy constructor.
        /// </summary>
        /// <param name="recipe"></param>
        private MRecipe(MRecipe recipe)
            : base(recipe)
        {
            DefaultFilePath = (string)recipe.DefaultFilePath.Clone();
            TransformInfo = (TransformInfo)recipe.TransformInfo.Clone();
            Plates = new ObservableCollection<MRecipePlate>();

            foreach (var plate in recipe.Plates)
            {
                var p = (MRecipePlate)plate.Clone();
                p.Parent = this;
                Plates.Add(p);
            }
        }

        public MRecipe()
            : base()
        {
            TransformInfo = new TransformInfo();
            Plates = new ObservableCollection<MRecipePlate>();
        }

        public MRecipe(MRecipePlate plate)
            : this()
        {
            Plates.Add(plate);
        }

        public MRecipe(MRecipePlate plate, TransformInfo transformInfo)
            : this(plate)
        {
            TransformInfo = transformInfo;
        } 

        #endregion

        #region Section: Public Methods

        public void AddPlate(MRecipePlate plate)
        {
            plate.Parent = this;
            Plates.Add(plate);
        }

        public void BeginGetAllPlates(Action<MRecipePlate> callback)
        {
            foreach (var plate in Plates)
            {
                callback(plate);
            }
        }

        public void BeginGetAllPlates_Parallel(Action<MRecipePlate> callback)
        {
            Parallel.ForEach(Plates, (plate) =>
            {
                callback(plate);
            });
        }

        public List<MRecipePlate> Flatten()
        {
            var plates = new List<MRecipePlate>();

            BeginGetAllPlates((plate) => 
            {
                plates.Add(plate);
            });

            return plates;
        }

        public void UpdateParents()
        {
            foreach (var plate in Plates)
            {
                plate.Parent = this;

                foreach (var device in plate.Devices)
                {
                    device.Parent = plate;

                    foreach (var layer in device.Layers)
                    {
                        layer.Parent = device;
                    }
                }
            }
        }

        public bool Save(string filePathIn)
        {
            XMLSerialiser.SerializeToXML(this, filePathIn);
            DefaultFilePath = filePathIn;

            return File.Exists(filePathIn);
        } 

        #endregion

        #region Section: Static Helpers

        public static MRecipe LoadFromFile(string filePathIn)
        {
            if (!File.Exists(filePathIn))
            {
                throw new FileNotFoundException($"could not find `{filePathIn}`");
            }

            var res = XMLSerialiser.DeserialiseFromXML<MRecipe>(filePathIn);
            res.DefaultFilePath = filePathIn;
            res.UpdateParents();

            return res;
        }

        public static Matrix4x4 GetRelativeTransform(MRecipePlate plate)
        {
            return plate.TransformInfo.ToMatrix4x4();
        }

        public static Matrix4x4 GetRelativeTransform(MRecipe recipe, MRecipeDevice device)
        {
            if (device.Parent == null)
                recipe.UpdateParents();

            return GeometricArithmeticModule.CombineTransformations(
                // add device's transform
                device.TransformInfo.ToMatrix4x4(),

                // add plate's transform
                (device.Parent as MRecipeBaseNode).TransformInfo.ToMatrix4x4()
            );
        }

        public static Matrix4x4 GetRelativeTransform(MRecipe recipe, MRecipeDeviceLayer layer)
        {
            if (layer.Parent?.Parent == null)
                recipe.UpdateParents();

            return GeometricArithmeticModule.CombineTransformations(
                // add layer's transform
                layer.TransformInfo.ToMatrix4x4(),

                // add device's transform
                (layer.Parent as MRecipeBaseNode).TransformInfo.ToMatrix4x4(),

                // add plate's transform
                (layer.Parent.Parent as MRecipeBaseNode).TransformInfo.ToMatrix4x4()
            );
        }

        public static Matrix4x4 GetRelativeTransform(MRecipe recipe, MRecipeBaseNode recipeNode)
        {
            if (recipeNode is MRecipePlate plate)
            {
                return GetRelativeTransform(plate);
            }
            else if (recipeNode is MRecipeDevice device)
            {
                return GetRelativeTransform(recipe, device);
            }
            else if (recipeNode is MRecipeDeviceLayer layer)
            {
                return GetRelativeTransform(recipe, layer);
            }

            return GeometricArithmeticModule.GetDefaultTransformationMatrix();
        }

        public static Matrix4x4 GetRelativeTransformFromParent(MRecipeBaseNode parentNode, MRecipeBaseNode recipeNode)
        {
            var transformChain = new List<Matrix4x4>();

            var nodeParent = recipeNode;
            while (
                nodeParent != null &&
                nodeParent != parentNode
            )
            {
                transformChain.Add(nodeParent.TransformInfo.ToMatrix4x4());
                nodeParent = nodeParent.Parent as MRecipeBaseNode;
            }

            transformChain.Add(parentNode.TransformInfo.ToMatrix4x4());
            return GeometricArithmeticModule.CombineTransformations(transformChain.ToArray());
        }

        /// <summary>
        ///     Returns nodes in the recipe structure without travesing arrays.
        /// </summary>
        /// <param name="recipeIn">The recipe structure</param>
        /// <param name="callbackIn">A callback to receive the recipe nodes</param>
        public static void BeginGetNodes(MRecipe recipeIn, Action<MRecipeBaseNode> callbackIn)
        {
            foreach (var plate in recipeIn.Plates)
            {
                callbackIn(plate);

                foreach (var device in plate.Devices)
                {
                    callbackIn(device);

                    foreach (var layer in device.Layers)
                    {
                        callbackIn(layer);
                    }
                }
            }
        }

        /// <summary>
        ///     Returns all layers in the recipe structure auto generating based on array its info.
        /// </summary>
        /// <param name="recipeIn">The recipe structure</param>
        /// <param name="callbackIn">A callback to receive the recipe layers</param>
        public static void BeginGetAllLayers(MRecipe recipeIn, Action<MRecipeDeviceLayer> callbackIn)
        {
            recipeIn.BeginGetAllPlates((plate) =>
            {
                BeginGetAllLayers(plate, callbackIn);
            });
        }

        /// <summary>
        ///     Returns all layers in the recipe plate auto generating based on array its info.
        /// </summary>
        /// <param name="plateIn">The plate</param>
        /// <param name="callbackIn">A callback to receive the recipe layers</param>
        public static void BeginGetAllLayers(MRecipePlate plateIn, Action<MRecipeDeviceLayer> callbackIn)
        {
            plateIn.BeginGetAllDevices((device) =>
            {
                BeginGetAllLayers(device, callbackIn);
            });
        }

        /// <summary>
        ///     Returns all layers in the recipe device auto generating based on array its info.
        /// </summary>
        /// <param name="deviceIn">The device</param>
        /// <param name="callbackIn">A callback to receive the recipe layers</param>
        public static void BeginGetAllLayers(MRecipeDevice deviceIn, Action<MRecipeDeviceLayer> callbackIn)
        {
            deviceIn.BeginGetAllLayers((layer) =>
            {
                callbackIn(layer);
            });
        }

        /// <summary>
        ///     Returns all layers in the recipe node auto generating based on its array info.
        /// </summary>
        /// <param name="nodeIn">The layer</param>
        /// <param name="callbackIn">A callback to receive the recipe layers</param>
        public static void BeginGetAllLayers(MRecipeBaseNode nodeIn, Action<MRecipeDeviceLayer> callbackIn)
        {
            if (nodeIn is MRecipePlate plate)
            {
                BeginGetAllLayers(plate, callbackIn);
            }
            else if (nodeIn is MRecipeDevice device)
            {
                BeginGetAllLayers(device, callbackIn);
            }
            else if (nodeIn is MRecipeDeviceLayer layer)
            {
                callbackIn(layer);
            }
        }

        /// <summary>
        ///     Returns all layers in the recipe structure auto generating based on array info.
        ///     Sacrifice order/arrangement for speed.
        /// </summary>
        /// <param name="recipeIn">The recipe structure</param>
        /// <param name="callbackIn">A callback to receive the recipe layers</param>
        public static void BeginGetAllLayers_Parallel(MRecipe recipeIn, Action<MRecipeDeviceLayer> callbackIn)
        {
            recipeIn.BeginGetAllPlates_Parallel((plate) =>
            {
                BeginGetAllLayers_Parallel(plate, callbackIn);
            });
        }

        /// <summary>
        ///     Returns all layers in the recipe plate auto generating based on its array info.
        ///     Sacrifice order/arrangement for speed.
        /// </summary>
        /// <param name="plateIn">The plate</param>
        /// <param name="callbackIn">A callback to receive the recipe layers</param>
        public static void BeginGetAllLayers_Parallel(MRecipePlate plateIn, Action<MRecipeDeviceLayer> callbackIn)
        {
            plateIn.BeginGetAllDevices_Parallel((device) =>
            {
                BeginGetAllLayers_Parallel(device, callbackIn);
            });
        }

        /// <summary>
        ///     Returns all layers in the recipe device auto generating based on its array info.
        ///     Sacrifice order/arrangement for speed.
        /// </summary>
        /// <param name="plateIn">The device</param>
        /// <param name="callbackIn">A callback to receive the recipe layers</param>
        public static void BeginGetAllLayers_Parallel(MRecipeDevice deviceIn, Action<MRecipeDeviceLayer> callbackIn)
        {
            deviceIn.BeginGetAllLayers_Parallel((layer) =>
            {
                callbackIn(layer);
            });
        }

        /// <summary>
        ///     Returns all layers in the recipe node auto generating based on its array info.
        ///     Sacrifice order/arrangement for speed.
        /// </summary>
        /// <param name="nodeIn">The layer</param>
        /// <param name="callbackIn">A callback to receive the recipe layers</param>
        public static void BeginGetAllLayers_Parallel(MRecipeBaseNode nodeIn, Action<MRecipeDeviceLayer> callbackIn)
        {
            if (nodeIn is MRecipePlate plate)
            {
                BeginGetAllLayers_Parallel(plate, callbackIn);
            }
            else if (nodeIn is MRecipeDevice device)
            {
                BeginGetAllLayers_Parallel(device, callbackIn);
            }
            else if (nodeIn is MRecipeDeviceLayer layer)
            {
                callbackIn(layer);
            }
        }

        /// <summary>
        ///     Use method to calculate a recipe's extents.
        ///     Warning this method could take a really long time depending on the number of geometries.
        /// </summary>
        /// <param name="recipeIn">The recipe</param>
        /// <param name="patternToExtentsFuncIn">A func that returns the count and extents of a given pattern file.</param>
        /// <returns>The count and extents of geometries within node</returns>
        public static (long Count, GeometryExtents<double> Extents) CalculateExtents(MRecipe recipeIn, Func<string, (long Count, GeometryExtents<double> Extents)> patternToExtentsFuncIn)
        {
            long count = 0;
            var __cache = new CachedLoader<(long, GeometryExtents<double>)>(15);
            var extents = GeometryExtents<double>.CreateDefaultDouble();

            BeginGetAllLayers_Parallel(recipeIn, (layer) => {

                // don't track empty layers
                if (!File.Exists(layer.PatternFilePath))
                    return;

                // use locker to fetch extents
                lock (__cache)
                {
                    (long Count, GeometryExtents<double> Extents) data = __cache.TryGet(layer.PatternFilePath, () => patternToExtentsFuncIn(layer.PatternFilePath));

                    // combine extents with previous
                    extents = GeometryExtents<double>.Combine(
                        extents,
                        data.Extents
                    );

                    // count geometries
                    count += data.Count;
                }
            });

            return (count, count <= 0 ? null : extents);
        }

        /// <summary>
        ///     Use method to calculate a node's extents.
        ///     Warning this method could take a really long time depending on the number of geometries.
        /// </summary>
        /// <param name="nodeIn">The recipe node</param>
        /// <param name="patternToExtentsFuncIn">A func that returns the count and extents of a given pattern file.</param>
        /// <returns>The count and extents of geometries within node</returns>
        public static (long Count, GeometryExtents<double> Extents) CalculateExtents(MRecipeBaseNode nodeIn, Func<string, (long Count, GeometryExtents<double> Extents)> patternToExtentsFuncIn)
        {
            long count = 0;
            var __cache = new CachedLoader<(long, GeometryExtents<double>)>(15);
            var extents = GeometryExtents<double>.CreateDefaultDouble();

            BeginGetAllLayers_Parallel(nodeIn, (layer) => {

                // don't track empty layers
                if (!File.Exists(layer.PatternFilePath))
                    return;

                // use locker to fetch extents
                lock (__cache)
                {
                    (long Count, GeometryExtents<double> Extents) data = __cache.TryGet(layer.PatternFilePath, () => patternToExtentsFuncIn(layer.PatternFilePath));

                    // combine extents with previous
                    extents = GeometryExtents<double>.Combine(
                        extents,
                        data.Extents
                    );

                    // count geometries
                    count += data.Count;
                }
            });

            return (count, count <= 0 ? null : extents);
        }

        /// <summary>
        ///     Use method to calculate a recipe's extents, if pattern file is DXF.
        ///     Warning this method could take a really long time depending on the number of geometries.
        /// </summary>
        /// <param name="recipeIn">The recipe</param>
        /// <returns>The count and extents of geometries</returns>
        public static (long Count, GeometryExtents<double> Extents) CalculateExtents(MRecipe recipeIn)
        {
            return CalculateExtents(recipeIn, (filePath) =>
            {
                var pattern = GeometricArithmeticModule.ExtractGeometriesFromDXF(filePath);
                return (pattern.Count, GeometricArithmeticModule.CalculateExtents(pattern));
            });
        }

        /// <summary>
        ///     Use method to calculate a recipe's extents, if pattern file is DXF.
        ///     Warning this method could take a really long time depending on the number of geometries.
        /// </summary>
        /// <param name="nodeIn">The recipe node</param>
        /// <returns>The count and extents of geometries</returns>
        public static (long Count, GeometryExtents<double> Extents) CalculateExtents(MRecipeBaseNode nodeIn)
        {
            return CalculateExtents(nodeIn, (filePath) =>
            {
                var pattern = GeometricArithmeticModule.ExtractGeometriesFromDXF(filePath);
                return (pattern.Count, GeometricArithmeticModule.CalculateExtents(pattern));
            });
        }

        #endregion

        #region Section: Public Overrides

        public override object Clone()
        {
            return new MRecipe(this);
        }

        public override string ToString()
        {
            return $"Recipe";
        } 

        #endregion
    }
}
