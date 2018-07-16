using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml.Shapes;
using Windows.Storage.Streams;
using Windows.UI.Input;
using System.Diagnostics;
using Protocol2.Utils;
using Windows.UI;
using Windows.UI.Core;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Protocol2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
  
        private Stack<InkStroke> undoStack { get; set; }

        InkAnalyzer analyzerShape = new InkAnalyzer();

        Dictionary<InkStroke, StrokeGroup> groups = new Dictionary<InkStroke, StrokeGroup>();

        //Stroke selection field
        private Polyline lasso;
        //Stroke selection area
        private Rect boundingRect;
        private bool isBoundRect;
        public bool selectedStrokesExist = false;

        private Random rng = new Random();
        public CommentModel comments;
        private Save save = null;

        private CoreCursor normalCursor = Window.Current.CoreWindow.PointerCursor;
        private CoreCursor inBoundingBox = new CoreCursor(CoreCursorType.SizeAll, 0);

        public MainPage()
        {
            this.InitializeComponent();

            canvas.RenderTransform = new TranslateTransform();
            inkCanvas.InkPresenter.InputDeviceTypes =

                Windows.UI.Core.CoreInputDeviceTypes.Mouse |
                // Uncomment the line below if you want to draw with touch
                // When commented out, long touch to create comment
                // Windows.UI.Core.CoreInputDeviceTypes.Touch |
                Windows.UI.Core.CoreInputDeviceTypes.Pen;

            //Multiple pen input, breaks undo!
            //inkCanvas.InkPresenter.ActivateCustomDrying();
            //inkCanvas.InkPresenter.SetPredefinedConfiguration(InkPresenterPredefinedConfiguration.SimpleMultiplePointer);

            undoStack = new Stack<InkStroke>();
            comments = new CommentModel();

            inkCanvas.InkPresenter.StrokeInput.StrokeEnded += ClearStack;                                                                                                                                                                                                                                 

            //inkCanvas.RightTapped += new RightTappedEventHandler(CreatePopup);
        }

        private async void CreatePopup(object sender, RightTappedRoutedEventArgs e)
        {
            Point point = e.GetPosition(inkCanvas);


            inkCanvas.InkPresenter.StrokeInput.StrokeEnded += ClearStack;
            
            // enable adding comments with right click (how in hub??)
            inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;

            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += OtherMakePopup;

            inkCanvas.RightTapped += TouchMakePopup;

        }


        public Rectangle DrawRectangle(Comment comment)
        {
            Rectangle rectangle = new Rectangle();
            rectangle.Fill = new SolidColorBrush(comment.fill);
            rectangle.Width = comment.width;
            rectangle.Height = comment.height;
            rectangle.Opacity = comment.opacity;
            Canvas.SetLeft(rectangle, comment.left);
            Canvas.SetTop(rectangle, comment.top);

            var rotation = new RotateTransform();
            rotation.Angle = comment.angle;
            rectangle.RenderTransform = rotation;

            // Add flyout
            var flyout = new Flyout();
            Style flyoutStyle = new Style();
            flyoutStyle.TargetType = typeof(FlyoutPresenter);
            flyoutStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(comment.fill)));
            flyout.FlyoutPresenterStyle = flyoutStyle;

            // Add delete button
            Button deleteButton = new Button();
            deleteButton.Content = new SymbolIcon(Symbol.Delete);
            deleteButton.Click += async delegate (object e, RoutedEventArgs evt)
            {
                canvas.Children.Remove(rectangle);
                comments.Remove(comment);
            };

            // Add canvas
            InkCanvas flyoutInkCanvas = new InkCanvas();
            flyoutInkCanvas.Width = 250;
            flyoutInkCanvas.Height = 250;
            flyoutInkCanvas.InkPresenter.InputDeviceTypes = 
              Windows.UI.Core.CoreInputDeviceTypes.Mouse |
              Windows.UI.Core.CoreInputDeviceTypes.Touch |
              Windows.UI.Core.CoreInputDeviceTypes.Pen;

            if (comment.ic != null)
            {
                flyoutInkCanvas.InkPresenter.StrokeContainer = comment.ic;
            }
            comment.ic = flyoutInkCanvas.InkPresenter.StrokeContainer;
        
            InkToolbar flyoutInkToolbar = new InkToolbar();
            flyoutInkToolbar.TargetInkCanvas = flyoutInkCanvas;

            // Add panels
            StackPanel stackPanel = new StackPanel();
            stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
            stackPanel.VerticalAlignment = VerticalAlignment.Center;

            StackPanel rightAlignment = new StackPanel();
            rightAlignment.HorizontalAlignment = HorizontalAlignment.Right;
            rightAlignment.Children.Add(deleteButton);

            stackPanel.Children.Add(rightAlignment);
            stackPanel.Children.Add(flyoutInkCanvas);
            stackPanel.Children.Add(flyoutInkToolbar);

            // Additional settings
            flyout.Content = stackPanel;
            flyout.LightDismissOverlayMode = LightDismissOverlayMode.On;
            rectangle.ContextFlyout = flyout;

            // settings for dragging comments 
            rectangle.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            rectangle.ManipulationDelta += new ManipulationDeltaEventHandler(Drag_Comment);
            canvas.Children.Add(rectangle);

            return rectangle;
        }

        private void Drag_Comment(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var rectangle = (Rectangle)sender;
            Canvas.SetLeft(rectangle, Canvas.GetLeft(rectangle) + e.Delta.Translation.X);
            Canvas.SetTop(rectangle, Canvas.GetTop(rectangle) + e.Delta.Translation.Y);
        }

        public void makeComment(double x, double y) 
        {
            
            var newComment = comments.CreateComment(x, y);
            var rect = DrawRectangle(newComment);
            rect.ContextFlyout.ShowAt(rect);
            

        }

        private void TouchMakePopup(object sender, RightTappedRoutedEventArgs args)
        {
            Point point = args.GetPosition(inkCanvas);
            makeComment(point.X, point.Y);

            
        }

        private void OtherMakePopup(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            PointerPoint point = args.CurrentPoint;

            makeComment(point.Position.X, point.Position.Y);
        }


        public async void saveAll(object sender, RoutedEventArgs e)
        {
            if (save == null)
            {
                save = new Save();
                // display some sort of selection screen
                await save.CreateFolder();
            
            }
     
            await save.SaveAll(inkCanvas, comments);

        }

        public async void loadAll(object sender, RoutedEventArgs e)
        {
            if (save == null)
            {
                save = new Save();
            }

            await save.LoadAll(inkCanvas, comments);
            if (comments != null)
            {
                canvas.Children.Clear(); // probably better way than this...
                Debug.WriteLine(comments.GetComments().Count);
                foreach(Comment c in comments.GetComments())
                {
                    DrawRectangle(c);
                }
            }
        }



        private void ClearStack(InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            // clear the stack if a new stroke has been added
            undoStack.Clear();
        }

        private void saveInk_ClickAsync(object sender, RoutedEventArgs e)
        {
           // Save.SaveInk(inkCanvas);
        }

        private void loadInk_ClickAsync(object sender, RoutedEventArgs e)
        {
            //Save.LoadInk(inkCanvas);
        }

        //private async void recogniseShape_ClickAsync(object sender, RoutedEventArgs e)
        //{
        //    strokesShape = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();

        //    if (strokesShape.Count > 0)
        //    {
        //        analyzerShape.AddDataForStrokes(strokesShape);
        //        resultShape = await analyzerShape.AnalyzeAsync();

        //        if (resultShape.Status == InkAnalysisStatus.Updated)
        //        {
        //            var drawings = analyzerShape.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);

        //            foreach (var drawing in drawings)
        //            {
        //                var shape = (InkAnalysisInkDrawing)drawing;
        //                if (shape.DrawingKind == InkAnalysisDrawingKind.Drawing)
        //                {
        //                    // Catch and process unsupported shapes (lines and so on) here.
        //                }
        //                else
        //                {
        //                    // Process recognized shapes here.
        //                    if (shape.DrawingKind == InkAnalysisDrawingKind.Circle || shape.DrawingKind == InkAnalysisDrawingKind.Ellipse)
        //                    {
        //                        DrawEllipse(shape);
        //                    }
        //                    else
        //                    {
        //                        DrawPolygon(shape);
        //                    }
        //                    foreach (var strokeId in shape.GetStrokeIds())
        //                    {
        //                        var stroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokeById(strokeId);
        //                        stroke.Selected = true;
        //                    }
        //                }
        //                analyzerShape.RemoveDataForStrokes(shape.GetStrokeIds());
        //            }
        //            inkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
        //        }
        //    }
        //}

        private void backToMenu(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Home));
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            //Source: http://edi.wang/post/2017/7/25/uwp-ink-undo-redo
            IReadOnlyList<InkStroke> strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (strokes.Count > 0)
            {
                strokes[strokes.Count - 1].Selected = true;
                undoStack.Push(strokes[strokes.Count - 1]);
                inkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
            }

        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (undoStack.Count > 0)
            {
                var stroke = undoStack.Pop();

                var strokeBuilder = new InkStrokeBuilder();
                strokeBuilder.SetDefaultDrawingAttributes(stroke.DrawingAttributes);
                System.Numerics.Matrix3x2 matrix = stroke.PointTransform;
                IReadOnlyList<InkPoint> inkPoints = stroke.GetInkPoints();
                InkStroke inkStroke = strokeBuilder.CreateStrokeFromInkPoints(inkPoints, matrix);
                inkCanvas.InkPresenter.StrokeContainer.AddStroke(inkStroke);
            }
        }

        //private void DrawEllipse(InkAnalysisInkDrawing shape)
        //{
        //    var points = shape.Points;
        //    Ellipse ellipse = new Ellipse();
        //    ellipse.Width = Math.Sqrt((points[0].X - points[2].X) * (points[0].X - points[2].X) +
        //         (points[0].Y - points[2].Y) * (points[0].Y - points[2].Y));
        //    ellipse.Height = Math.Sqrt((points[1].X - points[3].X) * (points[1].X - points[3].X) +
        //         (points[1].Y - points[3].Y) * (points[1].Y - points[3].Y));

        //    var rotAngle = Math.Atan2(points[2].Y - points[0].Y, points[2].X - points[0].X);
        //    RotateTransform rotateTransform = new RotateTransform();
        //    rotateTransform.Angle = rotAngle * 180 / Math.PI;
        //    rotateTransform.CenterX = ellipse.Width / 2.0;
        //    rotateTransform.CenterY = ellipse.Height / 2.0;

        //    TranslateTransform translateTransform = new TranslateTransform();
        //    translateTransform.X = shape.Center.X - ellipse.Width / 2.0;
        //    translateTransform.Y = shape.Center.Y - ellipse.Height / 2.0;

        //    TransformGroup transformGroup = new TransformGroup();
        //    transformGroup.Children.Add(rotateTransform);
        //    transformGroup.Children.Add(translateTransform);
        //    ellipse.RenderTransform = transformGroup;

        //    var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 0, 0, 255));
        //    ellipse.Stroke = brush;
        //    ellipse.StrokeThickness = 2;
        //    canvas.Children.Add(ellipse);
        //}

        //private void DrawPolygon(InkAnalysisInkDrawing shape)
        //{
        //    var points = shape.Points;
        //    Polygon polygon = new Polygon();

        //    foreach (var point in points)
        //    {
        //        polygon.Points.Add(point);
        //    }

        //    var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 0, 0, 255));
        //    polygon.Stroke = brush;
        //    polygon.StrokeThickness = 2;
        //    canvas.Children.Add(polygon);
        //}

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is bool && (bool)e.Parameter == true)
            {
                if (save == null)
                {
                    save = new Save();
                }

                await save.LoadAll(inkCanvas, comments);
                if (comments != null)
                {
                    canvas.Children.Clear(); // probably better way than this...
                    Debug.WriteLine(comments.GetComments().Count);
                    foreach (Comment c in comments.GetComments())
                    {
                        DrawRectangle(c);
                    }
                }
            }
            else if (e.Parameter is Save)
            {
                save = e.Parameter as Save;
                await save.LoadNew(inkCanvas, comments);
                if (comments != null)
                {
                    canvas.Children.Clear(); // probably better way than this...
                    Debug.WriteLine(comments.GetComments().Count);
                    foreach (Comment c in comments.GetComments())
                    {
                        DrawRectangle(c);
                    }
                }
            }


            base.OnNavigatedTo(e);
        }


        //Listeners for the lasso selection functionality

        private void UnprocessedInput_PointerPressed(InkUnprocessedInput sender, PointerEventArgs args)
        {
            //polyline draws a series of connected straight lines - going to pass it points where the pen is
            lasso = new Polyline()
            {
                Stroke = new SolidColorBrush(Windows.UI.Colors.Blue),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 5, 2 },
            };

            lasso.Points.Add(args.CurrentPoint.RawPosition);
            selectionCanvas.Children.Add(lasso);
            isBoundRect = true;
        }

        private void UnprocessedInput_PointerMoved(InkUnprocessedInput sender, PointerEventArgs args)
        {
            if (isBoundRect)
            {
                lasso.Points.Add(args.CurrentPoint.RawPosition);
            }
        }

        private void UnprocessedInput_PointerReleased(InkUnprocessedInput sender, PointerEventArgs args)
        {
            //add final point to the Polyline object
            lasso.Points.Add(args.CurrentPoint.RawPosition);

            boundingRect = inkCanvas.InkPresenter.StrokeContainer.SelectWithPolyLine(lasso.Points);

            updateSelected(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());

            isBoundRect = false;
            DrawBoundingRect();
        }

        private void updateSelected(IReadOnlyList<InkStroke> strokes)
        {
            StrokeGroup strokeGroup;
            Rect updatedBoundingBox = boundingRect;
            var updatedLeftX = updatedBoundingBox.X;
            var updatedRightX = updatedBoundingBox.X + updatedBoundingBox.Width;
            var updatedTopY = updatedBoundingBox.Y;
            var updatedBottomY = updatedBoundingBox.Y + updatedBoundingBox.Height;

            foreach (var stroke in strokes)
            {
                if (stroke.Selected)
                {
                    if (groups.TryGetValue(stroke, out strokeGroup))
                    {
                        strokeGroup.selectStrokesInGroup();
                        Rect groupBoundingBox = strokeGroup.findBoundingBox();

                        if (groupBoundingBox.X < updatedBoundingBox.X)
                        {
                            updatedLeftX = groupBoundingBox.X;
                        }

                        if (groupBoundingBox.Y < updatedBoundingBox.Y)
                        {
                            updatedTopY = groupBoundingBox.Y;
                        }

                        if (groupBoundingBox.X + groupBoundingBox.Width > updatedBoundingBox.X + updatedBoundingBox.Width)
                        {
                            updatedRightX = groupBoundingBox.X + groupBoundingBox.Width;
                        }

                        if (groupBoundingBox.Y + groupBoundingBox.Height > updatedBoundingBox.Y + updatedBoundingBox.Height)
                        {
                            updatedBottomY = groupBoundingBox.Y + groupBoundingBox.Height;
                        }
                    }
                }

                updatedBoundingBox = new Rect(updatedLeftX, updatedTopY, updatedRightX - updatedLeftX, updatedBottomY - updatedTopY);
            }
            boundingRect = updatedBoundingBox;
        }


        //handle new ink or erase strokes to clean up Selection UI 
        private void StrokeInput_StrokeStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            ClearSelection();
        }

        private void InkPresenter_StrokesErased(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            ClearSelection();
        }

        //clear existing content from the selection layer and draw a single bounding rectangle around the ink strokes encompassed by the lasso area
        private void DrawBoundingRect()
        {
            selectionCanvas.Children.Clear();

            //draw bounding box only if there are ink strokes within the lasso
            if (!((boundingRect.Width == 0) || (boundingRect.Height == 0) || boundingRect.IsEmpty))
            {
                selectedStrokesExist = true;
                SolidColorBrush transparent = new SolidColorBrush(Windows.UI.Colors.Coral);
                transparent.Opacity = 0;
                var rectangle = new Rectangle()
                {
                    Stroke = new SolidColorBrush(Windows.UI.Colors.Blue),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection() { 5, 2 },
                    Width = boundingRect.Width,
                    Height = boundingRect.Height,
                    Fill = transparent
                };

                Canvas.SetLeft(rectangle, boundingRect.X);
                Canvas.SetTop(rectangle, boundingRect.Y);
                rectangle.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                rectangle.ManipulationDelta += new ManipulationDeltaEventHandler(Drag_Stroke);
                rectangle.PointerEntered += new PointerEventHandler(Cursor_In_BoundingBox);
                rectangle.PointerExited += new PointerEventHandler(Cursor_Leave_BoundingBox);

                selectionCanvas.Children.Add(rectangle);
            } else
            {
                selectedStrokesExist = false;
            }

            Toggle_ActionBar();
        }

        private void Drag_Stroke(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var rectangle = (Rectangle)sender;
            Canvas.SetLeft(rectangle, Canvas.GetLeft(rectangle) + e.Delta.Translation.X);
            Canvas.SetTop(rectangle, Canvas.GetTop(rectangle) + e.Delta.Translation.Y);

            inkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(e.Delta.Translation.X, e.Delta.Translation.Y));
        }

        private void Cursor_In_BoundingBox(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = inBoundingBox;
        }

        private void Cursor_Leave_BoundingBox(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = normalCursor;
        }
       

        private void ClearSelection()
        {
            var strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            foreach (var stroke in strokes)
            {
                stroke.Selected = false;
            }
            ClearDrawnBoundingRect();
            selectedStrokesExist = false;
            Toggle_ActionBar();
        }

        private void ClearDrawnBoundingRect()
        {
            if (selectionCanvas.Children.Any())
            {
                selectionCanvas.Children.Clear();
                boundingRect = Rect.Empty;
            }
        }
        
        private void Click_Select(object sender, RightTappedRoutedEventArgs args)
        {
            Point clickedPoint = args.GetPosition(inkCanvas);
            //need to adjust it so that it works for different thickness strokes
            boundingRect = inkCanvas.InkPresenter.StrokeContainer.SelectWithLine(new Point(clickedPoint.X -1, clickedPoint.Y - 1), new Point(clickedPoint.X, clickedPoint.Y + 3));
            updateSelected(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());
            DrawBoundingRect();
        }

        private void ToolButton_Lasso(object sender, RoutedEventArgs e)
        {
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed -= OtherMakePopup;

            inkCanvas.RightTapped += new RightTappedEventHandler(Click_Select);
            //for passing modified input to the app for custom processing
            inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;

            //Listeners for unprocessed pointer events from the modified input
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += UnprocessedInput_PointerPressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased += UnprocessedInput_PointerReleased;

            //Listeners for new ink or erase strokes so that selection could be cleared when inking or erasing is detected
            inkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
            inkCanvas.InkPresenter.StrokesErased += InkPresenter_StrokesErased;
        }

        private void ToolButton_Comment(object sender, RoutedEventArgs e)
        {
            //for passing modified input to the app for custom processing

            //Remove listeners for unprocessed pointer events for selecting strokes
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed -= UnprocessedInput_PointerPressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved -= UnprocessedInput_PointerMoved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased -= UnprocessedInput_PointerReleased;

            inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += OtherMakePopup;
        }

        private void Combine_Strokes(object sender, RoutedEventArgs e)
        {
            bool selectedExists = false;
            var strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            List<InkStroke> selectedStrokes = new List<InkStroke>();
            StrokeGroup strokeGroup = new StrokeGroup();
            foreach (var stroke in strokes)
            {
                if (stroke.Selected)
                {
                    selectedExists = true;
                    selectedStrokes.Add(stroke);

                    if (groups.ContainsKey(stroke))
                    {
                        groups.TryGetValue(stroke, out strokeGroup);
                        strokeGroup.AddStroke(stroke);
                    } else
                    {
                        strokeGroup.AddStroke(stroke);
                        groups.Add(stroke, strokeGroup);
                    }
                }
            }

            if (selectedExists)
            {
                ClearSelection();
            }
        }

        private void Toggle_ActionBar()
        {
            this.combineStrokesButton.IsEnabled = selectedStrokesExist;
            this.drawPathButton.IsEnabled = selectedStrokesExist;
            if (selectedStrokesExist)
            {
                splitView.IsPaneOpen = true;
            }
            else
            {
                splitView.IsPaneOpen = false;
            }
        }

        private void Toggle_ActionBar_Pressed(Object sender, RoutedEventArgs e)
        {
            this.combineStrokesButton.IsEnabled = selectedStrokesExist;
            this.drawPathButton.IsEnabled = selectedStrokesExist;
            splitView.IsPaneOpen = !splitView.IsPaneOpen;
        }

        private void Move_Selected_Mode(Object sender, RoutedEventArgs e)
        {
            var moveButton = new Button()
            {
                Content = "hi",
                //FontFamily = new FontFamily("Segoe MDL2 Assets")

                Width = 50,
                Height = 50
            };

            Canvas.SetLeft(moveButton, boundingRect.X + boundingRect.Width/2 - 25);
            Canvas.SetTop(moveButton, boundingRect.Y + boundingRect.Height/2 - 25);

            selectionCanvas.Children.Add(moveButton);
        }
    }
}
  