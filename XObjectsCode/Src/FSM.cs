//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;

namespace Xml.Schema.Linq
{
    public class FSM
    {
        internal static int InvalidState = default(int);

        private readonly int startState;
        private readonly Set<int> acceptStates;
        private readonly IDictionary<int, Transitions> trans;

        public FSM(int startState, Set<int> acceptStates,
            IDictionary<int, Transitions> trans)
        {
            this.startState = startState;
            this.acceptStates = acceptStates;
            this.trans = trans;
        }

        public int Start
        {
            get { return startState; }
        }

        public Set<int> Accept
        {
            get { return acceptStates; }
        }

        public IDictionary<int, Transitions> Trans
        {
            get { return trans; }
        }

        public override String ToString()
        {
            return "DFA start=" + startState + "\naccept=" + acceptStates;
        }

        public bool isAccept(int state)
        {
            return this.acceptStates.Contains(state);
        }

        internal void AddTransitions(FSM otherFSM)
        {
            foreach (KeyValuePair<int, Transitions> pair in otherFSM.Trans)
            {
                this.trans.Add(pair);
            }
        }

        internal static void CloneTransitions(FSM srcFsm, int srcState, FSM destFsm, int destState)
        {
            //Clone the transitions from srcState to destState
            Transitions srcTrans = null;
            srcFsm.Trans.TryGetValue(srcState, out srcTrans);
            if (srcTrans == null) return;

            Transitions destTrans = null;
            destFsm.Trans.TryGetValue(destState, out destTrans);

            if (destTrans == null)
            {
                destTrans = new Transitions();
                destFsm.Trans.Add(destState, destTrans);
            }

            destTrans.CloneTransitions(srcTrans, srcState, destState);
        }
    }

    public class SingleTransition
    {
        internal XName nameLabel;
        internal WildCard wcLabel;
        internal int nextState;

        public SingleTransition(XName name, int newState)
        {
            nameLabel = name;
            wcLabel = null;
            nextState = newState;
        }

        public SingleTransition(WildCard wildCard, int newState)
        {
            wcLabel = wildCard;
            nameLabel = null;
            nextState = newState;
        }
    }

    public class Transitions
    {
        internal Dictionary<XName, int> nameTransitions;
        internal Dictionary<WildCard, int> wildCardTransitions;

        internal bool IsEmpty
        {
            get { return Count == 0; }
        }

        internal int Count
        {
            get
            {
                int count = 0;
                if (nameTransitions != null) count += nameTransitions.Count;
                if (wildCardTransitions != null) count += wildCardTransitions.Count;
                return count;
            }
        }

        public Transitions()
        {
        }

        public Transitions(params SingleTransition[] transitions)
        {
            if (transitions != null)
                foreach (SingleTransition st in transitions)
                {
                    if (st.nameLabel != null)
                    {
                        if (nameTransitions == null) nameTransitions = new Dictionary<XName, int>();
                        nameTransitions.Add(st.nameLabel, st.nextState);
                    }
                    else
                    {
                        if (wildCardTransitions == null) wildCardTransitions = new Dictionary<WildCard, int>();
                        wildCardTransitions.Add(st.wcLabel, st.nextState);
                    }
                }
        }

        public Transitions(Dictionary<XName, int> nameTrans,
            Dictionary<WildCard, int> wildCardTrans)
        {
            this.nameTransitions = nameTrans;
            this.wildCardTransitions = wildCardTrans;
        }

        internal static void Add<T>(ref Dictionary<T, int> d, T id, int nextState)
        {
            if (d == null)
            {
                d = new Dictionary<T, int>();
            }

            d[id] = nextState;
        }

        internal void Add(XName name, int nextState)
        {
            Add(ref this.nameTransitions, name, nextState);
        }

        internal void Add(WildCard wildCard, int nextState)
        {
            Add(ref this.wildCardTransitions, wildCard, nextState);
        }


        internal int GetNextState(XName inputSymbol, out XName matchingName, out WildCard matchingWildCard)
        {
            matchingWildCard = null;
            matchingName = null;

            //first try name table, then match any
            int state = FSM.InvalidState;
            if (nameTransitions != null && nameTransitions.TryGetValue(inputSymbol, out state))
            {
                matchingName = inputSymbol;
            }
            else if (wildCardTransitions != null)
            {
                //We need to scan the wildcard dictionary because this is not "equality" based checking
                foreach (KeyValuePair<WildCard, int> pair in wildCardTransitions)
                {
                    if (pair.Key.Allows(inputSymbol))
                    {
                        matchingWildCard = pair.Key;
                        state = pair.Value;
                    }
                }
            }

            return state;
        }

        internal void Clear()
        {
            if (nameTransitions != null) nameTransitions.Clear();
            if (wildCardTransitions != null) wildCardTransitions.Clear();
        }

        internal void CloneTransitions(Transitions otherTransitions, int srcState, int destState)
        {
            bool isEmpty = IsEmpty;
            if (otherTransitions.nameTransitions != null)
            {
                if (nameTransitions == null)
                {
                    nameTransitions = new Dictionary<XName, int>();
                }

                foreach (KeyValuePair<XName, int> pair in otherTransitions.nameTransitions)
                {
                    int nextState = pair.Value;

                    //An optimization: if my transition is empty, even copy self-loop
                    if (isEmpty && (nextState == srcState)) nextState = destState;

                    // This would silently overwrite the value for an existing key
                    // The assumption is due to UPA, if there are two same transitions
                    // they should lead to the same state
                    nameTransitions[pair.Key] = nextState;
                }
            }

            if (otherTransitions.wildCardTransitions != null)
            {
                if (wildCardTransitions == null)
                {
                    wildCardTransitions = new Dictionary<WildCard, int>();
                }

                foreach (KeyValuePair<WildCard, int> pair in otherTransitions.wildCardTransitions)
                {
                    int nextState = pair.Value;

                    //An optimization: if my transition is empty, even copy self-loop
                    if (isEmpty && (nextState == srcState)) nextState = destState;
                    wildCardTransitions[pair.Key] = nextState;
                }
            }
        }
    }

    public class WildCard
    {
        public readonly static WildCard DefaultWildCard = new WildCard("##any", "");

        XObjectsNamespaceList nsList;

        internal XObjectsNamespaceList NsList
        {
            get { return nsList; }
        }

        public WildCard(string namespaces, string targetNamespace)
        {
            if (targetNamespace == null) targetNamespace = "";
            if (namespaces == null) namespaces = "##any";
            this.nsList = new XObjectsNamespaceList(namespaces, targetNamespace);
        }

        public override bool Equals(object obj)
        {
            WildCard symbol = obj as WildCard;

            if (symbol != null) return symbol.NsList.Equals(this.NsList);
            return false;
        }

        public override int GetHashCode()
        {
            return NsList.GetHashCode();
        }

        internal bool Allows(XName symbol)
        {
            return this.NsList.Allows(symbol.Namespace.ToString());
        }

        public override string ToString()
        {
            return "<ANY> : " + this.NsList.ToString();
        }
    }

    public class Set<T> : ICollection<T>
    {
        // Only the keys matter; the type bool used for the value is arbitrary
        private Dictionary<T, bool> dictionary;

        public Set()
        {
            dictionary = new Dictionary<T, bool>();
        }

        public Set(T x) : this()
        {
            Add(x);
        }

        public Set(IEnumerable<T> collection) : this()
        {
            foreach (T x in collection)
                Add(x);
        }

        public Set(T[] array) : this()
        {
            foreach (T x in array) Add(x);
        }

        public bool Contains(T x)
        {
            return dictionary.ContainsKey(x);
        }

        public void Add(T x)
        {
            if (!Contains(x))
                dictionary.Add(x, false);
        }

        public bool Remove(T x)
        {
            return dictionary.Remove(x);
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return dictionary.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { return dictionary.Count; }
        }

        public void CopyTo(T[] arr, int i)
        {
            dictionary.Keys.CopyTo(arr, i);
        }

        public override String ToString()
        {
            StringBuilder str = new StringBuilder();
            str.Append("{ ");
            bool first = true;
            foreach (T x in this)
            {
                if (!first)
                    str.Append(", ");
                str.Append(x);
                first = false;
            }

            str.Append(" }");
            return str.ToString();
        }
    }
}