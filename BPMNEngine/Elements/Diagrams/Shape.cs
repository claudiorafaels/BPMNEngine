﻿using Microsoft.Maui.Graphics;
using BPMNEngine.Attributes;
using BPMNEngine.Drawing;

using BPMNEngine.Elements.Collaborations;
using BPMNEngine.Elements.Processes;
using BPMNEngine.Elements.Processes.Events;
using BPMNEngine.Elements.Processes.Tasks;
using BPMNEngine.State;
using BPMNEngine.Interfaces.Elements;

namespace BPMNEngine.Elements.Diagrams
{
    [XMLTag("bpmndi","BPMNShape")]
    [RequiredAttribute("id")]
    [ValidParent(typeof(Plane))]
    internal class Shape : ADiagramElement,IRenderingElement
    {
        public override RectF Rectangle => Children
            .OfType<Bounds>()
            .Select(elem => elem.Rectangle)
            .FirstOrDefault(new Rect(0, 0, 0, 0));

        public Label Label => (Label)Children
            .FirstOrDefault(elem => elem is Label);

        public Shape(XmlElement elem, XmlPrefixMap map, AElement parent)
            : base(elem, map, parent) { }

        public override bool IsValid(out IEnumerable<string> err)
        {
            var res = base.IsValid(out err);
            if (!Children.Any(elem => elem is Bounds))
            {
                err =(err ?? Array.Empty<string>()).Concat(new string[] { "No bounds specified for the shape." });
                return false;
            }
            return res;
        }

        public void Render(ICanvas surface, ProcessPath path, Definition definition)
        {
            var elem = definition.LocateElement(BPMNElement);
            var color = Diagram.GetColor(path.GetStatus(elem.ID));
            if (elem is Lane || elem is Participant || elem is LaneSet)
                RenderLane(elem, surface, color);
            else
            {
                if (elem is AEvent aEvent)
                    RenderEvent(aEvent, surface, color);
                else if (elem is Processes.Gateways.AGateway aGateway)
                    RenderGateway(aGateway, surface, color);
                else if (elem is ATask aTask)
                    RenderTask(aTask, surface, color);
                else if (elem is TextAnnotation)
                    RenderTextAnnotation(surface, color);
                else if (elem is SubProcess)
                    RenderSubProcess(surface, color);

                if (elem.ToString()!="")
                {
                    surface.FontColor=color;
                    surface.DrawString(elem.ToString(), (this.Label!=null ? this.Label.Bounds.Rectangle : this.Rectangle), HorizontalAlignment.Center, VerticalAlignment.Center);
                }
            }
        }

        #region constants
        private const float _PEN_SIZE = 2.0f;
        private const float _ACTIVITY_PEN_SIZE = 4.5f;
        private const float _LANE_CORNER_RADIUS = 3.0f;
        private const float _TASK_RADIUS = 10f;
        private const float _SUB_PROCESS_CORNER_RADIUS = 10f;
        private const float _CALL_ACTIVITY_SQUARE_SIZE = 10.0f;
        private const float _CALL_ACTIVITY_SQUARE_PADDING = 5.0f;
        #endregion

        #region Event
        private void RenderEvent(AEvent aEvent, ICanvas surface,Color color)
        {
            var icon = BPMIcons.StartEvent;
            if (aEvent is StartEvent)
            {
                if (aEvent.SubType.HasValue)
                {
                    switch (aEvent.SubType.Value)
                    {
                        case EventSubTypes.Message:
                            icon = BPMIcons.MessageStartEvent;
                            break;
                        case EventSubTypes.Conditional:
                            icon = BPMIcons.ConditionalStartEvent;
                            break;
                        case EventSubTypes.Signal:
                            icon = BPMIcons.SignalStartEvent;
                            break;
                        case EventSubTypes.Timer:
                            icon = BPMIcons.TimerStartEvent;
                            break;
                    }
                }
            }
            else if (aEvent is IntermediateThrowEvent)
            {
                icon = BPMIcons.IntermediateThrowEvent;
                if (aEvent.SubType.HasValue)
                {
                    switch (aEvent.SubType.Value)
                    {
                        case EventSubTypes.Message:
                            icon = BPMIcons.MessageIntermediateThrowEvent;
                            break;
                        case EventSubTypes.Compensation:
                            icon = BPMIcons.CompensationIntermediateThrowEvent;
                            break;
                        case EventSubTypes.Escalation:
                            icon = BPMIcons.EscalationIntermediateThrowEvent;
                            break;
                        case EventSubTypes.Link:
                            icon = BPMIcons.LinkIntermediateThrowEvent;
                            break;
                        case EventSubTypes.Signal:
                            icon = BPMIcons.SignalIntermediateThrowEvent;
                            break;
                        case EventSubTypes.Timer:
                            icon = BPMIcons.TimerStartEvent;
                            break;
                    }
                }
            }
            else if (aEvent is IntermediateCatchEvent)
            {
                if (aEvent.SubType.HasValue)
                {
                    switch (aEvent.SubType.Value)
                    {
                        case EventSubTypes.Conditional:
                            icon = BPMIcons.ConditionalIntermediateCatchEvent;
                            break;
                        case EventSubTypes.Link:
                            icon = BPMIcons.LinkIntermediateCatchEvent;
                            break;
                        case EventSubTypes.Message:
                            icon = BPMIcons.MessageIntermediateCatchEvent;
                            break;
                        case EventSubTypes.Signal:
                            icon = BPMIcons.SignalIntermediateCatchEvent;
                            break;
                        case EventSubTypes.Timer:
                            icon = BPMIcons.TimerIntermediateCatchEvent;
                            break;
                    }
                }
            }
            else if (aEvent is EndEvent)
            {
                icon = BPMIcons.EndEvent;
                if (aEvent.SubType.HasValue)
                {
                    switch (aEvent.SubType.Value)
                    {
                        case EventSubTypes.Compensation:
                            icon = BPMIcons.CompensationEndEvent;
                            break;
                        case EventSubTypes.Escalation:
                            icon = BPMIcons.EscalationEndEvent;
                            break;
                        case EventSubTypes.Message:
                            icon = BPMIcons.MessageEndEvent;
                            break;
                        case EventSubTypes.Signal:
                            icon = BPMIcons.SignalEndEvent;
                            break;
                        case EventSubTypes.Error:
                            icon = BPMIcons.ErrorEndEvent;
                            break;
                        case EventSubTypes.Terminate:
                            icon = BPMIcons.TerminateEndEvent;
                            break;
                    }
                }
            }
            else if (aEvent is BoundaryEvent @event)
            {
                switch (aEvent.SubType.Value)
                {
                    case EventSubTypes.Message:
                        icon = (@event.CancelActivity ? BPMIcons.InteruptingMessageBoundaryEvent : BPMIcons.NonInteruptingMessageBoundaryEvent);
                        break;
                    case EventSubTypes.Conditional:
                        icon = (@event.CancelActivity ? BPMIcons.InteruptingConditionalBoundaryEvent : BPMIcons.NonInteruptingConditionalBoundaryEvent);
                        break;
                    case EventSubTypes.Escalation:
                        icon = (@event.CancelActivity ? BPMIcons.InteruptingEscalationBoundaryEvent : BPMIcons.NonInteruptingEscalationBoundaryEvent);
                        break;
                    case EventSubTypes.Signal:
                        icon = (@event.CancelActivity ? BPMIcons.InteruptingSignalBoundaryEvent : BPMIcons.NonInteruptingSignalBoundaryEvent);
                        break;
                    case EventSubTypes.Timer:
                        icon = (@event.CancelActivity ? BPMIcons.InteruptingTimerBoundaryEvent : BPMIcons.NonInteruptingTimerBoundaryEvent);
                        break;
                    case EventSubTypes.Error:
                        icon=BPMIcons.InteruptingErrorBoundaryEvent;
                        break;
                    case EventSubTypes.Compensation:
                        icon=BPMIcons.InteruptingCompensationBoundaryEvent;
                        break;
                }
            }
            IconGraphic.AppendIcon(this.Rectangle, icon, surface, color);
        }

        #endregion

        #region Gateway
        private void RenderGateway(Processes.Gateways.AGateway aGateway, ICanvas surface, Color color)
        {
            IconGraphic.AppendIcon(this.Rectangle, (BPMIcons)Enum.Parse(typeof(BPMIcons), aGateway.GetType().Name), surface, color);
        }
        #endregion

        #region Lane
        private void RenderLane(IElement elem, ICanvas surface, Color color)
        {
            if (elem is not LaneSet)
            {
                surface.StrokeColor=color;
                surface.StrokeDashPattern=null;
                surface.StrokeSize = _PEN_SIZE;

                surface.DrawRoundedRectangle(this.Rectangle, _LANE_CORNER_RADIUS);
            }

            if (elem.ToString()!="")
            {
                surface.Rotate(-90,this.Rectangle.X,this.Rectangle.Y);
                surface.FontColor=color;
                surface.DrawString(elem.ToString(), new RectF(this.Rectangle.X-this.Rectangle.Height, this.Rectangle.Y+1, this.Rectangle.Height, this.Rectangle.Width), HorizontalAlignment.Center, VerticalAlignment.Top);
                surface.Rotate(90, this.Rectangle.X, this.Rectangle.Y);
            }
        }
        #endregion

        #region Task
        private void RenderTask(ATask aTask, ICanvas surface, Color color)
        {
            surface.StrokeColor = color;
            surface.StrokeDashPattern=null;
            surface.StrokeSize = aTask is CallActivity ? _ACTIVITY_PEN_SIZE : _PEN_SIZE;

            surface.DrawRoundedRectangle(this.Rectangle,_TASK_RADIUS);

            if (Enum.TryParse(typeof(BPMIcons), aTask.GetType().Name, out object obj))
                IconGraphic.AppendIcon(new Rect(this.Rectangle.X + 7, this.Rectangle.Y + 11, 15, 15), (BPMIcons)obj, surface, color);

            if (aTask is CallActivity)
            {
                surface.StrokeSize = _PEN_SIZE;
                var centerX = this.Rectangle.X+(this.Rectangle.Width/2);
                surface.DrawRectangle(centerX-(_CALL_ACTIVITY_SQUARE_SIZE/2), Rectangle.Y+Rectangle.Height-_CALL_ACTIVITY_SQUARE_PADDING-_CALL_ACTIVITY_SQUARE_SIZE, _CALL_ACTIVITY_SQUARE_SIZE, _CALL_ACTIVITY_SQUARE_SIZE);
                surface.DrawLine(centerX, Rectangle.Y+Rectangle.Height-_CALL_ACTIVITY_SQUARE_PADDING-_CALL_ACTIVITY_SQUARE_SIZE, centerX, Rectangle.Y+Rectangle.Height-_CALL_ACTIVITY_SQUARE_PADDING);
                surface.DrawLine(centerX-(_CALL_ACTIVITY_SQUARE_SIZE/2), Rectangle.Y+Rectangle.Height-_CALL_ACTIVITY_SQUARE_PADDING-(_CALL_ACTIVITY_SQUARE_SIZE/2), centerX+(_CALL_ACTIVITY_SQUARE_SIZE/2), Rectangle.Y+Rectangle.Height-_CALL_ACTIVITY_SQUARE_PADDING-(_CALL_ACTIVITY_SQUARE_SIZE/2));
            }
        }
        #endregion

        #region TextAnnotation
        private void RenderTextAnnotation(ICanvas surface, Color color)
        {
            surface.StrokeColor=color;
            surface.StrokeSize=_PEN_SIZE;
            surface.StrokeDashPattern=null;

            surface.DrawLine(new Point(this.Rectangle.X+20, this.Rectangle.Y),
                            new Point(this.Rectangle.X, this.Rectangle.Y));
            surface.DrawLine(new Point(this.Rectangle.X, this.Rectangle.Y),
                            new Point(this.Rectangle.X, this.Rectangle.Y+this.Rectangle.Height));
            surface.DrawLine(new Point(this.Rectangle.X, this.Rectangle.Y+this.Rectangle.Height),
                            new Point(this.Rectangle.X+20, this.Rectangle.Y+this.Rectangle.Height));
        }
        #endregion

        #region SubProcess
        private void RenderSubProcess(ICanvas surface, Color color)
        {
            surface.StrokeColor=color;
            surface.StrokeDashPattern=null;
            surface.StrokeSize = _PEN_SIZE;
            surface.DrawRoundedRectangle(this.Rectangle,_SUB_PROCESS_CORNER_RADIUS);
        }
        #endregion
    }
}
