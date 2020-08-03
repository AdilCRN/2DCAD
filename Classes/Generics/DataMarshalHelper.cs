using System.Collections.Generic;
using System.Linq;

namespace MarkGeometriesLib.Classes.Generics
{
    public static class DataMarshalHelper
    {
        public static List<T> ZipItemsIntoSingleList<T>(IEnumerable<List<T>> items)
        {
            var buffer = new List<T>();
            var nx = items.Select(itm => itm.Count()).Max();

            for (int i = 0; i < nx; i++)
            {
                foreach (var itm in items)
                {
                    if (itm.Count() > i)
                    {
                        buffer.Add(itm[i]);
                    }
                }
            }

            return buffer;
        }
    }
}
