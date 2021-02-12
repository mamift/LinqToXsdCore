namespace Xml.Schema.Linq
{
    /// <summary>
    /// Using separate annotationType object that will be added to XElement annotation
    /// Since XElement does not support looking up annotations by Super Type
    /// </summary>
    internal class XTypedElementAnnotation
    {
        internal XTypedElement typedElement;

        internal XTypedElementAnnotation(XTypedElement typedElement)
        {
            this.typedElement = typedElement;
        }
    }
}