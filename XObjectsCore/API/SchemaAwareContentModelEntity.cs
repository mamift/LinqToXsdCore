using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Xml.Schema.Linq
{
    public abstract class SchemaAwareContentModelEntity : ContentModelEntity
    {
        internal Dictionary<XName, NamedContentModelEntity> elementPositions;
        int last = 0;

        protected SchemaAwareContentModelEntity(params ContentModelEntity[] items)
        {
            elementPositions = new Dictionary<XName, NamedContentModelEntity>();

            foreach (var item in items)
            {
                item.ParentContentModel = this;

                if (item is NamedContentModelEntity named)
                {
                    UpdateElementPosition(named, true);
                }
                else if (item is SchemaAwareContentModelEntity group)
                {
                    //cmEntity is choice or sequence
                    foreach (NamedContentModelEntity namedItem in group.ElementPositions.Values)
                    {
                        UpdateElementPosition(namedItem, false);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            void UpdateElementPosition(NamedContentModelEntity entity, bool checkSubstitutionGroup)
            {
                if (!elementPositions.ContainsKey(entity.Name))
                {
                    //Pick the first position for a repeating name
                    entity.ElementPosition = last++;
                    elementPositions.Add(entity.Name, entity);
                    //Add subst members to the same position as head if this a substitution head
                    if (checkSubstitutionGroup)
                    {
                        CheckSubstitutionGroup(entity);
                    }
                }
            }
            void CheckSubstitutionGroup(NamedContentModelEntity entity)
            {
                if (entity is SubstitutedContentModelEntity substEntity)
                {
                    foreach (XName name in substEntity.Members)
                    {
                        //Add Subst members to the lookup table
                        if (!elementPositions.ContainsKey(name))
                        {
                            elementPositions.Add(name, entity);
                        }
                    }
                }
            }

        }

        internal IEnumerable<SchemaAwareContentModelEntity> GetSelfAndAncestorsUntil(SchemaAwareContentModelEntity ancestor)
        {
            yield return this;
            foreach (var thisAncestor in this.Ancestors)
            {
                if (thisAncestor == ancestor)
                {
                    break;
                }
                else
                {
                    yield return thisAncestor;
                }
            }
        }

        /// <summary>
        /// Notify ancestors that an element has been added to this <see cref="SchemaAwareContentModelEntity"/>.<br/>
        /// Let the <see cref="ChoiceContentModelEntity"/> class a chance to remove other elements.
        /// </summary>
        /// <param name="owner">The owner of the added element.</param>
        /// <param name="element">The added element.</param>
        /// <param name="parentElement">The parent of the added element.</param>
        internal virtual void OnElementAdded(SchemaAwareContentModelEntity owner, XElement element, XElement parentElement)
        {
            if (this.ParentContentModel != null)
            {
                this.ParentContentModel.OnElementAdded(owner, element, parentElement);
            }
        }

        private SchemaAwareContentModelEntity Root
        {
            get
            {
                if (this.ParentContentModel == null)
                {
                    return this;
                }
                return this.Ancestors.Last();
            }
        }

        internal bool Contains(XElement element)
        {
            var namedContentModel = this.GetNamedEntity(element.Name);
            return namedContentModel?.Ancestors.Any(cm => cm == this) ?? false;
        }

        internal XElement FindElementPosition(NamedContentModelEntity namedEntity, XElement parentElement,
            bool addToExisting, out EditAction editAction)
        {
            Debug.Assert(namedEntity != null);
            int newElementPos = namedEntity.ElementPosition;
            XElement lastElement = GetLastElement(parentElement);
            if (lastElement != null)
            {
                //Optimization to check last first
                var cmLast = GetNamedEntity(lastElement.Name);
                if (cmLast != null)
                {
                    int lastElementPos = cmLast.ElementPosition;
                    if (newElementPos == lastElementPos)
                    {
                        if (addToExisting)
                        {
                            editAction = EditAction.Append;
                        }
                        else
                        {
                            editAction = EditAction.Update;
                        }

                        return lastElement;
                    }

                    if (newElementPos > lastElementPos)
                    {
                        //We need to add the new element at the end
                        editAction = EditAction.Append;
                        return lastElement;
                    }
                }
            }

            XElement instanceElem = null;
            IEnumerator<XElement> enumerator = parentElement.Elements().GetEnumerator();

            while (enumerator.MoveNext())
            {
                instanceElem = enumerator.Current;
                int instanceElementPos = GetElementPosition(instanceElem.Name);
                if (instanceElementPos == newElementPos)
                {
                    if (!addToExisting)
                    {
                        //Matching element found for update
                        editAction = EditAction.Update;
                        return instanceElem;
                    }
                }
                else if (instanceElementPos > newElementPos)
                {
                    //Found first element greater than new position
                    editAction = EditAction.AddBefore;
                    return instanceElem;
                }
            }

            //Either its the first element being added or Scanned the whole list, end of list reached         
            editAction = EditAction.Append;
            return instanceElem;
        }

        private XElement GetLastElement(XElement parentElement)
        {
            XNode lastNode = parentElement.LastNode;
            XElement lastElement = lastNode as XElement;
            return lastElement;
        }

        internal XElement AddValueInPosition(XName name, XElement parentElement, bool addToExisting, object value,
            XmlSchemaDatatype datatype)
        {
            NamedContentModelEntity namedEntity = GetNamedEntity(name);
            if (namedEntity == null)
            {
                throw new LinqToXsdException("Name does not belong in content model. Cannot set value for child " +
                                             namedEntity.Name);
            }

            EditAction editAction = EditAction.None;
            XElement elementMarker = FindElementPosition(namedEntity, parentElement, addToExisting, out editAction);
            Debug.Assert(datatype != null); //Simple typed value add or set
            XElement element = elementMarker;

            switch (editAction)
            {
                case EditAction.Append:
                    element = new XElement(name, XTypedServices.GetXmlString(value, datatype, parentElement));
                    parentElement.Add(element);
                    break;

                case EditAction.Update:
                    Debug.Assert(elementMarker != null);
                    elementMarker.Value = XTypedServices.GetXmlString(value, datatype, elementMarker);
                    break;

                case EditAction.AddBefore:
                    Debug.Assert(elementMarker != null);
                    element = new XElement(name, XTypedServices.GetXmlString(value, datatype, elementMarker));
                    elementMarker.AddBeforeSelf(element);
                    break;

                default:
                    throw new InvalidOperationException();
            }
            return element;
        }

        internal XElement AddElementInPosition(XName name, XElement parentElement, bool addToExisting, XTypedElement xObj, Type elementBaseType)
        {
            NamedContentModelEntity namedEntity = GetNamedEntity(name);
            if (namedEntity == null)
            {
                // See http://linqtoxsd.codeplex.com/WorkItem/View.aspx?WorkItemId=3542
                throw new LinqToXsdException(
                    "Name does not belong in content model. Cannot set value for child " +
                    name.LocalName);
            }

            XElement elementMarker = FindElementPosition(namedEntity, parentElement, addToExisting, out var editAction);

            XElement newElement = XTypedServices.GetXElement(xObj, name, elementBaseType);
            Debug.Assert(xObj != null);

            switch (editAction)
            {
                case EditAction.Append:
                    parentElement.Add(newElement);
                    break;

                case EditAction.Update:
                    elementMarker.AddBeforeSelf(newElement);
                    elementMarker.Remove();
                    break;

                case EditAction.AddBefore:
                    elementMarker.AddBeforeSelf(newElement);
                    break;
            }
            return newElement;
        }

        public override XElement AddElementToParent(XName name, object value, XElement parentElement, bool addToExisting,
            XmlSchemaDatatype datatype, Type elementBaseType)
        {
            Debug.Assert(value != null);

            if (ReferenceEquals(value, XNil.Value))
            {
                // This is not the right XTypedElement subclass, but it doesn't matter for AddElementInPosition,
                // which only extracts the untyped XElement from it.
                // Metadata is also lost, but xsi:type isn't meaningful with xsi:nil so it doesn't matter.
                var typedElement = new XTypedElement(XNil.Element(name));
                return Root.AddElementInPosition(name, parentElement, addToExisting, typedElement, elementBaseType);
            }
            else if (datatype != null)
            {
                return Root.AddValueInPosition(name, parentElement, addToExisting, value, datatype);
            }
            else
            {
                return Root.AddElementInPosition(name, parentElement, addToExisting, value as XTypedElement, elementBaseType);
            }
        }

        internal virtual ContentModelType ContentModelType
        {
            get { return ContentModelType.None; }
        }

        internal NamedContentModelEntity GetNamedEntity(XName name)
        {
            NamedContentModelEntity namedEntity = null;
            elementPositions.TryGetValue(name, out namedEntity);
            return namedEntity;
        }

        internal int GetElementPosition(XName name)
        {
            NamedContentModelEntity named = GetNamedEntity(name);
            if (named != null)
            {
                return named.ElementPosition;
            }

            return -1;
        }

        internal Dictionary<XName, NamedContentModelEntity> ElementPositions
        {
            get
            {
                if (elementPositions == null)
                {
                    elementPositions = new Dictionary<XName, NamedContentModelEntity>();
                }

                return elementPositions;
            }
        }
    }
}