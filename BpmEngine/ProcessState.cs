﻿using Org.Reddragonit.BpmEngine.State;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

namespace Org.Reddragonit.BpmEngine
{
    internal sealed class ProcessState
    {
        private const string _BASE_STATE = "<?xml version=\"1.0\" encoding=\"utf-8\"?><ProcessState isSuspended=\"False\"></ProcessState>";

        private const string _PROCESS_STATE_ELEMENT = "ProcessState";

        private AutoResetEvent _evnt;
        private XmlDocument _doc;
        private XmlElement _stateElement;
        private LogContainer _log;
        private SuspendedStepContainer _suspensions;
        private StateVariableContainer _variables;

        private ProcessPath _path;
        internal ProcessPath Path { get { return _path; } }

        internal void SuspendStep(string elementID, TimeSpan span)
        {
            _process.WriteLogLine(elementID, LogLevels.Debug, new StackFrame(1, true), DateTime.Now, string.Format("Suspending Step for {0}", new object[] { span }));
            _suspensions.SuspendStep(elementID, _path.CurrentStepIndex(elementID), span);
            _stateChanged();
        }

        internal IEnumerable<sStepSuspension> SuspendedSteps
            => _suspensions.Steps
            .Where(ss => _path.IsStepWaiting(ss.Id, ss.StepIndex));

        internal bool IsSuspended { get { return (_stateElement.Attributes["isSuspended"]==null ? false : bool.Parse(_stateElement.Attributes["isSuspended"].Value)); }
            private set
            {
                if (_stateElement.Attributes["isSuspended"] != null)
                    _stateElement.Attributes.Append(_doc.CreateAttribute("isSuspended"));
                _stateElement.Attributes["isSuspended"].Value = value.ToString();
            }
        }

        internal IEnumerable<sSuspendedStep> ResumeSteps
            => !IsSuspended ? new sSuspendedStep[] { } : _path.ResumeSteps;

        internal IEnumerable<sDelayedStartEvent> DelayedEvents => _path.DelayedEvents;

        internal IEnumerable<string> ActiveSteps => _path.ActiveSteps;
        
        internal object this[string elementID, string variableName]
        {
            get
            {
                object ret = null;
                int stepIndex =-1;
                if (elementID == null)
                    stepIndex = _path.LastStep;
                else
                    stepIndex = _path.CurrentStepIndex(elementID);
                if (elementID!=null && stepIndex==-1)
                    stepIndex=_path.LastStep;
                ret = _variables[variableName, stepIndex];
                return ret;
            }
            set
            {
                _variables[variableName, _path.CurrentStepIndex(elementID)] = value;
                _stateChanged();
            }
        }

        internal IEnumerable<string> this[string elementID]
        {
            get
            {
                List<string> ret = new List<string>();
                int stepIndex = -1;
                if (elementID == null)
                    stepIndex = _path.LastStep;
                else
                    stepIndex = _path.CurrentStepIndex(elementID);
                if (elementID!=null && stepIndex==-1)
                    stepIndex=_path.LastStep;
                return _variables[stepIndex];
            }
        }

        private readonly OnStateChange _onStateChange;
        private readonly BusinessProcess _process;
        internal BusinessProcess Process { get { return _process; } }

        internal ProcessState(BusinessProcess process,ProcessStepComplete complete, ProcessStepError error,OnStateChange onStateChange)
        {
            _process = process;
            _evnt = new AutoResetEvent(true);
            _doc = new XmlDocument();
            _doc.LoadXml(_BASE_STATE);
            _stateElement = (XmlElement)_doc.GetElementsByTagName(_PROCESS_STATE_ELEMENT)[0];
            _log = new LogContainer(this);
            _variables = new StateVariableContainer(this);
            _suspensions = new SuspendedStepContainer(this);
            _path = new ProcessPath(complete, error, new processStateChanged(_stateChanged),this);
            _onStateChange = onStateChange;
        }

        internal bool Load(XmlDocument doc)
        {
            if (doc.GetElementsByTagName(_PROCESS_STATE_ELEMENT).Count == 0)
                return false;
            _evnt.WaitOne();
            try
            {
                _doc.LoadXml(doc.OuterXml);
                _stateElement = (XmlElement)_doc.GetElementsByTagName(_PROCESS_STATE_ELEMENT)[0];
            }catch(Exception e)
            {
                _process.WriteLogException((string)null,new StackFrame(1,true),DateTime.Now,e);
                _evnt.Set();
                return false;
            }
            _evnt.Set();
            _stateChanged();
            return true;
        }

        public XmlDocument Document
        {
            get
            {
                XmlDocument ret = new XmlDocument();
                _evnt.WaitOne();
                ret.LoadXml(_doc.OuterXml);
                _evnt.Set();
                return ret;
            }
        }

        private void _stateChanged()
        {
            if (_onStateChange != null)
            {
                XmlDocument doc = this.Document;
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        _onStateChange(doc);
                    }catch(Exception ex)
                    {
                        _process.WriteLogException((string)null, new StackFrame(2, true), DateTime.Now, ex);
                    }
                });
            }
        }

        internal void Suspend()
        {
            _process.WriteLogLine((string)null,LogLevels.Debug,new StackFrame(1,true),DateTime.Now,"Suspending Process State");
            IsSuspended = true;
        }

        internal void Resume()
        {
            _process.WriteLogLine((string)null, LogLevels.Debug, new StackFrame(1, true), DateTime.Now, "Resuming Process State");
            IsSuspended = false;
        }

        internal void LogLine(string elementID,AssemblyName assembly, string fileName, int lineNumber, LogLevels level, DateTime timestamp, string message)
        {
            _log.LogLine(elementID, assembly, fileName, lineNumber, level, timestamp, message);
        }

        internal void LogException(string elementID, AssemblyName assembly, string fileName, int lineNumber, DateTime timestamp, Exception exception)
        {
            _log.LogException(elementID, assembly, fileName, lineNumber, timestamp, exception);
        }

        #region StateContainerDelegates
        private XmlElement _FindContainer(string containerName)
        {
            XmlElement result = _doc.ChildNodes.Cast<XmlNode>()
                .Traverse(n=>n.ChildNodes.Cast<XmlNode>())
                .OfType<XmlElement>()
                .FirstOrDefault(elem=>elem.Name == containerName);
            if (result==null)
            {
                result = _doc.CreateElement(containerName);
                _stateElement.AppendChild(result);
            }
            return result;
        }
        internal XmlElement[] GetChildNodes(string containerName)
        {
            var result = Array.Empty<XmlElement>();
            _evnt.WaitOne();
            XmlElement cont = _FindContainer(containerName);
            result = cont.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().ToArray();
            _evnt.Set();
            return result;
        }
        internal void InsertBefore(string containerName, XmlElement element,XmlElement child)
        {
            _evnt.WaitOne();
            _FindContainer(containerName).InsertBefore(element, child);
            _evnt.Set();
        }
        internal void SetAttribute(XmlElement elem, string name, string value)
        {
            XmlAttribute att = _doc.CreateAttribute(name);
            att.Value = value;
            elem.Attributes.Append(att);
        }
        internal void AppendElement(string ContainerName, XmlElement element)
        {
            _evnt.WaitOne();
            _FindContainer(ContainerName).AppendChild(element);
            _evnt.Set();
        }
        internal XmlElement CreateElement(string elementName)
        {
            return _doc.CreateElement(elementName);
        }
        internal XmlElement EncodeFile(sFile file)
        {
            return file.ToElement(_doc);
        }
        internal XmlCDataSection EncodeCData(string content)
        {
            return _doc.CreateCDataSection(content);
        }
        internal void AppendValue(string containerName, string value)
        {
            _evnt.WaitOne();
            XmlElement elem = _FindContainer(containerName);
            if (elem.ChildNodes.Count > 0)
            {
                value = ((XmlCDataSection)elem.ChildNodes[0]).Data + value;
                elem.RemoveChild(elem.ChildNodes[0]);
            }
            
            XmlCDataSection cda = _doc.CreateCDataSection(value);
            elem.AppendChild(cda);
            _evnt.Set();
        }

        #endregion
    }
}
