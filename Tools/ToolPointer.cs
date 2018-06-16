namespace DrawTools
{
    using System.Collections;
    using System.Drawing;
    using System.Windows.Forms;

    using Draw;
    //курсор
    public class ToolPointer : Tool
    {
        #region Fields

        private PointF _lastPoint = new PointF(0,0);
        private DrawObject _resizedObject;
        private int _resizedObjectHandle;
        private SelectionMode _selectMode = SelectionMode.None;
        private PointF _startPoint = new PointF(0, 0);

        #endregion Fields

        #region Constructors

        public ToolPointer()
        {
            IsComplete = true;
        }

        #endregion Constructors

        #region Enumerations

        private enum SelectionMode
        {
            None,
            NetSelection, 
            Move,          
            Size           
        }

        #endregion Enumerations

        #region Methods

        public override void OnMouseDown(DrawArea drawArea, MouseEventArgs e)
        {
            _selectMode = SelectionMode.None;
            PointF point = new Point(e.X, e.Y);

            // òåñò äëÿ ðåñàéçà
            int n = drawArea.GraphicsList.SelectionCount;

                for ( int i = 0; i < n; i++ )
                {
                    DrawObject o = drawArea.GraphicsList.GetSelectedObject(i);
                    int handleNumber = o.HitTest(point);
                    bool hitOnOutline = o.HitOnCircumferance;

                    if ( handleNumber > 0 )
                    {
                        _selectMode = SelectionMode.Size;

                        // äåðæèì èçìåíÿåìûé îáúåêò â ÷ëåíàõ êëàññà
                        _resizedObject = o;
                        _resizedObjectHandle = handleNumber;

                        // ïðè ðåñàéçå îäíîãî îáúåêòà, àíñåëåêòÿòñÿ âñå îñòàëüíûå
                        drawArea.GraphicsList.UnselectAll();
                        o.Selected = true;
                        o.MouseClickOnHandle(handleNumber);

                        break;
                    }

                    if (hitOnOutline && (n == 1)) // åñëè òîëüêî îäèí îáúåêò âûáðàí 
                    {
                        _selectMode = SelectionMode.Size;
                        o.MouseClickOnBorder();
                        o.Selected = true;
                    }

            }

            // òåñò äëÿ ñäâèãà
            if ( _selectMode == SelectionMode.None )
            {
                int n1 = drawArea.GraphicsList.Count;
                DrawObject o = null;

                for ( int i = 0; i < n1; i++ )
                {
                    if ( drawArea.GraphicsList[i].HitTest(point) == 0 )
                    {
                        o = drawArea.GraphicsList[i];
                        break;
                    }
                }

                if ( o != null )
                {
                    _selectMode = SelectionMode.Move;

                    // àíñåëåêòíóòü âñå, åñëè íå çàæàò ctrl è êëèêíóòíûé îáúåêò åùå íå âûáðàí 
                    if ( ( Control.ModifierKeys & Keys.Control ) == 0  && !o.Selected )
                        drawArea.GraphicsList.UnselectAll();

                    // âûáðàòü êëèêíóòûé îáúåêò
                    o.Selected = true;

                    drawArea.Cursor = Cursors.SizeAll;
                }
            }

            if ( _selectMode == SelectionMode.None )
            {
                // êëèê íà çàäíåì ôîíå 
                if ( ( Control.ModifierKeys & Keys.Control ) == 0 )
                    drawArea.GraphicsList.UnselectAll();

                _selectMode = SelectionMode.NetSelection;
                drawArea.DrawNetRectangle = true;
            }

            _lastPoint.X = e.X;
            _lastPoint.Y = e.Y;
            _startPoint.X = e.X;
            _startPoint.Y = e.Y;

            drawArea.Capture = true;
            drawArea.NetRectangle = DrawRectangle.GetNormalizedRectangle(_startPoint, _lastPoint);

            drawArea.Refresh();
        }

        public override void OnMouseMove(DrawArea drawArea, MouseEventArgs e)
        {
            var point = new Point(e.X, e.Y);

            // óñòàíîâêà êóðñîðà, êîãäà ìûøü íå çàæàòà 
            if ( e.Button == MouseButtons.None )
            {
                Cursor cursor = null;

                for ( int i = 0; i < drawArea.GraphicsList.Count; i++ )
                {
                    int n = drawArea.GraphicsList[i].HitTest(point);

                    if ( n > 0 )
                    {
                        cursor = drawArea.GraphicsList[i].GetHandleCursor(n);
                        break;
                    }
                    if (drawArea.GraphicsList[i].HitOnCircumferance)
                    {
                        cursor = drawArea.GraphicsList[i].GetOutlineCursor(n);
                        break;
                    }
                }

                if ( cursor == null )
                    cursor = Cursors.Default;

                drawArea.Cursor = cursor;

                return;
            }

            if ( e.Button != MouseButtons.Left )
                return;

            // íàæàòèå ëåâîé êíîïêè

            // ðàçíèöà ìåæäó ïðåä. è òåê. ïîçèöèåé
            float dx = e.X - _lastPoint.X;
            float dy = e.Y - _lastPoint.Y;

            _lastPoint.X = e.X;
            _lastPoint.Y = e.Y;

            // ðåñàéç
            if ( _selectMode == SelectionMode.Size )
            {
                if ( _resizedObject != null )
                {
                    _resizedObject.MoveHandleTo(point, _resizedObjectHandle);
                    drawArea.SetDirty();
                    drawArea.Refresh();
                }
            }

            // ñäâèã
            if ( _selectMode == SelectionMode.Move )
            {
                int n = drawArea.GraphicsList.SelectionCount;

                for ( int i = 0; i < n; i++ )
                {
                    drawArea.GraphicsList.GetSelectedObject(i).Move(dx, dy);
                }

                drawArea.Cursor = Cursors.SizeAll;
                drawArea.SetDirty();
                drawArea.Refresh();
            }

            if ( _selectMode == SelectionMode.NetSelection )
            {
                drawArea.NetRectangle = DrawRectangle.GetNormalizedRectangle(_startPoint, _lastPoint);
                drawArea.Refresh();
                return;
            }
        }

        public override void OnMouseUp(DrawArea drawArea, MouseEventArgs e)
        {
            if ( _selectMode == SelectionMode.NetSelection )
            {
                // âûáîð ãðóïïû
                drawArea.GraphicsList.SelectInRectangle(drawArea.NetRectangle);

                _selectMode = SelectionMode.None;
                drawArea.DrawNetRectangle = false;
            }

            if ( _resizedObject != null )
            {
                // ïîñëå ðåñàéçà
                _resizedObject.Normalize();
                _resizedObject = null;
                drawArea.ResizeCommand(drawArea.GraphicsList.GetFirstSelected(),
                    new PointF(_startPoint.X, _startPoint.Y),
                    new PointF(e.X, e.Y),
                    _resizedObjectHandle);
            }

            drawArea.Capture = false;
            drawArea.Refresh();

            //ïóøàåì êîìàíäó â undo/redo ëèñò
            if (_selectMode == SelectionMode.Move)
            {
                var movedItemsList = new ArrayList();

                for (int i = 0; i < drawArea.GraphicsList.SelectionCount; i++)
                {
                    movedItemsList.Add(drawArea.GraphicsList.GetSelectedObject(i));
                }

                var delta = new PointF {X = e.X - _startPoint.X, Y = e.Y - _startPoint.Y};
                drawArea.MoveCommand(movedItemsList, delta);
            }

            IsComplete = true;
        }

        #endregion Methods
    }
}