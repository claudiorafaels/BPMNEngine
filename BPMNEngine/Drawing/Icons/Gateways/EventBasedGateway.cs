﻿using Microsoft.Maui.Graphics;
using BPMNEngine.Elements;

namespace BPMNEngine.Drawing.Icons.Gateways
{
    [IconTypeAttribute(Elements.Diagrams.BPMIcons.EventBasedGateway)]
    internal class EventBasedGateway : AGateway
    {
        private const float _CIRCLE_PEN_SIZE = 1.0f;

        private static readonly Point[] _POINTS = new Point[]
        {
            new Point(23f,26f),
            new Point(32f,21f),
            new Point(40f,26f),
            new Point(37f,38f),
            new Point(25f,38f),
            new Point(23f,26f)
        };

        protected override void InternalDraw(ICanvas surface, Color color)
        {
            base.InternalDraw(surface, color);

            Diagram.DrawLines(surface, _POINTS);

            surface.DrawLine(_POINTS[^1], _POINTS[0]);

            surface.StrokeSize = _CIRCLE_PEN_SIZE;

            surface.DrawEllipse(new Rect(16, 16, 30, 30));
            surface.DrawEllipse(new Rect(19, 19, 24, 24));
        }
    }
}
