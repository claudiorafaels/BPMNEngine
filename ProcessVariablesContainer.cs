﻿using Org.Reddragonit.BpmEngine.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Org.Reddragonit.BpmEngine
{
    internal sealed class ProcessVariablesContainer : IVariables
    {
        private List<string> _nulls;
        private Dictionary<string, object> _variables;
        private int _stepIndex;
        private string _elementID;
        private BusinessProcess _process = null;
        private ProcessInstance _processInstance;

        public ProcessVariablesContainer(Dictionary<string,object> props,BusinessProcess process,ProcessInstance processInstance)
        {
            _nulls = new List<string>();
            _variables = (props==null ? new Dictionary<string, object>() : props);
            _stepIndex = -1;
            _process=process;
            _processInstance= processInstance;
        }

        internal ProcessVariablesContainer(string elementID, ProcessInstance processInstance)
        {
            _processInstance= processInstance;
            _process = processInstance.Process;
            _process.WriteLogLine(elementID,LogLevels.Debug,new System.Diagnostics.StackFrame(1,true),DateTime.Now,string.Format("Producing Process Variables Container for element[{0}]", new object[] { elementID }));
            _elementID = elementID;
            _stepIndex = processInstance.State.Path.CurrentStepIndex(elementID);
            _nulls = new List<string>();
            _variables = new Dictionary<string, object>();
            foreach (string str in processInstance.State[elementID])
            {
                _process.WriteLogLine(elementID, LogLevels.Debug, new System.Diagnostics.StackFrame(1, true), DateTime.Now, string.Format("Adding variable {0} to Process Variables Container for element[{1}]", new object[] { str,elementID }));
                _variables.Add(str, processInstance.State[elementID, str]);
            }
        }

        internal void WriteLogLine(LogLevels level,string message)
        {
            if (_processInstance != null)
                _processInstance.WriteLogLine(_elementID, level, new StackFrame(2, true), DateTime.Now, message);
            else if (_process != null)
                _process.WriteLogLine(_elementID, level, new StackFrame(2, true), DateTime.Now, message);
        }

        internal void WriteLogLine(LogLevels level,StackFrame stack, DateTime stamp, string message)
        {
            if (_processInstance!=null)
                _processInstance.WriteLogLine(_elementID, level, stack, stamp, message);
            else if (_process!= null)
                _process.WriteLogLine(_elementID, level, stack, stamp, message);
        }

        public object this[string name]
        {
            get
            {
                object ret = null;
                bool found = false;
                lock (_variables)
                {
                    if (_variables.ContainsKey(name))
                    {
                        found = true;
                        ret = _variables[name];
                    }else if (_nulls.Contains(name))
                    {
                        found = true;
                        ret = null;
                    }
                }
                if (!found && _process != null)
                    ret = _process[name];
                return ret;
            }
            set
            {
                lock (_variables)
                {
                    _variables.Remove(name);
                    if (value == null && !_nulls.Contains(name))
                        _nulls.Add(name);
                    else if (value != null)
                    {
                        _variables.Add(name, value);
                        _nulls.Remove(name);
                    }
                }
            }
        }

        public string[] Keys
        {
            get
            {
                List<string> ret = new List<string>();
                lock (_variables)
                {
                    ret.AddRange(_variables.Keys);
                    ret.AddRange(_nulls);
                }
                return ret.ToArray();
            }
        }

        public string[] FullKeys
        {
            get
            {
                List<string> ret = new List<string>(Keys);
                if (_process!=null)
                {
                    foreach (string key in _process.Keys)
                    {
                        if (!ret.Contains(key))
                            ret.Add(key);
                    }
                }
                return ret.ToArray();
            }
        }
    }
}
