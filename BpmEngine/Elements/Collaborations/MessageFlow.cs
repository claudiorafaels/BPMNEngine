﻿using BPMNEngine.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace BPMNEngine.Elements.Collaborations
{
    [XMLTag("bpmn","messageFlow")]
    [RequiredAttribute("id")]
    [ValidParent(typeof(Collaboration))]
    internal class MessageFlow : AFlowElement
    {

        public MessageFlow(XmlElement elem, XmlPrefixMap map,AElement parent)
            : base(elem, map,parent) { }
    }
}
