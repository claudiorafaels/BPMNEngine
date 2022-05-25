﻿using Org.Reddragonit.BpmEngine.Elements.Collaborations;
using Org.Reddragonit.BpmEngine.Elements.Processes;
using Org.Reddragonit.BpmEngine.Elements.Processes.Events;
using Org.Reddragonit.BpmEngine.Elements.Processes.Gateways;
using Org.Reddragonit.BpmEngine.Elements.Processes.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Org.Reddragonit.BpmEngine.Interfaces;
using System.Threading;
using System.Globalization;
using Org.Reddragonit.BpmEngine.Elements;

namespace Org.Reddragonit.BpmEngine.State
{
    internal sealed class ProcessPath : AStateContainer
    {
        private const string _PATH_ENTRY_ELEMENT = "sPathEntry";
        private const string _STEP_STATUS = "status";
        private const string _ELEMENT_ID = "elementID";
        private const string _START_TIME = "startTime";
        private const string _END_TIME = "endTime";
        private const string _OUTGOING_ID = "outgoingID";
        private const string _INCOMING_ID = "incomingID";
        private const string _OUTGOING_ELEM = "outgoing";
        private const string _COMPLETED_BY = "CompletedByID";

        protected override string _ContainerName
        {
            get
            {
                return "ProcessPath";
            }
        }

        private ProcessStepComplete _complete;
        private ProcessStepError _error;
        private processStateChanged _stateChanged;

        private int _lastStep;
        internal int LastStep { get { return _lastStep; } }

        public ProcessPath(ProcessStepComplete complete, ProcessStepError error, processStateChanged stateChanged, ProcessState state)
            : base(state)
        {
            _complete = complete;
            _error = error;
            _stateChanged = stateChanged;
            _lastStep = int.MaxValue;
        }

        private string[] _ExtractOutgoing(XmlElement elem)
        {
            if (elem.Attributes[_OUTGOING_ID] != null)
                return new string[] { elem.Attributes[_OUTGOING_ID].Value };
            else
            {
                List<string> tmp = new List<string>();
                foreach (XmlNode n in elem.ChildNodes)
                {
                    if (n.NodeType == XmlNodeType.Element)
                    {
                        if (n.Name == _OUTGOING_ELEM)
                            tmp.Add(n.Value);
                    }
                }
                return (tmp.Count == 0 ? null : tmp.ToArray());
            }
        }

        internal sSuspendedStep[] ResumeSteps
        {
            get
            {
                List<sSuspendedStep> ret = new List<sSuspendedStep>();
                foreach (XmlElement elem in ChildNodes)
                {
                    switch ((StepStatuses)Enum.Parse(typeof(StepStatuses),elem.Attributes[_STEP_STATUS].Value))
                    {
                        case StepStatuses.Suspended:
                            bool add = true;
                            foreach (sSuspendedStep ss in ret)
                            {
                                if (ss.ElementID == elem.Attributes[_ELEMENT_ID].Value)
                                {
                                    add = false;
                                    break;
                                }
                            }
                            if (add)
                                ret.Add(new sSuspendedStep(elem.Attributes[_INCOMING_ID].Value,elem.Attributes[_ELEMENT_ID].Value));
                            break;
                        case StepStatuses.Succeeded:
                        case StepStatuses.Failed:
                        case StepStatuses.Waiting:
                        case StepStatuses.Aborted:
                            for(int x = 0; x < ret.Count; x++)
                            {
                                if (ret[x].ElementID == elem.Attributes[_ELEMENT_ID].Value)
                                {
                                    ret.RemoveAt(x);
                                    break;
                                }
                            }
                            string[] outgoing = _ExtractOutgoing(elem);
                            if (outgoing != null)
                            {
                                foreach (string str in outgoing)
                                {
                                    bool addNext = true;
                                    foreach (sSuspendedStep ss in ret)
                                    {
                                        if (ss.ElementID == str)
                                        {
                                            addNext = false;
                                            break;
                                        }
                                    }
                                    if (addNext)
                                        ret.Add(new sSuspendedStep(elem.Attributes[_ELEMENT_ID].Value, str));
                                }
                            }
                            break;
                    }
                }
                return ret.ToArray();
            }
        }

        internal sDelayedStartEvent[] DelayedEvents
        {
            get
            {
                List<sDelayedStartEvent> ret = new List<sDelayedStartEvent>();
                foreach (XmlElement elem in ChildNodes)
                {
                    switch ((StepStatuses)Enum.Parse(typeof(StepStatuses), elem.Attributes[_STEP_STATUS].Value))
                    {
                        case StepStatuses.WaitingStart:
                            ret.Add(new sDelayedStartEvent(elem.Attributes[_INCOMING_ID].Value, elem.Attributes[_ELEMENT_ID].Value, DateTime.ParseExact(elem.Attributes[_START_TIME].Value,Constants.DATETIME_FORMAT,CultureInfo.InvariantCulture)));
                            break;
                        case StepStatuses.Succeeded:
                        case StepStatuses.Failed:
                        case StepStatuses.Waiting:
                        case StepStatuses.Aborted:
                            for (int x = 0; x < ret.Count; x++)
                            {
                                if (ret[x].ElementID == elem.Attributes[_ELEMENT_ID].Value)
                                {
                                    ret.RemoveAt(x);
                                    break;
                                }
                            }
                            break;
                    }
                }
                return ret.ToArray();
            }
        }

        internal bool IsStepWaiting(string id, int stepIndex)
        {
            bool ret = true;
            XmlElement[] nodes = ChildNodes;
            for (int x = stepIndex+1; x < nodes.Length; x++)
            {
                if (nodes[x].Attributes[_ELEMENT_ID].Value == id)
                {
                    if ((StepStatuses)Enum.Parse(typeof(StepStatuses), (nodes[x].Attributes[_STEP_STATUS].Value)) == StepStatuses.Succeeded)
                    {
                        ret = false;
                        break;
                    }
                }
            }
            return ret;
        }

        public StepStatuses GetStatus(string elementid)
        {
            StepStatuses ret = StepStatuses.NotRun;
            XmlElement[] nodes = ChildNodes;
            for (int x = 0; x < Math.Min(nodes.Length,_lastStep);x++ )
            {
                if (nodes[x].Attributes[_ELEMENT_ID].Value == elementid)
                    ret = (StepStatuses)Enum.Parse(typeof(StepStatuses),nodes[x].Attributes[_STEP_STATUS].Value);
            }
            return ret;
        }

        public int GetStepSuccessCount(string elementid)
        {
            int ret = 0;
            XmlElement[] nodes = ChildNodes;
            for (int x = 0; x < Math.Min(nodes.Length, _lastStep); x++)
            {
                if (nodes[x].Attributes[_ELEMENT_ID].Value == elementid)
                    ret += ((StepStatuses)Enum.Parse(typeof(StepStatuses), nodes[x].Attributes[_STEP_STATUS].Value) == StepStatuses.Succeeded ? 1 : 0);
            }
            return ret;
        }

        public int CurrentStepIndex(string elementid)
        {
            int ret = -1;
            XmlElement[] nodes = ChildNodes;
            for (int x = 0; x < Math.Min(nodes.Length, _lastStep); x++)
            {
                if (nodes[x].Attributes[_ELEMENT_ID].Value == elementid)
                    ret = x;
            }
            return ret;
        }

        internal void StartAnimation()
        {
            _lastStep = -1;
        }

        internal string MoveToNextStep()
        {
            _lastStep++;
            XmlElement[] nodes = ChildNodes;
            return ((_lastStep==0 || (_lastStep>nodes.Length)) ? null : nodes[_lastStep-1].Attributes[_ELEMENT_ID].Value);
        }

        internal bool HasNext()
        {
            return ChildNodes.Length+1 > _lastStep;
        }

        internal void FinishAnimation()
        {
            _lastStep = int.MaxValue;
        }

        private void _addPathEntry(string elementID, string incomingID, StepStatuses status, DateTime start, DateTime end)
        {
            _addPathEntry(elementID, incomingID, null, status, start, end, null);
        }

        private void _addPathEntry(string elementID, string incomingID, StepStatuses status, DateTime start)
        {
            _addPathEntry(elementID, incomingID, null, status, start, null, null);
        }

        private void _addPathEntry(string elementID, string incomingID, string[] outgoingID, StepStatuses status, DateTime start, DateTime end)
        {
            _addPathEntry(elementID, incomingID, outgoingID, status, start, end, null);
        }

        private void _addPathEntry(string elementID, string incomingID, string outgoingID, StepStatuses status, DateTime start, DateTime end)
        {
            _addPathEntry(elementID, incomingID, new string[] { outgoingID }, status, start, end, null);
        }

        private void _addPathEntry(string elementID, string incomingID, string[] outgoingID, StepStatuses status, DateTime start, DateTime? end, string completedBy)
        {
            XmlElement elem = _ProduceElement(_PATH_ENTRY_ELEMENT);
            _SetAttribute(elem, _ELEMENT_ID, elementID);
            _SetAttribute(elem,_STEP_STATUS,status.ToString());
            _SetAttribute(elem, _START_TIME, start.ToString(Constants.DATETIME_FORMAT));
            if (incomingID != null)
                _SetAttribute(elem, _INCOMING_ID, incomingID);
            if (end.HasValue)
                _SetAttribute(elem, _END_TIME, end.Value.ToString(Constants.DATETIME_FORMAT));
            if (completedBy != null)
                _SetAttribute(elem, _COMPLETED_BY, completedBy);
            if (outgoingID != null)
            {
                if (outgoingID.Length == 1)
                    _SetAttribute(elem, _OUTGOING_ID, outgoingID[0]);
                else
                {
                    foreach (string str in outgoingID)
                    {
                        elem.AppendChild(_ProduceElement(_OUTGOING_ELEM));
                        elem.ChildNodes[elem.ChildNodes.Count - 1].InnerText = str;
                    }
                }
            }
            //Console.WriteLine(string.Format("Adding Path Entry for {0}-{1}", elementID,status));
            _AppendElement(elem);
            _stateChanged();
        }

        private void _GetIncomingIDAndStart(string elementID,out DateTime start,out string incoming)
        {
            start = DateTime.Now;
            incoming = null;
            XmlElement[] nodes = ChildNodes;
            for (int x = nodes.Length - 1; x >= 0; x--)
            {
                if (nodes[x].Attributes[_ELEMENT_ID].Value == elementID && nodes[x].Attributes[_STEP_STATUS].Value == StepStatuses.Waiting.ToString())
                {
                    incoming = (nodes[x].Attributes[_INCOMING_ID] == null ? null : nodes[x].Attributes[_INCOMING_ID].Value);
                    start = DateTime.ParseExact(nodes[x].Attributes[_START_TIME].Value, Constants.DATETIME_FORMAT, CultureInfo.InvariantCulture);
                    break;
                }
            }
        }

        internal void DelayEventStart(AEvent Event,string incoming,TimeSpan delay)
        {
            _WriteLogLine(Event.id, LogLevels.Debug, "Delaying start of event in Process Path");
            _addPathEntry(Event.id, incoming, StepStatuses.WaitingStart, DateTime.Now.Add(delay));
        }

        internal void StartEvent(AEvent Event, string incoming)
        {
            _WriteLogLine(Event.id,LogLevels.Debug,"Starting Event in Process Path");
            _addPathEntry(Event.id,incoming,StepStatuses.Waiting, DateTime.Now);
        }
        private async void _Complete(string incoming, string outgoing)
        {
            await System.Threading.Tasks.Task.Run(() => _complete.Invoke(incoming, outgoing));
        }

        private async void _Error(IElement step, Exception ex)
        {
            await System.Threading.Tasks.Task.Run(() => _error.Invoke(step,ex));
        }

        internal void SucceedEvent(AEvent Event)
        {
            _WriteLogLine(Event.id,LogLevels.Debug,"Succeeding Event in Process Path");
            string incoming;
            DateTime start;
            _GetIncomingIDAndStart(Event.id, out start, out incoming);
            string[] outgoing = (Event is IntermediateThrowEvent ? ((IntermediateThrowEvent)Event).Outgoing : Event.Outgoing);
            if (outgoing == null)
            {
                _addPathEntry(Event.id,incoming,StepStatuses.Succeeded, start, DateTime.Now);
                _Complete(Event.id, null);
            }
            else
            {
                _addPathEntry(Event.id, incoming, outgoing, StepStatuses.Succeeded, start, DateTime.Now);
                foreach (string id in outgoing)
                    _Complete(Event.id, id);
            }
        }

        internal void FailEvent(AEvent Event)
        {
            _WriteLogLine(Event.id, LogLevels.Debug, "Failing Event in Process Path");
            string incoming;
            DateTime start;
            _GetIncomingIDAndStart(Event.id, out start, out incoming);
            _addPathEntry(Event.id,incoming,StepStatuses.Failed, start, DateTime.Now);
            _Error(Event, null);
        }

        internal void StartSubProcess(SubProcess SubProcess, string incoming)
        {
            _WriteLogLine(SubProcess.id, LogLevels.Debug, "Starting SubProcess in Process Path");
            _addPathEntry(SubProcess.id, incoming, StepStatuses.Waiting, DateTime.Now);
        }

        internal void SucceedSubProcess(SubProcess SubProcess)
        {
            _WriteLogLine(SubProcess.id, LogLevels.Debug, "Succeeding SubProcess in Process Path");
            string incoming;
            DateTime start;
            _GetIncomingIDAndStart(SubProcess.id, out start, out incoming);
            string[] outgoing = SubProcess.Outgoing;
            if (outgoing == null)
            {
                _addPathEntry(SubProcess.id, incoming, StepStatuses.Succeeded, start, DateTime.Now);
                _Complete(SubProcess.id, null);
            }
            else
            {
                _addPathEntry(SubProcess.id, incoming, outgoing, StepStatuses.Succeeded, start, DateTime.Now);
                foreach (string id in outgoing)
                    _Complete(SubProcess.id, id);
            }
        }

        internal void FailSubProcess(SubProcess SubProcess)
        {
            _WriteLogLine(SubProcess.id, LogLevels.Debug, "Failing SubProcess in Process Path");
            string incoming;
            DateTime start;
            _GetIncomingIDAndStart(SubProcess.id, out start, out incoming);
            _addPathEntry(SubProcess.id, incoming, StepStatuses.Failed, start, DateTime.Now);
            _Error(SubProcess, null);
        }

        internal void ProcessMessageFlow(MessageFlow flow)
        {
            _WriteLogLine(flow.id, LogLevels.Debug, "Processing Message Flow in Process Path");
            _addPathEntry(flow.id, flow.sourceRef, flow.targetRef, StepStatuses.Succeeded, DateTime.Now, DateTime.Now);
            _Complete(flow.id, flow.targetRef);
        }

        internal void ProcessSequenceFlow(SequenceFlow flow)
        {
            _WriteLogLine(flow.id, LogLevels.Debug, "Processing Sequence Flow in Process Path");
            _addPathEntry(flow.id, flow.sourceRef,  flow.targetRef, StepStatuses.Succeeded, DateTime.Now, DateTime.Now);
            _Complete(flow.id, flow.targetRef);
        }

        internal void StartTask(ATask task, string incoming)
        {
            _WriteLogLine(task.id, LogLevels.Debug, "Starting Task in Process Path");
            _addPathEntry(task.id,incoming, StepStatuses.Waiting, DateTime.Now);
        }

        internal void FailTask(ATask task, Exception ex)
        {
            _WriteLogLine(task.id, LogLevels.Debug, "Failing Task in Process Path");
            string incoming;
            DateTime start;
            _GetIncomingIDAndStart(task.id, out start, out incoming);
            _addPathEntry(task.id,incoming,StepStatuses.Failed, start, DateTime.Now);
            _Error(task, ex);
        }

        internal void SucceedTask(UserTask task,string completedByID)
        {
            _WriteLogLine(task.id, LogLevels.Debug, string.Format("Succeeding Task in Process Path with Completed By ID {0}", new object[] { completedByID }));
            _SucceedTask(task, completedByID);
        }

        internal void SucceedTask(ATask task)
        {
            _WriteLogLine(task.id, LogLevels.Debug, "Succeeding Task in Process Path");
            _SucceedTask(task, null);
        }

        private void _SucceedTask(ATask task, string completedByID)
        {
            string incoming;
            DateTime start;
            _GetIncomingIDAndStart(task.id, out start, out incoming);
            _addPathEntry(task.id, incoming, task.Outgoing, StepStatuses.Succeeded, start, DateTime.Now, (task is UserTask ? completedByID : null));
            _Complete(task.id, (task.Outgoing == null ? null : task.Outgoing[0]));
        }

        internal void StartGateway(AGateway gateway, string incoming)
        {
            _WriteLogLine(gateway.id, LogLevels.Debug, "Starting Gateway in Process Path");
            _addPathEntry(gateway.id,incoming,StepStatuses.Waiting, DateTime.Now);
        }

        internal void FailGateway(AGateway gateway)
        {
            _WriteLogLine(gateway.id, LogLevels.Debug, "Failing Gateway in Process Path");
            string incoming;
            DateTime start;
            _GetIncomingIDAndStart(gateway.id, out start, out incoming);
            _addPathEntry(gateway.id, incoming, StepStatuses.Failed, start, DateTime.Now);
            _Error(gateway,null);
        }

        internal void SuccessGateway(AGateway gateway, string[] chosenExits)
        {
            _WriteLogLine(gateway.id, LogLevels.Debug, "Succeeding Gateway in Process Path");
            string incoming;
            DateTime start;
            _GetIncomingIDAndStart(gateway.id, out start, out incoming);
            _addPathEntry(gateway.id, incoming, chosenExits, StepStatuses.Succeeded, start, DateTime.Now);
            foreach (string outgoing in chosenExits)
                _Complete(gateway.id, outgoing);
        }

        internal void SuspendElement(string sourceID, IElement elem)
        {
            _WriteLogLine(elem.id, LogLevels.Debug, "Suspending Element in Process Path");
            _addPathEntry(elem.id,sourceID,StepStatuses.Suspended, DateTime.Now);
        }

        internal void AbortStep(string sourceID,string attachedToID)
        {
            _WriteLogLine(attachedToID, LogLevels.Debug, "Aborting Process Step");
            _addPathEntry(attachedToID, sourceID, StepStatuses.Aborted, DateTime.Now);

        }
    }
}
