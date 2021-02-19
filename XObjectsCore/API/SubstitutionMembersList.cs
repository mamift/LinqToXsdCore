using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Xml.Schema.Linq
{
    internal class SubstitutionMembersList : IEnumerable<XElement>
    {
        XTypedElement container;
        XName[] namesInList;

        internal SubstitutionMembersList(XTypedElement container, params XName[] memberNames)
        {
            this.container = container;
            this.namesInList = memberNames;
        }

        public IEnumerator<XElement> GetEnumerator()
        {
            foreach (XElement childElement in container.Untyped.Elements())
            {
                for (int i = 0; i < namesInList.Length; i++)
                {
                    if (namesInList.GetValue(i).Equals(childElement.Name))
                    {
                        yield return childElement;
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator) GetEnumerator();
        }

        internal IEnumerator<XElement> FSMGetEnumerator()
        {
            IEnumerator<XElement> enumerator = container.Untyped.Elements().GetEnumerator();
            XElement elem = null;
            container.StartFsm();
            do
            {
                elem = container.ExecuteFSMSubGroup(enumerator, namesInList);
                if (elem != null) yield return elem;
                else yield break;
            } while (elem != null);
        }
    }
}