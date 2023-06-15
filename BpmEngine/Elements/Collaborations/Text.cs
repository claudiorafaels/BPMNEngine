﻿using BPMNEngine.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace BPMNEngine.Elements.Collaborations
{
    [XMLTag("bpmn", "text")]
    [ValidParent(typeof(TextAnnotation))]
    internal class Text : AElement
    {

        public string Value =>  Element.InnerText;
        public Text(XmlElement elem, XmlPrefixMap map, AElement parent) : base(elem, map, parent)
        {
        }
    }
}
