using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Xml.Schema.Linq
{
    internal class TypeManager : ILinqToXsdTypeManager
    {
        private static XmlSchemaSet defaultSchemaSet = null;
        internal static TypeManager Default = new TypeManager();

        Dictionary<XName, Type> ILinqToXsdTypeManager.GlobalTypeDictionary
        {
            get { return XTypedServices.EmptyDictionary; }
        }

        Dictionary<XName, Type> ILinqToXsdTypeManager.GlobalElementDictionary
        {
            get { return XTypedServices.EmptyDictionary; }
        }

        Dictionary<Type, Type> ILinqToXsdTypeManager.RootContentTypeMapping
        {
            get { return XTypedServices.EmptyTypeMappingDictionary; }
        }

        XmlSchemaSet ILinqToXsdTypeManager.Schemas
        {
            get
            {
                if (defaultSchemaSet == null)
                {
                    XmlSchemaSet tempSet = new XmlSchemaSet();
                    Interlocked.CompareExchange<XmlSchemaSet>(ref defaultSchemaSet, tempSet, null);
                }

                return defaultSchemaSet;
            }
            set
            {
                defaultSchemaSet = value; //This operation should be atomic    
            }
        }
    }
}