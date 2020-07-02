using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSolvLib.MarkGeometry
{
    public interface IMarkGeometryWrapper
    {
        List<IMarkGeometry> Flatten();
        void BeginGetAll(Func<IMarkGeometry, bool> callback);
        void MapFunc(Func<IMarkGeometry, IMarkGeometry> function);
    }
}
