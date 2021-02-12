using System.Collections;
using System.Diagnostics;
using System.Text;

namespace Xml.Schema.Linq
{
    internal static class ListFormatter 
    {
        public static string ToString(object value)
        {
            Debug.Assert(value is IEnumerable);
            IEnumerable list = (IEnumerable) value;
            StringBuilder bldr = new StringBuilder();

            foreach (object o in list)
            {
                // Separate values by single space character
                if (bldr.Length != 0)
                    bldr.Append(' ');

                bldr.Append(o.ToString());
            }

            return bldr.ToString();
        }
    }
}