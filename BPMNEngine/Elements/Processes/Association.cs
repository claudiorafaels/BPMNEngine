﻿using BPMNEngine.Attributes;
using BPMNEngine.Interfaces.Elements;

namespace BPMNEngine.Elements.Processes
{
    [XMLTag("bpmn","association")]
    [RequiredAttribute("id")]
    [ValidParent(typeof(IProcess))]
    internal class Association : AFlowElement
    {
        public Association(XmlElement elem, XmlPrefixMap map,AElement parent)
            : base(elem, map,parent) { }
    }
}
