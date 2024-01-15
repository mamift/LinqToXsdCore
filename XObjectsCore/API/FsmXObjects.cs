//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml.Linq;
using System.Diagnostics;

//Defines interfaces involving FSM executions
namespace Xml.Schema.Linq
{
    public partial class XTypedElement
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int currentState;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal FSM ValidationStates
        {
            get
            {
                IXMetaData metaData = this as IXMetaData;
                Debug.Assert(metaData != null);
                return metaData.GetValidationStates();
            }
        }

        FSM IXMetaData.GetValidationStates()
        {
            return null;
        }

        internal void StartFsm()
        {
            if (this.ValidationStates != null) currentState = ValidationStates.Start;
        }

        internal int FsmMakeTransition(int prevState, XName inputSymbol, out XName matchingName,
            out WildCard matchingWildCard)
        {
            Transitions currTrans = ValidationStates.Trans[prevState];
            return currTrans.GetNextState(inputSymbol, out matchingName, out matchingWildCard);
        }

        internal XElement ExecuteFSM(IEnumerator<XElement> enumerator, XName requestingXName,
            WildCard requestingWildCard)
        {
            XElement currElem = null;
            WildCard matchingWildCard = null;
            XName matchingName = null;

            while (enumerator.MoveNext())
            {
                currElem = enumerator.Current;
                currentState = FsmMakeTransition(currentState, currElem.Name, out matchingName, out matchingWildCard);

                if (currentState != FSM.InvalidState)
                {
                    if ((requestingXName != null) && (matchingName != null))
                    {
                        if (requestingXName.Equals(currElem.Name)) return currElem;
                    }
                    else if ((requestingWildCard != null) && (matchingWildCard != null))
                    {
                        //requesting for ANY
                        if (requestingWildCard.Allows(currElem.Name)
                        ) //Make sure current element is allowed by requesting ANY property
                            return currElem;
                    }
                }
                else
                {
                    //Get stuck. No recovery attempt is provided for now.
                    return null;
                }
            }

            //No matching elements/wildcards are found
            return null;
        }

        internal XElement ExecuteFSMSubGroup(IEnumerator<XElement> enumerator, XName[] namesInList)
        {
            Debug.Assert(namesInList != null);

            XElement currElem = null;
            WildCard matchingWildCard = null;
            XName matchingName = null;

            while (enumerator.MoveNext())
            {
                currElem = enumerator.Current;
                currentState = FsmMakeTransition(currentState, currElem.Name, out matchingName, out matchingWildCard);

                if (currentState != FSM.InvalidState)
                {
                    if (matchingName != null)
                        for (int i = 0; i < namesInList.Length; i++)
                        {
                            if (namesInList.GetValue(i).Equals(currElem.Name)) return currElem;
                        }
                }
                else
                {
                    //Get stuck. No recovery attempt is provided for now.
                    return null;
                }
            }

            //No matching elements/wildcards are found
            return null;
        }

        private void FSMSetElement(XName name, object value, bool addToExisting, XmlSchemaDatatype datatype)
        {
            if (value == null)
            {
                //Delete existing node
                DeleteChild(name);
                return;
            }

            XElement parentElement = GetUntyped();
            parentElement.RemoveXsiNil();

            // Handle setting xsi:nil="true"
            if (ReferenceEquals(value, XNil.Value))
            {
                if (GetElement(name) is not {} pos)
                {
                    //happens for incomplete content, or choice
                    parentElement.Add(XNil.Element(name));
                }
                else if (addToExisting)
                {
                    pos.AddAfterSelf(XNil.Element(name));
                }
                else
                {
                    pos.SetXsiNil();
                }
                return;
            }

            //Simple typed element
            if (datatype != null)
            {            
                //Find out the matching element
                if (GetElement(name) is not {} pos)
                {
                    //happens for incomplete content, or choice
                    parentElement.Add(new XElement(name, XTypedServices.GetXmlString(value, datatype, parentElement)));
                }
                else if (addToExisting)
                {
                    pos.AddAfterSelf(new XElement(name, XTypedServices.GetXmlString(value, datatype, pos)));
                }
                else
                {
                    //Update value in place
                    pos.RemoveXsiNil();
                    pos.Value = XTypedServices.GetXmlString(value, datatype, pos);
                }
            }
            else
            {
                IXMetaData schemaMetaData = this;
                if (!schemaMetaData.LocalElementsDictionary.TryGetValue(name, out var elementBaseType))
                {
                    elementBaseType = value.GetType();
                }
                
                //Setting XTypedElement
                XElement newElement = XTypedServices.GetXElement(value as XTypedElement, name, elementBaseType);
                //Find the matching element
                if (GetElement(name) is not {} pos)
                {
                    //happens for incomplete content, or choice
                    parentElement.Add(newElement);
                }
                else
                {
                    pos.AddAfterSelf(newElement);
                    if (!addToExisting)
                    {
                        pos.Remove();
                    }
                }
            }
        }

        protected IEnumerable<XElement> GetWildCards(WildCard requestingWildCard)
        {
            IEnumerator<XElement> enumerator = this.GetUntyped().Elements().GetEnumerator();
            XElement elem = null;
            StartFsm();

            do
            {
                elem = ExecuteFSM(enumerator, null, requestingWildCard);
                if (elem != null) yield return elem;
                else yield break;
            } while (elem != null);
        }
    }
}