namespace Xml.Schema.Linq
{
    public class SequenceContentModelEntity : SchemaAwareContentModelEntity
    {
        public SequenceContentModelEntity(params ContentModelEntity[] items) : base(items) { }

        internal override ContentModelType ContentModelType
        {
            get { return ContentModelType.Sequence; }
        }
    }
}