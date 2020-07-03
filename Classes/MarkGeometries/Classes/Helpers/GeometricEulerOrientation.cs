using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSolvLib.MarkGeometry
{
    /// <summary>
    ///     This describes the euler orientation of the a geometric object
    ///     Given a flat grid, X+ travels towards you, Y travels perpendicular
    ///     to X and sideways parallel to you (right is the positive direction),
    ///     and Z+ travel up and out of the flat grid.
    /// </summary>
    public struct GeometricEulerOrientation
    {
        public double Roll;
        public double Pitch;
        public double Yaw;

        public GeometricEulerOrientation(double roll, double pitch, double yaw)
        {
            Roll = roll;
            Pitch = pitch;
            Yaw = yaw;
        }
    }
}
