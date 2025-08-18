using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Core.Events;
using Blazor.Diagrams.Core.Behaviors;
using Blazor.Diagrams;
using Blazor.Diagrams.Core;

namespace ReportGroups.Blazor.Behaviors
{
    public class SingleSelectionBehavior : Behavior
    {
        public SingleSelectionBehavior(Diagram diagram) : base(diagram)
        {
            Diagram.PointerDown += OnPointerDown;
        }

        private void OnPointerDown(Model? model, PointerEventArgs e)
        {
            if (model == null) // Canvas clicked
            {
                Diagram.UnselectAll();
            }
            else if (model is SelectableModel sm)
            {
                // Always unselect all first, then select only the clicked model
                Diagram.UnselectAll();
                Diagram.SelectModel(sm, false);
            }
        }

        public override void Dispose()
        {
            Diagram.PointerDown -= OnPointerDown;
        }
    }
}