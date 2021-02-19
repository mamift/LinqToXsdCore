//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml;

namespace Xml.Schema.Linq
{
    using System.Collections;
    using System.Text;
    using System.Diagnostics;

    internal class XObjectsNamespaceList
    {
        public enum ListType
        {
            Any,
            Other,
            Set
        };

        private ListType type = ListType.Any;
        private Hashtable set = null;
        private string targetNamespace;
        private string namespaces;

        internal string Namespaces
        {
            get { return namespaces; }
        }

        internal string TargetNamespace
        {
            get { return targetNamespace; }
        }

        public XObjectsNamespaceList()
        {
        }

        public XObjectsNamespaceList(string namespaces, string targetNamespace)
        {
            this.targetNamespace = targetNamespace;
            this.namespaces = namespaces;
            if (namespaces == "##any" || namespaces.Length == 0)
            {
                type = ListType.Any;
            }
            else if (namespaces == "##other")
            {
                type = ListType.Other;
            }
            else
            {
                type = ListType.Set;
                set = new Hashtable();
                foreach (string ns in XmlConvertExt.SplitString(namespaces))
                {
                    if (ns == "##local")
                    {
                        set[string.Empty] = string.Empty;
                    }
                    else if (ns == "##targetNamespace")
                    {
                        set[targetNamespace] = targetNamespace;
                    }
                    else
                    {
                        XmlConvertExt.ToUri(ns); // can throw
                        set[ns] = ns;
                    }
                }
            }
        }

        public XObjectsNamespaceList Clone()
        {
            XObjectsNamespaceList nsl = (XObjectsNamespaceList) MemberwiseClone();
            if (type == ListType.Set)
            {
                Debug.Assert(set != null);
                nsl.set = (Hashtable) (set.Clone());
            }

            return nsl;
        }

        public ListType Type
        {
            get { return type; }
        }

        public string Excluded
        {
            get { return targetNamespace; }
        }

        public ICollection Enumerate
        {
            get
            {
                switch (type)
                {
                    case ListType.Set:
                        return set.Keys;
                    case ListType.Other:
                    case ListType.Any:
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public virtual bool Allows(string ns)
        {
            switch (type)
            {
                case ListType.Any:
                    return true;
                case ListType.Other:
                    return ns != targetNamespace && ns.Length != 0;
                case ListType.Set:
                    return set[ns] != null;
            }

            Debug.Assert(false);
            return false;
        }

        public bool Allows(XmlQualifiedName qname)
        {
            return Allows(qname.Namespace);
        }

        public override string ToString()
        {
            switch (type)
            {
                case ListType.Any:
                    return "##any";
                case ListType.Other:
                    return "##other";
                case ListType.Set:
                    StringBuilder sb = new StringBuilder();
                    bool first = true;
                    foreach (string s in set.Keys)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(" ");
                        }

                        if (s == targetNamespace)
                        {
                            sb.Append("##targetNamespace");
                        }
                        else if (s.Length == 0)
                        {
                            sb.Append("##local");
                        }
                        else
                        {
                            sb.Append(s);
                        }
                    }

                    return sb.ToString();
            }

            Debug.Assert(false);
            return string.Empty;
        }

        public static bool IsSubset(XObjectsNamespaceList sub, XObjectsNamespaceList super)
        {
            if (super.type == ListType.Any)
            {
                return true;
            }
            else if (sub.type == ListType.Other && super.type == ListType.Other)
            {
                return super.targetNamespace == sub.targetNamespace;
            }
            else if (sub.type == ListType.Set)
            {
                if (super.type == ListType.Other)
                {
                    return !sub.set.Contains(super.targetNamespace);
                }
                else
                {
                    Debug.Assert(super.type == ListType.Set);
                    foreach (string ns in sub.set.Keys)
                    {
                        if (!super.set.Contains(ns))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }


        public static XObjectsNamespaceList Union(XObjectsNamespaceList o1, XObjectsNamespaceList o2, bool v1Compat)
        {
            XObjectsNamespaceList nslist = null;
            Debug.Assert(o1 != o2);
            if (o1.type == ListType.Any)
            {
                //clause 2 - o1 is Any
                nslist = new XObjectsNamespaceList();
            }
            else if (o2.type == ListType.Any)
            {
                //clause 2 - o2 is Any
                nslist = new XObjectsNamespaceList();
            }
            else if (o1.type == ListType.Set && o2.type == ListType.Set)
            {
                //clause 3 , both are sets
                nslist = o1.Clone();
                foreach (string ns in o2.set.Keys)
                {
                    nslist.set[ns] = ns;
                }
            }
            else if (o1.type == ListType.Other && o2.type == ListType.Other)
            {
                //clause 4, both are negations
                if (o1.targetNamespace == o2.targetNamespace)
                {
                    //negation of same value
                    nslist = o1.Clone();
                }
                else
                {
                    //Not a breaking change, going from not expressible to not(absent)
                    nslist = new XObjectsNamespaceList("##other",
                        string.Empty); //clause 4, negations of different values, result is not(absent)
                }
            }
            else if (o1.type == ListType.Set && o2.type == ListType.Other)
            {
                if (v1Compat)
                {
                    if (o1.set.Contains(o2.targetNamespace))
                    {
                        nslist = new XObjectsNamespaceList();
                    }
                    else
                    {
                        //This was not there originally in V1, added for consistency since its not breaking
                        nslist = o2.Clone();
                    }
                }
                else
                {
                    if (o2.targetNamespace != string.Empty)
                    {
                        //clause 5, o1 is set S, o2 is not(tns)
                        nslist = o1.CompareSetToOther(o2);
                    }
                    else if (o1.set.Contains(string.Empty))
                    {
                        //clause 6.1 - set S includes absent, o2 is not(absent) 
                        nslist = new XObjectsNamespaceList();
                    }
                    else
                    {
                        //clause 6.2 - set S does not include absent, result is not(absent)
                        nslist = new XObjectsNamespaceList("##other", string.Empty);
                    }
                }
            }
            else if (o2.type == ListType.Set && o1.type == ListType.Other)
            {
                if (v1Compat)
                {
                    if (o2.set.Contains(o2.targetNamespace))
                    {
                        nslist = new XObjectsNamespaceList();
                    }
                    else
                    {
                        nslist = o1.Clone();
                    }
                }
                else
                {
                    //New rules
                    if (o1.targetNamespace != string.Empty)
                    {
                        //clause 5, o1 is set S, o2 is not(tns)
                        nslist = o2.CompareSetToOther(o1);
                    }
                    else if (o2.set.Contains(string.Empty))
                    {
                        //clause 6.1 - set S includes absent, o2 is not(absent) 
                        nslist = new XObjectsNamespaceList();
                    }
                    else
                    {
                        //clause 6.2 - set S does not include absent, result is not(absent)
                        nslist = new XObjectsNamespaceList("##other", string.Empty);
                    }
                }
            }

            return nslist;
        }

        private XObjectsNamespaceList CompareSetToOther(XObjectsNamespaceList other)
        {
            //clause 5.1
            XObjectsNamespaceList nslist = null;
            if (this.set.Contains(other.targetNamespace))
            {
                //S contains negated ns
                if (this.set.Contains(string.Empty))
                {
                    // AND S contains absent
                    nslist = new XObjectsNamespaceList(); //any is the result
                }
                else
                {
                    //clause 5.2
                    nslist = new XObjectsNamespaceList("##other", string.Empty);
                }
            }
            else if (this.set.Contains(string.Empty))
            {
                //clause 5.3 - Not expressible
                nslist = null;
            }
            else
            {
                //clause 5.4 - Set S does not contain negated ns or absent 
                nslist = other.Clone();
            }

            return nslist;
        }

        public static XObjectsNamespaceList Intersection(XObjectsNamespaceList o1, XObjectsNamespaceList o2,
            bool v1Compat)
        {
            XObjectsNamespaceList nslist = null;
            Debug.Assert(o1 != o2); //clause 1
            if (o1.type == ListType.Any)
            {
                //clause 2 - o1 is any
                nslist = o2.Clone();
            }
            else if (o2.type == ListType.Any)
            {
                //clause 2 - o2 is any
                nslist = o1.Clone();
            }
            else if (o1.type == ListType.Set && o2.type == ListType.Other)
            {
                //Clause 3 o2 is other
                nslist = o1.Clone();
                nslist.RemoveNamespace(o2.targetNamespace);
                if (!v1Compat)
                {
                    nslist.RemoveNamespace(string.Empty); //remove ##local
                }
            }
            else if (o1.type == ListType.Other && o2.type == ListType.Set)
            {
                //Clause 3 o1 is other
                nslist = o2.Clone();
                nslist.RemoveNamespace(o1.targetNamespace);
                if (!v1Compat)
                {
                    nslist.RemoveNamespace(string.Empty); //remove ##local
                }
            }
            else if (o1.type == ListType.Set && o2.type == ListType.Set)
            {
                //clause 4
                nslist = o1.Clone();
                nslist = new XObjectsNamespaceList();
                nslist.type = ListType.Set;
                nslist.set = new Hashtable();
                foreach (string ns in o1.set.Keys)
                {
                    if (o2.set.Contains(ns))
                    {
                        nslist.set.Add(ns, ns);
                    }
                }
            }
            else if (o1.type == ListType.Other && o2.type == ListType.Other)
            {
                if (o1.targetNamespace == o2.targetNamespace)
                {
                    //negation of same namespace name
                    nslist = o1.Clone();
                    return nslist;
                }

                if (!v1Compat)
                {
                    if (o1.targetNamespace == string.Empty)
                    {
                        // clause 6 - o1 is negation of absent
                        nslist = o2.Clone();
                    }
                    else if (o2.targetNamespace == string.Empty)
                    {
                        //clause 6 - o1 is negation of absent
                        nslist = o1.Clone();
                    }
                }

                //if it comes here, its not expressible //clause 5
            }

            return nslist;
        }

        private void RemoveNamespace(string tns)
        {
            if (this.set[tns] != null)
            {
                this.set.Remove(tns);
            }
        }

        public bool IsEmpty()
        {
            return ((type == ListType.Set) && ((set == null) || set.Count == 0));
        }
    };

    internal class NamespaceListV1Compat : XObjectsNamespaceList
    {
        public NamespaceListV1Compat(string namespaces, string targetNamespace) : base(namespaces, targetNamespace)
        {
        }

        public override bool Allows(string ns)
        {
            if (this.Type == ListType.Other)
            {
                return ns != Excluded;
            }
            else
            {
                return base.Allows(ns);
            }
        }
    }
}