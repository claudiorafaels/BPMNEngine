﻿using Microsoft.Maui.Graphics;
using BPMNEngine.Attributes;
using BPMNEngine.Drawing;
using BPMNEngine.State;

namespace BPMNEngine.Elements.Diagrams
{
    [XMLTag("bpmndi","BPMNPlane")]
    [RequiredAttribute("id")]
    [ValidParent(typeof(Diagram))]
    internal class Plane : ADiagramElement, IRenderingElement
    {
        public Plane(XmlElement elem, XmlPrefixMap map, AElement parent)
            : base(elem, map, parent) { }

        private RectF? _rectangle;
        public override RectF Rectangle
        {
            get
            {
                if (_rectangle==null)
                    Children.OfType<ADiagramElement>().ForEach(ade => _rectangle=MergeRectangle(ade.Rectangle, _rectangle));
                return _rectangle.Value;
            }
        }

        public override bool IsValid(out IEnumerable<string> err)
        {
            var res = base.IsValid(out err);
            if (!Children.Any())
            {
                err =(err ?? Array.Empty<string>()).Concat(new string[] { "No child elements to render." });
                return false;
            }
            return res;
        }

        public void Render(ICanvas surface, ProcessPath path, Definition definition)
        {
            Children.OfType<IRenderingElement>().ForEach(ire => ire.Render(surface, path, definition));
        }
    }
}
