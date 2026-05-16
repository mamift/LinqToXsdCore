//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml.Linq;

namespace Xml.Schema.Linq.CodeGen
{
    public partial class GroupingInfo
    {
        public override FSM MakeFSM(StateNameSource stateNames)
        {
            FSM fsm = null;
            switch (this.contentModelType)
            {
                case ContentModelType.Sequence:
                    fsm = MakeSequenceFSM(stateNames);
                    break;
                case ContentModelType.Choice:
                    fsm = MakeChoiceFSM(stateNames);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return ImplementFSMCardinality(fsm, stateNames);
        }

        private FSM MakeSequenceFSM(StateNameSource stateNames)
        {
            FSM fsm = null;
            Set<int> fsmAccept = null;

            foreach (ContentInfo child in Children)
            {
                FSM currFsm = child.MakeFSM(stateNames);

                if (fsm == null)
                {
                    fsm = currFsm;
                    fsmAccept = currFsm.Accept;
                }
                else
                {
                    int currStart = currFsm.Start;
                    foreach (int oldFinalState in fsmAccept)
                    {
                        FSM.CloneTransitions(currFsm, currStart, fsm, oldFinalState);
                    }

                    fsm.AddTransitions(currFsm);
                    //clear old final states only if the initial state of currFsm is not a final state in currFsm
                    if (!currFsm.Accept.Contains(currStart)) fsmAccept.Clear();
                    Set<int> currAccept = currFsm.Accept;
                    foreach (int state in currAccept) fsmAccept.Add(state);
                }
            }

            return fsm;
        }

        private FSM MakeChoiceFSM(StateNameSource stateNames)
        {
            FSM fsm = null;
            int fsmStart = FSM.InvalidState;
            Set<int> fsmAccept = null;

            foreach (ContentInfo child in Children)
            {
                FSM currFsm = child.MakeFSM(stateNames);

                if (fsm == null)
                {
                    //first node
                    fsm = currFsm;
                    fsmStart = currFsm.Start;
                    fsmAccept = currFsm.Accept;
                }
                else
                {
                    //Merge the start states
                    FSM.CloneTransitions(currFsm, currFsm.Start, fsm, fsmStart);
                    //Copy other transitions
                    fsm.AddTransitions(currFsm);
                    //update final states
                    if (currFsm.IsAccept(currFsm.Start)) fsmAccept.Add(fsmStart);
                    foreach (int state in currFsm.Accept) fsmAccept.Add(state);
                }
            }

            return fsm;
        }
    }

    public partial class ClrPropertyInfo : ClrBasePropertyInfo
    {
        public override FSM MakeFSM(StateNameSource stateNames)
        {
            //Create a simple fsm with (0,(schemaName,1),{1})
            Dictionary<int, Transitions> transitions = new Dictionary<int, Transitions>();
            int start = stateNames.Next();
            int end = stateNames.Next();
            Transitions trans = new Transitions();

            if (this.IsSubstitutionHead)
            {
                foreach (XmlSchemaElement element in SubstitutionMembers)
                {
                    trans.Add(XName.Get(element.QualifiedName.Name, element.QualifiedName.Namespace), end);
                }
            }
            else
            {
                trans.Add(XName.Get(schemaName, PropertyNs), end);
            }

            transitions.Add(start, trans);
            return ImplementFSMCardinality(new FSM(start, new Set<int>(end), transitions), stateNames);
        }
    }

    public partial class ClrWildCardPropertyInfo : ClrBasePropertyInfo
    {
        public override FSM MakeFSM(StateNameSource stateNames)
        {
            Dictionary<int, Transitions> transitions = new Dictionary<int, Transitions>();
            int start = stateNames.Next();
            int end = stateNames.Next();
            transitions.Add(start,
                new Transitions(new SingleTransition(new WildCard(this.Namespaces, this.TargetNamespace), end)));
            FSM fsm = new FSM(start, new Set<int>(end), transitions);

            return ImplementFSMCardinality(fsm, stateNames);
        }
    }

    public class StateNameSource
    {
        private int nextName = 1;
        public int Next() => nextName++;
        public void Reset() => nextName = 1;
    }
}