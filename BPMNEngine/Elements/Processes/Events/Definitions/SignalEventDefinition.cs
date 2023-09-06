﻿using BPMNEngine.Attributes;
using BPMNEngine.Elements.Processes.Events.Definitions.Extensions;
using BPMNEngine.Interfaces.Elements;
using System.Linq;

namespace BPMNEngine.Elements.Processes.Events.Definitions
{
    [XMLTag("bpmn", "signalEventDefinition")]
    [ValidParent(typeof(AEvent))]
    internal class SignalEventDefinition : AParentElement
    {
        public IEnumerable<string> SignalTypes 
            => Array.Empty<string>()
                .Concat(ExtensionElement==null || ((IParentElement)ExtensionElement).Children==null ? Array.Empty<string>() :
                    ((IParentElement)ExtensionElement).Children
                    .OfType<SignalDefinition>()
                    .Select(ed => ed.Type ?? "*")
                ).Distinct()
                .DefaultIfEmpty("*");

        public SignalEventDefinition(XmlElement elem, XmlPrefixMap map, AElement parent) 
            : base(elem, map, parent) { }

        public override bool IsValid(out IEnumerable<string> err)
        {
            var res = base.IsValid(out err);
            if (SignalTypes.Any())
            {
                var errors = new List<string>();
                if (Parent is IntermediateThrowEvent)
                {
                    if (SignalTypes.Count() > 1)
                        errors.Add("A throw event can only have one error to be thrown.");
                    var elems = Definition.LocateElementsOfType<IntermediateCatchEvent>();
                    bool found = elems
                            .Any(catcher => catcher.Children
                            .Any(child => child is SignalEventDefinition definition && definition.SignalTypes.Contains(SignalTypes.First()))
                        ) ||
                        elems
                            .Any(catcher => catcher.Children
                            .Any(child => child is SignalEventDefinition definition && definition.SignalTypes.Contains("*"))
                        );
                    if (!found)
                        errors.Add("A defined signal type needs to have a Catch Event with a corresponding type or all");
                }
                err = (err??Array.Empty<string>()).Concat(errors);
                return res && !errors.Any();
            }
            return res;
        }
    }
}
