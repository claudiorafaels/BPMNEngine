﻿using Org.Reddragonit.BpmEngine.Attributes;
using Org.Reddragonit.BpmEngine.Elements.Collaborations;
using Org.Reddragonit.BpmEngine.Elements.Processes.Events;
using Org.Reddragonit.BpmEngine.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Org.Reddragonit.BpmEngine.Elements.Processes.Tasks
{
    [ValidParent(typeof(IProcess))]
    internal abstract class ATask : AFlowNode
    {
        public ATask(XmlElement elem, XmlPrefixMap map, AElement parent)
            : base(elem, map, parent) { }

        private static readonly EventSubTypes[] _blockedSubTypes = new EventSubTypes[]
        {
            EventSubTypes.Signal,
            EventSubTypes.Escalation,
            EventSubTypes.Error,
            EventSubTypes.Message,
            EventSubTypes.Link
        };

        public new IEnumerable<string> Outgoing
        {
            get
            {
                return base.Outgoing.Where(str =>
                {
                    bool add = true;
                    IElement afn = this.Definition.LocateElement(str);
                    string destID = null;
                    if (afn is MessageFlow messageFlow)
                        destID = messageFlow.targetRef;
                    else if (afn is SequenceFlow sequenceFlow)
                        destID = sequenceFlow.targetRef;
                    if (destID != null)
                    {
                        IElement ice = this.Definition.LocateElement(destID);
                        if (ice is IntermediateCatchEvent intermediateCatchEvent)
                            add = !intermediateCatchEvent.SubType.HasValue || !_blockedSubTypes.Contains(intermediateCatchEvent.SubType.Value);
                    }
                    return add;
                });
            }
        }
    }
}
