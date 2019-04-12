using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Xml.Schema.Linq
{
    public class ChoiceContentModelEntity : SchemaAwareContentModelEntity
    {
        public ChoiceContentModelEntity(params ContentModelEntity[] items) : base(items) { }

        internal override ContentModelType ContentModelType => ContentModelType.Choice;

        public override void AddElementToParent(XName name, object value, XElement parentElement, bool addToExisting,
            XmlSchemaDatatype datatype)
        {
            base.AddElementToParent(name, value, parentElement, addToExisting, datatype);
            CheckChoiceBranches(name, parentElement);
        }

        private void CheckChoiceBranches(XName currentBranch, XElement parentElement)
        {
            var elementsToRemove = new List<XElement>();
            NamedContentModelEntity otherBranch = null;
            foreach (XElement instanceElement in parentElement.Elements())
            {
                if (instanceElement.Name == currentBranch)
                {
                    //This is the element we set just now
                    continue;
                }

                otherBranch = GetNamedEntity(instanceElement.Name);
                if (otherBranch != null)
                {
                    //It is a branch of choice
                    Debug.Assert(otherBranch.ParentContentModel ==
                                 this); //Currently this should be invoked only for flat choices
                    elementsToRemove.Add(instanceElement);
                }
            }

            foreach (XElement elementToRemove in elementsToRemove)
            {
                elementToRemove.Remove();
            }
        }
    }
}