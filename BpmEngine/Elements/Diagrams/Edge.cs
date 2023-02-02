﻿using Org.Reddragonit.BpmEngine.Attributes;
using Org.Reddragonit.BpmEngine.Drawing;
using Org.Reddragonit.BpmEngine.Drawing.Wrappers;
using Org.Reddragonit.BpmEngine.Elements.Collaborations;
using Org.Reddragonit.BpmEngine.Elements.Processes;
using Org.Reddragonit.BpmEngine.Elements.Processes.Gateways;
using Org.Reddragonit.BpmEngine.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Org.Reddragonit.BpmEngine.Elements.Diagrams
{
    [XMLTag("bpmndi","BPMNEdge")]
    [RequiredAttribute("id")]
    [ValidParent(typeof(Plane))]
    internal class Edge : ADiagramElement
    {
        public IEnumerable<Point> Points => Children
            .Where(elem => elem is Waypoint)
            .Select(elem => ((Waypoint)elem).Point);

        private Rectangle _rectangle = null;
        public Rectangle Rectangle{
            get
            {
                if (_rectangle == null)
                {
                    _rectangle=new Rectangle(0, 0, 0, 0);
                    Point? previous = null;
                    foreach (Point p in Points)
                    {
                        if (previous!=null)
                            _rectangle = new Rectangle(previous!, p).Merge(_rectangle);
                        previous=p;
                    }
                    Label l = Label;
                    _rectangle = new Rectangle(_rectangle.X-3.5f, _rectangle.Y-3.5f, _rectangle.Width+6.5f, _rectangle.Height+6.5f);
                    if (l != null)
                        _rectangle = _rectangle.Merge(l.Bounds.Rectangle);
                }
                return _rectangle;
            }
        }

        public Label Label => (Label)Children
            .FirstOrDefault(elem => elem is Label);
        
        public Edge(XmlElement elem, XmlPrefixMap map, AElement parent)
            : base(elem, map, parent) { }

        public Pen ConstructPen(SolidBrush brush, Definition definition)
        {
            Pen ret = new Pen(brush, Constants.PEN_WIDTH);
            IElement elem = _GetLinkedElement(definition);
            if (elem != null&&(elem is Association || elem is MessageFlow))
                ret.DashPattern = Constants.DASH_PATTERN;
            return ret;
        }

        internal void AppendEnds(Image img, SolidBrush brush, Definition definition)
        {
            Pen p = ConstructPen(brush, definition);
            IElement elem = _GetLinkedElement(definition);
            if (elem != null)
            {
                Point[] points = Points.ToArray();
                if (elem is MessageFlow)
                {
                    img.FillEllipse(brush, new Rectangle(points[0].X - 0.5f, points[0].Y - 0.5f,1.5f, 1.5f));
                    _GenerateTriangle(img, brush, points[points.Length - 1],points[points.Length-2]);
                }
                else
                    _GenerateTriangle(img, brush, points[points.Length - 1], points[points.Length - 2]);
                if (elem is SequenceFlow || elem is MessageFlow)
                {
                    string sourceRef = (elem is SequenceFlow ? ((SequenceFlow)elem).sourceRef : ((MessageFlow)elem).sourceRef);
                    IElement gelem = definition.LocateElement(sourceRef);
                    if ((gelem is AGateway) && (((AGateway)gelem).Default??"")==elem.id)
                    {
                        Point centre = new Point(
                            (0.5f*points[0].X)+(0.5f*points[1].X),
                            (0.5f * points[0].Y) + (0.5f * points[1].Y)
                        );
                        img.DrawLine(p, new Point(centre.X-3f, centre.Y-3f), new Point(centre.X+3f, centre.Y+3f));
                    }
                }
            }
        }

        private static readonly float _baseTLength = Constants.PEN_WIDTH*1.5f;

        private void _GenerateTriangle(Image gp, SolidBrush brush, Point end,Point start)
        {
            float d = (float)Math.Sqrt(Math.Pow((double)end.X - (double)start.X, 2) + Math.Pow((double)end.Y - (double)start.Y, 2));
            float t = _baseTLength / d;
            Point pc = new Point(((1f - t) * end.X) + (t * start.X), ((1f - t) * end.Y) + (t * start.Y));
            Point fend = new Point(end.X, end.Y);
            Point p1 = new Point(pc.X-(fend.Y-pc.Y),(fend.X-pc.X)+pc.Y);
            Point p2 = new Point(fend.Y-pc.Y+pc.X,pc.Y-(fend.X-pc.X));
            gp.DrawLine(new Pen(Color.White,Constants.PEN_WIDTH), fend, pc);
            gp.FillPolygon(brush, new Point[] {
                fend,
                p1,
                p2,
                fend
            });
        }

        public override bool IsValid(out string[] err)
        {
            if (Points.Count()<2)
            {
                err = new string[] { "At least 2 points are required." };
                return false;
            }
            return base.IsValid(out err);
        }
    }
}
