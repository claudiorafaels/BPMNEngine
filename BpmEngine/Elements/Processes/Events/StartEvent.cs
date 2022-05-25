﻿using Org.Reddragonit.BpmEngine.Attributes;
using Org.Reddragonit.BpmEngine.Elements.Processes.Conditions;
using Org.Reddragonit.BpmEngine.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Org.Reddragonit.BpmEngine.Elements.Processes.Events
{
    [XMLTag("bpmn","startEvent")]
    internal class StartEvent : AEvent
    {
        public StartEvent(XmlElement elem, XmlPrefixMap map,AElement parent)
            : base(elem, map,parent) { }

        internal bool IsEventStartValid(IReadonlyVariables variables, IsEventStartValid isEventStartValid)
        {
            if (ExtensionElement != null)
            {
                ExtensionElements ee = (ExtensionElements)ExtensionElement;
                if (ee.Children != null)
                {
                    foreach (IElement ie in ee.Children)
                    {
                        if (ie is ConditionSet)
                        {
                            if (!((ConditionSet)ie).Evaluate(variables))
                                return false;
                        }
                    }
                }
            }
            return isEventStartValid(this, variables);
        }

        public override bool IsValid(out string[] err)
        {
            if (Incoming != null)
            {
                if (Incoming.Length > 0 && !SubType.HasValue)
                {
                    err = new string[] { "Start Events cannot have an incoming path." };
                    return false;
                }
            }
            if (Outgoing == null)
            {
                err = new string[] { "Start Events must have an outgoing path." };
                return false;
            }else if (Outgoing.Length > 1)
            {
                err = new string[] { "Start Events can only have 1 outgoing path." };
                return false;
            }
            return base.IsValid(out err);
        }
    }
}
