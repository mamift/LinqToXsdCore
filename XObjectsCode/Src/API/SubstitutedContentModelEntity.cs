using System.Xml.Linq;

namespace Xml.Schema.Linq
{
    public class SubstitutedContentModelEntity : NamedContentModelEntity
    {
        XName[] members;

        public SubstitutedContentModelEntity(params XName[] names) : base(names[names.Length - 1])
        {
            //this.name = names[names.Length -1]; //The last one is the name of the head element
            this.members = names;
        }

        internal XName[] Members
        {
            get { return members; }
        }
    }
}