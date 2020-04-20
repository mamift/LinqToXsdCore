using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            foreach (ContentModelEntity cmEntity in items)
            {
                NamedContentModelEntity named = cmEntity as NamedContentModelEntity;
                if (named != null)
                {
                    if (!elementPositions.ContainsKey(named.Name))
                    {
                        //Pick the first position for a repeating name
                        named.ElementPosition = last++;
                        named.ParentContentModel = this;
                        elementPositions.Add(named.Name, named);
                        //Add subst members to the same position as head if this a substitution head
                        CheckSubstitutionGroup(named);
                    }
                }
                else
                {
                    //cmEntity is choice or sequence
                    SchemaAwareContentModelEntity scmEntity = cmEntity as SchemaAwareContentModelEntity;
                    Debug.Assert(scmEntity != null);
                    foreach (NamedContentModelEntity childEntity in scmEntity.ElementPositions.Values)
                    {
                        if (!elementPositions.ContainsKey(childEntity.Name))
                        {
                            childEntity.ElementPosition = last++; //Update position w.r.t parent
                            elementPositions.Add(childEntity.Name, childEntity);
                        }
                    }
                }
            }
        }

        private void CheckSubstitutionGroup(NamedContentModelEntity named)
        {
            SubstitutedContentModelEntity substEntity = named as SubstitutedContentModelEntity;
            if (substEntity != null)
            {
                foreach (XName name in substEntity.Members)
                {
                    //Add Subst members to the lookup table
                    if (!elementPositions.ContainsKey(name))
                    {
                        elementPositions.Add(name, named);
                    }
                }
            }
        }

        internal XElement FindElementPosition(NamedContentModelEntity namedEntity, XElement parentElement,
            bool addToExisting, out EditAction editAction)
        {
            Debug.Assert(namedEntity != null);
            editAction = EditAction.None;
            int newElementPos = namedEntity.ElementPosition;
            XElement lastElement = GetLastElement(parentElement);
            if (lastElement != null)
            {
                //Optimization to check last first
                int lastElementPos = GetNamedEntity(lastElement.Name).ElementPosition;
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

            int instanceElementPos = -1;
            XElement instanceElem = null;
            IEnumerator<XElement> enumerator = parentElement.Elements().GetEnumerator();

            while (enumerator.MoveNext())
            {
                instanceElem = enumerator.Current;
                instanceElementPos = GetElementPosition(instanceElem.Name);
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

        internal void AddValueInPosition(XName name, XElement parentElement, bool addToExisting, object value,
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

            switch (editAction)
            {
                case EditAction.Append:
                    parentElement.Add(new XElement(name, XTypedServices.GetXmlString(value, datatype, parentElement)));
                    break;

                case EditAction.Update:
                    Debug.Assert(elementMarker != null);
                    elementMarker.Value = XTypedServices.GetXmlString(value, datatype, elementMarker);
                    break;

                case EditAction.AddBefore:
                    Debug.Assert(elementMarker != null);
                    elementMarker.AddBeforeSelf(new XElement(name,
                        XTypedServices.GetXmlString(value, datatype, elementMarker)));
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        internal void AddElementInPosition(XName name, XElement parentElement, bool addToExisting, XTypedElement xObj, Type elementBaseType)
        {
            NamedContentModelEntity namedEntity = GetNamedEntity(name);
            if (namedEntity == null)
            {
                // See http://linqtoxsd.codeplex.com/WorkItem/View.aspx?WorkItemId=3542
                throw new LinqToXsdException(
                    "Name does not belong in content model. Cannot set value for child " +
                    name.LocalName);
            }

            EditAction editAction = EditAction.None;
            XElement elementMarker = FindElementPosition(namedEntity, parentElement, addToExisting, out editAction);

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
        }

        public override void AddElementToParent(XName name, object value, XElement parentElement, bool addToExisting,
            XmlSchemaDatatype datatype, Type elementBaseType)
        {
            Debug.Assert(value != null);
            if (datatype != null)
            {
                AddValueInPosition(name, parentElement, addToExisting, value, datatype);
            }
            else
            {
                AddElementInPosition(name, parentElement, addToExisting, value as XTypedElement, elementBaseType);
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