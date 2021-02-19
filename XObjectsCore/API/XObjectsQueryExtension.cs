//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Diagnostics;

namespace Xml.Schema.Linq
{
    public partial class XTypedElement
    {
        //XML Query axes
        IEnumerable<T> IXTyped.Descendants<T>()
        {
            XTypedElement currentObject = this as XTypedElement;
            Type lookupType = typeof(T);

            //Metadata
            IXMetaData schemaMetaData = currentObject as IXMetaData;
            Dictionary<XName, System.Type> localElementsDict = null;
            ILinqToXsdTypeManager typeManager = schemaMetaData.TypeManager;
            Dictionary<XName, Type> typeDictionary = typeManager.GlobalTypeDictionary;

            //FSM
            XName matchingName = null;
            WildCard matchingWildCard = null;
            int currentState = FSM.InvalidState;

            XElement parentElement = null;
            Stack<XTypedElement> elementStack = new Stack<XTypedElement>();

            while (true)
            {
                schemaMetaData = currentObject as IXMetaData;
                FSM fsm = currentObject.ValidationStates;
                if (fsm != null)
                {
                    StartFsm();
                    currentState = fsm.Start;
                }

                Debug.Assert(schemaMetaData != null);
                localElementsDict = schemaMetaData.LocalElementsDictionary;
                parentElement = currentObject.Untyped;

                matchingName = null;
                matchingWildCard = null;

                XTypedElement childObject = null;
                bool validContent = true;

                foreach (XElement childElement in parentElement.Elements())
                {
                    bool isTypeT = IsAnnoatedElemTypeOf<T>(childElement, out childObject);

                    if (fsm != null)
                    {
                        //Always execute FSM no matter whether we find an element of type T
                        currentState = FsmMakeTransition(currentState, childElement.Name, out matchingName,
                            out matchingWildCard);
                        if (currentState == FSM.InvalidState)
                        {
                            validContent = false;
                            break;
                        }
                    }

                    if (!isTypeT)
                    {
                        //check dictionary
                        if (fsm != null && matchingWildCard != null)
                        {
                            childObject = XTypedServices.ToXTypedElement(childElement, typeManager); //Auto-typing
                        }
                        else
                        {
                            childObject = TypeChildElement(childElement, localElementsDict, typeManager);
                        }

                        if (childObject != null)
                        {
                            Type runtimeType = childObject.GetType();
                            if (lookupType.IsAssignableFrom(runtimeType))
                            {
                                isTypeT = true;
                            }
                            else
                            {
                                //Check content type
                                Type contentType = null;
                                if (typeManager.RootContentTypeMapping.TryGetValue(runtimeType, out contentType) &&
                                    lookupType.IsAssignableFrom(contentType))
                                {
                                    childObject = GetContentType(childObject);
                                    isTypeT = true;
                                }
                            }
                        }
                    }

                    if (isTypeT)
                    {
                        yield return (T) childObject;
                    }

                    if (childObject != null)
                    {
                        elementStack.Push(childObject);
                    }
                }

                if (validContent && elementStack.Count > 0)
                {
                    currentObject = elementStack.Pop();
                }
                else
                {
                    break;
                }
            }
        }

        private XTypedElement GetContentType(XTypedElement clrRootObject)
        {
            IXMetaData childMetaData = clrRootObject as IXMetaData;
            Debug.Assert(childMetaData.TypeOrigin == SchemaOrigin.Element);
            XTypedElement content = childMetaData.Content;
            return content;
        }

        IEnumerable<T> IXTyped.Ancestors<T>()
        {
            XTypedElement parent = getTypedParent();
            while (parent != null)
            {
                T parentT = parent as T;
                if (parentT != null)
                {
                    yield return parentT;
                }

                parent = parent.getTypedParent();
            }
        }

        XTypedElement getTypedParent()
        {
            XElement parentElement = this.Untyped.Parent;
            if (parentElement != null)
            {
                XTypedElementAnnotation annotation = parentElement.Annotation<XTypedElementAnnotation>();
                if (annotation != null)
                {
                    return annotation.typedElement;
                }
            }

            return null;
        }

        IEnumerable<T> IXTyped.SelfAndDescendants<T>()
        {
            T thisT = this as T;
            if (thisT != null)
                yield return thisT;
            foreach (T descendant in Query.Descendants<T>())
            {
                yield return descendant;
            }
        }

        IEnumerable<T> IXTyped.SelfAndAncestors<T>()
        {
            T thisT = this as T;
            if (thisT != null)
                yield return thisT;
            foreach (T ancestor in Query.Ancestors<T>())
            {
                yield return ancestor;
            }
        }

        private XTypedElement TypeChildElement(XElement element, Dictionary<XName, System.Type> localElementsDict,
            ILinqToXsdTypeManager typeManager)
        {
            Type clrType = null;
            XTypedElement childTypedElement = null;
            if (localElementsDict.TryGetValue(element.Name, out clrType))
            {
                Type contentType = null;
                if (typeManager.RootContentTypeMapping.TryGetValue(clrType, out contentType))
                {
                    childTypedElement = XTypedServices.ToXTypedElement(element, typeManager, clrType, contentType);
                }
                else if (typeof(XTypedElement).IsAssignableFrom(clrType))
                {
                    //It can also be simple types
                    childTypedElement = XTypedServices.ToXTypedElement(element, typeManager, clrType);
                }
            }
            else
            {
                //Type not found, fall-back to auto-typing
                childTypedElement = XTypedServices.ToXTypedElement(element, typeManager);
            }

            return childTypedElement;
        }

        private bool IsAnnoatedElemTypeOf<T>(XElement element, out XTypedElement childTypedElement)
            where T : XTypedElement
        {
            childTypedElement = null;
            T typedChild = XTypedServices.GetAnnotation<T>(element);
            if (typedChild != null)
            {
                childTypedElement = typedChild;
                return true;
            }
            else return false;
        }
    }
}