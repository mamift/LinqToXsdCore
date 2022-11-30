using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Xml.Schema.Linq.CodeGen;

public abstract partial class ContentInfo
{
    //Code for generating FSM
    public virtual FSM MakeFSM(StateNameSource stateNames)
    {
        throw new InvalidOperationException();
    }

    public FSM ImplementFSMCardinality(FSM origFsm, StateNameSource stateNames)
    {
        //Based on the occurence, add *,+, or ? semantics
        Debug.Assert(origFsm != null);
        FSM fsm = null;
        switch (this.OccursInSchema)
        {
            case Occurs.OneOrMore:
                fsm = MakePlusFSM(origFsm, stateNames);
                break;
            case Occurs.ZeroOrMore:
                fsm = MakeStarFSM(origFsm, stateNames);
                break;
            case Occurs.ZeroOrOne:
                fsm = MakeQMarkFSM(origFsm, stateNames);
                break;
            default:
                fsm = origFsm;
                break;
        }

        return fsm;
    }

    private FSM MakePlusFSM(FSM origFsm, StateNameSource stateNames)
    {
        //Clone transitions in the initial state into each final state
        int origStart = origFsm.Start;
        foreach (int s in origFsm.Accept)
        {
            if (s != origStart) FSM.CloneTransitions(origFsm, origStart, origFsm, s);
        }

        return origFsm;
    }

    private FSM MakeStarFSM(FSM origFsm, StateNameSource stateNames)
    {
        int start = origFsm.Start;
        Set<int> visited = new Set<int>();
        TransformToStar(start, start, origFsm, visited);
        origFsm.Accept.Add(start);

        return origFsm;
    }

    private void TransformToStar<T>(
        Dictionary<T, int> transitions,
        int startState,
        int currentState,
        FSM fsm,
        Set<int> visited)
    {
        if (transitions != null)
        {
            var toReroute = new List<T>();
            foreach (var transition in transitions)
            {
                int nextState = transition.Value;
                bool hasNextStates =
                    currentState != nextState &&
                    this.HasNextStates(nextState, fsm);
                if (fsm.isAccept(nextState))
                {
                    if (hasNextStates)
                    {
                        // see http://linqtoxsd.codeplex.com/WorkItem/View.aspx?WorkItemId=4083
                        if (!visited.Contains(nextState))
                        {
                            this.SimulatePlusQMark(fsm, startState, nextState);
                        }
                    }
                    else
                    {
                        toReroute.Add(transition.Key);
                    }
                }

                if (hasNextStates)
                {
                    this.TransformToStar(startState, nextState, fsm, visited);
                }
            }

            foreach (var id in toReroute)
            {
                transitions[id] = startState;
            }
        }
    }

    private void TransformToStar(int start, int currState, FSM fsm, Set<int> visited)
    {
        if (visited.Contains(currState)) return;
        else visited.Add(currState);

        Transitions currTrans = null;
        fsm.Trans.TryGetValue(currState, out currTrans);
        if (currTrans == null || currTrans.Count == 0) return;

        this.TransformToStar(
            currTrans.nameTransitions, start, currState, fsm, visited);
        this.TransformToStar(
            currTrans.wildCardTransitions, start, currState, fsm, visited);
    }

    private void SimulatePlusQMark(FSM fsm, int start, int currState)
    {
        //Simulate * using Plus and QMark
        if (currState != start)
        {
            FSM.CloneTransitions(fsm, start, fsm, currState);
        }
    }

    private bool HasNextStates(int state, FSM fsm)
    {
        Transitions currTrans = null;
        fsm.Trans.TryGetValue(state, out currTrans);
        if (currTrans == null || currTrans.Count == 0) return false;
        return true;
    }

    private FSM MakeQMarkFSM(FSM origFsm, StateNameSource stateNames)
    {
        //Change the start state to the final states
        origFsm.Accept.Add(origFsm.Start);
        return origFsm;
    }
}