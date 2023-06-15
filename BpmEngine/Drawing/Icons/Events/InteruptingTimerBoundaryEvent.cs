﻿using BPMNEngine.Drawing.Icons.IconParts;
using System;
using System.Collections.Generic;
using System.Text;

namespace BPMNEngine.Drawing.Icons.Events
{
    [IconTypeAttribute(Elements.Diagrams.BPMIcons.InteruptingTimerBoundaryEvent)]
    internal class InteruptingTimerBoundaryEvent : AIcon
    {
        private static readonly IIconPart[] _PARTS = new IIconPart[] {
            new OuterCircle(),
            new InnerCircle(),
            new Clock()
        };

        protected override IIconPart[] _parts
        {
            get { return _PARTS; }
        }
    }
}
