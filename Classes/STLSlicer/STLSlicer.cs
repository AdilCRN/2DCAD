using MSolvLib.MarkGeometry;
using SharpGLShader.Utils;
using STLSlicer.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace STLSlicer
{
    /// <summary>
    /// An improved slicing algorithm with efficient contour construction using STL files
    /// Adapted from https://www.researchgate.net/publication/276095353
    /// </summary>
    public class MSTLSlicer
    {
        private readonly static Regex MatchDouble = new Regex(@"([\-]?\d+[.]?\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

        #region Section: Public Properties
        
        public double MinZ { get; set; }
        public double MaxZ { get; set; }
        public List<STLFacet> Facets { get; set; } 

        #endregion

        public MSTLSlicer()
        {
            Facets = null;
        }

        public bool Load(string filePathIn)
        {
            if (!File.Exists(filePathIn))
                throw new FileNotFoundException($"file is missing or invaild: {filePathIn}");

            bool usingASCII = true;
            using (var reader = new StreamReader(filePathIn))
            {
                char[] buffer = new char[85];
                reader.Read(buffer, 0, 85);
                usingASCII = !buffer.Any(c => !(char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || (c == '-' || c == '.')));
            }

            if (usingASCII)
            {
                using (var reader = new StreamReader(filePathIn))
                {
                    (Facets, MinZ, MaxZ) = ParseASCII(reader);
                }
            }
            else
            {
                using (var stream = new FileStream(filePathIn, FileMode.Open, FileAccess.Read))
                {
                    using (var binaryStream = new BinaryReader(stream))
                    {
                        (Facets, MinZ, MaxZ) = ParseBinary(binaryStream);
                    }
                }
            }

            return true;
        }

        public List<ContourStructure>[] Slice()
        {
            return Slice(0.1 * (MaxZ - MinZ));
        }

        public List<MContourStructure>[] SliceParallel()
        {
            return SliceParallel(0.1 * (MaxZ - MinZ));
        }

        public List<ContourStructure>[] Slice(double sliceThickness, double tolerance = 0.0001)
        {
            if (Facets == null)
                return null;

            int numberOfSlices = (int)Math.Ceiling((MaxZ - MinZ) / sliceThickness);
            var contours = new List<ContourStructure>[numberOfSlices];
            IntersectionStructure intersection;
            LinkedList<IntersectionStructure> contourLinkedList;
            STLFacet currentFacet;
            STLEdge e1, e2;

            for (int i=0; i<Facets.Count; i++)
            {
                currentFacet = Facets[i];

                // calculating the slice number
                currentFacet.UpdateSliceNumber(MinZ, sliceThickness);

                // forward edge and backward edge judgments
                // use to select edge to use for intersection
                if (
                    (currentFacet.OrientationType == OrientationType.TypeA) && // Group A : oriented from min to max
                    (currentFacet.Vmax.Flag > currentFacet.Vmin.Flag) // and flagzmax > flagzmin
                )
                {
                    e1 = currentFacet.EdgeS2;
                    e2 = currentFacet.EdgeS1;
                }
                else if (
                    (currentFacet.OrientationType == OrientationType.TypeB) && // Group B : oriented from min to max
                    (currentFacet.Vmax.Flag < currentFacet.Vmin.Flag) // and flagzmax < flagzmin
                )
                {
                    e1 = currentFacet.EdgeS2;
                    e2 = currentFacet.EdgeS1;
                }
                else if (
                    (currentFacet.OrientationType == OrientationType.TypeC) && // Group C : oriented from max to min
                    (currentFacet.Vmax.Flag < currentFacet.Vmin.Flag) // and flagzmax < flagzmin
                )
                {
                    e1 = currentFacet.EdgeS1;
                    e2 = currentFacet.EdgeS2;
                }
                else if (
                    (currentFacet.OrientationType == OrientationType.TypeD) && // Group D : oriented from max to min
                    (currentFacet.Vmax.Flag > currentFacet.Vmin.Flag) // and flagzmax > flagzmin
                )
                {
                    e1 = currentFacet.EdgeS1;
                    e2 = currentFacet.EdgeS2;
                }
                else
                {
                    continue;
                }

                // slicing facet section below Smid
                for (int j = currentFacet.Smin; j < currentFacet.Smid; j++)
                {
                    double slicePosition = (j * sliceThickness) + MinZ;

                    // create intersecting structure
                    intersection = new IntersectionStructure()
                    {
                        ForwardEdgeIntersectionPoint = GetIntersection(
                            e1,
                            slicePosition // slice position
                        ),
                        ForwardEdge = e1,
                        BackwardEdge = e2
                    };

                    // The Efficient Contour Construction Algorithm (ECC)

                    // initialise contours array
                    if (contours[j] == null)
                        contours[j] = new List<ContourStructure>();

                    // used to mark whether the forward adjacent intersection structure or
                    // the backward adjacent intersection structure of IS has been found
                    bool foundForward = false;
                    bool foundBackward = false;

                    // used to mark the position where the backward adjacent intersection
                    // structure of IS has been found.
                    int position = -1;

                    for (int l = 0; l < contours[j].Count; l++)
                    {
                        contourLinkedList = contours[j][l].IntersectionList;

                        var connectionType = GetConnectionType(
                            contourLinkedList,
                            intersection,
                            tolerance
                        );

                        switch (connectionType)
                        {
                            case ConnectionType.START_TO_START: // these should never happen
                            case ConnectionType.END_TO_END: // must test to make sure
                            case ConnectionType.NONE:
                                continue;
                        }

                        if (
                            !foundForward &&
                            connectionType == ConnectionType.START_TO_END
                        )
                        {
                            if (foundBackward)
                            {
                                // copy items from current location to position
                                foreach (var node in contourLinkedList)
                                    contours[j][position].IntersectionList.AddLast(node);

                                // delete current location
                                contours[j].RemoveAt(l);
                                break;
                            }

                            foundForward = true;
                            position = l;

                            contourLinkedList.AddFirst(intersection);
                        }

                        if (
                            !foundBackward &&
                            connectionType == ConnectionType.END_TO_START
                        )
                        {
                            if (foundForward && position == l)
                            {
                                break; // contour must be closed since forward edge and backward edge are found in the same list
                            }
                            else if (foundForward)
                            {
                                // copy nodes from position to current location
                                // it's easier than copy from this location to the found location (reverse)
                                foreach (var node in contours[j][position].IntersectionList)
                                    contourLinkedList.AddLast(node);

                                // delete current location
                                contours[j].RemoveAt(position);
                                break;
                            }

                            foundBackward = true;
                            position = l;

                            contourLinkedList.AddLast(intersection);
                        }
                    }

                    if (!(foundForward || foundBackward)) // not forward or backward intersection; insert into contours list
                        contours[j].Add(new ContourStructure(intersection));
                }

                // forward edge and backward edge judgments
                // use to select edge to use for intersection
                if (
                    (currentFacet.OrientationType == OrientationType.TypeA) && // Group A : oriented from min to max
                    (currentFacet.Vmax.Flag > currentFacet.Vmin.Flag) // and flagzmax > flagzmin
                )
                {
                    e1 = currentFacet.EdgeS3;
                    e2 = currentFacet.EdgeS1;
                }
                else if (
                    (currentFacet.OrientationType == OrientationType.TypeB) && // Group B : oriented from min to max
                    (currentFacet.Vmax.Flag < currentFacet.Vmin.Flag) // and flagzmax < flagzmin
                )
                {
                    e1 = currentFacet.EdgeS3;
                    e2 = currentFacet.EdgeS1;
                }
                else if (
                    (currentFacet.OrientationType == OrientationType.TypeC) && // Group C : oriented from max to min
                    (currentFacet.Vmax.Flag < currentFacet.Vmin.Flag) // and flagzmax < flagzmin
                )
                {
                    e1 = currentFacet.EdgeS1;
                    e2 = currentFacet.EdgeS3;
                }
                else if (
                    (currentFacet.OrientationType == OrientationType.TypeD) && // Group D : oriented from max to min
                    (currentFacet.Vmax.Flag > currentFacet.Vmin.Flag) // and flagzmax > flagzmin
                )
                {
                    e1 = currentFacet.EdgeS1;
                    e2 = currentFacet.EdgeS3;
                }
                else
                {
                    continue;
                }

                // slicing facet section above Smid
                for (int k = currentFacet.Smid; k < currentFacet.Smax; k++)
                {
                    double slicePosition = (k * sliceThickness) + MinZ;

                    // create intersecting structure
                    intersection = new IntersectionStructure()
                    {
                        ForwardEdgeIntersectionPoint = GetIntersection(
                            e1,
                            slicePosition // slice position
                        ),
                        ForwardEdge = e1,
                        BackwardEdge = e2
                    };

                    // The Efficient Contour Construction Algorithm (ECC)

                    // initialise contours array
                    if (contours[k] == null)
                        contours[k] = new List<ContourStructure>();

                    // used to mark whether the forward adjacent intersection structure or
                    // the backward adjacent intersection structure of IS has been found
                    bool foundForward = false;
                    bool foundBackward = false;

                    // used to mark the position where the backward adjacent intersection
                    // structure of IS has been found.
                    int position = -1;

                    for (int l = 0; l < contours[k].Count; l++)
                    {
                        contourLinkedList = contours[k][l].IntersectionList;

                        var connectionType = GetConnectionType(
                            contourLinkedList,
                            intersection
                        );

                        switch (connectionType)
                        {
                            case ConnectionType.START_TO_START: // these should never happen
                            case ConnectionType.END_TO_END: // must test to make sure
                            case ConnectionType.NONE:
                                continue;
                        }

                        if (
                            !foundForward &&
                            connectionType == ConnectionType.START_TO_END
                        )
                        {
                            if (foundBackward)
                            {
                                // copy items from current location to position
                                foreach (var node in contourLinkedList)
                                    contours[k][position].IntersectionList.AddLast(node);
                                
                                // delete current location
                                contours[k].RemoveAt(l);
                                break;
                            }

                            foundForward = true;
                            position = l;

                            contourLinkedList.AddFirst(intersection);
                        }

                        if (
                            !foundBackward &&
                            connectionType == ConnectionType.END_TO_START
                        )
                        {
                            if (foundForward && position == l)
                            {
                                break; // contour must be closed since forward edge and backward edge are found in the same list
                            }
                            else if (foundForward)
                            {
                                // copy nodes from position to current location
                                // it's easier than copy from this location to the found location (reverse)
                                foreach (var node in contours[k][position].IntersectionList)
                                    contourLinkedList.AddLast(node);

                                // delete current location
                                contours[k].RemoveAt(position);
                                break;
                            }

                            foundBackward = true;
                            position = l;

                            contourLinkedList.AddLast(intersection);
                        }
                    }

                    if (!(foundForward || foundBackward)) // not forward or backward intersection; insert into contours list
                        contours[k].Add(new ContourStructure(intersection));
                }
            }

            return contours;
        }

        public List<MContourStructure>[] SliceParallel(double sliceThickness, double tolerance = 0.0001)
        {
            if (Facets == null)
                return null;

            int numberOfSlices = (int)Math.Ceiling((MaxZ - MinZ) / sliceThickness);
            var contours = new List<MContourStructure>[numberOfSlices];

            Parallel.For(0, Facets.Count, (i) =>
            {
                STLEdge e1, e2;
                STLFacet currentFacet = Facets[i];

                // calculating the slice number
                currentFacet.UpdateSliceNumber(MinZ, sliceThickness);

                // forward edge and backward edge judgments
                // use to select edge to use for intersection
                if (
                    (currentFacet.OrientationType == OrientationType.TypeA) && // Group A : oriented from min to max
                    (currentFacet.Vmax.Flag > currentFacet.Vmin.Flag) // and flagzmax > flagzmin
                )
                {
                    e1 = currentFacet.EdgeS2;
                    e2 = currentFacet.EdgeS1;
                }
                else if (
                    (currentFacet.OrientationType == OrientationType.TypeB) && // Group B : oriented from min to max
                    (currentFacet.Vmax.Flag < currentFacet.Vmin.Flag) // and flagzmax < flagzmin
                )
                {
                    e1 = currentFacet.EdgeS2;
                    e2 = currentFacet.EdgeS1;
                }
                else if (
                    (currentFacet.OrientationType == OrientationType.TypeC) && // Group C : oriented from max to min
                    (currentFacet.Vmax.Flag < currentFacet.Vmin.Flag) // and flagzmax < flagzmin
                )
                {
                    e1 = currentFacet.EdgeS1;
                    e2 = currentFacet.EdgeS2;
                }
                else if (
                    (currentFacet.OrientationType == OrientationType.TypeD) && // Group D : oriented from max to min
                    (currentFacet.Vmax.Flag > currentFacet.Vmin.Flag) // and flagzmax > flagzmin
                )
                {
                    e1 = currentFacet.EdgeS1;
                    e2 = currentFacet.EdgeS2;
                }
                else
                {
                    return;
                }

                // slicing facet section below Smid
                for (int j = currentFacet.Smin; j < currentFacet.Smid; j++)
                {
                    double slicePosition = (j * sliceThickness) + MinZ;

                    // create intersecting structure
                    var intersection = new IntersectionStructure()
                    {
                        ForwardEdgeIntersectionPoint = GetIntersection(
                            e1,
                            slicePosition // slice position
                        ),
                        ForwardEdge = e1,
                        BackwardEdge = e2,
                        SlicePosition = slicePosition
                    };

                    // The Efficient Contour Construction Algorithm (ECC)

                    // initialise contours array
                    if (contours[j] == null)
                        contours[j] = new List<MContourStructure>();

                    lock (contours[j])
                    {
                        // used to mark whether the forward adjacent intersection structure or
                        // the backward adjacent intersection structure of IS has been found
                        bool foundForward = false;
                        bool foundBackward = false;

                        // used to mark the position where the backward adjacent intersection
                        // structure of IS has been found.
                        int position = -1;

                        for (int l = 0; l < contours[j].Count; l++)
                        {
                            var contourLinkedList = contours[j][l].IntersectionList;

                            var connectionType = GetConnectionType(
                                contourLinkedList,
                                intersection,
                                tolerance
                            );

                            switch (connectionType)
                            {
                                case ConnectionType.START_TO_START: // these should never happen
                                case ConnectionType.END_TO_END: // must test to make sure
                                case ConnectionType.NONE:
                                    continue;
                            }

                            if (
                                !foundForward &&
                                connectionType == ConnectionType.START_TO_END
                            )
                            {
                                if (foundBackward)
                                {
                                    // copy items from current location to position
                                    contours[j][position].IntersectionList.AddLast(contourLinkedList);

                                    // delete current location
                                    contours[j].RemoveAt(l);
                                    break;
                                }

                                foundForward = true;
                                position = l;

                                contourLinkedList.AddFirst(intersection);
                            }

                            if (
                                !foundBackward &&
                                connectionType == ConnectionType.END_TO_START
                            )
                            {
                                if (foundForward && position == l)
                                {
                                    break; // contour must be closed since forward edge and backward edge are found in the same list
                                }
                                else if (foundForward)
                                {
                                    // copy nodes from position to current location
                                    // it's easier than copy from this location to the found location (reverse)
                                    contourLinkedList.AddLast(contours[j][position].IntersectionList);

                                    // delete current location
                                    contours[j].RemoveAt(position);
                                    break;
                                }

                                foundBackward = true;
                                position = l;

                                contourLinkedList.AddLast(intersection);
                            }
                        }

                        if (!(foundForward || foundBackward)) // not forward or backward intersection; insert into contours list
                            contours[j].Add(new MContourStructure(intersection));
                    }
                }

                // forward edge and backward edge judgments
                // use to select edge to use for intersection
                if (
                    (currentFacet.OrientationType == OrientationType.TypeA) && // Group A : oriented from min to max
                    (currentFacet.Vmax.Flag > currentFacet.Vmin.Flag) // and flagzmax > flagzmin
                )
                {
                    e1 = currentFacet.EdgeS3;
                    e2 = currentFacet.EdgeS1;
                }
                else if (
                    (currentFacet.OrientationType == OrientationType.TypeB) && // Group B : oriented from min to max
                    (currentFacet.Vmax.Flag < currentFacet.Vmin.Flag) // and flagzmax < flagzmin
                )
                {
                    e1 = currentFacet.EdgeS3;
                    e2 = currentFacet.EdgeS1;
                }
                else if (
                    (currentFacet.OrientationType == OrientationType.TypeC) && // Group C : oriented from max to min
                    (currentFacet.Vmax.Flag < currentFacet.Vmin.Flag) // and flagzmax < flagzmin
                )
                {
                    e1 = currentFacet.EdgeS1;
                    e2 = currentFacet.EdgeS3;
                }
                else if (
                    (currentFacet.OrientationType == OrientationType.TypeD) && // Group D : oriented from max to min
                    (currentFacet.Vmax.Flag > currentFacet.Vmin.Flag) // and flagzmax > flagzmin
                )
                {
                    e1 = currentFacet.EdgeS1;
                    e2 = currentFacet.EdgeS3;
                }
                else
                {
                    return;
                }

                // slicing facet section above Smid
                for (int k = currentFacet.Smid; k < currentFacet.Smax; k++)
                {
                    double slicePosition = (k * sliceThickness) + MinZ;

                    // create intersecting structure
                    var intersection = new IntersectionStructure()
                    {
                        ForwardEdgeIntersectionPoint = GetIntersection(
                            e1,
                            slicePosition // slice position
                        ),
                        ForwardEdge = e1,
                        BackwardEdge = e2
                    };

                    // The Efficient Contour Construction Algorithm (ECC)
                    
                    // initialise contours array
                    if (contours[k] == null)
                        contours[k] = new List<MContourStructure>();

                    lock (contours[k])
                    {
                        // used to mark whether the forward adjacent intersection structure or
                        // the backward adjacent intersection structure of IS has been found
                        bool foundForward = false;
                        bool foundBackward = false;

                        // used to mark the position where the backward adjacent intersection
                        // structure of IS has been found.
                        int position = -1;

                        for (int l = 0; l < contours[k].Count; l++)
                        {
                            var contourLinkedList = contours[k][l].IntersectionList;

                            var connectionType = GetConnectionType(
                                contourLinkedList,
                                intersection
                            );

                            switch (connectionType)
                            {
                                case ConnectionType.START_TO_START: // these should never happen
                                case ConnectionType.END_TO_END: // must test to make sure
                                case ConnectionType.NONE:
                                    continue;
                            }

                            if (
                                !foundForward &&
                                connectionType == ConnectionType.START_TO_END
                            )
                            {
                                if (foundBackward)
                                {
                                    // copy items from current location to position
                                    contours[k][position].IntersectionList.AddLast(contourLinkedList);

                                    // delete current location
                                    contours[k].RemoveAt(l);
                                    break;
                                }

                                foundForward = true;
                                position = l;

                                contourLinkedList.AddFirst(intersection);
                            }

                            if (
                                !foundBackward &&
                                connectionType == ConnectionType.END_TO_START
                            )
                            {
                                if (foundForward && position == l)
                                {
                                    break; // contour must be closed since forward edge and backward edge are found in the same list
                                }
                                else if (foundForward)
                                {
                                    // copy nodes from position to current location
                                    // it's easier than copy from this location to the found location (reverse)
                                    contourLinkedList.AddLast(contours[k][position].IntersectionList);

                                    // delete current location
                                    contours[k].RemoveAt(position);
                                    break;
                                }

                                foundBackward = true;
                                position = l;

                                contourLinkedList.AddLast(intersection);
                            }
                        }

                        if (!(foundForward || foundBackward)) // not forward or backward intersection; insert into contours list
                            contours[k].Add(new MContourStructure(intersection));
                    }
                }
            });

            return contours;
        }

        #region Section: Helpers

        public void PrintFacet(STLFacet stlFacet)
        {
            switch (stlFacet.OrientationType)
            {
                case OrientationType.TypeA:
                    Console.WriteLine($"Orientation Type: TypeA");
                    break;
                case OrientationType.TypeB:
                    Console.WriteLine($"Orientation Type: TypeB");
                    break;
                case OrientationType.TypeC:
                    Console.WriteLine($"Orientation Type: TypeC");
                    break;
                case OrientationType.TypeD:
                    Console.WriteLine($"Orientation Type: TypeD");
                    break;
                default:
                    break;
            }

            Console.WriteLine("");
            PrintEdge("Edge S1", stlFacet.EdgeS1);
            Console.WriteLine("");

            PrintEdge("Edge S2", stlFacet.EdgeS2);
            Console.WriteLine("");

            PrintEdge("Edge S3", stlFacet.EdgeS3);
            Console.WriteLine("");

            PrintVertex("Vmin", stlFacet.Vmin);
            PrintVertex("Vmid", stlFacet.Vmid);
            PrintVertex("Vmax", stlFacet.Vmax);
            Console.WriteLine("");

            PrintSliceNumber("Slice Number", stlFacet);
            Console.WriteLine("");
        }

        public void PrintEdge(string tag, STLEdge edge)
        {
            Console.WriteLine($"{tag} Edge");
            PrintVertex("Start", edge.Start);
            PrintVertex("End", edge.End);
        }

        public void PrintVertex(string tag, STLVertex vertex)
        {
            Console.WriteLine($"({vertex.Flag}){tag} X: {Math.Round(vertex.X, 4)}, Y: {Math.Round(vertex.Y, 4)}, Z: {Math.Round(vertex.Z, 4)}");
        }

        public void PrintSliceNumber(string tag, STLFacet stlFacet)
        {
            Console.WriteLine($"{tag} Smin: {stlFacet.Smin}, Smid: {stlFacet.Smid}, Smax: {stlFacet.Smax}");
        }

        /// <summary>
        /// http://www.ambrsoft.com/TrigoCalc/Plan3D/PlaneLineIntersection_.htm
        /// https://www.youtube.com/watch?v=WPYTruLFas8
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="planePosition"></param>
        /// <returns></returns>
        public static MVertex GetIntersection(STLEdge edge, double planePosition)
        {
            // plane equation = z - D; where D = planePosition
            // and the z equation in the parametric form
            // z = zp + ct; where c = edge.End.Z - edge.Start.Z and zp = edge.Start.Z

            double t = (planePosition - edge.Start.Z) / (edge.End.Z - edge.Start.Z);

            return new MVertex(
                edge.Start.X + ((edge.End.X - edge.Start.X) * t),
                edge.Start.Y + ((edge.End.Y - edge.Start.Y) * t)
            );
        }

        public ConnectionType GetConnectionType(LinkedList<IntersectionStructure> contourLinkedList, IntersectionStructure intersection, double tolerance = 0.0001)
        {
            if (CompareEqual(contourLinkedList.First.Value.ForwardEdge, intersection.BackwardEdge, tolerance))
                return ConnectionType.START_TO_END;
            else if (CompareEqual(contourLinkedList.Last.Value.BackwardEdge, intersection.ForwardEdge, tolerance))
                return ConnectionType.END_TO_START;

            return ConnectionType.NONE;
        }

        public ConnectionType GetConnectionType(MLinkedList<IntersectionStructure> contourLinkedList, IntersectionStructure intersection, double tolerance = 0.0001)
        {
            if (CompareEqual(contourLinkedList.First.Value.ForwardEdge, intersection.BackwardEdge, tolerance))
                return ConnectionType.START_TO_END;
            else if (CompareEqual(contourLinkedList.Last.Value.BackwardEdge, intersection.ForwardEdge, tolerance))
                return ConnectionType.END_TO_START;

            return ConnectionType.NONE;
        }

        public static bool CompareEqual(STLEdge e1, STLEdge e2, double tolerance = 0.0001)
        {
            return CompareEqual(e1.Start, e2.Start, tolerance) && CompareEqual(e1.End, e2.End, tolerance);
        }

        public static bool CompareEqual(STLVertex v1, STLVertex v2, double tolerance = 0.0001)
        {
            //return Math.Sqrt(Math.Pow(v2.X - v1.X, 2) + Math.Pow(v2.Y - v1.Y, 2)) < tolerance;
            return Math.Abs(v2.X - v1.X) < tolerance && Math.Abs(v2.Y - v1.Y) < tolerance;
        }

        private (List<STLFacet>, double MinZ, double MaxZ) ParseASCII(StreamReader readerIn)
        {
            double[] normals;
            var facets = new List<STLFacet>();

            double minZ = double.MaxValue;
            double maxZ = double.MinValue;

            while ((normals = GetNextMatch(readerIn, MatchDouble)) != null)
            {
                var facet = new STLFacet(
                    normals,
                    GetNextMatch(readerIn, MatchDouble),
                    GetNextMatch(readerIn, MatchDouble),
                    GetNextMatch(readerIn, MatchDouble)
                );

                // ignore facets that are parallel to the slicing plane
                if ((facet.Vmax.Z - facet.Vmin.Z) <= 0.0001)
                    continue;

                // track the minimum and maximum z coordinate
                if (facet.Vmax.Z > maxZ)
                    maxZ = facet.Vmax.Z;
                if (facet.Vmin.Z < minZ)
                    minZ = facet.Vmin.Z;

                facets.Add(
                    facet
                );
            }

            return (facets, minZ, maxZ);
        }

        private (List<STLFacet>, double MinZ, double MaxZ) ParseBinary(BinaryReader readerIn)
        {
            // read the header and number of facets.
            readerIn.ReadBytes(80);

            double[] normals = new double[3];
            double[] vertex1 = new double[3];
            double[] vertex2 = new double[3];
            double[] vertex3 = new double[3];

            double minZ = double.MaxValue;
            double maxZ = double.MinValue;

            uint howmany = BitConverter.ToUInt32(readerIn.ReadBytes(4), 0);
            var facets = new List<STLFacet>((int)howmany);

            for (int i = 0; i < howmany; i++)
            {
                normals[0] = BitConverter.ToSingle(readerIn.ReadBytes(4), 0);
                normals[1] = BitConverter.ToSingle(readerIn.ReadBytes(4), 0);
                normals[2] = BitConverter.ToSingle(readerIn.ReadBytes(4), 0);

                vertex1[0] = BitConverter.ToSingle(readerIn.ReadBytes(4), 0);
                vertex1[1] = BitConverter.ToSingle(readerIn.ReadBytes(4), 0);
                vertex1[2] = BitConverter.ToSingle(readerIn.ReadBytes(4), 0);

                vertex2[0] = BitConverter.ToSingle(readerIn.ReadBytes(4), 0);
                vertex2[1] = BitConverter.ToSingle(readerIn.ReadBytes(4), 0);
                vertex2[2] = BitConverter.ToSingle(readerIn.ReadBytes(4), 0);

                vertex3[0] = BitConverter.ToSingle(readerIn.ReadBytes(4), 0);
                vertex3[1] = BitConverter.ToSingle(readerIn.ReadBytes(4), 0);
                vertex3[2] = BitConverter.ToSingle(readerIn.ReadBytes(4), 0);

                // skip attribute byte count
                readerIn.ReadBytes(2);

                var facet = new STLFacet(
                    normals,
                    vertex1,
                    vertex2,
                    vertex3
                );

                // ignore facets that are parallel to the slicing plane
                if ((facet.Vmax.Z - facet.Vmin.Z) <= 0.001)
                    continue;

                // track the minimum and maximum z coordinate
                if (facet.Vmax.Z > maxZ)
                    maxZ = facet.Vmax.Z;
                if (facet.Vmin.Z < minZ)
                    minZ = facet.Vmin.Z;

                facets.Add(
                    facet
                );
            }

            return (facets, minZ, maxZ);
        }

        private double[] GetNextMatch(StreamReader readerIn, Regex patternIn)
        {
            string line;
            while ((line = readerIn.ReadLine()) != null)
            {
                if (
                    patternIn.IsMatch(line)
                )
                {
                    var collection = patternIn.Matches(line);
                    return new double[] {
                        double.Parse(collection[0].Value),
                        double.Parse(collection[1].Value),
                        double.Parse(collection[2].Value)
                    };
                }
            }

            return null;
        } 
        #endregion
    }
}

#region Section: Not Used
//try
//{
//    // create intersecting structure
//    intersection = new IntersectionStructure()
//{
//ForwardIntersectionPoint = GetIntersection(
//e1,
//slicePosition // slice position
//),
//        BackwardIntersectionPoint = GetIntersection(
//            e2,
//            slicePosition // slice position
//        ),
//        ForwardEdge = e1,
//        BackwardEdge = e2
//    };
//}
//catch (Exception exp)
//{
//    Console.WriteLine($"Slicing Mid to Max at {Math.Round(slicePosition, 4)}");
//    PrintFacet(currentFacet);
//PrintEdge("Forward (e1)", e1);
//PrintEdge("Backward (e2)", e2);
//    throw exp;
//} 
#endregion