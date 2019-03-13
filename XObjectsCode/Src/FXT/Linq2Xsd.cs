//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Xml.Fxt
{
    using System.Linq;
    using System.Xml.Schema;
    using System.Xml.Linq;
    using System.Collections.Generic;

    public static class FxtLinq2XsdInterpreter
    {
        static void interpret(XmlSchemaSet schemas, XElement trafo, FxtLog log, List<IFxtTransformation> trafos)
        {
            foreach (var child in trafo.Elements())
            {
                if (child.Name == FxtInterpreter.FxtNs + "Deanonymize"
                    && child.Elements().Count() == 0)
                {
                    bool strict = (bool?) child.Attribute("strict") ?? false;
                    foreach (var x in schemas.Deanonymize(new FxtScope(), strict, log))
                        trafos.Add(x);
                    continue;
                }

                throw FxtInterpreter.ex;
            }
        }

        public static FxtLog Run(XmlSchemaSet schemas, XElement trafo)
        {
            return FxtInterpreter.Run(schemas, trafo, interpret);
        }
    }
}