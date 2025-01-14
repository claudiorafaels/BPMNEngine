﻿using BPMNEngine.Drawing.Icons.IconParts;

namespace BPMNEngine.Drawing.Icons.Events
{
    [IconTypeAttribute(Elements.Diagrams.BPMIcons.TerminateEndEvent)]
    internal class TerminateEndEvent : AIcon
    {
        private static readonly IIconPart[] _PARTS = new IIconPart[]
        {
            new ThickCircle(),
            new FilledCircle()
        };

        protected override IIconPart[] Parts { get { return _PARTS; } }
    }
}
