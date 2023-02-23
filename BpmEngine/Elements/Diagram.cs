﻿using Microsoft.Maui.Graphics;
using Org.Reddragonit.BpmEngine.Attributes;
using Org.Reddragonit.BpmEngine.Drawing;
using Org.Reddragonit.BpmEngine.Drawing.Extensions;
using Org.Reddragonit.BpmEngine.Elements.Collaborations;
using Org.Reddragonit.BpmEngine.Elements.Diagrams;
using Org.Reddragonit.BpmEngine.Elements.Processes;
using Org.Reddragonit.BpmEngine.Elements.Processes.Events;
using Org.Reddragonit.BpmEngine.Elements.Processes.Tasks;
using Org.Reddragonit.BpmEngine.Interfaces;
using Org.Reddragonit.BpmEngine.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Org.Reddragonit.BpmEngine.Elements
{
    [XMLTag("bpmndi","BPMNDiagram")]
    [RequiredAttribute("id")]
    [ValidParent(typeof(Definition))]
    internal class Diagram : AParentElement
    {
        private const float _SUB_PROCESS_CORNER_RADIUS = 10f;
        private const float _TASK_CORNER_RADIUS = 7.5f;
        private const float _WIDE_TASK_CORNER_RADIUS = 12f;
        private const float _LANE_CORNER_RADIUS = 3f;

        public Diagram(XmlElement elem, XmlPrefixMap map, AElement parent)
            : base(elem, map, parent) { }

        public Size Size
        {
            get
            {
                int minX = 0;
                int maxX = 0;
                int minY = 0;
                int maxY = 0;
                foreach (IElement elem in Children)
                    _RecurGetDimensions(elem, ref minX, ref maxX, ref minY, ref maxY);
                return new Size(maxX - minX, maxY - minY);
            }
        }

        private void _RecurGetDimensions(IElement elem, ref int minX, ref int maxX, ref int minY, ref int maxY)
        {
            if (elem is Bounds b)
            {
                minX = Math.Min(minX, (int)b.Rectangle.X);
                maxX = Math.Max(maxX, (int)b.Rectangle.X + (int)b.Rectangle.Width);
                minY = Math.Min(minY, (int)b.Rectangle.Y);
                maxY = Math.Max(maxY, (int)b.Rectangle.Y + (int)b.Rectangle.Height);
            }
            else if (elem is Waypoint w)
            {
                minX = Math.Min(minX, (int)w.Point.X);
                maxX = Math.Max(maxX, (int)w.Point.X);
                minY = Math.Min(minY, (int)w.Point.Y);
                maxY = Math.Max(maxY, (int)w.Point.Y);
            }
            if (new List<Type>(elem.GetType().GetInterfaces()).Contains(typeof(IParentElement)))
            {
                foreach (IElement ie in ((IParentElement)elem).Children)
                    _RecurGetDimensions(ie, ref minX, ref maxX, ref minY, ref maxY);
            }
        }

        private IEnumerable<Edge> _cachedEdges = null;
        private IEnumerable<Edge> _Edges
        {
            get
            {
                if (_cachedEdges==null)
                {
                    List<Edge> ret = new List<Edge>();
                    foreach (IElement elem in Children)
                        _RecurLocateEdges(elem, ref ret);
                    _cachedEdges=ret;
                }
                return _cachedEdges;
            }
        }

        private void _RecurLocateEdges(IElement elem, ref List<Edge> edges)
        {
            if (elem is Edge edge)
                edges.Add(edge);
            if (elem is IParentElement element)
            {
                foreach (IElement celem in element.Children)
                    _RecurLocateEdges(celem, ref edges);
            }
        }

        private IEnumerable<Shape> _cachedShapes = null;
        private IEnumerable<Shape> _Shapes
        {
            get
            {
                if (_cachedShapes==null)
                {
                    List<Shape> ret = new List<Shape>();
                    foreach (IElement elem in Children)
                        _RecurLocateShapes(elem, ref ret);
                    _cachedShapes = ret;
                }
                return _cachedShapes;
            }
        }

        private void _RecurLocateShapes(IElement elem, ref List<Shape> shapes)
        {
            if (elem is Shape shape)
                shapes.Add(shape);
            if (elem is IParentElement element)
            {
                foreach (IElement celem in element.Children)
                    _RecurLocateShapes(celem, ref shapes);
            }
        }

        public Image Render(ProcessPath path, Definition definition)
        {
            return _Render(path, definition, null);
        }     

        private Rect _ShiftRectangle(Rect rectangle)
        {
            if (rectangle!=null) {
                int minX;
                int minY;
                int maxX;
                int maxY;
                _CalculateDimensions(out minX,out maxX,out minY,out maxY);
                return new Rect(Math.Abs(minX) + rectangle.X, Math.Abs(minY) + rectangle.Y, rectangle.Width, rectangle.Height);
            }
            return rectangle;
        }

        private int? _minX = null;
        private int? _minY = null;
        private int? _maxX = null;
        private int? _maxY = null;

        private void _CalculateDimensions(out int minX,out int maxX,out int minY,out int maxY)
        {
            if (!_minX.HasValue)
            {
                minX = 0;
                maxX = 0;
                minY = 0;
                maxY = 0;
                foreach (IElement elem in Children)
                    _RecurGetDimensions(elem, ref minX, ref maxX, ref minY, ref maxY);
                _minX = minX;
                _minY = minY;
                _maxX = maxX;
                _maxY = maxY;
            }
            else
            {
                minX = _minX.Value;
                minY = _minY.Value;
                maxX = _maxX.Value;
                maxY = _maxY.Value;
            }

        }

        private IEnumerable<Shape> _GetBoundShapes(Definition definition, string elemid)
            => definition.LocateElementsOfType<BoundaryEvent>()
            .Where(evt=>evt.AttachedToID==elemid)
            .SelectMany(elem => _Shapes.Where(shape => shape.bpmnElement==elem.id));

        private Image _Render(ProcessPath path, Definition definition, string elemid)
        {
            Image ret = new Image(Size);
            if (elemid==null)
                ret.Clear(Image.White);
            int minX;
            int minY;
            int maxX;
            int maxY;
            _CalculateDimensions(out minX, out maxX, out minY, out maxY);
            ret.TranslateTransform(Math.Abs(minX), Math.Abs(minY));
            foreach (Shape shape in _Shapes.Where(shape => shape.bpmnElement == (elemid == null ? shape.bpmnElement : elemid)))
            {
                Rect rect;
                Image img = _RenderShape(shape, path.GetStatus(shape.bpmnElement), shape.GetIcon(definition), definition.LocateElement(shape.bpmnElement), _GetBoundShapes(definition, shape.bpmnElement), out rect);
                ret.DrawImage(img, rect);
            }
            foreach (Edge edge in _Edges.Where(edge=> edge.bpmnElement == (elemid == null ? edge.bpmnElement : elemid)))
                    ret.DrawImage(_RenderEdge(edge, path.GetStatus(edge.bpmnElement), definition), edge.Rectangle);
            return ret;
        }

        internal Image RenderElement(ProcessPath path, Definition definition, string elementID,out Rect? rectangle)
        {
            var shape = _Shapes.FirstOrDefault(shape => shape.bpmnElement == elementID);
            if (shape!=null)
            {
                Rect r;
                Image ret = _RenderShape(shape, path.GetStatus(shape.bpmnElement), shape.GetIcon(definition), definition.LocateElement(shape.bpmnElement), _GetBoundShapes(definition, elementID), out r);
                rectangle = _ShiftRectangle(r);
                return ret;
            }
            var edge = _Edges.FirstOrDefault(edge => edge.bpmnElement == elementID);
            if (edge!=null)
            {
                rectangle = _ShiftRectangle(edge.Rectangle);
                return _RenderEdge(edge, path.GetStatus(edge.bpmnElement), definition);
            }
            rectangle = null;
            return null;
        }

        private Image _RenderEdge(Edge edge, StepStatuses status, Definition definition)
        {
            Image ret = new Image(edge.Rectangle);
            ret.TranslateTransform(0 - edge.Rectangle.X, 0 - edge.Rectangle.Y);
            ret.DrawLines(edge.ConstructPen(_GetColor(status), definition), edge.Points.ToArray());
            edge.AppendEnds(ret, _GetColor(status), definition);
            if (edge.Label != null)
            {
                IElement elem = definition.LocateElement(edge.bpmnElement);
                if (elem != null)
                {
                    Size sf = ret.MeasureString(elem.ToString(), new Size((int)edge.Label.Bounds.Rectangle.Width, (int)edge.Label.Bounds.Rectangle.Height));
                    ret.DrawString(elem.ToString(), _GetColor(status), new Rect(edge.Label.Bounds.Rectangle.X+Image.EdgeLabelHorizontalShift, edge.Label.Bounds.Rectangle.Y+Image.EdgeLabelVerticalShift, Math.Max(edge.Label.Bounds.Rectangle.Width, sf.Width), Math.Max(edge.Label.Bounds.Rectangle.Height, sf.Height)),true);
                }
            }
            return ret;
        }

        private Image _RenderShape(Shape shape, StepStatuses status, BPMIcons? icon,IElement elem,IEnumerable<Shape> boundElements,out Rect rect)
        {
            rect = Image.MergeRectangle(shape.Rectangle,(shape.Label!=null ? shape.Label.Bounds.Rectangle : null));
            Image ret = new Image(rect);
            ret.TranslateTransform(0 - (float)rect.X, 0 - (float)rect.Y);
            if (icon.HasValue)
            {
                switch (icon.Value)
                {
                    case BPMIcons.Task:
                    case BPMIcons.SendTask:
                    case BPMIcons.ReceiveTask:
                    case BPMIcons.UserTask:
                    case BPMIcons.ManualTask:
                    case BPMIcons.ServiceTask:
                    case BPMIcons.ScriptTask:
                    case BPMIcons.BusinessRuleTask:
                        IconGraphic.AppendIcon(new Rect(shape.Rectangle.X + 5, shape.Rectangle.Y + 5, 15, 15), icon.Value, ret, _GetColor(status));
                        break;
                    default:
                        IconGraphic.AppendIcon(shape.Rectangle, icon.Value, ret, _GetColor(status));
                        break;
                }
            }
            if (elem != null)
            {
                if (elem is TextAnnotation)
                    ret.DrawLines(new Pen(_GetColor(status), Constants.PEN_WIDTH), new Point[]{
                            new Point(shape.Rectangle.X+20,shape.Rectangle.Y),
                            new Point(shape.Rectangle.X,shape.Rectangle.Y),
                            new Point(shape.Rectangle.X,shape.Rectangle.Y+shape.Rectangle.Height),
                            new Point(shape.Rectangle.X+20,shape.Rectangle.Y+shape.Rectangle.Height)
                        });
                else if (elem is Lane || elem is Participant)
                    ret.DrawRoundRectangle(new Pen(_GetColor(status), Constants.PEN_WIDTH),shape.Rectangle,_LANE_CORNER_RADIUS);
                else if (elem is SubProcess)
                    ret.DrawRoundRectangle(new Pen(_GetColor(status), Constants.PEN_WIDTH), shape.Rectangle,_SUB_PROCESS_CORNER_RADIUS);
                else if (elem is ATask)
                {
                    if (elem is CallActivity)
                        ret.DrawRoundRectangle(new Pen(_GetColor(status), Constants.WIDE_PEN_WIDTH), shape.Rectangle, _WIDE_TASK_CORNER_RADIUS);
                    else
                        ret.DrawRoundRectangle(new Pen(_GetColor(status), Constants.PEN_WIDTH), shape.Rectangle, _TASK_CORNER_RADIUS);
                }
                if (boundElements!=null)
                {
                    foreach (Shape be in boundElements)
                        ret.FillEllipse(Image.White, be.Rectangle);
                }
                if (elem.ToString() != "")
                {
                    if (shape.Label != null)
                    {
                        Size sf = ret.MeasureString(elem.ToString(), new Size(shape.Label.Bounds.Rectangle.Width, (float)int.MaxValue));
                        ret.DrawString(elem.ToString(), _GetColor(status), new Rect(shape.Label.Bounds.Rectangle.X, shape.Label.Bounds.Rectangle.Y, Math.Max(shape.Label.Bounds.Rectangle.Width, sf.Width), Math.Max(shape.Label.Bounds.Rectangle.Height, sf.Height)),true);
                    }
                    else
                    {
                        Size size = ret.MeasureString(elem.ToString());
                        if (size.Height != 0 || size.Width != 0)
                        {
                            if (elem is Lane || elem is LaneSet || elem is Participant)
                            {
                                Image g = new Image((int)size.Height * 2, (int)size.Width);
                                g.TranslateTransform((float)g.Size.Width / 2f, (float)g.Size.Height);
                                g.RotateTransform(-90);
                                g.TranslateTransform(0, 0);
                                g.DrawString(elem.ToString(),_GetColor(status), new Point(0, 0));
                                ret.DrawImage(g, new Point(shape.Rectangle.X + Image.VerticalTextShift, shape.Rectangle.Y + ((shape.Rectangle.Height - g.Size.Height) / 2)));
                            }
                            else
                                ret.DrawString(elem.ToString(), _GetColor(status), new Rect(shape.Rectangle.X + 0.5f, shape.Rectangle.Y + 15, shape.Rectangle.Width - 1, shape.Rectangle.Height - 15.5f),true);
                        }
                    }
                }
            }
            return ret;
        }

        private Color _GetColor(StepStatuses status)
        {
            Color ret = Image.Black;
            switch (status)
            {
                case StepStatuses.Failed:
                    ret = Image.Red;
                    break;
                case StepStatuses.Succeeded:
                    ret = Image.Green;
                    break;
                case StepStatuses.Waiting:
                    ret = Image.Blue;
                    break;
                case StepStatuses.WaitingStart:
                    ret=Image.GoldenYellow;
                    break;
                case StepStatuses.Aborted:
                    ret=Image.Orange;
                    break;
            }
            return ret;
        }

        public override bool IsValid(out string[] err)
        {
            if (!Children.Any())
            {
                err = new string[] { "No child elements found." };
                return false;
            }
            return base.IsValid(out err);
        }

        internal bool RendersElement(string nextStep)
        {
            foreach (Shape shape in _Shapes)
            {
                if (shape.bpmnElement == nextStep)
                    return true;
            }
            foreach (Edge edge in _Edges)
            {
                if (edge.bpmnElement == nextStep)
                    return true;
            }
            return false;
        }

        internal Image UpdateState(ProcessPath path, Definition elem, string nextStep)
        {
            return _Render(path, elem, nextStep);
        }
    }
}
