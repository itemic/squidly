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
using Windows.UI.Core;
using System.Diagnostics;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HelloWorld
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
  
        private Stack<InkStroke> undoStack { get; set; }

        InkAnalyzer analyzerShape = new InkAnalyzer();
        //IReadOnlyList<InkStroke> strokesShape = null;
        //InkAnalysisResult resultShape = null;

        Dictionary<InkStroke, StrokeGroup> groups = new Dictionary<InkStroke, StrokeGroup>();

        //Stroke selection field
        private Polyline lasso;
        //Stroke selection area
        private Rect boundingRect;
        private bool isBoundRect;

        Symbol LassoSelect = (Symbol)0xEF20;

        public MainPage()
        {
            this.InitializeComponent();

            inkCanvas.InkPresenter.InputDeviceTypes =
                Windows.UI.Core.CoreInputDeviceTypes.Mouse |
                Windows.UI.Core.CoreInputDeviceTypes.Pen;

            Canvas2.InkPresenter.InputDeviceTypes =
               Windows.UI.Core.CoreInputDeviceTypes.Mouse |
               Windows.UI.Core.CoreInputDeviceTypes.Touch |
               Windows.UI.Core.CoreInputDeviceTypes.Pen;

            undoStack = new Stack<InkStroke>();

            inkCanvas.InkPresenter.StrokeInput.StrokeEnded += ClearStack;                                                                                                                                                                                                                                 

            //inkCanvas.RightTapped += new RightTappedEventHandler(CreatePopup);

            //Listeners for new ink or erase strokes so that selection could be cleared when inking or erasing is detected
            inkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
            inkCanvas.InkPresenter.StrokesErased += InkPresenter_StrokesErased;
        }

        private async void CreatePopup(object sender, RightTappedRoutedEventArgs e)
        {
            Point point = e.GetPosition(inkCanvas);
            

            var rectangle = new Rectangle();
            rectangle.Fill = new SolidColorBrush(Windows.UI.Colors.SteelBlue);
            rectangle.Width = 25;
            rectangle.Height = 25;

            Canvas.SetLeft(rectangle, point.X - 12.5);
            Canvas.SetTop(rectangle, point.Y -12.5);


            Flyout flyout = new Flyout();

            TextBlock tb = new TextBlock();
            tb.Text = "New Comment";

            InkCanvas ic = new InkCanvas();
            ic.Width = 250;
            ic.Height = 250;

            InkToolbar it = new InkToolbar();
            it.TargetInkCanvas = ic;
            ic.InkPresenter.InputDeviceTypes =
              Windows.UI.Core.CoreInputDeviceTypes.Mouse |
              Windows.UI.Core.CoreInputDeviceTypes.Touch |
              Windows.UI.Core.CoreInputDeviceTypes.Pen;

            StackPanel sp = new StackPanel();
            sp.Children.Add(tb);
            sp.Children.Add(it);
            sp.Children.Add(ic);

            flyout.Content = sp;

            rectangle.PointerReleased += async delegate (object s, PointerRoutedEventArgs evt)
            {
                ContentDialog noWifiDialog = new ContentDialog
                {
                    Title = "No wifi connection",
                    Content = "Check your connection and try again.",
                    CloseButtonText = "Ok"
                };

                ContentDialogResult result = await noWifiDialog.ShowAsync();
            };

            canvas.Children.Add(rectangle);

            flyout.ShowAt(rectangle);
        }

        private void ClearStack(InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            // clear the stack if a new stroke has been added
            undoStack.Clear();
        }

        private async void saveInk_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Count > 0) {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                savePicker.FileTypeChoices.Add("Gif with embedded ISF", new List<string> { ".gif" });

                var file = await savePicker.PickSaveFileAsync();

                if (null != file)
                {
                    using (IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                    {
                        await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(stream);
                    }
                }
            }
        }

        private async void loadInk_ClickAsync(object sender, RoutedEventArgs e)
        {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
            openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".gif");

            var file = await openPicker.PickSingleFileAsync();

            if (file != null)
            {
                IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                using (var inputStream = stream.GetInputStreamAt(0))
                {
                    await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
                }
                stream.Dispose();
            }
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
            if (e.Parameter is IRandomAccessStream)
            {
                var stream = (IRandomAccessStream)e.Parameter;
                using (var inputStream = stream.GetInputStreamAt(0))
                {
                    await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
                }
                stream.Dispose();
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

            updateBoundingRect(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());

            isBoundRect = false;
            DrawBoundingRect();
        }

        private void updateBoundingRect(IReadOnlyList<InkStroke> strokes)
        {
            StrokeGroup strokeGroup;
            foreach (var stroke in strokes)
            {
                if (stroke.Selected)
                {
                    if (groups.TryGetValue(stroke, out strokeGroup))
                    {
                        strokeGroup.selectStrokesInGroup();
                        boundingRect = strokeGroup.findBoundingBox();
                    }
                }
            }
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
                var rectangle = new Rectangle()
                {
                    Stroke = new SolidColorBrush(Windows.UI.Colors.Blue),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection() { 5, 2 },
                    Width = boundingRect.Width,
                    Height = boundingRect.Height
                };

                Canvas.SetLeft(rectangle, boundingRect.X);
                Canvas.SetTop(rectangle, boundingRect.Y);

                selectionCanvas.Children.Add(rectangle);
            }
        }

        private void ClearSelection()
        {
            var strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            foreach (var stroke in strokes)
            {
                stroke.Selected = false;
            }
            ClearDrawnBoundingRect();
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
            updateBoundingRect(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());
            DrawBoundingRect();
        }

        private void ToolButton_Lasso(object sender, RoutedEventArgs e)
        {

            inkCanvas.RightTapped += new RightTappedEventHandler(Click_Select);
            //for passing modified input to the app for custom processing
            inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;

            //Listeners for unprocessed pointer events from the modified input
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += UnprocessedInput_PointerPressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased += UnprocessedInput_PointerReleased;
        }

        private void Combine_Strokes(object sender, RoutedEventArgs e)
        {
            bool selectedExists = false;
            var strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            //InkStrokeBuilder strokeBuilder = new InkStrokeBuilder();
            //IReadOnlyList<InkPoint> inkPoints = null;
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
                ClearDrawnBoundingRect();
            }
        }

    }
}
