using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Xml.Schema.Linq
{
    public class ChoiceContentModelEntity : SchemaAwareContentModelEntity
    {
        public ChoiceContentModelEntity(params ContentModelEntity[] items) : base(items) { }

        internal override ContentModelType ContentModelType => ContentModelType.Choice;

        public override XElement AddElementToParent(XName name, object value, XElement parentElement, bool addToExisting,
            XmlSchemaDatatype datatype, Type elementBaseType)
        {
            var element = base.AddElementToParent(name, value, parentElement, addToExisting, datatype, elementBaseType);
            this.RemoveChoices(element, parentElement);
            base.OnElementAdded(this, element, parentElement);
            return element;
        }

        internal override void OnElementAdded(SchemaAwareContentModelEntity owner, XElement element, XElement parentElement)
        {
            this.RemoveChoices(owner, parentElement);
            base.OnElementAdded(this, element, parentElement);
        }

        private void RemoveChoices(XElement keep, XElement parentElement)
        {
            var candidates = parentElement.Elements().Where(elem => this.Contains(elem));
            var toRemove   = candidates.Where(elem => elem != keep).ToArray();
            foreach (var element in toRemove)
            {
                element.Remove();
            }
        }

        private void RemoveChoices(SchemaAwareContentModelEntity elementOwner, XElement parentElement)
        {
            var owners     = elementOwner.GetSelfAndAncestorsUntil(this).ToArray();
            var candidates = parentElement.Elements().Where(elem => this.Contains(elem));
            var toKeep     = parentElement.Elements().Where(elem => owners.Any(owner => owner.Contains(elem)));
            var toRemove   = candidates.Except(toKeep).ToArray();
            foreach (var element in toRemove)
            {
                element.Remove();
            }
        }
    }
}