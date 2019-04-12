namespace Xml.Schema.Linq
{
    internal class XTypedElementWrapperAnnotation
    {
        //Seperate class for annotating root elements
        internal XTypedElement typedElement;

        internal XTypedElementWrapperAnnotation(XTypedElement typedElement)
        {
            this.typedElement = typedElement;
        }
    }
}