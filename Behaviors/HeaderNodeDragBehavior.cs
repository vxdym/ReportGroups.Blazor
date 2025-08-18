using Blazor.Diagrams.Core.Behaviors;
using Blazor.Diagrams.Core.Events;
using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Geometry;
using ReportGroups.Blazor.Models.Nodes;

namespace ReportGroups.Blazor.Behaviors
{
    public class HeaderNodeDragBehavior : Behavior
    {
        private HeaderNode? _draggedHeaderNode;
        private Point? _lastPointerPosition;

        public HeaderNodeDragBehavior(Diagram diagram) : base(diagram)
        {
            Diagram.PointerDown += OnPointerDown;
            Diagram.PointerMove += OnPointerMove;
            Diagram.PointerUp += OnPointerUp;
        }

        private void OnPointerDown(Model? model, PointerEventArgs e)
        {
            if (model is HeaderNode headerNode)
            {
                _draggedHeaderNode = headerNode;
                _lastPointerPosition = new Point(e.ClientX, e.ClientY);
            }
        }

        private void OnPointerMove(Model? model, PointerEventArgs e)
        {
            if (_draggedHeaderNode != null && _lastPointerPosition != null)
            {
                var currentPosition = new Point(e.ClientX, e.ClientY);
                var deltaX = currentPosition.X - _lastPointerPosition.X;
                var deltaY = currentPosition.Y - _lastPointerPosition.Y;

                // Find the group that contains this header node
                var group = Diagram.Groups.FirstOrDefault(g => g.Children.Contains(_draggedHeaderNode));
                if (group != null)
                {
                    // Move the entire group
                    var newPosition = new Point(
                        (group.Position?.X ?? 0) + deltaX,
                        (group.Position?.Y ?? 0) + deltaY
                    );
                    group.SetPosition(newPosition.X, newPosition.Y);
                }

                _lastPointerPosition = currentPosition;
            }
        }

        private void OnPointerUp(Model? model, PointerEventArgs e)
        {
            _draggedHeaderNode = null;
            _lastPointerPosition = null;
        }

        public override void Dispose()
        {
            Diagram.PointerDown -= OnPointerDown;
            Diagram.PointerMove -= OnPointerMove;
            Diagram.PointerUp -= OnPointerUp;
        }
    }
}