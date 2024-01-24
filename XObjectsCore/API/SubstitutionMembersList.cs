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

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal IEnumerable<XElement> FSMGetEnumerator()
        {
            IEnumerator<XElement> enumerator = container.Untyped.Elements().GetEnumerator();
            container.StartFsm();
            while (true)
            {
                XElement elem = container.ExecuteFSMSubGroup(enumerator, namesInList);
                if (elem == null) yield break;
                yield return elem;
            }
        }
    }
}